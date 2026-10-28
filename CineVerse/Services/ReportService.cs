using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class MovieOccupancyItem
    {
        public string MaPhim { get; set; }
        public string TenPhim { get; set; }
        public decimal DoanhThu { get; set; }
        public int SoVeBan { get; set; }
        public int TongChoNgoi { get; set; }
        public double OccupancyRate { get; set; }
    }

    public class NameValueShareItem
    {
        public string Name { get; set; }
        public decimal Value { get; set; }
        public double Percentage { get; set; }
    }

    public class ExportOrderItem
    {
        public string MaDonHang { get; set; }
        public DateTime NgayDat { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public string TenPhim { get; set; }
        public string TenRap { get; set; }
        public string TenPhong { get; set; }
        public string GioChieu { get; set; }
        public int SoVe { get; set; }
        public string DanhSachGhe { get; set; }
        public decimal TienVe { get; set; }
        public decimal TienBapNuoc { get; set; }
        public decimal TienGiam { get; set; }
        public decimal TongTien { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public string TrangThaiThanhToan { get; set; }
        public string MaCheckIn { get; set; }
    }

    public class AnalyticsResult
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalTicketRevenue { get; set; }
        public decimal TotalFnBRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public double AverageOccupancyRate { get; set; }

        public List<string> DateLabels { get; set; } = new List<string>();
        public List<decimal> TicketRevenues { get; set; } = new List<decimal>();
        public List<decimal> FnBRevenues { get; set; } = new List<decimal>();
        public List<decimal> TotalRevenues { get; set; } = new List<decimal>();

        public List<MovieOccupancyItem> TopMovies { get; set; } = new List<MovieOccupancyItem>();
        public List<NameValueShareItem> CinemaShares { get; set; } = new List<NameValueShareItem>();
        public List<NameValueShareItem> PaymentShares { get; set; } = new List<NameValueShareItem>();
    }

    public class ReportService
    {
        private QL_VEPHIMEntities db;

        public ReportService()
        {
            this.db = new QL_VEPHIMEntities();
        }

        public ReportService(QL_VEPHIMEntities dbContext)
        {
            this.db = dbContext;
        }

        public decimal GetTotalRevenue()
        {
            return db.DONHANGs.Where(d => d.TRANGTHAI == "DaThanhToan").Sum(d => (decimal?)d.TONGTIEN) ?? 0;
        }

        public int GetTotalTickets()
        {
            return db.VEs.Count(v => v.TRANGTHAI != "Đã hủy");
        }

        public int GetTotalMovies()
        {
            return db.PHIMs.Count();
        }

        public int GetTotalCustomers()
        {
            return db.KHACHHANGs.Count();
        }

        public List<VE> GetRecentTickets(int count)
        {
            return db.VEs
                .Include(v => v.DONHANG.KHACHHANG.TAIKHOAN)
                .Include(v => v.LICHCHIEU.PHIM)
                .Include(v => v.GHE)
                .OrderByDescending(v => v.DONHANG.NGAYDAT)
                .Take(count)
                .ToList();
        }

        public AnalyticsResult GetAnalytics(string timeRange, string maRap, DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            var result = new AnalyticsResult();

            DateTime now = DateTime.Now;
            DateTime startDate;
            DateTime endDate = now.Date.AddDays(1).AddTicks(-1);

            string range = (timeRange ?? "7days").Trim().ToLowerInvariant();
            switch (range)
            {
                case "today":
                    startDate = now.Date;
                    break;
                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    break;
                case "custom":
                    startDate = tuNgay?.Date ?? now.Date.AddDays(-6);
                    if (denNgay.HasValue)
                    {
                        endDate = denNgay.Value.Date.AddDays(1).AddTicks(-1);
                    }
                    break;
                case "all":
                    startDate = new DateTime(2020, 1, 1);
                    break;
                case "7days":
                default:
                    startDate = now.Date.AddDays(-6);
                    break;
            }

            // Truy vấn đơn hàng đã thanh toán trong khoảng thời gian
            var ordersQuery = db.DONHANGs
                .Include(d => d.LICHCHIEU.PHIM)
                .Include(d => d.LICHCHIEU.PHONGCHIEU.RAPCHIEU)
                .Include(d => d.VEs)
                .Where(d => d.NGAYDAT >= startDate && d.NGAYDAT <= endDate && (d.TRANGTHAI == "DaThanhToan" || d.TRANGTHAITHANHTOAN == "DaThanhToan"));

            if (!string.IsNullOrEmpty(maRap))
            {
                string rId = maRap.Trim();
                ordersQuery = ordersQuery.Where(d => d.LICHCHIEU.PHONGCHIEU.MA_RAP.Trim() == rId);
            }

            var orders = ordersQuery.ToList();

            // Tổng quan
            result.TotalRevenue = orders.Sum(o => o.TONGTIEN);
            result.TotalTicketRevenue = orders.Sum(o => o.TIENVE);
            result.TotalFnBRevenue = orders.Sum(o => o.TIENMONAN);
            result.TotalTicketsSold = orders.SelectMany(o => o.VEs).Count(v => v.TRANGTHAI != "Đã hủy");

            // Biểu đồ 1: Doanh thu vé vs bắp nước theo từng ngày
            int daySpan = (int)(endDate.Date - startDate.Date).TotalDays;
            if (daySpan > 60) daySpan = 60; // Giới hạn tối đa 60 mốc nếu chọn "all"
            if (daySpan < 0) daySpan = 0;

            for (int i = 0; i <= daySpan; i++)
            {
                var curDate = startDate.Date.AddDays(i);
                if (curDate > endDate) break;

                string label = curDate.ToString("dd/MM");
                var dayOrders = orders.Where(o => o.NGAYDAT.Date == curDate).ToList();

                decimal dayTicket = dayOrders.Sum(o => o.TIENVE);
                decimal dayFnB = dayOrders.Sum(o => o.TIENMONAN);
                decimal dayTotal = dayOrders.Sum(o => o.TONGTIEN);

                result.DateLabels.Add(label);
                result.TicketRevenues.Add(dayTicket);
                result.FnBRevenues.Add(dayFnB);
                result.TotalRevenues.Add(dayTotal);
            }

            // Biểu đồ 2: Top 5 phim & Occupancy Rate
            var schedulesQuery = db.LICHCHIEUx
                .Include(l => l.PHIM)
                .Include(l => l.PHONGCHIEU.GHEs)
                .Include(l => l.VEs)
                .Where(l => l.NGAYCHIEU >= startDate && l.NGAYCHIEU <= endDate);

            if (!string.IsNullOrEmpty(maRap))
            {
                string rId = maRap.Trim();
                schedulesQuery = schedulesQuery.Where(l => l.PHONGCHIEU.MA_RAP.Trim() == rId);
            }

            var schedules = schedulesQuery.ToList();
            var moviesGrouped = schedules.GroupBy(s => s.PHIM).ToList();

            var movieStatsList = new List<MovieOccupancyItem>();
            foreach (var group in moviesGrouped)
            {
                if (group.Key == null) continue;
                string pId = group.Key.MA_PHIM.Trim();
                var pOrders = orders.Where(o => o.LICHCHIEU != null && o.LICHCHIEU.MA_PHIM.Trim() == pId).ToList();

                decimal pRevenue = pOrders.Sum(o => o.TONGTIEN);
                int pTickets = group.SelectMany(s => s.VEs).Count(v => v.TRANGTHAI != "Đã hủy");
                int pCapacity = group.Sum(s => s.PHONGCHIEU?.GHEs?.Count ?? 0);

                double rate = pCapacity > 0 ? Math.Round(((double)pTickets / pCapacity) * 100, 1) : 0;

                movieStatsList.Add(new MovieOccupancyItem
                {
                    MaPhim = pId,
                    TenPhim = group.Key.TEN_PHIM,
                    DoanhThu = pRevenue,
                    SoVeBan = pTickets,
                    TongChoNgoi = pCapacity,
                    OccupancyRate = Math.Min(100.0, rate)
                });
            }

            result.TopMovies = movieStatsList.OrderByDescending(m => m.DoanhThu).Take(5).ToList();

            // Tính Tỷ lệ lấp đầy ghế trung bình
            int totalCapacityAll = schedules.Sum(s => s.PHONGCHIEU?.GHEs?.Count ?? 0);
            result.AverageOccupancyRate = totalCapacityAll > 0
                ? Math.Round(((double)result.TotalTicketsSold / totalCapacityAll) * 100, 1)
                : 0;

            // Biểu đồ 3A: Tỷ trọng doanh thu theo Cụm rạp
            var cinemaGrouped = orders
                .Where(o => o.LICHCHIEU?.PHONGCHIEU?.RAPCHIEU != null)
                .GroupBy(o => o.LICHCHIEU.PHONGCHIEU.RAPCHIEU.TEN_RAP)
                .ToList();

            foreach (var cg in cinemaGrouped)
            {
                decimal rev = cg.Sum(o => o.TONGTIEN);
                double pct = result.TotalRevenue > 0 ? Math.Round((double)(rev / result.TotalRevenue) * 100, 1) : 0;
                result.CinemaShares.Add(new NameValueShareItem
                {
                    Name = cg.Key,
                    Value = rev,
                    Percentage = pct
                });
            }

            // Biểu đồ 3B: Tỷ trọng theo Phương thức thanh toán
            var paymentGrouped = orders.GroupBy(o => string.IsNullOrEmpty(o.PHUONGTHUCTHANHTOAN) ? "TaiQuay" : o.PHUONGTHUCTHANHTOAN.Trim()).ToList();
            foreach (var pg in paymentGrouped)
            {
                string ptName = pg.Key;
                if (ptName == "TaiQuay" || ptName == "TienMat") ptName = "Tiền mặt / Tại quầy";
                else if (ptName == "VNPay") ptName = "Cổng VNPay";
                else if (ptName == "VietQR") ptName = "Quét VietQR";

                decimal rev = pg.Sum(o => o.TONGTIEN);
                double pct = result.TotalRevenue > 0 ? Math.Round((double)(rev / result.TotalRevenue) * 100, 1) : 0;
                result.PaymentShares.Add(new NameValueShareItem
                {
                    Name = ptName,
                    Value = rev,
                    Percentage = pct
                });
            }

            return result;
        }

        public List<ExportOrderItem> GetExportData(string timeRange, string maRap, DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            DateTime now = DateTime.Now;
            DateTime startDate;
            DateTime endDate = now.Date.AddDays(1).AddTicks(-1);

            string range = (timeRange ?? "7days").Trim().ToLowerInvariant();
            switch (range)
            {
                case "today":
                    startDate = now.Date;
                    break;
                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    break;
                case "custom":
                    startDate = tuNgay?.Date ?? now.Date.AddDays(-6);
                    if (denNgay.HasValue)
                    {
                        endDate = denNgay.Value.Date.AddDays(1).AddTicks(-1);
                    }
                    break;
                case "all":
                    startDate = new DateTime(2020, 1, 1);
                    break;
                case "7days":
                default:
                    startDate = now.Date.AddDays(-6);
                    break;
            }

            var query = db.DONHANGs
                .Include(d => d.KHACHHANG.TAIKHOAN)
                .Include(d => d.LICHCHIEU.PHIM)
                .Include(d => d.LICHCHIEU.KHUNGGIO)
                .Include(d => d.LICHCHIEU.PHONGCHIEU.RAPCHIEU)
                .Include(d => d.VEs.Select(v => v.GHE))
                .Where(d => d.NGAYDAT >= startDate && d.NGAYDAT <= endDate && (d.TRANGTHAI == "DaThanhToan" || d.TRANGTHAITHANHTOAN == "DaThanhToan"));

            if (!string.IsNullOrEmpty(maRap))
            {
                string rId = maRap.Trim();
                query = query.Where(d => d.LICHCHIEU.PHONGCHIEU.MA_RAP.Trim() == rId);
            }

            var orders = query.OrderByDescending(d => d.NGAYDAT).ToList();

            var list = new List<ExportOrderItem>();
            foreach (var d in orders)
            {
                var gheNames = d.VEs.Where(v => v.GHE != null).Select(v => v.GHE.SOGHE.Trim()).ToList();
                string dsGhe = string.Join(", ", gheNames);

                string pt = d.PHUONGTHUCTHANHTOAN ?? "TaiQuay";
                if (pt == "TaiQuay" || pt == "TienMat") pt = "Tiền mặt / Tại quầy";

                list.Add(new ExportOrderItem
                {
                    MaDonHang = d.MA_DONHANG.Trim(),
                    NgayDat = d.NGAYDAT,
                    TenKhachHang = d.KHACHHANG?.TAIKHOAN?.HOTEN ?? "Khách vãng lai",
                    SoDienThoai = d.KHACHHANG?.TAIKHOAN?.SDT ?? "",
                    TenPhim = d.LICHCHIEU?.PHIM?.TEN_PHIM ?? "",
                    TenRap = d.LICHCHIEU?.PHONGCHIEU?.RAPCHIEU?.TEN_RAP ?? "",
                    TenPhong = d.LICHCHIEU?.PHONGCHIEU?.TEN_PHONG ?? "",
                    GioChieu = d.LICHCHIEU != null && d.LICHCHIEU.KHUNGGIO != null ? d.LICHCHIEU.KHUNGGIO.GIOBATDAU.ToString(@"hh\:mm") : "",
                    SoVe = d.VEs.Count,
                    DanhSachGhe = dsGhe,
                    TienVe = d.TIENVE,
                    TienBapNuoc = d.TIENMONAN,
                    TienGiam = d.TIENGIAM,
                    TongTien = d.TONGTIEN,
                    PhuongThucThanhToan = pt,
                    TrangThaiThanhToan = "Đã thanh toán",
                    MaCheckIn = d.MACHECKIN ?? ""
                });
            }

            return list;
        }
    }
}
