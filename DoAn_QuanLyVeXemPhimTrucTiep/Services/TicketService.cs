using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class TicketService
    {
        private QL_VEPHIM2Entities db;

        public TicketService()
        {
            this.db = new QL_VEPHIM2Entities();
        }

        public TicketService(QL_VEPHIM2Entities dbContext)
        {
            this.db = dbContext;
        }

        public List<VE> GetAllTickets(string searchSdt = null, string searchMaLich = null)
        {
            var ves = db.VEs.Include(v => v.DONHANG.KHACHHANG).Include(v => v.LICHCHIEU).Include(v => v.GHE).AsQueryable();

            if (!string.IsNullOrEmpty(searchSdt))
            {
                ves = ves.Where(v => v.DONHANG != null && v.DONHANG.KHACHHANG != null && v.DONHANG.KHACHHANG.SDT.Contains(searchSdt));
            }
            if (!string.IsNullOrEmpty(searchMaLich))
            {
                ves = ves.Where(v => v.MA_LICHCHIEU.Contains(searchMaLich));
            }

            return ves.OrderByDescending(v => v.DONHANG.NGAYDAT).ToList();
        }

        public void UpdateTicketStatus(string id, string status)
        {
            var ve = db.VEs.FirstOrDefault(v => v.MA_VE == id);
            if (ve != null)
            {
                ve.TRANGTHAI = status;
                db.SaveChanges();
            }
        }

        public void DeleteTicket(string id)
        {
            var ve = db.VEs.FirstOrDefault(v => v.MA_VE == id);
            if (ve != null)
            {
                db.VEs.Remove(ve);
                db.SaveChanges();
            }
        }
    }
}
