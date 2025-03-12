using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using static Deneme_proje.Models.YonetimEntities;

namespace Deneme_proje.Controllers
{
    public class KullaniciYonetimiController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _mikroDbConnection;

        public KullaniciYonetimiController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("ERPDatabase");
            _mikroDbConnection = _configuration.GetConnectionString("MikroDB_V16");
        }

        public async Task<IActionResult> Index()
        {
            var kullanicilar = new List<KullaniciListViewModel>();

            try
            {
                using (var connection = new SqlConnection(_mikroDbConnection))
                {
                    await connection.OpenAsync();

                    var query = @"SELECT k.User_no, k.User_name, k.User_LongName, k.User_EMail, 
                            ISNULL(ky.GirisYetkisi, 1) as GirisYetkisi 
                            FROM KULLANICILAR k 
                            LEFT JOIN [DBT_ERP].dbo.KullaniciYonetimi ky 
                            ON k.User_no = ky.User_no";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                kullanicilar.Add(new KullaniciListViewModel
                                {
                                    UserNo = reader["User_no"].ToString(),
                                    UserName = reader["User_name"].ToString(),
                                    LongName = reader["User_LongName"].ToString(),
                                    Email = reader["User_EMail"].ToString(),
                                    GirisYetkisi = Convert.ToBoolean(reader["GirisYetkisi"])
                                });
                            }
                        }
                    }
                }

                return View(kullanicilar);
            }
            catch (Exception ex)
            {
                // Hata yönetimi
                return View("Error", ex);
            }
        }
        [AllowAnonymous]
        public async Task<IActionResult> UpdateYetki(string userNo, bool girisYetkisi)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"IF EXISTS (SELECT 1 FROM KullaniciYonetimi WHERE User_no = @User_no)
                        UPDATE KullaniciYonetimi 
                        SET GirisYetkisi = @girisYetkisi 
                        WHERE User_no = @User_no
                        ELSE
                        INSERT INTO KullaniciYonetimi (User_no, GirisYetkisi) 
                        VALUES (@User_no, @girisYetkisi)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@User_no", userNo);
                        command.Parameters.AddWithValue("@girisYetkisi", girisYetkisi);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class UpdateYetkiModel
        {
            public string UserNo { get; set; }
            public bool GirisYetkisi { get; set; }
        }
    }
}