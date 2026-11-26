
using System;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class PasswordResetService
    {
        private static readonly MemoryCache Cache = MemoryCache.Default;

        private static readonly object LockObject = new object();

        // Thời gian token có hiệu lực: 15 phút
        private static readonly TimeSpan TokenExpiration =
            TimeSpan.FromMinutes(15);

        // Tạo token ngẫu nhiên
        public string CreateToken(string maTaiKhoan)
        {
            if (string.IsNullOrWhiteSpace(maTaiKhoan))
                throw new ArgumentException("Mã tài khoản không hợp lệ.");

            byte[] randomBytes = new byte[32];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Chuyển sang Base64 URL-safe
            string token = Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            // Chỉ lưu mã băm của token trong Cache
            string tokenHash = HashToken(token);

            Cache.Set(
                tokenHash,
                maTaiKhoan,
                new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.Now.Add(TokenExpiration)
                }
            );

            return token;
        }

        // Kiểm tra token còn hợp lệ hay không
        public bool TryGetAccountId(string token, out string maTaiKhoan)
        {
            maTaiKhoan = null;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            string tokenHash = HashToken(token);

            var accountId = Cache.Get(tokenHash) as string;

            if (string.IsNullOrWhiteSpace(accountId))
                return false;

            maTaiKhoan = accountId;

            return true;
        }

        // Sử dụng token một lần duy nhất
        public bool TryConsumeToken(string token, out string maTaiKhoan)
        {
            maTaiKhoan = null;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            string tokenHash = HashToken(token);

            lock (LockObject)
            {
                var accountId = Cache.Get(tokenHash) as string;

                if (string.IsNullOrWhiteSpace(accountId))
                    return false;

                // Xóa token ngay sau khi sử dụng
                Cache.Remove(tokenHash);

                maTaiKhoan = accountId;

                return true;
            }
        }

        // Băm token bằng SHA256
        private string HashToken(string token)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(token);
                byte[] hash = sha256.ComputeHash(bytes);

                return BitConverter.ToString(hash).Replace("-", "");
            }
        }
    }
}
