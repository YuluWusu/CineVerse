using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;
using DoAn_QuanLyVeXemPhimTrucTiep.Services;
using System.IO;
using DoAn_QuanLyVeXemPhimTrucTiep.Filters;

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

        // GET: Admin/Index (Dashboard)
        public ActionResult Index()
        {
            ViewBag.TotalRevenue = reportService.GetTotalRevenue();
            ViewBag.TotalTickets = reportService.GetTotalTickets();
            ViewBag.TotalMovies = reportService.GetTotalMovies();
            ViewBag.TotalCustomers = reportService.GetTotalCustomers();
            ViewBag.RecentTickets = reportService.GetRecentTickets(5);

            return View();
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
    }
}