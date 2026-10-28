using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using DoAn_QuanLyVeXemPhimTrucTiep.Models;

namespace DoAn_QuanLyVeXemPhimTrucTiep.Services
{
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _enableSsl;

        public EmailService()
        {
            _smtpHost = ConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
            int.TryParse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587", out _smtpPort);
            _smtpUser = ConfigurationManager.AppSettings["SmtpUser"] ?? "";
            _smtpPass = ConfigurationManager.AppSettings["SmtpPass"] ?? "";
            _fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"] ?? "cineverse.cinema@gmail.com";
            _fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "CinéVerse Cinema Vietnam";
            bool.TryParse(ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true", out _enableSsl);
        }

        /// <summary>
        /// Gửi email xác nhận vé điện tử E-Ticket bất đồng bộ (chạy ngầm, không chặn luồng thanh toán).
        /// Hỗ trợ gọi trực tiếp qua static method: EmailService.SendTicketConfirmationBackground(donHang)
        /// </summary>
        public static void SendTicketConfirmationBackground(DONHANG donHang, string recipientEmail = null)
        {
            var service = new EmailService();
            Task.Run(async () =>
            {
                try
                {
                    await service.SendTicketConfirmationAsync(donHang, recipientEmail);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EmailService] Lỗi gửi email vé {donHang?.MA_DONHANG}: {ex.Message}");
                }
            });
        }

        public async Task<bool> SendTicketConfirmationAsync(DONHANG donHang, string overrideEmail = null)
        {
            if (donHang == null) return false;

            string emailTo = overrideEmail;
            if (string.IsNullOrWhiteSpace(emailTo))
            {
                emailTo = donHang.KHACHHANG?.TAIKHOAN?.EMAIL ?? donHang.KHACHHANG?.EMAIL;
            }

            if (string.IsNullOrWhiteSpace(emailTo) || !emailTo.Contains("@"))
            {
                return false;
            }

            string customerName = donHang.KHACHHANG?.HOTEN ?? donHang.KHACHHANG?.TAIKHOAN?.HOTEN ?? "Quý khách";
            string movieTitle = donHang.LICHCHIEU?.PHIM?.TEN_PHIM ?? "Phim chiếu rạp";
            string cinemaName = donHang.LICHCHIEU?.PHONGCHIEU?.RAPCHIEU?.TEN_RAP ?? "CinéVerse Cinema";
            string roomName = donHang.LICHCHIEU?.PHONGCHIEU?.TEN_PHONG ?? "Phòng chiếu";
            string showDate = donHang.LICHCHIEU?.NGAYCHIEU.ToString("dd/MM/yyyy") ?? "";
            string showTime = donHang.LICHCHIEU?.KHUNGGIO?.GIOBATDAU.ToString(@"hh\:mm") ?? "";

            var seatNames = donHang.VEs != null && donHang.VEs.Any()
                ? string.Join(", ", donHang.VEs.Select(v => v.GHE?.SOGHE?.Trim() ?? v.MA_GHE.Trim()))
                : "Chưa xác định";

            string checkInCode = !string.IsNullOrEmpty(donHang.MACHECKIN) ? donHang.MACHECKIN.Trim() : donHang.MA_DONHANG.Trim();

            // Xây dựng danh sách bắp nước dạng HTML nếu có
            var fnbHtml = new StringBuilder();
            if (donHang.CHITIET_BAPNUOC != null && donHang.CHITIET_BAPNUOC.Any())
            {
                fnbHtml.Append("<div style='margin-top:15px; padding:12px; background:#1b2028; border-radius:8px;'>");
                fnbHtml.Append("<strong style='color:#e5a623; font-size:13px;'>🍿 BẮP NƯỚC & COMBO ĐI KÈM:</strong><br/>");
                foreach (var ct in donHang.CHITIET_BAPNUOC)
                {
                    string spName = ct.BAPNUOC?.TEN_SANPHAM ?? ct.MA_SANPHAM.Trim();
                    fnbHtml.Append($"<span style='color:#fff; font-size:13px;'>• {spName} <strong>x{ct.SOLUONG}</strong> ({ct.THANHTIEN:N0} đ)</span><br/>");
                }
                fnbHtml.Append("</div>");
            }

            string htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #0c0f14; color: #e1e7ec; margin: 0; padding: 20px; }}
        .ticket-box {{ max-width: 600px; margin: 0 auto; background-color: #141820; border-radius: 12px; border: 1px solid #2a313d; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #1f2530 0%, #0d1017 100%); padding: 24px; text-align: center; border-bottom: 2px solid #e5a623; }}
        .logo {{ font-size: 26px; font-weight: 800; color: #e5a623; letter-spacing: 2px; text-transform: uppercase; }}
        .sub {{ font-size: 13px; color: #8e9ca8; margin-top: 4px; }}
        .body {{ padding: 24px; }}
        .movie-title {{ font-size: 22px; font-weight: 700; color: #ffffff; margin-bottom: 8px; }}
        .info-row {{ display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 14px; color: #abb7c2; border-bottom: 1px dashed #222933; padding-bottom: 8px; }}
        .info-val {{ color: #ffffff; font-weight: 600; text-align: right; }}
        .checkin-card {{ background: #1a221a; border: 1px solid #2d5a37; border-radius: 8px; padding: 16px; text-align: center; margin: 20px 0; }}
        .checkin-code {{ font-family: monospace; font-size: 24px; font-weight: 800; color: #4be87c; letter-spacing: 3px; }}
        .total-box {{ background: #1c222c; border-radius: 8px; padding: 14px 18px; margin-top: 15px; text-align: right; }}
        .total-val {{ font-size: 20px; font-weight: 800; color: #e5a623; }}
        .footer {{ padding: 18px; text-align: center; font-size: 12px; color: #62707e; border-top: 1px solid #222933; }}
    </style>
</head>
<body>
    <div class='ticket-box'>
        <div class='header'>
            <div class='logo'>CINÉVERSE CINEMA</div>
            <div class='sub'>XÁC NHẬN ĐẶT VÉ &amp; VÉ ĐIỆN TỬ (E-TICKET)</div>
        </div>
        <div class='body'>
            <p>Xin chào <strong>{customerName}</strong>,</p>
            <p>Cảm ơn bạn đã lựa chọn CinéVerse. Đơn hàng của bạn đã được thanh toán thành công!</p>
            
            <div class='movie-title'>{movieTitle}</div>
            
            <div class='info-row'>
                <span>Mã đơn hàng:</span>
                <span class='info-val'>#{donHang.MA_DONHANG.Trim()}</span>
            </div>
            <div class='info-row'>
                <span>Rạp chiếu:</span>
                <span class='info-val'>{cinemaName}</span>
            </div>
            <div class='info-row'>
                <span>Phòng chiếu:</span>
                <span class='info-val'>{roomName}</span>
            </div>
            <div class='info-row'>
                <span>Suất chiếu:</span>
                <span class='info-val' style='color:#e5a623;'>{showTime} - Ngày {showDate}</span>
            </div>
            <div class='info-row'>
                <span>Ghế ngồi:</span>
                <span class='info-val' style='color:#4be87c;'>{seatNames}</span>
            </div>
            <div class='info-row'>
                <span>Phương thức thanh toán:</span>
                <span class='info-val'>{(donHang.PHUONGTHUCTHANHTOAN ?? "Trực tuyến")}</span>
            </div>

            {fnbHtml}

            <div class='checkin-card'>
                <div style='font-size:12px; color:#85ad8d; text-transform:uppercase;'>Mã Check-in Soát Vé Nhanh</div>
                <div class='checkin-code'>{checkInCode}</div>
                <div style='font-size:11px; color:#85ad8d; margin-top:4px;'>Vui lòng xuất trình mã này tại quầy vé hoặc cửa phòng chiếu</div>
            </div>

            <div class='total-box'>
                <span style='font-size:14px; color:#abb7c2;'>Tổng tiền đã thanh toán: </span>
                <span class='total-val'>{donHang.TONGTIEN:N0} đ</span>
            </div>
        </div>
        <div class='footer'>
            CinéVerse Cinema Vietnam &bull; Đẳng Cấp Trải Nghiệm Điện Ảnh<br/>
            Email tự động gửi từ hệ thống CinéVerse. Vui lòng không trả lời thư này.
        </div>
    </div>
</body>
</html>";

            try
            {
                // Kiểm tra nếu chưa cấu hình mật khẩu SMTP thực thì chỉ mô phỏng gửi thành công
                if (string.IsNullOrWhiteSpace(_smtpPass) || _smtpPass == "your_app_password_here")
                {
                    System.Diagnostics.Debug.WriteLine($"[EmailService DEMO] Đã tạo E-Ticket gửi tới {emailTo} (Mã đơn: {donHang.MA_DONHANG.Trim()})");
                    return true;
                }

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(_fromEmail, _fromName, Encoding.UTF8);
                    mail.To.Add(emailTo);
                    mail.Subject = $"[CinéVerse] Xác nhận vé điện tử: {movieTitle} (#{donHang.MA_DONHANG.Trim()})";
                    mail.Body = htmlBody;
                    mail.IsBodyHtml = true;
                    mail.BodyEncoding = Encoding.UTF8;

                    using (var smtp = new SmtpClient(_smtpHost, _smtpPort))
                    {
                        smtp.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
                        smtp.EnableSsl = _enableSsl;
                        await smtp.SendMailAsync(mail);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailService Exception] {ex.Message}");
                return false;
            }
        }
    }
}
