using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class ReportService
    {
        private QL_VEPHIM2Entities db;

        public ReportService()
        {
            this.db = new QL_VEPHIM2Entities();
        }

        public ReportService(QL_VEPHIM2Entities dbContext)
        {
            this.db = dbContext;
        }

        public decimal GetTotalRevenue()
        {
            return db.DONHANGs.Sum(d => (decimal?)d.TONGTIEN) ?? 0;
        }

        public int GetTotalTickets()
        {
            return db.VEs.Count();
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
            return db.VEs.OrderByDescending(v => v.DONHANG.NGAYDAT).Take(count).ToList();
        }
    }
}
