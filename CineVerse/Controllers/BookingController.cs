using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using DoAn_QuanLyVeXemPhimTrucTiep.Services;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class BookingController : Controller
    {
        private readonly BookingService bookingService = new BookingService();
        private readonly ScheduleService scheduleService = new ScheduleService();
        private readonly SeatService seatService = new SeatService();
        private readonly StaffService staffService = new StaffService();
        private readonly FnBService _fnBService = new FnBService();
        private readonly PricingService _pricingService = new PricingService();

        /// <summary>
        /// Lấy định danh duy nhất của phiên khách đặt vé (Mã khách hàng nếu đã đăng nhập hoặc VisitorToken trong Session).
        /// </summary>
        private string GetHolderId()
        {
            if (Session["KhachHang"] != null)
            {
                var kh = Session["KhachHang"] as KHACHHANG;
                if (kh != null && !string.IsNullOrWhiteSpace(kh.MA_KHACHHANG))
                {
                    return kh.MA_KHACHHANG.Trim();
                }
            }

            if (Session["VisitorToken"] == null)
            {
                Session["VisitorToken"] = "GUEST_" + Guid.NewGuid().ToString("N");
            }
            return Session["VisitorToken"].ToString();
        }

        #region BƯỚC 1: Chọn Ghế & Real-time Seat Hold API

        // GET: /Booking/ChonGhe?maLichChieu=...
        public ActionResult ChonGhe(string maLichChieu)
        {
            if (string.IsNullOrEmpty(maLichChieu))
            {
                return RedirectToAction("DanhSachPhim", "Movies");
            }

            var lichChieu = scheduleService.GetScheduleById(maLichChieu);
            if (lichChieu == null)
            {
                return HttpNotFound();
            }

            string holderId = GetHolderId();
            var danhSachGhe = seatService.GetSeatsByRoom(lichChieu.MA_PHONG);
            var gheDaDat = seatService.GetBookedSeatIds(maLichChieu);

            // Lấy trạng thái ghế đang giữ từ SeatHoldService
            SeatHoldService.Instance.GetSeatHoldStatus(
                maLichChieu,
                holderId,
                out var otherHeldSeats,
                out var myHeldSeats,
                out int remainingSeconds);

            ViewBag.GheDaDat = gheDaDat;
            ViewBag.OtherHeldSeats = otherHeldSeats;
            ViewBag.MyHeldSeats = myHeldSeats;
            ViewBag.RemainingSeconds = remainingSeconds;
            ViewBag.LichChieu = lichChieu;

            return View(danhSachGhe);
        }

        /// <summary>
        /// API siêu nhẹ Polling thời gian thực trạng thái ghế (Đã bán, Người khác giữ, Bạn đang giữ).
        /// </summary>
        [HttpGet]
        public ActionResult GetSeatStatus(string maLichChieu)
        {
            if (string.IsNullOrWhiteSpace(maLichChieu))
            {
                return Json(new { success = false, message = "Thiếu mã lịch chiếu" }, JsonRequestBehavior.AllowGet);
            }

            string holderId = GetHolderId();
            var bookedSeatIds = seatService.GetBookedSeatIds(maLichChieu);

            SeatHoldService.Instance.GetSeatHoldStatus(
                maLichChieu,
                holderId,
                out var otherHeldSeats,
                out var myHeldSeats,
                out int remainingSeconds);

            return Json(new
            {
                success = true,
                soldSeats = bookedSeatIds,
                otherHeldSeats = otherHeldSeats,
                myHeldSeats = myHeldSeats,
                remainingSeconds = remainingSeconds
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// API Khóa nguyên tử (Atomic Check-and-Lock) giữ ghế tạm thời trong 7 phút.
        /// </summary>
        [HttpPost]
        public ActionResult HoldSeats(string maLichChieu, string selectedSeats)
        {
            if (string.IsNullOrWhiteSpace(maLichChieu) || string.IsNullOrWhiteSpace(selectedSeats))
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một ghế." });
            }

            var lichChieu = scheduleService.GetScheduleById(maLichChieu);
            if (lichChieu == null)
            {
                return Json(new { success = false, message = "Suất chiếu không tồn tại." });
            }

            string holderId = GetHolderId();
            var seatIds = selectedSeats.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim().ToUpper())
                                      .Distinct()
                                      .ToList();

            // 1. Kiểm tra ghế đã bán trong CSDL chưa
            var bookedSeats = seatService.GetBookedSeatIds(maLichChieu);
            var soldConflicts = seatIds.Where(s => bookedSeats.Contains(s)).ToList();
            if (soldConflicts.Any())
            {
                return Json(new
                {
                    success = false,
                    message = $"Rất tiếc! Ghế [{string.Join(", ", soldConflicts)}] đã có người đặt mua thành công. Vui lòng chọn ghế khác."
                });
            }

            // 2. Kiểm tra quy tắc rạp: Ghế đôi & Ghế trống đơn mồ côi (Single-seat gap rule)
            var allSeatsInRoom = seatService.GetSeatsByRoom(lichChieu.MA_PHONG);
            if (!SeatHoldService.ValidateSeatRules(allSeatsInRoom, seatIds, bookedSeats, out string ruleWarning))
            {
                return Json(new
                {
                    success = false,
                    ruleViolation = true,
                    message = ruleWarning
                });
            }

            // 3. Thực hiện khóa nguyên tử trong SeatHoldService
            if (!SeatHoldService.Instance.HoldSeats(maLichChieu, seatIds, holderId, out string holdError, out int remainingSeconds))
            {
                return Json(new
                {
                    success = false,
                    message = holdError
                });
            }

            // Lưu phiên giữ ghế
            Session["HoldMaLichChieu"] = maLichChieu.Trim().ToUpper();
            Session["HoldSeats"] = seatIds;

            return Json(new
            {
                success = true,
                remainingSeconds = remainingSeconds,
                message = "Đã giữ ghế thành công!"
            });
        }

        /// <summary>
        /// API nhả ghế chủ động khi khách hủy hoặc thoát khỏi quy trình đặt vé.
        /// </summary>
        [HttpPost]
        public ActionResult ReleaseSeats(string maLichChieu)
        {
            string holderId = GetHolderId();
            if (!string.IsNullOrWhiteSpace(maLichChieu))
            {
                SeatHoldService.Instance.ReleaseSeats(maLichChieu, holderId);
            }

            Session.Remove("HoldMaLichChieu");
            Session.Remove("HoldSeats");
            Session.Remove("KhachHangChonMon");

            return Json(new { success = true, message = "Đã giải phóng ghế." });
        }

        /// <summary>
        /// API kiểm tra thời gian giữ ghế còn lại cho Countdown Timer.
        /// </summary>
        [HttpGet]
        public ActionResult GetRemainingTime(string maLichChieu)
        {
            string holderId = GetHolderId();
            int sec = SeatHoldService.Instance.GetRemainingSeconds(maLichChieu, holderId);
            return Json(new
            {
                remainingSeconds = sec,
                isExpired = (sec <= 0)
            }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region BƯỚC 2: Chọn Bắp nước & Combo F&B

        // GET: /Booking/ChonMonAn?maLichChieu=...&selectedSeats=...
        [HttpGet]
        public ActionResult ChonMonAn(string maLichChieu, string selectedSeats)
        {
            if (string.IsNullOrEmpty(maLichChieu) || string.IsNullOrEmpty(selectedSeats))
            {
                TempData["Loi"] = "Vui lòng chọn suất chiếu và ghế trước khi chọn bắp nước.";
                return RedirectToAction("DanhSachPhim", "Movies");
            }

            string holderId = GetHolderId();
            int remainingSeconds = SeatHoldService.Instance.GetRemainingSeconds(maLichChieu, holderId);

            // Nếu đã hết hạn giữ ghế hoặc chưa giữ, điều hướng về lại chọn ghế
            if (remainingSeconds <= 0)
            {
                TempData["Loi"] = "Thời gian giữ ghế đã hết hạn hoặc phiên đặt vé không hợp lệ. Vui lòng chọn lại ghế.";
                return RedirectToAction("ChonGhe", new { maLichChieu = maLichChieu });
            }

            var lichChieu = scheduleService.GetScheduleById(maLichChieu);
            if (lichChieu == null) return HttpNotFound();

            string[] listMaGhe = selectedSeats.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var gheDuocChon = seatService.GetSeatsByIds(listMaGhe.ToList());

            decimal phuTroi = lichChieu.GIATVE_PHUTROI ?? 0m;
            decimal tongTienVe = gheDuocChon.Sum(g => (g.LOAIGHE?.GIATVE ?? 0m) + phuTroi);

            // Lấy danh sách sản phẩm bắp nước đang mở bán
            var danhSachBapNuoc = _fnBService.GetAll(onlyActive: true);

            // Đọc giỏ hàng bắp nước tạm thời từ Session nếu khách đã chọn trước đó
            var selectedFnB = Session["KhachHangChonMon"] as Dictionary<string, int> ?? new Dictionary<string, int>();

            ViewBag.LichChieu = lichChieu;
            ViewBag.SelectedSeats = selectedSeats;
            ViewBag.DanhSachGhe = gheDuocChon;
            ViewBag.TongTienVe = tongTienVe;
            ViewBag.DanhSachBapNuoc = danhSachBapNuoc;
            ViewBag.SelectedFnB = selectedFnB;
            ViewBag.RemainingSeconds = remainingSeconds;

            return View();
        }

        // POST: /Booking/XacNhanChonMon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhanChonMon(string maLichChieu, string selectedSeats)
        {
            if (string.IsNullOrEmpty(maLichChieu) || string.IsNullOrEmpty(selectedSeats))
            {
                return RedirectToAction("DanhSachPhim", "Movies");
            }

            string holderId = GetHolderId();
            int remainingSeconds = SeatHoldService.Instance.GetRemainingSeconds(maLichChieu, holderId);
            if (remainingSeconds <= 0)
            {
                TempData["Loi"] = "Đã hết thời gian giữ ghế. Vui lòng chọn lại ghế.";
                return RedirectToAction("ChonGhe", new { maLichChieu = maLichChieu });
            }

            var fnbDict = new Dictionary<string, int>();
            foreach (string key in Request.Form.AllKeys)
            {
                if (key != null && (key.StartsWith("fnb_") || key.StartsWith("mon_")))
                {
                    string maSp = key.Substring(4).Trim();
                    if (int.TryParse(Request.Form[key], out int soLuong) && soLuong > 0)
                    {
                        fnbDict[maSp] = soLuong;
                    }
                }
            }

            // Lưu danh sách món ăn vào Session
            Session["KhachHangChonMon"] = fnbDict;

            return RedirectToAction("ThanhToan", new { maLichChieu = maLichChieu, selectedSeats = selectedSeats });
        }

        #endregion

        #region BƯỚC 3: Thanh toán & Xác nhận đơn hàng

        // GET: /Booking/ThanhToan?maLichChieu=...&selectedSeats=...
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult ThanhToan(string maLichChieu, string selectedSeats)
        {
            if (string.IsNullOrEmpty(selectedSeats) || string.IsNullOrEmpty(maLichChieu))
            {
                return RedirectToAction("ChonGhe", new { maLichChieu = maLichChieu });
            }

            string holderId = GetHolderId();
            int remainingSeconds = SeatHoldService.Instance.GetRemainingSeconds(maLichChieu, holderId);
            if (remainingSeconds <= 0)
            {
                TempData["Loi"] = "Đã hết thời gian giữ ghế (7 phút). Ghế đã được nhả cho khách khác, vui lòng chọn lại.";
                return RedirectToAction("ChonGhe", new { maLichChieu = maLichChieu });
            }

            var lichChieu = scheduleService.GetScheduleById(maLichChieu);
            if (lichChieu == null) return HttpNotFound();

            string[] listMaGhe = selectedSeats.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var gheDuocChon = seatService.GetSeatsByIds(listMaGhe.ToList());

            // Lấy danh sách bắp nước khách đã chọn từ Bước 2
            var fnbDict = Session["KhachHangChonMon"] as Dictionary<string, int> ?? new Dictionary<string, int>();
            var allProducts = _fnBService.GetAll();
            var fnbLookup = allProducts.ToDictionary(p => p.MA_SANPHAM.Trim(), p => p);

            var selectedFnBList = new List<FnBPriceItem>();
            decimal tongTienBapNuoc = 0m;

            foreach (var item in fnbDict)
            {
                string code = item.Key?.Trim() ?? "";
                if (item.Value > 0 && fnbLookup.TryGetValue(code, out BAPNUOC product))
                {
                    decimal subTotal = product.GIABAN * item.Value;
                    tongTienBapNuoc += subTotal;
                    selectedFnBList.Add(new FnBPriceItem
                    {
                        MaSanPham = product.MA_SANPHAM.Trim(),
                        TenSanPham = product.TEN_SANPHAM.Trim(),
                        PhanLoai = product.PHANLOAI?.Trim(),
                        SoLuong = item.Value,
                        DonGia = product.GIABAN,
                        ThanhTien = subTotal
                    });
                }
            }

            decimal phuTroi = lichChieu.GIATVE_PHUTROI ?? 0m;
            decimal tongTienVe = gheDuocChon.Sum(g => (g.LOAIGHE?.GIATVE ?? 0m) + phuTroi);

            KHACHHANG khachHang = null;
            if (Session["KhachHang"] != null)
            {
                var sessionKh = Session["KhachHang"] as KHACHHANG;
                khachHang = bookingService.GetCustomerById(sessionKh.MA_KHACHHANG);
            }

            decimal chietKhauThanhVienRate = 0m;
            decimal giamGiaThanhVien = 0m;
            if (khachHang != null)
            {
                chietKhauThanhVienRate = LoyaltyService.Instance.GetDiscountRate(khachHang.HANGTHANHVIEN);
                giamGiaThanhVien = Math.Round(tongTienVe * chietKhauThanhVienRate);
            }

            ViewBag.KhachHang = khachHang;
            ViewBag.ChietKhauThanhVienRate = chietKhauThanhVienRate;
            ViewBag.GiamGiaThanhVien = giamGiaThanhVien;

            ViewBag.LichChieu = lichChieu;
            ViewBag.SelectedSeats = selectedSeats;
            ViewBag.SelectedSeatsList = listMaGhe;
            ViewBag.TongTienVe = tongTienVe;
            ViewBag.SelectedFnBList = selectedFnBList;
            ViewBag.TongTienBapNuoc = tongTienBapNuoc;
            ViewBag.TongTruocGiam = tongTienVe + tongTienBapNuoc;
            ViewBag.DanhSachKM = bookingService.GetActivePromotions();
            ViewBag.RemainingSeconds = remainingSeconds;

            return View(gheDuocChon);
        }

        // POST: /Booking/XuLyThanhToan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XuLyThanhToan(string maLichChieu, string selectedSeats,
            string hoTen, string soDienThoai, string Email,
            string maCodeKhuyenMai, string maKmId,
            string paymentMethod = "VNPay")
        {
            var lichChieu = scheduleService.GetScheduleById(maLichChieu);
            if (lichChieu == null) return HttpNotFound();

            string holderId = GetHolderId();
            int remainingSeconds = SeatHoldService.Instance.GetRemainingSeconds(maLichChieu, holderId);
            if (remainingSeconds <= 0)
            {
                TempData["Loi"] = "Đã hết thời gian giữ ghế. Giao dịch thanh toán không thể tiếp tục, vui lòng chọn lại ghế.";
                return RedirectToAction("ChonGhe", new { maLichChieu = maLichChieu });
            }

            KHACHHANG khachHang = null;
            if (Session["KhachHang"] != null)
            {
                var sessionKh = Session["KhachHang"] as KHACHHANG;
                khachHang = bookingService.GetCustomerById(sessionKh.MA_KHACHHANG);
            }

            if (khachHang == null)
            {
                khachHang = bookingService.GetOrCreateCustomer(hoTen, soDienThoai, Email);
            }

            string[] listMaGhe = selectedSeats.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var gheList = seatService.GetSeatsByIds(listMaGhe.ToList());

            // Xử lý khuyến mãi
            KHUYENMAI khuyenMai = bookingService.ValidatePromotion(maKmId);

            // Lấy danh sách bắp nước từ Session hoặc Form
            var fnbDict = Session["KhachHangChonMon"] as Dictionary<string, int> ?? new Dictionary<string, int>();
            foreach (string key in Request.Form.AllKeys)
            {
                if (key != null && (key.StartsWith("fnb_") || key.StartsWith("mon_")))
                {
                    string maSp = key.Substring(4).Trim();
                    if (int.TryParse(Request.Form[key], out int soLuong) && soLuong > 0)
                    {
                        fnbDict[maSp] = soLuong;
                    }
                }
            }

            // Tính tiền vé và giảm giá
            var prices = bookingService.CalculatePrice(lichChieu, gheList, khuyenMai);
            decimal tongTienVe = prices.Item1;
            decimal soTienGiamKM = prices.Item2;

            // Giảm giá theo hạng thành viên (Loyalty: VIP 5%, VVIP 10%)
            decimal chietKhauThanhVienRate = 0m;
            decimal giamGiaThanhVien = 0m;
            if (khachHang != null)
            {
                chietKhauThanhVienRate = LoyaltyService.Instance.GetDiscountRate(khachHang.HANGTHANHVIEN);
                giamGiaThanhVien = Math.Round(tongTienVe * chietKhauThanhVienRate);
            }

            decimal tongSoTienGiam = soTienGiamKM + giamGiaThanhVien;

            // Tính tiền bắp nước
            var allProducts = _fnBService.GetAll();
            var fnbLookup = allProducts.ToDictionary(p => p.MA_SANPHAM.Trim(), p => p);
            decimal tienBapNuoc = 0m;
            foreach (var item in fnbDict)
            {
                if (item.Value > 0 && fnbLookup.TryGetValue(item.Key.Trim(), out BAPNUOC product))
                {
                    tienBapNuoc += product.GIABAN * item.Value;
                }
            }

            decimal tongTienSauGiam = (tongTienVe + tienBapNuoc) - tongSoTienGiam;
            if (tongTienSauGiam < 0) tongTienSauGiam = 0;

            DONHANG donHang = null;
            try
            {
                // Gọi ProcessBooking với Transaction Serializable chống hoàn toàn đặt trùng
                donHang = bookingService.ProcessBooking(khachHang, lichChieu, gheList, khuyenMai, tongSoTienGiam, fnbDict, holderId, paymentMethod);

                Session.Remove("KhachHangChonMon");
                Session.Remove("HoldMaLichChieu");
                Session.Remove("HoldSeats");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ChonGhe", new { maLichChieu = maLichChieu });
            }

            TempData["TongTienGoc"] = tongTienVe + tienBapNuoc;
            TempData["TienBapNuoc"] = tienBapNuoc;
            TempData["SoTienGiam"] = tongSoTienGiam;
            TempData["GiamGiaThanhVien"] = giamGiaThanhVien;
            TempData["TongTienSauGiam"] = tongTienSauGiam;
            TempData["TenKhuyenMai"] = khuyenMai?.TEN_KM;

            // Điều hướng theo phương thức thanh toán
            if (string.Equals(paymentMethod, "VNPay", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("ThanhToanVNPay", "Payment", new { maDonHang = donHang.MA_DONHANG.Trim() });
            }
            else if (string.Equals(paymentMethod, "VietQR", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("ThanhToanVietQR", "Payment", new { maDonHang = donHang.MA_DONHANG.Trim() });
            }
            else
            {
                // Thanh toán tại quầy
                EmailService.SendTicketConfirmationBackground(donHang);
                TempData["ThongBao"] = "Đặt vé giữ chỗ thành công! Vui lòng thanh toán và nhận vé tại quầy trước giờ chiếu 15 phút.";
                return RedirectToAction("ETicket", "Payment", new { maDonHang = donHang.MA_DONHANG.Trim() });
            }
        }

        public ActionResult DatVeThanhCong()
        {
            return View();
        }

        #endregion

        #region Lịch sử đặt vé

        [HttpGet]
        [DoAn_QuanLyVeXemPhimTrucTiep.Filters.CustomerAuthorize]
        public ActionResult LichSuVe()
        {
            if (Session["KhachHang"] == null)
            {
                return RedirectToAction("DangNhap", "Account");
            }

            var sessionKh = Session["KhachHang"] as KHACHHANG;
            var veList = bookingService.GetBookingHistory(sessionKh.MA_KHACHHANG);

            return View(veList);
        }

        #endregion
    }
}
