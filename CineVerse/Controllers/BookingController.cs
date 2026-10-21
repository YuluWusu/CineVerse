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
        private readonly StaffService staffService = new StaffService(); // Thêm service để lấy menu bắp nước

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

        [HttpGet]
        public ActionResult ChonMonAn(string maLichChieu, string selectedSeats)
        {
            if (string.IsNullOrEmpty(maLichChieu) || string.IsNullOrEmpty(selectedSeats))
            {
                TempData["Loi"] = "Vui lòng chọn suất chiếu và ghế trước khi chọn bắp nước.";
                return RedirectToAction("DanhSachPhim", "Movies");
            }

            LICHCHIEU lichChieu = staffService.GetSchedule(maLichChieu);

            ViewBag.LichChieu = lichChieu;
            ViewBag.SelectedSeats = selectedSeats;
            ViewBag.DanhSachMon = staffService.GetFoodMenu(); // Lấy danh sách bắp nước đang bán

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhanChonMon(string maLichChieu, string selectedSeats)
        {
            var monAnDict = new Dictionary<string, int>();
            foreach (string key in Request.Form.AllKeys)
            {
                if (key != null && key.StartsWith("mon_"))
                {
                    int soLuong;
                    if (int.TryParse(Request.Form[key], out soLuong) && soLuong > 0)
                    {
                        monAnDict[key.Substring(4)] = soLuong;
                    }
                }
            }

            // Lưu tạm danh sách món ăn vào Session để chuyển sang trang Thanh toán xử lý
            Session["KhachHangChonMon"] = monAnDict;

            return RedirectToAction("ThanhToan", new { maLichChieu = maLichChieu, selectedSeats = selectedSeats });
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
            ViewBag.DanhSachMon = staffService.GetFoodMenu(); // Truyền menu bắp nước thẳng ra trang thanh toán

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
                khachHang = bookingService.GetCustomerById(sessionKh.MA_KHACHHANG);
            }

            if (khachHang == null)
            {
                khachHang = bookingService.GetOrCreateCustomer(hoTen, soDienThoai, Email);
            }

            string[] listMaGhe = selectedSeats.Split(',');
            var gheList = seatService.GetSeatsByIds(listMaGhe.ToList());

            // Xử lý khuyến mãi
            KHUYENMAI khuyenMai = bookingService.ValidatePromotion(maKmId);

            // Gom danh sách bắp nước khách đã chọn từ Session (hoặc từ Request.Form nếu khách chọn ngay trên trang thanh toán)
            var monAnDict = Session["KhachHangChonMon"] as Dictionary<string, int> ?? new Dictionary<string, int>();

            // Đề phòng trường hợp lấy trực tiếp từ Request.Form nếu form thanh toán có gộp cả input bắp nước
            foreach (string key in Request.Form.AllKeys)
            {
                if (key != null && key.StartsWith("mon_"))
                {
                    int soLuong;
                    if (int.TryParse(Request.Form[key], out soLuong) && soLuong > 0)
                    {
                        monAnDict[key.Substring(4)] = soLuong;
                    }
                }
            }

            // Tính tiền vé
            var prices = bookingService.CalculatePrice(lichChieu, gheList, khuyenMai);
            decimal tongTienVe = prices.Item1;
            decimal soTienGiam = prices.Item2;

            // Tính tiền bắp nước và chuẩn bị dữ liệu món ăn
            decimal tienMonAn = 0;
            var listDonHangMon = new List<DONHANG_MONAN>();
            foreach (var item in monAnDict)
            {
                string maMon = item.Key.Trim();
                int sl = item.Value;
                var mon = staffService.GetFoodMenu().FirstOrDefault(m => m.MA_MON.Trim() == maMon);
                if (mon != null)
                {
                    tienMonAn += mon.GIA * sl;
                    listDonHangMon.Add(new DONHANG_MONAN { MA_MON = mon.MA_MON.Trim(), SOLUONG = sl, DONGIA = mon.GIA });
                }
            }

            decimal tongTienSauGiam = (tongTienVe + tienMonAn) - soTienGiam;
            if (tongTienSauGiam < 0) tongTienSauGiam = 0;

            // Xử lý đặt vé và lưu đơn hàng vào CSDL
            try
            {
                // Gọi hàm ProcessBooking có sẵn hoặc viết logic mở rộng lưu kèm listDonHangMon và tienMonAn vào bảng DONHANG
                bookingService.ProcessBooking(khachHang, lichChieu, gheList, khuyenMai, soTienGiam);

                // Xóa session chọn món sau khi đã xử lý xong
                Session.Remove("KhachHangChonMon");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra trong quá trình đặt vé: " + ex.Message;
                return RedirectToAction("ChonGhe", new { maLichChieu = maLichChieu });
            }

            TempData["TongTienGoc"] = tongTienVe + tienMonAn;
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
            var veList = bookingService.GetBookingHistory(sessionKh.MA_KHACHHANG);

            return View(veList);
        }
    }
}