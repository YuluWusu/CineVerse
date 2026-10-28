using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Filters;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using DoAn_QuanLyVeXemPhimTrucTiep.Services;
using Newtonsoft.Json;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    [AdminAuthorize(AllowStaff = true)]
    public class NhanVienController : Controller
    {
        private readonly StaffService staffService;
        private readonly BookingService bookingService;
        private readonly QL_VEPHIMEntities db;

        public NhanVienController()
        {
            this.db = new QL_VEPHIMEntities();
            this.staffService = new StaffService(db);
            this.bookingService = new BookingService(db);
        }

        private string MaNhanVien
        {
            get { return Session["NhanVienMa"] as string; }
        }

        private string MaRap
        {
            get { return Session["NhanVienRap"] as string; }
        }

        // GET: NhanVien/Index
        public ActionResult Index()
        {
            ViewBag.Title = "Tổng quan ca trực";
            List<LICHCHIEU> lichChieu = staffService.GetSchedules(MaRap, 1, false);
            ViewBag.ThongKe = staffService.GetThongKe(MaNhanVien, MaRap);
            ViewBag.ThongKeSuat = staffService.GetThongKeSuat(lichChieu);
            return View(lichChieu);
        }

        #region 1. MÀN HÌNH BÁN VÉ TẠI QUẦY (BOX OFFICE POS)

        // GET: NhanVien/BanVe hoặc NhanVien/POS (Single-page Workflow)
        [HttpGet]
        public ActionResult BanVe(string maRap)
        {
            ViewBag.Title = "Bán vé tại quầy (Box Office POS)";

            // Xác định rạp: ưu tiên từ query -> từ session nhân viên -> rạp đầu tiên
            string selectedRap = !string.IsNullOrEmpty(maRap) ? maRap.Trim() : (MaRap ?? "");
            var dsRap = db.RAPCHIEUx.ToList();
            if (string.IsNullOrEmpty(selectedRap) && dsRap.Any())
            {
                selectedRap = dsRap.First().MA_RAP.Trim();
            }

            ViewBag.DanhSachRap = dsRap;
            ViewBag.SelectedRap = selectedRap;
            ViewBag.MaNhanVien = MaNhanVien;

            // Danh sách suất chiếu còn bán được trong ngày của rạp
            List<LICHCHIEU> lichChieu = staffService.GetSchedules(selectedRap, 1, true);
            ViewBag.ThongKeSuat = staffService.GetThongKeSuat(lichChieu);

            // Menu Bắp nước & Combo
            ViewBag.DanhSachBapNuoc = staffService.GetConcessionsMenu();

            // Danh sách khuyến mãi khả dụng
            ViewBag.DanhSachKM = bookingService.GetActivePromotions();

            return View(lichChieu);
        }

        // Alias URL /NhanVien/POS
        [HttpGet]
        public ActionResult POS(string maRap)
        {
            return RedirectToAction("BanVe", new { maRap = maRap });
        }

        // AJAX GET: Lấy sơ đồ ghế và thông tin chi tiết của 1 suất chiếu
        [HttpGet]
        public ActionResult LaySoDoGhe(string maLichChieu)
        {
            if (string.IsNullOrEmpty(maLichChieu))
            {
                return Json(new { success = false, message = "Thiếu mã suất chiếu." }, JsonRequestBehavior.AllowGet);
            }

            var result = staffService.GetSeatMapForPos(maLichChieu);
            if (result == null)
            {
                return Json(new { success = false, message = "Không tìm thấy suất chiếu." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
        }

        // AJAX GET: Tra cứu nhanh thông tin khách hàng qua Số điện thoại
        [HttpGet]
        public ActionResult TraCuuKhachHang(string sdt)
        {
            var res = staffService.FindCustomerByPhone(sdt);
            if (res == null)
            {
                return Json(new { found = false }, JsonRequestBehavior.AllowGet);
            }
            return Json(res, JsonRequestBehavior.AllowGet);
        }

        // AJAX POST: Xử lý giao dịch bán vé tại quầy POS
        [HttpPost]
        public ActionResult XuLyBanVePOS(
            string maLichChieu,
            string selectedSeats,
            string fnbJson,
            string sdtKhach,
            string hoTenKhach,
            string maKm,
            string paymentMethod = "TienMat",
            decimal? tienKhachDua = null)
        {
            try
            {
                var danhSachGhe = new List<string>();
                if (!string.IsNullOrWhiteSpace(selectedSeats))
                {
                    danhSachGhe = selectedSeats.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                var bapNuocDict = new Dictionary<string, int>();
                if (!string.IsNullOrWhiteSpace(fnbJson))
                {
                    bapNuocDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(fnbJson) ?? new Dictionary<string, int>();
                }

                string maRapXuLy = MaRap;
                if (string.IsNullOrEmpty(maRapXuLy))
                {
                    var lc = staffService.GetSchedule(maLichChieu);
                    maRapXuLy = lc?.PHONGCHIEU?.MA_RAP?.Trim();
                }

                string staffId = !string.IsNullOrEmpty(MaNhanVien) ? MaNhanVien : "TK005"; // fallback nhân viên trực

                string maDonHang = staffService.BanVePOS(
                    staffId,
                    maRapXuLy,
                    maLichChieu,
                    danhSachGhe,
                    bapNuocDict,
                    sdtKhach,
                    hoTenKhach,
                    maKm,
                    paymentMethod,
                    tienKhachDua);

                return Json(new
                {
                    success = true,
                    message = "Xuất vé và thanh toán thành công!",
                    maDonHang = maDonHang,
                    receiptUrl = Url.Action("InVeK80", "NhanVien", new { id = maDonHang })
                });
            }
            catch (LoiNghiepVuException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi xử lý giao dịch: " + ex.Message });
            }
        }

        // GET: In vé nhiệt khổ 80mm (K80 Thermal Receipt)
        [HttpGet]
        public ActionResult InVeK80(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();
            var donHang = staffService.GetOrder(id);
            if (donHang == null) return HttpNotFound();

            ViewBag.Title = "Vé & Hóa đơn POS " + donHang.MA_DONHANG.Trim();
            return View(donHang);
        }

        // Legacy ChonGhe & XuLyBanVe giữ lại để tương thích ngược 100%
        [HttpGet]
        public ActionResult ChonGhe(string maLichChieu)
        {
            return RedirectToAction("BanVe", new { maRap = MaRap });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XuLyBanVe(string maLichChieu, string selectedSeats, string sdtKhach, string maKm)
        {
            return RedirectToAction("BanVe", new { maRap = MaRap });
        }

        [HttpGet]
        public ActionResult HoaDon(string id)
        {
            return RedirectToAction("InVeK80", new { id = id });
        }

        #endregion

        #region 2. MÀN HÌNH SOÁT VÉ & NHẬN BẮP NƯỚC (QR SCANNER)

        // GET: NhanVien/CheckIn
        [HttpGet]
        public ActionResult CheckIn(string tuKhoa)
        {
            ViewBag.Title = "Soát vé & Nhận bắp nước (QR Scanner)";
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaRap = MaRap;
            ViewBag.MaNhanVien = MaNhanVien;

            // Lấy danh sách vé gần đây nếu có tìm kiếm
            var danhSachTimKiem = staffService.TimVe(tuKhoa);
            return View(danhSachTimKiem);
        }

        // AJAX POST: Quét mã QR hoặc nhập mã vé Check-in
        [HttpPost]
        public ActionResult QuetMaQRCheckIn(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Vui lòng quét hoặc nhập mã vé." });
            }

            var result = staffService.KiemTraCheckInChiTiet(code, MaRap);
            return Json(new { success = true, data = result });
        }

        // AJAX POST: Xác nhận cho khách vào rạp
        [HttpPost]
        public ActionResult XacNhanVaoRap(string maDonHang)
        {
            if (string.IsNullOrWhiteSpace(maDonHang))
            {
                return Json(new { success = false, message = "Mã đơn hàng không hợp lệ." });
            }

            string staffId = !string.IsNullOrEmpty(MaNhanVien) ? MaNhanVien : "TK005";
            bool ok = staffService.XacNhanCheckInVaoRap(maDonHang, staffId);

            if (ok)
            {
                return Json(new
                {
                    success = true,
                    message = "Xác nhận Check-in thành công! Mời quý khách vào phòng chiếu.",
                    thoiGianCheckIn = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    nhanVien = staffId
                });
            }

            return Json(new { success = false, message = "Không thể cập nhật trạng thái soát vé." });
        }

        // AJAX POST: Xác nhận đã giao bắp nước
        [HttpPost]
        public ActionResult XacNhanGiaoBap(string maDonHang)
        {
            if (string.IsNullOrWhiteSpace(maDonHang))
            {
                return Json(new { success = false, message = "Mã đơn hàng không hợp lệ." });
            }

            bool ok = staffService.XacNhanGiaoBapNuoc(maDonHang);
            if (ok)
            {
                return Json(new { success = true, message = "Đã xác nhận giao bắp nước cho khách hàng!" });
            }
            return Json(new { success = false, message = "Không thể cập nhật trạng thái bắp nước." });
        }

        #endregion

        #region 3. LỊCH SỬ BÁN VÉ

        [HttpGet]
        public ActionResult LichSuBan()
        {
            ViewBag.Title = "Lịch sử đơn bán tại quầy";
            return View(staffService.GetOrdersByStaff(MaNhanVien, DateTime.Today));
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
