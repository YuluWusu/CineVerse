-- ===================================================================
-- GIAI ĐOẠN 6: HOÀN THIỆN TOÀN DIỆN HỆ THỐNG CINÉVERSE
-- 1. Bổ sung bảng Đánh giá & Bình luận phim xác thực (DANHGIA)
-- 2. Bổ sung tích điểm & hạng thành viên cho KHACHHANG
-- ===================================================================
USE QL_VEPHIM;
GO

-- 1. Thêm cột DIEMTICHLUY và HANGTHANHVIEN vào KHACHHANG nếu chưa có
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'KHACHHANG' AND COLUMN_NAME = 'DIEMTICHLUY'
)
BEGIN
    ALTER TABLE KHACHHANG ADD DIEMTICHLUY INT NOT NULL DEFAULT 0;
    PRINT N'Đã thêm cột DIEMTICHLUY vào bảng KHACHHANG.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'KHACHHANG' AND COLUMN_NAME = 'HANGTHANHVIEN'
)
BEGIN
    ALTER TABLE KHACHHANG ADD HANGTHANHVIEN NVARCHAR(20) NOT NULL DEFAULT N'Member';
    PRINT N'Đã thêm cột HANGTHANHVIEN vào bảng KHACHHANG.';
END
GO

-- Cập nhật dữ liệu hạng ban đầu cho khách hàng
UPDATE KHACHHANG
SET DIEMTICHLUY = ISNULL(DIEMTICHLUY, 0),
    HANGTHANHVIEN = ISNULL(HANGTHANHVIEN, N'Member')
WHERE HANGTHANHVIEN IS NULL;
GO

-- 2. Tạo bảng DANHGIA nếu chưa có
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DANHGIA')
BEGIN
    CREATE TABLE DANHGIA (
        MA_DANHGIA NVARCHAR(10) NOT NULL,
        MA_PHIM NCHAR(5) NOT NULL,
        MA_KHACHHANG NCHAR(5) NOT NULL,
        SOSAO INT NOT NULL CONSTRAINT CK_DANHGIA_SOSAO CHECK (SOSAO BETWEEN 1 AND 5),
        NOIDUNG NVARCHAR(1000) NULL,
        NGAYDANHGIA DATETIME NOT NULL CONSTRAINT DF_DANHGIA_NGAY DEFAULT GETDATE(),
        TRANGTHAI NVARCHAR(20) NOT NULL CONSTRAINT DF_DANHGIA_TRANGTHAI DEFAULT N'HienThi',
        CONSTRAINT PK_DANHGIA PRIMARY KEY (MA_DANHGIA),
        CONSTRAINT FK_DANHGIA_PHIM FOREIGN KEY (MA_PHIM) REFERENCES PHIM(MA_PHIM) ON DELETE CASCADE,
        CONSTRAINT FK_DANHGIA_KHACHHANG FOREIGN KEY (MA_KHACHHANG) REFERENCES KHACHHANG(MA_KHACHHANG) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng DANHGIA thành công.';
END
GO

-- 3. Seed dữ liệu đánh giá mẫu chân thực
IF NOT EXISTS (SELECT 1 FROM DANHGIA WHERE MA_DANHGIA = 'DG001')
BEGIN
    -- Lấy mã phim đầu tiên
    DECLARE @FirstFilm NCHAR(5) = (SELECT TOP 1 MA_PHIM FROM PHIM ORDER BY MA_PHIM);
    DECLARE @SecondFilm NCHAR(5) = (SELECT TOP 1 MA_PHIM FROM PHIM WHERE MA_PHIM != @FirstFilm ORDER BY MA_PHIM);
    DECLARE @Cust1 NCHAR(5) = (SELECT TOP 1 MA_KHACHHANG FROM KHACHHANG ORDER BY MA_KHACHHANG);

    IF @FirstFilm IS NOT NULL AND @Cust1 IS NOT NULL
    BEGIN
        INSERT INTO DANHGIA (MA_DANHGIA, MA_PHIM, MA_KHACHHANG, SOSAO, NOIDUNG, NGAYDANHGIA, TRANGTHAI)
        VALUES 
        ('DG001', @FirstFilm, @Cust1, 5, N'Phim đỉnh cao visual và âm thanh cực kỳ choáng ngợp! Rạp CinéVerse chiếu màn hình to xem rất đã mắt, bắp phô mai nóng giòn.', DATEADD(day, -2, GETDATE()), N'HienThi'),
        ('DG002', @FirstFilm, @Cust1, 4, N'Diễn xuất của diễn viên chính rất cảm xúc, nhạc phim hoành tráng. Đoạn cuối hơi nhanh một chút nhưng tổng thể xứng đáng 9/10.', DATEADD(day, -1, GETDATE()), N'HienThi');
    END

    IF @SecondFilm IS NOT NULL AND @Cust1 IS NOT NULL
    BEGIN
        INSERT INTO DANHGIA (MA_DANHGIA, MA_PHIM, MA_KHACHHANG, SOSAO, NOIDUNG, NGAYDANHGIA, TRANGTHAI)
        VALUES 
        ('DG003', @SecondFilm, @Cust1, 5, N'Kịch bản chặt chẽ, nhiều nút thắt bất ngờ khó đoán. Ghế VIP ngồi êm và suất chiếu rất đúng giờ.', GETDATE(), N'HienThi');
    END

    PRINT N'Đã seed dữ liệu đánh giá mẫu.';
END
GO

