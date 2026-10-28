# 🎬 CinéVerse - Hệ Thống Quản Trị & Đặt Vé Rạp Chiếu Phim Trực Tuyến Toàn Diện

> **Đồ án môn học:** Lập trình Web - Đại học Công Thương TP.HCM (HUIT)  
> **Nền tảng kỹ thuật:** ASP.NET MVC 5 (.NET Framework 4.7.2), Entity Framework 6 Database First, Microsoft SQL Server  
> **Kiến trúc:** 3-Tier Architecture, Service Layer, Role-Based Access Control (RBAC), Transaction Isolation Serializable  

---

## 📖 Giới Thiệu Dự Án

**CinéVerse** là hệ thống phần mềm toàn diện mô phỏng chuỗi rạp chiếu phim thương mại hiện đại. Hệ thống phục vụ trọn vẹn 3 nhóm đối tượng:
1. **Khách hàng (Audiences):** Khám phá phim, xem trailer, chọn ghế trực quan theo thời gian thực (Real-time Seat Locking), chọn bắp nước / combo ẩm thực, thanh toán trực tuyến qua VNPay Sandbox hoặc VietQR động, nhận vé điện tử E-Ticket với mã QR check-in, tích điểm thành viên và gửi đánh giá xác thực.
2. **Nhân viên quầy & Cửa phòng chiếu (Box Office POS & QR Check-in):** Màn hình bán vé tại quầy tối ưu 1 trang duy nhất (Single-Page Workflow), in vé nhiệt khổ 80mm (K80 Print CSS), máy quét mã QR bằng camera trực tiếp và súng bắn mã vạch, cơ chế âm thanh Web Audio API cảnh báo gian lận chống dùng vé 2 lần.
3. **Quản trị viên (Admin):** Quản lý toàn bộ danh mục phim, lịch chiếu chống trùng giờ, giá vé & loại ghế, phòng chiếu & cụm rạp, bắp nước F&B, nhân sự, kiểm duyệt đánh giá, và Dashboard phân tích dữ liệu kinh doanh đa chiều tích hợp Chart.js cùng tính năng xuất báo cáo Excel/CSV UTF-8 BOM chuẩn tiếng Việt.

---

## 🚀 6 Giai Đoạn Nâng Cấp Hệ Thống (Evolution Milestones)

Dự án được xây dựng và nâng cấp qua 6 giai đoạn tiêu chuẩn công nghiệp:

### 🛡 Giai đoạn 1: Chuẩn hóa Bảo mật, Phân quyền & Tầng Dịch vụ (Service Layer)
- **Bảo mật mật khẩu PBKDF2:** Xây dựng `PasswordHelper` sử dụng `Rfc2898DeriveBytes` với muối ngẫu nhiên (Salt 128-bit) và 10.000 vòng lặp. Không phụ thuộc package ngoài, an toàn trước tấn công từ điển và Rainbow Table.
- **Tương thích ngược tự động (Auto-upgrade hash):** Tự động phát hiện mật khẩu dạng plain-text cũ của dữ liệu seed (ví dụ `123456`) khi đăng nhập thành công và nâng cấp tức thì lên chuỗi hash PBKDF2 an toàn.
- **Custom Authorize Action Filters:** Xây dựng bộ lọc `[AdminAuthorize]` và `[CustomerAuthorize]` thay thế logic kiểm tra Session rải rác. Tự động kiểm tra vai trò `ADMIN`, `NHANVIEN`, `KHACHHANG`.
- **Trang lỗi chuyên biệt:** Xử lý điều hướng chuẩn HTTP 403 (Cấm truy cập), 404 (Không tìm thấy), 500 (Lỗi máy chủ).

### 🍿 Giai đoạn 2: Mở rộng CSDL & Phân hệ Bắp nước / Combo Ẩm thực (F&B Concessions)
- **Cơ sở dữ liệu:** Thiết kế bảng `BAPNUOC` và bảng liên kết `CHITIET_BAPNUOC` trong `QL_VEPHIM.sql` và đồng bộ 3 tầng SSDL, CSDL, MSL trong `QL_RapPhim.edmx`.
- **Dịch vụ F&B (`FnBService`):** Quản lý vòng đời sản phẩm, phân loại (`Combo`, `Bắp`, `Nước`, `Snack`), tự sinh mã sản phẩm (`CBxxx`, `BPxxx`, `NCxxx`), upload hình ảnh poster.
- **Quy trình đặt vé 3 bước (Booking Stepper):**
  - *Bước 1:* Chọn suất chiếu & vị trí ghế ngồi.
  - *Bước 2:* Chọn bắp nước & combo ưu đãi (tăng/giảm số lượng real-time).
  - *Bước 3:* Xác nhận & thanh toán đơn hàng tổng hợp.
- **Quản trị Admin F&B:** Bật/tắt trạng thái mở bán, cảnh báo tồn kho, chỉnh sửa giá bán.

### 💺 Giai đoạn 3: Giữ ghế Thời gian thực (Real-time Seat Locking) & Chống đặt trùng ghế
- **Dịch vụ Giữ ghế Server-side (`SeatHoldService`):** Kiến trúc Singleton thread-safe kết hợp `ConcurrentDictionary` và `MemoryCache`.
- **Thời gian giữ chỗ:** **7 phút** đếm ngược (Countdown Timer) trực quan trên giao diện đặt vé. Tự động giải phóng ghế khi hết hạn hoặc khi người dùng hủy luồng đặt vé.
- **Quy tắc chọn ghế rạp chiếu phim (Cinema Seating Rules):**
  - *Quy tắc ghế đôi (Couple Seat Rule):* Bắt buộc chọn trọn vẹn cả 2 ghế liền kề của một cặp ghế Sweetbox.
  - *Quy tắc chống để trống 1 ghế lẻ (Single-Seat Gap Rule):* Cảnh báo và ngăn chặn khách để trống duy nhất 1 ghế ở giữa hoặc sát lối đi.
- **Đồng bộ trạng thái đa thiết bị (Real-time Polling):** Cơ chế AJAX polling 3.5 giây cập nhật ghế đang bị người khác giữ (`holding`) và ghế đã bán (`booked`).
- **Database Concurrency Control:** Chốt chặn bằng `DbContextTransaction` với mức cô lập **Serializable** đảm bảo không bao giờ xảy ra tình trạng đặt trùng ghế (Double-booking).

### 💳 Giai đoạn 4: Tích hợp Thanh toán Trực tuyến (VNPay & VietQR) & Vé Điện tử E-Ticket
- **Giao dịch đơn hàng (`DONHANG`):** Mở rộng các trường `PHUONGTHUCTHANHTOAN`, `TRANGTHAITHANHTOAN`, `MAGIAODICH`, `MACHECKIN`, `TRANGTHAISOATVE`, `THOIGIANCHECKIN`.
- **Cổng thanh toán VNPay Sandbox:** Tích hợp `VnPayLibrary` chuẩn mã hóa **HMAC-SHA512**, tự động tạo URL thanh toán và xử lý callback an toàn.
- **Mã VietQR động:** Tích hợp chuẩn URL VietQR tự động sinh QR chuyển khoản ngân hàng kèm số tiền và mã đơn hàng chính xác, hỗ trợ xác nhận thanh toán nhanh.
- **Vé điện tử Cinematic E-Ticket:** Thiết kế cuống vé kỹ thuật số sắc nét, hiệu ứng xé cuống vé, mã vạch Barcode và mã QR Check-in riêng biệt.
- **Dịch vụ gửi Email tự động (`EmailService`):** Gửi email xác nhận đặt vé chạy ngầm bất đồng bộ (`Task.Run`), đính kèm link xem vé điện tử.

### 🎟 Giai đoạn 5: Phân hệ Vận hành Rạp (Box Office POS & QR Scanner Check-in) & Quản trị Nhân sự
- **Màn hình Bán vé tại quầy (Box Office POS Single-Page Workflow):**
  - Giao diện siêu tốc 1 màn hình cho nhân viên: Chọn nhanh rạp -> Chọn suất chiếu trong ngày -> Mở sơ đồ ghế -> Chọn nhanh Combo bắp nước.
  - Hỗ trợ chế độ **"Khách vãng lai (Walk-in Guest)"** không bắt buộc tài khoản hội viên.
  - Tự động tính tiền thối lại (Change Due) khi khách trả tiền mặt; hỗ trợ chuyển sang mã QR VietQR tại quầy.
  - Phím tắt bán vé nhanh (**F9** xuất vé, **Esc** làm mới đơn).
  - Xuất phiếu in nhiệt **K80 (80mm)** định dạng CSS `@media print` chuyên dụng cho máy in hóa đơn quầy vé.
- **Màn hình Soát vé & Nhận Bắp nước (Ticket & F&B QR Scanner):**
  - Quét mã QR trực tiếp bằng Camera/Webcam thiết bị (thư viện `html5-qrcode`) và hỗ trợ súng bắn mã vạch USB.
  - Phản hồi âm thanh qua **Web Audio API** (2 tiếng Beep thanh khi hợp lệ; tiếng Buzz trầm kéo dài khi lỗi).
  - Cơ chế chống gian lận: Cảnh báo đỏ nổi bật khi vé **ĐÃ CHECK-IN TRƯỚC ĐÓ** kèm thời gian và tên nhân viên đã soát vé.
  - Nút bấm xác nhận vào phòng chiếu và nút xác nhận giao bắp nước một chạm.
- **Quản trị Nhân sự:** Phân quyền vai trò `ADMIN` và `NHANVIEN`, gán rạp trực, khóa/mở tài khoản, theo dõi lịch sử số đơn xuất POS và số vé đã soát qua cửa.

### 🏆 Giai đoạn 6: Đánh giá Phim xác thực, Tích điểm Thành viên (Loyalty) & Dashboard Phân tích
- **Đánh giá & Bình luận Phim xác thực (Verified Movie Reviews):**
  - Bảng `DANHGIA` liên kết `PHIM` và `KHACHHANG`.
  - **Nghiệp vụ thực tế:** Chỉ khán giả đã mua vé và thanh toán thành công cho bộ phim đó mới được phép gửi đánh giá kèm huy hiệu **"Đã mua vé / Verified Viewer"** uy tín (mỗi khán giả tối đa 1 đánh giá/phim).
  - Điểm trung bình sao (⭐), biểu đồ phân bổ tỷ lệ từ 1 đến 5 sao. Admin có quyền duyệt/ẩn bình luận vi phạm.
- **Chương trình Tích điểm & Hạng thành viên (`LoyaltyService`):**
  - Cột `DIEMTICHLUY` và `HANGTHANHVIEN` trong `KHACHHANG`.
  - Tự động tích điểm khi thanh toán thành công (Online và POS có SĐT): 5% giá trị thanh toán quy đổi thành điểm thưởng.
  - Phân hạng: **Member** (0-499 điểm), **VIP** (500-1499 điểm, tự động chiết khấu 5% tiền vé), **VVIP** (1500+ điểm, tự động chiết khấu 10% tiền vé).
  - Hiển thị **Thẻ thành viên điện tử CinéVerse VIP Card** kỹ thuật số và thanh tiến trình thăng hạng trong Hồ sơ cá nhân.
- **Admin Analytics Dashboard & Xuất Báo cáo:**
  - Tích hợp **Chart.js** đa chiều: Lọc Hôm nay, 7 ngày qua, Tháng này, Tất cả, hoặc Tùy chọn khoảng ngày; lọc theo Cụm rạp.
  - Biểu đồ kép: Doanh thu Vé vs Doanh thu Bắp nước (F&B) theo ngày.
  - Biểu đồ Top 5 phim doanh thu & Tỷ lệ lấp đầy ghế (**Occupancy Rate %**).
  - Biểu đồ Doughnut tỷ trọng doanh thu theo Cụm rạp và Phương thức thanh toán.
  - Tính năng **Xuất Báo Cáo Doanh Thu ra file Excel / CSV chuẩn UTF-8 có BOM (`\uFEFF`)** hiển thị tiếng Việt hoàn hảo, không lỗi font.

---

## 🛠 Kiến Trúc Công Nghệ & Thư Viện

| Thành phần | Công nghệ / Thư viện | Vai trò |
| --- | --- | --- |
| **Backend Core** | C#, ASP.NET MVC 5, .NET Framework 4.7.2 | Bộ khung ứng dụng web mô hình MVC |
| **Data Access** | Entity Framework 6 (Database First / EDMX) | Truy xuất CSDL, quan hệ Navigation, Transaction Serializable |
| **Database** | Microsoft SQL Server (MSSQLSERVER) | Lưu trữ dữ liệu rạp chiếu phim |
| **Cryptography** | `System.Security.Cryptography.Rfc2898DeriveBytes` | Băm mật khẩu PBKDF2 chuẩn NIST |
| **In-Memory Cache** | `System.Runtime.Caching.MemoryCache` | Quản lý phiên giữ ghế 7 phút thread-safe |
| **Payment Gateway** | VNPay Sandbox API (HMAC-SHA512), VietQR dynamic API | Cổng thanh toán trực tuyến |
| **QR Code Scanner** | `html5-qrcode` CDN (JavaScript) | Quét mã QR soát vé qua camera thiết bị |
| **Data Visualization** | `Chart.js` v4 CDN | Vẽ biểu đồ doanh thu kép, occupancy rate, doughnut chart |
| **Audio Feedback** | HTML5 Web Audio API (`AudioContext`) | Tạo âm thanh Beep/Buzz phản hồi khi quét vé |
| **Frontend Framework** | Bootstrap 5, Bootstrap Icons, Google Fonts (DM Sans, Bebas Neue) | Giao diện Cinematic Dark Mode cao cấp |
| **Export Engine** | `System.Text.StringBuilder` + UTF-8 Byte Order Mark (BOM) | Xuất báo cáo CSV tương thích Microsoft Excel |

---

## 🗄 Cấu Trúc Cơ Sở Dữ Liệu `QL_VEPHIM`

Hệ thống bao gồm các bảng cốt lõi và các bảng mở rộng:
1. `RAPCHIEU`: Cụm rạp chiếu phim (Mã rạp, Tên rạp, Địa chỉ, Số phòng).
2. `PHONGCHIEU`: Phòng chiếu phim (Mã phòng, Tên phòng, Mã rạp, Loại màn hình).
3. `LOAIGHE`: Phân loại ghế ngồi (Ghế thường, Ghế VIP, Ghế đôi Sweetbox, Giá vé cơ sở).
4. `GHE`: Tọa độ ghế ngồi (Mã ghế, Mã phòng, Số ghế Hàng-Cột, Mã loại ghế).
5. `PHIM`: Danh mục phim (Mã phim, Tên phim, Thời lượng, Ngày khởi chiếu, Poster, Giới hạn tuổi, Chi phí nhập).
6. `THELOAI` & `PHIM_THELOAI`: Thể loại phim (Hành động, Kinh dị, Hoạt hình, Tình cảm...).
7. `KHUNGGIO`: Khung giờ chiếu phim cố định trong ngày (Giờ bắt đầu, Giờ kết thúc).
8. `LICHCHIEU`: Suất chiếu cụ thể (Mã suất, Phim, Phòng, Khung giờ, Ngày chiếu, Giá vé phụ trội).
9. `TAIKHOAN`: Tài khoản đăng nhập (Mã TK, Họ tên, SĐT, Email, Mật khẩu hash PBKDF2, Ngày tạo, Trạng thái).
10. `VAITRO` & `TAIKHOAN_VAITRO`: Bảng phân quyền người dùng (`ADMIN`, `NHANVIEN`, `KHACHHANG`).
11. `KHACHHANG`: Hồ sơ khách hàng (Mã KH, CCCD, Điểm tích lũy `DIEMTICHLUY`, Hạng thành viên `HANGTHANHVIEN`).
12. `NHANVIEN`: Hồ sơ nhân viên (Mã NV, Lương, Mã rạp trực, Ngày vào làm).
13. `KHUYENMAI`: Voucher giảm giá (Mã KM, Mã Code, % giảm, Giảm tối đa, Số lượt tối đa, Số lượt đã dùng).
14. `DONHANG`: Đơn hàng đặt vé & bắp nước (Mã ĐH, Mã KH, Mã Suất, Tiền vé, Tiền bắp nước, Tiền giảm, Tổng tiền, Phương thức thanh toán, Trạng thái thanh toán, Mã Check-in, Trạng thái soát vé, Thời gian check-in, Mã NV soát vé, Trạng thái nhận bắp nước).
15. `VE`: Vé xem phim chi tiết từng ghế (Mã vé, Mã đơn, Mã suất, Mã ghế, Đơn giá, Trạng thái).
16. `BAPNUOC`: Danh mục sản phẩm & Combo F&B (Mã SP, Tên SP, Phân loại, Giá bán, Số lượng tồn, Trạng thái mở bán).
17. `CHITIET_BAPNUOC`: Chi tiết sản phẩm bắp nước trong đơn hàng (Mã chi tiết, Mã đơn, Mã SP, Số lượng, Đơn giá, Thành tiền).
18. `DANHGIA`: Đánh giá & bình luận phim xác thực (Mã ĐG, Mã phim, Mã KH, Số sao 1-5, Nội dung, Ngày đánh giá, Trạng thái kiểm duyệt).

---

## 👥 Danh Sách Tài Khoản Demo (Phục vụ Bảo Vệ Đồ Án)

Tất cả tài khẩu demo đều được cấu hình mật khẩu mặc định là: **`123456`**

| Vai trò | Số điện thoại | Họ và tên | Chức năng kiểm thử nổi bật |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `0912345678` | Quản Trị Viên Hệ Thống | Toàn quyền Admin: Dashboard phân tích Chart.js, Xuất báo cáo Excel, Quản lý Nhân sự, Quản lý Bắp nước, Lịch chiếu, Phim, Kiểm duyệt đánh giá |
| **Nhân viên rạp (Staff)** | `0987654321` | Trần Thị Nhân Viên | Mở quầy bán vé POS Single-Page, In vé nhiệt K80, Quét mã QR soát vé qua Camera & Kiểm tra gian lận vé |
| **Khách hàng VIP** | `0901234567` | Nguyễn Văn Khách VIP | Hạng **VIP** (750 điểm), thẻ VIP Card vàng sang trọng, tự động giảm 5% giá vé khi đặt chỗ, đã mua vé và có quyền gửi Đánh giá xác thực |
| **Khách hàng Member** | `0933445566` | Lê Thị Thành Viên | Hạng **Member** (150 điểm), thanh tiến trình thăng hạng lên VIP, luồng đặt vé online VNPay & VietQR |

---

## 💻 Hướng Dẫn Cài Đặt & Chạy Ứng Dụng

### 1. Yêu cầu môi trường
- Hệ điều hành: Windows 10 / 11
- Công cụ phát triển: **Visual Studio 2019 / 2022** (hoặc Visual Studio Build Tools có cài ASP.NET & Web Development)
- Hệ quản trị CSDL: **Microsoft SQL Server 2017 trở lên** (Local SQL Server `MSSQLSERVER` hoặc `SQLEXPRESS`)
- Trình duyệt khuyến nghị: Google Chrome / Microsoft Edge / Firefox

### 2. Cài đặt Cơ sở Dữ liệu
1. Mở SQL Server Management Studio (SSMS) hoặc cửa sổ dòng lệnh `sqlcmd`.
2. Mở file script CSDL tổng hợp tại đường dẫn: `CineVerse/QL_VEPHIM.sql`.
3. Thực thi script (**Execute / F5**). Script sẽ tự động tạo CSDL `QL_VEPHIM`, tạo đầy đủ 18 bảng dữ liệu, các ràng buộc khóa ngoại, chỉ mục và nạp dữ liệu mẫu (phim, rạp, phòng, suất chiếu, bắp nước, tài khoản, đánh giá mẫu).

### 3. Cấu hình Chuỗi Kết Nối (`Web.config`)
Mở file `CineVerse/Web.config`, kiểm tra chuỗi kết nối `QL_VEPHIMEntities`:
```xml
<connectionStrings>
  <add name="QL_VEPHIMEntities" 
       connectionString="metadata=res://*/Models.QL_RapPhim.csdl|res://*/Models.QL_RapPhim.ssdl|res://*/Models.QL_RapPhim.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=.;initial catalog=QL_VEPHIM;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" 
       providerName="System.Data.EntityClient" />
</connectionStrings>
```
*(Nếu dùng SQL Express có tên instance khác, thay `data source=.` thành `data source=.\SQLEXPRESS`).*

### 4. Biên dịch & Khởi chạy ứng dụng
1. Mở solution `CineVerse/CineVerse.csproj` trong Visual Studio.
2. Chuột phải vào Project chọn **Restore NuGet Packages**.
3. Nhấn **Ctrl + F5** để chạy ứng dụng với IIS Express.
4. Truy cập các cổng giao diện chính:
   - **Trang chủ Khách hàng:** `http://localhost:xxxx/`
   - **Trang Đăng nhập:** `http://localhost:xxxx/Account/DangNhap`
   - **Màn hình Bán vé tại quầy (POS):** `http://localhost:xxxx/NhanVien/BanVe`
   - **Màn hình Quét mã QR Soát vé:** `http://localhost:xxxx/NhanVien/CheckIn`
   - **Bảng điều khiển Quản trị (Admin):** `http://localhost:xxxx/Admin/Index`

---

## 📱 Kiểm Tra Tính Tương Thích & Responsive

Hệ thống được thiết kế với chuẩn Mobile-First và tương thích hoàn toàn trên:
- **Desktop (1920x1080, 1440x900):** Tối ưu không gian hiển thị biểu đồ Chart.js kép, sơ đồ ghế phòng chiếu rộng, bố cục 3 cột checkout.
- **Tablet (iPad / Galaxy Tab):** Màn hình POS quầy vé hỗ trợ thao tác cảm ứng chạm mượt mà.
- **Mobile (iPhone / Android):**
  - Màn hình Soát vé QR tự động kích hoạt camera sau của điện thoại kèm đèn flash.
  - Thẻ thành viên kỹ thuật số và cuống vé E-Ticket co giãn vừa vặn màn hình điện thoại.
  - Sơ đồ ghế hỗ trợ cuộn ngang linh hoạt không bị tràn khung.

---

## 📜 Bản Quyền & Giảng Viên Hướng Dẫn

- **Học phần:** Lập trình Web - Năm 3  
- **Khoa:** Công nghệ Thông tin - Trường Đại học Công Thương TP.HCM (HUIT)  
- © 2026 **CinéVerse Team**. Tất cả các quyền được bảo lưu.
