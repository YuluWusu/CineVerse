using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class CustomerService
    {
        private QL_VEPHIMEntities db;

        public CustomerService()
        {
            this.db = new QL_VEPHIMEntities();
        }

        public CustomerService(QL_VEPHIMEntities dbContext)
        {
            this.db = dbContext;
        }

        public List<KHACHHANG> GetAllCustomers()
        {
            return db.KHACHHANGs
                .Include(k => k.TAIKHOAN)
                .Include(k => k.TAIKHOAN.VAITROes)
                .OrderByDescending(k => k.TAIKHOAN.NGAYTAO)
                .ToList();
        }

        public KHACHHANG GetCustomerById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string trimmed = id.Trim();
            return db.KHACHHANGs
                .Include(k => k.TAIKHOAN)
                .Include(k => k.TAIKHOAN.VAITROes)
                .FirstOrDefault(k => k.MA_KHACHHANG.Trim() == trimmed);
        }

        public void UpdateCustomerRole(string id, string vaiTro)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(vaiTro)) return;
            string trimmed = id.Trim();
            var kh = db.KHACHHANGs
                .Include(k => k.TAIKHOAN)
                .Include(k => k.TAIKHOAN.VAITROes)
                .FirstOrDefault(k => k.MA_KHACHHANG.Trim() == trimmed);

            if (kh != null && kh.TAIKHOAN != null)
            {
                string vtTrim = vaiTro.Trim();
                var roleObj = db.VAITROes.FirstOrDefault(r => r.MA_VAITRO.Trim() == vtTrim || r.TEN_VAITRO.Trim() == vtTrim);
                if (roleObj == null)
                {
                    roleObj = new VAITRO
                    {
                        MA_VAITRO = vtTrim,
                        TEN_VAITRO = vtTrim
                    };
                    db.VAITROes.Add(roleObj);
                }

                kh.TAIKHOAN.VAITROes.Clear();
                kh.TAIKHOAN.VAITROes.Add(roleObj);
                db.SaveChanges();
            }
        }

        public void DeleteCustomer(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            string trimmed = id.Trim();
            var kh = db.KHACHHANGs
                .Include(k => k.TAIKHOAN)
                .FirstOrDefault(k => k.MA_KHACHHANG.Trim() == trimmed);

            if (kh != null)
            {
                var tk = kh.TAIKHOAN;
                db.KHACHHANGs.Remove(kh);
                if (tk != null)
                {
                    tk.VAITROes.Clear();
                    db.TAIKHOANs.Remove(tk);
                }
                db.SaveChanges();
            }
        }

        public bool CanDeleteCustomer(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            string trimmed = id.Trim();
            return !db.DONHANGs.Any(d => d.MA_KHACHHANG.Trim() == trimmed);
        }
    }
}
