using ClosedXML.Excel;
using Deneme_proje.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // ILogger için gerekli
using System.Collections.Generic;
using System.Linq;
using static Deneme_proje.Models.Entities;

using System.IO;
using NuGet.Protocol.Core.Types;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Deneme_proje.Controllers
{
    [AuthFilter]

    public class FaturaController : BaseController
    {
        private readonly ILogger<FaturaController> _logger;
        private readonly FaturaRepository _faturaRepository;

        // Constructor
        public FaturaController(ILogger<FaturaController> logger, FaturaRepository faturaRepository)
        {
            _logger = logger;
            _faturaRepository = faturaRepository;
        }

        public ActionResult Index(string cariKodu)
        {
            float ticariFaiz = 66.24f;

            // Cari kodu boş olsa bile tüm verileri getirin
            var faturaData = _faturaRepository.GetFaturaData(cariKodu, ticariFaiz);

            return View(faturaData);
        }

        public IActionResult TedarikciKapaliFatura(string cariKodu)
        {
            float ticariFaiz = 66.24f;

            // Cari kodu boş olsa bile tüm verileri getirin
            var faturaData = _faturaRepository.GetTedarikciFaturaData(cariKodu, ticariFaiz);

            // Ensure the type here matches the view expectation
            return View(faturaData);
        }

        public IActionResult CustomerAnalysis(string cariKodu)
        {
            float ticariFaiz = 66.24f;

            // Cari kodu boş olsa bile tüm verileri getirin
            var customerAnalysisData = _faturaRepository.GetFaturaData(cariKodu, ticariFaiz);

            // Ensure the type here matches the view expectation
            return View(customerAnalysisData);
        }

        public IActionResult CariBazliTedarikci(string cariKodu)
        {
            float ticariFaiz = 66.24f;

            // Cari kodu boş olsa bile tüm verileri getirin
            var customerAnalysisData = _faturaRepository.GetTedarikciFaturaData(cariKodu, ticariFaiz);

            // Ensure the type here matches the view expectation
            return View(customerAnalysisData);
        }

        public IActionResult MaliBorc()
        {
            var krediDetayData = _faturaRepository.GetKrediDetayData();
            return View(krediDetayData);
        }

        public IActionResult CanliBilanco()
        {
            
            return View();
        }

        [AllowAnonymous]// Action to get detailed credit information by bank code
        public IActionResult GetKrediDetay(string bankCode)
        {
            try
            {
                var krediDetayListesi = _faturaRepository.GetKrediDetayListByBankCode(bankCode);

                if (krediDetayListesi == null || !krediDetayListesi.Any())
                {
                    ViewBag.ErrorMessage = "No data found for the provided bank code.";
                    return PartialView("_KrediDetayPartial", new Dictionary<string, Dictionary<string, List<KrediDetayi>>>());
                }

                var groupedData = krediDetayListesi
                    .GroupBy(d => d.krsoztaksit_sozkodu)
                    .ToDictionary(
                        g => g.Key, // Contract Code
                        g => g.GroupBy(d => d.AyAd).ToDictionary(
                            gg => gg.Key, // Month
                            gg => gg.ToList()
                        )
                    );

                return PartialView("_KrediDetayPartial", groupedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving kredi detay for bank code: {BankCode}", bankCode);
                return PartialView("_Error", new ErrorViewModel { ErrorMessage = "An error occurred while retrieving details." });
            }
        }

        public ActionResult aokf()
        {
            var krediDetayListesi = _faturaRepository.GetKrediDetayList();
            return View(krediDetayListesi);
        }

        public ActionResult MusteriYaslandirma(string cariIlkKod = "", string cariSonKod = "", string cariKodYapisi = "", DateTime? raporTarihi = null, byte hangiHesaplar = 0)
        {
            var data = _faturaRepository.GetCariMusteriYaslandirma(cariIlkKod, cariSonKod, cariKodYapisi, raporTarihi, hangiHesaplar);
            return View(data);
        }
        public ActionResult TedarikciYaslandirma(string cariIlkKod = "", string cariSonKod = "", string cariKodYapisi = "", DateTime? raporTarihi = null, byte hangiHesaplar = 0)
        {
            var data = _faturaRepository.GetCariTedarikciYaslandirma(cariIlkKod, cariSonKod, cariKodYapisi, raporTarihi, hangiHesaplar);
            return View(data);
        }
        public IActionResult StokYaslandirma(string stockCode = null, DateTime? reportDate = null, int? depoNo = null)
        {
            // Varsayılan olarak bugünün tarihini kullan
            reportDate ??= DateTime.Now;

            // Stok kodları ve isimlerini al
            var stockCodesAndNames = _faturaRepository.GetStockCodesAndNames();
            var stockSelectList = stockCodesAndNames
                .Select(x => new SelectListItem { Value = x.StockCode, Text = $"{x.StockCode} - {x.StockName}" })
                .ToList();

            ViewData["StockCodesAndNames"] = stockSelectList;

            // Depo numarası ve adlarını al
            var depoList = _faturaRepository.GetDepoList();
            var depoSelectList = depoList
                .Select(d => new SelectListItem { Value = d.DepoNo.ToString(), Text = d.DepoAdi })
                .ToList();

            ViewData["DepoList"] = depoSelectList;

            // Verileri al, depo numarası veya stok kodu filtreleri uygula (eğer varsa)
            var data = _faturaRepository.GetStokYaslandirma(stockCode, reportDate.Value, depoNo);

            if (!data.Any())
            {
                ViewBag.Message = string.IsNullOrEmpty(stockCode)
                    ? "Veri bulunamadı. Stok kodu veya depo seçimi yapabilirsiniz."
                    : "Aramanıza uygun veri bulunamadı.";
            }

            ViewData["SelectedStockCode"] = stockCode;
            ViewData["SelectedDepoNo"] = depoNo;

            return View(data);
        }





        public IActionResult Stok()
        {
            // Fetch stock codes and names
            var stockCodesAndNames = _faturaRepository.GetStockCodesAndNames();

            // Prepare the view model
            var viewModel = new StokViewModel
            {
                StockCodes = stockCodesAndNames.Select(x => x.StockCode).ToList()
            };

            // Return the view with the view model
            return View(viewModel);
        }
        public ActionResult StockAging(string stokKod, DateTime? raporTarihi)
        {
            // Eğer stok kodu boş veya null ise formu tekrar göster
            if (string.IsNullOrEmpty(stokKod))
            {
                return View(); // Kullanıcıdan stok kodu girmesini bekliyoruz
            }

            // Eğer stok kodu girilmişse raporu getir
            var stockAgingList = _faturaRepository.GetStockAging(stokKod, raporTarihi);

            if (stockAgingList == null || !stockAgingList.Any())
            {
                // Eğer rapor boşsa, kullanıcıya stok kodu bulunamadığı mesajını ver
                ViewBag.ErrorMessage = "Girilen stok kodu için rapor bulunamadı.";
                return View(); // Formu tekrar göster
            }

            // Eğer rapor varsa, sonuçları kullanıcıya göster
            return View(stockAgingList); // Rapor sonuçlarını model olarak gönderiyoruz
        }


        [HttpGet]
        public IActionResult NakitAkisi()
        {
            return View();
        }

        // POST method to process the form and show data
        [HttpPost]
        [AllowAnonymous]
        public IActionResult NakitAkisi(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            // Verilerin List'e dönüştürülmesi
            var musteriCekleri = _faturaRepository.GetMusteriCekleri(baslamaTarihi, bitisTarihi).ToList();
            var firmaCekleri = _faturaRepository.GetFirmaCekleri(baslamaTarihi, bitisTarihi).ToList();
            var musteriKrediKartlari = _faturaRepository.GetMusteriKrediKartlari(baslamaTarihi, bitisTarihi).ToList();
            var firmaKrediKartlari = _faturaRepository.GetFirmaKrediKartlari(baslamaTarihi, bitisTarihi).ToList();
            var artiBakiyeFaturaMusteri = _faturaRepository.GetArtiBakiyeFaturaMusteri(baslamaTarihi, bitisTarihi).ToList();
            var artiBakiyeFaturaTedarikci = _faturaRepository.GetArtiBakiyeFaturaTedarikci(baslamaTarihi, bitisTarihi).ToList();

            // Yeni: Kredi Detayları alınması
            var krediDetaylari = _faturaRepository.GetKrediDetay(baslamaTarihi, bitisTarihi).ToList();

            var viewModel = new CekDurumuViewModel
            {
                BaslamaTarihi = baslamaTarihi,
                BitisTarihi = bitisTarihi,
                MusteriCekleri = musteriCekleri,
                FirmaCekleri = firmaCekleri,
                MusteriKrediKartlari = musteriKrediKartlari,
                FirmaKrediKartlari = firmaKrediKartlari,
                ArtiBakiyeFaturaMusteri = artiBakiyeFaturaMusteri,
                ArtiBakiyeFaturaTedarikci = artiBakiyeFaturaTedarikci,
                KrediDetaylari = krediDetaylari // Yeni eklenen özellik
            };

            return View(viewModel);
        }

        public IActionResult CiroRaporuDepoBazli(DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            baslamaTarihi ??= DateTime.Now.AddMonths(-1); // Varsayılan 1 ay önce
            bitisTarihi ??= DateTime.Now;

            var ciroRaporu = _faturaRepository.GetCiroRaporuDepoBazli(baslamaTarihi.Value, bitisTarihi.Value);

            ViewData["BaslamaTarihi"] = baslamaTarihi.Value.ToString("yyyy-MM-dd");
            ViewData["BitisTarihi"] = bitisTarihi.Value.ToString("yyyy-MM-dd");

            return View(ciroRaporu);
        }

        public IActionResult EnCokSatilan(DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            baslamaTarihi ??= DateTime.Now.AddMonths(-1); // Varsayılan 1 ay önce
            bitisTarihi ??= DateTime.Now;

            var urunRaporu = _faturaRepository.GetEnCokSatilanUrunler(baslamaTarihi.Value, bitisTarihi.Value);

            ViewData["BaslamaTarihi"] = baslamaTarihi.Value.ToString("yyyy-MM-dd");
            ViewData["BitisTarihi"] = bitisTarihi.Value.ToString("yyyy-MM-dd");

            return View(urunRaporu);
        }

        public IActionResult SatilanMalinKarlilikveMaliyet(DateTime? baslamaTarihi, DateTime? bitisTarihi, string depoNo = "")
        {
            baslamaTarihi ??= DateTime.Now.AddMonths(-1);
            bitisTarihi ??= DateTime.Now;

            var rapor = _faturaRepository.GetSatilanMalinKarlilikveMaliyet(baslamaTarihi.Value, bitisTarihi.Value, depoNo);

            // Depo listesini al ve ViewData'ya ekle
            var depoList = _faturaRepository.GetDepoList();
            var depoSelectList = depoList
                .Select(d => new SelectListItem { Value = d.DepoNo.ToString(), Text = d.DepoAdi })
                .ToList();

            ViewData["DepoList"] = depoSelectList;

            ViewData["BaslamaTarihi"] = baslamaTarihi.Value.ToString("yyyy-MM-dd");
            ViewData["BitisTarihi"] = bitisTarihi.Value.ToString("yyyy-MM-dd");
            ViewData["DepoNo"] = depoNo;

            return View(rapor);
        }

        public IActionResult StokRaporu(int? anaGrup = null, int? reyonKodu = null, int? depoNo = null)
        {
            if (!depoNo.HasValue)
            {
                // Kullanıcı depo seçmediyse sayfa formu göster
                ViewData["ErrorMessage"] = "Lütfen bir depo numarası seçiniz.";
                return View();
            }

            try
            {
                var stokRaporu = _faturaRepository.GetStokRaporu(anaGrup, reyonKodu, depoNo.Value);

                if (!stokRaporu.Any())
                {
                    ViewBag.Message = "Arama kriterlerinize uygun sonuç bulunamadı.";
                }

                return View(stokRaporu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok raporu oluşturulurken hata oluştu.");
                ViewData["ErrorMessage"] = "Stok raporu oluşturulurken bir hata meydana geldi.";
                return View();
            }
        }


        [AllowAnonymous]
        public IActionResult GetIsEmirleri()
        {
            try
            {
                var isEmirleri = _faturaRepository.GetIsEmirleri();
                var hasProductionPermission = _faturaRepository.HasProductionPermission();

                // Property isimlerini açıkça belirterek JSON'a dönüştür
                return Json(new
                {
                    success = true,
                    isEmirleri = isEmirleri.Select(e => new
                    {
                        e.is_Guid,
                        e.is_Kod,
                        e.is_Ismi,
                        e.is_EmriDurumu,
                        e.is_BaslangicTarihi,
                        UrunKodu = e.UrunKodu,  // Açıkça belirt
                        UrunAdi = e.UrunAdi,    // Açıkça belirt
                        Miktar = e.Miktar,      // Açıkça belirt
                        IsMerkezi = e.IsMerkezi, // Açıkça belirt
                      
                    }),
                    hasProductionPermission
                }, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null // Property isimlerini olduğu gibi koru
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emirleri listelenirken hata oluştu");
                return Json(new { success = false, message = "Veriler getirilirken hata oluştu." });
            }
        }

        public IActionResult IsEmirleri()
        {
            try
            {
                var isEmirleri = _faturaRepository.GetIsEmirleri();

                // Üretim yetkisini ViewBag'e ekleyin
                ViewBag.HasProductionPermission = _faturaRepository.HasProductionPermission();

                return View(isEmirleri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emirleri listelenirken hata oluştu");
                TempData["ErrorMessage"] = "İş emirleri listelenirken bir hata oluştu.";
                return View("Error");
            }
        }
        [HttpPost]
     
        public JsonResult UretIsEmri(string isEmriKodu, string urunKodu, int depoNo)
        {
            try
            {
                var sonuc = _faturaRepository.UretIsEmri(isEmriKodu, urunKodu, depoNo);
                return Json(new
                {
                    success = true,
                    message = $"Üretim başarıyla tamamlandı. {sonuc}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Üretim işlemi sırasında hata oluştu");
                return Json(new
                {
                    success = false,
                    message = "Üretim işlemi sırasında bir hata oluştu."
                });
            }
        }


        [HttpPost]
        [AllowAnonymous]
        public IActionResult UpdateIsEmriDurumu(string isEmriKodu, int yeniDurum)
        {
            try
            {
                var success = _faturaRepository.UpdateIsEmriDurumu(isEmriKodu, yeniDurum);
                if (success)
                {
                    TempData["SuccessMessage"] = "İş emri durumu başarıyla güncellendi.";
                }
                else
                {
                    TempData["ErrorMessage"] = "İş emri durumu güncellenemedi.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş emri durumu güncellenirken hata oluştu");
                TempData["ErrorMessage"] = "İş emri durumu güncellenirken bir hata oluştu.";
            }

            return RedirectToAction("IsEmirleri");
        }
  
        public IActionResult MusteriRiskAnalizi(DateTime? raporTarihi = null)
        {
            try
            {
                var data = _faturaRepository.GetMusteriRiskAnalizi(raporTarihi);
                ViewData["RaporTarihi"] = raporTarihi?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri risk analizi görüntülenirken hata oluştu");
                TempData["HataMesaji"] = "Risk analizi oluşturulurken bir hata meydana geldi.";
                return View("Hata");
            }
        }

        public IActionResult SiparisDurum(string filter = "all")
        {
            // Başlangıç tarihi bugünden 15 gün öncesi
            var startDate = DateTime.Now.AddDays(-15);
            // Bitiş tarihi bugün
            var endDate = DateTime.Now;

            var siparisler = _faturaRepository.GetSiparisDetay(startDate, endDate);

            // Filtreleme işlemi
            if (filter == "started")
            {
                siparisler = siparisler.Where(s => s.IslemDurumu == "Basladi");
            }

            ViewData["CurrentFilter"] = filter; // Aktif filtreyi view'a gönder
            return View(siparisler);
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult UpdateSiparisDurum(int evrakSira, Guid siparisGuid, string rampaBilgisi, string islemDurumu)
        {
            try
            {
                var result = _faturaRepository.UpdateSiparisDurum(evrakSira, siparisGuid, rampaBilgisi, islemDurumu);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş durumu güncellenirken hata oluştu");
                return Json(new RampUpdateResult
                {
                    Success = false,
                    Message = "İşlem sırasında bir hata oluştu."
                });
            }
        }


        [HttpGet]
           [AllowAnonymous]
       
        public IActionResult StokHareketleriniGetir(string siparisGuid)
        {
            _logger.LogInformation($"Stok hareketleri istendi. SiparisGuid: {siparisGuid}");

            try
            {
                var stokHareketleri = _faturaRepository.GetStokHareketleri(siparisGuid);
                return Json(new
                {
                    success = true,
                    data = stokHareketleri,
                    count = stokHareketleri.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok hareketleri getirilirken hata oluştu");
                return Json(new
                {
                    success = false,
                    error = "Stok hareketleri yüklenirken bir hata oluştu.",
                    message = ex.Message
                });
            }
        }

    }
}

   


//[HttpPost]
//public IActionResult Stok(string stokKod, DateTime? raporTarihi)
//{
//    // Fetch data based on selected stock code and report date
//    var data = _faturaRepository.GetStokYaslandirmaData(stokKod, raporTarihi);

//    // Prepare the view model with the fetched data
//    var viewModel = new StokViewModel
//    {
//        StockCodes = _faturaRepository.GetStockCodesAndNames().Select(x => x.StockCode).ToList(),
//        StokYaslandirmaData = data
//    };

//    return View(viewModel);
//}











