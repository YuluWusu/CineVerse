using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using DoAn_QuanLyVeXemPhimTrucTiep.Services;
using System.IO;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Filters;
using DoAn_QuanLyVeXemPhimTrucTiep.Helpers;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Controllers
{
    [PhanQuyen("ADMIN")]
    public class AdminController : Controller
    {
        private readonly MovieService movieService = new MovieService();
        private readonly CinemaService cinemaService = new CinemaService();
        private readonly ScheduleService scheduleService = new ScheduleService();
        private readonly ReportService reportService = new ReportService();
        private readonly TicketService ticketService = new TicketService();
        private readonly CustomerService customerService = new CustomerService();
        private readonly PromotionService promotionService = new PromotionService();
        private readonly FnBService _fnBService = new FnBService();

        // GET: Admin/Index (Dashboard)
        public ActionResult Index()
        {
            ViewBag.TotalRevenue = reportService.GetTotalRevenue();
            ViewBag.TotalTickets = reportService.GetTotalTickets();
            ViewBag.TotalMovies = reportService.GetTotalMovies();
            ViewBag.TotalCustomers = reportService.GetTotalCustomers();
            ViewBag.RecentTickets = reportService.GetRecentTickets(6);

            using (var db = new QL_VEPHIMEntities())
            {
                ViewBag.RapList = db.RAPCHIEUx.OrderBy(r => r.TEN_RAP).ToList();
            }

            ViewBag.InitialAnalytics = reportService.GetAnalytics("7days", null);

            return View();
        }

        [HttpGet]
        public JsonResult GetAnalyticsData(string timeRange, string maRap, string tuNgay, string denNgay)
        {
            try
            {
                DateTime? tNgay = null;
                DateTime? dNgay = null;
                if (!string.IsNullOrEmpty(tuNgay) && DateTime.TryParse(tuNgay, out DateTime parsedTu))
                {
                    tNgay = parsedTu;
                }
                if (!string.IsNullOrEmpty(denNgay) && DateTime.TryParse(denNgay, out DateTime parsedDen))
                {
                    dNgay = parsedDen;
                }

                var data = reportService.GetAnalytics(timeRange, maRap, tNgay, dNgay);
                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult XuatBaoCaoDoanhThu(string timeRange, string maRap, string tuNgay, string denNgay)
        {
            DateTime? tNgay = null;
            DateTime? dNgay = null;
            if (!string.IsNullOrEmpty(tuNgay) && DateTime.TryParse(tuNgay, out DateTime parsedTu))
            {
                tNgay = parsedTu;
            }
            if (!string.IsNullOrEmpty(denNgay) && DateTime.TryParse(denNgay, out DateTime parsedDen))
            {
                dNgay = parsedDen;
            }

            var exportList = reportService.GetExportData(timeRange, maRap, tNgay, dNgay);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Mã đơn hàng,Ngày đặt,Khách hàng,Số điện thoại,Phim,Cụm rạp,Phòng chiếu,Giờ chiếu,Số vé,Ghế,Tiền vé (VNĐ),Tiền bắp nước (VNĐ),Tiền giảm (VNĐ),Tổng thanh toán (VNĐ),Phương thức thanh toán,Trạng thái,Mã Check-in");

            foreach (var item in exportList)
            {
                string khach = "\"" + (item.TenKhachHang ?? "").Replace("\"", "\"\"") + "\"";
                string phim = "\"" + (item.TenPhim ?? "").Replace("\"", "\"\"") + "\"";
                string rap = "\"" + (item.TenRap ?? "").Replace("\"", "\"\"") + "\"";
                string phong = "\"" + (item.TenPhong ?? "").Replace("\"", "\"\"") + "\"";
                string ghe = "\"" + (item.DanhSachGhe ?? "").Replace("\"", "\"\"") + "\"";
                string pt = "\"" + (item.PhuongThucThanhToan ?? "").Replace("\"", "\"\"") + "\"";

                sb.AppendLine($"{item.MaDonHang},{item.NgayDat:yyyy-MM-dd HH:mm},{khach},'{item.SoDienThoai},{phim},{rap},{phong},{item.GioChieu},{item.SoVe},{ghe},{item.TienVe:0},{item.TienBapNuoc:0},{item.TienGiam:0},{item.TongTien:0},{pt},{item.TrangThaiThanhToan},{item.MaCheckIn}");
            }

            byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
            byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            byte[] fileBytes = new byte[bom.Length + textBytes.Length];
            Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
            Buffer.BlockCopy(textBytes, 0, fileBytes, bom.Length, textBytes.Length);

            string fileName = $"BaoCao_DoanhThu_CineVerse_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(fileBytes, "text/csv; charset=utf-8", fileName);
        }

        // ========================= QUẢN LÝ PHIM =========================
        public ActionResult QuanLyPhim()
        {
            return View(movieService.GetAllMovies());
        }

        public ActionResult ThemPhim()
        {
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ThemPhim(PHIM phim, HttpPostedFileBase fileUpload)
        {
            if (ModelState.IsValid)
            {
                phim.MA_PHIM = movieService.GenerateNextMovieId();

                if (fileUpload != null && fileUpload.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(fileUpload.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/HinhAnh/"), fileName);
                    fileUpload.SaveAs(path);
                    phim.POSTER = fileName;
                }

                movieService.AddMovie(phim);
                return RedirectToAction("QuanLyPhim");
            }
            return View(phim);
        }

        public ActionResult SuaPhim(string id)
        {
            var phim = movieService.GetMovieById(id);
            if (phim == null) return HttpNotFound();
            return View(phim);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SuaPhim(PHIM phim, HttpPostedFileBase fileUpload)
        {
            if (ModelState.IsValid)
            {
                if (fileUpload != null && fileUpload.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(fileUpload.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/HinhAnh/"), fileName);
                    fileUpload.SaveAs(path);
                    phim.POSTER = fileName;
                }
                
                movieService.UpdateMovie(phim);
                return RedirectToAction("QuanLyPhim");
            }
            return View(phim);
        }

        [HttpPost]
        public ActionResult XoaPhim(string id)
        {
            movieService.DeleteMovie(id);
            return RedirectToAction("QuanLyPhim");
        }

        // ========================= QUẢN LÝ RẠP =========================
        public ActionResult QuanLyRap()
        {
            ViewBag.Title = "Quản lý Rạp chiếu";
            return View(cinemaService.GetAllCinemas());
        }

        public ActionResult ThemRap()
        {
            ViewBag.Title = "Thêm Rạp chiếu";
            return View();
        }

        [HttpPost]
        public ActionResult ThemRap(RAPCHIEU rap)
        {
            if (ModelState.IsValid)
            {
                rap.MA_RAP = cinemaService.GenerateNextCinemaId();
                cinemaService.AddCinema(rap);
                return RedirectToAction("QuanLyRap");
            }
            return View(rap);
        }

        public ActionResult SuaRap(string id)
        {
            ViewBag.Title = "Sửa Rạp chiếu";
            var rap = cinemaService.GetCinemaById(id);
            if (rap == null) return HttpNotFound();
            return View(rap);
        }

        [HttpPost]
        public ActionResult SuaRap(RAPCHIEU rap)
        {
            if (ModelState.IsValid)
            {
                cinemaService.UpdateCinema(rap);
                return RedirectToAction("QuanLyRap");
            }
            return View(rap);
        }

        [HttpPost]
        public ActionResult XoaRap(string id)
        {
            if (!cinemaService.CanDeleteCinema(id))
            {
                TempData["Error"] = "Không thể xóa rạp này vì đang có phòng chiếu trực thuộc.";
                return RedirectToAction("QuanLyRap");
            }
            cinemaService.DeleteCinema(id);
            return RedirectToAction("QuanLyRap");
        }

        // ========================= QUẢN LÝ PHÒNG =========================
        public ActionResult QuanLyPhong(string idRap)
        {
            ViewBag.Title = "Quản lý Phòng chiếu";
            var phongs = cinemaService.GetAllRooms(idRap);
            ViewBag.Raps = new SelectList(cinemaService.GetAllCinemas(), "MA_RAP", "TEN_RAP", idRap);
            return View(phongs);
        }

        public ActionResult ThemPhong()
        {
            ViewBag.Title = "Thêm Phòng chiếu";
            ViewBag.MA_RAP = new SelectList(cinemaService.GetAllCinemas(), "MA_RAP", "TEN_RAP");
            return View();
        }

        [HttpPost]
        public ActionResult ThemPhong(PHONGCHIEU phong)
        {
            if (ModelState.IsValid)
            {
                phong.MA_PHONG = cinemaService.GenerateNextRoomId();
                cinemaService.AddRoomAndGenerateSeats(phong);
                return RedirectToAction("QuanLyPhong");
            }
            ViewBag.MA_RAP = new SelectList(cinemaService.GetAllCinemas(), "MA_RAP", "TEN_RAP", phong.MA_RAP);
            return View(phong);
        }

        public ActionResult SuaPhong(string id)
        {
            ViewBag.Title = "Sửa Phòng chiếu";
            var phong = cinemaService.GetRoomById(id);
            if (phong == null) return HttpNotFound();
            ViewBag.MA_RAP = new SelectList(cinemaService.GetAllCinemas(), "MA_RAP", "TEN_RAP", phong.MA_RAP);
            return View(phong);
        }

        [HttpPost]
        public ActionResult SuaPhong(PHONGCHIEU phong)
        {
            if (ModelState.IsValid)
            {
                cinemaService.UpdateRoom(phong);
                return RedirectToAction("QuanLyPhong");
            }
            ViewBag.MA_RAP = new SelectList(cinemaService.GetAllCinemas(), "MA_RAP", "TEN_RAP", phong.MA_RAP);
            return View(phong);
        }

        [HttpPost]
        public ActionResult XoaPhong(string id)
        {
            if (!cinemaService.CanDeleteRoom(id))
            {
                TempData["Error"] = "Không thể xóa phòng này vì đang có lịch chiếu diễn ra tại đây.";
                return RedirectToAction("QuanLyPhong");
            }
            cinemaService.DeleteRoom(id);
            return RedirectToAction("QuanLyPhong");
        }

        // ========================= QUẢN LÝ LỊCH CHIẾU =========================
        public ActionResult QuanLyLichChieu(string maPhim)
        {
            ViewBag.Title = "Quản lý Lịch chiếu";
            var lichchieus = scheduleService.GetAllSchedules(maPhim);
            ViewBag.Phims = new SelectList(movieService.GetAllMovies(), "MA_PHIM", "TEN_PHIM", maPhim);
            return View(lichchieus);
        }

        [HttpGet]
        public JsonResult GetRoomsByCinema(string maRap)
        {
            var rooms = cinemaService.GetAllRooms(maRap)
                .Select(r => new {
                    maPhong = r.MA_PHONG.Trim(),
                    tenPhong = r.TEN_PHONG,
                    tenRap = r.RAPCHIEU != null ? r.RAPCHIEU.TEN_RAP : ""
                })
                .ToList();
            return Json(rooms, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ThemLichChieu()
        {
            ViewBag.Title = "Thêm Lịch chiếu";
            ViewBag.MA_PHIM = new SelectList(movieService.GetAllMovies(), "MA_PHIM", "TEN_PHIM");
            ViewBag.Cinemas = cinemaService.GetAllCinemas();
            ViewBag.MA_RAP = new SelectList(cinemaService.GetAllCinemas(), "MA_RAP", "TEN_RAP");

            var allRooms = cinemaService.GetAllRooms();
            ViewBag.DanhSachPhong = allRooms;
            ViewBag.MA_PHONG = new SelectList(allRooms.Select(r => new {
                MA_PHONG = r.MA_PHONG.Trim(),
                TEN_PHONG = (r.RAPCHIEU != null ? r.RAPCHIEU.TEN_RAP + " - " : "") + r.TEN_PHONG
            }), "MA_PHONG", "TEN_PHONG");

            ViewBag.MA_KHUNGGIO = new SelectList(scheduleService.GetAllTimeSlots().Select(k => new {
                MA_KHUNGGIO = k.MA_KHUNGGIO.Trim(),
                GIOBATDAU = k.GIOBATDAU.ToString(@"hh\:mm") + " - " + k.GIOKETTHUC.ToString(@"hh\:mm")
            }), "MA_KHUNGGIO", "GIOBATDAU");

            return View();
        }

        [HttpPost]
        public ActionResult ThemLichChieu(LICHCHIEU lichChieu)
        {
            if (ModelState.IsValid)
            {
                if (scheduleService.IsDuplicateSchedule(lichChieu.MA_PHONG, lichChieu.MA_KHUNGGIO, lichChieu.NGAYCHIEU))
                {
                    TempData["Error"] = "Lịch chiếu này bị trùng lặp (Cùng phòng, cùng ngày, cùng khung giờ). Vui lòng chọn lại.";
                }
                else
                {
                    lichChieu.MA_LICHCHIEU = scheduleService.GenerateNextScheduleId();
                    scheduleService.AddSchedule(lichChieu);
                    return RedirectToAction("QuanLyLichChieu");
                }
            }
            ViewBag.MA_PHIM = new SelectList(movieService.GetAllMovies(), "MA_PHIM", "TEN_PHIM", lichChieu.MA_PHIM);
            ViewBag.Cinemas = cinemaService.GetAllCinemas();
            ViewBag.MA_RAP = new SelectList(cinemaService.GetAllCinemas(), "MA_RAP", "TEN_RAP");

            var allRooms = cinemaService.GetAllRooms();
            ViewBag.DanhSachPhong = allRooms;
            ViewBag.MA_PHONG = new SelectList(allRooms.Select(r => new {
                MA_PHONG = r.MA_PHONG.Trim(),
                TEN_PHONG = (r.RAPCHIEU != null ? r.RAPCHIEU.TEN_RAP + " - " : "") + r.TEN_PHONG
            }), "MA_PHONG", "TEN_PHONG", lichChieu.MA_PHONG?.Trim());

            ViewBag.MA_KHUNGGIO = new SelectList(scheduleService.GetAllTimeSlots().Select(k => new {
                MA_KHUNGGIO = k.MA_KHUNGGIO.Trim(),
                GIOBATDAU = k.GIOBATDAU.ToString(@"hh\:mm") + " - " + k.GIOKETTHUC.ToString(@"hh\:mm")
            }), "MA_KHUNGGIO", "GIOBATDAU", lichChieu.MA_KHUNGGIO?.Trim());

            return View(lichChieu);
        }

        public ActionResult SuaLichChieu(string id)
        {
            ViewBag.Title = "Sửa Lịch chiếu";
            var lichChieu = scheduleService.GetScheduleById(id);
            if (lichChieu == null) return HttpNotFound();

            ViewBag.MA_PHIM = new SelectList(movieService.GetAllMovies(), "MA_PHIM", "TEN_PHIM", lichChieu.MA_PHIM);

            var room = cinemaService.GetRoomById(lichChieu.MA_PHONG);
            string currentRap = room?.MA_RAP?.Trim();
            ViewBag.SelectedRap = currentRap;
            ViewBag.Cinemas = cinemaService.GetAllCinemas();
            ViewBag.MA_RAP = new SelectList(cinemaService.GetAllCinemas(), "MA_RAP", "TEN_RAP", currentRap);

            var allRooms = cinemaService.GetAllRooms();
            ViewBag.DanhSachPhong = allRooms;
            ViewBag.MA_PHONG = new SelectList(allRooms.Select(r => new {
                MA_PHONG = r.MA_PHONG.Trim(),
                TEN_PHONG = (r.RAPCHIEU != null ? r.RAPCHIEU.TEN_RAP + " - " : "") + r.TEN_PHONG
            }), "MA_PHONG", "TEN_PHONG", lichChieu.MA_PHONG.Trim());

            ViewBag.MA_KHUNGGIO = new SelectList(scheduleService.GetAllTimeSlots().Select(k => new {
                MA_KHUNGGIO = k.MA_KHUNGGIO.Trim(),
                GIOBATDAU = k.GIOBATDAU.ToString(@"hh\:mm") + " - " + k.GIOKETTHUC.ToString(@"hh\:mm")
            }), "MA_KHUNGGIO", "GIOBATDAU", lichChieu.MA_KHUNGGIO.Trim());

            return View(lichChieu);
        }

        [HttpPost]
        public ActionResult SuaLichChieu(LICHCHIEU lichChieu)
        {
            if (ModelState.IsValid)
            {
                if (scheduleService.IsDuplicateSchedule(lichChieu.MA_PHONG, lichChieu.MA_KHUNGGIO, lichChieu.NGAYCHIEU, lichChieu.MA_LICHCHIEU))
                {
                    TempData["Error"] = "Lịch chiếu cập nhật bị trùng lặp với một suất khác. Vui lòng chọn lại.";
                }
                else
                {
                    scheduleService.UpdateSchedule(lichChieu);
                    return RedirectToAction("QuanLyLichChieu");
                }
            }
            ViewBag.MA_PHIM = new SelectList(movieService.GetAllMovies(), "MA_PHIM", "TEN_PHIM", lichChieu.MA_PHIM);

            var room = cinemaService.GetRoomById(lichChieu.MA_PHONG);
            string currentRap = room?.MA_RAP?.Trim();
            ViewBag.SelectedRap = currentRap;
            ViewBag.Cinemas = cinemaService.GetAllCinemas();
            ViewBag.MA_RAP = new SelectList(cinemaService.GetAllCinemas(), "MA_RAP", "TEN_RAP", currentRap);

            var allRooms = cinemaService.GetAllRooms();
            ViewBag.DanhSachPhong = allRooms;
            ViewBag.MA_PHONG = new SelectList(allRooms.Select(r => new {
                MA_PHONG = r.MA_PHONG.Trim(),
                TEN_PHONG = (r.RAPCHIEU != null ? r.RAPCHIEU.TEN_RAP + " - " : "") + r.TEN_PHONG
            }), "MA_PHONG", "TEN_PHONG", lichChieu.MA_PHONG?.Trim());

            ViewBag.MA_KHUNGGIO = new SelectList(scheduleService.GetAllTimeSlots().Select(k => new {
                MA_KHUNGGIO = k.MA_KHUNGGIO.Trim(),
                GIOBATDAU = k.GIOBATDAU.ToString(@"hh\:mm") + " - " + k.GIOKETTHUC.ToString(@"hh\:mm")
            }), "MA_KHUNGGIO", "GIOBATDAU", lichChieu.MA_KHUNGGIO?.Trim());

            return View(lichChieu);
        }

        [HttpPost]
        public ActionResult XoaLichChieu(string id)
        {
            if (!scheduleService.CanDeleteSchedule(id))
            {
                TempData["Error"] = "Không thể xóa lịch chiếu này vì đã có khách hàng đặt vé.";
                return RedirectToAction("QuanLyLichChieu");
            }
            scheduleService.DeleteSchedule(id);
            return RedirectToAction("QuanLyLichChieu");
        }

        // ========================= QUẢN LÝ VÉ =========================
        public ActionResult QuanLyVe(string searchSdt, string searchMaLich)
        {
            ViewBag.Title = "Quản lý Vé";
            ViewBag.searchSdt = searchSdt;
            ViewBag.searchMaLich = searchMaLich;
            return View(ticketService.GetAllTickets(searchSdt, searchMaLich));
        }

        [HttpPost]
        public ActionResult CapNhatTrangThaiVe(string id, string status)
        {
            ticketService.UpdateTicketStatus(id, status);
            return RedirectToAction("QuanLyVe");
        }

        [HttpPost]
        public ActionResult XoaVe(string id)
        {
            ticketService.DeleteTicket(id);
            return RedirectToAction("QuanLyVe");
        }

        // ========================= QUẢN LÝ KHÁCH HÀNG =========================
        public ActionResult QuanLyKhachHang()
        {
            ViewBag.Title = "Quản lý Khách hàng";
            return View(customerService.GetAllCustomers());
        }

        public ActionResult ChiTietKhachHang(string id)
        {
            ViewBag.Title = "Chi tiết Khách hàng";
            var kh = customerService.GetCustomerById(id);
            if (kh == null) return HttpNotFound();

            ViewBag.LichSuVe = new BookingService().GetBookingHistory(id); // Inject BookingService maybe, or just instantiate here temporarily.
            return View(kh);
        }

        [HttpPost]
        public ActionResult SuaQuyenKhachHang(string id, string vaiTro)
        {
            customerService.UpdateCustomerRole(id, vaiTro);
            return RedirectToAction("QuanLyKhachHang");
        }

        [HttpPost]
        public ActionResult XoaKhachHang(string id)
        {
            if (!customerService.CanDeleteCustomer(id))
            {
                TempData["Error"] = "Không thể xóa tài khoản này vì khách hàng đã có lịch sử đặt vé.";
                return RedirectToAction("QuanLyKhachHang");
            }
            customerService.DeleteCustomer(id);
            return RedirectToAction("QuanLyKhachHang");
        }

        // ========================= QUẢN LÝ KHUNG GIỜ =========================
        public ActionResult QuanLyKhungGio()
        {
            ViewBag.Title = "Quản lý Khung Giờ Chiếu";
            return View(scheduleService.GetAllTimeSlots());
        }

        public ActionResult ThemKhungGio()
        {
            ViewBag.Title = "Thêm Khung Giờ";
            return View();
        }

        [HttpPost]
        public ActionResult ThemKhungGio(string gioBatDau, string gioKetThuc)
        {
            if (!string.IsNullOrEmpty(gioBatDau) && !string.IsNullOrEmpty(gioKetThuc))
            {
                scheduleService.AddTimeSlot(gioBatDau, gioKetThuc);
                TempData["Success"] = "Thêm khung giờ thành công!";
                return RedirectToAction("QuanLyKhungGio");
            }
            TempData["Error"] = "Vui lòng nhập đầy đủ thông tin.";
            return View();
        }

        public ActionResult SuaKhungGio(string id)
        {
            ViewBag.Title = "Sửa Khung Giờ";
            var kg = scheduleService.GetTimeSlotById(id);
            if (kg == null) return HttpNotFound();
            return View(kg);
        }

        [HttpPost]
        public ActionResult SuaKhungGio(string id, string gioBatDau, string gioKetThuc)
        {
            if (!string.IsNullOrEmpty(gioBatDau) && !string.IsNullOrEmpty(gioKetThuc))
            {
                scheduleService.UpdateTimeSlot(id, gioBatDau, gioKetThuc);
                TempData["Success"] = "Cập nhật khung giờ thành công!";
            }
            return RedirectToAction("QuanLyKhungGio");
        }

        [HttpPost]
        public ActionResult XoaKhungGio(string id)
        {
            if (!scheduleService.CanDeleteTimeSlot(id))
            {
                TempData["Error"] = "Không thể xóa khung giờ này vì đang được sử dụng trong lịch chiếu!";
                return RedirectToAction("QuanLyKhungGio");
            }
            scheduleService.DeleteTimeSlot(id);
            TempData["Success"] = "Xóa khung giờ thành công!";
            return RedirectToAction("QuanLyKhungGio");
        }

        // ========================= QUẢN LÝ KHUYẾN MÃI =========================
        public ActionResult QuanLyKhuyenMai()
        {
            ViewBag.Title = "Quản lý Khuyến Mãi";
            return View(promotionService.GetAllPromotions());
        }

        public ActionResult ThemKhuyenMai()
        {
            ViewBag.Title = "Thêm Khuyến Mãi";
            return View();
        }

        [HttpPost]
        public ActionResult ThemKhuyenMai(KHUYENMAI km)
        {
            if (promotionService.IsCodeExists(km.MA_CODE))
            {
                TempData["Error"] = "Mã code khuyến mãi đã tồn tại!";
                return View(km);
            }

            km.MA_KM = promotionService.GenerateNextPromotionId();
            promotionService.AddPromotion(km);
            TempData["Success"] = "Thêm khuyến mãi thành công!";
            return RedirectToAction("QuanLyKhuyenMai");
        }

        public ActionResult SuaKhuyenMai(string id)
        {
            ViewBag.Title = "Sửa Khuyến Mãi";
            var km = promotionService.GetPromotionById(id);
            if (km == null) return HttpNotFound();
            return View(km);
        }

        [HttpPost]
        public ActionResult SuaKhuyenMai(KHUYENMAI km)
        {
            if (promotionService.IsCodeExists(km.MA_CODE, km.MA_KM))
            {
                TempData["Error"] = "Mã code khuyến mãi đã tồn tại!";
                return View(km);
            }

            promotionService.UpdatePromotion(km);
            TempData["Success"] = "Cập nhật khuyến mãi thành công!";
            return RedirectToAction("QuanLyKhuyenMai");
        }

        [HttpPost]
        public ActionResult XoaKhuyenMai(string id)
        {
            promotionService.DeletePromotion(id);
            TempData["Success"] = "Xóa khuyến mãi thành công!";
            return RedirectToAction("QuanLyKhuyenMai");
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult KiemTraKhuyenMai(string maCode, decimal tongTien)
        {
            if (string.IsNullOrEmpty(maCode))
                return Json(new { success = false, message = "Vui lòng nhập mã khuyến mãi." });

            var km = promotionService.ValidatePromotionCode(maCode);
            if (km == null)
                return Json(new { success = false, message = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn." });

            if (km.SO_LUOT_TOIDA.HasValue && km.SO_LUOT_DADUNG >= km.SO_LUOT_TOIDA.Value)
                return Json(new { success = false, message = "Mã khuyến mãi đã hết lượt sử dụng." });

            decimal soTienGiam = tongTien * km.GIAMGIA_PHANTRAM / 100m;
            if (km.GIAMGIA_TOIDA.HasValue && soTienGiam > km.GIAMGIA_TOIDA.Value)
                soTienGiam = km.GIAMGIA_TOIDA.Value;

            decimal tongSauGiam = tongTien - soTienGiam;

            return Json(new
            {
                success = true,
                maKm = km.MA_KM,
                tenKm = km.TEN_KM,
                phantram = km.GIAMGIA_PHANTRAM,
                soTienGiam = soTienGiam,
                tongSauGiam = tongSauGiam,
                message = $"Áp dụng thành công! Giảm {km.GIAMGIA_PHANTRAM}% (-{soTienGiam:0,0}đ)"
            });
        }

        // ========================= QUẢN LÝ LOẠI GHẾ =========================
        public ActionResult QuanLyLoaiGhe()
        {
            ViewBag.Title = "Quản lý Loại ghế";
            return View(cinemaService.GetAllSeatTypes());
        }

        public ActionResult ThemLoaiGhe()
        {
            ViewBag.Title = "Thêm Loại ghế";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemLoaiGhe(LOAIGHE loaiGhe)
        {
            if (ModelState.IsValid)
            {
                if (cinemaService.IsSeatTypeNameExists(loaiGhe.TEN_LOAIGHE))
                {
                    ModelState.AddModelError("TEN_LOAIGHE", "Tên loại ghế đã tồn tại. Vui lòng nhập tên khác.");
                    return View(loaiGhe);
                }

                loaiGhe.MA_LOAIGHE = cinemaService.GenerateNextSeatTypeId();
                cinemaService.AddSeatType(loaiGhe);
                return RedirectToAction("QuanLyLoaiGhe");
            }
            return View(loaiGhe);
        }

        public ActionResult SuaLoaiGhe(string id)
        {
            var loaiGhe = cinemaService.GetSeatTypeById(id);
            if (loaiGhe == null) return HttpNotFound();
            return View(loaiGhe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaLoaiGhe(LOAIGHE loaiGhe)
        {
            if (ModelState.IsValid)
            {
                if (cinemaService.IsSeatTypeNameExists(loaiGhe.TEN_LOAIGHE, loaiGhe.MA_LOAIGHE))
                {
                    TempData["Error"] = "Tên loại ghế đã tồn tại!";
                    return View(loaiGhe);
                }
                cinemaService.UpdateSeatType(loaiGhe);
                TempData["Success"] = "Cập nhật loại ghế thành công!";
                return RedirectToAction("QuanLyLoaiGhe");
            }
            return View(loaiGhe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaLoaiGhe(string id)
        {
            if (!cinemaService.CanDeleteSeatType(id))
            {
                TempData["Error"] = "Loại ghế đang được sử dụng, không thể xóa.";
                return RedirectToAction("QuanLyLoaiGhe");
            }
            cinemaService.DeleteSeatType(id);
            return RedirectToAction("QuanLyLoaiGhe");
        }

        // ========================= QUẢN LÝ THỂ LOẠI =========================
        public ActionResult QuanLyTheLoai()
        {
            ViewBag.Title = "Quản lý Thể loại phim";
            return View(movieService.GetAllGenres());
        }

        public ActionResult ThemTheLoai()
        {
            ViewBag.Title = "Thêm Thể loại phim";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemTheLoai(THELOAI theLoai)
        {
            if (ModelState.IsValid)
            {
                if (movieService.IsGenreNameExists(theLoai.TEN_THELOAI))
                {
                    ModelState.AddModelError("TEN_THELOAI", "Tên thể loại đã tồn tại. Vui lòng nhập tên khác.");
                    return View(theLoai);
                }

                theLoai.MA_THELOAI = movieService.GenerateNextGenreId();
                movieService.AddGenre(theLoai);
                return RedirectToAction("QuanLyTheLoai");
            }
            return View(theLoai);
        }

        public ActionResult SuaTheLoai(string id)
        {
            ViewBag.Title = "Sửa Thể loại phim";
            var theLoai = movieService.GetGenreById(id);
            if (theLoai == null) return HttpNotFound();
            return View(theLoai);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaTheLoai(THELOAI theLoai)
        {
            if (ModelState.IsValid)
            {
                if (movieService.IsGenreNameExists(theLoai.TEN_THELOAI, theLoai.MA_THELOAI))
                {
                    ModelState.AddModelError("TEN_THELOAI", "Tên thể loại đã tồn tại. Vui lòng nhập tên khác.");
                    return View(theLoai);
                }

                movieService.UpdateGenre(theLoai);
                return RedirectToAction("QuanLyTheLoai");
            }
            return View(theLoai);
        }

        [HttpPost]
        public ActionResult XoaTheLoai(string id)
        {
            try
            {
                movieService.DeleteGenre(id);
                TempData["Success"] = "Xóa thể loại thành công!";
            }
            catch
            {
                TempData["Error"] = "Đã xảy ra lỗi khi xóa hoặc thể loại đang có phim.";
            }
            return RedirectToAction("QuanLyTheLoai");
        }

        #region QUAN LY BAP NUOC (F&B CONCESSIONS)

        public ActionResult QuanLyBapNuoc(string phanLoai, string search)
        {
            ViewBag.CurrentPhanLoai = string.IsNullOrEmpty(phanLoai) ? "TatCa" : phanLoai;
            ViewBag.CurrentSearch = search;
            var list = _fnBService.GetAll(phanLoai, search, onlyActive: false);
            return View(list);
        }

        public ActionResult ThemBapNuoc()
        {
            var model = new BAPNUOC
            {
                PHANLOAI = "Combo",
                TRANGTHAI = "DangBan",
                GIABAN = 79000,
                SOLUONGTON = 100
            };
            ViewBag.AutoCode = _fnBService.GenerateNewCode("Combo");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemBapNuoc(BAPNUOC model, HttpPostedFileBase hinhAnhFile)
        {
            string uploadPath = Server.MapPath("~/Content/HinhAnh/FandB/");
            if (_fnBService.Create(model, hinhAnhFile, uploadPath, out string error))
            {
                TempData["Success"] = "Thêm sản phẩm bắp nước thành công!";
                return RedirectToAction("QuanLyBapNuoc");
            }
            TempData["Error"] = error;
            ViewBag.AutoCode = _fnBService.GenerateNewCode(model.PHANLOAI);
            return View(model);
        }

        public ActionResult SuaBapNuoc(string id)
        {
            var item = _fnBService.GetById(id);
            if (item == null) return HttpNotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaBapNuoc(BAPNUOC model, HttpPostedFileBase hinhAnhFile)
        {
            string uploadPath = Server.MapPath("~/Content/HinhAnh/FandB/");
            if (_fnBService.Update(model, hinhAnhFile, uploadPath, out string error))
            {
                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("QuanLyBapNuoc");
            }
            TempData["Error"] = error;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaBapNuoc(string id)
        {
            if (_fnBService.Delete(id, out string actionMsg, out string error))
            {
                TempData["Success"] = actionMsg;
            }
            else
            {
                TempData["Error"] = error;
            }
            return RedirectToAction("QuanLyBapNuoc");
        }

        [HttpPost]
        public JsonResult CapNhatTrangThaiBapNuoc(string id, string status)
        {
            if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "Mã sản phẩm không hợp lệ." });
            using (var db = new QL_VEPHIMEntities())
            {
                string pId = id.Trim();
                var p = db.BAPNUOCs.FirstOrDefault(b => b.MA_SANPHAM.Trim() == pId);
                if (p == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                p.TRANGTHAI = status == "DangBan" ? "DangBan" : "NgungBan";
                db.SaveChanges();
                return Json(new { success = true, newStatus = p.TRANGTHAI });
            }
        }

        #endregion

        #region QUAN LY NHAN VIEN & PHAN QUYEN

        public ActionResult QuanLyNhanVien(string tuKhoa, string maRap, string vaiTro)
        {
            using (var db = new QL_VEPHIMEntities())
            {
                var query = db.NHANVIENs
                    .Include(n => n.TAIKHOAN)
                    .Include(n => n.TAIKHOAN.VAITROes)
                    .Include(n => n.RAPCHIEU)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(tuKhoa))
                {
                    string tk = tuKhoa.Trim().ToLower();
                    query = query.Where(n => n.MA_NHANVIEN.ToLower().Contains(tk)
                        || n.TAIKHOAN.HOTEN.ToLower().Contains(tk)
                        || n.TAIKHOAN.SDT.Contains(tk)
                        || n.TAIKHOAN.EMAIL.ToLower().Contains(tk));
                }

                if (!string.IsNullOrWhiteSpace(maRap))
                {
                    string r = maRap.Trim();
                    query = query.Where(n => n.MA_RAP.Trim() == r);
                }

                if (!string.IsNullOrWhiteSpace(vaiTro))
                {
                    string vt = vaiTro.Trim();
                    query = query.Where(n => n.TAIKHOAN.VAITROes.Any(v => v.MA_VAITRO.Trim() == vt || v.TEN_VAITRO.Trim() == vt));
                }

                ViewBag.TuKhoa = tuKhoa;
                ViewBag.MaRap = maRap;
                ViewBag.VaiTro = vaiTro;
                ViewBag.RapList = db.RAPCHIEUx.OrderBy(r => r.TEN_RAP).ToList();

                return View(query.OrderByDescending(n => n.TAIKHOAN.NGAYTAO).ToList());
            }
        }

        public ActionResult ThemNhanVien()
        {
            using (var db = new QL_VEPHIMEntities())
            {
                ViewBag.RapList = db.RAPCHIEUx.OrderBy(r => r.TEN_RAP).ToList();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemNhanVien(string hoTen, string sdt, string email, string matKhau, string maRap, string vaiTro, decimal? luong)
        {
            using (var db = new QL_VEPHIMEntities())
            {
                string sdtTrim = sdt?.Trim() ?? "";
                if (db.TAIKHOANs.Any(t => t.SDT.Trim() == sdtTrim))
                {
                    TempData["Error"] = "Số điện thoại này đã được sử dụng cho một tài khoản khác trong hệ thống.";
                    ViewBag.RapList = db.RAPCHIEUx.OrderBy(r => r.TEN_RAP).ToList();
                    return View();
                }

                var allNvIds = db.NHANVIENs.Select(n => n.MA_NHANVIEN).ToList();
                int maxId = 0;
                foreach (var id in allNvIds)
                {
                    if (!string.IsNullOrEmpty(id) && id.Trim().StartsWith("NV") && int.TryParse(id.Trim().Substring(2), out int cId))
                    {
                        if (cId > maxId) maxId = cId;
                    }
                }
                string newMaNv = "NV" + (maxId + 1).ToString("D3");

                string rawPass = string.IsNullOrWhiteSpace(matKhau) ? "123456" : matKhau.Trim();
                string hashedPass = PasswordHelper.HashPassword(rawPass);

                var tk = new TAIKHOAN
                {
                    MA_TAIKHOAN = newMaNv,
                    HOTEN = hoTen?.Trim() ?? "Nhân viên mới",
                    SDT = sdtTrim,
                    EMAIL = email?.Trim(),
                    MATKHAU = hashedPass,
                    NGAYTAO = DateTime.Now,
                    TRANGTHAI = "HoatDong"
                };

                string targetRole = string.IsNullOrWhiteSpace(vaiTro) ? "NHANVIEN" : vaiTro.Trim().ToUpperInvariant();
                var roleObj = db.VAITROes.FirstOrDefault(v => v.MA_VAITRO.Trim() == targetRole || v.TEN_VAITRO.Trim() == targetRole);
                if (roleObj == null)
                {
                    roleObj = new VAITRO { MA_VAITRO = targetRole, TEN_VAITRO = targetRole };
                    db.VAITROes.Add(roleObj);
                }
                tk.VAITROes.Add(roleObj);
                db.TAIKHOANs.Add(tk);

                var nv = new NHANVIEN
                {
                    MA_NHANVIEN = newMaNv,
                    LUONG = luong ?? 6500000m,
                    MA_RAP = string.IsNullOrWhiteSpace(maRap) ? db.RAPCHIEUx.Select(r => r.MA_RAP).FirstOrDefault() : maRap.Trim(),
                    NGAYVAOLAM = DateTime.Now
                };
                db.NHANVIENs.Add(nv);

                db.SaveChanges();
                TempData["Success"] = $"Thêm nhân viên [{newMaNv} - {tk.HOTEN}] thành công!";
                return RedirectToAction("QuanLyNhanVien");
            }
        }

        public ActionResult SuaNhanVien(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();
            using (var db = new QL_VEPHIMEntities())
            {
                string trimmed = id.Trim();
                var nv = db.NHANVIENs
                    .Include(n => n.TAIKHOAN.VAITROes)
                    .Include(n => n.RAPCHIEU)
                    .FirstOrDefault(n => n.MA_NHANVIEN.Trim() == trimmed);

                if (nv == null) return HttpNotFound();

                ViewBag.RapList = db.RAPCHIEUx.OrderBy(r => r.TEN_RAP).ToList();
                return View(nv);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaNhanVien(string id, string hoTen, string sdt, string email, string matKhauMoi, string maRap, string vaiTro, decimal? luong)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();
            using (var db = new QL_VEPHIMEntities())
            {
                string trimmed = id.Trim();
                var nv = db.NHANVIENs
                    .Include(n => n.TAIKHOAN.VAITROes)
                    .FirstOrDefault(n => n.MA_NHANVIEN.Trim() == trimmed);

                if (nv == null) return HttpNotFound();

                if (nv.TAIKHOAN != null)
                {
                    nv.TAIKHOAN.HOTEN = hoTen?.Trim() ?? nv.TAIKHOAN.HOTEN;
                    nv.TAIKHOAN.SDT = sdt?.Trim() ?? nv.TAIKHOAN.SDT;
                    nv.TAIKHOAN.EMAIL = email?.Trim();

                    if (!string.IsNullOrWhiteSpace(matKhauMoi))
                    {
                        nv.TAIKHOAN.MATKHAU = PasswordHelper.HashPassword(matKhauMoi.Trim());
                    }

                    if (!string.IsNullOrWhiteSpace(vaiTro))
                    {
                        string targetRole = vaiTro.Trim().ToUpperInvariant();
                        var roleObj = db.VAITROes.FirstOrDefault(v => v.MA_VAITRO.Trim() == targetRole || v.TEN_VAITRO.Trim() == targetRole);
                        if (roleObj == null)
                        {
                            roleObj = new VAITRO { MA_VAITRO = targetRole, TEN_VAITRO = targetRole };
                            db.VAITROes.Add(roleObj);
                        }
                        nv.TAIKHOAN.VAITROes.Clear();
                        nv.TAIKHOAN.VAITROes.Add(roleObj);
                    }
                }

                nv.LUONG = luong ?? nv.LUONG;
                if (!string.IsNullOrWhiteSpace(maRap))
                {
                    nv.MA_RAP = maRap.Trim();
                }

                db.SaveChanges();
                TempData["Success"] = "Cập nhật hồ sơ nhân viên thành công!";
                return RedirectToAction("QuanLyNhanVien");
            }
        }

        [HttpPost]
        public JsonResult DoiTrangThaiNhanVien(string id)
        {
            if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "Mã nhân viên không hợp lệ." });
            using (var db = new QL_VEPHIMEntities())
            {
                string trimmed = id.Trim();
                var nv = db.NHANVIENs.Include(n => n.TAIKHOAN).FirstOrDefault(n => n.MA_NHANVIEN.Trim() == trimmed);
                if (nv == null || nv.TAIKHOAN == null) return Json(new { success = false, message = "Không tìm thấy hồ sơ nhân viên." });

                string currentStatus = nv.TAIKHOAN.TRANGTHAI?.Trim();
                string newStatus = (currentStatus == "HoatDong") ? "Khoa" : "HoatDong";
                nv.TAIKHOAN.TRANGTHAI = newStatus;
                db.SaveChanges();

                return Json(new { success = true, newStatus = newStatus, message = $"Đã {(newStatus == "HoatDong" ? "kích hoạt" : "khóa")} tài khoản [{trimmed}]." });
            }
        }

        public ActionResult ChiTietNhanVien(string id)
        {
            if (string.IsNullOrEmpty(id)) return HttpNotFound();
            using (var db = new QL_VEPHIMEntities())
            {
                string trimmed = id.Trim();
                var nv = db.NHANVIENs
                    .Include(n => n.TAIKHOAN)
                    .Include(n => n.TAIKHOAN.VAITROes)
                    .Include(n => n.RAPCHIEU)
                    .FirstOrDefault(n => n.MA_NHANVIEN.Trim() == trimmed);

                if (nv == null) return HttpNotFound();

                var donQuay = db.DONHANGs
                    .Include(d => d.LICHCHIEU.PHIM)
                    .Include(d => d.LICHCHIEU.KHUNGGIO)
                    .Include(d => d.VEs)
                    .Where(d => d.MA_NHANVIEN.Trim() == trimmed)
                    .OrderByDescending(d => d.NGAYDAT)
                    .Take(50)
                    .ToList();

                var donSoatVe = db.DONHANGs
                    .Include(d => d.LICHCHIEU.PHIM)
                    .Include(d => d.LICHCHIEU.KHUNGGIO)
                    .Include(d => d.VEs)
                    .Where(d => d.MANV_SOATVE.Trim() == trimmed)
                    .OrderByDescending(d => d.THOIGIANCHECKIN)
                    .Take(50)
                    .ToList();

                ViewBag.DonQuay = donQuay;
                ViewBag.DonSoatVe = donSoatVe;
                ViewBag.TongDoanhThu = donQuay.Where(d => d.TRANGTHAI == "DaThanhToan").Select(d => (decimal?)d.TONGTIEN).Sum() ?? 0m;
                ViewBag.TongVeDaSoat = donSoatVe.SelectMany(d => d.VEs).Count();

                return View(nv);
            }
        }

        #endregion

        #region QUAN LY DANH GIA PHIM

        public ActionResult QuanLyDanhGia(string maPhim, string trangThai)
        {
            using (var db = new QL_VEPHIMEntities())
            {
                var query = db.DANHGIAs
                    .Include(d => d.PHIM)
                    .Include(d => d.KHACHHANG.TAIKHOAN)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(maPhim))
                {
                    string pId = maPhim.Trim();
                    query = query.Where(d => d.MA_PHIM.Trim() == pId);
                }

                if (!string.IsNullOrEmpty(trangThai))
                {
                    string st = trangThai.Trim();
                    query = query.Where(d => d.TRANGTHAI == st);
                }

                ViewBag.PhimList = db.PHIMs.OrderBy(p => p.TEN_PHIM).ToList();
                ViewBag.SelectedPhim = maPhim;
                ViewBag.SelectedTrangThai = trangThai;

                return View(query.OrderByDescending(d => d.NGAYDANHGIA).ToList());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiTrangThaiDanhGia(string maDanhGia)
        {
            if (string.IsNullOrEmpty(maDanhGia))
            {
                return RedirectToAction("QuanLyDanhGia");
            }

            using (var db = new QL_VEPHIMEntities())
            {
                string id = maDanhGia.Trim();
                var dg = db.DANHGIAs.FirstOrDefault(d => d.MA_DANHGIA.Trim() == id);
                if (dg != null)
                {
                    dg.TRANGTHAI = dg.TRANGTHAI == "HienThi" ? "An" : "HienThi";
                    db.SaveChanges();
                    TempData["Success"] = "Đã cập nhật trạng thái hiển thị đánh giá thành công!";
                }
            }

            return RedirectToAction("QuanLyDanhGia");
        }

        #endregion

    }
}