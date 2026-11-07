using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class MovieService
    {
        private QL_VEPHIMEntities db;

        public MovieService()
        {
            this.db = new QL_VEPHIMEntities();
        }

        public MovieService(QL_VEPHIMEntities dbContext)
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

        public bool CanDeleteMovie(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            string cleanId = id.Trim();
            return !db.LICHCHIEUx.Any(lc => lc.MA_PHIM.Trim() == cleanId);
        }

        public bool DeleteMovie(string id, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                error = "Mã phim không hợp lệ.";
                return false;
            }

            string cleanId = id.Trim();
            if (!CanDeleteMovie(cleanId))
            {
                error = "Không thể xóa phim này vì đã có lịch chiếu trong hệ thống. Vui lòng chuyển trạng thái sang 'Ngừng chiếu' thay vì xóa.";
                return false;
            }

            var phim = db.PHIMs
                .Include(p => p.THELOAIs)
                .Include(p => p.DANHGIAs)
                .FirstOrDefault(p => p.MA_PHIM.Trim() == cleanId);

            if (phim == null)
            {
                error = "Không tìm thấy bộ phim cần xóa.";
                return false;
            }

            try
            {
                // 1. Gỡ bỏ liên kết N-N với Thể loại (Bảng liên kết PHIM_THELOAI)
                phim.THELOAIs.Clear();

                // 2. Xóa các đánh giá liên quan nếu có
                if (phim.DANHGIAs != null && phim.DANHGIAs.Any())
                {
                    db.DANHGIAs.RemoveRange(phim.DANHGIAs.ToList());
                }

                // 3. Xóa bản ghi phim
                db.PHIMs.Remove(phim);
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                error = "Lỗi khi xóa phim: " + (ex.InnerException?.Message ?? ex.Message);
                return false;
            }
        }

        public void DeleteMovie(string id)
        {
            if (!DeleteMovie(id, out string error))
            {
                throw new InvalidOperationException(error);
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
