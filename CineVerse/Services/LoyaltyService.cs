using System;
using System.Linq;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class TierProgressInfo
    {
        public string CurrentTier { get; set; }
        public string NextTier { get; set; }
        public int CurrentPoints { get; set; }
        public int PointsNeededForNext { get; set; }
        public int ProgressPercent { get; set; }
        public decimal DiscountRate { get; set; }
    }

    public class LoyaltyService
    {
        private static readonly Lazy<LoyaltyService> _instance = new Lazy<LoyaltyService>(() => new LoyaltyService());
        public static LoyaltyService Instance => _instance.Value;

        public const string TIER_MEMBER = "Member";
        public const string TIER_VIP = "VIP";
        public const string TIER_VVIP = "VVIP";

        public const int POINTS_VIP_THRESHOLD = 500;
        public const int POINTS_VVIP_THRESHOLD = 1500;

        /// <summary>
        /// Tính tỷ lệ giảm giá theo hạng thành viên (VIP: 5%, VVIP: 10%)
        /// </summary>
        public decimal GetDiscountRate(string hangThanhVien)
        {
            if (string.IsNullOrEmpty(hangThanhVien)) return 0m;
            string tier = hangThanhVien.Trim().ToUpperInvariant();
            if (tier == "VVIP") return 0.10m;
            if (tier == "VIP") return 0.05m;
            return 0m;
        }

        /// <summary>
        /// Xác định hạng thành viên dựa trên tổng điểm tích lũy
        /// </summary>
        public string DetermineTier(int points)
        {
            if (points >= POINTS_VVIP_THRESHOLD) return TIER_VVIP;
            if (points >= POINTS_VIP_THRESHOLD) return TIER_VIP;
            return TIER_MEMBER;
        }

        /// <summary>
        /// Tính điểm thưởng từ số tiền thanh toán (Quy đổi 5% giá trị thanh toán / 100)
        /// Ví dụ: 100.000đ -> +50 điểm. 1.000.000đ -> +500 điểm (Đạt VIP).
        /// </summary>
        public int CalculateEarnedPoints(decimal amount)
        {
            if (amount <= 0) return 0;
            int points = (int)Math.Round(amount * 0.05m / 100m, MidpointRounding.AwayFromZero);
            return Math.Max(1, points);
        }

        /// <summary>
        /// Tự động cộng điểm và cập nhật hạng thành viên khi thanh toán thành công
        /// </summary>
        public bool CongDiemVaXetHang(string maKhachHang, decimal tongTienThanhToan, QL_VEPHIMEntities dbContext = null)
        {
            if (string.IsNullOrEmpty(maKhachHang) || tongTienThanhToan <= 0) return false;

            bool isSelfOwned = dbContext == null;
            var db = dbContext ?? new QL_VEPHIMEntities();

            try
            {
                string khId = maKhachHang.Trim();
                var kh = db.KHACHHANGs.FirstOrDefault(k => k.MA_KHACHHANG.Trim() == khId);
                if (kh == null) return false;

                int diemCong = CalculateEarnedPoints(tongTienThanhToan);
                kh.DIEMTICHLUY += diemCong;
                kh.HANGTHANHVIEN = DetermineTier(kh.DIEMTICHLUY);

                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (isSelfOwned)
                {
                    db.Dispose();
                }
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết tiến trình thăng hạng của khách hàng
        /// </summary>
        public TierProgressInfo GetProgressInfo(KHACHHANG khachHang)
        {
            int points = khachHang != null ? khachHang.DIEMTICHLUY : 0;
            string currentTier = khachHang != null && !string.IsNullOrEmpty(khachHang.HANGTHANHVIEN)
                ? khachHang.HANGTHANHVIEN.Trim()
                : DetermineTier(points);

            var info = new TierProgressInfo
            {
                CurrentPoints = points,
                CurrentTier = currentTier,
                DiscountRate = GetDiscountRate(currentTier)
            };

            if (points < POINTS_VIP_THRESHOLD)
            {
                info.NextTier = TIER_VIP;
                info.PointsNeededForNext = POINTS_VIP_THRESHOLD - points;
                info.ProgressPercent = (int)Math.Min(100, Math.Round((points / (double)POINTS_VIP_THRESHOLD) * 100));
            }
            else if (points < POINTS_VVIP_THRESHOLD)
            {
                info.NextTier = TIER_VVIP;
                info.PointsNeededForNext = POINTS_VVIP_THRESHOLD - points;
                int pointsInTier = points - POINTS_VIP_THRESHOLD;
                int tierSpan = POINTS_VVIP_THRESHOLD - POINTS_VIP_THRESHOLD; // 1000
                info.ProgressPercent = (int)Math.Min(100, Math.Round((pointsInTier / (double)tierSpan) * 100));
            }
            else
            {
                info.NextTier = "Tối đa (Highest Tier)";
                info.PointsNeededForNext = 0;
                info.ProgressPercent = 100;
            }

            return info;
        }
    }
}

