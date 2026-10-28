using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DoAn_QuanLyVeXemPhimTrucTiep.Helpers;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TAIKHOAN User { get; set; }
        public KHACHHANG Customer { get; set; }
        public NHANVIEN Staff { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsStaff { get; set; }
        public bool IsCustomer { get; set; }
        public string PrimaryRole { get; set; }
    }

    /// <summary>
    /// Dịch vụ quản lý xác thực và phân quyền tài khoản (Khách hàng, Nhân viên, Quản trị viên).
    /// </summary>
    public class AuthService : IDisposable
    {
        private readonly QL_VEPHIMEntities _db;
        private readonly bool _disposeDb;

        public AuthService()
        {
            _db = new QL_VEPHIMEntities();
            _disposeDb = true;
        }

        public AuthService(QL_VEPHIMEntities dbContext)
        {
            _db = dbContext ?? new QL_VEPHIMEntities();
            _disposeDb = false;
        }

        /// <summary>
        /// Xác thực thông tin đăng nhập theo Số điện thoại và Mật khẩu.
        /// Tự động nâng cấp hash mật khẩu nếu tài khoản đang dùng mật khẩu plain-text cũ.
        /// </summary>
        public AuthResult Authenticate(string soDienThoai, string matKhau)
        {
            if (string.IsNullOrWhiteSpace(soDienThoai) || string.IsNullOrWhiteSpace(matKhau))
            {
                return new AuthResult { Success = false, ErrorMessage = "Vui lòng nhập đầy đủ số điện thoại và mật khẩu." };
            }

            string sdtTrim = soDienThoai.Trim();

            var tk = _db.TAIKHOANs
                .Include(t => t.KHACHHANG)
                .Include(t => t.NHANVIEN)
                .Include(t => t.VAITROes)
                .FirstOrDefault(t => t.SDT.Trim() == sdtTrim);

            if (tk == null)
            {
                return new AuthResult { Success = false, ErrorMessage = "Số điện thoại hoặc mật khẩu không chính xác." };
            }

            if (string.Equals(tk.TRANGTHAI, "Khoa", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tk.TRANGTHAI, "DaNghi", StringComparison.OrdinalIgnoreCase))
            {
                return new AuthResult { Success = false, ErrorMessage = "Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động. Vui lòng liên hệ quản trị viên." };
            }

            // Xác thực mật khẩu qua PasswordHelper
            bool isValidPassword = PasswordHelper.VerifyPassword(matKhau, tk.MATKHAU, out bool needsUpgrade);

            if (!isValidPassword)
            {
                return new AuthResult { Success = false, ErrorMessage = "Số điện thoại hoặc mật khẩu không chính xác." };
            }

            // Tự động nâng cấp hash sang PBKDF2 nếu mật khẩu cũ đang lưu plain-text
            if (needsUpgrade)
            {
                try
                {
                    tk.MATKHAU = PasswordHelper.HashPassword(matKhau);
                    _db.SaveChanges();
                }
                catch
                {
                    // Lỗi ghi log nếu có, không chặn quá trình đăng nhập của người dùng
                }
            }

            bool laAdmin = tk.VAITROes.Any(v => CheckRole(v, "ADMIN"));
            bool laNhanVien = tk.VAITROes.Any(v => CheckRole(v, "NHANVIEN") || CheckRole(v, "QUANLYRAP")) || tk.NHANVIEN != null;
            bool laKhachHang = tk.VAITROes.Any(v => CheckRole(v, "KHACHHANG")) || tk.KHACHHANG != null;

            string primaryRole = "KHACHHANG";
            if (laAdmin) primaryRole = "ADMIN";
            else if (laNhanVien) primaryRole = "NHANVIEN";

            // Đảm bảo khách hàng luôn có bản ghi KHACHHANG
            var kh = tk.KHACHHANG;
            if (kh == null && !laNhanVien && !laAdmin)
            {
                kh = new KHACHHANG
                {
                    MA_KHACHHANG = tk.MA_TAIKHOAN,
                    TAIKHOAN = tk
                };
                _db.KHACHHANGs.Add(kh);
                _db.SaveChanges();
            }

            return new AuthResult
            {
                Success = true,
                User = tk,
                Customer = kh,
                Staff = tk.NHANVIEN,
                IsAdmin = laAdmin,
                IsStaff = laNhanVien,
                IsCustomer = laKhachHang,
                PrimaryRole = primaryRole
            };
        }

        /// <summary>
        /// Đăng ký tài khoản khách hàng mới với mật khẩu băm PBKDF2 an toàn.
        /// </summary>
        public AuthResult RegisterCustomer(string hoTen, string soDienThoai, string cccd, string email, string matKhau)
        {
            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(soDienThoai) || string.IsNullOrWhiteSpace(matKhau))
            {
                return new AuthResult { Success = false, ErrorMessage = "Vui lòng nhập đầy đủ các trường thông tin bắt buộc." };
            }

            string sdtTrim = soDienThoai.Trim();

            // Kiểm tra trùng SĐT
            if (_db.TAIKHOANs.Any(t => t.SDT.Trim() == sdtTrim))
            {
                return new AuthResult { Success = false, ErrorMessage = "Số điện thoại này đã được sử dụng." };
            }

            // Kiểm tra trùng Email nếu có nhập
            if (!string.IsNullOrWhiteSpace(email))
            {
                string emailTrim = email.Trim();
                if (_db.TAIKHOANs.Any(t => t.EMAIL != null && t.EMAIL.Trim().ToLower() == emailTrim.ToLower()))
                {
                    return new AuthResult { Success = false, ErrorMessage = "Địa chỉ email này đã được sử dụng." };
                }
            }

            string newMaKhach = GenerateNextCustomerId();

            var tk = new TAIKHOAN
            {
                MA_TAIKHOAN = newMaKhach,
                HOTEN = hoTen.Trim(),
                SDT = sdtTrim,
                EMAIL = !string.IsNullOrWhiteSpace(email) ? email.Trim() : null,
                MATKHAU = PasswordHelper.HashPassword(matKhau),
                NGAYTAO = DateTime.Now,
                TRANGTHAI = "HoatDong"
            };

            var vaiTro = _db.VAITROes.FirstOrDefault(v => v.MA_VAITRO.Trim() == "KHACHHANG" || v.TEN_VAITRO.Trim() == "KhachHang");
            if (vaiTro != null)
            {
                tk.VAITROes.Add(vaiTro);
            }

            _db.TAIKHOANs.Add(tk);

            var kh = new KHACHHANG
            {
                MA_KHACHHANG = newMaKhach,
                CCCD = !string.IsNullOrWhiteSpace(cccd) ? cccd.Trim() : null,
                TAIKHOAN = tk
            };

            _db.KHACHHANGs.Add(kh);
            _db.SaveChanges();

            return new AuthResult
            {
                Success = true,
                User = tk,
                Customer = kh,
                IsCustomer = true,
                PrimaryRole = "KHACHHANG"
            };
        }

        /// <summary>
        /// Đổi mật khẩu an toàn cho tài khoản.
        /// </summary>
        public bool ChangePassword(string maTaiKhoan, string matKhauCu, string matKhauMoi, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(maTaiKhoan) || string.IsNullOrWhiteSpace(matKhauCu) || string.IsNullOrWhiteSpace(matKhauMoi))
            {
                errorMessage = "Thông tin đổi mật khẩu không hợp lệ.";
                return false;
            }

            string maTrim = maTaiKhoan.Trim();
            var tk = _db.TAIKHOANs.FirstOrDefault(t => t.MA_TAIKHOAN.Trim() == maTrim);

            if (tk == null)
            {
                errorMessage = "Không tìm thấy thông tin tài khoản.";
                return false;
            }

            if (!PasswordHelper.VerifyPassword(matKhauCu, tk.MATKHAU, out _))
            {
                errorMessage = "Mật khẩu hiện tại không chính xác.";
                return false;
            }

            tk.MATKHAU = PasswordHelper.HashPassword(matKhauMoi);
            _db.SaveChanges();
            return true;
        }

        private string GenerateNextCustomerId()
        {
            var allIds = _db.KHACHHANGs.Select(k => k.MA_KHACHHANG).ToList();
            int maxId = 0;
            foreach (var id in allIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    string trimmed = id.Trim();
                    if (trimmed.StartsWith("KH", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(trimmed.Substring(2), out int currentId) && currentId > maxId)
                        {
                            maxId = currentId;
                        }
                    }
                }
            }
            return "KH" + (maxId + 1).ToString("D3");
        }

        private static bool CheckRole(VAITRO vaiTro, string roleCode)
        {
            if (vaiTro == null) return false;
            return (vaiTro.MA_VAITRO != null && vaiTro.MA_VAITRO.Trim().Equals(roleCode, StringComparison.OrdinalIgnoreCase))
                || (vaiTro.TEN_VAITRO != null && vaiTro.TEN_VAITRO.Trim().Equals(roleCode, StringComparison.OrdinalIgnoreCase));
        }

        public void Dispose()
        {
            if (_disposeDb)
            {
                _db?.Dispose();
            }
        }
    }
}

