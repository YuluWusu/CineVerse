using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class BookingService
    {
        private QL_VEPHIMEntities db;

        public BookingService()
        {
            this.db = new QL_VEPHIMEntities();
        }

        public BookingService(QL_VEPHIMEntities dbContext)
        {
            this.db = dbContext;
        }

        public KHACHHANG GetOrCreateCustomer(string hoTen, string soDienThoai, string email)
        {
            string sdt = soDienThoai?.Trim() ?? "";
            var khachHang = db.KHACHHANGs
                .Include(k => k.TAIKHOAN)
                .Include(k => k.TAIKHOAN.VAITROes)
                .FirstOrDefault(k => k.TAIKHOAN != null && k.TAIKHOAN.SDT.Trim() == sdt);

            if (khachHang == null)
            {
                var allKhIds = db.KHACHHANGs.Select(k => k.MA_KHACHHANG).ToList();
                int maxKhId = 0;
                foreach (var id in allKhIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        string trimmed = id.Trim();
                        if (trimmed.StartsWith("KH"))
                        {
                            if (int.TryParse(trimmed.Substring(2), out int currentId))
                            {
                                if (currentId > maxKhId) maxKhId = currentId;
                            }
                        }
                    }
                }
                string newMaKhach = "KH" + (maxKhId + 1).ToString("D3");

                var tk = new TAIKHOAN
                {
                    MA_TAIKHOAN = newMaKhach,
                    HOTEN = string.IsNullOrWhiteSpace(hoTen) ? "Khách vãng lai" : hoTen.Trim(),
                    SDT = sdt,
                    EMAIL = string.IsNullOrWhiteSpace(email) ? (sdt + "@cineverse.vn") : email.Trim(),
                    MATKHAU = "123456",
                    NGAYTAO = DateTime.Now,
                    TRANGTHAI = "HoatDong"
                };

                var vaiTro = db.VAITROes.FirstOrDefault(v => v.MA_VAITRO.Trim() == "KhachHang" || v.TEN_VAITRO.Trim() == "KhachHang");
                if (vaiTro != null)
                {
                    tk.VAITROes.Add(vaiTro);
                }

                db.TAIKHOANs.Add(tk);

                khachHang = new KHACHHANG
                {
                    MA_KHACHHANG = newMaKhach,
                    TAIKHOAN = tk
                };
                db.KHACHHANGs.Add(khachHang);
                db.SaveChanges();
            }
            return khachHang;
        }

        public KHACHHANG GetCustomerByPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return null;
            string trimmed = phone.Trim();
            return db.KHACHHANGs
                .Include(k => k.TAIKHOAN)
                .Include(k => k.TAIKHOAN.VAITROes)
                .FirstOrDefault(k => k.TAIKHOAN != null && k.TAIKHOAN.SDT.Trim() == trimmed);
        }

        public KHACHHANG GetCustomerById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string trimmed = id.Trim();
            return db.KHACHHANGs
                .Include(k => k.TAIKHOAN)
                .Include(k => k.TAIKHOAN.VAITROes)
                .FirstOrDefault(k => k.MA_KHACHHANG.Trim() == trimmed);
        }

        public DONHANG GetOrderById(string maDonHang)
        {
            if (string.IsNullOrEmpty(maDonHang)) return null;
            string trimmed = maDonHang.Trim();
            return db.DONHANGs
                .Include(d => d.KHACHHANG)
                .Include(d => d.KHACHHANG.TAIKHOAN)
                .Include(d => d.LICHCHIEU.PHIM)
                .Include(d => d.LICHCHIEU.KHUNGGIO)
                .Include(d => d.LICHCHIEU.PHONGCHIEU.RAPCHIEU)
                .Include(d => d.VEs.Select(v => v.GHE.LOAIGHE))
                .Include(d => d.CHITIET_BAPNUOC.Select(c => c.BAPNUOC))
                .FirstOrDefault(d => d.MA_DONHANG.Trim() == trimmed);
        }

        public bool UpdatePaymentStatus(string maDonHang, string trangThaiThanhToan, string maGiaoDich = null, string phuongThuc = null)
        {
            if (string.IsNullOrEmpty(maDonHang)) return false;
            string trimmed = maDonHang.Trim();
            var order = db.DONHANGs.FirstOrDefault(d => d.MA_DONHANG.Trim() == trimmed);
            if (order == null) return false;

            order.TRANGTHAITHANHTOAN = trangThaiThanhToan;
            if (trangThaiThanhToan == "DaThanhToan")
            {
                order.TRANGTHAI = "DaThanhToan";
                var ves = db.VEs.Where(v => v.MA_DONHANG.Trim() == trimmed).ToList();
                foreach (var v in ves)
                {
                    v.TRANGTHAI = "Đã thanh toán";
                }
                if (!string.IsNullOrEmpty(order.MA_KHACHHANG))
                {
                    LoyaltyService.Instance.CongDiemVaXetHang(order.MA_KHACHHANG, order.TONGTIEN, db);
                }
            }
            else if (trangThaiThanhToan == "DaHuy")
            {
                order.TRANGTHAI = "DaHuy";
            }

            if (!string.IsNullOrEmpty(maGiaoDich))
            {
                order.MAGIAODICH = maGiaoDich;
            }
            if (!string.IsNullOrEmpty(phuongThuc))
            {
                order.PHUONGTHUCTHANHTOAN = phuongThuc;
            }

            db.SaveChanges();
            return true;
        }

        public List<VE> GetBookingHistory(string maKhach)
        {
            string trimmed = maKhach?.Trim() ?? "";
            return db.VEs
                .Include(v => v.DONHANG)
                .Include(v => v.DONHANG.CHITIET_BAPNUOC)
                .Include(v => v.DONHANG.CHITIET_BAPNUOC.Select(c => c.BAPNUOC))
                .Include(v => v.LICHCHIEU.PHIM)
                .Include(v => v.LICHCHIEU.KHUNGGIO)
                .Include(v => v.LICHCHIEU.PHONGCHIEU.RAPCHIEU)
                .Include(v => v.GHE.LOAIGHE)
                .Where(v => v.DONHANG != null && v.DONHANG.MA_KHACHHANG.Trim() == trimmed)
                .OrderByDescending(v => v.DONHANG.NGAYDAT)
                .ToList();
        }

        public KHUYENMAI ValidatePromotion(string maKmId)
        {
            if (string.IsNullOrEmpty(maKmId)) return null;

            string trimmed = maKmId.Trim();
            var khuyenMai = db.KHUYENMAIs.FirstOrDefault(k =>
                k.MA_KM.Trim() == trimmed &&
                k.TRANGTHAI == "HoatDong" &&
                k.NGAYBATDAU <= DateTime.Now &&
                k.NGAYKETTHUC >= DateTime.Now);

            if (khuyenMai != null &&
                khuyenMai.SO_LUOT_TOIDA.HasValue &&
                khuyenMai.SO_LUOT_DADUNG >= khuyenMai.SO_LUOT_TOIDA.Value)
            {
                return null;
            }

            return khuyenMai;
        }

        public Tuple<decimal, decimal, decimal> CalculatePrice(LICHCHIEU lichChieu, List<GHE> gheList, KHUYENMAI khuyenMai)
        {
            decimal tongTienGoc = 0;
            foreach (var ghe in gheList)
            {
                tongTienGoc += (ghe.LOAIGHE?.GIATVE ?? 0m) + (lichChieu.GIATVE_PHUTROI ?? 0);
            }

            decimal soTienGiam = 0;
            if (khuyenMai != null)
            {
                soTienGiam = tongTienGoc * khuyenMai.GIAMGIA_PHANTRAM / 100m;
                if (khuyenMai.GIAMGIA_TOIDA.HasValue && soTienGiam > khuyenMai.GIAMGIA_TOIDA.Value)
                    soTienGiam = khuyenMai.GIAMGIA_TOIDA.Value;
            }

            decimal tongTienSauGiam = tongTienGoc - soTienGiam;
            return new Tuple<decimal, decimal, decimal>(tongTienGoc, soTienGiam, tongTienSauGiam);
        }

        /// <summary>
        /// Xử lý lưu đơn hàng, vé và chi tiết bắp nước với Database Transaction Serializable
        /// chống hoàn toàn việc đặt trùng ghế (Concurrency Control), hỗ trợ thanh toán online và sinh mã Check-in.
        /// </summary>
        public DONHANG ProcessBooking(
            KHACHHANG khachHang,
            LICHCHIEU lichChieu,
            List<GHE> gheList,
            KHUYENMAI khuyenMai,
            decimal soTienGiam,
            Dictionary<string, int> fnbItems = null,
            string holderId = null,
            string phuongThucThanhToan = "TaiQuay")
        {
            if (khachHang == null || lichChieu == null || gheList == null || !gheList.Any())
            {
                throw new ArgumentException("Thông tin đơn hàng không hợp lệ.");
            }

            string maKhach = khachHang.MA_KHACHHANG.Trim();
            string maLichChieu = lichChieu.MA_LICHCHIEU.Trim();

            // Chốt chặn cuối cùng ở Database bằng DbContextTransaction Serializable
            using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    // 1. Kiểm tra tức thì trong Transaction xem có ghế nào đã có vé bán chưa
                    var seatIds = gheList.Select(g => g.MA_GHE.Trim()).ToList();
                    var bookedSeats = db.VEs
                        .Where(v => v.MA_LICHCHIEU.Trim() == maLichChieu && seatIds.Contains(v.MA_GHE.Trim()))
                        .Select(v => v.GHE != null ? v.GHE.SOGHE.Trim() : v.MA_GHE.Trim())
                        .ToList();

                    if (bookedSeats.Any())
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException($"Rất tiếc! Ghế [{string.Join(", ", bookedSeats)}] vừa được giao dịch khác hoàn tất thanh toán trước bạn một tích tắc. Đơn hàng đã bị hủy để tránh đặt trùng.");
                    }

                    // 2. Tạo mã đơn hàng mới
                    var allDhIds = db.DONHANGs.Select(d => d.MA_DONHANG).ToList();
                    int maxDhId = 0;
                    foreach (var id in allDhIds)
                    {
                        if (!string.IsNullOrEmpty(id))
                        {
                            string trimmed = id.Trim();
                            if (trimmed.StartsWith("DH") && int.TryParse(trimmed.Substring(2), out int currentId))
                            {
                                if (currentId > maxDhId) maxDhId = currentId;
                            }
                        }
                    }
                    string maDonHang = "DH" + (maxDhId + 1).ToString("D3");

                    decimal tongTienVe = gheList.Sum(g => (g.LOAIGHE?.GIATVE ?? 0m) + (lichChieu.GIATVE_PHUTROI ?? 0m));
                    decimal tienMonAn = 0m;
                    var chiTietBapNuocList = new List<CHITIET_BAPNUOC>();

                    // 3. Xử lý bắp nước & combo
                    if (fnbItems != null && fnbItems.Any())
                    {
                        var bapNuocList = db.BAPNUOCs.ToList();

                        var allCtIds = db.CHITIET_BAPNUOC.Select(c => c.MA_CHITIET).ToList();
                        int maxCtId = 0;
                        foreach (var id in allCtIds)
                        {
                            if (!string.IsNullOrEmpty(id))
                            {
                                string trimmed = id.Trim();
                                if (trimmed.StartsWith("CTB") && int.TryParse(trimmed.Substring(3), out int curId))
                                {
                                    if (curId > maxCtId) maxCtId = curId;
                                }
                            }
                        }

                        foreach (var pair in fnbItems)
                        {
                            string maSp = pair.Key?.Trim() ?? "";
                            int soLuong = pair.Value;
                            if (soLuong <= 0) continue;

                            var sp = bapNuocList.FirstOrDefault(b => b.MA_SANPHAM.Trim().Equals(maSp, StringComparison.OrdinalIgnoreCase));
                            if (sp != null)
                            {
                                decimal thanhTien = sp.GIABAN * soLuong;
                                tienMonAn += thanhTien;
                                maxCtId++;
                                string maCt = "CTB" + maxCtId.ToString("D7");

                                var chiTiet = new CHITIET_BAPNUOC
                                {
                                    MA_CHITIET = maCt,
                                    MA_DONHANG = maDonHang,
                                    MA_SANPHAM = sp.MA_SANPHAM.Trim(),
                                    SOLUONG = soLuong,
                                    DONGIA = sp.GIABAN,
                                    THANHTIEN = thanhTien
                                };
                                chiTietBapNuocList.Add(chiTiet);

                                if (sp.SOLUONGTON >= soLuong)
                                {
                                    sp.SOLUONGTON -= soLuong;
                                }
                                else
                                {
                                    sp.SOLUONGTON = 0;
                                }
                            }
                        }
                    }

                    decimal tongTienSauGiam = (tongTienVe + tienMonAn) - soTienGiam;
                    if (tongTienSauGiam < 0) tongTienSauGiam = 0;

                    // Sinh mã Check-in duy nhất dùng cho E-Ticket QR
                    string checkInCode = "CKI-" + maDonHang + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                    bool isOnlinePayment = phuongThucThanhToan == "VNPay" || phuongThucThanhToan == "VietQR";
                    string trangThaiDonHang = isOnlinePayment ? "ChoThanhToan" : "DaThanhToan";
                    string trangThaiThanhToan = isOnlinePayment ? "ChoThanhToan" : "DaThanhToan";

                    // 4. Tạo DONHANG
                    DONHANG donHang = new DONHANG
                    {
                        MA_DONHANG = maDonHang,
                        MA_KHACHHANG = maKhach,
                        MA_LICHCHIEU = maLichChieu,
                        MA_KM = !string.IsNullOrEmpty(khuyenMai?.MA_KM) ? khuyenMai.MA_KM.Trim() : null,
                        NGAYDAT = DateTime.Now,
                        TIENVE = tongTienVe,
                        TIENMONAN = tienMonAn,
                        TIENGIAM = soTienGiam,
                        TONGTIEN = tongTienSauGiam,
                        TRANGTHAI = trangThaiDonHang,
                        PHUONGTHUCTHANHTOAN = phuongThucThanhToan,
                        TRANGTHAITHANHTOAN = trangThaiThanhToan,
                        MACHECKIN = checkInCode,
                        TRANGTHAISOATVE = "ChuaSoat"
                    };
                    db.DONHANGs.Add(donHang);

                    foreach (var ct in chiTietBapNuocList)
                    {
                        db.CHITIET_BAPNUOC.Add(ct);
                    }

                    // 5. Tạo các VE
                    var allVeIds = db.VEs.Select(v => v.MA_VE).ToList();
                    int maxVeId = 0;
                    foreach (var id in allVeIds)
                    {
                        if (!string.IsNullOrEmpty(id))
                        {
                            string trimmed = id.Trim();
                            if (trimmed.StartsWith("VE"))
                            {
                                if (int.TryParse(trimmed.Substring(2), out int currentId))
                                {
                                    if (currentId > maxVeId) maxVeId = currentId;
                                }
                            }
                        }
                    }

                    foreach (var ghe in gheList)
                    {
                        decimal giaGhe = (ghe.LOAIGHE?.GIATVE ?? 0m) + (lichChieu.GIATVE_PHUTROI ?? 0m);
                        maxVeId++;
                        string maVeMoi = "VE" + maxVeId.ToString("D3");

                        VE veMoi = new VE
                        {
                            MA_VE = maVeMoi,
                            MA_DONHANG = maDonHang,
                            MA_LICHCHIEU = maLichChieu,
                            MA_GHE = ghe.MA_GHE.Trim(),
                            DONGIA = giaGhe,
                            TRANGTHAI = isOnlinePayment ? "Chờ thanh toán" : "Đã thanh toán"
                        };
                        db.VEs.Add(veMoi);
                    }

                    // 6. Cập nhật khuyến mãi
                    if (khuyenMai != null && !string.IsNullOrEmpty(khuyenMai.MA_KM))
                    {
                        string kmId = khuyenMai.MA_KM.Trim();
                        var km = db.KHUYENMAIs.FirstOrDefault(k => k.MA_KM.Trim() == kmId);
                        if (km != null)
                        {
                            km.SO_LUOT_DADUNG = (km.SO_LUOT_DADUNG) + 1;
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    // Nếu thanh toán tại quầy thành công, giải phóng ghế khỏi SeatHoldService và tích điểm loyalty
                    if (!isOnlinePayment)
                    {
                        string hId = !string.IsNullOrEmpty(holderId) ? holderId : maKhach;
                        SeatHoldService.Instance.ReleaseSeats(maLichChieu, hId);
                        LoyaltyService.Instance.CongDiemVaXetHang(maKhach, tongTienSauGiam, db);
                    }

                    return donHang;
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    transaction.Rollback();
                    var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.PropertyName + ": " + x.ErrorMessage);
                    throw new Exception("Lỗi xác thực dữ liệu: " + string.Join("; ", errorMessages), ex);
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                {
                    transaction.Rollback();
                    var inner = ex.InnerException?.InnerException ?? ex.InnerException ?? ex;
                    throw new Exception("Lỗi lưu cơ sở dữ liệu: " + inner.Message, ex);
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public List<KHUYENMAI> GetActivePromotions()
        {
            var now = DateTime.Now;

            return db.KHUYENMAIs.Where(km => km.TRANGTHAI == "HoatDong"
                       && km.NGAYBATDAU <= now
                       && km.NGAYKETTHUC >= now
                       && (km.SO_LUOT_TOIDA == null || km.SO_LUOT_DADUNG < km.SO_LUOT_TOIDA)).ToList();
        }
    }
}
