using System;
using System.Collections.Generic;
using System.Linq;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class CinemaService
    {
        private QL_VEPHIMEntities db;

        public CinemaService()
        {
            this.db = new QL_VEPHIMEntities();
        }

        public CinemaService(QL_VEPHIMEntities dbContext)
        {
            this.db = dbContext;
        }

        // ========================== RAPCHIEU ==========================
        public List<RAPCHIEU> GetAllCinemas()
        {
            return db.RAPCHIEUx.ToList();
        }

        public RAPCHIEU GetCinemaById(string id)
        {
            return db.RAPCHIEUx.FirstOrDefault(r => r.MA_RAP == id);
        }

        public void AddCinema(RAPCHIEU rap)
        {
            db.RAPCHIEUx.Add(rap);
            db.SaveChanges();
        }

        public void UpdateCinema(RAPCHIEU rap)
        {
            var r = db.RAPCHIEUx.FirstOrDefault(x => x.MA_RAP == rap.MA_RAP);
            if (r != null)
            {
                r.TEN_RAP = rap.TEN_RAP;
                r.DIACHI = rap.DIACHI;
                db.SaveChanges();
            }
        }

        public void DeleteCinema(string id)
        {
            var r = db.RAPCHIEUx.FirstOrDefault(x => x.MA_RAP == id);
            if (r != null)
            {
                db.RAPCHIEUx.Remove(r);
                db.SaveChanges();
            }
        }

        public string GenerateNextCinemaId()
        {
            string currentMax = db.RAPCHIEUx.Max(r => r.MA_RAP);
            int nextId = 1;
            if (!string.IsNullOrEmpty(currentMax) && currentMax.StartsWith("R"))
            {
                if (int.TryParse(currentMax.Substring(1), out int currId))
                {
                    nextId = currId + 1;
                }
            }
            return "R" + nextId.ToString("D2");
        }

        public bool CanDeleteCinema(string id)
        {
            return !db.PHONGCHIEUx.Any(p => p.MA_RAP == id);
        }

        // ========================== PHONGCHIEU ==========================
        public List<PHONGCHIEU> GetAllRooms(string idRap = null)
        {
            var phongs = db.PHONGCHIEUx.AsQueryable();
            if (!string.IsNullOrEmpty(idRap))
            {
                phongs = phongs.Where(p => p.MA_RAP == idRap);
            }
            return phongs.ToList();
        }

        public PHONGCHIEU GetRoomById(string id)
        {
            return db.PHONGCHIEUx.FirstOrDefault(p => p.MA_PHONG == id);
        }

        public void AddRoomAndGenerateSeats(PHONGCHIEU phong)
        {
            db.PHONGCHIEUx.Add(phong);
            db.SaveChanges();

            int maxRows = phong.SO_HANG ?? 10;
            int maxCols = phong.SO_COT ?? 10;
            char[] rows = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O' };
            
            for(int r = 0; r < maxRows && r < rows.Length; r++)
            {
                for(int c = 1; c <= maxCols; c++)
                {
                    string soghe = rows[r].ToString() + c.ToString();
                    
                    string currentMaxGhe = db.GHEs.Max(g => g.MA_GHE);
                    int nextGheId = 1;
                    if (!string.IsNullOrEmpty(currentMaxGhe) && currentMaxGhe.StartsWith("G"))
                    {
                        if (int.TryParse(currentMaxGhe.Substring(1), out int currId))
                        {
                            nextGheId = currId + 1;
                        }
                    }
                    
                    string loaiGhe = "Thường";
                    // Giả sử 2 hàng cuối là VIP
                    if(r >= maxRows - 2)
                    {
                        loaiGhe = "VIP";
                    }

                    db.GHEs.Add(new GHE {
                        MA_GHE = "G" + nextGheId.ToString("D4"),
                        MA_PHONG = phong.MA_PHONG,
                        SOGHE = soghe,
                        MA_LOAIGHE = loaiGhe,
                        SO_HANG = r + 1,
                        SO_COT = c
                    });
                    
                    db.SaveChanges();
                }
            }
        }

        public void UpdateRoom(PHONGCHIEU phong)
        {
            var p = db.PHONGCHIEUx.FirstOrDefault(x => x.MA_PHONG == phong.MA_PHONG);
            if (p != null)
            {
                p.TEN_PHONG = phong.TEN_PHONG;
                p.MA_RAP = phong.MA_RAP;
                p.SO_HANG = phong.SO_HANG;
                p.SO_COT = phong.SO_COT;
                db.SaveChanges();
            }
        }

        public void DeleteRoom(string id)
        {
            var p = db.PHONGCHIEUx.FirstOrDefault(x => x.MA_PHONG == id);
            if (p != null)
            {
                var ghes = db.GHEs.Where(g => g.MA_PHONG == id).ToList();
                db.GHEs.RemoveRange(ghes);
                db.PHONGCHIEUx.Remove(p);
                db.SaveChanges();
            }
        }

        public string GenerateNextRoomId()
        {
            string currentMax = db.PHONGCHIEUx.Max(p => p.MA_PHONG);
            int nextId = 1;
            if (!string.IsNullOrEmpty(currentMax) && currentMax.StartsWith("PC"))
            {
                if (int.TryParse(currentMax.Substring(2), out int currId))
                {
                    nextId = currId + 1;
                }
            }
            return "PC" + nextId.ToString("D2");
        }

        public bool CanDeleteRoom(string id)
        {
            return !db.LICHCHIEUx.Any(l => l.MA_PHONG == id);
        }

        // ========================== LOAIGHE ==========================
        public List<LOAIGHE> GetAllSeatTypes()
        {
            return db.LOAIGHEs.ToList();
        }

        public LOAIGHE GetSeatTypeById(string id)
        {
            return db.LOAIGHEs.Find(id);
        }

        public void AddSeatType(LOAIGHE loaiGhe)
        {
            db.LOAIGHEs.Add(loaiGhe);
            db.SaveChanges();
        }

        public void UpdateSeatType(LOAIGHE loaiGhe)
        {
            var existing = db.LOAIGHEs.Find(loaiGhe.MA_LOAIGHE);
            if (existing != null)
            {
                existing.TEN_LOAIGHE = loaiGhe.TEN_LOAIGHE.Trim();
                existing.GIATVE = loaiGhe.GIATVE;
                db.SaveChanges();
            }
        }

        public void DeleteSeatType(string id)
        {
            var lg = db.LOAIGHEs.Find(id);
            if (lg != null)
            {
                db.LOAIGHEs.Remove(lg);
                db.SaveChanges();
            }
        }

        public string GenerateNextSeatTypeId()
        {
            string maxMa = db.LOAIGHEs.Max(l => l.MA_LOAIGHE);
            int nextId = 1;
            if (!string.IsNullOrEmpty(maxMa) && maxMa.StartsWith("LG"))
            {
                if (int.TryParse(maxMa.Substring(2), out int curr))
                    nextId = curr + 1;
            }
            return "LG" + nextId.ToString("D2");
        }

        public bool IsSeatTypeNameExists(string name, string ignoreId = null)
        {
            var query = db.LOAIGHEs.Where(l => l.TEN_LOAIGHE.ToLower() == name.ToLower());
            if (!string.IsNullOrEmpty(ignoreId))
            {
                query = query.Where(l => l.MA_LOAIGHE != ignoreId);
            }
            return query.Any();
        }

        public bool CanDeleteSeatType(string id)
        {
            return !db.GHEs.Any(g => g.MA_LOAIGHE == id);
        }
    }
}
