using System;
using System.Collections.Generic;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Filters;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using DoAn_QuanLyVeXemPhimTrucTiep.Services;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    [PhanQuyen("NHANVIEN", "ADMIN")]
    public class NhanVienController : Controller
    {
        private readonly StaffService staffService = new StaffService();
        private readonly BookingService bookingService = new BookingService();

        private string MaNhanVien
        {
            get { return Session["NhanVienMa"] as string; }
        }

        private string MaRap
        {
            get { return Session["NhanVienRap"] as string; }
        }

        // GET: NhanVien/Index
        public ActionResult Index()
        {
            ViewBag.Title = "Tổng quan";
            List<LICHCHIEU> lichChieu = staffService.GetSchedules(MaRap, 1, false);
            ViewBag.ThongKe = staffService.GetThongKe(MaNhanVien, MaRap);
            ViewBag.ThongKeSuat = staffService.GetThongKeSuat(lichChieu);
            return View(lichChieu);
        }

        // ========================= CHECK-IN =========================
        [HttpGet]
        public ActionResult CheckIn(string tuKhoa)
        {
            ViewBag.Title = "Check-in vé";
            ViewBag.TuKhoa = tuKhoa;
            return View(staffService.TimVe(tuKhoa));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckInVe(string maVe, string tuKhoa)
        {
            CheckInKetQua ketQua = staffService.CheckIn(maVe, MaRap);
            if (ketQua.ThanhCong)
            {
                TempData["CheckInOk"] = ketQua.ThongBao;
            }
            else
            {
                TempData["CheckInLoi"] = ketQua.ThongBao;
            }
            return RedirectToAction("CheckIn", new { tuKhoa = tuKhoa });
        }

        // ========================= BÁN VÉ TẠI QUẦY =========================
        [HttpGet]
        public ActionResult BanVe()
        {
            ViewBag.Title = "Bán vé tại quầy";
            if (string.IsNullOrEmpty(MaNhanVien))
            {
                ViewBag.CanhBao = "Tài khoản của bạn chưa có hồ sơ nhân viên nên chỉ xem được danh sách suất, chưa bán vé được.";
            }
            List<LICHCHIEU> lichChieu = staffService.GetSchedules(MaRap, 2, true);
            ViewBag.ThongKeSuat = staffService.GetThongKeSuat(lichChieu);
            return View(lichChieu);
        }

        [HttpGet]
        public ActionResult ChonGhe(string maLichChieu)
        {
            ViewBag.Title = "Bán vé tại quầy";

            LICHCHIEU lichChieu = staffService.GetSchedule(maLichChieu);
            if (lichChieu == null)
            {
                return RedirectToAction("BanVe");
            }
            if (!StaffService.ConBanDuoc(lichChieu))
            {
                TempData["Loi"] = "Suất chiếu này không còn bán vé được.";
                return RedirectToAction("BanVe");
            }
            if (!string.IsNullOrEmpty(MaRap) && lichChieu.PHONGCHIEU.MA_RAP.Trim() != MaRap.Trim())
            {
                TempData["Loi"] = "Suất chiếu này thuộc rạp khác.";
                return RedirectToAction("BanVe");
            }

            ViewBag.LichChieu = lichChieu;
            ViewBag.GheDaDat = staffService.GetBookedSeatIds(maLichChieu);
            ViewBag.DanhSachMon = staffService.GetFoodMenu();
            ViewBag.DanhSachKM = bookingService.GetActivePromotions();
            return View(staffService.GetSeatsByRoom(lichChieu.MA_PHONG));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XuLyBanVe(string maLichChieu, string selectedSeats, string sdtKhach, string maKm)
        {
            var danhSachGhe = new List<string>();
            if (!string.IsNullOrWhiteSpace(selectedSeats))
            {
                danhSachGhe.AddRange(selectedSeats.Split(','));
            }

            var monAn = new Dictionary<string, int>();
            foreach (string key in Request.Form.AllKeys)
            {
                if (key != null && key.StartsWith("mon_"))
                {
                    int soLuong;
                    if (int.TryParse(Request.Form[key], out soLuong) && soLuong > 0)
                    {
                        monAn[key.Substring(4)] = soLuong;
                    }
                }
            }

            try
            {
                string maDonHang = staffService.BanVeTaiQuay(MaNhanVien, MaRap, maLichChieu, danhSachGhe, monAn, sdtKhach, maKm);
                return RedirectToAction("HoaDon", new { id = maDonHang });
            }
            catch (LoiNghiepVuException ex)
            {
                TempData["Loi"] = ex.Message;
                return RedirectToAction("ChonGhe", new { maLichChieu = maLichChieu });
            }
        }

        [HttpGet]
        public ActionResult HoaDon(string id)
        {
            ViewBag.Title = "Hóa đơn";
            DONHANG donHang = staffService.GetOrder(id);
            if (donHang == null)
            {
                return HttpNotFound();
            }
            return View(donHang);
        }

        [HttpGet]
        public ActionResult LichSuBan()
        {
            ViewBag.Title = "Đơn đã bán";
            return View(staffService.GetOrdersByStaff(MaNhanVien, DateTime.Today));
        }
    }
}
