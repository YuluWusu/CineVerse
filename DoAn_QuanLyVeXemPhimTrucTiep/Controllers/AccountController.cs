using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class AccountController : Controller
    {
        QL_VEPHIM2Entities data = new QL_VEPHIM2Entities();

        // GET: Account/DangKy
        [HttpGet]
        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangKy(string HOTEN, string SDT, string EMAIL, string MATKHAU, string cancuoc, string XacNhanMatKhau)
        {
            if (string.IsNullOrEmpty(HOTEN) || string.IsNullOrEmpty(SDT) || string.IsNullOrEmpty(MATKHAU))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin bắt buộc.";
                return View();
            }

            if (MATKHAU != XacNhanMatKhau)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            var checkKhach = data.KHACHHANGs.FirstOrDefault(k => k.SDT == SDT);
            if (checkKhach != null)
            {
                ViewBag.Error = "Số điện thoại này đã được đăng ký.";
                return View();
            }

            var allIds = data.KHACHHANGs.Select(k => k.MA_KHACH).ToList();
            int maxId = 0;
            foreach (var id in allIds)
            {
                if (!string.IsNullOrEmpty(id) && id.StartsWith("KH"))
                {
                    if (int.TryParse(id.Substring(2), out int currentId))
                    {
                        if (currentId > maxId) maxId = currentId;
                    }
                }
            }
            int nextId = maxId + 1;
            string newMaKhach = "KH" + nextId.ToString("D3");

            KHACHHANG kh = new KHACHHANG
            {
                MA_KHACH = newMaKhach,
                HOTEN = HOTEN,
                SDT = SDT,
                EMAIL = EMAIL,
                CCCD = cancuoc,
                MATKHAU = MATKHAU,
                VAITRO = "KhachHang",
                NGAYTAO = DateTime.Now
            };

            data.KHACHHANGs.Add(kh);
            data.SaveChanges();

            ViewBag.Success = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
            return View();
        }

        // GET: Account/DangNhap
        [HttpGet]
        public ActionResult DangNhap()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangNhap(string SDT, string MATKHAU)
        {
            if (string.IsNullOrEmpty(SDT) || string.IsNullOrEmpty(MATKHAU))
            {
                ViewBag.Error = "Vui lòng nhập số điện thoại và mật khẩu.";
                return View();
            }

            var kh = data.KHACHHANGs.FirstOrDefault(k => k.SDT == SDT && k.MATKHAU == MATKHAU);
            if (kh != null)
            {
                Session["KhachHang"] = kh;
                return RedirectToAction("TrangChu", "Home");
            }
            else
            {
                ViewBag.Error = "Số điện thoại hoặc mật khẩu không đúng.";
                return View();
            }
        }

        [HttpGet]
        public ActionResult HoSo()
        {
            if (Session["KhachHang"] == null)
            {
                return RedirectToAction("DangNhap", "Account");
            }

            var sessionKh = Session["KhachHang"] as KHACHHANG;
            
            var kh = data.KHACHHANGs.FirstOrDefault(k => k.MA_KHACH == sessionKh.MA_KHACH);

            if (kh == null)
            {
                Session["KhachHang"] = null;
                return RedirectToAction("DangNhap", "Account");
            }

            return View(kh);
        }

        public ActionResult DangXuat()
        {
            Session["KhachHang"] = null;
            return RedirectToAction("TrangChu", "Home");
        }
    }
}