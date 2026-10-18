using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class SeatService
    {
        private QL_VEPHIMEntities db;

        public SeatService()
        {
            this.db = new QL_VEPHIMEntities();
        }

        public SeatService(QL_VEPHIMEntities dbContext)
        {
            this.db = dbContext;
        }

        public List<GHE> GetSeatsByRoom(string maPhong)
        {
            return db.GHEs.Include(g => g.LOAIGHE).Where(g => g.MA_PHONG == maPhong).ToList();
        }

        public List<string> GetBookedSeatIds(string maLichChieu)
        {
            // Only get seats that are "Đã đặt" / "DaThanhToan" or any valid status
            // Depending on current DB logic, it just checks existence in VE table
            return db.VEs.Where(v => v.MA_LICHCHIEU == maLichChieu).Select(v => v.MA_GHE).ToList();
        }

        public List<GHE> GetSeatsByIds(List<string> seatIds)
        {
            return db.GHEs.Include(g => g.LOAIGHE).Where(g => seatIds.Contains(g.MA_GHE)).ToList();
        }

        public GHE GetSeatById(string maGhe)
        {
            return db.GHEs.Include(g => g.LOAIGHE).FirstOrDefault(g => g.MA_GHE == maGhe);
        }
    }
}
