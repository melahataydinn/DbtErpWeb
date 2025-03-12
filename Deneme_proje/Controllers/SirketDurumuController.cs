using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using static Deneme_proje.Models.SirketDurumuEntites;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;
using Deneme_proje.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Deneme_proje.Models.Entities;
using System.Text;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class SirketDurumuController : BaseController
    {
        private readonly string _connectionString;
        private readonly ILogger<SirketDurumuController> _logger;
        private readonly SirketDurumuRepository _sirketDurumuRepository;
        private readonly DatabaseSelectorService _dbSelectorService;

        public SirketDurumuController(IConfiguration configuration, ILogger<SirketDurumuController> logger, SirketDurumuRepository sirketDurumuRepository, DatabaseSelectorService dbSelectorService)
        {
            _dbSelectorService = dbSelectorService;
            _logger = logger;
            _sirketDurumuRepository = sirketDurumuRepository;
        }

        // Çek Analiz Metodu
        public IActionResult CekAnaliz(string sck_sonpoz, string projeKodu, string srmMerkeziKodu, DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            IEnumerable<CekAnalizi> cekAnaliziList;
            IEnumerable<MusteriCekViewModel> musteriCekleriList;

            try
            {
                // Using repository methods to fetch data
                cekAnaliziList = _sirketDurumuRepository.GetFirmaCekleri(sck_sonpoz, projeKodu, srmMerkeziKodu, baslamaTarihi, bitisTarihi);
                musteriCekleriList = _sirketDurumuRepository.GetMusteriCekleri(baslamaTarihi ?? DateTime.MinValue, bitisTarihi ?? DateTime.MaxValue); // Default date range if null
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Çek analizi verileri alınırken bir hata oluştu.");
                return View("Error");
            }

            // Creating a ViewModel with the fetched data
            var viewModel = new CekAnaliziViewModel
            {
                CekAnaliziList = cekAnaliziList,
               
            };

            // Passing parameters to the view using ViewData
            ViewData["sck_sonpoz"] = sck_sonpoz;
            ViewData["projeKodu"] = projeKodu;
            ViewData["srmMerkeziKodu"] = srmMerkeziKodu;
            ViewData["baslamaTarihi"] = baslamaTarihi?.ToString("yyyy-MM-dd"); // Format the date properly
            ViewData["bitisTarihi"] = bitisTarihi?.ToString("yyyy-MM-dd");

            return View(viewModel);
        }

        // Banka Analiz Metodu
        public IActionResult BankaAnaliz(DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            var viewModel = new BankDetailsViewModel
            {
                Banks = _sirketDurumuRepository.GetBanks(),
                BaslamaTarihi = baslamaTarihi ?? DateTime.Now.AddMonths(-1),
                BitisTarihi = bitisTarihi ?? DateTime.Now
            };

            return View(viewModel);
        }

        // Banka Detayları Getirme Metodu
        public IActionResult Getir_Detay(string BankaKodu, DateTime? BaslamaTarihi, DateTime? BitisTarihi)
        {
            List<BankDetailModel> bankDetails;
            BaslamaTarihi ??= DateTime.Now.AddMonths(-1);
            BitisTarihi ??= DateTime.Now;

            try
            {
                bankDetails = _sirketDurumuRepository.GetBankDetails(BankaKodu, BaslamaTarihi.Value, BitisTarihi.Value, 0, null, null).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Banka detayları alınırken bir hata oluştu.");
                return Content("Banka detayları alınırken bir hata oluştu.");
            }

            return PartialView("_BankDetailsGrid", bankDetails);
        }

        // İyileştirilmiş GetCariHareketDetay Controller Metodu
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetCariHareketDetay(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return Content("<div class='alert alert-warning'>GUID değeri geçersiz veya boş.</div>", "text/html");
            }

            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // İlk olarak bu guid'e ait veri var mı kontrol et
                    string checkQuery = "SELECT COUNT(*) FROM CARI_HESAP_HAREKETLERI WHERE cha_Guid = @guid";
                    var checkCommand = new SqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@guid", guid);
                    int recordCount = (int)checkCommand.ExecuteScalar();

                    if (recordCount == 0)
                    {
                        return Content("<div class='alert alert-info'>Bu evrak için detay kaydı bulunamadı.</div>", "text/html");
                    }

                    // İstenen sorguyu çalıştırma
                    string query = @"SELECT 
                sth_fat_uid,
                sth_evrakno_seri,
                sth_evrakno_sira,
                sth_stok_kod,
                sto_isim,
                sth_miktar,
                cha_meblag,
                cha_tarihi 
            FROM CARI_HESAP_HAREKETLERI 
            LEFT JOIN STOK_HAREKETLERI ON cha_Guid = sth_fat_uid
            LEFT JOIN STOKLAR ON sth_stok_kod = sto_kod 
            WHERE cha_Guid = @guid";

                    var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@guid", guid);
                    command.CommandTimeout = 60; // 60 saniye zaman aşımı

                    var reader = command.ExecuteReader();

                    // Stok kodu ve tarihe göre gruplamak için anahtar oluşturma
                    Dictionary<string, List<dynamic>> gruplar = new Dictionary<string, List<dynamic>>();
                    Dictionary<string, (double toplamMiktar, double toplamMeblag)> toplamlar = new Dictionary<string, (double, double)>();

                    // Verileri oku ve grupla
                    while (reader.Read())
                    {
                        var stokKod = reader.IsDBNull(reader.GetOrdinal("sth_stok_kod")) ? "-" : reader.GetString(reader.GetOrdinal("sth_stok_kod"));
                        var tarih = reader.IsDBNull(reader.GetOrdinal("cha_tarihi")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("cha_tarihi"));
                        var tarihStr = tarih != DateTime.MinValue ? tarih.ToString("dd.MM.yyyy") : "-";

                        // Grup anahtarı oluştur: "StokKodu_Tarih"
                        string grupAnahtari = $"{stokKod}_{tarihStr}";

                        // Hareket verilerini al
                        var miktar = reader.IsDBNull(reader.GetOrdinal("sth_miktar")) ? 0 : reader.GetDouble(reader.GetOrdinal("sth_miktar"));
                        var meblag = reader.IsDBNull(reader.GetOrdinal("cha_meblag")) ? 0 : reader.GetDouble(reader.GetOrdinal("cha_meblag"));

                        // Diğer verileri al
                        var fatUid = reader.IsDBNull(reader.GetOrdinal("sth_fat_uid")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("sth_fat_uid"));
                        var evrakSeri = reader.IsDBNull(reader.GetOrdinal("sth_evrakno_seri")) ? "-" : reader.GetString(reader.GetOrdinal("sth_evrakno_seri"));
                        var evrakSira = reader.IsDBNull(reader.GetOrdinal("sth_evrakno_sira")) ? 0 : reader.GetInt32(reader.GetOrdinal("sth_evrakno_sira"));
                        var stokIsim = reader.IsDBNull(reader.GetOrdinal("sto_isim")) ? "-" : reader.GetString(reader.GetOrdinal("sto_isim"));

                        // Hareket bilgisini oluştur
                        var hareket = new
                        {
                            FatUid = fatUid,
                            EvrakSeri = evrakSeri,
                            EvrakSira = evrakSira,
                            StokKod = stokKod,
                            StokIsim = stokIsim,
                            Miktar = miktar,
                            Meblag = meblag,
                            Tarih = tarihStr
                        };

                        // Grubu kontrol et veya oluştur
                        if (!gruplar.ContainsKey(grupAnahtari))
                        {
                            gruplar[grupAnahtari] = new List<dynamic>();
                            toplamlar[grupAnahtari] = (0, 0);
                        }

                        // Hareketi gruba ekle
                        gruplar[grupAnahtari].Add(hareket);

                        // Toplamları güncelle
                        var mevcutToplam = toplamlar[grupAnahtari];
                        toplamlar[grupAnahtari] = (mevcutToplam.toplamMiktar + miktar, mevcutToplam.toplamMeblag + meblag);
                    }

                    reader.Close();

                    // Genel toplam hesapla
                    double genelToplamMiktar = toplamlar.Values.Sum(t => t.toplamMiktar);
                    double genelToplamMeblag = toplamlar.Values.Sum(t => t.toplamMeblag);

                    // HTML tablosu oluştur - PDF dostu sınıflar ve ID ekleyerek
                    var detailHtml = new StringBuilder();
                    detailHtml.Append("<div class='detail-report-container'>");

                    // Başlık ve açıklama eklendi
                    detailHtml.Append("<h4 class='mb-3'>Evrak Detay Bilgileri</h4>");
                    detailHtml.Append("<p class='mb-3'>Aşağıdaki tabloda, seçilen evraka ait stok hareketleri stok kodu ve tarih bazında gruplandırılmış olarak gösterilmektedir.</p>");

                    detailHtml.Append("<div class='table-responsive'>");
                    detailHtml.Append("<table class='table table-striped table-bordered detail-table' id='detailTable'>");

                    // Tablo başlığı
                    detailHtml.Append("<thead class='table-dark'>");
                    detailHtml.Append("<tr>");
                    detailHtml.Append("<th>Evrak Seri/Sıra</th>");
                    detailHtml.Append("<th>Tarih</th>");
                    detailHtml.Append("<th>Stok Kodu</th>");
                    detailHtml.Append("<th>Stok Adı</th>");
                    detailHtml.Append("<th class='text-right'>Toplam Miktar</th>");
                    detailHtml.Append("<th class='text-right'>Toplam Meblağ</th>");
                    detailHtml.Append("</tr>");
                    detailHtml.Append("</thead>");

                    // Tablo içeriği
                    detailHtml.Append("<tbody>");

                    if (gruplar.Count > 0)
                    {
                        foreach (var grup in gruplar)
                        {
                            var ilkHareket = grup.Value.FirstOrDefault();
                            var toplamDegerler = toplamlar[grup.Key];

                            if (ilkHareket != null)
                            {
                                detailHtml.Append("<tr>");
                                detailHtml.Append($"<td>{ilkHareket.EvrakSeri}/{ilkHareket.EvrakSira}</td>");
                                detailHtml.Append($"<td>{ilkHareket.Tarih}</td>");
                                detailHtml.Append($"<td>{ilkHareket.StokKod}</td>");
                                detailHtml.Append($"<td>{ilkHareket.StokIsim}</td>");
                                detailHtml.Append($"<td class='text-right'>{toplamDegerler.toplamMiktar.ToString("N2")}</td>");
                                detailHtml.Append($"<td class='text-right'>{toplamDegerler.toplamMeblag.ToString("N2")} ₺</td>");
                                detailHtml.Append("</tr>");
                            }
                        }

                        // Genel toplam satırı ekle
                        detailHtml.Append("<tr class='table-info font-weight-bold'>");
                        detailHtml.Append("<td colspan='4' class='text-right'>GENEL TOPLAM</td>");
                        detailHtml.Append($"<td class='text-right'>{genelToplamMiktar.ToString("N2")}</td>");
                        detailHtml.Append($"<td class='text-right'>{genelToplamMeblag.ToString("N2")} ₺</td>");
                        detailHtml.Append("</tr>");
                    }
                    else
                    {
                        detailHtml.Append("<tr><td colspan='6' class='text-center'>Bu evrak için detay bulunamadı.</td></tr>");
                    }

                    detailHtml.Append("</tbody>");
                    detailHtml.Append("</table>");
                    detailHtml.Append("</div>");
                    detailHtml.Append("</div>");

                    return Content(detailHtml.ToString(), "text/html");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cari hareket detayları getirilirken hata oluştu. GUID: {Guid}", guid);
                    return Content($"<div class='alert alert-danger'><i class='fa fa-exclamation-circle'></i> Veriler getirilirken bir hata oluştu: {ex.Message}</div>", "text/html");
                }
            }
        }// Gösterilmeyecek kolonları belirlemek için yardımcı metot
        private bool ShouldSkipColumn(string columnName)
        {
            // Atlanacak kolonların listesi
            var columnsToSkip = new List<string>
    {
        // Burada göstermek istemediğiniz kolonları ekleyin
        "cha_Guid", // GUID zaten biliniyor
        // Diğer gösterilmeyecek kolonlar...
    };

            return columnsToSkip.Contains(columnName);
        }
        // Cari Hareket Metodu
        [HttpGet]
        public JsonResult GetCariHesaplar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            var cariList = new List<object>();

            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Query to fetch all cari accounts from CARI_HESAPLAR
                    string query = @"SELECT 
                            cari_kod, 
                            cari_unvan1 
                            FROM CARI_HESAPLAR 
                            ORDER BY cari_kod";

                    var command = new SqlCommand(query, connection);
                    var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        cariList.Add(new
                        {
                            Kod = reader["cari_kod"].ToString(),
                            Unvan = reader["cari_unvan1"].ToString()
                        });
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cari hesaplar listelenirken hata oluştu.");
                }
            }

            return Json(cariList);
        }

        public IActionResult CariHareket(int cariCins, string cariKod, string selectedCariText, DateTime? ilkTar, DateTime? sonTar)
        {
            string firmalar = "0";
            int? grupNo = null;
            int odemeEmriDegerlemeDok = 0;

            // Default date range to current month if not provided
            if (!ilkTar.HasValue)
            {
                // First day of current month
                ilkTar = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            if (!sonTar.HasValue)
            {
                // Last day of current month
                sonTar = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                                       DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            }

            // Get the cari kodlari for dropdown
            var cariKodlari = _sirketDurumuRepository.GetCariKodlari();

            // Seçilen cari koduna ait unvan bilgisini al
            string selectedCariUnvan = null;
            if (!string.IsNullOrEmpty(cariKod))
            {
                var selectedCari = cariKodlari.FirstOrDefault(c => c.CariKod == cariKod);
                if (selectedCari != null)
                {
                    selectedCariUnvan = selectedCari.CariUnvan1;
                    // If selectedCariText is not provided, construct it
                    if (string.IsNullOrEmpty(selectedCariText))
                    {
                        selectedCariText = $"{cariKod} - {selectedCariUnvan}";
                    }
                }
            }

            // Get customer transaction data
            var cariHareketFoyu = _sirketDurumuRepository.GetCariHareketFoyu(
                firmalar, cariCins, cariKod, grupNo, null, ilkTar, sonTar, odemeEmriDegerlemeDok, "");

            // ViewData'ya bilgileri aktar
            ViewData["CariKodlari"] = Newtonsoft.Json.JsonConvert.SerializeObject(cariKodlari);
            ViewData["SelectedCariKod"] = cariKod;
            ViewData["SelectedCariUnvan"] = selectedCariUnvan;
            ViewData["SelectedCariText"] = selectedCariText; // Kullanıcının gördüğü tam metin
            ViewData["SelectedCariCins"] = cariCins;
            ViewData["IlkTarih"] = ilkTar?.ToString("yyyy-MM-dd");
            ViewData["SonTarih"] = sonTar?.ToString("yyyy-MM-dd");

            return View(cariHareketFoyu);
        }        // Stok Hareket Metodu
        public IActionResult StokHareket(string stokKodu, DateTime? baslamaTarihi, DateTime? bitisTarihi, int paraBirimi = 0, string depolar = null)
        {
            try
            {
                var depolarList = _sirketDurumuRepository.GetDepolar();
                var stoklarList = _sirketDurumuRepository.GetStoklar();

                ViewBag.Depolar = depolarList;
                ViewBag.Stoklar = stoklarList;

                // Varsayılan tarih aralığı
                if (!baslamaTarihi.HasValue)
                    baslamaTarihi = DateTime.Now.AddMonths(-1);

                if (!bitisTarihi.HasValue)
                    bitisTarihi = DateTime.Now;

                // Depo seçimi kontrolü ve loglaması
                _logger.LogInformation("Controller'da depolar parametresi: {depolar}", depolar ?? "NULL");

                // Stok kodu kontrolü
                if (string.IsNullOrEmpty(stokKodu))
                {
                    return View(Enumerable.Empty<StokHareketFoyu>());
                }

                // Stok hareketlerini al
                var stokHareketFoyu = _sirketDurumuRepository.GetStokHareketFoyu(
                    stokKodu,
                    baslamaTarihi.Value,
                    bitisTarihi.Value,
                    paraBirimi,
                    depolar
                );

                // ViewBag'e değerleri ekle
                ViewBag.SelectedStokKodu = stokKodu;
                ViewBag.BaslamaTarihi = baslamaTarihi.Value.ToString("yyyy-MM-dd");
                ViewBag.BitisTarihi = bitisTarihi.Value.ToString("yyyy-MM-dd");
                ViewBag.ParaBirimi = paraBirimi;
                ViewBag.SelectedDepolar = depolar;

                return View(stokHareketFoyu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok hareket verileri alınırken bir hata oluştu. StokKodu: {StokKodu}", stokKodu);

                TempData["ErrorMessage"] = "Stok hareket verileri alınırken bir hata oluştu: " + ex.Message;

                var depolarList = _sirketDurumuRepository.GetDepolar();
                var stoklarList = _sirketDurumuRepository.GetStoklar();

                ViewBag.Depolar = depolarList;
                ViewBag.Stoklar = stoklarList;

                return View(Enumerable.Empty<StokHareketFoyu>());
            }
        }
        [AllowAnonymous]
        public IActionResult EvrakAktarim()
        {
            return View();
        }
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
