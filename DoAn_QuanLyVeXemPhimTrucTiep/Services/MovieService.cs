using System;
using System.Collections.Generic;
using System.Linq;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class MovieService
    {
        private QL_VEPHIM2Entities db;

        public MovieService()
        {
            this.db = new QL_VEPHIM2Entities();
        }

        public MovieService(QL_VEPHIM2Entities dbContext)
        {
            this.db = dbContext;
        }

        public List<PHIM> GetAllMovies()
        {
            return db.PHIMs.OrderByDescending(p => p.MA_PHIM).ToList();
        }

        public PHIM GetMovieById(string id)
        {
            return db.PHIMs.FirstOrDefault(p => p.MA_PHIM == id);
        }

        public void AddMovie(PHIM phim)
        {
            db.PHIMs.Add(phim);
            db.SaveChanges();
        }

        public void UpdateMovie(PHIM phim)
        {
            var p = db.PHIMs.FirstOrDefault(x => x.MA_PHIM == phim.MA_PHIM);
            if (p != null)
            {
                p.TEN_PHIM = phim.TEN_PHIM;
                p.THOILUONG = phim.THOILUONG;
                p.NGAYKHOICHIEU = phim.NGAYKHOICHIEU;
                p.MOTA = phim.MOTA;
                p.DAO_DIEN = phim.DAO_DIEN;
                p.DIEN_VIEN = phim.DIEN_VIEN;
                p.GIOI_HAN_TUOI = phim.GIOI_HAN_TUOI;
                p.TRANGTHAI = phim.TRANGTHAI;
                if (!string.IsNullOrEmpty(phim.POSTER))
                {
                    p.POSTER = phim.POSTER;
                }
                db.SaveChanges();
            }
        }

        public void DeleteMovie(string id)
        {
            var p = db.PHIMs.FirstOrDefault(x => x.MA_PHIM == id);
            if (p != null)
            {
                db.PHIMs.Remove(p);
                db.SaveChanges();
            }
        }

        public string GenerateNextMovieId()
        {
            string currentMax = db.PHIMs.Max(p => p.MA_PHIM);
            int nextId = 1;
            if (!string.IsNullOrEmpty(currentMax) && currentMax.StartsWith("P"))
            {
                if (int.TryParse(currentMax.Substring(1), out int currId))
                {
                    nextId = currId + 1;
                }
            }
            return "P" + nextId.ToString("D2");
        }

        public List<THELOAI> GetAllGenres()
        {
            return db.THELOAIs.ToList();
        }

        public THELOAI GetGenreById(string id)
        {
            return db.THELOAIs.Find(id);
        }

        public void AddGenre(THELOAI genre)
        {
            db.THELOAIs.Add(genre);
            db.SaveChanges();
        }

        public void UpdateGenre(THELOAI genre)
        {
            var tl = db.THELOAIs.Find(genre.MA_THELOAI);
            if (tl != null)
            {
                tl.TEN_THELOAI = genre.TEN_THELOAI;
                db.SaveChanges();
            }
        }

        public void DeleteGenre(string id)
        {
            var theLoai = db.THELOAIs.Find(id);
            if (theLoai != null)
            {
                db.THELOAIs.Remove(theLoai);
                db.SaveChanges();
            }
        }

        public string GenerateNextGenreId()
        {
            string maxMa = db.THELOAIs.Max(t => t.MA_THELOAI);
            int nextId = 1;
            if (!string.IsNullOrEmpty(maxMa) && maxMa.StartsWith("TL"))
            {
                if (int.TryParse(maxMa.Substring(2), out int curr))
                    nextId = curr + 1;
            }
            return "TL" + nextId.ToString("D2");
        }

        public bool IsGenreNameExists(string name, string ignoreId = null)
        {
            var lowerName = name.ToLower();
            var query = db.THELOAIs.Where(t => t.TEN_THELOAI.ToLower() == lowerName);
            if (!string.IsNullOrEmpty(ignoreId))
            {
                query = query.Where(t => t.MA_THELOAI != ignoreId);
            }
            return query.Any();
        }
    }
}
