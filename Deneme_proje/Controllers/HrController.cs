using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using static Deneme_proje.Models.HrEntities;
using System.Net.Mail;
using System.Net;

namespace Deneme_proje.Controllers
{
    public class HrController : BaseController
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly EmailNotificationService _emailService;  // Yeni eklenen
        public HrController(
         IConfiguration configuration,
         DatabaseSelectorService dbSelectorService,
         EmailNotificationService emailService)  // Yeni parametre
        {
            _configuration = configuration;
            _dbSelectorService = dbSelectorService;
            _emailService = emailService;  // Atama
        }

        // HrController'da IzinTalepFormu action'ında:
        public async Task<IActionResult> IzinTalepFormu()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                // MikroDB_V16'dan kullanıcı bilgilerini al
                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                string userNo = null;

                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    string userQuery = "SELECT User_no FROM KULLANICILAR WHERE User_name = @username";

                    using (SqlCommand command = new SqlCommand(userQuery, mikroConnection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        var result = await command.ExecuteScalarAsync();
                        userNo = result?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(userNo))
                {
                    string erpConnectionString = _dbSelectorService.GetConnectionString();

                    using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                    {
                        await erpConnection.OpenAsync();
                        string personnelQuery = @"SELECT per_kod, per_adi, per_soyadi 
                                            FROM PERSONELLER 
                                            WHERE per_UserNo = @userNo";

                        using (SqlCommand command = new SqlCommand(personnelQuery, erpConnection))
                        {
                            command.Parameters.AddWithValue("@userNo", userNo);
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    ViewBag.PersonnelCode = reader["per_kod"]?.ToString();
                                    ViewBag.PersonnelName = reader["per_adi"]?.ToString();
                                    ViewBag.PersonnelSurname = reader["per_soyadi"]?.ToString();
                                }
                            }
                        }
                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hata: {ex.Message}");
                return View();
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> IzinTalepKaydet(string talepTarihi, string izinTipi, string eksikNedeni,
          int izinGun, int izinSaat, string baslangicTarihi, string bitisTarihi, string iseBaslamaTarihi, string baslamaSaat,
          string izinAmaci, string personnelCode)
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                // Personel kodunu doğrudan kullan
                string persKod = personnelCode;

                // Kullanıcı bilgilerini alalım
                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                string userNo = null;

                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    string userQuery = "SELECT User_no FROM KULLANICILAR WHERE User_name = @username";

                    using (SqlCommand command = new SqlCommand(userQuery, mikroConnection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        var result = await command.ExecuteScalarAsync();
                        userNo = result?.ToString();
                    }
                }

                if (string.IsNullOrEmpty(persKod) || string.IsNullOrEmpty(userNo))
                {
                    return Json(new { success = false, message = "Personel veya kullanıcı bilgisi bulunamadı." });
                }

                Guid izinGuid = Guid.NewGuid(); // Yeni GUID oluştur
                string sessionKey = "IseBaslamaTarihi_" + izinGuid.ToString();
                HttpContext.Session.SetString(sessionKey, iseBaslamaTarihi);
                int izinSatirNo = 0;

                string connectionString = _dbSelectorService.GetConnectionString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Önce satır numarası al
                    string getMaxSatirNo = "SELECT ISNULL(MAX(pit_satir_no), 0) + 1 FROM PERSONEL_IZIN_TALEPLERI WHERE pit_pers_kod = @persKod";
                    using (SqlCommand command = new SqlCommand(getMaxSatirNo, connection))
                    {
                        command.Parameters.AddWithValue("@persKod", persKod);
                        var result = await command.ExecuteScalarAsync();
                        izinSatirNo = Convert.ToInt32(result);
                    }

                    string insertQuery = @"INSERT INTO PERSONEL_IZIN_TALEPLERI (
                pit_guid,
                pit_pers_kod,
                pit_talep_tarihi,
                pit_izin_tipi,
                pit_eksikcalismanedeni,
                pit_gun_sayisi,
                pit_yol_izni,
                pit_baslangictarih,
                pit_BaslamaSaati,
                pit_saat,
                pit_amac,
                pit_izin_durum,
                pit_onaylayan_kullanici,
                pit_create_user,
                pit_create_date,
                pit_lastup_user,
                pit_lastup_date,
                pit_special1,
                pit_special2,
                pit_special3,
                pit_iptal,
                pit_hidden,
                pit_kilitli,
                pit_degisti,
                pit_mali_yil,
                pit_satir_no,
                pit_SpecRECno,
                pit_fileid,
                pit_checksum,
                pit_cadde,
                pit_mahalle,
                pit_sokak,
                pit_il,
                pit_ulke,
                pit_Semt,
                pit_Apt_No,
                pit_Daire_No,
                pit_posta_kodu,
                pit_ilce,
                pit_adres_kodu,
                pit_tel1,
                pit_tel2,
                pit_email,
                pit_aciklama1,
                pit_aciklama2
            ) VALUES (
                @izinGuid,
                @persKod,
                @talepTarihi,
                @izinTipi,
                @eksikNedeni,
                @izinGun,
                0, -- Yol izni varsayılan olarak 0
                @baslangicTarihi,
                @baslamaSaat,
                @izinSaat,
                @izinAmaci,
                0, -- izin_durum varsayılan olarak 0 (beklemede)
                0, -- onaylayan kullanıcı varsayılan olarak 0
                @createUser,
                GETDATE(),
                @lastupUser,
                GETDATE(),
                ' ',
                ' ',
                ' ',
                0,
                0,
                0,
                0,
                YEAR(GETDATE()),
                @izinSatirNo,
                0,
                229,
                0,
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' ',
                ' '
            )";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@izinGuid", izinGuid);
                        command.Parameters.AddWithValue("@persKod", persKod);
                        command.Parameters.AddWithValue("@talepTarihi", Convert.ToDateTime(talepTarihi));
                        command.Parameters.AddWithValue("@izinTipi", Convert.ToByte(izinTipi));
                        command.Parameters.AddWithValue("@eksikNedeni", !string.IsNullOrEmpty(eksikNedeni) ? Convert.ToByte(eksikNedeni) : (byte)0);
                        command.Parameters.AddWithValue("@izinGun", izinGun);
                        command.Parameters.AddWithValue("@baslangicTarihi", Convert.ToDateTime(baslangicTarihi));
                        command.Parameters.AddWithValue("@izinSatirNo", izinSatirNo);

                        // Başlama saati formatlaması - eğer varsa
                        if (!string.IsNullOrEmpty(baslamaSaat))
                        {
                            try
                            {
                                command.Parameters.AddWithValue("@baslamaSaat", Convert.ToDateTime(baslamaSaat).TimeOfDay.TotalHours);
                            }
                            catch
                            {
                                command.Parameters.AddWithValue("@baslamaSaat", 0); // Dönüştürme hatasında varsayılan değer
                            }
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@baslamaSaat", 0);
                        }

                        command.Parameters.AddWithValue("@izinSaat", izinSaat); // izinSaat değeri pit_saat alanına kaydedilecek
                        command.Parameters.AddWithValue("@izinAmaci", !string.IsNullOrEmpty(izinAmaci) ? izinAmaci : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@createUser", userNo);
                        command.Parameters.AddWithValue("@lastupUser", userNo);

                        await command.ExecuteNonQueryAsync();
                    }

                    // İzin kaydını gerçekleştirdikten sonra idari amire e-posta gönderme
                    await SendNotificationToManagerAsync(persKod, izinGuid, connection);

                    // İzin kaydını gerçekleştirdikten sonra izinId değerini döndürme
                    HttpContext.Session.SetString("LastIzinGuid", izinGuid.ToString());
                    HttpContext.Session.SetString("LastIzinTarihi", baslangicTarihi);
                    HttpContext.Session.SetString("LastIseBaslamaTarihi", iseBaslamaTarihi);
                    HttpContext.Session.SetString("LastIzinTipi", izinTipi);
                    HttpContext.Session.SetString("LastIzinAmaci", izinAmaci ?? "");
                }

                return Json(new { success = true, message = "İzin talebi başarıyla kaydedildi.", izinGuid = izinGuid });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        // İdari amire e-posta gönderme metodu
        private async Task SendNotificationToManagerAsync(string personelKodu, Guid izinGuid, SqlConnection connection)
        {
            try
            {
                // 1. Personelin idari amirinin kodunu bul
                string managerCode = null;
                string managerEmail = null;

                string managerQuery = "SELECT per_IdariAmirKodu FROM PERSONELLER WHERE per_kod = @personelKodu";
                using (SqlCommand managerCommand = new SqlCommand(managerQuery, connection))
                {
                    managerCommand.Parameters.AddWithValue("@personelKodu", personelKodu);
                    var result = await managerCommand.ExecuteScalarAsync();
                    managerCode = result?.ToString();
                }

                if (string.IsNullOrEmpty(managerCode))
                {
                    System.Diagnostics.Debug.WriteLine($"Personel {personelKodu} için idari amir bulunamadı.");
                    return;
                }

                // 2. İdari amirin e-posta adresini bul
                string emailQuery = "SELECT Per_PersMailAddress FROM PERSONELLER WHERE per_kod = @managerCode";
                using (SqlCommand emailCommand = new SqlCommand(emailQuery, connection))
                {
                    emailCommand.Parameters.AddWithValue("@managerCode", managerCode);
                    var result = await emailCommand.ExecuteScalarAsync();
                    managerEmail = result?.ToString();
                }

                if (string.IsNullOrEmpty(managerEmail))
                {
                    System.Diagnostics.Debug.WriteLine($"İdari amir {managerCode} için e-posta adresi bulunamadı.");
                    return;
                }

                // 3. İzin talebi detaylarını çek
                var izinTalebi = await GetIzinTalebiDetaylariAsync(izinGuid, connection);
                if (izinTalebi == null)
                {
                    System.Diagnostics.Debug.WriteLine($"İzin talebi detayları bulunamadı: {izinGuid}");
                    return;
                }

                // 4. E-posta gönderme
                await SendManagerNotificationEmailAsync(managerEmail, izinTalebi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"İdari amire bildirim gönderilirken hata oluştu: {ex.Message}");
                // Burada hatayı sadece logluyoruz, ana işlemi bozmamak için tekrar fırlatmıyoruz
            }
        }

        // İdari amire özel e-posta gönderme metodu
        private async Task SendManagerNotificationEmailAsync(string managerEmail, IzinTalepModel izinTalebi)
        {
            try
            {
                // SMTP ayarlarını konfigürasyondan al
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderDisplayName = _configuration["EmailSettings:SenderDisplayName"];

                // SSL güvenlik protokolünü ayarla
                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.SecurityProtocolType.Tls12 |
                    System.Net.SecurityProtocolType.Tls11 |
                    System.Net.SecurityProtocolType.Tls;

                // Sertifika doğrulamasını geçici olarak atla
                System.Net.ServicePointManager.ServerCertificateValidationCallback =
                    delegate { return true; };

                // E-posta başlığı
                string subject = $"Yeni İzin Talebi: {izinTalebi.PersonelAdSoyad}";

                // E-posta içeriği
                string body = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ width: 100%; max-width: 650px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 15px; border-bottom: 3px solid #007bff; }}
        .header h1 {{ color: #007bff; margin: 0; }}
        .content {{ padding: 20px 0; }}
        .details {{ background-color: #f8f9fa; padding: 15px; margin: 15px 0; border-left: 3px solid #007bff; }}
        .footer {{ font-size: 12px; color: #6c757d; margin-top: 30px; border-top: 1px solid #e9ecef; padding-top: 10px; }}
        .button {{ display: inline-block; background-color: #007bff; color: white; text-decoration: none; padding: 10px 15px; 
                   border-radius: 4px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Yeni İzin Talebi Bildirimi</h1>
        </div>
        <div class=""content"">
            <p>Sayın Yönetici,</p>
            
            <p><strong>{izinTalebi.PersonelAdSoyad}</strong> adlı personelden yeni bir izin talebi oluşturulmuştur. 
               Lütfen bu talebi inceleyip onaylama veya reddetme işlemini gerçekleştiriniz.</p>
            
            <div class=""details"">
                <h3>İzin Talebi Detayları:</h3>
                <p><strong>Personel:</strong> {izinTalebi.PersonelAdSoyad}</p>
                <p><strong>Talep Tarihi:</strong> {izinTalebi.TalepTarihi:dd.MM.yyyy}</p>
                <p><strong>İzin Başlangıç Tarihi:</strong> {izinTalebi.BaslangicTarihi:dd.MM.yyyy}</p>
                <p><strong>İzin Günü Sayısı:</strong> {izinTalebi.GunSayisi}</p>
                <p><strong>İzin Amacı:</strong> {izinTalebi.Amac ?? "Belirtilmemiş"}</p>
            </div>
            
            <p>Bu izin talebini incelemek ve işlem yapmak için lütfen İnsan Kaynakları portalına giriş yapınız.</p>
            
            <p>Bilgilerinize sunarız.</p>
        </div>
        <div class=""footer"">
            <p>Bu e-posta otomatik olarak oluşturulmuştur. Lütfen yanıtlamayınız.</p>
        </div>
    </div>
</body>
</html>";

                // SMTP istemcisini oluştur
                using (var client = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000 // 20 saniye timeout
                })
                {
                    // E-posta mesajını oluştur
                    using (var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderDisplayName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        // Alıcı e-posta adresini ekle
                        mailMessage.To.Add(managerEmail);

                        // E-postayı gönder
                        await client.SendMailAsync(mailMessage);

                        // Başarılı gönderim log'u
                        System.Diagnostics.Debug.WriteLine($"İdari amire izin talebi bildirimi gönderildi: {managerEmail}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Genel hata yakalama
                System.Diagnostics.Debug.WriteLine($"İdari amire e-posta gönderme hatası: {ex.Message}");

                // İç içe hata varsa onu da logla
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"İç Hata: {ex.InnerException.Message}");
                }
            }
            finally
            {
                // Güvenlik için sertifika doğrulamasını geri yükle
                System.Net.ServicePointManager.ServerCertificateValidationCallback = null;
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> IzinTalepPdfIndir(Guid izinGuid)
        {
            try
            {
                // İzin ve personel bilgilerini veritabanından al
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Izinlerim");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                // Personel bilgilerini al
                string personelKodu = "";
                string personelAdi = "";
                string personelSoyadi = "";
                string personelGorev = "";
                string personelBirim = "";
                string personelTcNo = "";
                DateTime izinBaslangic = DateTime.Now;
                DateTime iseBaslamaTarih;
                string izinTipi = "0";
                string izinAmaci = "";

                // JavaScript'te hesaplanan işe başlama tarihini Session'dan almaya çalış
                string iseBaslamaStr = HttpContext.Session.GetString("LastIseBaslamaTarihi");
                if (string.IsNullOrEmpty(iseBaslamaStr))
                {
                    // Session değeri yoksa varsayılan bir değer oluştur (bu durumda çok nadiren karşılaşılmalı)
                    iseBaslamaTarih = DateTime.Now.AddDays(1);
                }
                else
                {
                    // Session'dan alınan tarihi kullan
                    iseBaslamaTarih = DateTime.Parse(iseBaslamaStr);
                }

                string connectionString = _dbSelectorService.GetConnectionString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Önce izin talebini al
                    if (izinGuid != Guid.Empty)
                    {
                        string izinQuery = @"
                SELECT 
                    pit_pers_kod, 
                    pit_baslangictarih, 
                    pit_izin_tipi, 
                    pit_amac 
                FROM PERSONEL_IZIN_TALEPLERI 
                WHERE pit_guid = @izinGuid";

                        using (SqlCommand command = new SqlCommand(izinQuery, connection))
                        {
                            command.Parameters.AddWithValue("@izinGuid", izinGuid);
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    personelKodu = reader["pit_pers_kod"].ToString();
                                    izinBaslangic = Convert.ToDateTime(reader["pit_baslangictarih"]);
                                    izinTipi = reader["pit_izin_tipi"].ToString();
                                    izinAmaci = reader["pit_amac"]?.ToString() ?? "";
                                }
                            }
                        }
                    }

                    // Personel bilgilerini al
                    string personelQuery = @"
            SELECT 
                p.per_adi, 
                p.per_soyadi, 
                p.per_kim_gorev, 
                d.pdp_adi, 
                p.Per_TcKimlikNo
            FROM PERSONELLER p
            LEFT JOIN DEPARTMANLAR d ON p.per_dept_kod = d.pdp_kodu
            WHERE p.per_kod = @personelKodu";

                    using (SqlCommand command = new SqlCommand(personelQuery, connection))
                    {
                        command.Parameters.AddWithValue("@personelKodu", personelKodu);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                personelAdi = reader["per_adi"].ToString();
                                personelSoyadi = reader["per_soyadi"].ToString();
                                personelGorev = reader["per_kim_gorev"]?.ToString() ?? "";
                                personelBirim = reader["pdp_adi"]?.ToString() ?? "";
                                personelTcNo = reader["Per_TcKimlikNo"]?.ToString() ?? "";
                            }
                        }
                    }
                }

                // PDF oluştur
                using (MemoryStream ms = new MemoryStream())
                {
                    // Türkçe karakter desteği için BaseFont ayarı
                    // Türkçe karakter desteği için BaseFont ayarı
                    BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, "Cp1254", BaseFont.NOT_EMBEDDED);
                    Font normalFont = new Font(baseFont, 10, Font.NORMAL);
                    Font boldFont = new Font(baseFont, 10, Font.BOLD);
                    Font titleFont = new Font(baseFont, 18, Font.BOLD);
                    Font smallFont = new Font(baseFont, 8, Font.NORMAL);

                    Document document = new Document(PageSize.A4, 50, 50, 50, 50);
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    document.Open();

                    // Logo ekle
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logo.png");
                    if (System.IO.File.Exists(imagePath))
                    {
                        iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(imagePath);
                        logo.ScaleToFit(150f, 100f);
                        logo.Alignment = iTextSharp.text.Image.ALIGN_LEFT;
                        document.Add(logo);
                    }

                    // Başlık
                    PdfPTable headerTable = new PdfPTable(1);
                    headerTable.WidthPercentage = 100;
                    headerTable.DefaultCell.Border = Rectangle.NO_BORDER;

                    PdfPCell titleCell = new PdfPCell(new Phrase("PERSONEL İZİN\nİSTEK FORMU", titleFont));
                    titleCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    titleCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    titleCell.Border = Rectangle.NO_BORDER;
                    titleCell.PaddingBottom = 10;
                    headerTable.AddCell(titleCell);
                    document.Add(headerTable);

                    // Şirket bilgileri
                    PdfPTable companyInfoTable = new PdfPTable(1);
                    companyInfoTable.WidthPercentage = 100;
                    companyInfoTable.DefaultCell.Border = Rectangle.NO_BORDER;
                    companyInfoTable.DefaultCell.HorizontalAlignment = Element.ALIGN_RIGHT;

                    PdfPCell infoCell = new PdfPCell();
                    infoCell.Border = Rectangle.NO_BORDER;
                    infoCell.HorizontalAlignment = Element.ALIGN_RIGHT;

                    Paragraph infoPara = new Paragraph();
                    infoPara.Add(new Chunk("Dioki Petrokimya", boldFont));
                    infoPara.Add(new Chunk(" Serbest Bölgesi, Adana-Yumurtalık, 01920 Adana\n", smallFont));
                    infoPara.Add(new Chunk("T : +(0322) 634 20 15\n", smallFont));

                    infoCell.AddElement(infoPara);
                    companyInfoTable.AddCell(infoCell);
                    document.Add(companyInfoTable);

                    document.Add(new Paragraph(" ")); // Boşluk

                    // Personel Bilgileri Tablosu
                    PdfPTable personelTable = new PdfPTable(5);
                    personelTable.WidthPercentage = 100;
                    personelTable.SetWidths(new float[] { 20f, 20f, 20f, 20f, 20f });

                    // Başlık satırı
                    PdfPCell personelHeaderCell = new PdfPCell(new Phrase("PERSONELİN", boldFont));
                    personelHeaderCell.BackgroundColor = new BaseColor(230, 230, 230);
                    personelHeaderCell.Rowspan = 2;
                    personelHeaderCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    personelHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(personelHeaderCell);

                    // Başlık hücreleri
                    AddHeaderCell(personelTable, "ADI SOYADI", boldFont);
                    AddHeaderCell(personelTable, "GÖREVİ", boldFont);
                    AddHeaderCell(personelTable, "BİRİMİ", boldFont);
                    AddHeaderCell(personelTable, "T.C.SİCİL NO", boldFont);

                    // İçerik satırı
                    PdfPCell adSoyadCell = new PdfPCell(new Phrase(personelAdi + " " + personelSoyadi, normalFont));
                    adSoyadCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    adSoyadCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(adSoyadCell);

                    PdfPCell gorevCell = new PdfPCell(new Phrase(personelGorev, normalFont));
                    gorevCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    gorevCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(gorevCell);

                    PdfPCell birimCell = new PdfPCell(new Phrase(personelBirim, normalFont));
                    birimCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    birimCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(birimCell);

                    PdfPCell tcNoCell = new PdfPCell(new Phrase(personelTcNo, normalFont));
                    tcNoCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    tcNoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    personelTable.AddCell(tcNoCell);

                    document.Add(personelTable);
                    document.Add(new Paragraph(" ")); // Boşluk

                    // İzin Türü Tablosu
                    PdfPTable izinTuruTable = new PdfPTable(8);
                    izinTuruTable.WidthPercentage = 100;
                    izinTuruTable.SetWidths(new float[] { 20f, 10f, 10f, 10f, 10f, 10f, 10f, 10f });

                    // Başlık satırı
                    PdfPCell izinTuruHeaderCell = new PdfPCell(new Phrase("İSTENEN İZNİN NİTELİĞİ", boldFont));
                    izinTuruHeaderCell.BackgroundColor = new BaseColor(230, 230, 230);
                    izinTuruHeaderCell.Rowspan = 2;
                    izinTuruHeaderCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    izinTuruHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    izinTuruTable.AddCell(izinTuruHeaderCell);

                    // Başlık hücreleri
                    AddHeaderCell(izinTuruTable, "YILLIK", boldFont);
                    AddHeaderCell(izinTuruTable, "DOĞUM", boldFont);
                    AddHeaderCell(izinTuruTable, "ÖLÜM", boldFont);
                    AddHeaderCell(izinTuruTable, "MAZERET", boldFont);
                    AddHeaderCell(izinTuruTable, "ÜCRETLİ", boldFont);
                    AddHeaderCell(izinTuruTable, "ÜCRETSİZ", boldFont);
                    AddHeaderCell(izinTuruTable, "DİĞER", boldFont);

                    // İçerik satırı - checkbox'lar için izinTipi değerini kontrol et
                    bool isYillikIzin = izinTipi == "0";
                    bool isDogumIzin = izinTipi == "11" || izinTipi == "12" || izinTipi == "13";
                    bool isOlumIzin = izinTipi == "14";
                    bool isMazeretIzin = izinTipi == "4";
                    bool isUcretliIzin = izinTipi == "1";
                    bool isUcretsizIzin = izinTipi == "8";
                    bool isDigerIzin = izinTipi == "2" || izinTipi == "3" || izinTipi == "5" || izinTipi == "6" ||
                                      izinTipi == "7" || izinTipi == "9" || izinTipi == "10" || izinTipi == "15";

                    // Checkbox hücrelerini ekle
                    AddCheckboxCell(izinTuruTable, isYillikIzin);
                    AddCheckboxCell(izinTuruTable, isDogumIzin);
                    AddCheckboxCell(izinTuruTable, isOlumIzin);
                    AddCheckboxCell(izinTuruTable, isMazeretIzin);
                    AddCheckboxCell(izinTuruTable, isUcretliIzin);
                    AddCheckboxCell(izinTuruTable, isUcretsizIzin);
                    AddCheckboxCell(izinTuruTable, isDigerIzin);

                    document.Add(izinTuruTable);
                    document.Add(new Paragraph(" ")); // Boşluk

                    // İzin Tarihleri Tablosu
                    PdfPTable tarihTable = new PdfPTable(3);
                    tarihTable.WidthPercentage = 100;
                    tarihTable.SetWidths(new float[] { 20f, 40f, 40f });

                    // Başlık satırı
                    PdfPCell tarihHeaderCell = new PdfPCell(new Phrase("KULLANILACAK İZİNİN", boldFont));
                    tarihHeaderCell.BackgroundColor = new BaseColor(230, 230, 230);
                    tarihHeaderCell.Rowspan = 2;
                    tarihHeaderCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    tarihHeaderCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    tarihTable.AddCell(tarihHeaderCell);

                    AddHeaderCell(tarihTable, "İZİN BAŞLANGIÇ TARİHİ", boldFont);
                    AddHeaderCell(tarihTable, "İZİN DÖNÜŞÜ GÖREVE BAŞLAMA TARİHİ", boldFont);

                    // İçerik satırı - tarihler
                    PdfPCell baslangicCell = new PdfPCell(new Phrase(izinBaslangic.ToString("dd/MM/yyyy"), normalFont));
                    baslangicCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    baslangicCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    tarihTable.AddCell(baslangicCell);

                    // JavaScript'ten alınan işe başlama tarihini kullan
                    PdfPCell iseBaslamaCell = new PdfPCell(new Phrase(iseBaslamaTarih.ToString("dd/MM/yyyy"), normalFont));
                    iseBaslamaCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    iseBaslamaCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    tarihTable.AddCell(iseBaslamaCell);

                    document.Add(tarihTable);
                    document.Add(new Paragraph(" ")); // Boşluk

                    // Not bölümü
                    PdfPTable noteTable = new PdfPTable(1);
                    noteTable.WidthPercentage = 100;

                    PdfPCell noteCell = new PdfPCell(new Phrase("NOT : 1) İdari, Teknik, Destek Personeli ve işçilerin yıllık iznini 4857 Sayılı İş Kanunu ile Şirketimiz \"İzin Kullanma Esasları\"na göre, ait olduğu yıl içinde bir seferde kullanır.", normalFont));
                    noteCell.BackgroundColor = new BaseColor(240, 240, 240);
                    noteCell.Border = Rectangle.BOX;
                    noteCell.PaddingTop = 5;
                    noteCell.PaddingBottom = 5;
                    noteCell.PaddingLeft = 5;
                    noteCell.PaddingRight = 5;
                    noteTable.AddCell(noteCell);

                    document.Add(noteTable);
                    document.Add(new Paragraph(" ")); // Boşluk

                    // İzin adresi
                    document.Add(new Paragraph("İzin Açıklaması:", boldFont));
                    PdfPTable adresTable = new PdfPTable(1);
                    adresTable.WidthPercentage = 100;

                    PdfPCell adresCell = new PdfPCell(new Phrase(izinAmaci, normalFont));
                    adresCell.MinimumHeight = 70;
                    adresCell.VerticalAlignment = Element.ALIGN_TOP;
                    adresTable.AddCell(adresCell);

                    document.Add(adresTable);
                    document.Add(new Paragraph(" ")); // Boşluk

                    // İmza alanları
                    PdfPTable signatureTable = new PdfPTable(2);
                    signatureTable.WidthPercentage = 100;

                    // Personel imza alanı
                    PdfPCell personelSignatureCell = new PdfPCell();
                    personelSignatureCell.BorderWidthTop = 1f;
                    personelSignatureCell.BorderWidthBottom = 0f;
                    personelSignatureCell.BorderWidthLeft = 0f;
                    personelSignatureCell.BorderWidthRight = 0f;
                    personelSignatureCell.PaddingTop = 10;
                    personelSignatureCell.HorizontalAlignment = Element.ALIGN_CENTER;

                    Paragraph personelSignatureText = new Paragraph();
                    personelSignatureText.Alignment = Element.ALIGN_CENTER;
                    personelSignatureText.Add(new Chunk("PERSONELİN\n", boldFont));
                    personelSignatureText.Add(new Chunk("ADI SOYADI: " + personelAdi + " " + personelSoyadi + "\n\n", normalFont));
                    personelSignatureText.Add(new Chunk("İMZASI:\n\n", normalFont));
                    personelSignatureText.Add(new Chunk("......./......./20......", normalFont));

                    personelSignatureCell.AddElement(personelSignatureText);
                    signatureTable.AddCell(personelSignatureCell);

                    // Birim amiri imza alanı
                    PdfPCell amirSignatureCell = new PdfPCell();
                    amirSignatureCell.BorderWidthTop = 1f;
                    amirSignatureCell.BorderWidthBottom = 0f;
                    amirSignatureCell.BorderWidthLeft = 0f;
                    amirSignatureCell.BorderWidthRight = 0f;
                    amirSignatureCell.PaddingTop = 10;
                    amirSignatureCell.HorizontalAlignment = Element.ALIGN_CENTER;

                    Paragraph amirSignatureText = new Paragraph();
                    amirSignatureText.Alignment = Element.ALIGN_CENTER;
                    amirSignatureText.Add(new Chunk("BİRİM AMİRİNİN\n", boldFont));
                    amirSignatureText.Add(new Chunk("ADI SOYADI:\n\n", normalFont));
                    amirSignatureText.Add(new Chunk("İMZASI:\n\n", normalFont));
                    amirSignatureText.Add(new Chunk("......./......./20......", normalFont));

                    amirSignatureCell.AddElement(amirSignatureText);
                    signatureTable.AddCell(amirSignatureCell);

                    document.Add(signatureTable);

                    // Form referans numarası
                    Paragraph formRef = new Paragraph("F/M/TARD/EV001", new Font(baseFont, 6));
                    formRef.Alignment = Element.ALIGN_RIGHT;
                    document.Add(formRef);

                    document.Close();

                    // PDF'i döndür
                    byte[] pdfBytes = ms.ToArray();
                    string personelAdSoyad = personelAdi + "_" + personelSoyadi;
                    return File(pdfBytes, "application/pdf", $"IzinTalep_{personelAdSoyad}.pdf");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Izinlerim", new { error = "PDF oluşturulurken bir hata oluştu: " + ex.Message });
            }
        }

        // Yardımcı metotlar
        private void AddHeaderCell(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.BackgroundColor = new BaseColor(230, 230, 230);
            table.AddCell(cell);
        }

        private void AddCheckboxCell(PdfPTable table, bool isChecked)
        {
            PdfPCell cell = new PdfPCell();
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.MinimumHeight = 20f;

            Paragraph p = new Paragraph();
            if (isChecked)
            {
                p.Add(new Chunk("☑", new Font(Font.FontFamily.ZAPFDINGBATS, 12)));
            }
            else
            {
                p.Add(new Chunk("☐", new Font(Font.FontFamily.ZAPFDINGBATS, 12)));
            }

            cell.AddElement(p);
            table.AddCell(cell);
        }

        public async Task<IActionResult> IzinliPersonel()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");
                System.Diagnostics.Debug.WriteLine($"Username: {username}, Version: {version}");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                int userNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                    userCommand.Parameters.AddWithValue("@username", username);
                    var result = await userCommand.ExecuteScalarAsync();
                    userNo = Convert.ToInt32(result);
                }

                List<IzinTalepModel> onayliIzinler = new List<IzinTalepModel>();
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    await erpConnection.OpenAsync();

                    string personelKodu;
                    using (SqlCommand personelCommand = new SqlCommand("SELECT per_kod FROM PERSONELLER WHERE per_UserNo = @userNo", erpConnection))
                    {
                        personelCommand.Parameters.AddWithValue("@userNo", userNo);
                        var result = await personelCommand.ExecuteScalarAsync();
                        personelKodu = result?.ToString();
                    }

                    if (string.IsNullOrEmpty(personelKodu))
                    {
                        return View(new List<IzinTalepModel>());
                    }

                    using SqlCommand command = new SqlCommand(@"
WITH PersonelHiyerarsi AS (
    SELECT 
        p1.per_kod as AltPersonelKod, 
        p2.per_kod as UstPersonelKod,
        p1.per_IdariAmirKodu as IdariAmirKodu,
        1 as Seviye
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod)
    WHERE p2.per_kod = @personelKodu
        AND p1.per_IdariAmirKodu IS NOT NULL

    UNION ALL

    SELECT 
        p1.per_kod,
        p2.per_kod,
        p1.per_IdariAmirKodu,
        ph.Seviye + 1
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod)
    INNER JOIN PersonelHiyerarsi ph ON p2.per_kod = ph.AltPersonelKod
    WHERE ph.Seviye < 5
        AND p1.per_IdariAmirKodu IS NOT NULL
)
SELECT DISTINCT 
    t.pit_pers_kod,
    p.per_adi + ' ' + p.per_soyadi as PersonelAdSoyad,
    p.per_IdariAmirKodu,
    (SELECT per_adi + ' ' + per_soyadi 
     FROM PERSONELLER 
     WHERE per_kod = p.per_IdariAmirKodu) as IdariAmirAdi,
    t.pit_talep_tarihi,
    t.pit_izin_tipi,
    t.pit_gun_sayisi,
    t.pit_baslangictarih,
    t.pit_BaslamaSaati,
    t.pit_saat,
    t.pit_amac,
    t.pit_izin_durum,
    t.pit_create_date,
    t.pit_onaylayan_kullanici
FROM PERSONEL_IZIN_TALEPLERI t
INNER JOIN PERSONELLER p ON t.pit_pers_kod = p.per_kod
WHERE t.pit_izin_durum = 1 AND t.pit_pers_kod IN (
    SELECT AltPersonelKod 
    FROM PersonelHiyerarsi 
)
ORDER BY t.pit_baslangictarih DESC", erpConnection);

                    command.Parameters.AddWithValue("@personelKodu", personelKodu);

                    using SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        onayliIzinler.Add(new IzinTalepModel
                        {
                            PersonelKodu = reader.GetString(reader.GetOrdinal("pit_pers_kod")),
                            PersonelAdSoyad = reader.GetString(reader.GetOrdinal("PersonelAdSoyad")),
                            IdariAmirKodu = !reader.IsDBNull(reader.GetOrdinal("per_IdariAmirKodu")) ? reader.GetString(reader.GetOrdinal("per_IdariAmirKodu")) : null,
                            IdariAmirAdi = !reader.IsDBNull(reader.GetOrdinal("IdariAmirAdi")) ? reader.GetString(reader.GetOrdinal("IdariAmirAdi")) : null,
                            TalepTarihi = reader.GetDateTime(reader.GetOrdinal("pit_talep_tarihi")),
                            IzinTipi = reader.GetByte(reader.GetOrdinal("pit_izin_tipi")),
                            GunSayisi = reader.GetByte(reader.GetOrdinal("pit_gun_sayisi")),
                            BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("pit_baslangictarih")),
                            BaslamaSaati = !reader.IsDBNull(reader.GetOrdinal("pit_BaslamaSaati"))
                                ? reader.GetDateTime(reader.GetOrdinal("pit_BaslamaSaati")).TimeOfDay.TotalHours
                                : 0.0,
                            BitisSaati = !reader.IsDBNull(reader.GetOrdinal("pit_saat"))
                                ? (float)reader.GetDouble(reader.GetOrdinal("pit_saat"))
                                : 0.0f,
                            Amac = !reader.IsDBNull(reader.GetOrdinal("pit_amac")) ? reader.GetString(reader.GetOrdinal("pit_amac")) : string.Empty,
                            IzinDurumu = reader.GetByte(reader.GetOrdinal("pit_izin_durum")),
                            OlusturmaTarihi = reader.GetDateTime(reader.GetOrdinal("pit_create_date")),
                            OnaylayanKullanici = !reader.IsDBNull(reader.GetOrdinal("pit_onaylayan_kullanici"))
                                ? reader.GetInt32(reader.GetOrdinal("pit_onaylayan_kullanici")).ToString()
                                : null
                        });
                    }

                    return View(onayliIzinler);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HATA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View(new List<IzinTalepModel>());
            }
        }

        public IActionResult hakedisraporu()
        {
            return View();
        }

        public async Task<IActionResult> IzinTalepleri()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");
                System.Diagnostics.Debug.WriteLine($"Username: {username}, Version: {version}");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                int userNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                    userCommand.Parameters.AddWithValue("@username", username);
                    var result = await userCommand.ExecuteScalarAsync();
                    userNo = Convert.ToInt32(result);
                    System.Diagnostics.Debug.WriteLine($"UserNo: {userNo}");
                }

                List<IzinTalepModel> izinTalepleri = new List<IzinTalepModel>();
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    await erpConnection.OpenAsync();

                    // Personel kodunu al
                    string personelKodu;
                    using (SqlCommand personelCommand = new SqlCommand("SELECT per_kod FROM PERSONELLER WHERE per_UserNo = @userNo", erpConnection))
                    {
                        personelCommand.Parameters.AddWithValue("@userNo", userNo);
                        var result = await personelCommand.ExecuteScalarAsync();
                        personelKodu = result?.ToString();
                        System.Diagnostics.Debug.WriteLine($"PersonelKodu: {personelKodu}");
                    }

                    if (string.IsNullOrEmpty(personelKodu))
                    {
                        System.Diagnostics.Debug.WriteLine("PersonelKodu bulunamadı!");
                        return View(new List<IzinTalepModel>());
                    }

                    // Ana sorgu - GUID eklendi
                    using SqlCommand command = new SqlCommand(@"
WITH PersonelHiyerarsi AS (
    SELECT 
        p1.per_kod as AltPersonelKod, 
        p2.per_kod as UstPersonelKod,
        p1.per_IdariAmirKodu as IdariAmirKodu,
        1 as Seviye
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod)
    WHERE p2.per_kod = @personelKodu
        AND p1.per_IdariAmirKodu IS NOT NULL

    UNION ALL

    SELECT 
        p1.per_kod,
        p2.per_kod,
        p1.per_IdariAmirKodu,
        ph.Seviye + 1
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod)
    INNER JOIN PersonelHiyerarsi ph ON p2.per_kod = ph.AltPersonelKod
    WHERE ph.Seviye < 5
        AND p1.per_IdariAmirKodu IS NOT NULL
)
SELECT DISTINCT 
    t.pit_guid,
    t.pit_pers_kod,
    p.per_adi + ' ' + p.per_soyadi as PersonelAdSoyad,
    p.per_IdariAmirKodu,
    (SELECT per_adi + ' ' + per_soyadi 
     FROM PERSONELLER 
     WHERE per_kod = p.per_IdariAmirKodu) as IdariAmirAdi,
    t.pit_talep_tarihi,
    t.pit_izin_tipi,
    t.pit_gun_sayisi,
    t.pit_baslangictarih,
    t.pit_BaslamaSaati,
    t.pit_saat,
    t.pit_amac,
    t.pit_izin_durum,
    t.pit_create_date
FROM PERSONEL_IZIN_TALEPLERI t
INNER JOIN PERSONELLER p ON t.pit_pers_kod = p.per_kod
WHERE t.pit_izin_durum=0 AND t.pit_pers_kod IN (
    SELECT AltPersonelKod 
    FROM PersonelHiyerarsi 
)", erpConnection);

                    command.Parameters.AddWithValue("@personelKodu", personelKodu);
                    System.Diagnostics.Debug.WriteLine("Ana sorgu çalıştırılıyor...");

                    using SqlDataReader reader = await command.ExecuteReaderAsync();
                    int kayitSayisi = 0;
                    while (await reader.ReadAsync())
                    {
                        kayitSayisi++;
                        izinTalepleri.Add(new IzinTalepModel
                        {
                            Guid = reader.GetGuid(reader.GetOrdinal("pit_guid")),
                            PersonelKodu = reader.GetString(reader.GetOrdinal("pit_pers_kod")),
                            PersonelAdSoyad = reader.GetString(reader.GetOrdinal("PersonelAdSoyad")),
                            IdariAmirKodu = !reader.IsDBNull(reader.GetOrdinal("per_IdariAmirKodu")) ? reader.GetString(reader.GetOrdinal("per_IdariAmirKodu")) : null,
                            IdariAmirAdi = !reader.IsDBNull(reader.GetOrdinal("IdariAmirAdi")) ? reader.GetString(reader.GetOrdinal("IdariAmirAdi")) : null,
                            TalepTarihi = reader.GetDateTime(reader.GetOrdinal("pit_talep_tarihi")),
                            IzinTipi = reader.GetByte(reader.GetOrdinal("pit_izin_tipi")),
                            GunSayisi = reader.GetByte(reader.GetOrdinal("pit_gun_sayisi")),
                            BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("pit_baslangictarih")),
                            BaslamaSaati = !reader.IsDBNull(reader.GetOrdinal("pit_BaslamaSaati"))
                                ? reader.GetDateTime(reader.GetOrdinal("pit_BaslamaSaati")).TimeOfDay.TotalHours
                                : 0.0,
                            BitisSaati = !reader.IsDBNull(reader.GetOrdinal("pit_saat"))
                                ? (float)reader.GetDouble(reader.GetOrdinal("pit_saat"))
                                : 0.0f,
                            Amac = !reader.IsDBNull(reader.GetOrdinal("pit_amac")) ? reader.GetString(reader.GetOrdinal("pit_amac")) : string.Empty,
                            IzinDurumu = reader.GetByte(reader.GetOrdinal("pit_izin_durum")),
                            OlusturmaTarihi = reader.GetDateTime(reader.GetOrdinal("pit_create_date"))
                        });
                        System.Diagnostics.Debug.WriteLine($"Kayıt eklendi: {izinTalepleri[izinTalepleri.Count - 1].PersonelAdSoyad}");
                    }

                    return View(izinTalepleri);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HATA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View(new List<IzinTalepModel>());
            }
        }
        [HttpPost]
        [AllowAnonymous]
        private async Task<string> GetPersonelEmailAsync(string personelKodu, SqlConnection connection)
        {
            string emailQuery = "SELECT Per_PersMailAddress FROM PERSONELLER WHERE per_kod = @personelKodu";

            using (SqlCommand emailCommand = new SqlCommand(emailQuery, connection))
            {
                emailCommand.Parameters.AddWithValue("@personelKodu", personelKodu);
                var emailResult = await emailCommand.ExecuteScalarAsync();

                return emailResult?.ToString();
            }
        }
        [HttpPost]
        [AllowAnonymous]
        // İzin talebi detaylarını alma metodu
        private async Task<IzinTalepModel> GetIzinTalebiDetaylariAsync(Guid guid, SqlConnection connection)
        {
            string query = @"
SELECT 
    t.pit_pers_kod,
    p.per_adi + ' ' + p.per_soyadi as PersonelAdSoyad,
    t.pit_talep_tarihi,
    t.pit_izin_tipi,
    t.pit_gun_sayisi,
    t.pit_baslangictarih,
    t.pit_amac,
    t.pit_aciklama1 as ReddetmeNedeni
FROM PERSONEL_IZIN_TALEPLERI t
INNER JOIN PERSONELLER p ON t.pit_pers_kod = p.per_kod
WHERE t.pit_guid = @guid";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@guid", guid);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new IzinTalepModel
                        {
                            PersonelKodu = reader.GetString(reader.GetOrdinal("pit_pers_kod")),
                            PersonelAdSoyad = reader.GetString(reader.GetOrdinal("PersonelAdSoyad")),
                            TalepTarihi = reader.GetDateTime(reader.GetOrdinal("pit_talep_tarihi")),
                            IzinTipi = reader.GetByte(reader.GetOrdinal("pit_izin_tipi")),
                            GunSayisi = reader.GetByte(reader.GetOrdinal("pit_gun_sayisi")),
                            BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("pit_baslangictarih")),
                            Amac = !reader.IsDBNull(reader.GetOrdinal("pit_amac"))
                                ? reader.GetString(reader.GetOrdinal("pit_amac"))
                                : null,
                            ReddetmeNedeni = !reader.IsDBNull(reader.GetOrdinal("ReddetmeNedeni"))
                                ? reader.GetString(reader.GetOrdinal("ReddetmeNedeni"))
                                : null
                        };
                    }

                    return null;
                }
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> OnaylaIzinTalebi(Guid guid, string personelKodu, DateTime talepTarihi)
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
                }

                // MikroDB'den kullanıcı numarasını al
                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                short onaylayanKullaniciNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                    userCommand.Parameters.AddWithValue("@username", username);
                    var result = await userCommand.ExecuteScalarAsync();

                    if (result == null)
                    {
                        return Json(new { success = false, message = "Kullanıcı bilgileri bulunamadı." });
                    }

                    onaylayanKullaniciNo = Convert.ToInt16(result);
                }

                // Ana veritabanı bağlantı dizesini al
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();

                    // Güncelleme sorgusu
                    string updateQuery = @"
            UPDATE PERSONEL_IZIN_TALEPLERI 
            SET pit_izin_durum = 1, 
                pit_onaylayan_kullanici = @onaylayanKullaniciNo,
                pit_lastup_user = @onaylayanKullaniciNo,
                pit_lastup_date = GETDATE()
            WHERE pit_Guid = @guid 
            AND pit_izin_durum = 0";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@guid", guid);
                        command.Parameters.AddWithValue("@personelKodu", personelKodu);
                        command.Parameters.AddWithValue("@talepTarihi", talepTarihi);
                        command.Parameters.AddWithValue("@onaylayanKullaniciNo", onaylayanKullaniciNo);
             

                        int affectedRows = await command.ExecuteNonQueryAsync();

                        if (affectedRows > 0)
                        {
                            // Personel e-posta adresini al
                            string personelEmail = await GetPersonelEmailAsync(personelKodu, connection);

                            if (!string.IsNullOrEmpty(personelEmail))
                            {
                                // İzin talebi detaylarını çek
                                var izinTalebi = await GetIzinTalebiDetaylariAsync(guid, connection);

                                // E-posta gönder
                                await _emailService.SendLeaveRequestNotificationAsync(
                                    personelEmail,
                                    true,  // Onaylandı
                                    izinTalebi
                                );
                            }

                            return Json(new { success = true, message = "İzin talebi başarıyla onaylandı." });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"İzin talebi onaylanamadı - Personel Kodu: {personelKodu}, Talep Tarihi: {talepTarihi}");
                            return Json(new { success = false, message = "İzin talebi onaylanamadı. Lütfen tekrar deneyin." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hatayı log'la
                System.Diagnostics.Debug.WriteLine($"HATA (OnaylaIzinTalebi): {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Hata yanıtını döndür
                return Json(new { success = false, message = "Bir hata oluştu. Lütfen sistem yöneticinize başvurun." });
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ReddetIzinTalebi(Guid guid, string personelKodu, DateTime talepTarihi, string reddetmeNedeni)
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });
                }

                // MikroDB'den kullanıcı numarasını al
                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                short onaylayanKullaniciNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                    userCommand.Parameters.AddWithValue("@username", username);
                    var result = await userCommand.ExecuteScalarAsync();

                    if (result == null)
                    {
                        return Json(new { success = false, message = "Kullanıcı bilgileri bulunamadı." });
                    }

                    onaylayanKullaniciNo = Convert.ToInt16(result);
                }

                // Ana veritabanı bağlantı dizesini al
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection connection = new SqlConnection(erpConnectionString))
                {
                    await connection.OpenAsync();

                    // Güncelleme sorgusu - izin durumunu 2 (Reddedildi) olarak ayarla ve reddetme nedenini ekle
                    string updateQuery = @"
            UPDATE PERSONEL_IZIN_TALEPLERI 
            SET pit_izin_durum = 2, 
                pit_onaylayan_kullanici = @onaylayanKullaniciNo,
                pit_lastup_user = @onaylayanKullaniciNo,
                pit_lastup_date = GETDATE(),
                pit_aciklama1 = @reddetmeNedeni
            WHERE pit_Guid = @guid 
            AND pit_izin_durum = 0";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@guid", guid);
                        command.Parameters.AddWithValue("@personelKodu", personelKodu);
                        command.Parameters.AddWithValue("@talepTarihi", talepTarihi);
                        command.Parameters.AddWithValue("@onaylayanKullaniciNo", onaylayanKullaniciNo);
                        command.Parameters.AddWithValue("@reddetmeNedeni", !string.IsNullOrEmpty(reddetmeNedeni) ? reddetmeNedeni : "");

                        int affectedRows = await command.ExecuteNonQueryAsync();

                        if (affectedRows > 0)
                        {
                            // Personel e-posta adresini al
                            string personelEmail = await GetPersonelEmailAsync(personelKodu, connection);

                            if (!string.IsNullOrEmpty(personelEmail))
                            {
                                // İzin talebi detaylarını çek
                                var izinTalebi = await GetIzinTalebiDetaylariAsync(guid, connection);
                                izinTalebi.ReddetmeNedeni = reddetmeNedeni;

                                // E-posta gönder
                                await _emailService.SendLeaveRequestNotificationAsync(
                                    personelEmail,
                                    false,  // Reddedildi
                                    izinTalebi
                                );
                            }

                            return Json(new { success = true, message = "İzin talebi başarıyla reddedildi." });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"İzin talebi reddedilemedi - Personel Kodu: {personelKodu}, Talep Tarihi: {talepTarihi}");
                            return Json(new { success = false, message = "İzin talebi reddedilemedi. Lütfen tekrar deneyin." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hatayı log'la
                System.Diagnostics.Debug.WriteLine($"HATA (ReddetIzinTalebi): {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Hata yanıtını döndür
                return Json(new { success = false, message = "Bir hata oluştu. Lütfen sistem yöneticinize başvurun." });
            }
        }

        public async Task<IActionResult> ReddedilenIzinler()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");
                System.Diagnostics.Debug.WriteLine($"Username: {username}, Version: {version}");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                int userNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    using SqlCommand userCommand = new SqlCommand("SELECT User_no FROM KULLANICILAR WHERE User_name = @username", mikroConnection);
                    userCommand.Parameters.AddWithValue("@username", username);
                    var result = await userCommand.ExecuteScalarAsync();
                    userNo = Convert.ToInt32(result);
                }

                List<IzinTalepModel> reddedilenIzinler = new List<IzinTalepModel>();
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    await erpConnection.OpenAsync();

                    string personelKodu;
                    using (SqlCommand personelCommand = new SqlCommand("SELECT per_kod FROM PERSONELLER WHERE per_UserNo = @userNo", erpConnection))
                    {
                        personelCommand.Parameters.AddWithValue("@userNo", userNo);
                        var result = await personelCommand.ExecuteScalarAsync();
                        personelKodu = result?.ToString();
                    }

                    if (string.IsNullOrEmpty(personelKodu))
                    {
                        return View(new List<IzinTalepModel>());
                    }

                    // IzinliPersonel metodundan alınan sorguyu, izin_durum = 2 olacak şekilde ve pit_aciklama1 ekleyerek düzenliyoruz
                    using SqlCommand command = new SqlCommand(@"
WITH PersonelHiyerarsi AS (
    SELECT 
        p1.per_kod as AltPersonelKod, 
        p2.per_kod as UstPersonelKod,
        p1.per_IdariAmirKodu as IdariAmirKodu,
        1 as Seviye
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod)
    WHERE p2.per_kod = @personelKodu
        AND p1.per_IdariAmirKodu IS NOT NULL

    UNION ALL

    SELECT 
        p1.per_kod,
        p2.per_kod,
        p1.per_IdariAmirKodu,
        ph.Seviye + 1
    FROM PERSONELLER p1
    INNER JOIN PERSONELLER p2 ON 
        (p1.per_IdariAmirKodu = p2.per_kod)
    INNER JOIN PersonelHiyerarsi ph ON p2.per_kod = ph.AltPersonelKod
    WHERE ph.Seviye < 5
        AND p1.per_IdariAmirKodu IS NOT NULL
)
SELECT DISTINCT 
    t.pit_pers_kod,
    p.per_adi + ' ' + p.per_soyadi as PersonelAdSoyad,
    p.per_IdariAmirKodu,
    (SELECT per_adi + ' ' + per_soyadi 
     FROM PERSONELLER 
     WHERE per_kod = p.per_IdariAmirKodu) as IdariAmirAdi,
    t.pit_talep_tarihi,
    t.pit_izin_tipi,
    t.pit_gun_sayisi,
    t.pit_baslangictarih,
    t.pit_BaslamaSaati,
    t.pit_saat,
    t.pit_amac,
    t.pit_izin_durum,
    t.pit_create_date,
    t.pit_onaylayan_kullanici,
    t.pit_aciklama1 as ReddetmeNedeni
FROM PERSONEL_IZIN_TALEPLERI t
INNER JOIN PERSONELLER p ON t.pit_pers_kod = p.per_kod
WHERE t.pit_izin_durum = 2 AND t.pit_pers_kod IN (
    SELECT AltPersonelKod 
    FROM PersonelHiyerarsi 
)
ORDER BY t.pit_baslangictarih DESC", erpConnection);

                    command.Parameters.AddWithValue("@personelKodu", personelKodu);

                    using SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        // IzinTipiAdi artık kullanılmıyor, bu kodu kaldırıyoruz

                        reddedilenIzinler.Add(new IzinTalepModel
                        {
                            PersonelKodu = reader.GetString(reader.GetOrdinal("pit_pers_kod")),
                            PersonelAdSoyad = reader.GetString(reader.GetOrdinal("PersonelAdSoyad")),
                            IdariAmirKodu = !reader.IsDBNull(reader.GetOrdinal("per_IdariAmirKodu")) ? reader.GetString(reader.GetOrdinal("per_IdariAmirKodu")) : null,
                            IdariAmirAdi = !reader.IsDBNull(reader.GetOrdinal("IdariAmirAdi")) ? reader.GetString(reader.GetOrdinal("IdariAmirAdi")) : null,
                            TalepTarihi = reader.GetDateTime(reader.GetOrdinal("pit_talep_tarihi")),
                            IzinTipi = reader.GetByte(reader.GetOrdinal("pit_izin_tipi")),
                            // IzinTipiAdi artık kullanılmıyor
                            GunSayisi = reader.GetByte(reader.GetOrdinal("pit_gun_sayisi")),
                            BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("pit_baslangictarih")),
                            
                            
                            Amac = !reader.IsDBNull(reader.GetOrdinal("pit_amac")) ? reader.GetString(reader.GetOrdinal("pit_amac")) : string.Empty,
                            IzinDurumu = reader.GetByte(reader.GetOrdinal("pit_izin_durum")),
                            OlusturmaTarihi = reader.GetDateTime(reader.GetOrdinal("pit_create_date")),
                            OnaylayanKullanici = !reader.IsDBNull(reader.GetOrdinal("pit_onaylayan_kullanici"))
                                ? reader.GetInt32(reader.GetOrdinal("pit_onaylayan_kullanici")).ToString()
                                : null,
                            // Kullanıcı adını ayrıca almıyoruz, sadece ID kullanıyoruz
                            ReddetmeNedeni = !reader.IsDBNull(reader.GetOrdinal("ReddetmeNedeni"))
                                ? reader.GetString(reader.GetOrdinal("ReddetmeNedeni"))
                                : null
                        });
                    }

                    return View(reddedilenIzinler);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HATA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View(new List<IzinTalepModel>());
            }
        }
        public async Task<IActionResult> Izinlerim()
        {
            try
            {
                string username = HttpContext.Session.GetString("Username");
                string version = HttpContext.Session.GetString("SelectedVersion");

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                string mikroDbConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                string userNo;
                using (SqlConnection mikroConnection = new SqlConnection(mikroDbConnectionString))
                {
                    await mikroConnection.OpenAsync();
                    string userQuery = "SELECT User_no FROM KULLANICILAR WHERE User_name = @username";

                    using (SqlCommand command = new SqlCommand(userQuery, mikroConnection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        var result = await command.ExecuteScalarAsync();
                        userNo = result?.ToString();
                    }
                }

                if (string.IsNullOrEmpty(userNo))
                {
                    return View(new List<IzinTalepModel>());
                }

                List<IzinTalepModel> izinTalepleri = new List<IzinTalepModel>();
                string erpConnectionString = _dbSelectorService.GetConnectionString();

                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    await erpConnection.OpenAsync();

                    string personelKodu;
                    using (SqlCommand personelCommand = new SqlCommand(
                        "SELECT per_kod FROM PERSONELLER WHERE per_UserNo = @userNo", erpConnection))
                    {
                        personelCommand.Parameters.AddWithValue("@userNo", userNo);
                        var result = await personelCommand.ExecuteScalarAsync();
                        personelKodu = result?.ToString();
                    }

                    if (string.IsNullOrEmpty(personelKodu))
                    {
                        return View(new List<IzinTalepModel>());
                    }

                    string query = @"
                SELECT 
                    t.pit_pers_kod,
                    p.per_adi + ' ' + p.per_soyadi as PersonelAdSoyad,
                    p.per_IdariAmirKodu,
                    ISNULL((SELECT TOP 1 per_adi + ' ' + per_soyadi 
                     FROM PERSONELLER 
                     WHERE per_kod = p.per_IdariAmirKodu), '') as IdariAmirAdi,
                    t.pit_talep_tarihi,
                    t.pit_izin_tipi,
                    t.pit_gun_sayisi,
                    t.pit_baslangictarih,
                    t.pit_BaslamaSaati,
                    t.pit_saat,
                    t.pit_amac,
                    t.pit_izin_durum,
                    t.pit_create_date,
                    t.pit_onaylayan_kullanici,
                    ISNULL((SELECT TOP 1 per_adi + ' ' + per_soyadi 
                     FROM PERSONELLER 
                     WHERE per_UserNo = t.pit_onaylayan_kullanici), '') as OnaylayanKullanici
                FROM PERSONEL_IZIN_TALEPLERI t
                INNER JOIN PERSONELLER p ON t.pit_pers_kod = p.per_kod
                WHERE t.pit_pers_kod = @personelKodu
                ORDER BY t.pit_talep_tarihi DESC";

                    using (SqlCommand command = new SqlCommand(query, erpConnection))
                    {
                        command.Parameters.AddWithValue("@personelKodu", personelKodu);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                izinTalepleri.Add(new IzinTalepModel
                                {
                                    PersonelKodu = reader.GetString(reader.GetOrdinal("pit_pers_kod")),
                                    PersonelAdSoyad = reader.GetString(reader.GetOrdinal("PersonelAdSoyad")),
                                    IdariAmirKodu = !reader.IsDBNull(reader.GetOrdinal("per_IdariAmirKodu")) ? reader.GetString(reader.GetOrdinal("per_IdariAmirKodu")) : null,
                                    IdariAmirAdi = !reader.IsDBNull(reader.GetOrdinal("IdariAmirAdi")) ? reader.GetString(reader.GetOrdinal("IdariAmirAdi")) : null,
                                    TalepTarihi = reader.GetDateTime(reader.GetOrdinal("pit_talep_tarihi")),
                                    IzinTipi = reader.GetByte(reader.GetOrdinal("pit_izin_tipi")),
                                    GunSayisi = reader.GetByte(reader.GetOrdinal("pit_gun_sayisi")),
                                    BaslangicTarihi = reader.GetDateTime(reader.GetOrdinal("pit_baslangictarih")),
                                    BaslamaSaati = !reader.IsDBNull(reader.GetOrdinal("pit_BaslamaSaati"))
            ? reader.GetDateTime(reader.GetOrdinal("pit_BaslamaSaati")).TimeOfDay.TotalHours
            : 0.0,
                                    BitisSaati = !reader.IsDBNull(reader.GetOrdinal("pit_saat"))
    ? (float)reader.GetDouble(reader.GetOrdinal("pit_saat"))
    : 0.0f,
                                    Amac = !reader.IsDBNull(reader.GetOrdinal("pit_amac")) ? reader.GetString(reader.GetOrdinal("pit_amac")) : string.Empty,
                                    IzinDurumu = reader.GetByte(reader.GetOrdinal("pit_izin_durum")),
                                    OlusturmaTarihi = reader.GetDateTime(reader.GetOrdinal("pit_create_date"))
                                });
                            }
                        }
                    }
                }

                return View(izinTalepleri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HATA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View(new List<IzinTalepModel>());
            }
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetLeaveEntitlementInfo(string personnelCode, int year)
        {
            try
            {
                if (string.IsNullOrEmpty(personnelCode))
                {
                    return Json(new { success = false, message = "Personel kodu boş olamaz" });
                }

                string erpConnectionString = _dbSelectorService.GetConnectionString();
                using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                {
                    await erpConnection.OpenAsync();

                    string query = @"
                DECLARE @Yil INT = @year;
                WITH PersonelBilgileri AS (
                    SELECT 
                        per_kod,
                        per_adi,
                        per_soyadi,
                        per_giris_tar,
                        DATEDIFF(YEAR, per_giris_tar, CONVERT(DATE, CONCAT(@Yil, '0101'))) AS CalismaSuresiYil
                    FROM PERSONELLER WITH (NOLOCK)
                    WHERE per_kod = @personnelCode
                ),
                IzinHaklari AS (
                    SELECT
                        per_kod,
                        per_adi,
                        per_soyadi,
                        per_giris_tar,
                        CalismaSuresiYil,
                        CASE 
                            WHEN CalismaSuresiYil >= 5 THEN 21
                            WHEN CalismaSuresiYil >= 1 THEN 14
                            ELSE 0
                        END AS HakEdilenYillikIzin
                    FROM PersonelBilgileri
                ),
                GecenYilDevredilen AS (
                    SELECT
                        IH.per_kod,
                        IH.per_adi,
                        IH.per_soyadi,
                        IH.CalismaSuresiYil,
                        IH.HakEdilenYillikIzin,
                        ISNULL((SELECT SUM(pro_gecyil_devir_izin) 
                                FROM dbo.PERSONEL_TAHAKKUK_OZETLERI WITH (NOLOCK) 
                                WHERE pro_kodozet = IH.per_kod 
                                AND pro_ozetyili = @Yil), 0) AS GecenYilDevirIzinGun,
                        ISNULL((SELECT SUM(pro_gecyil_devir_saatlikizin) 
                                FROM dbo.PERSONEL_TAHAKKUK_OZETLERI WITH (NOLOCK) 
                                WHERE pro_kodozet = IH.per_kod 
                                AND pro_ozetyili = @Yil), 0) AS GecenYilDevirIzinSaat
                    FROM IzinHaklari IH
                ),
                KullanilanIzinler AS (
                    SELECT
                        GYD.per_kod,
                        GYD.per_adi,
                        GYD.per_soyadi,
                        GYD.CalismaSuresiYil,
                        GYD.HakEdilenYillikIzin,
                        GYD.GecenYilDevirIzinGun,
                        GYD.GecenYilDevirIzinSaat,
                        ISNULL((SELECT SUM(pz_gun_sayisi) 
                                FROM PERSONEL_IZINLERI WITH (NOLOCK) 
                                WHERE pz_pers_kod = GYD.per_kod 
                                AND pz_izin_yil = @Yil), 0) AS KullanilanIzinGun
                    FROM GecenYilDevredilen GYD
                )
                SELECT
                    CalismaSuresiYil AS CalismaSuresi,
                    HakEdilenYillikIzin AS YillikIzinHakki,
                    GecenYilDevirIzinGun AS GecenYilDevirGun,
                    GecenYilDevirIzinSaat AS GecenYilDevirSaat,
                    KullanilanIzinGun AS KullanilanIzin,
                    (GecenYilDevirIzinGun + HakEdilenYillikIzin - KullanilanIzinGun) AS KalanIzinBakiyesi
                FROM KullanilanIzinler";

                    using (SqlCommand command = new SqlCommand(query, erpConnection))
                    {
                        command.Parameters.AddWithValue("@personnelCode", personnelCode);
                        command.Parameters.AddWithValue("@year", year);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // İzin bilgilerini JSON ile dön
                                return Json(new
                                {
                                    success = true,
                                    previousYearDays = Convert.ToDouble(reader["GecenYilDevirGun"]),
                                    previousYearHours = Convert.ToDouble(reader["GecenYilDevirSaat"]),
                                    yearlyEntitlement = Convert.ToInt32(reader["YillikIzinHakki"]),
                                    usedDays = Convert.ToDouble(reader["KullanilanIzin"]),
                                    remainingDays = Convert.ToDouble(reader["KalanIzinBakiyesi"])
                                });
                            }
                            else
                            {
                                return Json(new { success = false, message = "Personel bilgisi bulunamadı" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
        
        

        /// <summary>
        /// İşe giriş tarihine göre yıllık izin hakkını hesaplar
        /// (1-5 yıl arası 14 gün, 5+ yıl 21 gün)
        /// </summary>
        private int CalculateYearlyEntitlement(DateTime startDate)
        {
            int yearsOfService = (DateTime.Now - startDate).Days / 365;

            if (yearsOfService < 1)
            {
                return 0; // 1 yıldan az çalışma süresi varsa izin hakkı yok
            }
            else if (yearsOfService < 5)
            {
                return 14; // 1-5 yıl arası 14 gün
            }
            else
            {
                return 21; // 5+ yıl 21 gün
            }
        }
    }
}