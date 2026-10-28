using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Filters
{
    /// <summary>
    /// Attribute phân quyền cho phân hệ Quản trị (Admin) và Vận hành (Staff / POS).
    /// Mặc định yêu cầu vai trò ADMIN.
    /// Nếu đặt AllowStaff = true hoặc Roles = "NHANVIEN,ADMIN", cho phép cả nhân viên rạp truy cập.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Cho phép cả nhân viên (NHANVIEN / QUANLYRAP) truy cập hay chỉ riêng ADMIN.
        /// Mặc định: false (chỉ ADMIN).
        /// </summary>
        public bool AllowStaff { get; set; } = false;

        /// <summary>
        /// Danh sách vai trò cho phép, phân cách bằng dấu phẩy (ví dụ: "ADMIN,NHANVIEN,QUANLYRAP").
        /// </summary>
        public string Roles { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;
            string vaiTro = session?["VaiTro"] as string;
            string maNhanVien = session?["NhanVienMa"] as string;

            bool daDangNhap = !string.IsNullOrEmpty(vaiTro);

            if (!daDangNhap)
            {
                XuLyChuaDangNhap(filterContext);
                return;
            }

            bool laAdmin = string.Equals(vaiTro, "ADMIN", StringComparison.OrdinalIgnoreCase);
            bool laNhanVien = string.Equals(vaiTro, "NHANVIEN", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(vaiTro, "QUANLYRAP", StringComparison.OrdinalIgnoreCase)
                           || !string.IsNullOrEmpty(maNhanVien);

            bool hopLe = false;

            if (!string.IsNullOrEmpty(Roles))
            {
                var roleList = Roles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(r => r.Trim().ToUpperInvariant())
                                    .ToList();

                if (laAdmin && roleList.Contains("ADMIN")) hopLe = true;
                if (laNhanVien && (roleList.Contains("NHANVIEN") || roleList.Contains("STAFF") || roleList.Contains("QUANLYRAP"))) hopLe = true;
            }
            else if (AllowStaff)
            {
                hopLe = laAdmin || laNhanVien;
            }
            else
            {
                // Mặc định chỉ Admin
                hopLe = laAdmin;
            }

            if (!hopLe)
            {
                XuLyKhongDuQuyen(filterContext);
            }
        }

        private void XuLyChuaDangNhap(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.Result = new JsonResult
                {
                    Data = new { success = false, requireLogin = true, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            else
            {
                string returnUrl = filterContext.HttpContext.Request.RawUrl;
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        controller = "Account",
                        action = "DangNhap",
                        returnUrl = returnUrl
                    }));
            }
        }

        private void XuLyKhongDuQuyen(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.HttpContext.Response.StatusCode = 403;
                filterContext.Result = new JsonResult
                {
                    Data = new { success = false, message = "Bạn không có quyền thực hiện thao tác này." },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            else
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        controller = "Error",
                        action = "Forbidden"
                    }));
            }
        }
    }
}

