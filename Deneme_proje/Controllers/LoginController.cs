using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Deneme_proje.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly string encryptionKey = "YourSecretKey123!"; // Güvenli bir key kullanın

        public LoginController(IConfiguration configuration, DatabaseSelectorService dbSelectorService)
        {
            _configuration = configuration;
            _dbSelectorService = dbSelectorService;
        }

        // Şifre encryption metodu
        private string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return null;

            try
            {
                using (var aes = Aes.Create())
                {
                    using (var md5 = MD5.Create())
                    {
                        aes.Key = md5.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
                        aes.IV = new byte[16];
                    }

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
                        return Convert.ToBase64String(encryptedBytes);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Şifre decryption metodu
        private string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword)) return null;

            try
            {
                using (var aes = Aes.Create())
                {
                    using (var md5 = MD5.Create())
                    {
                        aes.Key = md5.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
                        aes.IV = new byte[16];
                    }

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IActionResult Index()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");

                // Admin kullanıcı bağlantı bilgilerini kontrol et
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"SELECT versiyon, ip_adresi, db_kullaniciadi, db_sifre 
                    FROM Web_Kullanici 
                    WHERE kullanici_adi = 'admin'";

                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool hasConnectionInfo =
                                !string.IsNullOrEmpty(reader["versiyon"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["ip_adresi"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["db_kullaniciadi"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["db_sifre"]?.ToString());

                            if (hasConnectionInfo)
                            {
                                // Bağlantı bilgileri doluysa direkt LoginKullanici'ya yönlendir
                                HttpContext.Session.SetString("Version", reader["versiyon"].ToString());
                                return RedirectToAction("LoginKullanici");
                            }
                        }
                    }
                }

                // Bağlantı bilgileri eksikse normal Index sayfasını göster
                return View();
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine("Login Controller Index'te hata oluştu.");
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                // Store the error message in ViewData
                ViewData["ErrorMessage"] = ex.Message;
                ViewData["StackTrace"] = ex.StackTrace;

                return View("~/Views/Shared/ErrorView.cshtml");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");

                // Admin kullanıcı kontrolü ve yetkilendirme
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"SELECT versiyon, ip_adresi, db_kullaniciadi, db_sifre, db_varsayilan 
                    FROM Web_Kullanici 
                    WHERE kullanici_adi = 'admin'";

                    SqlCommand command = new SqlCommand(query, connection);
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            bool hasConnectionInfo =
                                !string.IsNullOrEmpty(reader["versiyon"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["ip_adresi"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["db_kullaniciadi"]?.ToString()) &&
                                !string.IsNullOrEmpty(reader["db_sifre"]?.ToString());

                            if (hasConnectionInfo)
                            {
                                HttpContext.Session.SetString("Version", reader["versiyon"].ToString());
                                return RedirectToAction("LoginKullanici");
                            }
                        }
                    }
                }

                // Normal kullanıcı girişi
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Web_Kullanici WHERE kullanici_adi = @username AND sifre = @password";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);

                    await connection.OpenAsync();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Kullanıcı doğrulandı, dinamik veritabanı bağlantısını güncelle
                            string selectedVersion = reader["versiyon"]?.ToString() ?? "V16"; // Varsayılan versiyon
                            string ipAddress = reader["ip_adresi"]?.ToString();
                            string dbUsername = reader["db_kullaniciadi"]?.ToString();
                            string dbPassword = DecryptPassword(reader["db_sifre"]?.ToString());
                            string defaultDatabase = reader["db_varsayilan"]?.ToString();

                            // Dinamik bağlantı stringini oluştur
                            string dynamicConnectionString = selectedVersion == "V16"
                                ? $"Server={ipAddress};Database=MikroDB_V16;User Id={dbUsername};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;"
                                : $"Server={ipAddress};Database=MikroDesktop;User Id={dbUsername};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;";

                            // Bağlantı dizesini appsettings.json'da güncelle
                            UpdateAppSettings(
                                selectedVersion == "V16" ? "MikroDB_V16" : "MikroDesktop",
                                dynamicConnectionString
                            );

                            // Session'a gerekli bilgileri kaydet
                            HttpContext.Session.SetString("Username", username);
                            HttpContext.Session.SetString("SelectedVersion", selectedVersion);
                            HttpContext.Session.SetString("SelectedDatabase",
                                selectedVersion == "V16" ? "MikroDB_V16" : "MikroDesktop");

                            TempData["ShowModal"] = true;
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                ViewBag.Message = "Geçersiz kullanıcı adı veya şifre";
                return View();
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine("Login Controller Post Index'te hata oluştu.");
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                // Store the error message in ViewData
                ViewData["ErrorMessage"] = ex.Message;
                ViewData["StackTrace"] = ex.StackTrace;

                return View("~/Views/Shared/ErrorView.cshtml");
            }
        }


        [HttpPost]
        public async Task<IActionResult> LoginKullanici(string username, string password)
        {
            try
            {
                // Version bilgisini kontrol et
                string version = HttpContext.Session.GetString("Version");
                if (string.IsNullOrEmpty(version))
                {
                    return RedirectToAction("Index");
                }

                // Version bilgisini SelectedVersion olarak da kaydet
                HttpContext.Session.SetString("SelectedVersion", version);

                // Admin kullanıcının varsayılan veritabanını bul
                string defaultDatabase = await GetAdminDefaultDatabase();

                // Varsayılan veritabanını session'a kaydet
                HttpContext.Session.SetString("SelectedDatabase", defaultDatabase);

                // Connection string'i seç
                string baseConnectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                using (SqlConnection connection = new SqlConnection(baseConnectionString))
                {
                    await connection.OpenAsync();

                    // Önce kullanıcının User_no'sunu al
                    string userNoQuery = "SELECT User_no FROM KULLANICILAR WHERE User_name = @username AND User_name = @password";
                    string userNo = null;

                    using (SqlCommand userNoCommand = new SqlCommand(userNoQuery, connection))
                    {
                        userNoCommand.Parameters.AddWithValue("@username", username);
                        userNoCommand.Parameters.AddWithValue("@password", password);
                        var result = await userNoCommand.ExecuteScalarAsync();
                        if (result != null)
                        {
                            userNo = result.ToString();
                        }
                        else
                        {
                            ModelState.AddModelError("", "Geçersiz kullanıcı adı veya şifre");
                            return View();
                        }
                    }

                    // Kullanıcının giriş yetkisini kontrol et
                    string erpConnectionString = _configuration.GetConnectionString("ERPDatabase");
                    using (SqlConnection erpConnection = new SqlConnection(erpConnectionString))
                    {
                        await erpConnection.OpenAsync();
                        string yetkiQuery = "SELECT GirisYetkisi FROM KullaniciYonetimi WHERE User_no = @User_no";

                        using (SqlCommand yetkiCommand = new SqlCommand(yetkiQuery, erpConnection))
                        {
                            yetkiCommand.Parameters.AddWithValue("@User_no", userNo);
                            var yetkiResult = await yetkiCommand.ExecuteScalarAsync();

                            if (yetkiResult == null || !(bool)yetkiResult)
                            {
                                ModelState.AddModelError("", "Sisteme giriş yetkiniz bulunmamaktadır.");
                                return View();
                            }
                        }
                    }

                    // Kullanıcı girişi için ana sorgu
                    string tableName = "KULLANICILAR";
                    string query = $"SELECT * FROM {tableName} WHERE User_name = @username AND User_name = @password";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", password);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Kullanıcı doğrulandı
                                HttpContext.Session.SetString("Username", username);
                                HttpContext.Session.SetString("IsAuthenticated", "true");
                                HttpContext.Session.SetString("UserNo", userNo);

                                try
                                {
                                    var finalConnectionString = _dbSelectorService.GetConnectionString();
                                    return RedirectToAction("Index", "Home");
                                }
                                catch (Exception dbEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Database connection error: {dbEx.Message}");
                                    ModelState.AddModelError("", $"Veritabanı bağlantı hatası: {dbEx.Message}");
                                    return View();
                                }
                            }
                        }
                    }
                }

                ModelState.AddModelError("", "Geçersiz kullanıcı adı veya şifre");
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                ModelState.AddModelError("", $"Bağlantı hatası oluştu: {ex.Message}");
                return View();
            }
        }

        // Yeni eklenen metot
        private async Task<string> GetAdminDefaultDatabase()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT db_varsayilan FROM Web_Kullanici WHERE kullanici_adi = 'admin'";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        var defaultDatabase = await command.ExecuteScalarAsync();

                        // Null kontrolü ve ToString() çevirimi
                        if (defaultDatabase == null)
                        {
                            // Eğer varsayılan veritabanı yoksa, ilk veritabanını bul
                            string version = HttpContext.Session.GetString("SelectedVersion") ?? "V16";
                            return GetFirstAvailableDatabase(version);
                        }

                        return defaultDatabase.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Admin default database error: {ex.Message}");

                // Hata durumunda varsayılan versiyon için ilk veritabanını bul
                string version = HttpContext.Session.GetString("SelectedVersion") ?? "V16";
                return GetFirstAvailableDatabase(version);
            }
        }

        // Yardımcı metot
        private string GetFirstAvailableDatabase(string version)
        {
            try
            {
                string connectionString = version == "V16"
                    ? _configuration.GetConnectionString("MikroDB_V16")
                    : _configuration.GetConnectionString("MikroDesktop");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT TOP 1 DB_kod FROM VERI_TABANLARI ORDER BY DB_kod";

                    using (var command = new SqlCommand(query, connection))
                    {
                        var firstDb = command.ExecuteScalar()?.ToString();

                        if (string.IsNullOrEmpty(firstDb))
                        {
                            throw new Exception("Hiç veritabanı bulunamadı");
                        }

                        return firstDb;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"First available database error: {ex.Message}");
                throw;
            }
        }

        public IActionResult LoginKullanici()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Version")))
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UpdateConnectionInfo([FromBody] ConnectionInfo model)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");
                string username = HttpContext.Session.GetString("Username");

                // Şifreyi encrypt et
                string encryptedPassword = model.DbPassword != null
                    ? EncryptPassword(model.DbPassword)
                    : null;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = @"UPDATE Web_Kullanici 
                   SET versiyon = @version,
                       ip_adresi = @ipAddress,
                       db_kullaniciadi = @dbUsername,
                       db_sifre = @dbPassword,
                       db_varsayilan = @defaultDb
                   WHERE kullanici_adi = @username";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@version", model.Version ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ipAddress", model.IpAddress ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@dbUsername", model.DbUsername ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@dbPassword", encryptedPassword ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@defaultDb", model.DefaultDatabase ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@username", username);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    if (!string.IsNullOrEmpty(model.Version))
                    {
                        HttpContext.Session.SetString("SelectedVersion", model.Version);
                    }

                    if (!string.IsNullOrEmpty(model.DefaultDatabase))
                    {
                        HttpContext.Session.SetString("SelectedDatabase", model.DefaultDatabase);
                    }
                }

                return Ok(new { success = true, message = "Bağlantı bilgileri güncellendi" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private void UpdateAppSettings(string key, string connectionString)
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["ConnectionStrings"][key] = connectionString;
            string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            System.IO.File.WriteAllText(appSettingsPath, output);
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }

    public class UserConnectionInfo
    {
        public string Version { get; set; }
        public string IpAddress { get; set; }
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public string DefaultDb { get; set; }
    }

    public class ConnectionInfo
    {
        public string Version { get; set; }
        public string IpAddress { get; set; }
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
        public string DefaultDatabase { get; set; }
    }
}