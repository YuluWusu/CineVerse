using System;
using System.Linq;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Models
{
    public partial class KHACHHANG
    {
        public string MA_KHACH
        {
            get => MA_KHACHHANG;
            set => MA_KHACHHANG = value;
        }

        public string HOTEN
        {
            get => TAIKHOAN?.HOTEN;
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.HOTEN = value;
            }
        }

        public string SDT
        {
            get => TAIKHOAN?.SDT;
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.SDT = value;
            }
        }

        public string EMAIL
        {
            get => TAIKHOAN?.EMAIL;
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.EMAIL = value;
            }
        }

        public string MATKHAU
        {
            get => TAIKHOAN?.MATKHAU;
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.MATKHAU = value;
            }
        }

        public DateTime? NGAYTAO
        {
            get => TAIKHOAN?.NGAYTAO;
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.NGAYTAO = value;
            }
        }

        public string VAITRO
        {
            get
            {
                if (TAIKHOAN?.VAITROes == null) return "KhachHang";
                bool isAdmin = TAIKHOAN.VAITROes.Any(v =>
                    (v.MA_VAITRO != null && v.MA_VAITRO.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase)) ||
                    (v.TEN_VAITRO != null && v.TEN_VAITRO.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase)));
                return isAdmin ? "Admin" : "KhachHang";
            }
        }
    }

    public partial class DONHANG
    {
        public string MA_KHACH
        {
            get => MA_KHACHHANG;
            set => MA_KHACHHANG = value;
        }

        public string MA_NV
        {
            get => MA_NHANVIEN;
            set => MA_NHANVIEN = value;
        }
    }

    public partial class NHANVIEN
    {
        public string MA_NV
        {
            get => MA_NHANVIEN;
            set => MA_NHANVIEN = value;
        }

        public string HOTEN
        {
            get => TAIKHOAN?.HOTEN;
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.HOTEN = value;
            }
        }

        public string SDT
        {
            get => TAIKHOAN?.SDT;
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.SDT = value;
            }
        }

        public string EMAIL
        {
            get => TAIKHOAN?.EMAIL;
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.EMAIL = value;
            }
        }

        public string MATKHAU
        {
            get => TAIKHOAN?.MATKHAU;
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.MATKHAU = value;
            }
        }
    }

    /// <summary>
    /// Alias class for QL_VEPHIMEntities to support both naming conventions seamlessly.
    /// </summary>
    public class QL_VEPHIM2Entities : QL_VEPHIMEntities
    {
        public QL_VEPHIM2Entities() : base() { }
    }
}
