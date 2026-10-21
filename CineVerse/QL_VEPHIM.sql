/* ============================================================================
   HỆ THỐNG QUẢN LÝ VÉ PHIM (QL_VEPHIM)
   ----------------------------------------------------------------------------
    */

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'QL_VEPHIM')
BEGIN
    CREATE DATABASE QL_VEPHIM;
END
GO

USE QL_VEPHIM;
GO

/* ============================================================================
   1. CÁC BẢNG DANH MỤC ĐỘC LẬP & NHÂN VIÊN, ĐỒ ĂN
   ============================================================================ */

CREATE TABLE RAPCHIEU (
    MA_RAP          NCHAR(5) PRIMARY KEY,
    TEN_RAP         NVARCHAR(100) NOT NULL,
    DIACHI          NVARCHAR(200) NULL,
    SO_PHONG_TOIDA  INT NULL CHECK (SO_PHONG_TOIDA IS NULL OR SO_PHONG_TOIDA > 0)
);
GO

CREATE TABLE LOAIGHE (
    MA_LOAIGHE  NCHAR(5) PRIMARY KEY,
    TEN_LOAIGHE NVARCHAR(10) NOT NULL CHECK (TEN_LOAIGHE IN (N'Thường', N'Đôi', N'VIP')),
    GIATVE      DECIMAL(10, 0) NOT NULL DEFAULT 0
);
GO

CREATE TABLE THELOAI (
    MA_THELOAI  NCHAR(5) PRIMARY KEY,
    TEN_THELOAI NVARCHAR(100) NOT NULL
);
GO

CREATE TABLE KHUNGGIO (
    MA_KHUNGGIO NCHAR(5) PRIMARY KEY,
    GIOBATDAU   TIME(7) NOT NULL,
    GIOKETTHUC  TIME(7) NOT NULL
);
GO

CREATE TABLE TAIKHOAN (
    MA_TAIKHOAN  NCHAR(5) PRIMARY KEY,
    HOTEN        NVARCHAR(50) NOT NULL,
    SDT          NCHAR(11) NOT NULL UNIQUE,
    EMAIL        NVARCHAR(100) NOT NULL UNIQUE,
    MATKHAU      NVARCHAR(255) NOT NULL,
    NGAYTAO      DATETIME DEFAULT GETDATE(),
    TRANGTHAI    NVARCHAR(20) DEFAULT N'HoatDong' CHECK (TRANGTHAI IN (N'HoatDong', N'Khoa', N'DaNghi'))
);
GO

CREATE TABLE VAITRO (
    MA_VAITRO   NVARCHAR(20) PRIMARY KEY,
    TEN_VAITRO  NVARCHAR(50) NOT NULL
);
GO

CREATE TABLE TAIKHOAN_VAITRO (
    MA_TAIKHOAN NCHAR(5) NOT NULL,
    MA_VAITRO   NVARCHAR(20) NOT NULL,
    PRIMARY KEY (MA_TAIKHOAN, MA_VAITRO),
    FOREIGN KEY (MA_TAIKHOAN) REFERENCES TAIKHOAN(MA_TAIKHOAN),
    FOREIGN KEY (MA_VAITRO) REFERENCES VAITRO(MA_VAITRO)
);
GO

CREATE TABLE KHACHHANG (
    MA_KHACHHANG    NCHAR(5) PRIMARY KEY,
    CCCD            NCHAR(12) NULL,
    FOREIGN KEY (MA_KHACHHANG) REFERENCES TAIKHOAN(MA_TAIKHOAN)
);
GO

CREATE TABLE KHUYENMAI (
    MA_KM            NVARCHAR(10) PRIMARY KEY,
    TEN_KM           NVARCHAR(100) NOT NULL,
    MA_CODE          NVARCHAR(20) NOT NULL UNIQUE,
    GIAMGIA_PHANTRAM INT NOT NULL CHECK (GIAMGIA_PHANTRAM BETWEEN 1 AND 100),
    GIAMGIA_TOIDA    DECIMAL(18, 2) NULL,
    NGAYBATDAU       DATETIME NOT NULL,
    NGAYKETTHUC      DATETIME NOT NULL,
    SO_LUOT_TOIDA    INT NULL,
    SO_LUOT_DADUNG   INT NOT NULL DEFAULT 0,
    MOTA             NVARCHAR(200) NULL,
    TRANGTHAI        NVARCHAR(20) NOT NULL DEFAULT N'HoatDong' CHECK (TRANGTHAI IN (N'HetHan', N'TamDung', N'HoatDong'))
);
GO

CREATE TABLE PHIM (
    MA_PHIM         NCHAR(5) PRIMARY KEY,
    TEN_PHIM        NVARCHAR(200) NOT NULL,
    THOILUONG       INT NULL CHECK (THOILUONG > 0),
    NGAYKHOICHIEU   DATE NULL,
    MOTA            NVARCHAR(MAX) NULL,
    POSTER          NVARCHAR(255) NULL,
    DAO_DIEN        NVARCHAR(100) NULL,
    DIEN_VIEN       NVARCHAR(500) NULL,
    GIOI_HAN_TUOI   NVARCHAR(5) NULL CHECK (GIOI_HAN_TUOI IN ('T18', 'T16', 'T13', 'P')),
    TRANGTHAI       NVARCHAR(20) DEFAULT N'Đang chiếu' CHECK (TRANGTHAI IN (N'Ngừng chiếu', N'Sắp chiếu', N'Đang chiếu')),
    CHIPHI_NHAP     DECIMAL(14,0) NOT NULL DEFAULT 0
);
GO

CREATE TABLE NHANVIEN (
    MA_NHANVIEN     NCHAR(5) PRIMARY KEY,
    LUONG           DECIMAL(12,0) NOT NULL DEFAULT 0 CHECK (LUONG >= 0),
    MA_RAP          NCHAR(5) NOT NULL,
    NGAYVAOLAM      DATE NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (MA_NHANVIEN) REFERENCES TAIKHOAN(MA_TAIKHOAN),
    FOREIGN KEY (MA_RAP) REFERENCES RAPCHIEU(MA_RAP)
);
GO

CREATE TABLE MONAN (
    MA_MON      NCHAR(5) PRIMARY KEY,
    TEN_MON     NVARCHAR(100) NOT NULL,
    LOAI        NVARCHAR(20) NOT NULL CHECK (LOAI IN (N'BapNuoc', N'NuocUong', N'ComBo', N'Khac')),
    GIA         DECIMAL(10,0) NOT NULL CHECK (GIA >= 0),
    HINHANH     NVARCHAR(255) NULL,
    TRANGTHAI   NVARCHAR(20) NOT NULL DEFAULT N'ConBan' CHECK (TRANGTHAI IN (N'ConBan', N'NgungBan'))
);
GO

/* ============================================================================
   2. CÁC BẢNG LIÊN KẾT & PHÒNG/GHẾ/ĐƠN HÀNG
   ============================================================================ */

CREATE TABLE PHONGCHIEU (
    MA_PHONG    NCHAR(5) PRIMARY KEY,
    TEN_PHONG   NVARCHAR(50) NOT NULL,
    MA_RAP      NCHAR(5) NOT NULL,
    SO_HANG     INT NULL,
    SO_COT      INT NULL,
    FOREIGN KEY (MA_RAP) REFERENCES RAPCHIEU(MA_RAP)
);
GO

CREATE TABLE PHIM_THELOAI (
    MA_PHIM     NCHAR(5) NOT NULL,
    MA_THELOAI  NCHAR(5) NOT NULL,
    PRIMARY KEY (MA_PHIM, MA_THELOAI),
    FOREIGN KEY (MA_PHIM) REFERENCES PHIM(MA_PHIM),
    FOREIGN KEY (MA_THELOAI) REFERENCES THELOAI(MA_THELOAI)
);
GO

CREATE TABLE GHE (
    MA_GHE      NCHAR(5) PRIMARY KEY,
    SOGHE       NVARCHAR(5) NOT NULL,
    MA_PHONG    NCHAR(5) NOT NULL,
    MA_LOAIGHE  NCHAR(5) NULL,
    SO_HANG     INT NOT NULL,
    SO_COT      INT NOT NULL,
    FOREIGN KEY (MA_PHONG) REFERENCES PHONGCHIEU(MA_PHONG),
    FOREIGN KEY (MA_LOAIGHE) REFERENCES LOAIGHE(MA_LOAIGHE),
    CONSTRAINT UQ_GHE_TOADO UNIQUE (MA_PHONG, SO_HANG, SO_COT)
);
GO

CREATE TABLE LICHCHIEU (
    MA_LICHCHIEU    NCHAR(5) PRIMARY KEY,
    MA_PHIM         NCHAR(5) NOT NULL,
    MA_PHONG        NCHAR(5) NOT NULL,
    MA_KHUNGGIO     NCHAR(5) NOT NULL,
    NGAYCHIEU       DATE NOT NULL,
    GIATVE_PHUTROI  DECIMAL(10, 0) DEFAULT 0,
    CONSTRAINT UQ_PHONG_GIO_NGAY UNIQUE (MA_PHONG, MA_KHUNGGIO, NGAYCHIEU),
    FOREIGN KEY (MA_PHIM) REFERENCES PHIM(MA_PHIM),
    FOREIGN KEY (MA_PHONG) REFERENCES PHONGCHIEU(MA_PHONG),
    FOREIGN KEY (MA_KHUNGGIO) REFERENCES KHUNGGIO(MA_KHUNGGIO)
);
GO

CREATE TABLE DONHANG (
    MA_DONHANG    NCHAR(5) PRIMARY KEY,
    MA_KHACHHANG  NCHAR(5) NULL,
    MA_NHANVIEN   NCHAR(5) NULL,
    MA_LICHCHIEU  NCHAR(5) NOT NULL,
    MA_KM         NVARCHAR(10) NULL,
    NGAYDAT       DATETIME NOT NULL DEFAULT GETDATE(),
    TIENVE        DECIMAL(12,0) NOT NULL DEFAULT 0,
    TIENMONAN     DECIMAL(12,0) NOT NULL DEFAULT 0,
    TIENGIAM      DECIMAL(12,0) NOT NULL DEFAULT 0,
    TONGTIEN      DECIMAL(12,0) NOT NULL DEFAULT 0,
    TRANGTHAI     NVARCHAR(20) NOT NULL DEFAULT N'ChoThanhToan' CHECK (TRANGTHAI IN (N'ChoThanhToan', N'DaThanhToan', N'DaHuy')),
    FOREIGN KEY (MA_KHACHHANG) REFERENCES KHACHHANG(MA_KHACHHANG),
    FOREIGN KEY (MA_NHANVIEN) REFERENCES NHANVIEN(MA_NHANVIEN),
    FOREIGN KEY (MA_LICHCHIEU) REFERENCES LICHCHIEU(MA_LICHCHIEU),
    FOREIGN KEY (MA_KM) REFERENCES KHUYENMAI(MA_KM),
    CONSTRAINT CK_DONHANG_NGUOITAO CHECK (MA_KHACHHANG IS NOT NULL OR MA_NHANVIEN IS NOT NULL)
);
GO

CREATE TABLE VE (
    MA_VE         NCHAR(5) PRIMARY KEY,
    MA_DONHANG    NCHAR(5) NOT NULL,
    MA_LICHCHIEU  NCHAR(5) NOT NULL,
    MA_GHE        NCHAR(5) NOT NULL,
    DONGIA        DECIMAL(10,0) NOT NULL,
    TRANGTHAI     NVARCHAR(20) DEFAULT N'Đã đặt' CHECK (TRANGTHAI IN (N'Đã hủy', N'Đã đặt', N'Đã thanh toán', N'Đã check-in')),
    CONSTRAINT UQ_GHE_LICH UNIQUE (MA_GHE, MA_LICHCHIEU),
    FOREIGN KEY (MA_DONHANG) REFERENCES DONHANG(MA_DONHANG),
    FOREIGN KEY (MA_LICHCHIEU) REFERENCES LICHCHIEU(MA_LICHCHIEU),
    FOREIGN KEY (MA_GHE) REFERENCES GHE(MA_GHE)
);
GO

CREATE TABLE DONHANG_MONAN (
    MA_DONHANG  NCHAR(5) NOT NULL,
    MA_MON      NCHAR(5) NOT NULL,
    SOLUONG     INT NOT NULL CHECK (SOLUONG > 0),
    DONGIA      DECIMAL(10,0) NOT NULL,
    PRIMARY KEY (MA_DONHANG, MA_MON),
    FOREIGN KEY (MA_DONHANG) REFERENCES DONHANG(MA_DONHANG),
    FOREIGN KEY (MA_MON) REFERENCES MONAN(MA_MON)
);
GO

/* ============================================================================
   3. NẠP DỮ LIỆU MẪU (SEED DATA)
   ============================================================================ */

INSERT INTO RAPCHIEU (MA_RAP, TEN_RAP, DIACHI, SO_PHONG_TOIDA) VALUES
('A01', N'CinéVerse Huế',     N'Vincom Center, 50 Hùng Vương, TP. Huế', 8),
('A02', N'CinéVerse Đà Nẵng', N'Lotte Mart, 6 Nại Nam, Đà Nẵng', 6),
('A03', N'CinéVerse Hà Nội', N'Tràng Tiền Plaza, 24 Hai Bà Trưng, Hoàn Kiếm, Hà Nội', 6),
('A04', N'CinéVerse TP.HCM', N'Vincom Center Landmark 81, 720A Điện Biên Phủ, TP.HCM', 6);
GO

INSERT INTO PHONGCHIEU (MA_PHONG, TEN_PHONG, MA_RAP, SO_HANG, SO_COT) VALUES
('PA01', N'Phòng 1', 'A01', 2, 4),
('PA02', N'Phòng 2', 'A01', 0, 0),
('PB01', N'Phòng 1', 'A02', 2, 2),
('PC01', N'Phòng 1', 'A03', 15, 16),
('PC02', N'Phòng 2', 'A03', 12, 14),
('PC03', N'Phòng 3', 'A03', 12, 14),
('PC04', N'Phòng 4', 'A03', 12, 14),
('PC05', N'Phòng 5', 'A03', 10, 12),
('PC06', N'Phòng 6', 'A03', 10, 12),
('PD01', N'Phòng 1', 'A04', 15, 16),
('PD02', N'Phòng 2', 'A04', 12, 14),
('PD03', N'Phòng 3', 'A04', 12, 14),
('PD04', N'Phòng 4', 'A04', 12, 14),
('PD05', N'Phòng 5', 'A04', 10, 12),
('PD06', N'Phòng 6', 'A04', 10, 12);
GO

INSERT INTO LOAIGHE (MA_LOAIGHE, TEN_LOAIGHE, GIATVE) VALUES
('LG01', N'Thường', 75000),
('LG02', N'VIP',    110000),
('LG03', N'Đôi',    150000);
GO

INSERT INTO GHE (MA_GHE, SOGHE, MA_PHONG, MA_LOAIGHE, SO_HANG, SO_COT) VALUES
('G0001', 'A1', 'PA01', 'LG01', 1, 1),
('G0002', 'A2', 'PA01', 'LG01', 1, 2),
('G0003', 'A3', 'PA01', 'LG01', 1, 3),
('G0004', 'A4', 'PA01', 'LG01', 1, 4),
('G0005', 'B1', 'PA01', 'LG02', 2, 1),
('G0006', 'B2', 'PA01', 'LG02', 2, 2),
('G0007', 'A1', 'PB01', 'LG01', 1, 1),
('G0008', 'A2', 'PB01', 'LG01', 1, 2),
('G0009', 'B1', 'PB01', 'LG03', 2, 1),

('G0010', 'A1', 'PC01', 'LG01', 1, 1), ('G0011', 'A2', 'PC01', 'LG01', 1, 2), ('G0012', 'A3', 'PC01', 'LG01', 1, 3), ('G0013', 'A4', 'PC01', 'LG01', 1, 4), ('G0014', 'A5', 'PC01', 'LG01', 1, 5), ('G0015', 'A6', 'PC01', 'LG01', 1, 6), ('G0016', 'A7', 'PC01', 'LG01', 1, 7), ('G0017', 'A8', 'PC01', 'LG01', 1, 8), ('G0018', 'A9', 'PC01', 'LG01', 1, 9), ('G0019', 'A10', 'PC01', 'LG01', 1, 10), ('G0020', 'A11', 'PC01', 'LG01', 1, 11), ('G0021', 'A12', 'PC01', 'LG01', 1, 12), ('G0022', 'A13', 'PC01', 'LG01', 1, 13), ('G0023', 'A14', 'PC01', 'LG01', 1, 14), ('G0024', 'A15', 'PC01', 'LG01', 1, 15), ('G0025', 'A16', 'PC01', 'LG01', 1, 16),
('G0026', 'B1', 'PC01', 'LG01', 2, 1), ('G0027', 'B2', 'PC01', 'LG01', 2, 2), ('G0028', 'B3', 'PC01', 'LG01', 2, 3), ('G0029', 'B4', 'PC01', 'LG01', 2, 4), ('G0030', 'B5', 'PC01', 'LG01', 2, 5), ('G0031', 'B6', 'PC01', 'LG01', 2, 6), ('G0032', 'B7', 'PC01', 'LG01', 2, 7), ('G0033', 'B8', 'PC01', 'LG01', 2, 8), ('G0034', 'B9', 'PC01', 'LG01', 2, 9), ('G0035', 'B10', 'PC01', 'LG01', 2, 10), ('G0036', 'B11', 'PC01', 'LG01', 2, 11), ('G0037', 'B12', 'PC01', 'LG01', 2, 12), ('G0038', 'B13', 'PC01', 'LG01', 2, 13), ('G0039', 'B14', 'PC01', 'LG01', 2, 14), ('G0040', 'B15', 'PC01', 'LG01', 2, 15), ('G0041', 'B16', 'PC01', 'LG01', 2, 16),
('G0042', 'C1', 'PC01', 'LG01', 3, 1), ('G0043', 'C2', 'PC01', 'LG01', 3, 2), ('G0044', 'C3', 'PC01', 'LG01', 3, 3), ('G0045', 'C4', 'PC01', 'LG01', 3, 4), ('G0046', 'C5', 'PC01', 'LG01', 3, 5), ('G0047', 'C6', 'PC01', 'LG01', 3, 6), ('G0048', 'C7', 'PC01', 'LG01', 3, 7), ('G0049', 'C8', 'PC01', 'LG01', 3, 8), ('G0050', 'C9', 'PC01', 'LG01', 3, 9), ('G0051', 'C10', 'PC01', 'LG01', 3, 10), ('G0052', 'C11', 'PC01', 'LG01', 3, 11), ('G0053', 'C12', 'PC01', 'LG01', 3, 12), ('G0054', 'C13', 'PC01', 'LG01', 3, 13), ('G0055', 'C14', 'PC01', 'LG01', 3, 14), ('G0056', 'C15', 'PC01', 'LG01', 3, 15), ('G0057', 'C16', 'PC01', 'LG01', 3, 16),
('G0058', 'D1', 'PC01', 'LG02', 4, 1), ('G0059', 'D2', 'PC01', 'LG02', 4, 2), ('G0060', 'D3', 'PC01', 'LG02', 4, 3), ('G0061', 'D4', 'PC01', 'LG02', 4, 4), ('G0062', 'D5', 'PC01', 'LG02', 4, 5), ('G0063', 'D6', 'PC01', 'LG02', 4, 6), ('G0064', 'D7', 'PC01', 'LG02', 4, 7), ('G0065', 'D8', 'PC01', 'LG02', 4, 8), ('G0066', 'D9', 'PC01', 'LG02', 4, 9), ('G0067', 'D10', 'PC01', 'LG02', 4, 10), ('G0068', 'D11', 'PC01', 'LG02', 4, 11), ('G0069', 'D12', 'PC01', 'LG02', 4, 12), ('G0070', 'D13', 'PC01', 'LG02', 4, 13), ('G0071', 'D14', 'PC01', 'LG02', 4, 14), ('G0072', 'D15', 'PC01', 'LG02', 4, 15), ('G0073', 'D16', 'PC01', 'LG02', 4, 16),
('G0074', 'E1', 'PC01', 'LG02', 5, 1), ('G0075', 'E2', 'PC01', 'LG02', 5, 2), ('G0076', 'E3', 'PC01', 'LG02', 5, 3), ('G0077', 'E4', 'PC01', 'LG02', 5, 4), ('G0078', 'E5', 'PC01', 'LG02', 5, 5), ('G0079', 'E6', 'PC01', 'LG02', 5, 6), ('G0080', 'E7', 'PC01', 'LG02', 5, 7), ('G0081', 'E8', 'PC01', 'LG02', 5, 8), ('G0082', 'E9', 'PC01', 'LG02', 5, 9), ('G0083', 'E10', 'PC01', 'LG02', 5, 10), ('G0084', 'E11', 'PC01', 'LG02', 5, 11), ('G0085', 'E12', 'PC01', 'LG02', 5, 12), ('G0086', 'E13', 'PC01', 'LG02', 5, 13), ('G0087', 'E14', 'PC01', 'LG02', 5, 14), ('G0088', 'E15', 'PC01', 'LG02', 5, 15), ('G0089', 'E16', 'PC01', 'LG02', 5, 16),
('G0090', 'F1', 'PC01', 'LG02', 6, 1), ('G0091', 'F2', 'PC01', 'LG02', 6, 2), ('G0092', 'F3', 'PC01', 'LG02', 6, 3), ('G0093', 'F4', 'PC01', 'LG02', 6, 4), ('G0094', 'F5', 'PC01', 'LG02', 6, 5), ('G0095', 'F6', 'PC01', 'LG02', 6, 6), ('G0096', 'F7', 'PC01', 'LG02', 6, 7), ('G0097', 'F8', 'PC01', 'LG02', 6, 8), ('G0098', 'F9', 'PC01', 'LG02', 6, 9), ('G0099', 'F10', 'PC01', 'LG02', 6, 10), ('G0100', 'F11', 'PC01', 'LG02', 6, 11), ('G0101', 'F12', 'PC01', 'LG02', 6, 12), ('G0102', 'F13', 'PC01', 'LG02', 6, 13), ('G0103', 'F14', 'PC01', 'LG02', 6, 14), ('G0104', 'F15', 'PC01', 'LG02', 6, 15), ('G0105', 'F16', 'PC01', 'LG02', 6, 16),
('G0106', 'G1', 'PC01', 'LG02', 7, 1), ('G0107', 'G2', 'PC01', 'LG02', 7, 2), ('G0108', 'G3', 'PC01', 'LG02', 7, 3), ('G0109', 'G4', 'PC01', 'LG02', 7, 4), ('G0110', 'G5', 'PC01', 'LG02', 7, 5), ('G0111', 'G6', 'PC01', 'LG02', 7, 6), ('G0112', 'G7', 'PC01', 'LG02', 7, 7), ('G0113', 'G8', 'PC01', 'LG02', 7, 8), ('G0114', 'G9', 'PC01', 'LG02', 7, 9), ('G0115', 'G10', 'PC01', 'LG02', 7, 10), ('G0116', 'G11', 'PC01', 'LG02', 7, 11), ('G0117', 'G12', 'PC01', 'LG02', 7, 12), ('G0118', 'G13', 'PC01', 'LG02', 7, 13), ('G0119', 'G14', 'PC01', 'LG02', 7, 14), ('G0120', 'G15', 'PC01', 'LG02', 7, 15), ('G0121', 'G16', 'PC01', 'LG02', 7, 16),
('G0122', 'H1', 'PC01', 'LG02', 8, 1), ('G0123', 'H2', 'PC01', 'LG02', 8, 2), ('G0124', 'H3', 'PC01', 'LG02', 8, 3), ('G0125', 'H4', 'PC01', 'LG02', 8, 4), ('G0126', 'H5', 'PC01', 'LG02', 8, 5), ('G0127', 'H6', 'PC01', 'LG02', 8, 6), ('G0128', 'H7', 'PC01', 'LG02', 8, 7), ('G0129', 'H8', 'PC01', 'LG02', 8, 8), ('G0130', 'H9', 'PC01', 'LG02', 8, 9), ('G0131', 'H10', 'PC01', 'LG02', 8, 10), ('G0132', 'H11', 'PC01', 'LG02', 8, 11), ('G0133', 'H12', 'PC01', 'LG02', 8, 12), ('G0134', 'H13', 'PC01', 'LG02', 8, 13), ('G0135', 'H14', 'PC01', 'LG02', 8, 14), ('G0136', 'H15', 'PC01', 'LG02', 8, 15), ('G0137', 'H16', 'PC01', 'LG02', 8, 16),
('G0138', 'I1', 'PC01', 'LG02', 9, 1), ('G0139', 'I2', 'PC01', 'LG02', 9, 2), ('G0140', 'I3', 'PC01', 'LG02', 9, 3), ('G0141', 'I4', 'PC01', 'LG02', 9, 4), ('G0142', 'I5', 'PC01', 'LG02', 9, 5), ('G0143', 'I6', 'PC01', 'LG02', 9, 6), ('G0144', 'I7', 'PC01', 'LG02', 9, 7), ('G0145', 'I8', 'PC01', 'LG02', 9, 8), ('G0146', 'I9', 'PC01', 'LG02', 9, 9), ('G0147', 'I10', 'PC01', 'LG02', 9, 10), ('G0148', 'I11', 'PC01', 'LG02', 9, 11), ('G0149', 'I12', 'PC01', 'LG02', 9, 12), ('G0150', 'I13', 'PC01', 'LG02', 9, 13), ('G0151', 'I14', 'PC01', 'LG02', 9, 14), ('G0152', 'I15', 'PC01', 'LG02', 9, 15), ('G0153', 'I16', 'PC01', 'LG02', 9, 16),
('G0154', 'J1', 'PC01', 'LG02', 10, 1), ('G0155', 'J2', 'PC01', 'LG02', 10, 2), ('G0156', 'J3', 'PC01', 'LG02', 10, 3), ('G0157', 'J4', 'PC01', 'LG02', 10, 4), ('G0158', 'J5', 'PC01', 'LG02', 10, 5), ('G0159', 'J6', 'PC01', 'LG02', 10, 6), ('G0160', 'J7', 'PC01', 'LG02', 10, 7), ('G0161', 'J8', 'PC01', 'LG02', 10, 8), ('G0162', 'J9', 'PC01', 'LG02', 10, 9), ('G0163', 'J10', 'PC01', 'LG02', 10, 10), ('G0164', 'J11', 'PC01', 'LG02', 10, 11), ('G0165', 'J12', 'PC01', 'LG02', 10, 12), ('G0166', 'J13', 'PC01', 'LG02', 10, 13), ('G0167', 'J14', 'PC01', 'LG02', 10, 14), ('G0168', 'J15', 'PC01', 'LG02', 10, 15), ('G0169', 'J16', 'PC01', 'LG02', 10, 16),
('G0170', 'K1', 'PC01', 'LG02', 11, 1), ('G0171', 'K2', 'PC01', 'LG02', 11, 2), ('G0172', 'K3', 'PC01', 'LG02', 11, 3), ('G0173', 'K4', 'PC01', 'LG02', 11, 4), ('G0174', 'K5', 'PC01', 'LG02', 11, 5), ('G0175', 'K6', 'PC01', 'LG02', 11, 6), ('G0176', 'K7', 'PC01', 'LG02', 11, 7), ('G0177', 'K8', 'PC01', 'LG02', 11, 8), ('G0178', 'K9', 'PC01', 'LG02', 11, 9), ('G0179', 'K10', 'PC01', 'LG02', 11, 10), ('G0180', 'K11', 'PC01', 'LG02', 11, 11), ('G0181', 'K12', 'PC01', 'LG02', 11, 12), ('G0182', 'K13', 'PC01', 'LG02', 11, 13), ('G0183', 'K14', 'PC01', 'LG02', 11, 14), ('G0184', 'K15', 'PC01', 'LG02', 11, 15), ('G0185', 'K16', 'PC01', 'LG02', 11, 16),
('G0186', 'L1', 'PC01', 'LG02', 12, 1), ('G0187', 'L2', 'PC01', 'LG02', 12, 2), ('G0188', 'L3', 'PC01', 'LG02', 12, 3), ('G0189', 'L4', 'PC01', 'LG02', 12, 4), ('G0190', 'L5', 'PC01', 'LG02', 12, 5), ('G0191', 'L6', 'PC01', 'LG02', 12, 6), ('G0192', 'L7', 'PC01', 'LG02', 12, 7), ('G0193', 'L8', 'PC01', 'LG02', 12, 8), ('G0194', 'L9', 'PC01', 'LG02', 12, 9), ('G0195', 'L10', 'PC01', 'LG02', 12, 10), ('G0196', 'L11', 'PC01', 'LG02', 12, 11), ('G0197', 'L12', 'PC01', 'LG02', 12, 12), ('G0198', 'L13', 'PC01', 'LG02', 12, 13), ('G0199', 'L14', 'PC01', 'LG02', 12, 14), ('G0200', 'L15', 'PC01', 'LG02', 12, 15), ('G0201', 'L16', 'PC01', 'LG02', 12, 16),
('G0202', 'M1', 'PC01', 'LG02', 13, 1), ('G0203', 'M2', 'PC01', 'LG02', 13, 2), ('G0204', 'M3', 'PC01', 'LG02', 13, 3), ('G0205', 'M4', 'PC01', 'LG02', 13, 4), ('G0206', 'M5', 'PC01', 'LG02', 13, 5), ('G0207', 'M6', 'PC01', 'LG02', 13, 6), ('G0208', 'M7', 'PC01', 'LG02', 13, 7), ('G0209', 'M8', 'PC01', 'LG02', 13, 8), ('G0210', 'M9', 'PC01', 'LG02', 13, 9), ('G0211', 'M10', 'PC01', 'LG02', 13, 10), ('G0212', 'M11', 'PC01', 'LG02', 13, 11), ('G0213', 'M12', 'PC01', 'LG02', 13, 12), ('G0214', 'M13', 'PC01', 'LG02', 13, 13), ('G0215', 'M14', 'PC01', 'LG02', 13, 14), ('G0216', 'M15', 'PC01', 'LG02', 13, 15), ('G0217', 'M16', 'PC01', 'LG02', 13, 16),
('G0218', 'N1', 'PC01', 'LG03', 14, 1), ('G0219', 'N2', 'PC01', 'LG03', 14, 2), ('G0220', 'N3', 'PC01', 'LG03', 14, 3), ('G0221', 'N4', 'PC01', 'LG03', 14, 4), ('G0222', 'N5', 'PC01', 'LG03', 14, 5), ('G0223', 'N6', 'PC01', 'LG03', 14, 6), ('G0224', 'N7', 'PC01', 'LG03', 14, 7), ('G0225', 'N8', 'PC01', 'LG03', 14, 8), ('G0226', 'N9', 'PC01', 'LG03', 14, 9), ('G0227', 'N10', 'PC01', 'LG03', 14, 10), ('G0228', 'N11', 'PC01', 'LG03', 14, 11), ('G0229', 'N12', 'PC01', 'LG03', 14, 12), ('G0230', 'N13', 'PC01', 'LG03', 14, 13), ('G0231', 'N14', 'PC01', 'LG03', 14, 14), ('G0232', 'N15', 'PC01', 'LG03', 14, 15), ('G0233', 'N16', 'PC01', 'LG03', 14, 16),
('G0234', 'O1', 'PC01', 'LG03', 15, 1), ('G0235', 'O2', 'PC01', 'LG03', 15, 2), ('G0236', 'O3', 'PC01', 'LG03', 15, 3), ('G0237', 'O4', 'PC01', 'LG03', 15, 4), ('G0238', 'O5', 'PC01', 'LG03', 15, 5), ('G0239', 'O6', 'PC01', 'LG03', 15, 6), ('G0240', 'O7', 'PC01', 'LG03', 15, 7), ('G0241', 'O8', 'PC01', 'LG03', 15, 8), ('G0242', 'O9', 'PC01', 'LG03', 15, 9), ('G0243', 'O10', 'PC01', 'LG03', 15, 10), ('G0244', 'O11', 'PC01', 'LG03', 15, 11), ('G0245', 'O12', 'PC01', 'LG03', 15, 12), ('G0246', 'O13', 'PC01', 'LG03', 15, 13), ('G0247', 'O14', 'PC01', 'LG03', 15, 14), ('G0248', 'O15', 'PC01', 'LG03', 15, 15), ('G0249', 'O16', 'PC01', 'LG03', 15, 16);
GO

-- Cập nhật lại Kích thước lưới cho PHONGCHIEU từ ghế thực tế
UPDATE PC
SET PC.SO_HANG = G.MaxHang, PC.SO_COT = G.MaxCot
FROM PHONGCHIEU PC
JOIN (
    SELECT MA_PHONG, MAX(SO_HANG) AS MaxHang, MAX(SO_COT) AS MaxCot
    FROM GHE GROUP BY MA_PHONG
) G ON G.MA_PHONG = PC.MA_PHONG;
GO

INSERT INTO THELOAI (MA_THELOAI, TEN_THELOAI) VALUES
('TL01', N'Hành động'), ('TL02', N'Tình cảm'), ('TL03', N'Kinh dị'),
('TL04', N'Hoạt hình'), ('TL05', N'Phiêu lưu'), ('TL06', N'Tâm lý'),
('TL07', N'Hài hước'), ('TL08', N'Khoa học viễn tưởng'), ('TL09', N'Chiến tranh'),
('TL10', N'Lịch sử'), ('TL11', N'Âm nhạc'), ('TL12', N'Thể thao'),
('TL13', N'Hình sự'), ('TL14', N'Gia đình'), ('TL15', N'Bí ẩn'),
('TL16', N'Siêu anh hùng'), ('TL17', N'Thần thoại'), ('TL18', N'Kỳ ảo'),
('TL19', N'Cổ trang'), ('TL20', N'Chính kịch'), ('TL21', N'Tài liệu'),
('TL22', N'Phiêu lưu mạo hiểm'), ('TL23', N'Cảnh sát'), ('TL24', N'Ma thuật');
GO

INSERT INTO PHIM (MA_PHIM, TEN_PHIM, THOILUONG, NGAYKHOICHIEU, MOTA, POSTER, DAO_DIEN, DIEN_VIEN, GIOI_HAN_TUOI, TRANGTHAI, CHIPHI_NHAP) VALUES
('P0001', N'Avengers: Kỷ Nguyên Mới', 150, '2025-01-01', N'Các siêu anh hùng tập hợp để đối mặt với mối đe dọa lớn nhất từ trước đến nay.', 'avengers.jpg', N'Anthony Russo', N'Robert Downey Jr., Chris Evans, Scarlett Johansson', 'T13', N'Đang chiếu', 500000000),
('P0002', N'Thám Tử Conan: Vùng Đất Bóng Tối', 120, '2025-02-01', N'Conan đối mặt với tổ chức Áo Đen trong một vụ án tại Tokyo.', 'conan.jpg', N'Kobun Shizuno', N'Kappei Yamaguchi, Minami Takayama', 'P', N'Đang chiếu', 150000000),
('P0003', N'Kinh Dị Vùng Rừng Sâu', 100, '2025-03-15', N'Nhóm bạn trẻ mắc kẹt trong khu rừng bí ẩn với thứ gì đó đang rình rập.', 'horror.jpg', N'James Wan', N'Diễn viên A, Diễn viên B', 'T18', N'Sắp chiếu', 150000000),
('P0004', N'Hành Trình Của Mèo Mướp', 88, '2025-04-01', N'Cuộc phiêu lưu đáng yêu của chú mèo nhỏ trên con đường tìm về nhà.', 'cat.jpg', N'Hayao Miyazaki', N'Lồng tiếng: Nhiều diễn viên', 'P', N'Sắp chiếu', 150000000),
('P0005', N'Nhà Bà Nữ 2', 125, '2025-02-10', N'Phần tiếp theo của bộ phim gia đình đầy cảm xúc về tình mẫu tử và những bí mật trong gia đình.', 'nhabanu2.jpg', N'Trấn Thành', N'Trấn Thành, Ngọc Giàu, Lê Giang, Uyển Ân', 'T13', N'Đang chiếu', 150000000),
('P0006', N'Oppenheimer', 180, '2025-01-20', N'Câu chuyện về J. Robert Oppenheimer và sự ra đời của bom nguyên tử.', 'oppenheimer.jpg', N'Christopher Nolan', N'Cillian Murphy, Emily Blunt, Robert Downey Jr.', 'T16', N'Đang chiếu', 500000000),
('P0007', N'Barbie', 114, '2025-01-25', N'Barbie và Ken bước vào thế giới thực và khám phá những bài học về cuộc sống.', 'barbie.jpg', N'Greta Gerwig', N'Margot Robbie, Ryan Gosling, America Ferrera', 'T13', N'Đang chiếu', 150000000),
('P0008', N'Wonka', 116, '2025-02-05', N'Hành trình của Willy Wonka trước khi trở thành ông chủ lò sôcôla nổi tiếng.', 'wonka.jpg', N'Paul King', N'Timothée Chalamet, Hugh Grant, Olivia Colman', 'P', N'Đang chiếu', 150000000),
('P0009', N'Aquaman 2: Đế Chế Thất Lạc', 124, '2025-01-15', N'Aquaman phải hợp tác với người anh em bị ghẻ lạnh để bảo vệ Atlantis.', 'aquaman2.jpg', N'James Wan', N'Jason Momoa, Patrick Wilson, Amber Heard', 'T13', N'Đang chiếu', 500000000),
('P0010', N'Godzilla x Kong: Đế Chế Mới', 115, '2025-02-20', N'Godzilla và Kong hợp sức chống lại kẻ thù chung đe dọa sự tồn vong của loài người.', 'godzilla.jpg', N'Adam Wingard', N'Rebecca Hall, Brian Tyree Henry, Dan Stevens', 'T13', N'Đang chiếu', 150000000),
('P0011', N'Five Nights at Freddy''s', 110, '2025-01-30', N'Bảo vệ mắc kẹt trong quán pizza với những con thú robot trở nên sống động về đêm.', 'fnaf.jpg', N'Emma Tammi', N'Josh Hutcherson, Elizabeth Lail, Piper Rubio', 'T16', N'Đang chiếu', 150000000),
('P0012', N'Kẻ Ăn Hồn', 95, '2025-02-25', N'Câu chuyện kinh dị về lời nguyền và những linh hồn bị mắc kẹt trong ngôi nhà hoang.', 'keanhon.jpg', N'Victor Vũ', N'Quỳnh Anh Shyn, Trần Nghĩa, Hạnh Thúy', 'T16', N'Đang chiếu', 150000000),
('P0013', N'Đất Rừng Phương Nam', 120, '2025-01-10', N'Cuộc phiêu lưu của cậu bé An trong vùng đất Nam Bộ thời Pháp thuộc.', 'datrungphuongnam.jpg', N'Nguyễn Quang Dũng', N'Hồng Ánh, Mai Tài Phến, Tuấn Trần', 'P', N'Đang chiếu', 150000000),
('P0014', N'Deadpool 3', 127, '2025-03-01', N'Deadpool trở lại với những pha hành động bạo lực và hài hước đặc trưng.', 'deadpool3.jpg', N'Shawn Levy', N'Ryan Reynolds, Hugh Jackman, Morena Baccarin', 'T18', N'Đang chiếu', 150000000),
('P0015', N'Dune: Phần Hai', 166, '2025-03-10', N'Paul Atreides tiếp tục cuộc chiến giành quyền kiểm soát vũ trụ.', 'dune2.jpg', N'Denis Villeneuve', N'Timothée Chalamet, Zendaya, Austin Butler', 'T13', N'Đang chiếu', 500000000),
('P0016', N'The Fall Guy', 125, '2025-03-05', N'Diễn viên đóng thế bị cuốn vào âm mưu khám phá bí mật đạo diễn mất tích.', 'fallguy.jpg', N'David Leitch', N'Ryan Gosling, Emily Blunt, Aaron Taylor-Johnson', 'T13', N'Đang chiếu', 150000000),
('P0017', N'Joker: Folie à Deux', 138, '2025-05-15', N'Phần tiếp theo của Joker với sự xuất hiện của Harley Quinn.', 'joker2.jpg', N'Todd Phillips', N'Joaquin Phoenix, Lady Gaga, Zazie Beetz', 'T18', N'Sắp chiếu', 150000000),
('P0018', N'Inside Out 2', 100, '2025-06-10', N'Những cảm xúc bên trong cô bé Riley khi bước vào tuổi dậy thì.', 'insideout2.jpg', N'Kelsey Mann', N'Amy Poehler, Phyllis Smith, Lewis Black', 'P', N'Sắp chiếu', 150000000),
('P0019', N'Despicable Me 4', 95, '2025-07-01', N'Gru, Lucy và các Minion trở lại với những cuộc phiêu lưu mới.', 'minion4.jpg', N'Chris Renaud', N'Steve Carell, Kristen Wiig, Will Ferrell', 'P', N'Sắp chiếu', 150000000),
('P0020', N'Kung Fu Panda 4', 94, '2025-08-05', N'Po tiếp tục hành trình bảo vệ Thung lũng Hòa bình với kẻ thù mới.', 'kungfupanda4.jpg', N'Mike Mitchell', N'Jack Black, Awkwafina, Viola Davis', 'P', N'Sắp chiếu', 150000000),
('P0021', N'Furiosa', 148, '2025-10-15', N'Phần tiền truyện của Mad Max kể về hành trình của chiến binh Furiosa.', 'furiosa.jpg', N'George Miller', N'Anya Taylor-Joy, Chris Hemsworth', 'T16', N'Sắp chiếu', 150000000),
('P0022', N'Venom 3', 110, '2025-11-01', N'Venom đối mặt với kẻ thù mới từ vũ trụ symbiote.', 'venom3.jpg', N'Kelly Marcel', N'Tom Hardy, Juno Temple, Chiwetel Ejiofor', 'T13', N'Sắp chiếu', 150000000),
('P0023', N'Cậu Bé Và Chim Diệc', 124, '2025-03-25', N'Bộ phim cuối cùng của huyền thoại Hayao Miyazaki.', 'caubeyachimdiec.jpg', N'Hayao Miyazaki', N'Soma Santoki, Masaki Suda, Aimyon', 'P', N'Sắp chiếu', 150000000),
('P0024', N'Mufasa: Vòng Quanh Thế Giới', 118, '2025-12-20', N'Phần tiền truyện của Vua Sư Tử kể về Mufasa khi còn trẻ.', 'mufasa.jpg', N'Barry Jenkins', N'Aaron Pierre, Kelvin Harrison Jr., Seth Rogen', 'P', N'Sắp chiếu', 150000000),
('P0025', N'Kẻ Trộm Mặt Trăng', 120, '2025-04-10', N'Phim hành động giật gân về một vụ trộm kim cương táo bạo.', 'ketrommattrang.jpg', N'Lưu Thành Trấn', N'Lý Liên Kiệt, Chân Tử Đan, Lưu Đức Hoa', 'T16', N'Sắp chiếu', 150000000),
('P0026', N'Deadpool & Wolverine', 128, '2025-07-26', N'Wolverine trở lại cùng Deadpool trong cuộc phiêu lưu hoành tráng nhất.', 'deadpoolwolverine.jpg', N'Shawn Levy', N'Ryan Reynolds, Hugh Jackman, Emma Corrin', 'T18', N'Sắp chiếu', 150000000),
('P0027', N'Fast X', 141, '2024-12-01', N'Dom Toretto và gia đình đối mặt với kẻ thù nguy hiểm nhất từ trước đến nay.', 'fastx.jpg', N'Louis Leterrier', N'Vin Diesel, Jason Momoa, Michelle Rodriguez', 'T13', N'Ngừng chiếu', 150000000),
('P0028', N'The Marvels', 105, '2024-11-15', N'Carol Danvers, Kamala Khan và Monica Rambeau phải hợp sức.', 'themarvels.jpg', N'Nia DaCosta', N'Brie Larson, Teyonah Parris, Iman Vellani', 'T13', N'Ngừng chiếu', 150000000),
('P0029', N'Indiana Jones 5', 154, '2024-10-20', N'Cuộc phiêu lưu cuối cùng của Indiana Jones.', 'indianajones5.jpg', N'James Mangold', N'Harrison Ford, Phoebe Waller-Bridge, Mads Mikkelsen', 'T13', N'Ngừng chiếu', 150000000),
('P0030', N'Napoleon', 158, '2024-11-10', N'Câu chuyện về sự trỗi dậy và sụp đổ của hoàng đế Napoleon Bonaparte.', 'napoleon.jpg', N'Ridley Scott', N'Joaquin Phoenix, Vanessa Kirby, Ben Miles', 'T16', N'Ngừng chiếu', 150000000);
GO

INSERT INTO PHIM_THELOAI VALUES
('P0001', 'TL01'), ('P0001', 'TL05'),
('P0002', 'TL04'), ('P0002', 'TL06'),
('P0003', 'TL03'),
('P0004', 'TL04'), ('P0004', 'TL05'),
('P0005', 'TL02'), ('P0005', 'TL14'), ('P0005', 'TL06'),
('P0006', 'TL09'), ('P0006', 'TL10'), ('P0006', 'TL20'),
('P0007', 'TL07'), ('P0007', 'TL18'), ('P0007', 'TL02'),
('P0008', 'TL07'), ('P0008', 'TL18'), ('P0008', 'TL14'), ('P0008', 'TL11'),
('P0009', 'TL01'), ('P0009', 'TL16'), ('P0009', 'TL17'), ('P0009', 'TL05'),
('P0010', 'TL01'), ('P0010', 'TL08'), ('P0010', 'TL05'),
('P0011', 'TL03'), ('P0011', 'TL15'),
('P0012', 'TL03'), ('P0012', 'TL06'), ('P0012', 'TL13'),
('P0013', 'TL05'), ('P0013', 'TL10'), ('P0013', 'TL14'),
('P0014', 'TL01'), ('P0014', 'TL07'), ('P0014', 'TL16'), ('P0014', 'TL05'),
('P0015', 'TL08'), ('P0015', 'TL01'), ('P0015', 'TL05'), ('P0015', 'TL20'),
('P0016', 'TL01'), ('P0016', 'TL07'), ('P0016', 'TL13'), ('P0016', 'TL05'),
('P0017', 'TL20'), ('P0017', 'TL13'), ('P0017', 'TL11'),
('P0018', 'TL04'), ('P0018', 'TL07'), ('P0018', 'TL14'),
('P0019', 'TL04'), ('P0019', 'TL07'), ('P0019', 'TL14'),
('P0020', 'TL04'), ('P0020', 'TL01'), ('P0020', 'TL07'), ('P0020', 'TL14'),
('P0021', 'TL01'), ('P0021', 'TL08'), ('P0021', 'TL05'), ('P0021', 'TL09'),
('P0022', 'TL01'), ('P0022', 'TL08'), ('P0022', 'TL16'), ('P0022', 'TL07'),
('P0023', 'TL04'), ('P0023', 'TL05'), ('P0023', 'TL18'), ('P0023', 'TL20'),
('P0024', 'TL04'), ('P0024', 'TL05'), ('P0024', 'TL14'), ('P0024', 'TL02'),
('P0025', 'TL01'), ('P0025', 'TL13'), ('P0025', 'TL15'),
('P0026', 'TL01'), ('P0026', 'TL07'), ('P0026', 'TL16'), ('P0026', 'TL05'),
('P0027', 'TL01'), ('P0027', 'TL05'), ('P0027', 'TL13'),
('P0028', 'TL01'), ('P0028', 'TL08'), ('P0028', 'TL16'), ('P0028', 'TL05'),
('P0029', 'TL01'), ('P0029', 'TL05'), ('P0029', 'TL15'),
('P0030', 'TL09'), ('P0030', 'TL10'), ('P0030', 'TL20');
GO

INSERT INTO KHUNGGIO VALUES
('KG01', '08:00', '10:30'),
('KG02', '10:45', '13:15'),
('KG03', '13:30', '16:00'),
('KG04', '16:15', '18:45'),
('KG05', '19:00', '21:30'),
('KG06', '21:45', '23:59');
GO

INSERT INTO LICHCHIEU VALUES
('LC001', 'P0001', 'PA01', 'KG03', '2025-04-15', 0),
('LC002', 'P0001', 'PA01', 'KG05', '2025-04-15', 20000), 
('LC003', 'P0002', 'PA01', 'KG02', '2025-04-15', 0),
('LC004', 'P0001', 'PA01', 'KG03', '2025-04-16', 0),
('LC005', 'P0015', 'PC01', 'KG05', '2025-04-30', 30000),
('LC006', 'P0001', 'PD01', 'KG06', '2025-04-30', 25000),
('LC007', 'P0005', 'PC02', 'KG03', '2025-04-30', 0),
('LC008', 'P0012', 'PD02', 'KG04', '2025-04-30', 0),
('LC009', 'P0018', 'PC05', 'KG01', '2025-05-01', 10000);
GO

INSERT INTO VAITRO VALUES
('ADMIN', N'Quản trị viên'),
('KHACHHANG', N'Khách hàng'),
('NHANVIEN', N'Nhân viên'),
('QUANLYRAP', N'Quản lý rạp');
GO

INSERT INTO TAIKHOAN (MA_TAIKHOAN, HOTEN, SDT, EMAIL, MATKHAU) VALUES
('TK002', N'Nguyễn Văn An',  '0901234567', 'nguyenvanan@gmail.com',  'hash_matkhau_1'),
('TK003', N'Trần Thị Bình',  '0912345678', 'tranthibinh@gmail.com',  'hash_matkhau_2'),
('TK004', N'Lê Hoàng Cường', '0923456789', 'lehoangcuong@gmail.com', 'hash_matkhau_3'),
('TK001', N'Quản Trị Viên',  '0999999999', 'admin@cineverse.vn',     'hash_admin_pw'),
('TK005', N'Phạm Thị Duyên', '0934567890', 'phamduyen@cineverse.vn', 'hash_nv_1'),
('TK006', N'Hồ Văn Em',      '0945678901', 'hovanem@cineverse.vn',   'hash_nv_2'),
('TK007', N'Ngô Thị Phương', '0956789012', 'ngophuong@cineverse.vn', 'hash_nv_3')
GO

INSERT INTO TAIKHOAN_VAITRO VALUES
('TK001', 'ADMIN'),
('TK002', 'KHACHHANG'),
('TK003', 'KHACHHANG'),
('TK004', 'KHACHHANG'),
('TK005', 'NHANVIEN'),
('TK006', 'QUANLYRAP'),
('TK007', 'NHANVIEN');
GO

INSERT INTO KHACHHANG (MA_KHACHHANG, CCCD) VALUES
('TK001', NULL),
('TK002', '123456789012'),
('TK003', '234567890123'),
('TK004', '345678901234');
GO

INSERT INTO NHANVIEN (MA_NHANVIEN, LUONG, MA_RAP, NGAYVAOLAM) VALUES
('TK005', 6000000, 'A01', '2024-03-01'),
('TK006', 9500000, 'A01', '2023-05-15'),
('TK007', 6000000, 'A02', '2024-08-20');
GO

INSERT INTO KHUYENMAI (MA_KM, TEN_KM, MA_CODE, GIAMGIA_PHANTRAM, GIAMGIA_TOIDA, NGAYBATDAU, NGAYKETTHUC, SO_LUOT_TOIDA) VALUES 
('KM001', N'Giảm giá Tân binh', 'WELCOME20', 20, 50000, '2026-01-01', '2026-12-31', 100);
GO

INSERT INTO MONAN VALUES
('MN001', N'Bắp rang bơ (lớn)',              N'BapNuoc', 55000, 'bap-bo.jpg',      N'ConBan'),
('MN002', N'Bắp phô mai (lớn)',              N'BapNuoc', 60000, 'bap-phomai.jpg',  N'ConBan'),
('MN003', N'Coca-Cola (lớn)',                N'NuocUong',35000, 'coca.jpg',        N'ConBan'),
('MN004', N'Combo đôi (1 bắp lớn + 2 nước)', N'ComBo',   89000, 'combo-doi.jpg',   N'ConBan');
GO

INSERT INTO DONHANG (MA_DONHANG, MA_KHACHHANG, MA_NHANVIEN, MA_LICHCHIEU, MA_KM, NGAYDAT, TIENVE, TIENMONAN, TIENGIAM, TONGTIEN, TRANGTHAI) VALUES
('DH001', 'TK002', NULL, 'LC001', NULL, GETDATE(), 150000, 89000, 0, 239000, N'DaThanhToan'),
('DH002', 'TK003', NULL, 'LC002', NULL, GETDATE(), 130000,     0, 0, 130000, N'DaThanhToan')
GO

INSERT INTO VE (MA_VE, MA_DONHANG, MA_LICHCHIEU, MA_GHE, DONGIA, TRANGTHAI) VALUES
('VE001', 'DH001', 'LC001', 'G0001',  75000, N'Đã thanh toán'),
('VE002', 'DH001', 'LC001', 'G0002',  75000, N'Đã thanh toán'),
('VE003', 'DH002', 'LC002', 'G0005', 130000, N'Đã thanh toán')
GO

INSERT INTO DONHANG_MONAN VALUES 
('DH001', 'MN004', 1, 89000);
GO
