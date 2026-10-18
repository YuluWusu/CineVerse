using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class TicketService
    {
        private QL_VEPHIMEntities db;

        public TicketService()
        {
            this.db = new QL_VEPHIMEntities();
        }

        public TicketService(QL_VEPHIMEntities dbContext)
        {
            this.db = dbContext;
        }

        public List<VE> GetAllTickets(string searchSdt = null, string searchMaLich = null)
        {
            var ves = db.VEs
                .Include(v => v.DONHANG.KHACHHANG.TAIKHOAN)
                .Include(v => v.LICHCHIEU.PHIM)
                .Include(v => v.LICHCHIEU.KHUNGGIO)
                .Include(v => v.LICHCHIEU.PHONGCHIEU.RAPCHIEU)
                .Include(v => v.GHE.LOAIGHE)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchSdt))
            {
                string sdtTrim = searchSdt.Trim();
                ves = ves.Where(v => v.DONHANG != null &&
                                     v.DONHANG.KHACHHANG != null &&
                                     v.DONHANG.KHACHHANG.TAIKHOAN != null &&
                                     v.DONHANG.KHACHHANG.TAIKHOAN.SDT.Contains(sdtTrim));
            }
            if (!string.IsNullOrEmpty(searchMaLich))
            {
                string lichTrim = searchMaLich.Trim();
                ves = ves.Where(v => v.MA_LICHCHIEU.Contains(lichTrim));
            }

            return ves.OrderByDescending(v => v.DONHANG.NGAYDAT).ToList();
        }

        public void UpdateTicketStatus(string id, string status)
        {
            if (string.IsNullOrEmpty(id)) return;
            string trimmed = id.Trim();
            var ve = db.VEs.FirstOrDefault(v => v.MA_VE.Trim() == trimmed);
            if (ve != null)
            {
                ve.TRANGTHAI = status;
                db.SaveChanges();
            }
        }

        public void DeleteTicket(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            string trimmed = id.Trim();
            var ve = db.VEs.FirstOrDefault(v => v.MA_VE.Trim() == trimmed);
            if (ve != null)
            {
                db.VEs.Remove(ve);
                db.SaveChanges();
            }
        }
    }
}
