using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class LoiNghiepVuException : Exception
    {
        public LoiNghiepVuException(string message) : base(message)
        {
        }
    }

    public class CheckInKetQua
    {
        public bool ThanhCong { get; set; }
        public string ThongBao { get; set; }
    }

    public class ThongKeSuat
    {
        public string MaLichChieu { get; set; }
        public int DaBan { get; set; }
        public int DaCheckIn { get; set; }
    }

    public class ThongKeNhanVien
    {
        public int VeChoCheckIn { get; set; }
        public int VeDaCheckIn { get; set; }
        public int DonDaBan { get; set; }
        public decimal DoanhThuQuay { get; set; }
    }

    
    public class CheckInScanResult
    {
        public bool HopLe { get; set; }
        public string LoaiKetQua { get; set; }
        public string ThongBao { get; set; }
        public string MaDonHang { get; set; }
        public string MaCheckIn { get; set; }
        public string TenPhim { get; set; }
        public string Poster { get; set; }
        public string TenRap { get; set; }
        public string TenPhong { get; set; }
        public string GioChieu { get; set; }
        public string NgayChieu { get; set; }
        public List<string> DanhSachGhe { get; set; } = new List<string>();
        public decimal TongTien { get; set; }
        public string TenKhachHang { get; set; }
        public string SdtKhachHang { get; set; }
        public bool DaSoatVe { get; set; }
        public DateTime? ThoiGianCheckIn { get; set; }
        public string NhanVienSoatVe { get; set; }
        public List<CheckInBapNuocItem> DanhSachBapNuoc { get; set; } = new List<CheckInBapNuocItem>();
        public string TrangThaiNhanBapNuoc { get; set; }
    }

    public class CheckInBapNuocItem
    {
        public string TenSanPham { get; set; }
        public int SoLuong { get; set; }
        public string PhanLoai { get; set; }
    }

    public class PosSeatItem
    {
        public string MaGhe { get; set; }
        public string SoGhe { get; set; }
        public string HangGhe { get; set; }
        public int CotGhe { get; set; }
        public string MaLoaiGhe { get; set; }
        public string TenLoaiGhe { get; set; }
        public decimal GiaGhe { get; set; }
        public bool DaDat { get; set; }
    }

    public class StaffService
    {
        public const string VeDaThanhToan = "Đã thanh toán";
        public const string VeDaCheckIn = "Đã check-in";
        public const string VeDaHuy = "Đã hủy";

        public static readonly TimeSpan CheckInSom = TimeSpan.FromMinutes(60);
        public static readonly TimeSpan BanVeTre = TimeSpan.FromMinutes(15);

        private QL_VEPHIMEntities db;

        public StaffService()
        {
            this.db = new QL_VEPHIMEntities();
        }

        public StaffService(QL_VEPHIMEntities dbContext)
        {
            this.db = dbContext;
        }

        // ========================== THỜI GIAN SUẤT CHIẾU ==========================
        public static DateTime GioBatDau(LICHCHIEU lichChieu)
        {
            return lichChieu.NGAYCHIEU.Date + lichChieu.KHUNGGIO.GIOBATDAU;
        }

        public static DateTime GioKetThuc(LICHCHIEU lichChieu)
        {
            DateTime batDau = GioBatDau(lichChieu);
            DateTime ketThuc = lichChieu.NGAYCHIEU.Date + lichChieu.KHUNGGIO.GIOKETTHUC;
            return ketThuc <= batDau ? ketThuc.AddDays(1) : ketThuc;
        }

        public static string GioChieu(LICHCHIEU lichChieu)
        {
            TimeSpan bd = lichChieu.KHUNGGIO.GIOBATDAU;
            TimeSpan kt = lichChieu.KHUNGGIO.GIOKETTHUC;
            return string.Format("{0:00}:{1:00} - {2:00}:{3:00}", bd.Hours, bd.Minutes, kt.Hours, kt.Minutes);
        }

        public static string TrangThaiSuat(LICHCHIEU lichChieu)
        {
            DateTime bayGio = DateTime.Now;
            if (bayGio >= GioKetThuc(lichChieu)) return "Đã kết thúc";
            if (bayGio >= GioBatDau(lichChieu)) return "Đang chiếu";
            return "Sắp chiếu";
        }

        public static bool ConBanDuoc(LICHCHIEU lichChieu)
        {
            return GioBatDau(lichChieu).Add(BanVeTre) > DateTime.Now;
        }

        // ========================== NHÂN VIÊN ==========================
        public NHANVIEN GetStaff(string maNhanVien)
        {
            if (string.IsNullOrEmpty(maNhanVien)) return null;
            string ma = maNhanVien.Trim();
            return db.NHANVIENs.Include(n => n.TAIKHOAN).FirstOrDefault(n => n.MA_NHANVIEN.Trim() == ma);
        }

        // ========================== SUẤT CHIẾU ==========================
        public List<LICHCHIEU> GetSchedules(string maRap, int soNgay, bool chiConBanDuoc)
        {
            DateTime homNay = DateTime.Today;
            DateTime den = homNay.AddDays(soNgay);

            var query = db.LICHCHIEUx
                .Include(l => l.PHIM)
                .Include(l => l.KHUNGGIO)
                .Include(l => l.PHONGCHIEU.RAPCHIEU)
                .Where(l => l.NGAYCHIEU >= homNay && l.NGAYCHIEU < den);

            if (!string.IsNullOrEmpty(maRap))
            {
                string rap = maRap.Trim();
                query = query.Where(l => l.PHONGCHIEU.MA_RAP.Trim() == rap);
            }

            List<LICHCHIEU> danhSach = query.ToList();

            if (chiConBanDuoc)
            {
                danhSach = danhSach.Where(l => ConBanDuoc(l)).ToList();
            }

            return danhSach.OrderBy(l => GioBatDau(l)).ToList();
        }

        public LICHCHIEU GetSchedule(string maLichChieu)
        {
            if (string.IsNullOrEmpty(maLichChieu)) return null;
            string ma = maLichChieu.Trim();
            return db.LICHCHIEUx
                .Include(l => l.PHIM)
                .Include(l => l.KHUNGGIO)
                .Include(l => l.PHONGCHIEU.RAPCHIEU)
                .FirstOrDefault(l => l.MA_LICHCHIEU.Trim() == ma);
        }

        public Dictionary<string, ThongKeSuat> GetThongKeSuat(List<LICHCHIEU> lichChieu)
        {
            var ketQua = new Dictionary<string, ThongKeSuat>();
            foreach (var l in lichChieu)
            {
                string ma = l.MA_LICHCHIEU.Trim();
                ketQua[ma] = new ThongKeSuat { MaLichChieu = ma };
            }

            var danhSachMa = lichChieu.Select(l => l.MA_LICHCHIEU).ToList();
            var veList = db.VEs
                .Where(v => danhSachMa.Contains(v.MA_LICHCHIEU) && v.TRANGTHAI != VeDaHuy)
                .Select(v => new { v.MA_LICHCHIEU, v.TRANGTHAI })
                .ToList();

            foreach (var ve in veList)
            {
                ThongKeSuat thongKe;
                if (ketQua.TryGetValue(ve.MA_LICHCHIEU.Trim(), out thongKe))
                {
                    thongKe.DaBan++;
                    if (ve.TRANGTHAI == VeDaCheckIn) thongKe.DaCheckIn++;
                }
            }

            return ketQua;
        }

        public ThongKeNhanVien GetThongKe(string maNhanVien, string maRap)
        {
            DateTime homNay = DateTime.Today;
            DateTime ngayMai = homNay.AddDays(1);

            var veHomNay = db.VEs.Where(v => v.LICHCHIEU.NGAYCHIEU >= homNay && v.LICHCHIEU.NGAYCHIEU < ngayMai);
            if (!string.IsNullOrEmpty(maRap))
            {
                string rap = maRap.Trim();
                veHomNay = veHomNay.Where(v => v.LICHCHIEU.PHONGCHIEU.MA_RAP.Trim() == rap);
            }

            var thongKe = new ThongKeNhanVien();
            thongKe.VeChoCheckIn = veHomNay.Count(v => v.TRANGTHAI == VeDaThanhToan);
            thongKe.VeDaCheckIn = veHomNay.Count(v => v.TRANGTHAI == VeDaCheckIn);

            var donQuay = db.DONHANGs.Where(d => d.MA_NHANVIEN != null
                                                 && d.NGAYDAT >= homNay && d.NGAYDAT < ngayMai
                                                 && d.TRANGTHAI == "DaThanhToan");
            if (!string.IsNullOrEmpty(maNhanVien))
            {
                string ma = maNhanVien.Trim();
                donQuay = donQuay.Where(d => d.MA_NHANVIEN.Trim() == ma);
            }

            thongKe.DonDaBan = donQuay.Count();
            thongKe.DoanhThuQuay = donQuay.Select(d => (decimal?)d.TONGTIEN).Sum() ?? 0;
            return thongKe;
        }

        // ========================== CHECK-IN ==========================
        public List<VE> TimVe(string tuKhoa)
        {
            if (string.IsNullOrWhiteSpace(tuKhoa)) return new List<VE>();
            string k = tuKhoa.Trim();

            return db.VEs
                .Include(v => v.DONHANG.KHACHHANG.TAIKHOAN)
                .Include(v => v.LICHCHIEU.PHIM)
                .Include(v => v.LICHCHIEU.KHUNGGIO)
                .Include(v => v.LICHCHIEU.PHONGCHIEU.RAPCHIEU)
                .Include(v => v.GHE.LOAIGHE)
                .Where(v => v.MA_VE.Trim() == k
                            || v.MA_DONHANG.Trim() == k
                            || (v.DONHANG.KHACHHANG != null && v.DONHANG.KHACHHANG.TAIKHOAN.SDT.Trim() == k))
                .OrderByDescending(v => v.LICHCHIEU.NGAYCHIEU)
                .ThenBy(v => v.MA_VE)
                .Take(50)
                .ToList();
        }

        public CheckInKetQua CheckIn(string maVe, string maRapNhanVien)
        {
            if (string.IsNullOrWhiteSpace(maVe))
            {
                return Loi("Thiếu mã vé.");
            }

            string ma = maVe.Trim();
            VE ve = db.VEs
                .Include(v => v.LICHCHIEU.PHIM)
                .Include(v => v.LICHCHIEU.KHUNGGIO)
                .Include(v => v.LICHCHIEU.PHONGCHIEU)
                .Include(v => v.GHE)
                .FirstOrDefault(v => v.MA_VE.Trim() == ma);

            if (ve == null)
            {
                return Loi("Không tìm thấy vé " + ma + ".");
            }

            string trangThai = ve.TRANGTHAI == null ? "" : ve.TRANGTHAI.Trim();
            if (trangThai == VeDaCheckIn)
            {
                return Loi("Vé " + ma + " đã được check-in trước đó.");
            }
            if (trangThai == VeDaHuy)
            {
                return Loi("Vé " + ma + " đã bị hủy, không thể check-in.");
            }
            if (trangThai != VeDaThanhToan)
            {
                return Loi("Vé " + ma + " chưa thanh toán, không thể check-in.");
            }

            LICHCHIEU lichChieu = ve.LICHCHIEU;
            if (!string.IsNullOrEmpty(maRapNhanVien)
                && lichChieu.PHONGCHIEU.MA_RAP.Trim() != maRapNhanVien.Trim())
            {
                return Loi("Vé " + ma + " thuộc rạp khác, không thể check-in tại rạp của bạn.");
            }

            DateTime bayGio = DateTime.Now;
            DateTime batDau = GioBatDau(lichChieu);
            DateTime ketThuc = GioKetThuc(lichChieu);

            if (bayGio >= ketThuc)
            {
                return Loi("Suất chiếu của vé " + ma + " đã kết thúc.");
            }
            if (bayGio < batDau - CheckInSom)
            {
                return Loi("Chưa đến giờ check-in. Suất chiếu " + GioChieu(lichChieu) + " ngày "
                           + lichChieu.NGAYCHIEU.ToString("dd/MM/yyyy") + ", mở check-in trước giờ chiếu 60 phút.");
            }

            ve.TRANGTHAI = VeDaCheckIn;
            db.SaveChanges();

            return new CheckInKetQua
            {
                ThanhCong = true,
                ThongBao = "Check-in thành công vé " + ma + " - ghế " + ve.GHE.SOGHE + " - "
                           + lichChieu.PHIM.TEN_PHIM + " (" + GioChieu(lichChieu) + ")."
            };
        }

        private static CheckInKetQua Loi(string thongBao)
        {
            return new CheckInKetQua { ThanhCong = false, ThongBao = thongBao };
        }

        // ========================== BÁN VÉ TẠI QUẦY ==========================
        public List<string> GetBookedSeatIds(string maLichChieu)
        {
            string ma = maLichChieu.Trim();
            return db.VEs.Where(v => v.MA_LICHCHIEU == ma).Select(v => v.MA_GHE).ToList()
                .Select(g => g.Trim()).ToList();
        }

        public List<GHE> GetSeatsByRoom(string maPhong)
        {
            string ma = maPhong.Trim();
            return db.GHEs.Include(g => g.LOAIGHE).Where(g => g.MA_PHONG == ma).ToList();
        }

        public List<MONAN> GetFoodMenu()
        {
            return db.MONANs.Where(m => m.TRANGTHAI == "ConBan").OrderBy(m => m.LOAI).ThenBy(m => m.TEN_MON).ToList();
        }

        public string BanVeTaiQuay(string maNhanVien, string maRap, string maLichChieu, List<string> maGhe,
            Dictionary<string, int> monAn, string sdtKhach, string maKhuyenMai)
        {
            NHANVIEN nhanVien = GetStaff(maNhanVien);
            if (nhanVien == null)
            {
                throw new LoiNghiepVuException("Tài khoản của bạn chưa có hồ sơ nhân viên nên không thể bán vé tại quầy.");
            }

            LICHCHIEU lichChieu = GetSchedule(maLichChieu);
            if (lichChieu == null)
            {
                throw new LoiNghiepVuException("Không tìm thấy suất chiếu.");
            }
            if (!string.IsNullOrEmpty(maRap) && lichChieu.PHONGCHIEU.MA_RAP.Trim() != maRap.Trim())
            {
                throw new LoiNghiepVuException("Suất chiếu này thuộc rạp khác.");
            }
            if (!ConBanDuoc(lichChieu))
            {
                throw new LoiNghiepVuException("Suất chiếu đã bắt đầu quá 15 phút hoặc đã kết thúc, không thể bán thêm vé.");
            }

            List<string> dsMaGhe = (maGhe ?? new List<string>())
                .Select(g => g.Trim()).Where(g => g.Length > 0).Distinct().ToList();
            if (dsMaGhe.Count == 0)
            {
                throw new LoiNghiepVuException("Vui lòng chọn ít nhất một ghế.");
            }

            List<GHE> gheList = db.GHEs.Include(g => g.LOAIGHE).Where(g => dsMaGhe.Contains(g.MA_GHE)).ToList();
            string maPhong = lichChieu.MA_PHONG.Trim();
            if (gheList.Count != dsMaGhe.Count || gheList.Any(g => g.MA_PHONG.Trim() != maPhong))
            {
                throw new LoiNghiepVuException("Có ghế không thuộc phòng chiếu của suất này.");
            }

            string maLich = lichChieu.MA_LICHCHIEU.Trim();
            List<string> gheDaCoNguoi = db.VEs
                .Where(v => v.MA_LICHCHIEU == maLich && dsMaGhe.Contains(v.MA_GHE))
                .Select(v => v.MA_GHE).ToList();
            if (gheDaCoNguoi.Count > 0)
            {
                string tenGhe = string.Join(", ", gheList
                    .Where(g => gheDaCoNguoi.Any(x => x.Trim() == g.MA_GHE.Trim()))
                    .Select(g => g.SOGHE));
                throw new LoiNghiepVuException("Ghế " + tenGhe + " đã có người đặt, vui lòng chọn ghế khác.");
            }

            var bookingService = new BookingService(db);

            KHACHHANG khachHang = null;
            if (!string.IsNullOrWhiteSpace(sdtKhach))
            {
                khachHang = bookingService.GetCustomerByPhone(sdtKhach);
                if (khachHang == null)
                {
                    throw new LoiNghiepVuException("Không tìm thấy khách hàng có số điện thoại " + sdtKhach.Trim()
                                                   + ". Hãy để trống ô này nếu là khách vãng lai.");
                }
            }

            KHUYENMAI khuyenMai = null;
            if (!string.IsNullOrWhiteSpace(maKhuyenMai))
            {
                khuyenMai = bookingService.ValidatePromotion(maKhuyenMai);
                if (khuyenMai == null)
                {
                    throw new LoiNghiepVuException("Khuyến mãi không hợp lệ hoặc đã hết lượt dùng.");
                }
            }

            decimal tienMonAn = 0;
            var dongMon = new List<DONHANG_MONAN>();
            if (monAn != null)
            {
                foreach (var cap in monAn.Where(x => x.Value > 0))
                {
                    string maMon = cap.Key.Trim();
                    if (cap.Value > 20)
                    {
                        throw new LoiNghiepVuException("Mỗi món chỉ được mua tối đa 20 phần.");
                    }
                    MONAN mon = db.MONANs.FirstOrDefault(m => m.MA_MON.Trim() == maMon && m.TRANGTHAI == "ConBan");
                    if (mon == null)
                    {
                        throw new LoiNghiepVuException("Món " + maMon + " không còn bán.");
                    }
                    tienMonAn += mon.GIA * cap.Value;
                    dongMon.Add(new DONHANG_MONAN { MA_MON = mon.MA_MON.Trim(), SOLUONG = cap.Value, DONGIA = mon.GIA });
                }
            }

            Tuple<decimal, decimal, decimal> gia = bookingService.CalculatePrice(lichChieu, gheList, khuyenMai);
            decimal tienVe = gia.Item1;
            decimal tienGiam = gia.Item2;
            decimal tongTien = gia.Item3 + tienMonAn;

            int soDonHang = LaySoLonNhat(db.DONHANGs.Select(d => d.MA_DONHANG).ToList(), "DH") + 1;
            string maDonHang = "DH" + soDonHang.ToString("D3");

            var donHang = new DONHANG
            {
                MA_DONHANG = maDonHang,
                MA_KHACHHANG = khachHang == null ? null : khachHang.MA_KHACHHANG.Trim(),
                MA_NHANVIEN = nhanVien.MA_NHANVIEN.Trim(),
                MA_LICHCHIEU = maLich,
                MA_KM = khuyenMai == null ? null : khuyenMai.MA_KM.Trim(),
                NGAYDAT = DateTime.Now,
                TIENVE = tienVe,
                TIENMONAN = tienMonAn,
                TIENGIAM = tienGiam,
                TONGTIEN = tongTien,
                TRANGTHAI = "DaThanhToan"
            };
            db.DONHANGs.Add(donHang);

            int soVe = LaySoLonNhat(db.VEs.Select(v => v.MA_VE).ToList(), "VE");
            foreach (GHE ghe in gheList)
            {
                soVe++;
                db.VEs.Add(new VE
                {
                    MA_VE = "VE" + soVe.ToString("D3"),
                    MA_DONHANG = maDonHang,
                    MA_LICHCHIEU = maLich,
                    MA_GHE = ghe.MA_GHE.Trim(),
                    DONGIA = ghe.LOAIGHE.GIATVE + (lichChieu.GIATVE_PHUTROI ?? 0),
                    TRANGTHAI = VeDaThanhToan
                });
            }

            foreach (DONHANG_MONAN dong in dongMon)
            {
                dong.MA_DONHANG = maDonHang;
                db.DONHANG_MONAN.Add(dong);
            }

            if (khuyenMai != null)
            {
                string maKm = khuyenMai.MA_KM.Trim();
                KHUYENMAI km = db.KHUYENMAIs.FirstOrDefault(k => k.MA_KM.Trim() == maKm);
                if (km != null)
                {
                    km.SO_LUOT_DADUNG = km.SO_LUOT_DADUNG + 1;
                }
            }

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                Exception goc = ex;
                while (goc.InnerException != null) goc = goc.InnerException;

                if (goc.Message.Contains("UQ_GHE_LICH") || goc.Message.Contains("UNIQUE"))
                {
                    throw new LoiNghiepVuException("Ghế vừa được người khác đặt, vui lòng chọn lại.");
                }
                throw new LoiNghiepVuException("Lỗi lưu cơ sở dữ liệu: " + goc.Message);
            }

            return maDonHang;
        }

        public DONHANG GetOrder(string maDonHang)
        {
            if (string.IsNullOrEmpty(maDonHang)) return null;
            string ma = maDonHang.Trim();
            return db.DONHANGs
                .Include(d => d.KHACHHANG.TAIKHOAN)
                .Include(d => d.NHANVIEN.TAIKHOAN)
                .Include(d => d.KHUYENMAI)
                .Include(d => d.LICHCHIEU.PHIM)
                .Include(d => d.LICHCHIEU.KHUNGGIO)
                .Include(d => d.LICHCHIEU.PHONGCHIEU.RAPCHIEU)
                .Include(d => d.VEs.Select(v => v.GHE.LOAIGHE))
                .Include(d => d.DONHANG_MONAN.Select(m => m.MONAN))
                .FirstOrDefault(d => d.MA_DONHANG.Trim() == ma);
        }

        public List<DONHANG> GetOrdersByStaff(string maNhanVien, DateTime ngay)
        {
            DateTime tu = ngay.Date;
            DateTime den = tu.AddDays(1);

            var query = db.DONHANGs
                .Include(d => d.LICHCHIEU.PHIM)
                .Include(d => d.LICHCHIEU.KHUNGGIO)
                .Include(d => d.NHANVIEN.TAIKHOAN)
                .Include(d => d.VEs)
                .Where(d => d.MA_NHANVIEN != null && d.NGAYDAT >= tu && d.NGAYDAT < den);

            if (!string.IsNullOrEmpty(maNhanVien))
            {
                string ma = maNhanVien.Trim();
                query = query.Where(d => d.MA_NHANVIEN.Trim() == ma);
            }

            return query.OrderByDescending(d => d.NGAYDAT).ToList();
        }

        private static int LaySoLonNhat(IEnumerable<string> cacMa, string tienTo)
        {
            int max = 0;
            foreach (string ma in cacMa)
            {
                if (string.IsNullOrEmpty(ma)) continue;
                string t = ma.Trim();
                int so;
                if (t.StartsWith(tienTo) && int.TryParse(t.Substring(tienTo.Length), out so) && so > max)
                {
                    max = so;
                }
            }
            return max;
        }

        // ========================== PHÂN HỆ POS & QR SCANNER MỚI ==========================

        public List<BAPNUOC> GetConcessionsMenu()
        {
            return db.BAPNUOCs
                .Where(b => b.TRANGTHAI == "DangBan")
                .OrderBy(b => b.PHANLOAI)
                .ThenBy(b => b.TEN_SANPHAM)
                .ToList();
        }

        public object GetSeatMapForPos(string maLichChieu)
        {
            var lichChieu = GetSchedule(maLichChieu);
            if (lichChieu == null) return null;

            string maPhong = lichChieu.MA_PHONG.Trim();
            var danhSachGhe = db.GHEs
                .Include(g => g.LOAIGHE)
                .Where(g => g.MA_PHONG == maPhong)
                .ToList();

            var gheDaDat = GetBookedSeatIds(maLichChieu);
            decimal phuTroi = lichChieu.GIATVE_PHUTROI ?? 0m;

            var resultSeats = danhSachGhe.Select(g => new PosSeatItem
            {
                MaGhe = g.MA_GHE.Trim(),
                SoGhe = g.SOGHE.Trim(),
                HangGhe = !string.IsNullOrEmpty(g.SOGHE) ? g.SOGHE.Substring(0, 1) : ("H" + g.SO_HANG),
                CotGhe = g.SO_COT,
                MaLoaiGhe = g.MA_LOAIGHE?.Trim(),
                TenLoaiGhe = g.LOAIGHE?.TEN_LOAIGHE ?? "Thường",
                GiaGhe = (g.LOAIGHE?.GIATVE ?? 0m) + phuTroi,
                DaDat = gheDaDat.Contains(g.MA_GHE.Trim())
            }).OrderBy(s => s.HangGhe).ThenBy(s => s.CotGhe).ToList();

            return new
            {
                maLichChieu = lichChieu.MA_LICHCHIEU.Trim(),
                tenPhim = lichChieu.PHIM.TEN_PHIM,
                tenPhong = lichChieu.PHONGCHIEU.TEN_PHONG,
                tenRap = lichChieu.PHONGCHIEU.RAPCHIEU.TEN_RAP,
                ngayChieu = lichChieu.NGAYCHIEU.ToString("dd/MM/yyyy"),
                gioChieu = GioChieu(lichChieu),
                dinhDang = "2D Phụ đề",
                phuTroi = phuTroi,
                soGheTrong = resultSeats.Count(s => !s.DaDat),
                tongSoGhe = resultSeats.Count,
                danhSachGhe = resultSeats
            };
        }

        public object FindCustomerByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            string sdt = phone.Trim();

            var kh = db.KHACHHANGs
                .Include(k => k.TAIKHOAN)
                .FirstOrDefault(k => k.TAIKHOAN != null && k.TAIKHOAN.SDT.Trim() == sdt);

            if (kh != null)
            {
                int soDonDaDat = db.DONHANGs.Count(d => d.MA_KHACHHANG == kh.MA_KHACHHANG);
                decimal tongChiTieu = db.DONHANGs.Where(d => d.MA_KHACHHANG == kh.MA_KHACHHANG && d.TRANGTHAI == "DaThanhToan").Select(d => (decimal?)d.TONGTIEN).Sum() ?? 0m;

                return new
                {
                    found = true,
                    maKhachHang = kh.MA_KHACHHANG.Trim(),
                    hoTen = kh.TAIKHOAN.HOTEN,
                    sdt = kh.TAIKHOAN.SDT,
                    email = kh.TAIKHOAN.EMAIL,
                    soDonDaDat = soDonDaDat,
                    tongChiTieu = tongChiTieu.ToString("#,##0") + " đ"
                };
            }

            return new { found = false };
        }

        public string BanVePOS(
            string maNhanVien,
            string maRap,
            string maLichChieu,
            List<string> maGhe,
            Dictionary<string, int> bapNuoc,
            string sdtKhach,
            string hoTenKhach,
            string maKhuyenMai,
            string phuongThucThanhToan = "TienMat",
            decimal? tienKhachDua = null)
        {
            NHANVIEN nhanVien = GetStaff(maNhanVien);
            if (nhanVien == null)
            {
                throw new LoiNghiepVuException("Tài khoản của bạn chưa có hồ sơ nhân viên nên không thể thao tác bán vé tại quầy.");
            }

            LICHCHIEU lichChieu = GetSchedule(maLichChieu);
            if (lichChieu == null)
            {
                throw new LoiNghiepVuException("Không tìm thấy suất chiếu.");
            }

            if (!string.IsNullOrEmpty(maRap) && lichChieu.PHONGCHIEU.MA_RAP.Trim() != maRap.Trim())
            {
                throw new LoiNghiepVuException("Suất chiếu này thuộc rạp khác.");
            }

            if (!ConBanDuoc(lichChieu))
            {
                throw new LoiNghiepVuException("Suất chiếu đã bắt đầu quá 15 phút hoặc đã kết thúc, không thể bán thêm vé.");
            }

            List<string> dsMaGhe = (maGhe ?? new List<string>())
                .Select(g => g.Trim()).Where(g => g.Length > 0).Distinct().ToList();
            if (dsMaGhe.Count == 0)
            {
                throw new LoiNghiepVuException("Vui lòng chọn ít nhất một vị trí ghế.");
            }

            List<GHE> gheList = db.GHEs.Include(g => g.LOAIGHE).Where(g => dsMaGhe.Contains(g.MA_GHE)).ToList();
            string maPhong = lichChieu.MA_PHONG.Trim();
            if (gheList.Count != dsMaGhe.Count || gheList.Any(g => g.MA_PHONG.Trim() != maPhong))
            {
                throw new LoiNghiepVuException("Có ghế được chọn không thuộc phòng chiếu của suất này.");
            }

            // Kiểm tra ghế trùng
            string maLich = lichChieu.MA_LICHCHIEU.Trim();
            List<string> gheDaCoNguoi = db.VEs
                .Where(v => v.MA_LICHCHIEU == maLich && dsMaGhe.Contains(v.MA_GHE) && v.TRANGTHAI != VeDaHuy)
                .Select(v => v.MA_GHE).ToList();

            if (gheDaCoNguoi.Count > 0)
            {
                string tenGhe = string.Join(", ", gheList
                    .Where(g => gheDaCoNguoi.Any(x => x.Trim() == g.MA_GHE.Trim()))
                    .Select(g => g.SOGHE));
                throw new LoiNghiepVuException("Ghế [" + tenGhe + "] đã có người đặt, vui lòng chọn lại ghế khác.");
            }

            var bookingService = new BookingService(db);

            // Khách hàng: nếu có SĐT thì tìm hoặc tạo mới; nếu không thì dùng khách vãng lai
            KHACHHANG khachHang = null;
            if (!string.IsNullOrWhiteSpace(sdtKhach))
            {
                khachHang = bookingService.GetOrCreateCustomer(hoTenKhach, sdtKhach, "");
            }
            else
            {
                khachHang = db.KHACHHANGs.FirstOrDefault(k => k.MA_KHACHHANG == "KH001") ?? db.KHACHHANGs.FirstOrDefault();
            }

            // Khuyến mãi
            KHUYENMAI khuyenMai = null;
            if (!string.IsNullOrWhiteSpace(maKhuyenMai))
            {
                khuyenMai = bookingService.ValidatePromotion(maKhuyenMai);
                if (khuyenMai == null)
                {
                    throw new LoiNghiepVuException("Mã khuyến mãi không hợp lệ hoặc đã hết lượt sử dụng.");
                }
            }

            // Tính tiền Bắp nước Combo
            decimal tienBapNuoc = 0m;
            var chiTietBapNuocList = new List<CHITIET_BAPNUOC>();
            if (bapNuoc != null)
            {
                var allProducts = db.BAPNUOCs.Where(b => b.TRANGTHAI == "DangBan").ToList();
                var prodDict = allProducts.ToDictionary(p => p.MA_SANPHAM.Trim(), p => p);

                foreach (var item in bapNuoc.Where(x => x.Value > 0))
                {
                    string maSp = item.Key.Trim();
                    if (prodDict.TryGetValue(maSp, out BAPNUOC sp))
                    {
                        decimal thanhTien = sp.GIABAN * item.Value;
                        tienBapNuoc += thanhTien;
                        chiTietBapNuocList.Add(new CHITIET_BAPNUOC
                        {
                            MA_SANPHAM = sp.MA_SANPHAM.Trim(),
                            SOLUONG = item.Value,
                            DONGIA = sp.GIABAN,
                            THANHTIEN = thanhTien
                        });
                    }
                }
            }

            // Tính giá vé & giảm giá
            Tuple<decimal, decimal, decimal> gia = bookingService.CalculatePrice(lichChieu, gheList, khuyenMai);
            decimal tienVe = gia.Item1;
            decimal tienGiam = gia.Item2;
            decimal tongTien = gia.Item3 + tienBapNuoc;
            if (tongTien < 0) tongTien = 0;

            int soDonHang = LaySoLonNhat(db.DONHANGs.Select(d => d.MA_DONHANG).ToList(), "DH") + 1;
            string maDonHang = "DH" + soDonHang.ToString("D3");
            string maCheckIn = "CKI-" + maDonHang + "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

            string ptThanhToan = string.IsNullOrEmpty(phuongThucThanhToan) ? "TienMat" : phuongThucThanhToan.Trim();

            var donHang = new DONHANG
            {
                MA_DONHANG = maDonHang,
                MA_KHACHHANG = khachHang?.MA_KHACHHANG.Trim(),
                MA_NHANVIEN = nhanVien.MA_NHANVIEN.Trim(),
                MA_LICHCHIEU = maLich,
                MA_KM = khuyenMai?.MA_KM.Trim(),
                NGAYDAT = DateTime.Now,
                TIENVE = tienVe,
                TIENMONAN = tienBapNuoc,
                TIENGIAM = tienGiam,
                TONGTIEN = tongTien,
                TRANGTHAI = "DaThanhToan",
                PHUONGTHUCTHANHTOAN = ptThanhToan,
                TRANGTHAITHANHTOAN = "DaThanhToan",
                MAGIAODICH = ptThanhToan == "VietQR" ? ("VQR-POS-" + DateTime.Now.ToString("yyyyMMddHHmmss")) : ("CASH-POS-" + DateTime.Now.ToString("yyyyMMddHHmmss")),
                MACHECKIN = maCheckIn,
                TRANGTHAISOATVE = "ChuaSoatVe",
                TRANGTHAINHANBAPNUOC = chiTietBapNuocList.Any() ? "ChuaNhan" : "KhongCo"
            };
            db.DONHANGs.Add(donHang);

            // Thêm vé
            int soVe = LaySoLonNhat(db.VEs.Select(v => v.MA_VE).ToList(), "VE");
            foreach (GHE ghe in gheList)
            {
                soVe++;
                db.VEs.Add(new VE
                {
                    MA_VE = "VE" + soVe.ToString("D3"),
                    MA_DONHANG = maDonHang,
                    MA_LICHCHIEU = maLich,
                    MA_GHE = ghe.MA_GHE.Trim(),
                    DONGIA = ghe.LOAIGHE.GIATVE + (lichChieu.GIATVE_PHUTROI ?? 0),
                    TRANGTHAI = VeDaThanhToan
                });
            }

            // Thêm chi tiết bắp nước
            foreach (var ct in chiTietBapNuocList)
            {
                ct.MA_DONHANG = maDonHang;
                db.CHITIET_BAPNUOC.Add(ct);
            }

            // Cập nhật khuyến mãi
            if (khuyenMai != null)
            {
                string kmId = khuyenMai.MA_KM.Trim();
                KHUYENMAI km = db.KHUYENMAIs.FirstOrDefault(k => k.MA_KM.Trim() == kmId);
                if (km != null)
                {
                    km.SO_LUOT_DADUNG = km.SO_LUOT_DADUNG + 1;
                }
            }

            db.SaveChanges();

            // Tự động tích điểm cho khách hàng thành viên khi có SĐT
            if (!string.IsNullOrWhiteSpace(sdtKhach) && khachHang != null)
            {
                LoyaltyService.Instance.CongDiemVaXetHang(khachHang.MA_KHACHHANG, tongTien, db);
            }

            return maDonHang;
        }

        // ========================== SOÁT VÉ QR SCANNER ==========================

        public CheckInScanResult KiemTraCheckInChiTiet(string code, string maRapNhanVien)
        {
            var res = new CheckInScanResult();
            if (string.IsNullOrWhiteSpace(code))
            {
                res.HopLe = false;
                res.LoaiKetQua = "KHONG_TON_TAI";
                res.ThongBao = "Vui lòng quét hoặc nhập mã vé / mã Check-in.";
                return res;
            }

            string k = code.Trim();

            // Tìm đơn hàng theo MaCheckIn, MaDonHang hoặc MaVe
            DONHANG donHang = db.DONHANGs
                .Include(d => d.KHACHHANG.TAIKHOAN)
                .Include(d => d.NHANVIEN.TAIKHOAN)
                .Include(d => d.LICHCHIEU.PHIM)
                .Include(d => d.LICHCHIEU.KHUNGGIO)
                .Include(d => d.LICHCHIEU.PHONGCHIEU.RAPCHIEU)
                .Include(d => d.VEs.Select(v => v.GHE.LOAIGHE))
                .Include(d => d.CHITIET_BAPNUOC.Select(c => c.BAPNUOC))
                .FirstOrDefault(d => d.MACHECKIN.Trim() == k || d.MA_DONHANG.Trim() == k);

            if (donHang == null)
            {
                // Thử tìm qua mã vé
                var ve = db.VEs.Include(v => v.DONHANG).FirstOrDefault(v => v.MA_VE.Trim() == k);
                if (ve != null && ve.DONHANG != null)
                {
                    string maDh = ve.DONHANG.MA_DONHANG.Trim();
                    donHang = db.DONHANGs
                        .Include(d => d.KHACHHANG.TAIKHOAN)
                        .Include(d => d.NHANVIEN.TAIKHOAN)
                        .Include(d => d.LICHCHIEU.PHIM)
                        .Include(d => d.LICHCHIEU.KHUNGGIO)
                        .Include(d => d.LICHCHIEU.PHONGCHIEU.RAPCHIEU)
                        .Include(d => d.VEs.Select(v => v.GHE.LOAIGHE))
                        .Include(d => d.CHITIET_BAPNUOC.Select(c => c.BAPNUOC))
                        .FirstOrDefault(d => d.MA_DONHANG.Trim() == maDh);
                }
            }

            if (donHang == null)
            {
                res.HopLe = false;
                res.LoaiKetQua = "KHONG_TON_TAI";
                res.ThongBao = "Không tìm thấy thông tin vé với mã: " + k;
                return res;
            }

            // Điền thông tin cơ bản
            res.MaDonHang = donHang.MA_DONHANG.Trim();
            res.MaCheckIn = !string.IsNullOrEmpty(donHang.MACHECKIN) ? donHang.MACHECKIN.Trim() : ("CKI-" + res.MaDonHang);
            res.TenPhim = donHang.LICHCHIEU?.PHIM?.TEN_PHIM;
            res.Poster = donHang.LICHCHIEU?.PHIM?.POSTER;
            res.TenRap = donHang.LICHCHIEU?.PHONGCHIEU?.RAPCHIEU?.TEN_RAP;
            res.TenPhong = donHang.LICHCHIEU?.PHONGCHIEU?.TEN_PHONG;
            res.GioChieu = donHang.LICHCHIEU != null ? GioChieu(donHang.LICHCHIEU) : "";
            res.NgayChieu = donHang.LICHCHIEU != null ? donHang.LICHCHIEU.NGAYCHIEU.ToString("dd/MM/yyyy") : "";
            res.DanhSachGhe = donHang.VEs.Select(v => v.GHE?.SOGHE?.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            res.TongTien = donHang.TONGTIEN;
            res.TenKhachHang = donHang.KHACHHANG?.HOTEN ?? donHang.KHACHHANG?.TAIKHOAN?.HOTEN ?? "Khách vãng lai";
            res.SdtKhachHang = donHang.KHACHHANG?.SDT ?? donHang.KHACHHANG?.TAIKHOAN?.SDT;
            res.DaSoatVe = string.Equals(donHang.TRANGTHAISOATVE?.Trim(), "DaSoatVe", StringComparison.OrdinalIgnoreCase);
            res.ThoiGianCheckIn = donHang.THOIGIANCHECKIN;
            res.NhanVienSoatVe = donHang.MANV_SOATVE;
            res.TrangThaiNhanBapNuoc = donHang.TRANGTHAINHANBAPNUOC ?? "KhongCo";

            foreach (var ct in donHang.CHITIET_BAPNUOC)
            {
                res.DanhSachBapNuoc.Add(new CheckInBapNuocItem
                {
                    TenSanPham = ct.BAPNUOC?.TEN_SANPHAM ?? "Sản phẩm",
                    SoLuong = ct.SOLUONG,
                    PhanLoai = ct.BAPNUOC?.PHANLOAI
                });
            }

            // Kiểm tra trạng thái đơn
            if (donHang.TRANGTHAI == "DaHuy" || donHang.TRANGTHAITHANHTOAN == "DaHuy")
            {
                res.HopLe = false;
                res.LoaiKetQua = "DA_HUY";
                res.ThongBao = "CẢNH BÁO: Đơn vé này đã bị HỦY trước đó!";
                return res;
            }

            if (donHang.TRANGTHAITHANHTOAN != "DaThanhToan")
            {
                res.HopLe = false;
                res.LoaiKetQua = "CHUA_THANH_TOAN";
                res.ThongBao = "CẢNH BÁO: Vé chưa thanh toán (Trạng thái: " + donHang.TRANGTHAITHANHTOAN + "). Khách phải thanh toán trước khi vào rạp!";
                return res;
            }

            // Kiểm tra ĐÃ CHECK-IN trước đó
            if (res.DaSoatVe)
            {
                res.HopLe = false;
                res.LoaiKetQua = "DA_CHECK_IN";
                string nvInfo = !string.IsNullOrEmpty(res.NhanVienSoatVe) ? (" bởi NV [" + res.NhanVienSoatVe.Trim() + "]") : "";
                res.ThongBao = "CẢNH BÁO GIAN LẬN: Vé này ĐÃ ĐƯỢC CHECK-IN vào lúc " + (res.ThoiGianCheckIn.HasValue ? res.ThoiGianCheckIn.Value.ToString("dd/MM/yyyy HH:mm:ss") : "trước đó") + nvInfo + "! Không cho phép sử dụng lại mã QR.";
                return res;
            }

            // Kiểm tra Rạp
            if (!string.IsNullOrEmpty(maRapNhanVien) && donHang.LICHCHIEU?.PHONGCHIEU?.MA_RAP.Trim() != maRapNhanVien.Trim())
            {
                res.HopLe = false;
                res.LoaiKetQua = "SAI_RAP";
                res.ThongBao = "CẢNH BÁO: Vé này thuộc rạp " + res.TenRap + ", không phải rạp bạn đang trực!";
                return res;
            }

            // Kiểm tra Suất chiếu
            if (donHang.LICHCHIEU != null)
            {
                DateTime bayGio = DateTime.Now;
                DateTime batDau = GioBatDau(donHang.LICHCHIEU);
                DateTime ketThuc = GioKetThuc(donHang.LICHCHIEU);

                if (bayGio >= ketThuc)
                {
                    res.HopLe = false;
                    res.LoaiKetQua = "HET_HAN";
                    res.ThongBao = "CẢNH BÁO: Suất chiếu này đã KẾT THÚC (" + res.GioChieu + " " + res.NgayChieu + "). Vé không còn hiệu lực!";
                    return res;
                }

                if (bayGio < batDau - CheckInSom)
                {
                    res.HopLe = false;
                    res.LoaiKetQua = "CHUA_DEN_GIO";
                    res.ThongBao = "LƯU Ý: Chưa đến giờ mở cửa phòng chiếu. Suất chiếu bắt đầu lúc " + res.GioChieu + " ngày " + res.NgayChieu + " (Mở check-in trước 60 phút).";
                    return res;
                }
            }

            // HỢP LỆ!
            res.HopLe = true;
            res.LoaiKetQua = "THANH_CONG";
            res.ThongBao = "VÉ HỢP LỆ! Sẵn sàng xác nhận check-in vào phòng chiếu.";
            return res;
        }

        public bool XacNhanCheckInVaoRap(string maDonHang, string maNhanVien)
        {
            if (string.IsNullOrEmpty(maDonHang)) return false;
            string trimmed = maDonHang.Trim();

            var donHang = db.DONHANGs.Include(d => d.VEs).FirstOrDefault(d => d.MA_DONHANG.Trim() == trimmed);
            if (donHang == null) return false;

            donHang.TRANGTHAISOATVE = "DaSoatVe";
            donHang.THOIGIANCHECKIN = DateTime.Now;
            if (!string.IsNullOrEmpty(maNhanVien))
            {
                donHang.MANV_SOATVE = maNhanVien.Trim();
            }

            foreach (var ve in donHang.VEs)
            {
                ve.TRANGTHAI = VeDaCheckIn;
            }

            db.SaveChanges();
            return true;
        }

        public bool XacNhanGiaoBapNuoc(string maDonHang)
        {
            if (string.IsNullOrEmpty(maDonHang)) return false;
            string trimmed = maDonHang.Trim();

            var donHang = db.DONHANGs.FirstOrDefault(d => d.MA_DONHANG.Trim() == trimmed);
            if (donHang == null) return false;

            donHang.TRANGTHAINHANBAPNUOC = "DaNhan";
            db.SaveChanges();
            return true;
        }
    }
}


