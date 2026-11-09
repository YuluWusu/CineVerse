using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Filters;
using DoAn_QuanLyVeXemPhimTrucTiep.Helpers;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using DoAn_QuanLyVeXemPhimTrucTiep.Services;
using System.Threading.Tasks;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class AccountController : Controller
    {
        private readonly QL_VEPHIMEntities data = new QL_VEPHIMEntities();
        private readonly AuthService authService = new AuthService();
        private readonly PasswordResetService passwordResetService = new PasswordResetService();

        // GET: Account/DangKy
        [HttpGet]
        public ActionResult DangKy()
        {
            if (Session["KhachHang"] != null)
            {
                return RedirectToAction("TrangChu", "Home");
            }
            return View();
        }

        // POST: Account/DangKy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(string HOTEN, string SDT, string EMAIL, string MATKHAU, string cancuoc, string XacNhanMatKhau)
        {
            if (string.IsNullOrWhiteSpace(HOTEN) || string.IsNullOrWhiteSpace(SDT) || string.IsNullOrWhiteSpace(MATKHAU))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ các trường thông tin bắt buộc (*).";
                return View();
            }

            if (MATKHAU != XacNhanMatKhau)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            if (MATKHAU.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải chứa ít nhất 6 ký tự để đảm bảo an toàn.";
                return View();
            }

            var result = authService.RegisterCustomer(HOTEN, SDT, cancuoc, EMAIL, MATKHAU);
            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
                return View();
            }

            TempData["ToastMessage"] = "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay.";
            TempData["ToastType"] = "success";
            ViewBag.Success = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
            return RedirectToAction("DangNhap");
        }

        // GET: Account/DangNhap
        [HttpGet]
        public ActionResult DangNhap(string returnUrl)
        {
            if (Session["KhachHang"] != null || Session["NhanVienMa"] != null)
            {
                return RedirectToAction("TrangChu", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/DangNhap
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(string EMAIL, string MATKHAU, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(EMAIL) ||
                string.IsNullOrWhiteSpace(MATKHAU))
            {
                ViewBag.Error = "Vui lòng nhập email và mật khẩu.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var result = authService.Authenticate(EMAIL, MATKHAU);

            if (!result.Success)
            {
                ViewBag.Error = result.ErrorMessage;
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            // Làm sạch session cũ để phòng chống Session Fixation
            Session.Clear();

            if (result.IsStaff && !result.IsAdmin && result.Staff != null)
            {
                Session["VaiTro"] = "NHANVIEN";
                Session["NhanVienMa"] = result.Staff.MA_NHANVIEN.Trim();
                Session["NhanVienTen"] = result.User.HOTEN;
                Session["NhanVienRap"] = result.Staff.MA_RAP == null
                    ? null
                    : result.Staff.MA_RAP.Trim();

                TempData["ToastMessage"] = "Đăng nhập thành công với quyền Nhân viên rạp.";
                TempData["ToastType"] = "success";

                return RedirectToAction("Index", "NhanVien");
            }

            var kh = result.Customer;

            Session["KhachHang"] = kh;
            Session["VaiTro"] = result.IsAdmin ? "ADMIN" : "KHACHHANG";
            Session["HoTen"] = result.User.HOTEN;
            Session["MaTaiKhoan"] = result.User.MA_TAIKHOAN.Trim();

            TempData["ToastMessage"] =
                "Đăng nhập thành công! Chào mừng " +
                result.User.HOTEN + " đến với CinéVerse.";

            TempData["ToastType"] = "success";

            // Kiểm tra returnUrl chống Open Redirect
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (result.IsAdmin)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("TrangChu", "Home");
        }

        // GET: Account/HoSo
        [HttpGet]
        [CustomerAuthorize]
        public ActionResult HoSo()
        {
            var sessionKh = Session["KhachHang"] as KHACHHANG;
            if (sessionKh == null)
            {
                return RedirectToAction("DangNhap", "Account");
            }

            string maKh = sessionKh.MA_KHACHHANG.Trim();

            var kh = data.KHACHHANGs
                .Include(k => k.TAIKHOAN)
                .Include(k => k.TAIKHOAN.VAITROes)
                .FirstOrDefault(k => k.MA_KHACHHANG.Trim() == maKh);

            if (kh == null)
            {
                Session.Clear();
                return RedirectToAction("DangNhap", "Account");
            }

            Session["KhachHang"] = kh;
            ViewBag.LoyaltyInfo = LoyaltyService.Instance.GetProgressInfo(kh);
            return View(kh);
        }

        // POST: Account/DoiMatKhau
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomerAuthorize]
        public ActionResult DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhanMatKhau)
        {
            if (string.IsNullOrWhiteSpace(matKhauCu) || string.IsNullOrWhiteSpace(matKhauMoi))
            {
                TempData["ToastMessage"] = "Vui lòng nhập đầy đủ thông tin mật khẩu.";
                TempData["ToastType"] = "error";
                return RedirectToAction("HoSo");
            }

            if (matKhauMoi != xacNhanMatKhau)
            {
                TempData["ToastMessage"] = "Mật khẩu mới và xác nhận mật khẩu không khớp.";
                TempData["ToastType"] = "error";
                return RedirectToAction("HoSo");
            }

            if (matKhauMoi.Length < 6)
            {
                TempData["ToastMessage"] = "Mật khẩu mới phải có tối thiểu 6 ký tự.";
                TempData["ToastType"] = "error";
                return RedirectToAction("HoSo");
            }

            var sessionKh = Session["KhachHang"] as KHACHHANG;
            string maTaiKhoan = sessionKh?.MA_KHACHHANG?.Trim() ?? Session["MaTaiKhoan"] as string;

            bool success = authService.ChangePassword(maTaiKhoan, matKhauCu, matKhauMoi, out string err);
            if (success)
            {
                TempData["ToastMessage"] = "Đổi mật khẩu thành công!";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = err ?? "Đổi mật khẩu thất bại.";
                TempData["ToastType"] = "error";
            }

            return RedirectToAction("HoSo");
        }

        // GET: Account/DangXuat
        public ActionResult DangXuat()
        {
            Session.Clear();
            TempData["ToastMessage"] = "Bạn đã đăng xuất an toàn khỏi hệ thống.";
            TempData["ToastType"] = "info";
            return RedirectToAction("TrangChu", "Home");
        }

        [HttpGet]
        public ActionResult QuenMatKhau()
        {
            return View();
        }


        // POST: Account/QuenMatKhau

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> QuenMatKhau(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Vui lòng nhập địa chỉ email.";
                return View();
            }

            string emailTrim = email.Trim().ToLower();

            try
            {
                var taiKhoan = data.TAIKHOANs.FirstOrDefault(t =>
                    t.EMAIL != null &&
                    t.EMAIL.Trim().ToLower() == emailTrim
                );

                if (taiKhoan != null)
                {
                    string token = passwordResetService.CreateToken(
                        taiKhoan.MA_TAIKHOAN
                    );

                    string resetLink = Url.Action(
                        "DatLaiMatKhau",
                        "Account",
                        new { token = token },
                        Request.Url.Scheme
                    );

                    var emailService = new EmailService();

                    bool sent = await emailService.SendPasswordResetEmailAsync(
                        taiKhoan.EMAIL,
                        taiKhoan.HOTEN,
                        resetLink
                    );

                    if (sent)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "GỬI EMAIL ĐẶT LẠI MẬT KHẨU THÀNH CÔNG: " + taiKhoan.EMAIL
                        );
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "GỬI EMAIL ĐẶT LẠI MẬT KHẨU THẤT BẠI: " + taiKhoan.EMAIL
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "LỖI QUÊN MẬT KHẨU: " + ex.ToString()
                );
            }

            ViewBag.Success =
                "Nếu email đã được đăng ký, hệ thống sẽ gửi hướng dẫn đặt lại mật khẩu. Vui lòng kiểm tra hộp thư.";

            return View();
        }



        [HttpGet]
        public ActionResult DatLaiMatKhau(string token)
        {
            string maTaiKhoan;

            if (!passwordResetService.TryGetAccountId(token, out maTaiKhoan))
            {
                ViewBag.Error = "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return View();
            }

            ViewBag.Token = token;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatLaiMatKhau(
            string token,
            string matKhauMoi,
            string xacNhanMatKhau)
        {
            if (string.IsNullOrWhiteSpace(matKhauMoi) ||
                string.IsNullOrWhiteSpace(xacNhanMatKhau))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ mật khẩu.";
                ViewBag.Token = token;
                return View();
            }

            if (matKhauMoi.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
                ViewBag.Token = token;
                return View();
            }

            if (matKhauMoi != xacNhanMatKhau)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                ViewBag.Token = token;
                return View();
            }

   
            string maTaiKhoan;

            if (!passwordResetService.TryGetAccountId(token, out maTaiKhoan))
            {
                ViewBag.Error = "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return View();
            }

            // Tiêu thụ token: chỉ sử dụng được một lần
            if (!passwordResetService.TryConsumeToken(token, out maTaiKhoan))
            {
                ViewBag.Error = "Link đã được sử dụng hoặc hết hạn.";
                return View();
            }

            // Cập nhật mật khẩu
            bool success = authService.ResetPassword(
                maTaiKhoan,
                matKhauMoi,
                out string error
            );

            if (!success)
            {
                ViewBag.Error = error ?? "Không thể cập nhật mật khẩu.";
                return View();
            }

            TempData["ToastMessage"] = "Đặt lại mật khẩu thành công! Hãy đăng nhập lại.";
            TempData["ToastType"] = "success";

            return RedirectToAction("DangNhap");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                data?.Dispose();
                authService?.Dispose();
            }
            base.Dispose(disposing);
        }

     
    }
}
