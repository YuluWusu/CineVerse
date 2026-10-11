using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class CustomerService
    {
        private QL_VEPHIM2Entities db;

        public CustomerService()
        {
            this.db = new QL_VEPHIM2Entities();
        }

        public CustomerService(QL_VEPHIM2Entities dbContext)
        {
            this.db = dbContext;
        }

        public List<KHACHHANG> GetAllCustomers()
        {
            return db.KHACHHANGs.OrderByDescending(k => k.NGAYTAO).ToList();
        }

        public KHACHHANG GetCustomerById(string id)
        {
            return db.KHACHHANGs.FirstOrDefault(k => k.MA_KHACH == id);
        }

        public void UpdateCustomerRole(string id, string vaiTro)
        {
            var kh = db.KHACHHANGs.FirstOrDefault(k => k.MA_KHACH == id);
            if (kh != null)
            {
                kh.VAITRO = vaiTro;
                db.SaveChanges();
            }
        }

        public void DeleteCustomer(string id)
        {
            var kh = db.KHACHHANGs.FirstOrDefault(k => k.MA_KHACH == id);
            if (kh != null)
            {
                db.KHACHHANGs.Remove(kh);
                db.SaveChanges();
            }
        }

        public bool CanDeleteCustomer(string id)
        {
            return !db.DONHANGs.Any(d => d.MA_KHACH == id);
        }
    }
}
