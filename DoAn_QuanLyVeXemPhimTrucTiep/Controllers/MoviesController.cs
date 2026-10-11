using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class MoviesController : Controller
    {
        // GET: Movies
        QL_VEPHIM2Entities data = new QL_VEPHIM2Entities();
        public ActionResult DanhSachPhim(string trangthai = "Đang chiếu")
        {
            var phims = data.PHIMs
                .Where(p => p.TRANGTHAI == trangthai)
                .AsQueryable();
            ViewBag.DanhSachPhim = phims.ToList();
            ViewBag.TrangThai = trangthai;

            return View();
        }
        public ActionResult ChiTietPhim(string id)
        {
            PHIM phim = data.PHIMs.FirstOrDefault(p=>p.MA_PHIM ==  id);
            if (phim == null)
            {
                return HttpNotFound();
            }
            
            var lichChieus = data.LICHCHIEUx
                                .Where(l => l.MA_PHIM == id)
                                .OrderByDescending(l => l.NGAYCHIEU)
                                .ThenBy(l => l.KHUNGGIO.GIOBATDAU)
                                .ToList();
            
            ViewBag.LichChieu = lichChieus;

            return View(phim);
        }
        
        
        public ActionResult LichChieuChung()
        {
            var lichChieus = data.LICHCHIEUx
                                .OrderByDescending(l => l.NGAYCHIEU)
                                .ThenBy(l => l.PHIM.TEN_PHIM)
                                .ToList();
            return View(lichChieus);
        }
    }
}