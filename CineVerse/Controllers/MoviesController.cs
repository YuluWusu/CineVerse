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
        public ActionResult DanhSachPhim(string trangthai = null, string search = null)
        {
            var query = data.PHIMs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim();
                query = query.Where(p => p.TEN_PHIM.Contains(kw)
                                      || (p.DAO_DIEN != null && p.DAO_DIEN.Contains(kw))
                                      || (p.DIEN_VIEN != null && p.DIEN_VIEN.Contains(kw))
                                      || p.THELOAIs.Any(t => t.TEN_THELOAI.Contains(kw)));
                ViewBag.Search = kw;
            }

            if (!string.IsNullOrEmpty(trangthai))
            {
                query = query.Where(p => p.TRANGTHAI == trangthai);
                ViewBag.TrangThai = trangthai;
            }
            else if (string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.TRANGTHAI == "Đang chiếu");
                ViewBag.TrangThai = "Đang chiếu";
            }

            ViewBag.DanhSachPhim = query.OrderByDescending(p => p.NGAYKHOICHIEU).ToList();
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