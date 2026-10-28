using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class MoviesController : Controller
    {
        private QL_VEPHIMEntities data = new QL_VEPHIMEntities();

        public ActionResult DanhSachPhim(string trangthai = null, string search = null, string status = null, string maTheLoai = null, string sortOrder = null)
        {
            string filterStatus = !string.IsNullOrEmpty(trangthai) ? trangthai : status;

            var query = data.PHIMs.Include(p => p.THELOAIs).AsQueryable();

            // 1. Tìm kiếm từ khóa (Tên phim, đạo diễn, diễn viên, thể loại)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim();
                query = query.Where(p => p.TEN_PHIM.Contains(kw)
                                      || (p.DAO_DIEN != null && p.DAO_DIEN.Contains(kw))
                                      || (p.DIEN_VIEN != null && p.DIEN_VIEN.Contains(kw))
                                      || p.THELOAIs.Any(t => t.TEN_THELOAI.Contains(kw)));
                ViewBag.Search = kw;
            }

            // 2. Lọc theo Trạng thái (Đang chiếu, Sắp chiếu, Ngừng chiếu)
            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "TatCa")
            {
                query = query.Where(p => p.TRANGTHAI == filterStatus);
                ViewBag.TrangThai = filterStatus;
                ViewBag.CurrentStatus = filterStatus;
            }
            else if (string.IsNullOrWhiteSpace(search) && string.IsNullOrWhiteSpace(maTheLoai))
            {
                query = query.Where(p => p.TRANGTHAI == "Đang chiếu");
                ViewBag.TrangThai = "Đang chiếu";
                ViewBag.CurrentStatus = "Đang chiếu";
            }

            // 3. Lọc theo Thể loại phim từ CSDL (THELOAI)
            if (!string.IsNullOrWhiteSpace(maTheLoai) && maTheLoai != "TatCa")
            {
                string tlId = maTheLoai.Trim();
                query = query.Where(p => p.THELOAIs.Any(t => t.MA_THELOAI.Trim() == tlId));
                ViewBag.MaTheLoai = tlId;
                var tlObj = data.THELOAIs.FirstOrDefault(t => t.MA_THELOAI.Trim() == tlId);
                if (tlObj != null) ViewBag.TenTheLoai = tlObj.TEN_THELOAI;
            }

            // 4. Sắp xếp danh sách phim (Sort)
            ViewBag.SortOrder = sortOrder;
            switch (sortOrder)
            {
                case "ten_asc":
                    query = query.OrderBy(p => p.TEN_PHIM);
                    break;
                case "ten_desc":
                    query = query.OrderByDescending(p => p.TEN_PHIM);
                    break;
                case "thoiluong_asc":
                    query = query.OrderBy(p => p.THOILUONG);
                    break;
                case "thoiluong_desc":
                    query = query.OrderByDescending(p => p.THOILUONG);
                    break;
                case "ngay_asc":
                    query = query.OrderBy(p => p.NGAYKHOICHIEU);
                    break;
                case "ngay_desc":
                default:
                    query = query.OrderByDescending(p => p.NGAYKHOICHIEU);
                    break;
            }

            // Đưa toàn bộ danh sách Thể loại sang View để render bộ lọc tab/pills
            ViewBag.TheLoais = data.THELOAIs.OrderBy(t => t.TEN_THELOAI).ToList();

            var phims = query.ToList();
            ViewBag.DanhSachPhim = phims;
            return View(phims);
        }

        public ActionResult ChiTietPhim(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();
            string maPhim = id.Trim();

            PHIM phim = data.PHIMs.FirstOrDefault(p => p.MA_PHIM.Trim() == maPhim);
            if (phim == null)
            {
                return HttpNotFound();
            }

            var lichChieus = data.LICHCHIEUx
                                .Include(l => l.KHUNGGIO)
                                .Include(l => l.PHONGCHIEU.RAPCHIEU)
                                .Where(l => l.MA_PHIM.Trim() == maPhim)
                                .OrderByDescending(l => l.NGAYCHIEU)
                                .ThenBy(l => l.KHUNGGIO.GIOBATDAU)
                                .ToList();

            ViewBag.LichChieu = lichChieus;

            // Lấy danh sách đánh giá đã duyệt hiển thị
            var danhGias = data.DANHGIAs
                .Include(d => d.KHACHHANG.TAIKHOAN)
                .Where(d => d.MA_PHIM.Trim() == maPhim && d.TRANGTHAI == "HienThi")
                .OrderByDescending(d => d.NGAYDANHGIA)
                .ToList();

            int tongDanhGia = danhGias.Count;
            double diemTB = tongDanhGia > 0 ? Math.Round(danhGias.Average(d => d.SOSAO), 1) : 5.0;

            int[] starCounts = new int[6];
            foreach (var dg in danhGias)
            {
                if (dg.SOSAO >= 1 && dg.SOSAO <= 5)
                {
                    starCounts[dg.SOSAO]++;
                }
            }

            // Kiểm tra phân quyền đánh giá của khách hiện tại
            bool daDangNhap = false;
            bool daMuaVe = false;
            bool daDanhGia = false;
            var sessionKh = Session["KhachHang"] as KHACHHANG;

            if (sessionKh != null)
            {
                daDangNhap = true;
                string maKh = sessionKh.MA_KHACHHANG.Trim();
                daDanhGia = data.DANHGIAs.Any(d => d.MA_PHIM.Trim() == maPhim && d.MA_KHACHHANG.Trim() == maKh);

                // Khách hàng phải có ít nhất 1 đơn hàng thanh toán thành công của phim này
                daMuaVe = data.DONHANGs.Any(d => d.MA_KHACHHANG.Trim() == maKh
                    && (d.TRANGTHAI == "DaThanhToan" || d.TRANGTHAITHANHTOAN == "DaThanhToan")
                    && d.LICHCHIEU.MA_PHIM.Trim() == maPhim);
            }

            ViewBag.DanhGias = danhGias;
            ViewBag.TongDanhGia = tongDanhGia;
            ViewBag.DiemTB = diemTB;
            ViewBag.StarCounts = starCounts;
            ViewBag.DaDangNhap = daDangNhap;
            ViewBag.DaMuaVe = daMuaVe;
            ViewBag.DaDanhGia = daDanhGia;

            // Lấy danh sách phim liên quan cùng thể loại (N-N THELOAI)
            var maTheLoais = phim.THELOAIs.Select(t => t.MA_THELOAI.Trim()).ToList();
            var phimLienQuan = data.PHIMs
                .Include(p => p.THELOAIs)
                .Where(p => p.MA_PHIM.Trim() != maPhim && p.THELOAIs.Any(t => maTheLoais.Contains(t.MA_THELOAI.Trim())))
                .OrderByDescending(p => p.NGAYKHOICHIEU)
                .Take(5)
                .ToList();

            if (phimLienQuan.Count < 4)
            {
                var additional = data.PHIMs
                    .Include(p => p.THELOAIs)
                    .Where(p => p.MA_PHIM.Trim() != maPhim && !phimLienQuan.Select(pl => pl.MA_PHIM.Trim()).Contains(p.MA_PHIM.Trim()) && p.TRANGTHAI == "Đang chiếu")
                    .Take(4 - phimLienQuan.Count)
                    .ToList();
                phimLienQuan.AddRange(additional);
            }
            ViewBag.PhimLienQuan = phimLienQuan;

            return View(phim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiDanhGia(string maPhim, int soSao, string noiDung)
        {
            if (string.IsNullOrEmpty(maPhim))
            {
                return RedirectToAction("DanhSachPhim");
            }
            string phimId = maPhim.Trim();

            var sessionKh = Session["KhachHang"] as KHACHHANG;
            if (sessionKh == null)
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để gửi đánh giá phim.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("ChiTietPhim", new { id = phimId });
            }

            string maKh = sessionKh.MA_KHACHHANG.Trim();

            // Kiểm tra khách đã từng mua vé phim này chưa
            bool daMuaVe = data.DONHANGs.Any(d => d.MA_KHACHHANG.Trim() == maKh
                && (d.TRANGTHAI == "DaThanhToan" || d.TRANGTHAITHANHTOAN == "DaThanhToan")
                && d.LICHCHIEU.MA_PHIM.Trim() == phimId);

            if (!daMuaVe)
            {
                TempData["ToastMessage"] = "Chỉ khán giả đã mua vé xem phim này tại CinéVerse mới có thể gửi đánh giá xác thực.";
                TempData["ToastType"] = "error";
                return RedirectToAction("ChiTietPhim", new { id = phimId });
            }

            // Kiểm tra đã đánh giá chưa (mỗi khách 1 lần/phim)
            bool daDanhGia = data.DANHGIAs.Any(d => d.MA_PHIM.Trim() == phimId && d.MA_KHACHHANG.Trim() == maKh);
            if (daDanhGia)
            {
                TempData["ToastMessage"] = "Bạn đã gửi đánh giá cho bộ phim này trước đó. Cảm ơn phản hồi của bạn!";
                TempData["ToastType"] = "info";
                return RedirectToAction("ChiTietPhim", new { id = phimId });
            }

            if (soSao < 1 || soSao > 5) soSao = 5;
            string reviewText = noiDung?.Trim();
            if (string.IsNullOrEmpty(reviewText))
            {
                TempData["ToastMessage"] = "Vui lòng nhập nội dung đánh giá của bạn.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("ChiTietPhim", new { id = phimId });
            }

            // Sinh mã đánh giá
            var allDgIds = data.DANHGIAs.Select(d => d.MA_DANHGIA).ToList();
            int maxDgId = 0;
            foreach (var id in allDgIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    string trimmed = id.Trim();
                    if (trimmed.StartsWith("DG") && int.TryParse(trimmed.Substring(2), out int cur))
                    {
                        if (cur > maxDgId) maxDgId = cur;
                    }
                }
            }
            string maDgMoi = "DG" + (maxDgId + 1).ToString("D3");

            var dgMoi = new DANHGIA
            {
                MA_DANHGIA = maDgMoi,
                MA_PHIM = phimId,
                MA_KHACHHANG = maKh,
                SOSAO = soSao,
                NOIDUNG = reviewText,
                NGAYDANHGIA = DateTime.Now,
                TRANGTHAI = "HienThi"
            };

            data.DANHGIAs.Add(dgMoi);
            data.SaveChanges();

            TempData["ToastMessage"] = "Cảm ơn bạn! Đánh giá xác thực của bạn đã được đăng thành công.";
            TempData["ToastType"] = "success";
            return RedirectToAction("ChiTietPhim", new { id = phimId });
        }

        public ActionResult LichChieuChung()
        {
            var lichChieus = data.LICHCHIEUx
                                .Include(l => l.PHIM)
                                .Include(l => l.KHUNGGIO)
                                .Include(l => l.PHONGCHIEU.RAPCHIEU)
                                .OrderByDescending(l => l.NGAYCHIEU)
                                .ThenBy(l => l.PHIM.TEN_PHIM)
                                .ToList();
            return View(lichChieus);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                data?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
