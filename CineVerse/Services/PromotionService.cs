using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class PromotionService
    {
        private QL_VEPHIM2Entities db;

        public PromotionService()
        {
            this.db = new QL_VEPHIM2Entities();
        }

        public PromotionService(QL_VEPHIM2Entities dbContext)
        {
            this.db = dbContext;
        }

        public List<KHUYENMAI> GetAllPromotions()
        {
            return db.KHUYENMAIs.OrderByDescending(k => k.NGAYBATDAU).ToList();
        }

        public KHUYENMAI GetPromotionById(string id)
        {
            return db.KHUYENMAIs.FirstOrDefault(k => k.MA_KM == id);
        }

        public void AddPromotion(KHUYENMAI km)
        {
            km.MA_CODE = km.MA_CODE.ToUpper().Trim();
            if (string.IsNullOrEmpty(km.TRANGTHAI))
                km.TRANGTHAI = "HoatDong";
            km.SO_LUOT_DADUNG = 0;

            db.KHUYENMAIs.Add(km);
            db.SaveChanges();
        }

        public void UpdatePromotion(KHUYENMAI km)
        {
            var existing = db.KHUYENMAIs.FirstOrDefault(k => k.MA_KM == km.MA_KM);
            if (existing != null)
            {
                existing.TEN_KM = km.TEN_KM;
                existing.MA_CODE = km.MA_CODE.ToUpper().Trim();
                existing.GIAMGIA_PHANTRAM = km.GIAMGIA_PHANTRAM;
                existing.GIAMGIA_TOIDA = km.GIAMGIA_TOIDA;
                existing.NGAYBATDAU = km.NGAYBATDAU;
                existing.NGAYKETTHUC = km.NGAYKETTHUC;
                existing.SO_LUOT_TOIDA = km.SO_LUOT_TOIDA;
                existing.MOTA = km.MOTA;
                existing.TRANGTHAI = km.TRANGTHAI;
                db.SaveChanges();
            }
        }

        public void DeletePromotion(string id)
        {
            var km = db.KHUYENMAIs.FirstOrDefault(k => k.MA_KM == id);
            if (km != null)
            {
                db.KHUYENMAIs.Remove(km);
                db.SaveChanges();
            }
        }

        public string GenerateNextPromotionId()
        {
            var allIds = db.KHUYENMAIs.Select(k => k.MA_KM).ToList();
            int maxId = 0;
            foreach (var id in allIds)
            {
                if (!string.IsNullOrEmpty(id) && id.StartsWith("KM"))
                {
                    if (int.TryParse(id.Substring(2), out int cur) && cur > maxId)
                        maxId = cur;
                }
            }
            return "KM" + (maxId + 1).ToString("D3");
        }

        public bool IsCodeExists(string code, string ignoreId = null)
        {
            var upperCode = code.ToUpper().Trim();
            var query = db.KHUYENMAIs.Where(k => k.MA_CODE == upperCode);
            if (!string.IsNullOrEmpty(ignoreId))
            {
                query = query.Where(k => k.MA_KM != ignoreId);
            }
            return query.Any();
        }

        public KHUYENMAI ValidatePromotionCode(string maCode)
        {
            if (string.IsNullOrEmpty(maCode)) return null;

            var upperCode = maCode.ToUpper().Trim();
            return db.KHUYENMAIs.FirstOrDefault(k =>
                k.MA_CODE == upperCode &&
                k.TRANGTHAI == "HoatDong" &&
                k.NGAYBATDAU <= DateTime.Now &&
                k.NGAYKETTHUC >= DateTime.Now);
        }
    }
}
