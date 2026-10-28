using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class FnBService
    {
        private readonly QL_VEPHIMEntities _db;

        public FnBService(QL_VEPHIMEntities db = null)
        {
            _db = db ?? new QL_VEPHIMEntities();
        }

        /// <summary>
        /// Lấy toàn bộ danh sách bắp nước & combo có hỗ trợ lọc theo phân loại, từ khóa tìm kiếm và trạng thái.
        /// </summary>
        public List<BAPNUOC> GetAll(string phanLoai = null, string search = null, bool onlyActive = false)
        {
            var query = _db.BAPNUOCs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(phanLoai) && !phanLoai.Equals("TatCa", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => b.PHANLOAI == phanLoai.Trim());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchLower = search.Trim().ToLower();
                query = query.Where(b => b.TEN_SANPHAM.ToLower().Contains(searchLower) ||
                                         (b.MOTA != null && b.MOTA.ToLower().Contains(searchLower)));
            }

            if (onlyActive)
            {
                query = query.Where(b => b.TRANGTHAI == "DangBan");
            }

            return query.OrderBy(b => b.PHANLOAI).ThenBy(b => b.GIABAN).ToList();
        }

        /// <summary>
        /// Lấy chi tiết sản phẩm theo mã.
        /// </summary>
        public BAPNUOC GetById(string maSanPham)
        {
            if (string.IsNullOrWhiteSpace(maSanPham)) return null;
            string trimmed = maSanPham.Trim();
            return _db.BAPNUOCs.FirstOrDefault(b => b.MA_SANPHAM.Trim() == trimmed);
        }

        /// <summary>
        /// Sinh mã sản phẩm tự động dựa trên phân loại.
        /// </summary>
        public string GenerateNewCode(string phanLoai)
        {
            string prefix = "SP";
            if (!string.IsNullOrWhiteSpace(phanLoai))
            {
                switch (phanLoai.Trim().ToLower())
                {
                    case "combo":
                        prefix = "CB";
                        break;
                    case "bắp":
                    case "bap":
                        prefix = "BP";
                        break;
                    case "nước":
                    case "nuoc":
                        prefix = "NU";
                        break;
                    case "snack":
                        prefix = "SN";
                        break;
                }
            }

            var existingCodes = _db.BAPNUOCs
                .Where(b => b.MA_SANPHAM.StartsWith(prefix))
                .Select(b => b.MA_SANPHAM)
                .ToList();

            int maxIndex = 0;
            foreach (var code in existingCodes)
            {
                string numPart = code.Trim().Substring(prefix.Length);
                if (int.TryParse(numPart, out int idx) && idx > maxIndex)
                {
                    maxIndex = idx;
                }
            }

            return $"{prefix}{(maxIndex + 1):D3}";
        }

        /// <summary>
        /// Thêm mới sản phẩm bắp nước/combo.
        /// </summary>
        public bool Create(BAPNUOC model, HttpPostedFileBase hinhAnhFile, string uploadFolderPath, out string error)
        {
            error = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(model.TEN_SANPHAM))
                {
                    error = "Tên sản phẩm không được để trống.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(model.PHANLOAI))
                {
                    model.PHANLOAI = "Combo";
                }

                if (string.IsNullOrWhiteSpace(model.MA_SANPHAM))
                {
                    model.MA_SANPHAM = GenerateNewCode(model.PHANLOAI);
                }
                else
                {
                    model.MA_SANPHAM = model.MA_SANPHAM.Trim().ToUpper();
                    if (_db.BAPNUOCs.Any(b => b.MA_SANPHAM.Trim() == model.MA_SANPHAM))
                    {
                        error = $"Mã sản phẩm '{model.MA_SANPHAM}' đã tồn tại trong hệ thống.";
                        return false;
                    }
                }

                // Xử lý upload ảnh
                if (hinhAnhFile != null && hinhAnhFile.ContentLength > 0)
                {
                    string ext = Path.GetExtension(hinhAnhFile.FileName);
                    string fileName = $"fnb_{model.MA_SANPHAM}_{DateTime.Now.Ticks}{ext}";
                    if (!Directory.Exists(uploadFolderPath))
                    {
                        Directory.CreateDirectory(uploadFolderPath);
                    }
                    string physicalPath = Path.Combine(uploadFolderPath, fileName);
                    hinhAnhFile.SaveAs(physicalPath);
                    model.HINHANH = fileName;
                }

                if (string.IsNullOrWhiteSpace(model.TRANGTHAI))
                {
                    model.TRANGTHAI = "DangBan";
                }

                _db.BAPNUOCs.Add(model);
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                error = "Lỗi lưu dữ liệu: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Cập nhật thông tin sản phẩm bắp nước/combo.
        /// </summary>
        public bool Update(BAPNUOC model, HttpPostedFileBase hinhAnhFile, string uploadFolderPath, out string error)
        {
            error = string.Empty;
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.MA_SANPHAM))
                {
                    error = "Mã sản phẩm không hợp lệ.";
                    return false;
                }

                var entity = GetById(model.MA_SANPHAM);
                if (entity == null)
                {
                    error = "Không tìm thấy sản phẩm cần cập nhật.";
                    return false;
                }

                entity.TEN_SANPHAM = model.TEN_SANPHAM?.Trim();
                entity.MOTA = model.MOTA?.Trim();
                entity.PHANLOAI = model.PHANLOAI?.Trim();
                entity.GIABAN = model.GIABAN;
                entity.SOLUONGTON = model.SOLUONGTON;
                entity.TRANGTHAI = string.IsNullOrWhiteSpace(model.TRANGTHAI) ? "DangBan" : model.TRANGTHAI.Trim();

                if (hinhAnhFile != null && hinhAnhFile.ContentLength > 0)
                {
                    string ext = Path.GetExtension(hinhAnhFile.FileName);
                    string fileName = $"fnb_{entity.MA_SANPHAM.Trim()}_{DateTime.Now.Ticks}{ext}";
                    if (!Directory.Exists(uploadFolderPath))
                    {
                        Directory.CreateDirectory(uploadFolderPath);
                    }
                    string physicalPath = Path.Combine(uploadFolderPath, fileName);
                    hinhAnhFile.SaveAs(physicalPath);
                    entity.HINHANH = fileName;
                }

                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                error = "Lỗi cập nhật: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Bật/tắt trạng thái mở bán nhanh của sản phẩm (DangBan <-> NgungBan).
        /// </summary>
        public bool ToggleStatus(string maSanPham, out string newStatus, out string error)
        {
            newStatus = string.Empty;
            error = string.Empty;
            try
            {
                var entity = GetById(maSanPham);
                if (entity == null)
                {
                    error = "Không tìm thấy sản phẩm.";
                    return false;
                }

                if (entity.TRANGTHAI == "DangBan")
                {
                    entity.TRANGTHAI = "NgungBan";
                }
                else
                {
                    entity.TRANGTHAI = "DangBan";
                }

                _db.SaveChanges();
                newStatus = entity.TRANGTHAI;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Xóa sản phẩm. Nếu sản phẩm đã phát sinh chi tiết đơn hàng thì chuyển trạng thái NgungBan để đảm bảo toàn vẹn.
        /// </summary>
        public bool Delete(string maSanPham, out string actionMessage, out string error)
        {
            actionMessage = string.Empty;
            error = string.Empty;
            try
            {
                var entity = GetById(maSanPham);
                if (entity == null)
                {
                    error = "Không tìm thấy sản phẩm cần xóa.";
                    return false;
                }

                bool hasOrders = _db.CHITIET_BAPNUOC.Any(c => c.MA_SANPHAM.Trim() == entity.MA_SANPHAM.Trim());
                if (hasOrders)
                {
                    entity.TRANGTHAI = "NgungBan";
                    _db.SaveChanges();
                    actionMessage = "Sản phẩm đã có lịch sử đặt hàng nên hệ thống đã tự động chuyển sang trạng thái 'Ngừng bán' để bảo toàn dữ liệu thống kê.";
                    return true;
                }

                _db.BAPNUOCs.Remove(entity);
                _db.SaveChanges();
                actionMessage = "Đã xóa sản phẩm thành công.";
                return true;
            }
            catch (Exception ex)
            {
                error = "Lỗi khi xóa: " + ex.Message;
                return false;
            }
        }
    }
}

