# 🎬 CinéVerse - Hệ Thống Quản Trị & Đặt Vé Rạp Chiếu Phim Trực Tuyến Toàn Diện

> **Đồ án môn học:** Lập trình Web — Khoa Công nghệ Thông tin, Trường Đại học Công Thương TP.HCM (HUIT)  
> **Nền tảng kỹ thuật:** ASP.NET MVC 5 (.NET Framework 4.7.2), Entity Framework 6 Database First, Microsoft SQL Server  
> **Kiến trúc:** 3-Tier Layered Architecture, Service Layer, Role-Based Access Control (RBAC), Serializable Transaction

---

## 📖 Giới Thiệu Dự Án

**CinéVerse** là hệ thống phần mềm mô phỏng chuỗi rạp chiếu phim thương mại hiện đại, phục vụ trọn vẹn **3 nhóm đối tượng**:

1. **Khách hàng (Audiences):**
   - Duyệt danh sách phim, lọc theo **Menu 2 cấp động từ CSDL** (`THELOAI`, `RAPCHIEU`), tìm kiếm đa tiêu chí, sắp xếp linh hoạt.
   - Xem chi tiết phim (trailer, xử lý chuỗi diễn viên/quy định `<ul><li>`, danh sách **Phim cùng thể loại**, đánh giá xác thực **Verified Viewer**).
   - Đặt mua bắp nước & combo tại **Cửa hàng F&B** qua **Giỏ hàng Session** (`Session["GioHang"]`) hỗ trợ tùy chọn size, cập nhật số lượng realtime bằng AJAX.
   - Quy trình đặt vé 3 bước trực quan: chọn ghế thời gian thực (**Real-time Seat Locking 7 phút**, chống ghế mồ côi & bắt buộc chọn cặp ghế đôi) $\to$ chọn combo F&B $\to$ thanh toán kép (**VNPay Sandbox** / **VietQR Napas247** / **Tại quầy**).
   - Nhận vé điện tử **E-Ticket** kèm mã QR check-in, email xác nhận tự động chạy ngầm và tích điểm nâng hạng **Member / VIP / VVIP**.
2. **Nhân viên quầy & Cửa phòng chiếu (Box Office POS & QR Check-in):**
   - Màn hình bán vé tại quầy tối ưu 1 trang duy nhất (**Single-Page Workflow**), hỗ trợ khách vãng lai, phím tắt nhanh (`F9`, `Esc`) và **in vé nhiệt khổ 80mm (K80)**.
   - Máy quét mã QR soát vé và giao bắp nước bằng **Camera thiết bị** (`html5-qrcode`) hoặc súng bắn mã vạch, phản hồi âm thanh **Web Audio API** và cảnh báo đỏ chống gian lận dùng vé 2 lần.
3. **Quản trị viên (Admin):**
   - Quản trị Phim (upload poster, bắt buộc cụm **Radio Button** Trạng thái/Độ tuổi & **Checkbox N-N** chọn nhiều thể loại), quản lý **Thể loại đủ 4 chức năng** (Thêm, Xóa, Sửa, Hiển thị kèm kiểm tra khóa ngoại).
   - Quản lý Rạp chiếu, Phòng chiếu, Loại ghế, Khung giờ, Lịch chiếu chống trùng suất, Bắp nước F&B, Khuyến mãi, Đơn hàng, Vé, Nhân sự & Kiểm duyệt đánh giá.
   - **Analytics Dashboard** tích hợp **Chart.js** đa chiều (doanh thu vé vs F&B, Top 5 phim & Occupancy Rate %, tỷ trọng rạp/thanh toán) và xuất báo cáo **Excel/CSV UTF-8 BOM**.

---

## 🚀 Các Giai Đoạn Phát Triển & Nâng Cấp Hệ Thống

### 🛡 Giai đoạn 1: Bảo mật, Phân quyền & Tầng Dịch vụ (Service Layer)
- **Bảo mật mật khẩu PBKDF2:** `PasswordHelper` sử dụng `Rfc2898DeriveBytes` với Salt 128-bit và 10.000 vòng lặp, chống tấn công Rainbow Table.
- **Phòng chống CSRF & Phân quyền RBAC:** Áp dụng `[ValidateAntiForgeryToken]` trên toàn bộ form POST, cùng bộ lọc `[AdminAuthorize]` / kiểm tra vai trò `ADMIN`, `NHANVIEN`, `KHACHHANG`.
- **Trang lỗi chuyên biệt:** Xử lý chuẩn HTTP 403, 404, 500.

### 🍿 Giai đoạn 2: Giỏ Hàng Session & Phân Hệ Bắp Nước F&B
- **Giỏ hàng (`GioHangController`):** Lưu trữ `Session["GioHang"]`, hỗ trợ mua 1 hoặc nhiều sản phẩm kèm tùy chọn kích cỡ, tăng/giảm số lượng AJAX, xóa từng món, xóa toàn bộ, áp mã voucher và lưu đồng bộ vào `DONHANG`, `VE`, `CHITIET_BAPNUOC`.
- **Cửa hàng F&B (`CuaHangBapNuoc`):** Trưng bày đầy đủ **16 hình ảnh sản phẩm thật** (`Content/img/fnb/`) cho bắp rang, nước uống, snack và combo.

### 💺 Giai đoạn 3: Giữ Ghế Thời Gian Thực & Chống Đặt Trùng
- **Dịch vụ `SeatHoldService`:** Singleton thread-safe với `ConcurrentDictionary`, khóa giữ ghế **7 phút** (Countdown Timer), tự động giải phóng khi hết hạn hoặc hủy.
- **Quy tắc ghế rạp chiếu phim:** Bắt buộc chọn theo cặp đối với ghế đôi Sweetbox; cảnh báo và chặn lỗi để trống 1 ghế lẻ (Single-Seat Gap Rule).
- **Database Concurrency Control:** Chốt chặn giao dịch bằng `IsolationLevel.Serializable` triệt tiêu tình trạng đặt trùng ghế (Double-booking).

### 💳 Giai đoạn 4: Thanh Toán Trực Tuyến, E-Ticket & Email Ngầm
- **Thanh toán kép:** VNPay Sandbox (chuẩn ký **HMAC-SHA512**) và VietQR Napas247 động kèm mã đơn hàng.
- **Vé điện tử E-Ticket & Email (`EmailService`):** Phát hành vé kỹ thuật số kèm mã QR Check-in và gửi email xác nhận bất đồng bộ (`Task.Run`), có chế độ mô phỏng an toàn khi chưa cấu hình SMTP thực.

### 🎟 Giai đoạn 5: Quầy Bán Vé POS, In K80 & Camera QR Check-in
- **Box Office POS:** Chọn suất chiếu $\to$ chọn ghế $\to$ chọn combo $\to$ tính tiền thối $\to$ in hóa đơn nhiệt **K80**.
- **QR Check-in Scanner:** Quét QR bằng camera trình duyệt (`html5-qrcode`), phát âm thanh Beep/Buzz qua Web Audio API, khóa vé sau khi soát và cảnh báo gian lận nếu quét lại.

### 🏆 Giai đoạn 6: Đánh Giá Xác Thực, Loyalty VIP & Dashboard Chart.js
- **Verified Movie Reviews (`DANHGIA`):** Chỉ khách đã mua vé xem phim đó mới được chấm sao và bình luận.
- **Tích điểm & Hạng thành viên (`LoyaltyService`):** Tự động tích điểm và thăng hạng **Member** $\to$ **VIP** (giảm 5% vé) $\to$ **VVIP** (giảm 10% vé) hiển thị trên Thẻ hội viên điện tử.
- **Giao diện Dark Cinema & Typography tiếng Việt:** Sử dụng font **Be Vietnam Pro**, hiệu ứng nền ảnh mờ (**Blurred Background + Glassmorphism**) cho Đăng nhập, Đăng ký, Hồ sơ, Lịch sử vé, banner ngang rạp chiếu phim tại trang **Giới thiệu** và **Cinematic Mesh Gradient** đồng bộ toàn hệ thống.

---

## 🛠 Kiến Trúc Công Nghệ & Thư Viện

| Thành phần | Công nghệ / Thư viện | Vai trò |
| --- | --- | --- |
| **Backend Core** | C#, ASP.NET MVC 5, .NET Framework 4.7.2 | Bộ khung ứng dụng web mô hình MVC |
| **Data Access** | Entity Framework 6 (Database First / EDMX) | Truy xuất CSDL, Navigation Properties, Serializable Transaction |
| **Database** | Microsoft SQL Server (MSSQLSERVER) | Lưu trữ dữ liệu rạp chiếu phim `QL_VEPHIM` |
| **Cryptography** | `System.Security.Cryptography.Rfc2898DeriveBytes` | Băm mật khẩu PBKDF2 chuẩn NIST |
| **In-Memory Cache** | `ConcurrentDictionary` & `MemoryCache` | Quản lý phiên giữ ghế 7 phút thread-safe |
| **Payment Gateway** | VNPay Sandbox API (HMAC-SHA512), VietQR Napas247 | Cổng thanh toán trực tuyến |
| **QR Code** | `QRCoder` (.NET) & `html5-qrcode` (JS) | Sinh mã QR vé điện tử & quét QR qua camera thiết bị |
| **Data Visualization** | `Chart.js` CDN | Biểu đồ doanh thu kép, occupancy rate %, doughnut chart |
| **Frontend UI** | Bootstrap 5.3, Bootstrap Icons, Google Fonts (**Be Vietnam Pro**) | Giao diện Dark Cinema, Glassmorphism & Mesh Gradient |
| **Export Engine** | `StringBuilder` + UTF-8 Byte Order Mark (BOM) | Xuất báo cáo CSV/Excel hiển thị chuẩn tiếng Việt |

---

## 🗄 Cơ Sở Dữ Liệu `QL_VEPHIM`

Script `CineVerse/QL_VEPHIM.sql` tự động tạo mới hoàn toàn CSDL chuẩn 3NF kèm bộ dữ liệu mẫu phong phú (**4 cụm rạp, 15 phòng chiếu, 2.496 ghế, 30 phim, 24 thể loại, 179 suất chiếu**):
- **Hệ thống Tài khoản & Phân quyền:** `TAIKHOAN`, `VAITRO`, `TAIKHOAN_VAITRO`, `KHACHHANG` (`DIEMTICHLUY`, `HANGTHANHVIEN`), `NHANVIEN`.
- **Hạ tầng Rạp & Phim:** `RAPCHIEU`, `PHONGCHIEU`, `LOAIGHE`, `GHE`, `PHIM`, `THELOAI`, `PHIM_THELOAI`, `KHUNGGIO`, `LICHCHIEU`.
- **Giao dịch, F&B & Tương tác:** `KHUYENMAI`, `DONHANG`, `VE`, `BAPNUOC`, `CHITIET_BAPNUOC`, `DANHGIA`.

---

## 👥 Danh Sách Tài Khoản Demo & Đăng Nhập Nhanh

Trang Đăng nhập (`/Account/DangNhap`) đã tích hợp sẵn **nút Đăng nhập nhanh 1-Click** phục vụ kiểm thử và bảo vệ đồ án:

| Vai trò | Số điện thoại | Mật khẩu trong DB | Họ và tên | Chức năng kiểm thử nổi bật |
| :--- | :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `0999999999` | `hash_admin_pw` | Quản Trị Viên | Toàn quyền Admin: Dashboard Chart.js, Xuất Excel, Quản lý Phim (Radio/Checkbox), Thể loại, Nhân sự, Lịch chiếu, Đơn hàng *(Có nút 1-Click)* |
| **Nhân viên rạp (Staff)** | `0934567890` | `hash_nv_1` | Phạm Thị Duyên | Quầy bán vé POS Single-Page, In vé nhiệt K80, Máy quét mã QR Check-in qua Camera |
| **Khách hàng (Member)** | `0901234567` | `hash_matkhau_1` | Nguyễn Văn An | Đặt vé giữ ghế 7 phút, Giỏ hàng F&B, VNPay/VietQR, Vé điện tử QR, Đánh giá xác thực, Hồ sơ thẻ thành viên *(Có nút 1-Click)* |
| **Khách hàng (Member)** | `0912345678` | `hash_matkhau_2` | Trần Thị Bình | Tài khoản khách hàng bổ sung kiểm thử luồng giữ ghế đồng thời trên 2 trình duyệt |

---

## 💻 Hướng Dẫn Cài Đặt & Chạy Ứng Dụng

### 1. Yêu cầu môi trường
- **Hệ điều hành:** Windows 10 / 11 (64-bit)
- **Công cụ phát triển:** Visual Studio 2022 trở lên (cài sẵn workload *ASP.NET and web development*, .NET Framework 4.7.2)
- **Hệ quản trị CSDL:** Microsoft SQL Server 2019 / 2022 (`MSSQLSERVER` hoặc `SQLEXPRESS`)

### 2. Cài đặt Cơ sở Dữ liệu
- **Cách 1 (Khuyên dùng - SSMS):** Mở **SQL Server Management Studio**, mở file `CineVerse/QL_VEPHIM.sql` và nhấn **F5 (Execute)**.
- **Cách 2 (Dòng lệnh `sqlcmd`):** Bắt buộc truyền cờ **`-f 65001`** (UTF-8) để giữ nguyên dấu tiếng Việt:
  ```powershell
  sqlcmd -S . -E -f 65001 -i "CineVerse\QL_VEPHIM.sql"
  ```

### 3. Cấu hình Chuỗi Kết Nối (`ConnectionStrings.config`)
> ⚠️ **Lưu ý quan trọng:** File `CineVerse/Web.config` sử dụng `<connectionStrings configSource="ConnectionStrings.config" />` và `ConnectionStrings.config` nằm trong `.gitignore` để tránh xung đột cấu hình giữa các thành viên. Tuyệt đối **không** viết thẻ `<add>` trực tiếp vào bên trong `<connectionStrings>` của `Web.config`.

Hãy tạo mới file **`CineVerse/ConnectionStrings.config`** với nội dung sau (thay `data source=.` bằng tên SQL Server trên máy bạn nếu cần, ví dụ `.\SQLEXPRESS`):
```xml
<connectionStrings>
  <add name="QL_VEPHIMEntities"
       connectionString="metadata=res://*/Models.QL_RapPhim.csdl|res://*/Models.QL_RapPhim.ssdl|res://*/Models.QL_RapPhim.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=.;initial catalog=QL_VEPHIM;integrated security=True;trustservercertificate=True;MultipleActiveResultSets=True;App=EntityFramework&quot;"
       providerName="System.Data.EntityClient" />
</connectionStrings>
```

### 4. Biên dịch & Khởi chạy ứng dụng
1. Mở `CineVerse/CineVerse.csproj` (hoặc Solution) trong Visual Studio.
2. Chuột phải vào Project chọn **Restore NuGet Packages**, sau đó Build Solution.
3. Nhấn **Ctrl + F5** để khởi chạy IIS Express và truy cập:
   - **Trang chủ Khách hàng:** `https://localhost:xxxx/`
   - **Trang Giới thiệu:** `https://localhost:xxxx/Home/GioiThieu`
   - **Trang Đăng nhập:** `https://localhost:xxxx/Account/DangNhap`
   - **Cửa hàng Bắp nước & Giỏ hàng:** `https://localhost:xxxx/GioHang/CuaHangBapNuoc`
   - **Quầy Nhân viên (POS / Check-in):** `https://localhost:xxxx/NhanVien/BanVe` · `/NhanVien/CheckIn`
   - **Quản trị Admin:** `https://localhost:xxxx/Admin/Index`

---

## 👨‍💻 Đội Ngũ Thực Hiện (Khoa CNTT — HUIT)

| STT | Họ và Tên | Vai trò chính | Phụ trách chuyên môn |
| :---: | :--- | :--- | :--- |
| **1** | **Hoàng Xuân Phi Long** | *Nhóm trưởng · System Architect* | Thiết kế CSDL `QL_VEPHIM`, EF6, PBKDF2 & Anti-CSRF, `SeatHoldService`, VNPay/VietQR, `MenuHaiCap`, Quản trị hạ tầng Admin, Hợp nhất Git & Báo cáo |
| **2** | **Tô Hồng Vũ** | *UI/UX & Client Portal* | Layout `_Khung`, Dark Cinema UI/Gradient, Tài khoản & Quick Login, Danh mục/Chi tiết phim (`<ul><li>`, SP liên quan), Module Đánh giá, Quản trị Nhân sự & Seed 179 suất chiếu / 2.496 ghế |
| **3** | **Ngô Hoàng Anh Phát** | *Cart · Booking & Loyalty* | Giỏ hàng `GioHangController`, Cửa hàng F&B (16 ảnh), Stepper 3 bước, `LoyaltyService` (VIP/VVIP), Lịch sử vé, E-Ticket QR & `EmailService` |
| **4** | **Nguyễn Tấn Hiệu** | *Admin · Analytics & POS* | Quản lý Phim (Radio Button & Checkbox N-N), CRUD Thể loại 4 chức năng, Quản lý F&B/Voucher/Đơn hàng/Vé, Dashboard Chart.js & Xuất Excel, Quầy POS in K80 & Camera QR Check-in |

---

## 📜 Bản Quyền & Học Phần

- **Học phần:** Lập trình Web — Năm 3  
- **Khoa:** Công nghệ Thông tin — Trường Đại học Công Thương TP.HCM (HUIT)  
- © 2026 **CinéVerse Team**.
