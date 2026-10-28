using System;
using System.Security.Cryptography;
using System.Text;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Helpers
{
    /// <summary>
    /// Helper xử lý băm mật khẩu chuẩn PBKDF2 (Rfc2898DeriveBytes) tích hợp sẵn trong .NET Framework.
    /// Hỗ trợ cơ chế tương thích ngược (Auto-upgrade hash) cho các tài khoản đang lưu mật khẩu plain-text.
    /// </summary>
    public static class PasswordHelper
    {
        private const int SaltSize = 16;       // 128 bit
        private const int SubkeySize = 32;     // 256 bit
        private const int DefaultIterations = 10000;
        private const string Prefix = "PBKDF2";

        /// <summary>
        /// Băm mật khẩu theo chuẩn PBKDF2 với Salt ngẫu nhiên và 10,000 vòng lặp.
        /// Định dạng chuỗi xuất ra: PBKDF2$10000$saltBase64$hashBase64
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password), "Mật khẩu không được để trống.");
            }

            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            byte[] subkey;
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, DefaultIterations))
            {
                subkey = deriveBytes.GetBytes(SubkeySize);
            }

            string saltBase64 = Convert.ToBase64String(salt);
            string subkeyBase64 = Convert.ToBase64String(subkey);

            return $"{Prefix}${DefaultIterations}${saltBase64}${subkeyBase64}";
        }

        /// <summary>
        /// Xác thực mật khẩu nhập vào với chuỗi hash trong CSDL.
        /// Hỗ trợ tương thích ngược: Nếu mật khẩu trong CSDL đang lưu dạng plain-text cũ,
        /// hàm sẽ so sánh trực tiếp và bật cờ needsUpgrade = true để caller cập nhật chuỗi băm mới.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash, out bool needsUpgrade)
        {
            needsUpgrade = false;

            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            // Kiểm tra xem mật khẩu có phải định dạng PBKDF2 của hệ thống không
            if (storedHash.StartsWith(Prefix + "$", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = storedHash.Split('$');
                if (parts.Length == 4)
                {
                    if (int.TryParse(parts[1], out int iterations))
                    {
                        try
                        {
                            byte[] salt = Convert.FromBase64String(parts[2]);
                            byte[] expectedSubkey = Convert.FromBase64String(parts[3]);

                            byte[] actualSubkey;
                            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations))
                            {
                                actualSubkey = deriveBytes.GetBytes(expectedSubkey.Length);
                            }

                            bool matches = FixedTimeEquals(actualSubkey, expectedSubkey);

                            // Nếu số vòng lặp cũ thấp hơn cấu hình mặc định hiện tại thì cần nâng cấp
                            if (matches && iterations < DefaultIterations)
                            {
                                needsUpgrade = true;
                            }

                            return matches;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }
            }

            // Mật khẩu lưu dạng plain-text cũ trong DB (như 123456, hash_admin_pw...)
            if (string.Equals(password, storedHash, StringComparison.Ordinal))
            {
                // Mật khẩu khớp -> Cần nâng cấp ngay sang chuỗi băm an toàn
                needsUpgrade = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// So sánh hai mảng byte trong thời gian không đổi (Constant-time comparison) để chống Side-channel / Timing attack.
        /// </summary>
        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }
    }
}

