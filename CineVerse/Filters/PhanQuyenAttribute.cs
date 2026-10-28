using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Filters
{
    /// <summary>
    /// Attribute phân quyền tương thích ngược (Legacy filter).
    /// </summary>
    public class PhanQuyenAttribute : ActionFilterAttribute
    {
        private readonly string[] cacVaiTro;

        public PhanQuyenAttribute(params string[] cacVaiTro)
        {
            this.cacVaiTro = cacVaiTro ?? new string[0];
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string vaiTro = filterContext.HttpContext.Session?["VaiTro"] as string;

            if (string.IsNullOrEmpty(vaiTro))
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Account", action = "DangNhap", returnUrl = filterContext.HttpContext.Request.RawUrl }));
                return;
            }

            if (!cacVaiTro.Any(v => string.Equals(v, vaiTro, StringComparison.OrdinalIgnoreCase)))
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Error", action = "Forbidden" }));
            }
        }
    }
}
