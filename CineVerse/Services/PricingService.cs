using System;
using System.Collections.Generic;
using System.Linq;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class PricingResult
    {
        public decimal TienGheGoc { get; set; }
        public decimal TienPhuTroiSuat { get; set; }
        public decimal TienPhuThuCuoiTuan { get; set; }
        public decimal TongTienVe { get; set; }
        public decimal TienMonAn { get; set; }
        public decimal TongTruocGiam { get; set; }
        public decimal TienGiamGia { get; set; }
        public decimal TongThanhToan { get; set; }
        public string MaKhuyenMai { get; set; }
        public string TenKhuyenMai { get; set; }
        public int PhanTramGiam { get; set; }
        public List<SeatPriceItem> ChiTietGhe { get; set; } = new List<SeatPriceItem>();
        public List<FnBPriceItem> ChiTietBapNuoc { get; set; } = new List<FnBPriceItem>();
    }

    public class SeatPriceItem
    {
        public string MaGhe { get; set; }
        public string SoGhe { get; set; }
        public string TenLoaiGhe { get; set; }
        public decimal GiaGoc { get; set; }
        public decimal PhuTroiSuat { get; set; }
        public decimal PhuThuCuoiTuan { get; set; }
        public decimal DonGiaCuoiCung { get; set; }
    }

    public class FnBPriceItem
    {
        public string MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public string PhanLoai { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }

    /// <summary>
    /// Dịch vụ tính toán bảng giá vé, phụ thu cuối tuần, khung giờ và khuyến mãi tập trung.
    /// Dùng chung cho cả Web Client (Booking) và Quầy vé (POS Nhân viên).
    /// </summary>
    public class PricingService
    {
        /// <summary>
        /// Phụ thu cuối tuần mặc định cho mỗi vé vào Thứ Bảy & Chủ Nhật (VNĐ).
        /// Có thể đặt bằng 0 nếu rạp không áp dụng hoặc cấu hình riêng.
        /// </summary>
        public decimal WeekendSurchargePerSeat { get; set; } = 0m;

        /// <summary>
        /// Kiểm tra suất chiếu có rơi vào ngày cuối tuần (Thứ 7 / Chủ Nhật) không.
        /// </summary>
        public static bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        /// <summary>
        /// Tính toán chi tiết giá vé và tổng tiền thanh toán cho danh sách ghế và bắp nước (Phase 2 BAPNUOC).
        /// </summary>
        public PricingResult CalculateTotalWithFnB(
            LICHCHIEU lichChieu,
            IEnumerable<GHE> danhSachGhe,
            KHUYENMAI khuyenMai = null,
            Dictionary<string, int> bapNuocDict = null,
            IEnumerable<BAPNUOC> danhSachBapNuoc = null)
        {
            var result = new PricingResult();

            if (danhSachGhe == null)
            {
                return result;
            }

            decimal phuTroiSuat = lichChieu?.GIATVE_PHUTROI ?? 0m;
            bool laCuoiTuan = lichChieu != null && IsWeekend(lichChieu.NGAYCHIEU);
            decimal phuThuCuoiTuan = (laCuoiTuan && WeekendSurchargePerSeat > 0) ? WeekendSurchargePerSeat : 0m;

            decimal tongGiaGoc = 0m;
            decimal tongPhuTroi = 0m;
            decimal tongCuoiTuan = 0m;

            foreach (var ghe in danhSachGhe)
            {
                decimal giaGoc = ghe.LOAIGHE?.GIATVE ?? 0m;
                decimal donGiaGhe = giaGoc + phuTroiSuat + phuThuCuoiTuan;

                tongGiaGoc += giaGoc;
                tongPhuTroi += phuTroiSuat;
                tongCuoiTuan += phuThuCuoiTuan;

                result.ChiTietGhe.Add(new SeatPriceItem
                {
                    MaGhe = ghe.MA_GHE?.Trim(),
                    SoGhe = ghe.SOGHE?.Trim(),
                    TenLoaiGhe = ghe.LOAIGHE?.TEN_LOAIGHE?.Trim() ?? "Thường",
                    GiaGoc = giaGoc,
                    PhuTroiSuat = phuTroiSuat,
                    PhuThuCuoiTuan = phuThuCuoiTuan,
                    DonGiaCuoiCung = donGiaGhe
                });
            }

            result.TienGheGoc = tongGiaGoc;
            result.TienPhuTroiSuat = tongPhuTroi;
            result.TienPhuThuCuoiTuan = tongCuoiTuan;
            result.TongTienVe = tongGiaGoc + tongPhuTroi + tongCuoiTuan;

            // Tính tiền bắp nước / combo
            decimal tongTienMon = 0m;
            if (bapNuocDict != null && danhSachBapNuoc != null)
            {
                var bapNuocLookup = danhSachBapNuoc.ToDictionary(b => b.MA_SANPHAM.Trim(), b => b);
                foreach (var item in bapNuocDict)
                {
                    string maSp = item.Key?.Trim() ?? "";
                    int soLuong = item.Value;
                    if (soLuong > 0 && bapNuocLookup.TryGetValue(maSp, out BAPNUOC product))
                    {
                        decimal thanhTien = product.GIABAN * soLuong;
                        tongTienMon += thanhTien;
                        result.ChiTietBapNuoc.Add(new FnBPriceItem
                        {
                            MaSanPham = product.MA_SANPHAM.Trim(),
                            TenSanPham = product.TEN_SANPHAM.Trim(),
                            PhanLoai = product.PHANLOAI?.Trim(),
                            SoLuong = soLuong,
                            DonGia = product.GIABAN,
                            ThanhTien = thanhTien
                        });
                    }
                }
            }
            result.TienMonAn = tongTienMon;
            result.TongTruocGiam = result.TongTienVe + result.TienMonAn;

            // Áp dụng khuyến mãi
            decimal tienGiam = 0m;
            if (khuyenMai != null && IsPromotionValid(khuyenMai))
            {
                result.MaKhuyenMai = khuyenMai.MA_KM?.Trim();
                result.TenKhuyenMai = khuyenMai.TEN_KM?.Trim();
                result.PhanTramGiam = khuyenMai.GIAMGIA_PHANTRAM;

                // Giảm giá áp dụng trên tổng tiền vé
                decimal coSoGiamGia = result.TongTienVe;
                tienGiam = coSoGiamGia * khuyenMai.GIAMGIA_PHANTRAM / 100m;

                if (khuyenMai.GIAMGIA_TOIDA.HasValue && tienGiam > khuyenMai.GIAMGIA_TOIDA.Value)
                {
                    tienGiam = khuyenMai.GIAMGIA_TOIDA.Value;
                }
            }

            result.TienGiamGia = Math.Round(tienGiam, 0);
            decimal tongThanhToan = result.TongTruocGiam - result.TienGiamGia;
            result.TongThanhToan = tongThanhToan > 0 ? tongThanhToan : 0m;

            return result;
        }

        /// <summary>
        /// Tính toán chi tiết giá vé và tổng tiền thanh toán cho danh sách ghế và món ăn kèm (Backward compatibility).
        /// </summary>
        public PricingResult CalculateTotal(
            LICHCHIEU lichChieu,
            IEnumerable<GHE> danhSachGhe,
            KHUYENMAI khuyenMai = null,
            Dictionary<string, int> monAnDict = null,
            IEnumerable<MONAN> danhSachMonAn = null)
        {
            var result = new PricingResult();

            if (danhSachGhe == null)
            {
                return result;
            }

            decimal phuTroiSuat = lichChieu?.GIATVE_PHUTROI ?? 0m;
            bool laCuoiTuan = lichChieu != null && IsWeekend(lichChieu.NGAYCHIEU);
            decimal phuThuCuoiTuan = (laCuoiTuan && WeekendSurchargePerSeat > 0) ? WeekendSurchargePerSeat : 0m;

            decimal tongGiaGoc = 0m;
            decimal tongPhuTroi = 0m;
            decimal tongCuoiTuan = 0m;

            foreach (var ghe in danhSachGhe)
            {
                decimal giaGoc = ghe.LOAIGHE?.GIATVE ?? 0m;
                decimal donGiaGhe = giaGoc + phuTroiSuat + phuThuCuoiTuan;

                tongGiaGoc += giaGoc;
                tongPhuTroi += phuTroiSuat;
                tongCuoiTuan += phuThuCuoiTuan;

                result.ChiTietGhe.Add(new SeatPriceItem
                {
                    MaGhe = ghe.MA_GHE?.Trim(),
                    SoGhe = ghe.SOGHE?.Trim(),
                    TenLoaiGhe = ghe.LOAIGHE?.TEN_LOAIGHE?.Trim() ?? "Thường",
                    GiaGoc = giaGoc,
                    PhuTroiSuat = phuTroiSuat,
                    PhuThuCuoiTuan = phuThuCuoiTuan,
                    DonGiaCuoiCung = donGiaGhe
                });
            }

            result.TienGheGoc = tongGiaGoc;
            result.TienPhuTroiSuat = tongPhuTroi;
            result.TienPhuThuCuoiTuan = tongCuoiTuan;
            result.TongTienVe = tongGiaGoc + tongPhuTroi + tongCuoiTuan;

            // Tính tiền bắp nước / đồ ăn nếu có
            decimal tongTienMon = 0m;
            if (monAnDict != null && danhSachMonAn != null)
            {
                var monAnLookup = danhSachMonAn.ToDictionary(m => m.MA_MON.Trim(), m => m.GIA);
                foreach (var item in monAnDict)
                {
                    string maMon = item.Key?.Trim() ?? "";
                    int soLuong = item.Value;
                    if (soLuong > 0 && monAnLookup.TryGetValue(maMon, out decimal giaMon))
                    {
                        tongTienMon += giaMon * soLuong;
                    }
                }
            }
            result.TienMonAn = tongTienMon;
            result.TongTruocGiam = result.TongTienVe + result.TienMonAn;

            // Áp dụng khuyến mãi
            decimal tienGiam = 0m;
            if (khuyenMai != null && IsPromotionValid(khuyenMai))
            {
                result.MaKhuyenMai = khuyenMai.MA_KM?.Trim();
                result.TenKhuyenMai = khuyenMai.TEN_KM?.Trim();
                result.PhanTramGiam = khuyenMai.GIAMGIA_PHANTRAM;

                // Giảm giá áp dụng trên tiền vé
                decimal coSoGiamGia = result.TongTienVe;
                tienGiam = coSoGiamGia * khuyenMai.GIAMGIA_PHANTRAM / 100m;

                if (khuyenMai.GIAMGIA_TOIDA.HasValue && tienGiam > khuyenMai.GIAMGIA_TOIDA.Value)
                {
                    tienGiam = khuyenMai.GIAMGIA_TOIDA.Value;
                }
            }

            result.TienGiamGia = Math.Round(tienGiam, 0);
            decimal tongThanhToan = result.TongTruocGiam - result.TienGiamGia;
            result.TongThanhToan = tongThanhToan > 0 ? tongThanhToan : 0m;

            return result;
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của mã khuyến mãi tại thời điểm hiện tại.
        /// </summary>
        public static bool IsPromotionValid(KHUYENMAI km)
        {
            if (km == null) return false;

            DateTime now = DateTime.Now;

            bool conHieuLuc = string.Equals(km.TRANGTHAI?.Trim(), "HoatDong", StringComparison.OrdinalIgnoreCase)
                           && km.NGAYBATDAU <= now
                           && km.NGAYKETTHUC >= now;

            if (!conHieuLuc) return false;

            if (km.SO_LUOT_TOIDA.HasValue && km.SO_LUOT_DADUNG >= km.SO_LUOT_TOIDA.Value)
            {
                return false;
            }

            return true;
        }
    }
}
