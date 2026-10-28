-- =========================================================================
-- Phase 4 Migration: Thanh toán Trực tuyến (VNPay, VietQR), Vé Điện tử QR & Check-in
-- Dự án: CinéVerse Cinema Management System
-- CSDL: QL_VEPHIM
-- =========================================================================

USE [QL_VEPHIM];
GO

-- 1. Bổ sung các cột phục vụ thanh toán trực tuyến và vé điện tử vào DONHANG
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DONHANG]') AND name = 'PHUONGTHUCTHANHTOAN')
BEGIN
    ALTER TABLE [dbo].[DONHANG] ADD [PHUONGTHUCTHANHTOAN] NVARCHAR(50) NULL;
    PRINT 'Đã thêm cột PHUONGTHUCTHANHTOAN.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DONHANG]') AND name = 'TRANGTHAITHANHTOAN')
BEGIN
    ALTER TABLE [dbo].[DONHANG] ADD [TRANGTHAITHANHTOAN] NVARCHAR(50) NULL;
    PRINT 'Đã thêm cột TRANGTHAITHANHTOAN.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DONHANG]') AND name = 'MAGIAODICH')
BEGIN
    ALTER TABLE [dbo].[DONHANG] ADD [MAGIAODICH] NVARCHAR(100) NULL;
    PRINT 'Đã thêm cột MAGIAODICH.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DONHANG]') AND name = 'MACHECKIN')
BEGIN
    ALTER TABLE [dbo].[DONHANG] ADD [MACHECKIN] NVARCHAR(50) NULL;
    PRINT 'Đã thêm cột MACHECKIN.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DONHANG]') AND name = 'TRANGTHAISOATVE')
BEGIN
    ALTER TABLE [dbo].[DONHANG] ADD [TRANGTHAISOATVE] NVARCHAR(50) NULL CONSTRAINT [DF_DONHANG_TRANGTHAISOATVE] DEFAULT (N'ChuaSoat');
    PRINT 'Đã thêm cột TRANGTHAISOATVE.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DONHANG]') AND name = 'THOIGIANCHECKIN')
BEGIN
    ALTER TABLE [dbo].[DONHANG] ADD [THOIGIANCHECKIN] DATETIME NULL;
    PRINT 'Đã thêm cột THOIGIANCHECKIN.';
END
GO

-- 2. Cập nhật dữ liệu mặc định cho các đơn hàng cũ
UPDATE [dbo].[DONHANG]
SET 
    [PHUONGTHUCTHANHTOAN] = ISNULL([PHUONGTHUCTHANHTOAN], N'TaiQuay'),
    [TRANGTHAITHANHTOAN] = ISNULL([TRANGTHAITHANHTOAN], N'DaThanhToan'),
    [MACHECKIN] = ISNULL([MACHECKIN], N'CKI-' + RTRIM([MA_DONHANG]) + N'-' + SUBSTRING(CONVERT(VARCHAR(50), NEWID()), 1, 8)),
    [TRANGTHAISOATVE] = ISNULL([TRANGTHAISOATVE], N'ChuaSoat')
WHERE [MACHECKIN] IS NULL;
GO

PRINT 'Hoàn thành migration Phase 4 cho bảng DONHANG.';
GO

