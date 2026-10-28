using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    /// <summary>
    /// Thông tin giữ ghế tạm thời trong bộ nhớ.
    /// </summary>
    public class SeatHoldItem
    {
        public string MaLichChieu { get; set; }
        public string MaGhe { get; set; }
        public string HolderId { get; set; }
        public DateTime HoldTime { get; set; }
        public DateTime ExpireAt { get; set; }

        public bool IsExpired => DateTime.Now >= ExpireAt;

        public int RemainingSeconds
        {
            get
            {
                var diff = (int)(ExpireAt - DateTime.Now).TotalSeconds;
                return diff > 0 ? diff : 0;
            }
        }
    }

    /// <summary>
    /// Service quản lý Giữ ghế Tạm thời theo Thời gian thực (Real-time Seat Locking).
    /// Thiết kế Singleton, Thread-Safe với ConcurrentDictionary, tự động giải phóng ghế sau 7 phút.
    /// </summary>
    public class SeatHoldService
    {
        private static readonly Lazy<SeatHoldService> _instance = new Lazy<SeatHoldService>(() => new SeatHoldService());
        public static SeatHoldService Instance => _instance.Value;

        // Thời gian giữ ghế mặc định: 7 phút (420 giây)
        public const int DEFAULT_HOLD_MINUTES = 7;

        // Key: $"{MaLichChieu.Trim()}_{MaGhe.Trim()}" -> SeatHoldItem
        private readonly ConcurrentDictionary<string, SeatHoldItem> _holds = new ConcurrentDictionary<string, SeatHoldItem>();

        private readonly object _lockObj = new object();

        private SeatHoldService() { }

        private string BuildKey(string maLichChieu, string maGhe)
        {
            return $"{maLichChieu?.Trim().ToUpper()}_{maGhe?.Trim().ToUpper()}";
        }

        /// <summary>
        /// Dọn dẹp các ghế đã hết hạn giữ.
        /// </summary>
        public void CleanupExpired()
        {
            var now = DateTime.Now;
            foreach (var kvp in _holds)
            {
                if (kvp.Value.ExpireAt <= now)
                {
                    _holds.TryRemove(kvp.Key, out _);
                }
            }
        }

        /// <summary>
        /// Khóa nguyên tử (Atomic Check-and-Lock) danh sách ghế cho một phiên (Session/Khách hàng).
        /// </summary>
        public bool HoldSeats(string maLichChieu, IEnumerable<string> seatIds, string holderId, out string errorMessage, out int remainingSeconds)
        {
            errorMessage = string.Empty;
            remainingSeconds = 0;

            if (string.IsNullOrWhiteSpace(maLichChieu) || seatIds == null || string.IsNullOrWhiteSpace(holderId))
            {
                errorMessage = "Thông tin suất chiếu hoặc ghế không hợp lệ.";
                return false;
            }

            var cleanSeats = seatIds.Where(s => !string.IsNullOrWhiteSpace(s))
                                    .Select(s => s.Trim().ToUpper())
                                    .Distinct()
                                    .ToList();

            if (!cleanSeats.Any())
            {
                errorMessage = "Vui lòng chọn ít nhất một ghế.";
                return false;
            }

            maLichChieu = maLichChieu.Trim().ToUpper();
            holderId = holderId.Trim();

            lock (_lockObj)
            {
                CleanupExpired();

                // 1. Kiểm tra xem có ghế nào đang bị người khác giữ không
                var conflictingSeats = new List<string>();
                foreach (var maGhe in cleanSeats)
                {
                    string key = BuildKey(maLichChieu, maGhe);
                    if (_holds.TryGetValue(key, out var existingHold))
                    {
                        if (!existingHold.IsExpired && !string.Equals(existingHold.HolderId, holderId, StringComparison.OrdinalIgnoreCase))
                        {
                            conflictingSeats.Add(maGhe);
                        }
                    }
                }

                if (conflictingSeats.Any())
                {
                    errorMessage = $"Rất tiếc! Ghế [{string.Join(", ", conflictingSeats)}] vừa được khách hàng khác giữ chỗ. Vui lòng chọn ghế khác.";
                    return false;
                }

                // 2. Nhả những ghế cũ của holderId ở suất chiếu này mà không nằm trong danh sách mới
                var currentKeysOfHolder = _holds.Where(kvp => kvp.Value.MaLichChieu == maLichChieu &&
                                                              string.Equals(kvp.Value.HolderId, holderId, StringComparison.OrdinalIgnoreCase))
                                                .Select(kvp => kvp.Key)
                                                .ToList();

                var newKeys = cleanSeats.Select(s => BuildKey(maLichChieu, s)).ToHashSet();

                foreach (var oldKey in currentKeysOfHolder)
                {
                    if (!newKeys.Contains(oldKey))
                    {
                        _holds.TryRemove(oldKey, out _);
                    }
                }

                // 3. Cấp mới hoặc gia hạn 7 phút cho các ghế được chọn
                var now = DateTime.Now;
                var expireAt = now.AddMinutes(DEFAULT_HOLD_MINUTES);

                foreach (var maGhe in cleanSeats)
                {
                    string key = BuildKey(maLichChieu, maGhe);
                    var holdItem = new SeatHoldItem
                    {
                        MaLichChieu = maLichChieu,
                        MaGhe = maGhe,
                        HolderId = holderId,
                        HoldTime = now,
                        ExpireAt = expireAt
                    };
                    _holds.AddOrUpdate(key, holdItem, (k, old) => holdItem);
                }

                remainingSeconds = (int)(expireAt - now).TotalSeconds;
                return true;
            }
        }

        /// <summary>
        /// Lấy danh sách các ghế đang bị người khác giữ và ghế do chính mình giữ.
        /// </summary>
        public void GetSeatHoldStatus(
            string maLichChieu,
            string currentHolderId,
            out List<string> otherHeldSeats,
            out List<string> myHeldSeats,
            out int myRemainingSeconds)
        {
            otherHeldSeats = new List<string>();
            myHeldSeats = new List<string>();
            myRemainingSeconds = 0;

            if (string.IsNullOrWhiteSpace(maLichChieu)) return;
            maLichChieu = maLichChieu.Trim().ToUpper();
            currentHolderId = currentHolderId?.Trim() ?? string.Empty;

            CleanupExpired();

            var holdsForShow = _holds.Values.Where(h => h.MaLichChieu == maLichChieu && !h.IsExpired).ToList();

            foreach (var hold in holdsForShow)
            {
                if (!string.IsNullOrEmpty(currentHolderId) &&
                    string.Equals(hold.HolderId, currentHolderId, StringComparison.OrdinalIgnoreCase))
                {
                    myHeldSeats.Add(hold.MaGhe);
                    if (hold.RemainingSeconds > myRemainingSeconds)
                    {
                        myRemainingSeconds = hold.RemainingSeconds;
                    }
                }
                else
                {
                    otherHeldSeats.Add(hold.MaGhe);
                }
            }
        }

        /// <summary>
        /// Lấy số giây còn lại của phiên giữ ghế hiện tại.
        /// </summary>
        public int GetRemainingSeconds(string maLichChieu, string holderId)
        {
            if (string.IsNullOrWhiteSpace(maLichChieu) || string.IsNullOrWhiteSpace(holderId))
                return 0;

            maLichChieu = maLichChieu.Trim().ToUpper();
            holderId = holderId.Trim();

            CleanupExpired();

            var maxRemaining = _holds.Values
                .Where(h => h.MaLichChieu == maLichChieu &&
                            string.Equals(h.HolderId, holderId, StringComparison.OrdinalIgnoreCase) &&
                            !h.IsExpired)
                .Select(h => h.RemainingSeconds)
                .DefaultIfEmpty(0)
                .Max();

            return maxRemaining;
        }

        /// <summary>
        /// Giải phóng (nhả) toàn bộ ghế của một khách hàng trong một suất chiếu.
        /// </summary>
        public void ReleaseSeats(string maLichChieu, string holderId)
        {
            if (string.IsNullOrWhiteSpace(maLichChieu) || string.IsNullOrWhiteSpace(holderId))
                return;

            maLichChieu = maLichChieu.Trim().ToUpper();
            holderId = holderId.Trim();

            var keysToRemove = _holds.Where(kvp => kvp.Value.MaLichChieu == maLichChieu &&
                                                   string.Equals(kvp.Value.HolderId, holderId, StringComparison.OrdinalIgnoreCase))
                                     .Select(kvp => kvp.Key)
                                     .ToList();

            foreach (var k in keysToRemove)
            {
                _holds.TryRemove(k, out _);
            }
        }

        /// <summary>
        /// Kiểm tra quy tắc Ghế đôi và Quy tắc Ghế trống đơn mồ côi (Single-seat gap rule).
        /// </summary>
        /// <param name="allSeatsInRoom">Tất cả ghế trong phòng chiếu</param>
        /// <param name="selectedSeatIds">Danh sách mã ghế khách đang chọn</param>
        /// <param name="soldOrHeldSeatIds">Danh sách mã ghế đã bán hoặc đang bị người khác giữ</param>
        /// <param name="warningMessage">Thông điệp cảnh báo nếu vi phạm quy tắc</param>
        /// <returns>True nếu hợp lệ, False nếu vi phạm quy tắc rạp</returns>
        public static bool ValidateSeatRules(
            IEnumerable<GHE> allSeatsInRoom,
            IEnumerable<string> selectedSeatIds,
            IEnumerable<string> soldOrHeldSeatIds,
            out string warningMessage)
        {
            warningMessage = string.Empty;

            if (allSeatsInRoom == null || selectedSeatIds == null)
            {
                return true;
            }

            var selectedSet = new HashSet<string>(selectedSeatIds.Select(s => s.Trim().ToUpper()));
            if (!selectedSet.Any())
            {
                return true;
            }

            var soldOrHeldSet = new HashSet<string>((soldOrHeldSeatIds ?? Enumerable.Empty<string>()).Select(s => s.Trim().ToUpper()));

            // 1. Kiểm tra quy tắc Ghế Đôi (Couple seat rule)
            // Ghế đôi phải chọn theo cặp (nếu số ghế đôi được chọn là số lẻ thì cảnh báo)
            var selectedCoupleSeats = allSeatsInRoom
                .Where(g => selectedSet.Contains(g.MA_GHE.Trim().ToUpper()) &&
                            (g.MA_LOAIGHE?.Trim() == "LG03" || (g.LOAIGHE?.TEN_LOAIGHE != null && g.LOAIGHE.TEN_LOAIGHE.Contains("Đôi"))))
                .ToList();

            if (selectedCoupleSeats.Count % 2 != 0)
            {
                warningMessage = "Ghế đôi (Sweetbox) bắt buộc phải đặt theo cặp 2 người liền kề. Vui lòng chọn đủ cặp ghế đôi.";
                return false;
            }

            // 2. Kiểm tra quy tắc Single-Seat Gap (không để trống 1 ghế đơn độc)
            // Nhóm ghế theo từng hàng ghế (SO_HANG)
            var seatsByRow = allSeatsInRoom.GroupBy(g => g.SO_HANG).OrderBy(g => g.Key);

            foreach (var rowGroup in seatsByRow)
            {
                var rowSeats = rowGroup.OrderBy(g => g.SO_COT).ToList();
                int n = rowSeats.Count;
                if (n <= 2) continue;

                // Mảng trạng thái sau khi khách chọn:
                // true = Đã có người ngồi (đã bán, người khác giữ, hoặc bạn chọn)
                // false = Trống
                bool[] isOccupied = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    string maGhe = rowSeats[i].MA_GHE.Trim().ToUpper();
                    isOccupied[i] = selectedSet.Contains(maGhe) || soldOrHeldSet.Contains(maGhe);
                }

                // Kiểm tra xem hàng này có ghế nào được user chọn không
                bool userHasSeatInRow = rowSeats.Any(g => selectedSet.Contains(g.MA_GHE.Trim().ToUpper()));
                if (!userHasSeatInRow) continue;

                // Kiểm tra ghế trống ở vị trí đầu dãy (ghế 0 trống, ghế 1 bị chiếm)
                if (!isOccupied[0] && isOccupied[1])
                {
                    // Nếu ghế 0 trống và ghế 1 có người -> Ghế 0 bị mồ côi sát rìa lối đi/tường
                    string tenGhe = rowSeats[0].SOGHE?.Trim();
                    warningMessage = $"Không thể để trống duy nhất 1 ghế sát rìa ({tenGhe}). Vui lòng chọn ghế đó hoặc chọn vị trí khác.";
                    return false;
                }

                // Kiểm tra ghế trống ở vị trí cuối dãy (ghế n-1 trống, ghế n-2 bị chiếm)
                if (!isOccupied[n - 1] && isOccupied[n - 2])
                {
                    string tenGhe = rowSeats[n - 1].SOGHE?.Trim();
                    warningMessage = $"Không thể để trống duy nhất 1 ghế sát rìa ({tenGhe}). Vui lòng chọn ghế đó hoặc chọn vị trí khác.";
                    return false;
                }

                // Kiểm tra ghế trống kẹp ở giữa: [Occupied] [Empty] [Occupied]
                for (int i = 1; i < n - 1; i++)
                {
                    if (!isOccupied[i] && isOccupied[i - 1] && isOccupied[i + 1])
                    {
                        string tenGhe = rowSeats[i].SOGHE?.Trim();
                        warningMessage = $"Không thể để trống duy nhất 1 ghế đơn ({tenGhe}) ở giữa các vị trí đã chọn. Vui lòng chọn ghế liền kề hoặc cách từ 2 ghế trở lên.";
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

