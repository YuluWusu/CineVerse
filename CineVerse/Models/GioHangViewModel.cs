using System;
using System.Collections.Generic;
using System.Linq;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Models
{
    public class GioHangViewModel
    {
        public List<GioHangItem> Items { get; set; }
        public string MaKhuyenMai { get; set; }
        public decimal GiamGiaKhuyenMai { get; set; }
        public string HangThanhVien { get; set; }
        public decimal PhanTramChietKhau { get; set; }
        public decimal ChietKhauThanhVien { get; set; }
        public string PhuongThucThanhToan { get; set; }

        // Thông tin người nhận vé / liên hệ
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public string GhiChu { get; set; }

        public GioHangViewModel()
        {
            Items = new List<GioHangItem>();
            PhuongThucThanhToan = "VietQR";
            HangThanhVien = "STANDARD";
        }

        public decimal TongTienHang
        {
            get { return Items != null ? Items.Sum(i => i.ThanhTien) : 0m; }
        }

        public int TongSoLuong
        {
            get { return Items != null ? Items.Sum(i => i.SoLuong) : 0; }
        }

        public int SoLuongVe
        {
            get { return Items != null ? Items.Where(i => i.LoaiItem == "VE").Sum(i => i.SoLuong) : 0; }
        }

        public int SoLuongBapNuoc
        {
            get { return Items != null ? Items.Where(i => i.LoaiItem == "BAPNUOC").Sum(i => i.SoLuong) : 0; }
        }

        public decimal TongThanhToan
        {
            get
            {
                decimal total = TongTienHang - GiamGiaKhuyenMai - ChietKhauThanhVien;
                return total > 0 ? total : 0m;
            }
        }
    }
}

