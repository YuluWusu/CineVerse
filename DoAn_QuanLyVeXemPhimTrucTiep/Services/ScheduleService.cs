using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class ScheduleService
    {
        private QL_VEPHIM2Entities db;

        public ScheduleService()
        {
            this.db = new QL_VEPHIM2Entities();
        }

        public ScheduleService(QL_VEPHIM2Entities dbContext)
        {
            this.db = dbContext;
        }

        // ========================== LICHCHIEU ==========================
        public List<LICHCHIEU> GetAllSchedules(string maPhim = null)
        {
            var lichchieus = db.LICHCHIEUx.Include(l => l.PHIM).Include(l => l.PHONGCHIEU).Include(l => l.KHUNGGIO).AsQueryable();
            
            if (!string.IsNullOrEmpty(maPhim))
            {
                lichchieus = lichchieus.Where(l => l.MA_PHIM == maPhim);
            }
            
            return lichchieus.OrderByDescending(l => l.NGAYCHIEU).ToList();
        }

        public LICHCHIEU GetScheduleById(string id)
        {
            return db.LICHCHIEUx.FirstOrDefault(l => l.MA_LICHCHIEU == id);
        }

        public void AddSchedule(LICHCHIEU lichChieu)
        {
            db.LICHCHIEUx.Add(lichChieu);
            db.SaveChanges();
        }

        public void UpdateSchedule(LICHCHIEU lichChieu)
        {
            var l = db.LICHCHIEUx.FirstOrDefault(x => x.MA_LICHCHIEU == lichChieu.MA_LICHCHIEU);
            if (l != null)
            {
                l.MA_PHIM = lichChieu.MA_PHIM;
                l.MA_PHONG = lichChieu.MA_PHONG;
                l.MA_KHUNGGIO = lichChieu.MA_KHUNGGIO;
                l.NGAYCHIEU = lichChieu.NGAYCHIEU;
                l.GIATVE_PHUTROI = lichChieu.GIATVE_PHUTROI;
                db.SaveChanges();
            }
        }

        public void DeleteSchedule(string id)
        {
            var l = db.LICHCHIEUx.FirstOrDefault(x => x.MA_LICHCHIEU == id);
            if (l != null)
            {
                db.LICHCHIEUx.Remove(l);
                db.SaveChanges();
            }
        }

        public string GenerateNextScheduleId()
        {
            string currentMax = db.LICHCHIEUx.Max(l => l.MA_LICHCHIEU);
            int nextId = 1;
            if (!string.IsNullOrEmpty(currentMax) && currentMax.StartsWith("LC"))
            {
                if (int.TryParse(currentMax.Substring(2), out int currId))
                {
                    nextId = currId + 1;
                }
            }
            return "LC" + nextId.ToString("D3");
        }

        public bool IsDuplicateSchedule(string maPhong, string maKhungGio, DateTime? ngayChieu, string excludeId = null)
        {
            var query = db.LICHCHIEUx.Where(l => 
                l.MA_PHONG == maPhong && 
                l.MA_KHUNGGIO == maKhungGio && 
                l.NGAYCHIEU == ngayChieu);
                
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(l => l.MA_LICHCHIEU != excludeId);
            }
            
            return query.Any();
        }

        public bool CanDeleteSchedule(string id)
        {
            return !db.VEs.Any(v => v.MA_LICHCHIEU == id);
        }

        // ========================== KHUNGGIO ==========================
        public List<KHUNGGIO> GetAllTimeSlots()
        {
            return db.KHUNGGIOs.ToList();
        }

        public KHUNGGIO GetTimeSlotById(string id)
        {
            return db.KHUNGGIOs.FirstOrDefault(k => k.MA_KHUNGGIO == id);
        }

        public void AddTimeSlot(string gioBatDau, string gioKetThuc)
        {
            var allIds = db.KHUNGGIOs.Select(k => k.MA_KHUNGGIO).ToList();
            int maxId = 0;
            foreach (var i in allIds)
            {
                if (!string.IsNullOrEmpty(i) && i.StartsWith("KG"))
                {
                    if (int.TryParse(i.Substring(2), out int cur) && cur > maxId)
                        maxId = cur;
                }
            }
            string maKg = "KG" + (maxId + 1).ToString("D2");

            TimeSpan tsBatDau = TimeSpan.Parse(gioBatDau);
            TimeSpan tsKetThuc = TimeSpan.Parse(gioKetThuc);

            var kg = new KHUNGGIO
            {
                MA_KHUNGGIO = maKg,
                GIOBATDAU = tsBatDau,
                GIOKETTHUC = tsKetThuc
            };
            db.KHUNGGIOs.Add(kg);
            db.SaveChanges();
        }

        public void UpdateTimeSlot(string id, string gioBatDau, string gioKetThuc)
        {
            var kg = db.KHUNGGIOs.FirstOrDefault(k => k.MA_KHUNGGIO == id);
            if (kg != null)
            {
                kg.GIOBATDAU = TimeSpan.Parse(gioBatDau);
                kg.GIOKETTHUC = TimeSpan.Parse(gioKetThuc);
                db.SaveChanges();
            }
        }

        public void DeleteTimeSlot(string id)
        {
            var kg = db.KHUNGGIOs.FirstOrDefault(k => k.MA_KHUNGGIO == id);
            if (kg != null)
            {
                db.KHUNGGIOs.Remove(kg);
                db.SaveChanges();
            }
        }

        public bool CanDeleteTimeSlot(string id)
        {
            return !db.LICHCHIEUx.Any(l => l.MA_KHUNGGIO == id);
        }
    }
}
