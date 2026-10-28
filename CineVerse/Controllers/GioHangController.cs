using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using DoAn_QuanLyVeXemPhimTrucTiep.Services;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class GioHangController : Controller
    {
        private QL_VEPHIMEntities db = new QL_VEPHIMEntities();

        #region HELPER SESSION GIỎ HÀNG

        /// <summary>
        /// Lấy danh sách sản phẩm trong giỏ hàng từ Session. Nếu chưa có thì khởi tạo mới.
        /// </summary>
        public List<GioHangItem> GetGioHang()
        {
            var gioHang = Session["GioHang"] as List<GioHangItem>;
            if (gioHang == null)
            {
                gioHang = new List<GioHangItem>();
                Session["GioHang"] = gioHang;
            }
            return gioHang;
        }

        /// <summary>
        /// Badge hiển thị số lượng item giỏ hàng trên thanh điều hướng Header.
        /// </summary>
        [ChildActionOnly]
        public ActionResult GioHangBadge()
        {
            var gioHang = GetGioHang();
            int totalCount = gioHang.Sum(i => i.SoLuong);
            return PartialView("_GioHangPartial", totalCount);
        }

        #endregion

        #region XEM & QUẢN LÝ GIỎ HÀNG (Barem 1.5 điểm)

        // GET: /GioHang/Index
        public ActionResult Index(string maKM = null)
        {
            var gioHang = GetGioHang();
            var model = new GioHangViewModel
            {
                Items = gioHang
            };

            // 1. Tự động lấy thông tin khách hàng nếu đã đăng nhập
            var kh = Session["KhachHang"] as KHACHHANG;
            if (kh != null)
            {
                model.HoTen = kh.HOTEN;
                model.Email = kh.EMAIL;
                model.SoDienThoai = kh.SDT;
                model.HangThanhVien = !string.IsNullOrEmpty(kh.HANGTHANHVIEN) ? kh.HANGTHANHVIEN.Trim().ToUpper() : "STANDARD";

                // Chiết khấu thành viên theo barem Loyalty: VIP 5%, VVIP 10%
                if (model.HangThanhVien == "VVIP")
                {
                    model.PhanTramChietKhau = 10;
                }
                else if (model.HangThanhVien == "VIP")
                {
                    model.PhanTramChietKhau = 5;
                }
                else
                {
                    model.PhanTramChietKhau = 0;
                }

                if (model.PhanTramChietKhau > 0 && model.TongTienHang > 0)
                {
                    model.ChietKhauThanhVien = Math.Round(model.TongTienHang * (model.PhanTramChietKhau / 100m));
                }
            }

            // 2. Xử lý mã khuyến mãi nếu có
            string promoCode = maKM ?? Session["MaKhuyenMaiGioHang"] as string;
            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                promoCode = promoCode.Trim().ToUpper();
                var km = db.KHUYENMAIs.FirstOrDefault(k => (k.MA_KM.Trim().ToUpper() == promoCode || (k.MA_CODE != null && k.MA_CODE.Trim().ToUpper() == promoCode)) && k.TRANGTHAI == "HoatDong");
                if (km != null && DateTime.Now >= km.NGAYBATDAU && DateTime.Now <= km.NGAYKETTHUC)
                {
                    model.MaKhuyenMai = km.MA_KM.Trim();
                    decimal discount = 0m;
                    if (km.GIAMGIA_PHANTRAM > 0)
                    {
                        discount = model.TongTienHang * (km.GIAMGIA_PHANTRAM / 100m);
                        if (km.GIAMGIA_TOIDA.HasValue && discount > km.GIAMGIA_TOIDA.Value)
                        {
                            discount = km.GIAMGIA_TOIDA.Value;
                        }
                    }

                    if (discount > model.TongTienHang) discount = model.TongTienHang;
                    model.GiamGiaKhuyenMai = discount;
                    Session["MaKhuyenMaiGioHang"] = model.MaKhuyenMai;
                    ViewBag.KMTen = km.TEN_KM;
                }
                else
                {
                    ViewBag.KMLoi = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn.";
                    Session.Remove("MaKhuyenMaiGioHang");
                }
            }

            return View(model);
        }

        #endregion

        #region THÊM SẢN PHẨM VÀO GIỎ HÀNG (Mua 1 & Mua N có tùy chọn)

        /// <summary>
        /// Mua 1 sản phẩm nhanh hoặc mua N sản phẩm Bắp Nước kèm tùy chọn Size và Số lượng.
        /// </summary>
        [HttpPost]
        public ActionResult ThemBapNuocVaoGio(string maSanPham, string tuyChon = "Tiêu chuẩn (Size Vừa)", int soLuong = 1, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(maSanPham))
            {
                TempData["ToastMessage"] = "Sản phẩm không hợp lệ.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("CuaHangBapNuoc");
            }

            if (soLuong <= 0) soLuong = 1;

            string id = maSanPham.Trim();
            var sp = db.BAPNUOCs.FirstOrDefault(b => b.MA_SANPHAM.Trim() == id);
            if (sp == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy món ăn/nước uống.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("CuaHangBapNuoc");
            }

            var gioHang = GetGioHang();

            // Tính đơn giá theo tùy chọn Size
            decimal donGia = sp.GIABAN;
            if (!string.IsNullOrEmpty(tuyChon))
            {
                if (tuyChon.Contains("Size Lớn") || tuyChon.Contains("Size L"))
                {
                    donGia += 10000m;
                }
                else if (tuyChon.Contains("Size Khổng lồ") || tuyChon.Contains("Size XL"))
                {
                    donGia += 20000m;
                }
                else if (tuyChon.Contains("Size Nhỏ") || tuyChon.Contains("Size S"))
                {
                    donGia = Math.Max(0m, donGia - 10000m);
                }
            }

            // Key duy nhất cho sản phẩm kèm tùy chọn size
            string itemKey = $"FNB_{id}_{tuyChon?.Replace(" ", "")}";

            var tonTai = gioHang.FirstOrDefault(i => i.MaItem == itemKey);
            if (tonTai != null)
            {
                tonTai.SoLuong += soLuong;
            }
            else
            {
                gioHang.Add(new GioHangItem
                {
                    LoaiItem = "BAPNUOC",
                    MaItem = itemKey,
                    MaBapNuoc = sp.MA_SANPHAM.Trim(),
                    TenSanPham = sp.TEN_SANPHAM,
                    HinhAnh = sp.HINHANH,
                    TuyChon = tuyChon ?? "Size Vừa",
                    DonGia = donGia,
                    SoLuong = soLuong
                });
            }

            TempData["ToastMessage"] = $"Đã thêm {soLuong}x {sp.TEN_SANPHAM} ({tuyChon}) vào giỏ hàng!";
            TempData["ToastType"] = "success";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Thêm danh sách Ghế xem phim đã chọn tại trang ChonGhe vào Giỏ hàng
        /// </summary>
        [HttpPost]
        public ActionResult ThemGheVaoGio(string maLichChieu, string danhSachGhe, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(maLichChieu) || string.IsNullOrWhiteSpace(danhSachGhe))
            {
                TempData["ToastMessage"] = "Vui lòng chọn ít nhất 1 ghế để thêm vào giỏ hàng.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("DanhSachPhim", "Movies");
            }

            string lcId = maLichChieu.Trim();
            var lc = db.LICHCHIEUx
                .Include(l => l.PHIM)
                .Include(l => l.PHONGCHIEU.RAPCHIEU)
                .Include(l => l.KHUNGGIO)
                .FirstOrDefault(l => l.MA_LICHCHIEU.Trim() == lcId);

            if (lc == null)
            {
                TempData["ToastMessage"] = "Suất chiếu không tồn tại.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("DanhSachPhim", "Movies");
            }

            var listGhe = danhSachGhe.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(g => g.Trim())
                                     .Distinct()
                                     .ToList();

            var gioHang = GetGioHang();
            int countAdded = 0;

            foreach (var maGhe in listGhe)
            {
                var ghe = db.GHEs.Include(g => g.LOAIGHE).FirstOrDefault(g => g.MA_GHE.Trim() == maGhe);
                if (ghe == null) continue;

                string itemKey = $"VE_{lcId}_{maGhe}";
                if (!gioHang.Any(i => i.MaItem == itemKey))
                {
                    decimal giaVe = (ghe.LOAIGHE?.GIATVE ?? 75000m) + (lc.GIATVE_PHUTROI ?? 0m);

                    gioHang.Add(new GioHangItem
                    {
                        LoaiItem = "VE",
                        MaItem = itemKey,
                        MaLichChieu = lcId,
                        MaPhim = lc.MA_PHIM.Trim(),
                        TenPhim = lc.PHIM?.TEN_PHIM,
                        TenPhong = lc.PHONGCHIEU?.TEN_PHONG,
                        TenRap = lc.PHONGCHIEU?.RAPCHIEU?.TEN_RAP,
                        MaGhe = ghe.MA_GHE.Trim(),
                        TenGhe = !string.IsNullOrEmpty(ghe.SOGHE) ? ghe.SOGHE.Trim() : ghe.MA_GHE.Trim(),
                        LoaiGhe = ghe.LOAIGHE?.TEN_LOAIGHE ?? "Ghế thường",
                        NgayChieu = lc.NGAYCHIEU,
                        GioChieu = lc.KHUNGGIO?.GIOBATDAU,
                        TenSanPham = $"Vé xem phim: {lc.PHIM?.TEN_PHIM}",
                        HinhAnh = lc.PHIM?.POSTER,
                        TuyChon = $"Ghế {ghe.SOGHE} ({ghe.LOAIGHE?.TEN_LOAIGHE}) - {lc.PHONGCHIEU?.TEN_PHONG} ({lc.NGAYCHIEU:dd/MM/yyyy} {lc.KHUNGGIO?.GIOBATDAU:hh\\:mm})",
                        DonGia = giaVe,
                        SoLuong = 1
                    });
                    countAdded++;
                }
            }

            TempData["ToastMessage"] = $"Đã thêm thành công {countAdded} vé ghế vào giỏ hàng!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index");
        }

        #endregion

        #region HIỆU CHỈNH SỐ LƯỢNG & XÓA KHỎI GIỎ HÀNG (Barem đầy đủ)

        /// <summary>
        /// Cập nhật số lượng của một dòng sản phẩm trong giỏ hàng.
        /// </summary>
        [HttpPost]
        public ActionResult CapNhatSoLuong(string maItem, int soLuong)
        {
            var gioHang = GetGioHang();
            var item = gioHang.FirstOrDefault(i => i.MaItem == maItem);
            if (item != null)
            {
                if (item.LoaiItem == "VE")
                {
                    item.SoLuong = 1;
                }
                else
                {
                    if (soLuong <= 0)
                    {
                        gioHang.Remove(item);
                        TempData["ToastMessage"] = $"Đã xóa {item.TenSanPham} khỏi giỏ hàng.";
                    }
                    else
                    {
                        item.SoLuong = soLuong;
                        TempData["ToastMessage"] = $"Đã cập nhật số lượng cho {item.TenSanPham}: {soLuong}.";
                    }
                }
                TempData["ToastType"] = "success";
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa 1 dòng sản phẩm khỏi giỏ hàng.
        /// </summary>
        [HttpPost]
        public ActionResult XoaItem(string maItem)
        {
            var gioHang = GetGioHang();
            var item = gioHang.FirstOrDefault(i => i.MaItem == maItem);
            if (item != null)
            {
                gioHang.Remove(item);
                TempData["ToastMessage"] = $"Đã xóa {item.TenSanPham} khỏi giỏ hàng.";
                TempData["ToastType"] = "info";
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa toàn bộ sản phẩm khỏi giỏ hàng.
        /// </summary>
        [HttpPost]
        public ActionResult XoaGioHang()
        {
            Session.Remove("GioHang");
            Session.Remove("MaKhuyenMaiGioHang");
            TempData["ToastMessage"] = "Đã xóa toàn bộ giỏ hàng.";
            TempData["ToastType"] = "info";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Áp dụng mã khuyến mãi vào giỏ hàng.
        /// </summary>
        [HttpPost]
        public ActionResult ApDungKhuyenMai(string maKM)
        {
            if (string.IsNullOrWhiteSpace(maKM))
            {
                Session.Remove("MaKhuyenMaiGioHang");
                TempData["ToastMessage"] = "Đã hủy áp dụng mã khuyến mãi.";
                TempData["ToastType"] = "info";
                return RedirectToAction("Index");
            }

            string promo = maKM.Trim().ToUpper();
            var km = db.KHUYENMAIs.FirstOrDefault(k => (k.MA_KM.Trim().ToUpper() == promo || (k.MA_CODE != null && k.MA_CODE.Trim().ToUpper() == promo)) && k.TRANGTHAI == "HoatDong");
            if (km != null && DateTime.Now >= km.NGAYBATDAU && DateTime.Now <= km.NGAYKETTHUC)
            {
                Session["MaKhuyenMaiGioHang"] = km.MA_KM.Trim();
                TempData["ToastMessage"] = $"Áp dụng thành công mã {km.MA_KM}: {km.TEN_KM}!";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Mã khuyến mãi không tồn tại hoặc đã hết hiệu lực.";
                TempData["ToastType"] = "danger";
            }
            return RedirectToAction("Index");
        }

        #endregion

        #region XÁC NHẬN ĐẶT HÀNG & LƯU CSDL EF6 (DONHANG, VE, CHITIET_BAPNUOC)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhanDatHang(GioHangViewModel form)
        {
            var gioHang = GetGioHang();
            if (gioHang == null || !gioHang.Any())
            {
                TempData["ToastMessage"] = "Giỏ hàng của bạn đang trống. Vui lòng chọn vé hoặc bắp nước trước khi thanh toán.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index");
            }

            // 1. Xác định khách hàng đặt
            string maKh = null;
            var sessionKh = Session["KhachHang"] as KHACHHANG;
            if (sessionKh != null)
            {
                maKh = sessionKh.MA_KHACHHANG.Trim();
            }
            else
            {
                // Nếu khách chưa đăng nhập, gán vào tài khoản khách hàng mặc định đầu tiên trong CSDL
                var defaultKh = db.KHACHHANGs.FirstOrDefault();
                if (defaultKh != null)
                {
                    maKh = defaultKh.MA_KHACHHANG.Trim();
                }
            }

            // 2. Lấy mã lịch chiếu (từ vé đầu tiên nếu có, hoặc lịch chiếu mới nhất đang mở)
            var firstVe = gioHang.FirstOrDefault(i => i.LoaiItem == "VE");
            string maLichChieu = firstVe != null ? firstVe.MaLichChieu : db.LICHCHIEUx.Select(l => l.MA_LICHCHIEU).FirstOrDefault();

            // 3. Tính toán các khoản tiền
            decimal tienVe = gioHang.Where(i => i.LoaiItem == "VE").Sum(i => i.ThanhTien);
            decimal tienMonAn = gioHang.Where(i => i.LoaiItem == "BAPNUOC").Sum(i => i.ThanhTien);
            decimal tongTienHang = tienVe + tienMonAn;

            // Chiết khấu thành viên
            decimal chietKhau = 0m;
            if (sessionKh != null)
            {
                string rank = !string.IsNullOrEmpty(sessionKh.HANGTHANHVIEN) ? sessionKh.HANGTHANHVIEN.Trim().ToUpper() : "STANDARD";
                if (rank == "VVIP") chietKhau = Math.Round(tongTienHang * 0.10m);
                else if (rank == "VIP") chietKhau = Math.Round(tongTienHang * 0.05m);
            }

            // Giảm giá khuyến mãi
            decimal giamGiaKM = 0m;
            string promoCode = Session["MaKhuyenMaiGioHang"] as string;
            string maKmDb = null;
            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                var km = db.KHUYENMAIs.FirstOrDefault(k => (k.MA_KM.Trim().ToUpper() == promoCode.ToUpper() || (k.MA_CODE != null && k.MA_CODE.Trim().ToUpper() == promoCode.ToUpper())) && k.TRANGTHAI == "HoatDong");
                if (km != null && km.GIAMGIA_PHANTRAM > 0)
                {
                    maKmDb = km.MA_KM.Trim();
                    giamGiaKM = tongTienHang * (km.GIAMGIA_PHANTRAM / 100m);
                    if (km.GIAMGIA_TOIDA.HasValue && giamGiaKM > km.GIAMGIA_TOIDA.Value)
                    {
                        giamGiaKM = km.GIAMGIA_TOIDA.Value;
                    }
                }
            }

            decimal tongGiam = chietKhau + giamGiaKM;
            decimal tongThanhToan = Math.Max(0m, tongTienHang - tongGiam);

            // 4. Tạo DONHANG
            string maDonHang = "DH" + DateTime.Now.ToString("yyyyMMddHHmmss") + (new Random().Next(100, 999));
            string maCheckIn = "CV-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            var donHang = new DONHANG
            {
                MA_DONHANG = maDonHang,
                MA_KHACHHANG = maKh,
                MA_LICHCHIEU = maLichChieu,
                MA_KM = maKmDb,
                NGAYDAT = DateTime.Now,
                TIENVE = tienVe,
                TIENMONAN = tienMonAn,
                TIENGIAM = tongGiam,
                TONGTIEN = tongThanhToan,
                TRANGTHAI = "ChoXacNhan",
                PHUONGTHUCTHANHTOAN = form.PhuongThucThanhToan ?? "VietQR",
                TRANGTHAITHANHTOAN = (form.PhuongThucThanhToan == "TaiQuay") ? "ChuaThanhToan" : "DaThanhToan",
                MACHECKIN = maCheckIn,
                TRANGTHAISOATVE = "ChuaSoatVe",
                TRANGTHAINHANBAPNUOC = tienMonAn > 0 ? "ChuaNhan" : "KhongCo"
            };

            db.DONHANGs.Add(donHang);

            // 5. Lưu VEs nếu có
            int idxVe = 1;
            foreach (var item in gioHang.Where(i => i.LoaiItem == "VE"))
            {
                string maVe = "VE" + DateTime.Now.ToString("yyMMddHHmmss") + (idxVe++);
                var ve = new VE
                {
                    MA_VE = maVe,
                    MA_DONHANG = maDonHang,
                    MA_LICHCHIEU = item.MaLichChieu,
                    MA_GHE = item.MaGhe,
                    DONGIA = item.DonGia,
                    TRANGTHAI = "DaBan"
                };
                db.VEs.Add(ve);
            }

            // 6. Lưu CHITIET_BAPNUOC nếu có
            int idxCt = 1;
            foreach (var item in gioHang.Where(i => i.LoaiItem == "BAPNUOC"))
            {
                string maCt = "CTB" + DateTime.Now.ToString("yyMMddHHmmss") + (idxCt++);
                var ct = new CHITIET_BAPNUOC
                {
                    MA_CHITIET = maCt,
                    MA_DONHANG = maDonHang,
                    MA_SANPHAM = item.MaBapNuoc,
                    SOLUONG = item.SoLuong,
                    DONGIA = item.DonGia,
                    THANHTIEN = item.ThanhTien
                };
                db.CHITIET_BAPNUOC.Add(ct);
            }

            // 7. Cộng điểm tích lũy thành viên nếu có
            if (!string.IsNullOrEmpty(maKh))
            {
                LoyaltyService.Instance.CongDiemVaXetHang(maKh, donHang.TONGTIEN, db);
            }

            db.SaveChanges();

            // 8. Dọn dẹp giỏ hàng
            Session.Remove("GioHang");
            Session.Remove("MaKhuyenMaiGioHang");

            TempData["ToastMessage"] = "Đặt hàng thành công! Mã đơn của bạn là: " + maDonHang;
            TempData["ToastType"] = "success";

            return RedirectToAction("DatHangThanhCong", new { id = maDonHang });
        }

        // GET: /GioHang/DatHangThanhCong/{id}
        public ActionResult DatHangThanhCong(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("TrangChu", "Home");

            string maDh = id.Trim();
            var donHang = db.DONHANGs
                .Include(d => d.KHACHHANG)
                .Include(d => d.KHACHHANG.TAIKHOAN)
                .Include(d => d.LICHCHIEU.PHIM)
                .Include(d => d.LICHCHIEU.PHONGCHIEU.RAPCHIEU)
                .Include(d => d.LICHCHIEU.KHUNGGIO)
                .Include(d => d.VEs.Select(v => v.GHE.LOAIGHE))
                .Include(d => d.CHITIET_BAPNUOC.Select(c => c.BAPNUOC))
                .FirstOrDefault(d => d.MA_DONHANG.Trim() == maDh);

            if (donHang == null) return HttpNotFound();

            return View(donHang);
        }

        #endregion

        #region CỬA HÀNG BẮP NƯỚC ĐỘC LẬP (F&B Catalog)

        // GET: /GioHang/CuaHangBapNuoc
        public ActionResult CuaHangBapNuoc(string phanLoai = null, string search = null)
        {
            var query = db.BAPNUOCs.Where(b => b.TRANGTHAI == "DangBan").AsQueryable();

            if (!string.IsNullOrWhiteSpace(phanLoai) && phanLoai != "TatCa")
            {
                query = query.Where(b => b.PHANLOAI == phanLoai);
                ViewBag.PhanLoai = phanLoai;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim();
                query = query.Where(b => b.TEN_SANPHAM.Contains(kw) || (b.MOTA != null && b.MOTA.Contains(kw)));
                ViewBag.Search = kw;
            }

            var list = query.OrderBy(b => b.PHANLOAI).ThenBy(b => b.GIABAN).ToList();
            ViewBag.PhanLoaiList = db.BAPNUOCs.Where(b => b.TRANGTHAI == "DangBan").Select(b => b.PHANLOAI).Distinct().ToList();

            return View(list);
        }

        #endregion
    }
}
