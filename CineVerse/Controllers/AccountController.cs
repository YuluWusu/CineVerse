using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Filters;
using DoAn_QuanLyVeXemPhimTrucTiep.Helpers;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using DoAn_QuanLyVeXemPhimTrucTiep.Services;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class AccountController : Controller
    {
        private readonly QL_VEPHIMEntities data = new QL_VEPHIMEntities();
        private readonly AuthService authService = new AuthService();

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
        public ActionResult DangNhap(string SDT, string MATKHAU, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(SDT) || string.IsNullOrWhiteSpace(MATKHAU))
            {
                ViewBag.Error = "Vui lòng nhập số điện thoại và mật khẩu.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var result = authService.Authenticate(SDT, MATKHAU);
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
                Session["NhanVienRap"] = result.Staff.MA_RAP == null ? null : result.Staff.MA_RAP.Trim();

                TempData["ToastMessage"] = "Đăng nhập thành công với quyền Nhân viên rạp.";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index", "NhanVien");
            }

            var kh = result.Customer;
            Session["KhachHang"] = kh;
            Session["VaiTro"] = result.IsAdmin ? "ADMIN" : "KHACHHANG";
            Session["HoTen"] = result.User.HOTEN;
            Session["MaTaiKhoan"] = result.User.MA_TAIKHOAN.Trim();

            TempData["ToastMessage"] = "Đăng nhập thành công! Chào mừng " + result.User.HOTEN + " đến với CinéVerse.";
            TempData["ToastType"] = "success";

            // Kiểm tra returnUrl an toàn chống Open Redirect
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
