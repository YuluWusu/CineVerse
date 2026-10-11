# 🎬 CinéVerse - Modern Cinema Ticket Booking & Management System

> Hệ thống website đặt vé xem phim trực tuyến & quản trị cụm rạp chiếu phim hiện đại được xây dựng trên nền tảng **ASP.NET MVC 5**, **Entity Framework 6** và **Microsoft SQL Server**.

---

<p align="center">
  <img src="https://img.shields.io/badge/.NET%20Framework-4.7.2-purple.svg?style=for-the-badge&logo=dotnet" alt=".NET Framework" />
  <img src="https://img.shields.io/badge/ASP.NET%20MVC-5.2.9-blue.svg?style=for-the-badge&logo=microsoft" alt="ASP.NET MVC 5" />
  <img src="https://img.shields.io/badge/Entity%20Framework-6.5.2-red.svg?style=for-the-badge" alt="Entity Framework 6" />
  <img src="https://img.shields.io/badge/SQL%20Server-2019%2B-orange.svg?style=for-the-badge&logo=microsoftsqlserver" alt="SQL Server" />
  <img src="https://img.shields.io/badge/Bootstrap-5.3-success.svg?style=for-the-badge&logo=bootstrap" alt="Bootstrap 5" />
  <img src="https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge" alt="License" />
</p>

---

## 📖 Giới thiệu Dự án

**CinéVerse** là ứng dụng web toàn diện phục vụ trải nghiệm xem phim của khách hàng và quy trình vận hành rạp chiếu phim chuyên nghiệp. Dự án được thiết kế với giao diện Dark Mode cao cấp, đậm chất điện ảnh, tối ưu hóa trải nghiệm người dùng từ bước tra cứu phim, đặt vé đến khâu thanh toán và nhận vé điện tử.

---

## ✨ Tính năng Nổi bật

### 🌟 Phân hệ Khách hàng (Client Portal)
* **Khám phá phim:** Xem danh sách phim đang chiếu, sắp chiếu, phim đã kết thúc với trailer và thông tin chi tiết (đạo diễn, diễn viên, thời lượng, giới hạn độ tuổi P/T13/T16/T18).
* **Tìm kiếm thông minh:** Tìm kiếm tức thì theo từ khóa (tên phim, diễn viên, đạo diễn, thể loại).
* **Lịch chiếu đa rạp:** Tra cứu lịch chiếu theo ngày, theo cụm rạp và từng khung giờ tiện lợi.
* **Sơ đồ chọn ghế trực quan:**
  * Ma trận ghế ngồi hiển thị trạng thái thời gian thực (ghế trống, ghế đang chọn, ghế đã được đặt).
  * Phân biệt rõ các hạng ghế: **Ghế Thường**, **Ghế VIP**, **Ghế Đôi**.
  * Tự động tính toán tổng giá vé theo loại ghế và phụ trội khung giờ/suất chiếu.
* **Mã giảm giá (Promotion):** Áp dụng voucher khuyến mãi hợp lệ, tính toán mức giảm phần trăm và giới hạn số tiền giảm tối đa.
* **Thanh toán & Hóa đơn:** Xác nhận thông tin người đặt, tạo đơn hàng và mã vé tự động.
* **Quản lý tài khoản:** Đăng ký, đăng nhập bảo mật, tra cứu lịch sử mua vé và mã vé điện tử.

---

### 🛡 Phân hệ Quản trị (Admin Portal)
* **Dashboard trực quan:** Báo cáo tổng quan số lượng vé bán ra, tổng doanh thu, phim nổi bật.
* **Quản lý Phim & Thể loại:** Thêm, sửa, cập nhật poster, trạng thái chiếu, phân loại theo thể loại phim.
* **Quản lý Cụm Rạp & Phòng chiếu:**
  * Quản lý thông tin cụm rạp, địa chỉ chi tiết.
  * Thêm phòng chiếu và tự động khởi tạo ma trận ghế theo số hàng, số cột.
* **Quản lý Lịch chiếu:**
  * Chọn rạp chiếu để lọc danh sách phòng chiếu tương ứng.
  * Tự động kiểm tra trùng lặp lịch chiếu (tránh xung đột cùng phòng, cùng ngày, cùng khung giờ).
* **Quản lý Loại ghế & Bảng giá:** Cấu hình giá vé tiêu chuẩn cho từng phân khúc ghế.
* **Quản lý Mã khuyến mãi:** Thiết lập phần trăm giảm, thời hạn kích hoạt, số lượt sử dụng tối đa.
* **Quản lý Vé & Đơn hàng:** Tra cứu thông tin vé, cập nhật trạng thái đơn hàng.

---

## 🛠 Công nghệ Sử dụng

| Tầng kiến trúc | Công nghệ & Thư viện |
| :--- | :--- |
| **Backend** | C#, ASP.NET MVC 5, .NET Framework 4.7.2 |
| **ORM / Data Access** | Entity Framework 6 (Database First / EDMX) |
| **Cơ sở dữ liệu** | Microsoft SQL Server (Transact-SQL) |
| **Frontend UI** | Razor Views (`.cshtml`), HTML5, CSS3, JavaScript |
| **UI Framework & Icons** | Bootstrap 5, Bootstrap Icons, Google Fonts (DM Sans, Bebas Neue) |
| **Libraries** | jQuery 3.7, Newtonsoft.Json, WebGrease, Modernizr |

---

## 🗄 Cấu trúc Cơ sở Dữ liệu

Cơ sở dữ liệu `QL_VEPHIM` bao gồm các bảng chính:
* `RAPCHIEU` — Cụm rạp chiếu phim (Mã rạp, tên rạp, địa chỉ, số phòng).
* `PHONGCHIEU` — Phòng chiếu trực thuộc từng rạp (Số hàng, số cột).
* `LOAIGHE` — Bảng phân loại ghế & giá vé cơ bản (Thường, VIP, Đôi).
* `GHE` — Tọa độ và vị trí từng ghế ngồi trong phòng.
* `PHIM` & `THELOAI` — Thông tin tác phẩm điện ảnh và phân loại.
* `KHUNGGIO` — Các khung giờ chiếu trong ngày.
* `LICHCHIEU` — Suất chiếu liên kết giữa Phim, Phòng, Khung giờ và Ngày chiếu.
* `KHACHHANG` & `NHANVIEN` — Phân quyền tài khoản người dùng và quản trị.
* `KHUYENMAI` — Chương trình ưu đãi và mã voucher.
* `DONHANG` & `VE` — Giao dịch đặt vé, chi tiết từng vé ngồi và thanh toán.

---

## 🚀 Hướng dẫn Cài đặt & Khởi chạy

### Yêu cầu môi trường
* **Visual Studio 2019 / 2022** (hỗ trợ ASP.NET and web development)
* **Microsoft SQL Server 2017+** & **SQL Server Management Studio (SSMS)**
* **.NET Framework 4.7.2**

### Các bước thực hiện

1. **Clone repository về máy:**
   ```bash
   git clone https://github.com/YuluWusu/CineVerse.git
   ```

2. **Cài đặt Cơ sở dữ liệu:**
   * Mở SSMS, kết nối đến SQL Server Instance của bạn.
   * Mở file script `DoAn_QuanLyVeXemPhimTrucTiep/QL_VEPHIM.sql` và nhấn **Execute (F5)** để tạo CSDL `QL_VEPHIM` cùng dữ liệu mẫu.

3. **Cấu hình Connection String:**
   * Mở solution trong Visual Studio.
   * Mở file `Web.config` và kiểm tra chuỗi kết nối `QL_VEPHIM2Entities`:
     ```xml
     <add name="QL_VEPHIM2Entities" 
          connectionString="metadata=res://*/Models.QL_VEPHIM_DuLieu.csdl|...;provider=System.Data.SqlClient;provider connection string=&quot;data source=YOUR_SERVER_NAME;initial catalog=QL_VEPHIM;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" 
          providerName="System.Data.EntityClient" />
     ```
   *(Thay `YOUR_SERVER_NAME` bằng tên SQL Server trên máy bạn, ví dụ: `.` hoặc `localhost` hoặc `DESKTOP-XXXXX\SQLEXPRESS`).*

4. **Khởi chạy ứng dụng:**
   * Trong Visual Studio, chuột phải vào Solution chọn **Restore NuGet Packages**.
   * Nhấn **Ctrl + F5** (hoặc nút **IIS Express**) để khởi chạy website.

---

## 👤 Tài khoản Trải nghiệm Mẫu

| Phân hệ | Tài khoản (Email / SĐT) | Mật khẩu mặc định | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin@cineverse.vn` | `123456` | Toàn quyền quản trị hệ thống |
| **Khách hàng** | `nguyenvanan@gmail.com` | `123456` | Tài khoản đặt vé mẫu |

---

## 📝 Bản quyền

Dự án được xây dựng phục vụ học tập và nghiên cứu môn **Lập trình Web** - Trường Đại học Công Thương TP.HCM (HUIT).  
© 2026 **CinéVerse**. All rights reserved.