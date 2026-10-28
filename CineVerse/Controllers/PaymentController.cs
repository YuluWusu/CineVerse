using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Helpers;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using DoAn_QuanLyVeXemPhimTrucTiep.Services;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class PaymentController : Controller
    {
        private readonly BookingService _bookingService;
        private readonly QL_VEPHIMEntities _db;

        public PaymentController()
        {
            _bookingService = new BookingService();
            _db = new QL_VEPHIMEntities();
        }

        #region 1. VNPay Sandbox Payment Gateway

        /// <summary>
        /// Khởi tạo URL chuyển hướng sang cổng thanh toán VNPay Sandbox
        /// </summary>
        [HttpGet]
        public ActionResult ThanhToanVNPay(string maDonHang)
        {
            if (string.IsNullOrEmpty(maDonHang))
            {
                return RedirectToAction("Index", "Home");
            }

            var donHang = _bookingService.GetOrderById(maDonHang.Trim());
            if (donHang == null)
            {
                return HttpNotFound("Không tìm thấy đơn hàng cần thanh toán.");
            }

            // Nếu đơn hàng đã hoàn tất thanh toán trước đó
            if (donHang.TRANGTHAITHANHTOAN == "DaThanhToan")
            {
                return RedirectToAction("ETicket", new { maDonHang = donHang.MA_DONHANG.Trim() });
            }

            // Đọc cấu hình từ Web.config
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"] ?? "2QXUI4B4";
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"] ?? "AXYZVJRAUQVIAVHAZXZNJMYZGOPRAGQO";
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"];

            if (string.IsNullOrEmpty(vnp_Returnurl))
            {
                vnp_Returnurl = Url.Action("PaymentCallback", "Payment", null, Request.Url.Scheme);
            }

            // Khởi tạo thư viện VNPay và gắn các tham số
            var vnpay = new VnPayLibrary();
            long amountInVnd = (long)Math.Round(donHang.TONGTIEN);

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (amountInVnd * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", VnPayLibrary.GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan ve xem phim CineVerse - Don hang " + donHang.MA_DONHANG.Trim());
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", donHang.MA_DONHANG.Trim());

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(paymentUrl);
        }

        /// <summary>
        /// Nhận Callback GET trả về từ VNPay sau khi khách thao tác
        /// </summary>
        [HttpGet]
        public ActionResult PaymentCallback()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"] ?? "AXYZVJRAUQVIAVHAZXZNJMYZGOPRAGQO";
                var vnpay = new VnPayLibrary();

                foreach (string key in Request.QueryString.AllKeys)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(key, Request.QueryString[key]);
                    }
                }

                string vnp_TxnRef = Request.QueryString["vnp_TxnRef"];
                string vnp_TransactionNo = Request.QueryString["vnp_TransactionNo"];
                string vnp_ResponseCode = Request.QueryString["vnp_ResponseCode"];
                string vnp_TransactionStatus = Request.QueryString["vnp_TransactionStatus"];
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

                if (checkSignature)
                {
                    string maDonHang = vnp_TxnRef?.Trim();
                    var donHang = _bookingService.GetOrderById(maDonHang);

                    if (donHang != null)
                    {
                        if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                        {
                            // Thanh toán thành công!
                            _bookingService.UpdatePaymentStatus(donHang.MA_DONHANG, "DaThanhToan", vnp_TransactionNo, "VNPay");

                            // Giải phóng ghế khỏi SeatHoldService vì đã chuyển thành vé chính thức
                            SeatHoldService.Instance.ReleaseSeats(donHang.MA_LICHCHIEU, donHang.MA_KHACHHANG);

                            // Gửi email vé điện tử nền bất đồng bộ
                            EmailService.SendTicketConfirmationBackground(donHang);

                            TempData["ThongBao"] = "Thanh toán qua VNPay thành công! Mã giao dịch: " + vnp_TransactionNo;
                            return RedirectToAction("ETicket", new { maDonHang = donHang.MA_DONHANG.Trim() });
                        }
                        else
                        {
                            // Thanh toán thất bại hoặc khách chủ động hủy trên cổng VNPay
                            _bookingService.UpdatePaymentStatus(donHang.MA_DONHANG, "DaHuy", vnp_TransactionNo, "VNPay");

                            // Giải phóng ghế giữ tạm ngay lập tức
                            SeatHoldService.Instance.ReleaseSeats(donHang.MA_LICHCHIEU, donHang.MA_KHACHHANG);

                            string errorDesc = GetVnPayErrorMessage(vnp_ResponseCode);
                            ViewBag.OrderId = donHang.MA_DONHANG.Trim();
                            ViewBag.ErrorCode = vnp_ResponseCode;
                            ViewBag.ErrorMessage = errorDesc;
                            ViewBag.TransactionNo = vnp_TransactionNo;
                            return View("PaymentFailed");
                        }
                    }
                }
            }

            ViewBag.ErrorMessage = "Chữ ký bảo mật không hợp lệ hoặc dữ liệu phản hồi bị can thiệp.";
            return View("PaymentFailed");
        }

        #endregion

        #region 2. VietQR Payment

        /// <summary>
        /// Trang hiển thị mã VietQR động để khách chuyển khoản
        /// </summary>
        [HttpGet]
        public ActionResult ThanhToanVietQR(string maDonHang)
        {
            if (string.IsNullOrEmpty(maDonHang))
            {
                return RedirectToAction("Index", "Home");
            }

            var donHang = _bookingService.GetOrderById(maDonHang.Trim());
            if (donHang == null)
            {
                return HttpNotFound("Không tìm thấy đơn hàng cần thanh toán.");
            }

            if (donHang.TRANGTHAITHANHTOAN == "DaThanhToan")
            {
                return RedirectToAction("ETicket", new { maDonHang = donHang.MA_DONHANG.Trim() });
            }

            string bankId = ConfigurationManager.AppSettings["VietQR:BankId"] ?? "MB";
            string accountNo = ConfigurationManager.AppSettings["VietQR:AccountNo"] ?? "0388888888";
            string accountName = ConfigurationManager.AppSettings["VietQR:AccountName"] ?? "CONG TY CO PHAN RAP PHIM CINEVERSE";
            string template = ConfigurationManager.AppSettings["VietQR:Template"] ?? "compact2";

            long amount = (long)Math.Round(donHang.TONGTIEN);
            string transferContent = "CINEVERSE " + donHang.MA_DONHANG.Trim();

            // Link sinh mã QR động chuẩn VietQR
            string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={amount}&addInfo={Uri.EscapeDataString(transferContent)}&accountName={Uri.EscapeDataString(accountName)}";

            // Lấy số giây giữ ghế còn lại
            int remainingSeconds = SeatHoldService.Instance.GetRemainingSeconds(donHang.MA_LICHCHIEU, donHang.MA_KHACHHANG);
            if (remainingSeconds <= 0) remainingSeconds = 300; // Mặc định 5 phút dự phòng

            ViewBag.QrUrl = qrUrl;
            ViewBag.BankId = bankId;
            ViewBag.AccountNo = accountNo;
            ViewBag.AccountName = accountName;
            ViewBag.TransferContent = transferContent;
            ViewBag.Amount = amount;
            ViewBag.RemainingSeconds = remainingSeconds;

            return View(donHang);
        }

        /// <summary>
        /// AJAX Endpoint xác nhận chuyển khoản VietQR (phục vụ demo kiểm thử hoặc Webhook ngân hàng)
        /// </summary>
        [HttpPost]
        public ActionResult XacNhanVietQR(string maDonHang)
        {
            if (string.IsNullOrEmpty(maDonHang))
            {
                return Json(new { success = false, message = "Mã đơn hàng không hợp lệ." });
            }

            var donHang = _bookingService.GetOrderById(maDonHang.Trim());
            if (donHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            string vqrTxn = "VQR-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            bool updated = _bookingService.UpdatePaymentStatus(donHang.MA_DONHANG, "DaThanhToan", vqrTxn, "VietQR");

            if (updated)
            {
                // Giải phóng ghế khỏi SeatHoldService
                SeatHoldService.Instance.ReleaseSeats(donHang.MA_LICHCHIEU, donHang.MA_KHACHHANG);

                // Gửi email vé điện tử nền
                EmailService.SendTicketConfirmationBackground(donHang);

                return Json(new
                {
                    success = true,
                    message = "Xác nhận chuyển khoản thành công!",
                    redirectUrl = Url.Action("ETicket", "Payment", new { maDonHang = donHang.MA_DONHANG.Trim() })
                });
            }

            return Json(new { success = false, message = "Không thể cập nhật trạng thái đơn hàng." });
        }

        #endregion

        #region 3. Hủy Thanh toán & Giải phóng ghế

        /// <summary>
        /// Khách hàng chủ động hủy giao dịch khi đang ở trang thanh toán
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyThanhToan(string maDonHang)
        {
            if (!string.IsNullOrEmpty(maDonHang))
            {
                var donHang = _bookingService.GetOrderById(maDonHang.Trim());
                if (donHang != null && donHang.TRANGTHAITHANHTOAN != "DaThanhToan")
                {
                    _bookingService.UpdatePaymentStatus(donHang.MA_DONHANG, "DaHuy", null, donHang.PHUONGTHUCTHANHTOAN);
                    SeatHoldService.Instance.ReleaseSeats(donHang.MA_LICHCHIEU, donHang.MA_KHACHHANG);
                }
            }

            TempData["ThongBao"] = "Giao dịch đặt vé đã được hủy.";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region 4. Vé Điện Tử (E-Ticket) & Tra Cứu

        /// <summary>
        /// Hiển thị cuống vé điện tử (E-Ticket) phong cách điện ảnh cao cấp kèm mã QR Check-in
        /// </summary>
        [HttpGet]
        public ActionResult ETicket(string maDonHang)
        {
            if (string.IsNullOrEmpty(maDonHang))
            {
                return RedirectToAction("Index", "Home");
            }

            var donHang = _bookingService.GetOrderById(maDonHang.Trim());
            if (donHang == null)
            {
                return HttpNotFound("Không tìm thấy thông tin vé điện tử.");
            }

            return View(donHang);
        }

        /// <summary>
        /// Tra cứu và kiểm tra thông tin vé qua mã Check-in QR Code (dành cho nhân viên soát vé hoặc khách kiểm tra)
        /// </summary>
        [HttpGet]
        public ActionResult KiemTraCheckIn(string maCheckIn)
        {
            if (string.IsNullOrEmpty(maCheckIn))
            {
                return Json(new { success = false, message = "Vui lòng nhập hoặc quét mã Check-in." }, JsonRequestBehavior.AllowGet);
            }

            string trimmed = maCheckIn.Trim();
            var donHang = _db.DONHANGs.FirstOrDefault(d => d.MACHECKIN.Trim() == trimmed);

            if (donHang == null)
            {
                return Json(new { success = false, message = "Mã Check-in không tồn tại trên hệ thống!" }, JsonRequestBehavior.AllowGet);
            }

            bool daSoatVe = string.Equals(donHang.TRANGTHAISOATVE?.Trim(), "DaSoatVe", StringComparison.OrdinalIgnoreCase);

            var result = new
            {
                success = true,
                maDonHang = donHang.MA_DONHANG.Trim(),
                maCheckIn = donHang.MACHECKIN.Trim(),
                tenKhachHang = donHang.KHACHHANG?.HOTEN ?? "Khách vãng lai",
                sdt = donHang.KHACHHANG?.SDT,
                tenPhim = donHang.LICHCHIEU?.PHIM?.TEN_PHIM,
                tenRap = donHang.LICHCHIEU?.PHONGCHIEU?.RAPCHIEU?.TEN_RAP,
                tenPhong = donHang.LICHCHIEU?.PHONGCHIEU?.TEN_PHONG,
                ngayChieu = donHang.LICHCHIEU?.NGAYCHIEU.ToString("dd/MM/yyyy"),
                gioChieu = donHang.LICHCHIEU?.KHUNGGIO?.GIOBATDAU.ToString(@"hh\:mm"),
                danhSachGhe = string.Join(", ", donHang.VEs.Select(v => v.GHE?.SOGHE?.Trim()).Where(s => !string.IsNullOrEmpty(s))),
                tongTien = donHang.TONGTIEN.ToString("#,##0") + " đ",
                trangThaiThanhToan = donHang.TRANGTHAITHANHTOAN,
                trangThaiSoatVe = daSoatVe ? "Đã soát vé" : "Chưa soát vé",
                thoiGianCheckIn = donHang.THOIGIANCHECKIN.HasValue ? donHang.THOIGIANCHECKIN.Value.ToString("dd/MM/yyyy HH:mm:ss") : null,
                daSoatVe = daSoatVe
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Nhân viên xác nhận soát vé thành công bằng mã Check-in
        /// </summary>
        [HttpPost]
        public ActionResult XacNhanSoatVe(string maCheckIn)
        {
            if (string.IsNullOrEmpty(maCheckIn))
            {
                return Json(new { success = false, message = "Mã Check-in không hợp lệ." });
            }

            string trimmed = maCheckIn.Trim();
            var donHang = _db.DONHANGs.FirstOrDefault(d => d.MACHECKIN.Trim() == trimmed);

            if (donHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin đơn vé!" });
            }

            if (string.Equals(donHang.TRANGTHAISOATVE?.Trim(), "DaSoatVe", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = $"Vé này ĐÃ ĐƯỢC SOÁT vào lúc {donHang.THOIGIANCHECKIN:dd/MM/yyyy HH:mm:ss}! Vui lòng kiểm tra lại để tránh dùng vé 2 lần."
                });
            }

            donHang.TRANGTHAISOATVE = "DaSoatVe";
            donHang.THOIGIANCHECKIN = DateTime.Now;

            foreach (var ve in donHang.VEs)
            {
                ve.TRANGTHAI = "Đã soát vé";
            }

            _db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Soát vé thành công! Mời quý khách vào phòng chiếu.",
                thoiGianCheckIn = donHang.THOIGIANCHECKIN.Value.ToString("dd/MM/yyyy HH:mm:ss")
            });
        }

        #endregion

        #region Helper: Mã lỗi VNPay

        private string GetVnPayErrorMessage(string responseCode)
        {
            switch (responseCode)
            {
                case "07":
                    return "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).";
                case "09":
                    return "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking tại ngân hàng.";
                case "10":
                    return "Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.";
                case "11":
                    return "Đã hết hạn chờ thanh toán. Vui lòng thực hiện lại giao dịch.";
                case "12":
                    return "Thẻ/Tài khoản bị khóa.";
                case "13":
                    return "Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).";
                case "24":
                    return "Khách hàng đã hủy giao dịch trên cổng thanh toán.";
                case "51":
                    return "Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.";
                case "65":
                    return "Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.";
                case "75":
                    return "Ngân hàng thanh toán đang bảo trì.";
                case "79":
                    return "Quý khách nhập sai mật khẩu thanh toán quá số lần quy định.";
                case "99":
                    return "Lỗi không xác định từ cổng thanh toán VNPay.";
                default:
                    return "Giao dịch không thành công (Mã lỗi: " + responseCode + ").";
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

