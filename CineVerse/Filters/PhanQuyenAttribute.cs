using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Filters
{
    public class PhanQuyenAttribute : ActionFilterAttribute
    {
        private readonly string[] cacVaiTro;

        public PhanQuyenAttribute(params string[] cacVaiTro)
        {
            this.cacVaiTro = cacVaiTro;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string vaiTro = filterContext.HttpContext.Session["VaiTro"] as string;

            if (vaiTro == null || !cacVaiTro.Contains(vaiTro))
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Account", action = "DangNhap" }));
            }
        }
    }
}
