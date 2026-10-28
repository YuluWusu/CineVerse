-- =========================================================================
-- Phase 2 Migration: Hệ thống Bắp nước & Combo (F&B Concessions)
-- Dự án: CinéVerse Cinema Management System
-- CSDL: QL_VEPHIM
-- =========================================================================

USE [QL_VEPHIM];
GO

-- 1. Tạo bảng BAPNUOC nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BAPNUOC')
BEGIN
    CREATE TABLE [dbo].[BAPNUOC] (
        [MA_SANPHAM] NCHAR(5) NOT NULL,
        [TEN_SANPHAM] NVARCHAR(100) NOT NULL,
        [MOTA] NVARCHAR(500) NULL,
        [PHANLOAI] NVARCHAR(20) NOT NULL, -- 'Combo', 'Bắp', 'Nước', 'Snack'
        [GIABAN] DECIMAL(12, 0) NOT NULL,
        [HINHANH] NVARCHAR(255) NULL,
        [SOLUONGTON] INT NOT NULL CONSTRAINT [DF_BAPNUOC_SOLUONGTON] DEFAULT (100),
        [TRANGTHAI] NVARCHAR(20) NOT NULL CONSTRAINT [DF_BAPNUOC_TRANGTHAI] DEFAULT (N'DangBan'), -- 'DangBan', 'NgungBan'
        CONSTRAINT [PK_BAPNUOC] PRIMARY KEY CLUSTERED ([MA_SANPHAM] ASC),
        CONSTRAINT [CK_BAPNUOC_PHANLOAI] CHECK ([PHANLOAI] IN (N'Combo', N'Bắp', N'Nước', N'Snack')),
        CONSTRAINT [CK_BAPNUOC_GIABAN] CHECK ([GIABAN] >= 0),
        CONSTRAINT [CK_BAPNUOC_SOLUONGTON] CHECK ([SOLUONGTON] >= 0),
        CONSTRAINT [CK_BAPNUOC_TRANGTHAI] CHECK ([TRANGTHAI] IN (N'DangBan', N'NgungBan'))
    );
    PRINT 'Đã tạo bảng BAPNUOC thành công.';
END
ELSE
BEGIN
    PRINT 'Bảng BAPNUOC đã tồn tại.';
END
GO

-- 2. Tạo bảng CHITIET_BAPNUOC liên kết N-N giữa DONHANG và BAPNUOC
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CHITIET_BAPNUOC')
BEGIN
    CREATE TABLE [dbo].[CHITIET_BAPNUOC] (
        [MA_CHITIET] NCHAR(10) NOT NULL,
        [MA_DONHANG] NCHAR(5) NOT NULL,
        [MA_SANPHAM] NCHAR(5) NOT NULL,
        [SOLUONG] INT NOT NULL,
        [DONGIA] DECIMAL(12, 0) NOT NULL,
        [THANHTIEN] DECIMAL(12, 0) NOT NULL,
        CONSTRAINT [PK_CHITIET_BAPNUOC] PRIMARY KEY CLUSTERED ([MA_CHITIET] ASC),
        CONSTRAINT [FK_CHITIET_BAPNUOC_DONHANG] FOREIGN KEY ([MA_DONHANG]) REFERENCES [dbo].[DONHANG] ([MA_DONHANG]) ON DELETE CASCADE,
        CONSTRAINT [FK_CHITIET_BAPNUOC_BAPNUOC] FOREIGN KEY ([MA_SANPHAM]) REFERENCES [dbo].[BAPNUOC] ([MA_SANPHAM]),
        CONSTRAINT [CK_CHITIET_BAPNUOC_SOLUONG] CHECK ([SOLUONG] > 0),
        CONSTRAINT [CK_CHITIET_BAPNUOC_DONGIA] CHECK ([DONGIA] >= 0),
        CONSTRAINT [CK_CHITIET_BAPNUOC_THANHTIEN] CHECK ([THANHTIEN] >= 0)
    );
    PRINT 'Đã tạo bảng CHITIET_BAPNUOC thành công.';
END
ELSE
BEGIN
    PRINT 'Bảng CHITIET_BAPNUOC đã tồn tại.';
END
GO

-- 3. Seed Dữ liệu mẫu phong phú cho Bắp nước & Combo
IF NOT EXISTS (SELECT 1 FROM [dbo].[BAPNUOC] WHERE [MA_SANPHAM] = N'CB001')
BEGIN
    INSERT INTO [dbo].[BAPNUOC] ([MA_SANPHAM], [TEN_SANPHAM], [MOTA], [PHANLOAI], [GIABAN], [HINHANH], [SOLUONGTON], [TRANGTHAI])
    VALUES 
    (N'CB001', N'Combo Solo Đậm Vị', N'1 Bắp vừa tự chọn (Ngọt/Mặn) + 1 Nước ngọt có ga lớn 32oz', N'Combo', 79000, N'combo-solo.png', 150, N'DangBan'),
    (N'CB002', N'Combo Couple Ngọt Ngào', N'1 Bắp lớn 2 vị đặc biệt (Caramel + Phô mai) + 2 Nước ngọt có ga lớn 32oz', N'Combo', 109000, N'combo-couple.png', 200, N'DangBan'),
    (N'CB003', N'Combo Family Trọn Vẹn', N'2 Bắp khổng lồ (Caramel & Phô mai) + 3 Nước lớn + 1 Snack Khoai tây giòn', N'Combo', 169000, N'combo-family.png', 80, N'DangBan'),
    (N'CB004', N'Combo CineVIP Signature', N'1 Bắp Caramel thượng hạng + 2 Trà đào cam sả thanh mát + 1 Xúc xích xông khói', N'Combo', 149000, N'combo-vip.png', 90, N'DangBan'),
    
    (N'BP001', N'Bắp Rang Bơ Truyền Thống (Lớn)', N'Hạt bắp nổ giòn rụm ngào bơ thơm ngát quyến rũ', N'Bắp', 55000, N'bap-bo.png', 300, N'DangBan'),
    (N'BP002', N'Bắp Phô Mai Thượng Hạng (Lớn)', N'Lớp bột phô mai béo ngậy mặn mặn đậm đà chuẩn vị rạp chiếu', N'Bắp', 65000, N'bap-phomai.png', 250, N'DangBan'),
    (N'BP003', N'Bắp Caramel Đậm Đà (Lớn)', N'Sốt caramel nâu óng ánh phủ đều từng hạt bắp giòn ngọt', N'Bắp', 65000, N'bap-caramel.png', 250, N'DangBan'),
    (N'BP004', N'Bắp Rang Socola Đen (Lớn)', N'Hương vị socola đen tinh tế dành cho tín đồ mê ngọt dịu', N'Bắp', 69000, N'bap-socola.png', 120, N'DangBan'),

    (N'NU001', N'Pepsi Tươi Mát Lạnh (32oz)', N'Ly nước ngọt có ga sảng khoái đánh tan cơn khát', N'Nước', 35000, N'pepsi.png', 500, N'DangBan'),
    (N'NU002', N'Coca-Cola Zero Sugar (32oz)', N'Không đường thanh nhẹ, giữ dáng trọn vẹn', N'Nước', 35000, N'coca-zero.png', 400, N'DangBan'),
    (N'NU003', N'7Up Chanh Tươi Mát Lạnh (32oz)', N'Vị chanh tự nhiên tươi mới, cực kỳ đã khát', N'Nước', 35000, N'7up.png', 350, N'DangBan'),
    (N'NU004', N'Trà Đào Cam Sả Tươi (24oz)', N'Vị thanh mát của trà, lát đào giòn ngọt và hương sả nồng nàn', N'Nước', 45000, N'tra-dao.png', 150, N'DangBan'),
    (N'NU005', N'Nước Suối Khoáng Aquafina (500ml)', N'Nước khoáng tinh khiết đóng chai tiện lợi', N'Nước', 20000, N'aquafina.png', 400, N'DangBan'),

    (N'SN001', N'Snack Khoai Tây Lay''s Giòn Tan', N'Vị tảo biển Nori thơm ngon giòn rụm', N'Snack', 25000, N'snack-lays.png', 200, N'DangBan'),
    (N'SN002', N'Xúc Xích Xông Khói Phô Mai Nóng', N'Thịt heo xông khói nhân phô mai tan chảy khi cắn', N'Snack', 35000, N'xuc-xich.png', 100, N'DangBan');

    PRINT 'Đã chèn dữ liệu mẫu Bắp nước & Combo thành công.';
END
ELSE
BEGIN
    PRINT 'Dữ liệu BAPNUOC đã tồn tại, không chèn lại.';
END
GO

