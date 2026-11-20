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
            get { return TAIKHOAN != null ? TAIKHOAN.HOTEN : null; }
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.HOTEN = value;
            }
        }

        public string SDT
        {
            get { return TAIKHOAN != null ? TAIKHOAN.SDT : null; }
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.SDT = value;
            }
        }

        public string EMAIL
        {
            get { return TAIKHOAN != null ? TAIKHOAN.EMAIL : null; }
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.EMAIL = value;
            }
        }

        public string MATKHAU
        {
            get { return TAIKHOAN != null ? TAIKHOAN.MATKHAU : null; }
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.MATKHAU = value;
            }
        }

        public DateTime? NGAYTAO
        {
            get { return TAIKHOAN != null ? TAIKHOAN.NGAYTAO : null; }
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.NGAYTAO = value;
            }
        }

        public string VAITRO
        {
            get
            {
                if (TAIKHOAN == null || TAIKHOAN.VAITROes == null) return "KhachHang";
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
            get { return TAIKHOAN != null ? TAIKHOAN.HOTEN : null; }
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.HOTEN = value;
            }
        }

        public string SDT
        {
            get { return TAIKHOAN != null ? TAIKHOAN.SDT : null; }
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.SDT = value;
            }
        }

        public string EMAIL
        {
            get { return TAIKHOAN != null ? TAIKHOAN.EMAIL : null; }
            set
            {
                if (TAIKHOAN != null) TAIKHOAN.EMAIL = value;
            }
        }

        public string MATKHAU
        {
            get { return TAIKHOAN != null ? TAIKHOAN.MATKHAU : null; }
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
