using System;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Models
{
    [Serializable]
    public class GioHangItem
    {
        /// <summary>
        /// Phân loại: "VE" hoặc "BAPNUOC"
        /// </summary>
        public string LoaiItem { get; set; }

        /// <summary>
        /// Mã định danh duy nhất của dòng trong giỏ hàng (ví dụ: "VE_LC01_A1" hoặc "FNB_CB01_SizeLon")
        /// </summary>
        public string MaItem { get; set; }

        // Thông tin chung
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public string TuyChon { get; set; } // Size Vừa, Size Lớn (+10k), Ghế VIP,...
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }

        // Thông tin chuyên biệt cho Vé Phim
        public string MaLichChieu { get; set; }
        public string MaPhim { get; set; }
        public string TenPhim { get; set; }
        public string TenPhong { get; set; }
        public string TenRap { get; set; }
        public string MaGhe { get; set; }
        public string TenGhe { get; set; }
        public string LoaiGhe { get; set; }
        public DateTime? NgayChieu { get; set; }
        public TimeSpan? GioChieu { get; set; }

        // Thông tin chuyên biệt cho Bắp nước
        public string MaBapNuoc { get; set; }
        public string GhiChu { get; set; }

        // Tính thành tiền
        public decimal ThanhTien
        {
            get { return DonGia * SoLuong; }
        }

        public GioHangItem()
        {
            SoLuong = 1;
            LoaiItem = "BAPNUOC";
            TuyChon = "Tiêu chuẩn";
        }
    }
}

