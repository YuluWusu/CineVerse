-- ===================================================================
-- GIAI ĐOẠN 5: NÂNG CẤP PHÂN HỆ VẬN HÀNH RẠP CHO NHÂN VIÊN (POS & QR SCANNER)
-- Bổ sung cột lưu vết nhân viên soát vé và trạng thái nhận bắp nước
-- ===================================================================
USE QL_VEPHIM;
GO

-- 1. Thêm cột MANV_SOATVE vào bảng DONHANG nếu chưa tồn tại
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'DONHANG' AND COLUMN_NAME = 'MANV_SOATVE'
)
BEGIN
    ALTER TABLE DONHANG ADD MANV_SOATVE NVARCHAR(20) NULL;
    PRINT N'Đã thêm cột MANV_SOATVE vào bảng DONHANG.';
END
GO

-- 2. Thêm cột TRANGTHAINHANBAPNUOC vào bảng DONHANG nếu chưa tồn tại
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'DONHANG' AND COLUMN_NAME = 'TRANGTHAINHANBAPNUOC'
)
BEGIN
    ALTER TABLE DONHANG ADD TRANGTHAINHANBAPNUOC NVARCHAR(50) NULL DEFAULT N'ChuaNhan';
    PRINT N'Đã thêm cột TRANGTHAINHANBAPNUOC vào bảng DONHANG.';
END
GO

-- Cập nhật giá trị mặc định cho các đơn hàng cũ
UPDATE DONHANG 
SET TRANGTHAINHANBAPNUOC = N'ChuaNhan'
WHERE TRANGTHAINHANBAPNUOC IS NULL;
GO

