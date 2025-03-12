using Microsoft.AspNetCore.Mvc;
using Deneme_proje.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Linq;
using static Deneme_proje.Models.ServisEntities;

namespace Deneme_proje.Controllers
{
    [AuthFilter]
    public class ServisHareketleriController : Controller
    {
        private readonly IConfiguration _configuration;

        public ServisHareketleriController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet]

        public IActionResult GetNextEvrakNo(string servisMerkezi)
        {
            if (string.IsNullOrEmpty(servisMerkezi))
            {
                return BadRequest("Servis Merkezi belirtilmedi.");
            }

            string connectionString = _configuration.GetConnectionString("ERPDatabase");
            int nextNo;

            string query = @"SELECT ISNULL(MAX(Evrak_No), 0) + 1 
                     FROM ServisHareketleri 
                     WHERE Servis_Merkezi = @ServisMerkezi";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ServisMerkezi", servisMerkezi);
                con.Open();
                nextNo = (int)cmd.ExecuteScalar();
                con.Close();
            }

            return Json(new { nextEvrakNo = nextNo });
        }
        [HttpGet]
        public IActionResult GetEvrak(int evrakNo)
        {
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            var evrak = new
            {
                EvrakNo = 0,
                Tarih = "",
                MusteriAdi = "",
                PlakaNo = "",
                CihazMarkaModel = "",
                CalismaSaati = 0,
                ServiseGirisTarihi = "",
                IlkGozlem = "",
                YedekParcaNo = "",
                YedekParcaAdi = "",
                Adet = "",
                BirimFiyat = "",
                Tutar = ""
            };

            string query = @"SELECT Evrak_No, Tarih, Musteri_Adi, Plaka_No, Cihaz_Marka_Model, 
                     Calisma_Saati, Servise_Giris_Tarihi, Ilk_Gozlem, 
                     Yedek_Parca_No, Yedek_Parca_Adi, Adet, Birim_Fiyat, Tutar
                     FROM ServisHareketleri
                     WHERE Evrak_No = @EvrakNo";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@EvrakNo", evrakNo);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (evrak.EvrakNo == 0)
                    {
                        evrak = new
                        {
                            EvrakNo = (int)reader["Evrak_No"],
                            Tarih = ((DateTime)reader["Tarih"]).ToString("yyyy-MM-dd"),
                            MusteriAdi = reader["Musteri_Adi"].ToString(),
                            PlakaNo = reader["Plaka_No"].ToString(),
                            CihazMarkaModel = reader["Cihaz_Marka_Model"].ToString(),
                            CalismaSaati = (int)reader["Calisma_Saati"],
                            ServiseGirisTarihi = ((DateTime)reader["Servise_Giris_Tarihi"]).ToString("yyyy-MM-dd"),
                            IlkGozlem = reader["Ilk_Gozlem"].ToString(),
                            YedekParcaNo = reader["Yedek_Parca_No"].ToString(),
                            YedekParcaAdi = reader["Yedek_Parca_Adi"].ToString(),
                            Adet = reader["Adet"].ToString(),
                            BirimFiyat = reader["Birim_Fiyat"].ToString(),
                            Tutar = reader["Tutar"].ToString()
                        };
                    }
                }
                con.Close();
            }

            return Json(evrak);
        }

        [HttpGet]
        public IActionResult GetEvrakDetayFull(int evrakNo)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("ERPDatabase");

                // Ana evrak bilgilerini ve detaylarını içerecek bir dictionary
                var evrakDetay = new Dictionary<string, object>();

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Ana evrak bilgilerini çeken detaylı SQL sorgusu
                    string queryEvrak = @"
                SELECT TOP 1 
                    Evrak_No, 
                    Tarih, 
                    Servis_Merkezi, 
                    No, 
                    Musteri_Adi, 
                    Sofor_Adi, 
                    Sofor_Telefon, 
                    Plaka_No, 
                    Cihaz_Marka_Model, 
                    Teknisyen_Adi, 
                    Calisma_Saati, 
                    Servise_Giris_Tarihi, 
                    is_turu, 
                    Ilk_Gozlem,
                    A_Toplam,
                    Vergi,
 Birim_Fiyat, 
    Tutar, 
    iscilik_tutari, 
    harici_iscilik_tutari,
                    G_Toplam
                FROM ServisHareketleri 
                WHERE Evrak_No = @EvrakNo AND Evrak_Sira_No = 1";

                    using (SqlCommand cmdEvrak = new SqlCommand(queryEvrak, con))
                    {
                        cmdEvrak.Parameters.AddWithValue("@EvrakNo", evrakNo);


                        using (SqlDataReader reader = cmdEvrak.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Ana evrak bilgilerini dictionary'e ekle
                                evrakDetay["evrakNo"] = SafeGetString(reader, "Evrak_No");
                                evrakDetay["tarih"] = reader["Tarih"] != DBNull.Value
                                    ? ((DateTime)reader["Tarih"]).ToString("yyyy-MM-dd")
                                    : "";
                                evrakDetay["servisMerkezi"] = SafeGetString(reader, "Servis_Merkezi");
                                evrakDetay["no"] = SafeGetString(reader, "No");
                                evrakDetay["musteriAdi"] = SafeGetString(reader, "Musteri_Adi");
                                evrakDetay["soforAdi"] = SafeGetString(reader, "Sofor_Adi");
                                evrakDetay["soforTelefon"] = SafeGetString(reader, "Sofor_Telefon");
                                evrakDetay["plakaNo"] = SafeGetString(reader, "Plaka_No");
                                evrakDetay["cihazMarkaModel"] = SafeGetString(reader, "Cihaz_Marka_Model");
                                evrakDetay["teknisyenAdi"] = SafeGetString(reader, "Teknisyen_Adi");
                                evrakDetay["calismaSaati"] = SafeGetString(reader, "Calisma_Saati");
                                evrakDetay["serviseGirisTarihi"] = reader["Servise_Giris_Tarihi"] != DBNull.Value
                                    ? ((DateTime)reader["Servise_Giris_Tarihi"]).ToString("yyyy-MM-dd")
                                    : "";
                                evrakDetay["isTuru"] = SafeGetString(reader, "is_turu");
                                evrakDetay["ilkGozlem"] = SafeGetString(reader, "Ilk_Gozlem");
                                evrakDetay["aToplam"] = SafeGetDecimal(reader, "A_Toplam");
                                evrakDetay["vergi"] = SafeGetDecimal(reader, "Vergi");
                                evrakDetay["gToplam"] = SafeGetDecimal(reader, "G_Toplam");
                                evrakDetay["birimFiyat"] = SafeGetString(reader, "Birim_Fiyat");
                                evrakDetay["tutar"] = SafeGetString(reader, "Tutar");
                                evrakDetay["iscilikTutari"] = SafeGetString(reader, "iscilik_tutari");
                                evrakDetay["hariciIscilikTutari"] = SafeGetString(reader, "harici_iscilik_tutari");
                            }
                            else
                            {
                                // Ana kayıt bulunamadıysa hata döndür
                                return Json(new
                                {
                                    success = false,
                                    message = $"Evrak No {evrakNo} için kayıt bulunamadı."
                                });
                            }
                        }
                    }

                    // Detay kayıtlarını çeken SQL sorgusu
                    // Detay kayıtlarını çeken SQL sorgusu
                    string queryDetay = @"
SELECT 
    Evrak_No,
    Evrak_Sira_No,
    Yedek_Parca_No, 
    Yedek_Parca_Adi,
    Servis_Merkezi, 
    Adet, 
    Birim_Fiyat, 
    Tutar, 
    iscilik_tutari, 
    harici_iscilik_tutari
FROM ServisHareketleri 
WHERE Evrak_No = @EvrakNo 
AND Servis_Merkezi = @ServisMerkezi
ORDER BY Evrak_Sira_No";

                    // Hata ayıklama için log ekleyelim
                    using (SqlCommand cmdDetay = new SqlCommand(queryDetay, con))
                    {
                        cmdDetay.Parameters.AddWithValue("@EvrakNo", evrakNo);
                        cmdDetay.Parameters.AddWithValue("@ServisMerkezi", evrakDetay["servisMerkezi"]); // Ana evrak bilgileri içinde bulunan servis merkezi

                        using (SqlDataReader reader = cmdDetay.ExecuteReader())
                        {
                            var stokHizmetListesi = new List<Dictionary<string, string>>();
                            while (reader.Read())
                            {
                                stokHizmetListesi.Add(new Dictionary<string, string>
                                {
                                    ["evrakNo"] = SafeGetString(reader, "Evrak_No"),
                                    ["evrakSiraNo"] = SafeGetString(reader, "Evrak_Sira_No"),
                                    ["yedekParcaNo"] = SafeGetString(reader, "Yedek_Parca_No"),
                                    ["yedekParcaAdi"] = SafeGetString(reader, "Yedek_Parca_Adi"),
                                    ["servisMerkezi"] = SafeGetString(reader, "Servis_Merkezi"),
                                    ["adet"] = SafeGetString(reader, "Adet"),
                                    ["birimFiyat"] = SafeGetString(reader, "Birim_Fiyat"),
                                    ["tutar"] = SafeGetString(reader, "Tutar"),
                                    ["iscilikTutari"] = SafeGetString(reader, "iscilik_tutari"),
                                    ["hariciIscilikTutari"] = SafeGetString(reader, "harici_iscilik_tutari")
                                });
                            }

                            // Detay kayıtları varsa ekle
                            if (stokHizmetListesi.Any())
                            {
                                evrakDetay["stokHizmetListesi"] = stokHizmetListesi;
                            }
                            else
                            {
                                // Detay kayıt bulunamadıysa boş liste gönder
                                evrakDetay["stokHizmetListesi"] = new List<Dictionary<string, string>>();
                            }
                        }
                    }
                }

                // Başarılı veri çekme durumunda JSON olarak dön
                return Json(evrakDetay);
            }
            catch (Exception ex)
            {
                // Hata durumunda detaylı hata mesajı dön
                return Json(new
                {
                    success = false,
                    message = "Evrak bilgileri alınamadı: " + ex.Message
                });
            }
        }


        // Güvenli string alma metodu
        private string SafeGetString(SqlDataReader reader, string columnName)
        {
            int columnIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(columnIndex) ? "" : reader.GetValue(columnIndex).ToString().Trim();
        }

        // Güvenli decimal alma metodu
        private string SafeGetDecimal(SqlDataReader reader, string columnName)
        {
            int columnIndex = reader.GetOrdinal(columnName);
            return reader.IsDBNull(columnIndex) ? "0" : Math.Round(reader.GetDecimal(columnIndex), 2).ToString();
        }




        [HttpPost]
        public IActionResult UpdateEvrak([FromBody] EvrakUpdateDTO updateData)
        {
            if (updateData == null || updateData.model == null || updateData.stokHizmet == null)
            {
                return Json(new { success = false, message = "Geçersiz veri formatı." });
            }

            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // Ana evrak bilgilerini güncelle
                        string queryUpdateEvrak = @"
                    UPDATE ServisHareketleri 
                    SET 
                        Musteri_Adi = @MusteriAdi, 
                        Sofor_Adi = @SoforAdi,
                        Sofor_Telefon = @SoforTelefon,
                        Plaka_No = @PlakaNo, 
                        Cihaz_Marka_Model = @CihazMarkaModel, 
                        Teknisyen_Adi = @TeknisyenAdi,
                        Calisma_Saati = @CalismaSaati, 
                        Servise_Giris_Tarihi = @ServiseGirisTarihi, 
                        is_turu = @IsTuru,
                        Ilk_Gozlem = @IlkGozlem
                    WHERE Evrak_No = @EvrakNo AND Evrak_Sira_No = 1";

                        using (SqlCommand cmdUpdateEvrak = new SqlCommand(queryUpdateEvrak, con, transaction))
                        {
                            cmdUpdateEvrak.Parameters.AddWithValue("@MusteriAdi", updateData.model.MusteriAdi);
                            cmdUpdateEvrak.Parameters.AddWithValue("@SoforAdi", updateData.model.SoforAdi ?? (object)DBNull.Value);
                            cmdUpdateEvrak.Parameters.AddWithValue("@SoforTelefon", updateData.model.SoforTelefon ?? (object)DBNull.Value);
                            cmdUpdateEvrak.Parameters.AddWithValue("@PlakaNo", updateData.model.PlakaNo);
                            cmdUpdateEvrak.Parameters.AddWithValue("@CihazMarkaModel", updateData.model.CihazMarkaModel);
                            cmdUpdateEvrak.Parameters.AddWithValue("@TeknisyenAdi", updateData.model.TeknisyenAdi ?? (object)DBNull.Value);
                            cmdUpdateEvrak.Parameters.AddWithValue("@CalismaSaati", updateData.model.CalismaSaati);
                            cmdUpdateEvrak.Parameters.AddWithValue("@ServiseGirisTarihi", updateData.model.ServiseGirisTarihi);
                            cmdUpdateEvrak.Parameters.AddWithValue("@IsTuru", updateData.model.IsTuru ?? (object)DBNull.Value);
                            cmdUpdateEvrak.Parameters.AddWithValue("@IlkGozlem", updateData.model.IlkGozlem ?? (object)DBNull.Value);
                            cmdUpdateEvrak.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);

                            cmdUpdateEvrak.ExecuteNonQuery();
                        }

                        // Detay kayıtlarını temizle ve tekrar ekle
                        string queryDeleteDetay = @"
                    DELETE FROM ServisHareketleri 
                    WHERE Evrak_No = @EvrakNo AND Evrak_Sira_No > 1";

                        using (SqlCommand cmdDeleteDetay = new SqlCommand(queryDeleteDetay, con, transaction))
                        {
                            cmdDeleteDetay.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);
                            cmdDeleteDetay.ExecuteNonQuery();
                        }

                        // Yeni detayları ekle ve hesaplamaları yap
                        decimal toplamTutar = 0;
                        int evrakSiraNo = 2; // İlk satır zaten var, sonraki satırlar 2'den başlasın

                        foreach (var item in updateData.stokHizmet)
                        {
                            // Her satır için ayrı hesaplama
                            decimal parcaTutari = item.Adet * item.BirimFiyat;
                            decimal iscilikTutari = item.IscilikTutari ?? 0;
                            decimal hariciIscilikTutari = item.HariciIscilikTutari ?? 0;
                            decimal toplamSatirTutari = parcaTutari + iscilikTutari + hariciIscilikTutari;

                            toplamTutar += toplamSatirTutari;

                            string queryInsertDetay = @"
                    INSERT INTO ServisHareketleri (
                        Evrak_No, Evrak_Sira_No, Tarih, Musteri_Adi, Plaka_No, 
                        Cihaz_Marka_Model, Calisma_Saati, Servise_Giris_Tarihi, 
                        Ilk_Gozlem, Yedek_Parca_No, Yedek_Parca_Adi, 
                        Adet, Birim_Fiyat, Tutar, 
                        iscilik_tutari, harici_iscilik_tutari,
                        A_Toplam, Vergi, G_Toplam, Servis_Merkezi, Durum
                    ) VALUES (
                        @EvrakNo, @EvrakSiraNo, @Tarih, @MusteriAdi, @PlakaNo, 
                        @CihazMarkaModel, @CalismaSaati, @ServiseGirisTarihi, 
                        @IlkGozlem, @YedekParcaNo, @YedekParcaAdi, 
                        @Adet, @BirimFiyat, @Tutar,
                        @IscilikTutari, @HariciIscilikTutari,
                        @AToplam, @Vergi, @GToplam, @ServisMerkezi, @Durum
                    )";

                            using (SqlCommand cmdInsertDetay = new SqlCommand(queryInsertDetay, con, transaction))
                            {
                                cmdInsertDetay.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);
                                cmdInsertDetay.Parameters.AddWithValue("@EvrakSiraNo", evrakSiraNo);
                                cmdInsertDetay.Parameters.AddWithValue("@Tarih", updateData.model.Tarih);
                                cmdInsertDetay.Parameters.AddWithValue("@MusteriAdi", updateData.model.MusteriAdi);
                                cmdInsertDetay.Parameters.AddWithValue("@PlakaNo", updateData.model.PlakaNo);
                                cmdInsertDetay.Parameters.AddWithValue("@CihazMarkaModel", updateData.model.CihazMarkaModel);
                                cmdInsertDetay.Parameters.AddWithValue("@CalismaSaati", updateData.model.CalismaSaati);
                                cmdInsertDetay.Parameters.AddWithValue("@ServiseGirisTarihi", updateData.model.ServiseGirisTarihi);
                                cmdInsertDetay.Parameters.AddWithValue("@IlkGozlem", updateData.model.IlkGozlem ?? (object)DBNull.Value);
                                cmdInsertDetay.Parameters.AddWithValue("@YedekParcaNo", item.YedekParcaNo ?? (object)DBNull.Value);
                                cmdInsertDetay.Parameters.AddWithValue("@YedekParcaAdi", item.YedekParcaAdi ?? (object)DBNull.Value);
                                cmdInsertDetay.Parameters.AddWithValue("@Adet", item.Adet);
                                cmdInsertDetay.Parameters.AddWithValue("@BirimFiyat", item.BirimFiyat);
                                cmdInsertDetay.Parameters.AddWithValue("@Tutar", toplamSatirTutari);
                                cmdInsertDetay.Parameters.AddWithValue("@IscilikTutari", iscilikTutari);
                                cmdInsertDetay.Parameters.AddWithValue("@HariciIscilikTutari", hariciIscilikTutari);
                                cmdInsertDetay.Parameters.AddWithValue("@AToplam", toplamTutar);
                                cmdInsertDetay.Parameters.AddWithValue("@Vergi", toplamTutar * 0.20M);
                                cmdInsertDetay.Parameters.AddWithValue("@GToplam", toplamTutar * 1.20M);
                                cmdInsertDetay.Parameters.AddWithValue("@ServisMerkezi", updateData.model.Servis_Merkezi);
                                cmdInsertDetay.Parameters.AddWithValue("@Durum", 1); // Yeni eklenen satırlar aktif olarak işaretlenir

                                cmdInsertDetay.ExecuteNonQuery();
                            }

                            evrakSiraNo++;
                        }

                        // Ana kayıttaki toplam değerleri güncelle
                        string queryUpdateToplam = @"
                    UPDATE ServisHareketleri 
                    SET 
                        A_Toplam = @AToplam, 
                        Vergi = @Vergi, 
                        G_Toplam = @GToplam
                    WHERE Evrak_No = @EvrakNo AND Evrak_Sira_No = 1";

                        using (SqlCommand cmdUpdateToplam = new SqlCommand(queryUpdateToplam, con, transaction))
                        {
                            cmdUpdateToplam.Parameters.AddWithValue("@AToplam", toplamTutar);
                            cmdUpdateToplam.Parameters.AddWithValue("@Vergi", toplamTutar * 0.20M);
                            cmdUpdateToplam.Parameters.AddWithValue("@GToplam", toplamTutar * 1.20M);
                            cmdUpdateToplam.Parameters.AddWithValue("@EvrakNo", updateData.model.EvrakNo);

                            cmdUpdateToplam.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return Json(new { success = true });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new
                        {
                            success = false,
                            message = "Güncelleme sırasında hata oluştu: " + ex.Message
                        });
                    }
                }
            }
        }

        // JSON veri transfer nesnesi






        [HttpGet]
        public IActionResult AllIsEmri(string servisMerkezi, int? evrakNo)
        {
            int nextNo = 1;
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            // Evrak No için sorgu
            string evrakNoQuery = @"SELECT ISNULL(MAX(Evrak_No), 0) + 1 FROM ServisHareketleri";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(evrakNoQuery, con);
                con.Open();
                nextNo = (int)cmd.ExecuteScalar();
                con.Close();
            }

            List<IsEmirleri> servisHareketleri = new List<IsEmirleri>();

            // Dinamik sorgu
            string selectQuery = @"
     SELECT Evrak_No, Evrak_Sira_No, Musteri_Adi, Plaka_No, Tarih, Cihaz_Marka_Model, 
           Calisma_Saati, Yedek_Parca_No, Yedek_Parca_Adi, Adet, Birim_Fiyat, Tutar, 
           A_Toplam, Vergi, G_Toplam, Durum, Servis_Merkezi
    FROM ServisHareketleri
    WHERE 1=1"; // Bu satır sorgu koşullarını kolayca ekleyebilmek için

            // Koşulları dinamik olarak ekle
            if (!string.IsNullOrEmpty(servisMerkezi))
            {
                selectQuery += " AND Servis_Merkezi = @ServisMerkezi";
            }
            if (evrakNo.HasValue)
            {
                selectQuery += " AND Evrak_No = @EvrakNo";
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(selectQuery, con);

                // Parametreleri ekle
                if (!string.IsNullOrEmpty(servisMerkezi))
                {
                    cmd.Parameters.AddWithValue("@ServisMerkezi", servisMerkezi);
                }
                if (evrakNo.HasValue)
                {
                    cmd.Parameters.AddWithValue("@EvrakNo", evrakNo.Value);
                }

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    servisHareketleri.Add(new IsEmirleri
                    {
                        EvrakNo = (int)reader["Evrak_No"],
                        EvrakSiraNo = (int)reader["Evrak_Sira_No"],
                        MusteriAdi = reader["Musteri_Adi"].ToString(),
                        PlakaNo = reader["Plaka_No"].ToString(),
                        Tarih = (DateTime)reader["Tarih"],
                        CihazMarkaModel = reader["Cihaz_Marka_Model"].ToString(),
                        CalismaSaati = (int)reader["Calisma_Saati"],
                        YedekParcaNo = reader["Yedek_Parca_No"].ToString(),
                        YedekParcaAdi = reader["Yedek_Parca_Adi"].ToString(),
                        Adet = (int)reader["Adet"],
                        BirimFiyat = (decimal)reader["Birim_Fiyat"],
                        Tutar = (decimal)reader["Tutar"],
                        AToplam = (decimal)reader["A_Toplam"],
                        Vergi = (decimal)reader["Vergi"],
                        GToplam = (decimal)reader["G_Toplam"],
                        Durum = (int)reader["Durum"],
                        Servis_Merkezi = reader["Servis_Merkezi"].ToString(),
                    });
                }
                con.Close();
            }

            var servisHareketGruplari = servisHareketleri
                .GroupBy(h => new { h.EvrakNo, h.Servis_Merkezi })
                .ToList();
            ViewBag.ServisHareketGruplari = servisHareketGruplari;

            var model = new IsEmirleri
            {
                No = nextNo,
                Tarih = DateTime.Now
            };

            return View(model);
        }



        [HttpGet]
        public IActionResult IsEmriGiris(string servisMerkezi)
        {
            int nextNo = 1;
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            string evrakNoQuery = @"SELECT ISNULL(MAX(Evrak_No), 0) + 1 FROM ServisHareketleri";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(evrakNoQuery, con);
                con.Open();
                nextNo = (int)cmd.ExecuteScalar();
                con.Close();
            }

            List<IsEmirleri> servisHareketleri = new List<IsEmirleri>();

            // Dinamik sorgu
            string selectQuery = @"
 SELECT TOP 10 Evrak_No, Evrak_Sira_No, Musteri_Adi, Plaka_No, Tarih, Cihaz_Marka_Model, 
    Calisma_Saati, Yedek_Parca_No, Yedek_Parca_Adi, Adet, Birim_Fiyat, Tutar, 
    A_Toplam, Vergi, G_Toplam, Durum, Servis_Merkezi
FROM ServisHareketleri";

            if (!string.IsNullOrEmpty(servisMerkezi))
            {
                selectQuery += " WHERE Servis_Merkezi = @ServisMerkezi";
            }

            // Tek bir ORDER BY ifadesi bırakılıyor.
            selectQuery += " ORDER BY  Tarih DESC;";



            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(selectQuery, con);
                if (!string.IsNullOrEmpty(servisMerkezi))
                {
                    cmd.Parameters.AddWithValue("@ServisMerkezi", servisMerkezi);
                }
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    servisHareketleri.Add(new IsEmirleri
                    {
                        EvrakNo = (int)reader["Evrak_No"],
                        EvrakSiraNo = (int)reader["Evrak_Sira_No"],
                        MusteriAdi = reader["Musteri_Adi"].ToString(),
                        PlakaNo = reader["Plaka_No"].ToString(),
                        Tarih = (DateTime)reader["Tarih"],
                        CihazMarkaModel = reader["Cihaz_Marka_Model"].ToString(),
                        CalismaSaati = (int)reader["Calisma_Saati"],
                        YedekParcaNo = reader["Yedek_Parca_No"].ToString(),
                        YedekParcaAdi = reader["Yedek_Parca_Adi"].ToString(),
                        Adet = (int)reader["Adet"],
                        BirimFiyat = (decimal)reader["Birim_Fiyat"],
                        Tutar = (decimal)reader["Tutar"],
                        AToplam = (decimal)reader["A_Toplam"],
                        Vergi = (decimal)reader["Vergi"],
                        GToplam = (decimal)reader["G_Toplam"],
                        Durum = (int)reader["Durum"],
                        Servis_Merkezi = reader["Servis_Merkezi"].ToString(),
                    });
                }
                con.Close();
            }

            var servisHareketGruplari = servisHareketleri
                .GroupBy(h => new { h.EvrakNo, h.Servis_Merkezi })
                .ToList();

            ViewBag.ServisHareketGruplari = servisHareketGruplari;

            // İş Merkezleri için Dropdown Listesi
            ViewBag.ServisMerkezleri = servisHareketleri
                .Select(h => h.Servis_Merkezi)
                .Distinct()
                .ToList();

            var model = new IsEmirleri
            {
                No = nextNo,
                Tarih = DateTime.Now
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult IsEmriGiris(IsEmirleri model, List<StokHizmet> stokHizmet)
        {
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            // Yeni Evrak No'yu Servis Merkezi bazında hesapla
            int evrakNo = 1;
            string queryEvrakNo = @"SELECT ISNULL(MAX(Evrak_No), 0) + 1 
                    FROM ServisHareketleri 
                    WHERE Servis_Merkezi = @ServisMerkezi";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(queryEvrakNo, con))
                {
                    cmd.Parameters.AddWithValue("@ServisMerkezi", model.Servis_Merkezi);
                    evrakNo = (int)cmd.ExecuteScalar();
                }

                // A Toplamı, Vergiyi ve G Toplamı hesapla
                decimal aToplam = 0;
                foreach (var item in stokHizmet)
                {
                    if (item.Adet <= 0 || item.BirimFiyat <= 0)
                        continue;

                    // Parça tutarı + İşçilik tutarları
                    decimal parcaTutari = item.Adet * item.BirimFiyat;
                    decimal iscilikTutari = item.IscilikTutari ?? 0;
                    decimal hariciIscilikTutari = item.HariciIscilikTutari ?? 0;

                    // Toplam tutar hesaplama
                    item.Tutar = parcaTutari + iscilikTutari + hariciIscilikTutari;
                    aToplam += item.Tutar;
                }

                decimal vergi = aToplam * 0.20M; // %20 vergi
                decimal gToplam = aToplam + vergi;

                // Evrak Sıra No'yu bul
                int evrakSiraNo = 1;
                string queryEvrakSiraNo = @"SELECT ISNULL(MAX(Evrak_Sira_No), 0) + 1 
                            FROM ServisHareketleri 
                            WHERE Evrak_No = @EvrakNo AND Servis_Merkezi = @ServisMerkezi";

                foreach (var item in stokHizmet)
                {
                    if (string.IsNullOrWhiteSpace(item.YedekParcaNo) || string.IsNullOrWhiteSpace(item.YedekParcaAdi) ||
                        item.Adet <= 0 || item.BirimFiyat <= 0)
                    {
                        continue;
                    }

                    using (SqlCommand cmd = new SqlCommand(queryEvrakSiraNo, con))
                    {
                        cmd.Parameters.AddWithValue("@EvrakNo", evrakNo);
                        cmd.Parameters.AddWithValue("@ServisMerkezi", model.Servis_Merkezi);
                        evrakSiraNo = (int)cmd.ExecuteScalar();
                    }

                    string queryInsert = @"
        INSERT INTO ServisHareketleri (
            Evrak_No, Servis_Merkezi, Evrak_Sira_No, Tarih, Musteri_Adi, Plaka_No, 
            Cihaz_Marka_Model, Calisma_Saati, Servise_Giris_Tarihi, Ilk_Gozlem,
            Yedek_Parca_No, Yedek_Parca_Adi, Adet, Birim_Fiyat, Tutar, A_Toplam, 
            Vergi, G_Toplam, Sofor_Adi, Sofor_Telefon, Teknisyen_Adi, is_turu,
            iscilik_tutari, harici_iscilik_tutari
        )
        VALUES (
            @EvrakNo, @ServisMerkezi, @EvrakSiraNo, @Tarih, @MusteriAdi, @PlakaNo, 
            @CihazMarkaModel, @CalismaSaati, @ServiseGirisTarihi, @IlkGozlem, 
            @YedekParcaNo, @YedekParcaAdi, @Adet, @BirimFiyat, @Tutar, @AToplam, 
            @Vergi, @GToplam, @SoforAdi, @SoforTelefon, @TeknisyenAdi, @IsTuru,
            @IscilikTutari, @HariciIscilikTutari
        )";

                    using (SqlCommand cmdInsert = new SqlCommand(queryInsert, con))
                    {
                        cmdInsert.Parameters.AddWithValue("@EvrakNo", evrakNo);
                        cmdInsert.Parameters.AddWithValue("@ServisMerkezi", model.Servis_Merkezi);
                        cmdInsert.Parameters.AddWithValue("@EvrakSiraNo", evrakSiraNo);
                        cmdInsert.Parameters.AddWithValue("@Tarih", model.Tarih);
                        cmdInsert.Parameters.AddWithValue("@MusteriAdi", model.MusteriAdi);
                        cmdInsert.Parameters.AddWithValue("@PlakaNo", model.PlakaNo);
                        cmdInsert.Parameters.AddWithValue("@CihazMarkaModel", model.CihazMarkaModel);
                        cmdInsert.Parameters.AddWithValue("@CalismaSaati", model.CalismaSaati);
                        cmdInsert.Parameters.AddWithValue("@ServiseGirisTarihi", model.ServiseGirisTarihi);
                        cmdInsert.Parameters.AddWithValue("@IlkGozlem", model.IlkGozlem ?? (object)DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@YedekParcaNo", item.YedekParcaNo);
                        cmdInsert.Parameters.AddWithValue("@YedekParcaAdi", item.YedekParcaAdi);
                        cmdInsert.Parameters.AddWithValue("@Adet", item.Adet);
                        cmdInsert.Parameters.AddWithValue("@BirimFiyat", item.BirimFiyat);
                        cmdInsert.Parameters.AddWithValue("@Tutar", item.Tutar);
                        cmdInsert.Parameters.AddWithValue("@AToplam", aToplam);
                        cmdInsert.Parameters.AddWithValue("@Vergi", vergi);
                        cmdInsert.Parameters.AddWithValue("@GToplam", gToplam);
                        cmdInsert.Parameters.AddWithValue("@SoforAdi", model.SoforAdi ?? (object)DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@SoforTelefon", model.SoforTelefon ?? (object)DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@TeknisyenAdi", model.TeknisyenAdi ?? (object)DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@IsTuru", model.IsTuru ?? (object)DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@IscilikTutari", (object)item.IscilikTutari ?? DBNull.Value);
                        cmdInsert.Parameters.AddWithValue("@HariciIscilikTutari", (object)item.HariciIscilikTutari ?? DBNull.Value);

                        cmdInsert.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToAction("IsEmriGiris");
        }





        [HttpPost]
        public JsonResult RedEvrak([FromBody] int evrakNo)
        {
            string connectionString = _configuration.GetConnectionString("ERPDatabase");

            try
            {
                string queryUpdate = "UPDATE ServisHareketleri SET Durum = 0 WHERE Evrak_No = @EvrakNo";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(queryUpdate, con);
                    cmd.Parameters.AddWithValue("@EvrakNo", evrakNo);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    con.Close();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Evrak No bulunamadı veya güncellenmedi." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class RedSatirRequest
        {
            public int EvrakNo { get; set; }
            public int EvrakSiraNo { get; set; }
        }

        [HttpPost]
        public JsonResult RedSatir([FromBody] RedSatirRequest request)
        {
            string connectionString = _configuration.GetConnectionString("ERPDatabase");
            int evrakNo = request.EvrakNo;
            int evrakSiraNo = request.EvrakSiraNo;

            try
            {
                string queryUpdate = "UPDATE ServisHareketleri SET Durum = 0 WHERE Evrak_No = @EvrakNo AND Evrak_Sira_No = @EvrakSiraNo";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(queryUpdate, con);
                    cmd.Parameters.AddWithValue("@EvrakNo", evrakNo);
                    cmd.Parameters.AddWithValue("@EvrakSiraNo", evrakSiraNo);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    con.Close();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Evrak ve Satır No bulunamadı veya güncellenmedi." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}