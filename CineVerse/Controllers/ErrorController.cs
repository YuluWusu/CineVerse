using System.Web.Mvc;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Error/NotFound (404)
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            ViewBag.Title = "404 - Không tìm thấy trang";
            return View();
        }

        // GET: Error/Forbidden (403)
        public ActionResult Forbidden()
        {
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            ViewBag.Title = "403 - Quyền truy cập bị từ chối";
            return View();
        }

        // GET: Error/InternalServerError (500)
        public ActionResult InternalServerError()
        {
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;
            ViewBag.Title = "500 - Lỗi máy chủ";
            return View();
        }
    }
}

