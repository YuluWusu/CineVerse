using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class AccountController : Controller
    {
        QL_VEPHIMEntities data = new QL_VEPHIMEntities();

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

            string sdtTrim = SDT.Trim();
            var checkTk = data.TAIKHOANs.FirstOrDefault(t => t.SDT.Trim() == sdtTrim);
            if (checkTk != null)
            {
                ViewBag.Error = "Số điện thoại này đã được đăng ký.";
                return View();
            }

            var allIds = data.KHACHHANGs.Select(k => k.MA_KHACHHANG).ToList();
            int maxId = 0;
            foreach (var id in allIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    string trimmed = id.Trim();
                    if (trimmed.StartsWith("KH"))
                    {
                        if (int.TryParse(trimmed.Substring(2), out int currentId))
                        {
                            if (currentId > maxId) maxId = currentId;
                        }
                    }
                }
            }
            int nextId = maxId + 1;
            string newMaKhach = "KH" + nextId.ToString("D3");

            var tk = new TAIKHOAN
            {
                MA_TAIKHOAN = newMaKhach,
                HOTEN = HOTEN.Trim(),
                SDT = sdtTrim,
                EMAIL = !string.IsNullOrWhiteSpace(EMAIL) ? EMAIL.Trim() : null,
                MATKHAU = MATKHAU,
                NGAYTAO = DateTime.Now,
                TRANGTHAI = "HoatDong"
            };

            var vaiTro = data.VAITROes.FirstOrDefault(v => v.MA_VAITRO.Trim() == "KhachHang" || v.TEN_VAITRO.Trim() == "KhachHang");
            if (vaiTro != null)
            {
                tk.VAITROes.Add(vaiTro);
            }

            data.TAIKHOANs.Add(tk);

            KHACHHANG kh = new KHACHHANG
            {
                MA_KHACHHANG = newMaKhach,
                CCCD = cancuoc,
                TAIKHOAN = tk
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

            string sdtTrim = SDT.Trim();
            var tk = data.TAIKHOANs
                .Include(t => t.KHACHHANG)
                .Include(t => t.NHANVIEN)
                .Include(t => t.VAITROes)
                .FirstOrDefault(t => t.SDT.Trim() == sdtTrim && t.MATKHAU == MATKHAU);

            if (tk != null)
            {
                bool laAdmin = tk.VAITROes.Any(v => LaVaiTro(v, "ADMIN"));
                bool laNhanVien = tk.VAITROes.Any(v => LaVaiTro(v, "NHANVIEN") || LaVaiTro(v, "QUANLYRAP"));

                if (laNhanVien && !laAdmin && tk.NHANVIEN != null)
                {
                    Session.Clear();
                    Session["VaiTro"] = "NHANVIEN";
                    Session["NhanVienMa"] = tk.NHANVIEN.MA_NHANVIEN.Trim();
                    Session["NhanVienTen"] = tk.HOTEN;
                    Session["NhanVienRap"] = tk.NHANVIEN.MA_RAP == null ? null : tk.NHANVIEN.MA_RAP.Trim();
                    return RedirectToAction("Index", "NhanVien");
                }

                var kh = tk.KHACHHANG;
                if (kh == null)
                {
                    kh = new KHACHHANG
                    {
                        MA_KHACHHANG = tk.MA_TAIKHOAN,
                        TAIKHOAN = tk
                    };
                    data.KHACHHANGs.Add(kh);
                    data.SaveChanges();
                }

                Session["KhachHang"] = kh;
                Session["VaiTro"] = laAdmin ? "ADMIN" : "KHACHHANG";
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
            string maKh = sessionKh.MA_KHACHHANG.Trim();
            
            var kh = data.KHACHHANGs
                .Include(k => k.TAIKHOAN)
                .Include(k => k.TAIKHOAN.VAITROes)
                .FirstOrDefault(k => k.MA_KHACHHANG.Trim() == maKh);

            if (kh == null)
            {
                Session["KhachHang"] = null;
                return RedirectToAction("DangNhap", "Account");
            }

            Session["KhachHang"] = kh;
            return View(kh);
        }

        private static bool LaVaiTro(VAITRO vaiTro, string ma)
        {
            return (vaiTro.MA_VAITRO != null && vaiTro.MA_VAITRO.Trim().Equals(ma, StringComparison.OrdinalIgnoreCase))
                || (vaiTro.TEN_VAITRO != null && vaiTro.TEN_VAITRO.Trim().Equals(ma, StringComparison.OrdinalIgnoreCase));
        }

        public ActionResult DangXuat()
        {
            Session.Clear();
            Session["KhachHang"] = null;
            return RedirectToAction("TrangChu", "Home");
        }
    }
}