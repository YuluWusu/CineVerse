using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using DoAn_QuanLyVeXemPhimTrucTiep.Services;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    public class BookingController : Controller
    {
        private readonly BookingService bookingService = new BookingService();
        private readonly ScheduleService scheduleService = new ScheduleService();
        private readonly SeatService seatService = new SeatService();
        
        public ActionResult ChonGhe(string maLichChieu)
        {
            if (string.IsNullOrEmpty(maLichChieu))
            {
                return RedirectToAction("DanhSachPhim", "Movies");
            }

            var lichChieu = scheduleService.GetScheduleById(maLichChieu);
            if (lichChieu == null)
            {
                return HttpNotFound();
            }

            var danhSachGhe = seatService.GetSeatsByRoom(lichChieu.MA_PHONG);
            var gheDaDat = seatService.GetBookedSeatIds(maLichChieu);

            ViewBag.GheDaDat = gheDaDat;
            ViewBag.LichChieu = lichChieu;

            return View(danhSachGhe);
        }

        [HttpPost]
        public ActionResult ThanhToan(string maLichChieu, string selectedSeats)
        {
            if (string.IsNullOrEmpty(selectedSeats) || string.IsNullOrEmpty(maLichChieu))
            {
                return RedirectToAction("ChonGhe", new { maLichChieu = maLichChieu });
            }

            var lichChieu = scheduleService.GetScheduleById(maLichChieu);
            if (lichChieu == null) return HttpNotFound();

            string[] listMaGhe = selectedSeats.Split(',');
            var gheDuocChon = seatService.GetSeatsByIds(listMaGhe.ToList());

            ViewBag.LichChieu = lichChieu;
            ViewBag.SelectedSeatsList = listMaGhe;
            ViewBag.DanhSachKM = bookingService.GetActivePromotions();

            return View(gheDuocChon);
        }

        [HttpPost]
        public ActionResult XuLyThanhToan(string maLichChieu, string selectedSeats,
            string hoTen, string soDienThoai, string Email,
            string maCodeKhuyenMai, string maKmId)
        {
            var lichChieu = scheduleService.GetScheduleById(maLichChieu);
            if (lichChieu == null) return HttpNotFound();

            KHACHHANG khachHang = null;
            
            if (Session["KhachHang"] != null)
            {
                var sessionKh = Session["KhachHang"] as KHACHHANG;
                khachHang = bookingService.GetCustomerById(sessionKh.MA_KHACH);
            }

            if (khachHang == null)
            {
                khachHang = bookingService.GetOrCreateCustomer(hoTen, soDienThoai, Email);
            }

            string[] listMaGhe = selectedSeats.Split(',');
            var gheList = seatService.GetSeatsByIds(listMaGhe.ToList());

            // Xử lý khuyến mãi
            KHUYENMAI khuyenMai = bookingService.ValidatePromotion(maKmId);

            // Tính tiền
            var prices = bookingService.CalculatePrice(lichChieu, gheList, khuyenMai);
            decimal tongTienGoc = prices.Item1;
            decimal soTienGiam = prices.Item2;
            decimal tongTienSauGiam = prices.Item3;

            // Xử lý đặt vé
            try
            {
                bookingService.ProcessBooking(khachHang, lichChieu, gheList, khuyenMai, soTienGiam);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra trong quá trình đặt vé: " + ex.Message;
                return RedirectToAction("ChonGhe", new { maLichChieu = maLichChieu });
            }

            TempData["TongTienGoc"] = tongTienGoc;
            TempData["SoTienGiam"] = soTienGiam;
            TempData["TongTienSauGiam"] = tongTienSauGiam;
            TempData["TenKhuyenMai"] = khuyenMai?.TEN_KM;

            return RedirectToAction("DatVeThanhCong");
        }

        public ActionResult DatVeThanhCong()
        {
            return View();
        }

        [HttpGet]
        public ActionResult LichSuVe()
        {
            if (Session["KhachHang"] == null)
            {
                return RedirectToAction("DangNhap", "Account");
            }

            var sessionKh = Session["KhachHang"] as KHACHHANG;
            var veList = bookingService.GetBookingHistory(sessionKh.MA_KHACH);

            return View(veList);
        }
    }
}
