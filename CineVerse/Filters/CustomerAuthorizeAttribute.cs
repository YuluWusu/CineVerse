using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Filters
{
    /// <summary>
    /// Attribute phân quyền dành riêng cho Khách hàng đã đăng nhập.
    /// Cho phép Khách hàng (hoặc Admin đóng vai khách hàng) truy cập các tính năng như Lịch sử vé, Hồ sơ cá nhân.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class CustomerAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;
            var khachHang = session?["KhachHang"];
            string vaiTro = session?["VaiTro"] as string;

            bool daDangNhap = khachHang != null || string.Equals(vaiTro, "KHACHHANG", StringComparison.OrdinalIgnoreCase) || string.Equals(vaiTro, "ADMIN", StringComparison.OrdinalIgnoreCase);

            if (!daDangNhap)
            {
                if (filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    filterContext.Result = new JsonResult
                    {
                        Data = new { success = false, requireLogin = true, message = "Vui lòng đăng nhập để tiếp tục." },
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
        }
    }
}

