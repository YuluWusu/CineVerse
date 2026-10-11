using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class BookingService
    {
        private QL_VEPHIM2Entities db;

        public BookingService()
        {
            this.db = new QL_VEPHIM2Entities();
        }

        public BookingService(QL_VEPHIM2Entities dbContext)
        {
            this.db = dbContext;
        }

        public KHACHHANG GetOrCreateCustomer(string hoTen, string soDienThoai, string email)
        {
            var khachHang = db.KHACHHANGs.FirstOrDefault(k => k.SDT == soDienThoai);
            if (khachHang == null)
            {
                var allKhIds = db.KHACHHANGs.Select(k => k.MA_KHACH).ToList();
                int maxKhId = 0;
                foreach (var id in allKhIds)
                {
                    if (!string.IsNullOrEmpty(id) && id.StartsWith("KH"))
                    {
                        if (int.TryParse(id.Substring(2), out int currentId))
                        {
                            if (currentId > maxKhId) maxKhId = currentId;
                        }
                    }
                }
                string newMaKhach = "KH" + (maxKhId + 1).ToString("D3");

                khachHang = new KHACHHANG
                {
                    MA_KHACH = newMaKhach,
                    HOTEN = hoTen,
                    SDT = soDienThoai,
                    EMAIL = email,
                    VAITRO = "KhachHang",
                    NGAYTAO = DateTime.Now
                };
                db.KHACHHANGs.Add(khachHang);
                db.SaveChanges(); // Save to get the customer available for later
            }
            return khachHang;
        }

        public KHACHHANG GetCustomerByPhone(string phone)
        {
            return db.KHACHHANGs.FirstOrDefault(k => k.SDT == phone);
        }

        public KHACHHANG GetCustomerById(string id)
        {
            return db.KHACHHANGs.FirstOrDefault(k => k.MA_KHACH == id);
        }

        public List<VE> GetBookingHistory(string maKhach)
        {
            return db.VEs
                .Where(v => v.DONHANG.MA_KHACH == maKhach)
                .OrderByDescending(v => v.DONHANG.NGAYDAT)
                .ToList();
        }

        public KHUYENMAI ValidatePromotion(string maKmId)
        {
            if (string.IsNullOrEmpty(maKmId)) return null;

            var khuyenMai = db.KHUYENMAIs.FirstOrDefault(k =>
                k.MA_KM == maKmId &&
                k.TRANGTHAI == "HoatDong" &&
                k.NGAYBATDAU <= DateTime.Now &&
                k.NGAYKETTHUC >= DateTime.Now);

            if (khuyenMai != null &&
                khuyenMai.SO_LUOT_TOIDA.HasValue &&
                khuyenMai.SO_LUOT_DADUNG >= khuyenMai.SO_LUOT_TOIDA.Value)
            {
                return null; // Hết lượt dùng
            }

            return khuyenMai;
        }

        public Tuple<decimal, decimal, decimal> CalculatePrice(LICHCHIEU lichChieu, List<GHE> gheList, KHUYENMAI khuyenMai)
        {
            decimal tongTienGoc = 0;
            foreach (var ghe in gheList)
            {
                tongTienGoc += ghe.LOAIGHE.GIATVE + (lichChieu.GIATVE_PHUTROI ?? 0);
            }

            decimal soTienGiam = 0;
            if (khuyenMai != null)
            {
                soTienGiam = tongTienGoc * khuyenMai.GIAMGIA_PHANTRAM / 100m;
                if (khuyenMai.GIAMGIA_TOIDA.HasValue && soTienGiam > khuyenMai.GIAMGIA_TOIDA.Value)
                    soTienGiam = khuyenMai.GIAMGIA_TOIDA.Value;
            }

            decimal tongTienSauGiam = tongTienGoc - soTienGiam;
            return new Tuple<decimal, decimal, decimal>(tongTienGoc, soTienGiam, tongTienSauGiam);
        }

        public void ProcessBooking(KHACHHANG khachHang, LICHCHIEU lichChieu, List<GHE> gheList, KHUYENMAI khuyenMai, decimal soTienGiam)
        {
            // Tạo DONHANG
            var allDhIds = db.DONHANGs.Select(d => d.MA_DONHANG).ToList();
            int maxDhId = 0;
            foreach (var id in allDhIds)
            {
                if (!string.IsNullOrEmpty(id) && id.StartsWith("DH"))
                {
                    if (int.TryParse(id.Substring(2), out int currentId))
                    {
                        if (currentId > maxDhId) maxDhId = currentId;
                    }
                }
            }
            string maDonHang = "DH" + (maxDhId + 1).ToString("D3");

            decimal tongTienGoc = gheList.Sum(g => g.LOAIGHE.GIATVE + (lichChieu.GIATVE_PHUTROI ?? 0));
            decimal tongTienSauGiam = tongTienGoc - soTienGiam;

            DONHANG donHang = new DONHANG
            {
                MA_DONHANG = maDonHang,
                MA_KHACH = khachHang.MA_KHACH,
                MA_LICHCHIEU = lichChieu.MA_LICHCHIEU,
                MA_KM = khuyenMai?.MA_KM,
                NGAYDAT = DateTime.Now,
                TIENVE = tongTienGoc,
                TIENMONAN = 0,
                TIENGIAM = soTienGiam,
                TONGTIEN = tongTienSauGiam,
                TRANGTHAI = "Đã thanh toán"
            };
            db.DONHANGs.Add(donHang);

            // Tạo các VE thuộc DONHANG
            var allVeIds = db.VEs.Select(v => v.MA_VE).ToList();
            int maxVeId = 0;
            foreach (var id in allVeIds)
            {
                if (!string.IsNullOrEmpty(id) && id.StartsWith("VE"))
                {
                    if (int.TryParse(id.Substring(2), out int currentId))
                    {
                        if (currentId > maxVeId) maxVeId = currentId;
                    }
                }
            }

            foreach (var ghe in gheList)
            {
                decimal giaGhe = ghe.LOAIGHE.GIATVE + (lichChieu.GIATVE_PHUTROI ?? 0);
                maxVeId++;
                string maVeMoi = "VE" + maxVeId.ToString("D3");

                VE veMoi = new VE
                {
                    MA_VE = maVeMoi,
                    MA_DONHANG = maDonHang,
                    MA_LICHCHIEU = lichChieu.MA_LICHCHIEU,
                    MA_GHE = ghe.MA_GHE,
                    DONGIA = giaGhe,
                    TRANGTHAI = "Đã đặt"
                };
                db.VEs.Add(veMoi);
            }

            if (khuyenMai != null)
            {
                var km = db.KHUYENMAIs.Find(khuyenMai.MA_KM);
                if (km != null)
                {
                    km.SO_LUOT_DADUNG++;
                }
            }

            db.SaveChanges();
        }
    }
}
