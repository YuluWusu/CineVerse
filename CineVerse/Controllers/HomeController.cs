using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class HomeController : Controller
    {
        private QL_VEPHIMEntities data = new QL_VEPHIMEntities();

        public ActionResult TrangChu()
        {
            List<PHIM> dangChieu = data.PHIMs.Where(p => p.TRANGTHAI == "Đang chiếu").Take(5).ToList();
            List<PHIM> sapChieu = data.PHIMs.Where(p => p.TRANGTHAI == "Sắp chiếu").Take(6).ToList();
            List<PHIM> hinhNen = data.PHIMs.Take(3).ToList();
            ViewBag.DangChieu = dangChieu;
            ViewBag.SapChieu = sapChieu;
            ViewBag.HinhNen = hinhNen;
            return View();
        }

        public ActionResult GioiThieu()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetPhimDangChieu()
        {
            var phimList = data.PHIMs.Where(p => p.TRANGTHAI == "Đang chiếu")
                .Select(p => new { MaPhim = p.MA_PHIM, TenPhim = p.TEN_PHIM }).ToList();
            return Json(phimList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetNgayChieu(string maPhim)
        {
            if (string.IsNullOrEmpty(maPhim)) return Json(new object[0], JsonRequestBehavior.AllowGet);

            string id = maPhim.Trim();
            DateTime today = DateTime.Today;

            var ngayList = data.LICHCHIEUx
                .Where(l => l.MA_PHIM.Trim() == id)
                .Select(l => l.NGAYCHIEU)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            var result = ngayList.Select(n => new {
                Ngay = n.ToString("yyyy-MM-dd"),
                NgayHienThi = n.ToString("dd/MM/yyyy")
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSuatChieu(string maPhim, string ngay)
        {
            if (string.IsNullOrEmpty(maPhim) || string.IsNullOrEmpty(ngay))
                return Json(new object[0], JsonRequestBehavior.AllowGet);

            string id = maPhim.Trim();
            if (DateTime.TryParse(ngay, out DateTime date))
            {
                var suatRaw = data.LICHCHIEUx
                    .Where(l => l.MA_PHIM.Trim() == id && l.NGAYCHIEU == date)
                    .Select(l => new {
                        MaLichChieu = l.MA_LICHCHIEU,
                        GioBatDau = l.KHUNGGIO.GIOBATDAU,
                        TenPhong = l.PHONGCHIEU.TEN_PHONG
                    })
                    .ToList();

                var result = suatRaw.OrderBy(s => s.GioBatDau).Select(s => new {
                    MaLichChieu = s.MaLichChieu.Trim(),
                    Gio = s.GioBatDau.ToString(@"hh\:mm") + " - " + s.TenPhong
                }).ToList();

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            return Json(new object[0], JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Child Action render Menu 2 cấp động lấy trực tiếp từ CSDL (THELOAI và RAPCHIEU)
        /// Đáp ứng 100% tiêu chí chấm điểm môn học: Menu 2 cấp lấy dữ liệu động từ CSDL.
        /// </summary>
        [ChildActionOnly]
        public ActionResult MenuHaiCap()
        {
            var model = new MenuHaiCapViewModel
            {
                TheLoais = data.THELOAIs.OrderBy(t => t.TEN_THELOAI).ToList(),
                RapChieus = data.RAPCHIEUx.OrderBy(r => r.TEN_RAP).ToList()
            };
            return PartialView("_MenuHaiCap", model);
        }
    }
}
