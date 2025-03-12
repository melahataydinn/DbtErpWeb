using Microsoft.Data.SqlClient; // SqlConnection için yeni ad alanı
using System.Data;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Collections.Generic;
using Microsoft.Extensions.Logging; // ILogger ekleyin
using Deneme_proje.Models;
using static Deneme_proje.Models.Entities;
using static Deneme_proje.Models.SirketDurumuEntites;
namespace Deneme_proje.Repository
{
    public class FaturaRepository
    {
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<FaturaRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FaturaRepository(
            DatabaseSelectorService dbSelectorService,
            ILogger<FaturaRepository> logger,
            IHttpContextAccessor httpContextAccessor) // Constructor'a ekleme
        {
            _dbSelectorService = dbSelectorService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public IEnumerable<Deneme_proje.Models.Entities.FaturaViewModel> GetFaturaData(string cariUnvani, float ticariFaiz)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
SELECT 
    cha_evrakno_sira AS [EvrakNo],
    cari_unvan1 AS [CariUnvani],
    cari_kod AS [CariKodu],
   cha_tarihi AS [FaturaTarihi],

    CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(cha_tarihi, GETDATE())) AS FLOAT) AS [FaturaTarihiSayi],
    chk_Alacakvade AS [AlacakVade],
    chk_BorcVade AS [FaturaVadeTarihi],
    (CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_BorcVade, GETDATE())) AS FLOAT) - 
     CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(cha_tarihi, GETDATE())) AS FLOAT)) AS [FaturaVadesi],
    cha_meblag AS [FaturaTutari],
    chk_Tutar AS [TaksitTutar],
    CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_Alacakvade, GETDATE())) AS FLOAT) AS [AlacakVadeTarihiSayi],
    66.24 AS [FaizOrani],
    (ISNULL(chk_Tutar, 0) * CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_Alacakvade, GETDATE())) AS FLOAT)) AS [BorcTutari]
FROM 
    CARI_HAREKET_BORC_ALACAK_ESLEME 
LEFT JOIN 
    CARI_HESAPLAR ON chk_ChKodu = cari_kod
LEFT JOIN 
    CARI_HESAP_HAREKETLERI ON chk_Borc_uid = cha_Guid
	WHERE 
    cari_kod LIKE '120%' AND  cha_meblag IS NOT NULL AND cha_tarihi IS NOT NULL AND cha_evrak_tip IN (29, 63)
";

                // Eğer cariUnvani boş değilse, LIKE ifadesi ile kısmi eşleşme yapın
                if (!string.IsNullOrEmpty(cariUnvani))
                {
                    query += " WHERE cari_unvan1 LIKE @CariUnvani";
                }

                var parameters = new { CariUnvani = "%" + cariUnvani + "%" };

                try
                {
                    var results = connection.Query<Deneme_proje.Models.Entities.FaturaViewModel>(query, parameters).ToList();
                    _logger.LogInformation($"Results Count: {results.Count}");

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hata oluştu");
                    throw;
                }
            }
        }

        public IEnumerable<TedarikciFaturaViewModel> GetTedarikciFaturaData(string cariUnvani, float ticariFaiz)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
SELECT 
    cha_evrakno_sira AS [EvrakNo],
    cha_kod AS [CariKodu],
    cari_unvan1 AS [CariUnvani],
    cha_tarihi AS [FaturaTarihi],
    CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(cha_tarihi, GETDATE())) AS FLOAT) AS [FaturaTarihiSayi],
    chk_BorcVade AS [BorcVade],
    chk_Alacakvade AS [FaturaVadeTarihi],
    (CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_Alacakvade, GETDATE())) AS FLOAT) - 
     CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(cha_tarihi, GETDATE())) AS FLOAT)) AS [FaturaVadesi],
    cha_meblag AS [FaturaTutari],
    chk_Tutar AS [OdemeTutar],
    CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_BorcVade, GETDATE())) AS FLOAT) AS [BorcVadeTarihiSayi],
    66.24 AS [FaizOrani],
    (ISNULL(chk_Tutar, 0) * CAST(DATEDIFF(DAY, '1899-12-30', ISNULL(chk_BorcVade, GETDATE())) AS FLOAT)) AS [BorcTutari]
FROM 
    CARI_HAREKET_BORC_ALACAK_ESLEME 
LEFT JOIN 
    CARI_HESAPLAR ON chk_ChKodu = cari_kod
LEFT JOIN 
    CARI_HESAP_HAREKETLERI ON chk_Alc_uid = cha_Guid
	WHERE 
    cari_kod LIKE '320%' AND  cha_meblag IS NOT NULL AND cha_tarihi IS NOT NULL  AND cha_evrak_tip IN (29, 0)

";

                // Eğer cariUnvani boş değilse, LIKE ifadesi ile kısmi eşleşme yapın
                if (!string.IsNullOrEmpty(cariUnvani))
                {
                    query += " WHERE cari_unvan1 LIKE @CariUnvani";
                }

                var parameters = new { CariUnvani = "%" + cariUnvani + "%" };

                try
                {
                    var results = connection.Query<TedarikciFaturaViewModel>(query, parameters).ToList();
                    _logger.LogInformation($"Results Count: {results.Count}");

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hata oluştu");
                    throw;
                }
            }
        }

        public IEnumerable<KrediDetayViewModel> GetKrediDetayData()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                const string procedureName = "DBT_Finans_KrediDetayTL";

                try
                {
                    var results = connection.Query<KrediDetayViewModel>(
                        procedureName,
                        commandType: CommandType.StoredProcedure).ToList();

                    _logger.LogInformation($"Results Count: {results.Count}");
                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hata oluştu");
                    throw;
                }
            }
        }

        public IEnumerable<KrediDetayModel> GetKrediDetay()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                const string procedureName = "DBT_Finans_KrediDetay_AyBazli";

                try
                {
                    connection.Open();
                    var results = connection.Query<KrediDetayModel>(
                        procedureName,
                        commandType: CommandType.StoredProcedure).ToList();

                    _logger.LogInformation($"Results Count: {results.Count}");

                    foreach (var result in results)
                    {
                        _logger.LogInformation($"Yıl: {result.Yıl}, Ay: {result.Ay}, VadeTarihi: {result.VadeTarihi}, TaksitAnapara: {result.TaksitAnapara}, TaksitFaiz: {result.TaksitFaiz}, TaksitBSMV: {result.TaksitBSMV}");
                    }

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hata oluştu");
                    throw;
                }
            }
        }





        public IEnumerable<KrediDetay> GetKrediDetayListesi()
        {
            var krediDetayListesi = new List<KrediDetay>();

            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("dbo.DBT_Finans_KrediDetayTL", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            krediDetayListesi.Add(new KrediDetay
                            {
                                Banka = reader["Banka"].ToString(),
                                AnaPara = reader.IsDBNull(reader.GetOrdinal("AnaPara"))
                                          ? 0
                                          : Convert.ToDecimal(reader["AnaPara"]),
                                //Aokf = reader.IsDBNull(reader.GetOrdinal("Aokf"))
                                //       ? 0
                                //       : Convert.ToDecimal(reader["Aokf"])
                            });
                        }
                    }
                }
            }

            return krediDetayListesi;
        }
        public IEnumerable<KrediDetayi> GetKrediDetayListByBankCode(string bankCode)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sql = @"
        SELECT 
            krsoztaksit_sozkodu, 
            krsoztaksit_vade, 
            krsoztaksit_taksit, 
            krsoztaksit_anapara, 
            krsoztaksit_faiz, 
            krsoztaksit_bsmv,
            krsoz_sozbankakodu,
            ban_ismi,
            krsoztaksit_faizorani,
            krsoztaksit_kalananapara 
        FROM KREDI_SOZLESMESI_TANIMLARI
        LEFT JOIN Bankalar ON ban_kod = krsoz_sozbankakodu
        LEFT JOIN KREDI_SOZLESMESI_TAKSIT_TANIMLARI ON krsoz_kodu = krsoztaksit_sozkodu
        WHERE krsoz_sozbankakodu = @BankCode";

                var krediDetayListesi = connection.Query<KrediDetayi>(sql, new { BankCode = bankCode }).ToList();

                return krediDetayListesi;
            }
        }



        public IEnumerable<IGrouping<string, KrediDetayi>> GetKrediDetayList()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sql = @"
SELECT 
    krsoztaksit_sozkodu, 
    krsoztaksit_vade, 
    krsoztaksit_taksit, 
    krsoztaksit_anapara, 
    krsoztaksit_odenen_ana, 
    krsoztaksit_faiz, 
    krsoztaksit_bsmv,
    krsoz_sozbankakodu,
    ban_ismi,
    krsoztaksit_odemeevraksira,
    krsoztaksit_faizorani,
    (krsoztaksit_taksit - krsoztaksit_odenen_ana) AS kalan -- Kalan anapara hesaplaması
FROM KREDI_SOZLESMESI_TANIMLARI
LEFT JOIN Bankalar ON ban_kod = krsoz_sozbankakodu
LEFT JOIN KREDI_SOZLESMESI_TAKSIT_TANIMLARI ON krsoz_kodu = krsoztaksit_sozkodu
WHERE   (krsoztaksit_taksit - krsoztaksit_odenen_ana)  > 10 -- Filter for anapara greater than 10
";

                var krediDetayListesi = connection.Query<KrediDetayi>(sql).ToList();

                // Group by bank name (ban_ismi) instead of bank code
                var groupedData = krediDetayListesi
                    .GroupBy(x => x.ban_ismi) // Grouping by Bank Name
                    .ToList();

                return groupedData;
            }
        }



        public IEnumerable<CariBakiyeYaslandirma> GetCariMusteriYaslandirma(string cariIlkKod, string cariSonKod, string cariKodYapisi, DateTime? raporTarihi, byte hangiHesaplar)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("dbo.DBT_sp_Cari_Musteri_Yaslandirma_WEB", connection))
                {
                    command.CommandTimeout = 120;
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@CariIlkKod", cariIlkKod);
                    command.Parameters.AddWithValue("@CariSonKod", cariSonKod);
                    command.Parameters.AddWithValue("@CariKodYapisi", cariKodYapisi);
                    command.Parameters.AddWithValue("@RaporTarihi", raporTarihi.HasValue ? (object)raporTarihi.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@HangiHesaplar", hangiHesaplar);

                    using (var reader = command.ExecuteReader())
                    {
                        var result = new List<CariBakiyeYaslandirma>();
                        while (reader.Read())
                        {
                            result.Add(new CariBakiyeYaslandirma
                            {
                                MusteriKodu = reader.GetString(0),
                                Unvan = reader.GetString(1),
                                VadesiGecenBakiye = reader.GetDouble(2),
                                VadesiGecmisBakiye = reader.GetDouble(3),
                                ToplamBakiye = reader.GetDouble(4),
                                BakiyeTipi = reader.GetString(5),
                                Gun30 = reader.GetDouble(6),
                                Gun60 = reader.GetDouble(7),
                                Gun90 = reader.GetDouble(8),
                                Gun120 = reader.GetDouble(9),
                                gecmisGun30 = reader.GetDouble(10),
                                gecmisGun60 = reader.GetDouble(11),
                                gecmisGun90 = reader.GetDouble(12),
                                gecmisGun120 = reader.GetDouble(13),
                                GunUstu120 = reader.GetDouble(14),
                                eksi120GunUstu = reader.GetDouble(reader.GetOrdinal("eksi120GunUstu"))

                            });
                        }
                        return result;
                    }
                }
            }

        }

        public IEnumerable<CariBakiyeYaslandirma> GetCariTedarikciYaslandirma(string cariIlkKod, string cariSonKod, string cariKodYapisi, DateTime? raporTarihi, byte hangiHesaplar)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("dbo.DBT_sp_Cari_Tedarikci_Yaslandirma_WEB", connection))
                {
                    command.CommandTimeout = 120;
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@CariIlkKod", cariIlkKod);
                    command.Parameters.AddWithValue("@CariSonKod", cariSonKod);
                    command.Parameters.AddWithValue("@CariKodYapisi", cariKodYapisi);
                    command.Parameters.AddWithValue("@RaporTarihi", raporTarihi.HasValue ? (object)raporTarihi.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@HangiHesaplar", hangiHesaplar);

                    using (var reader = command.ExecuteReader())
                    {
                        var result = new List<CariBakiyeYaslandirma>();
                        while (reader.Read())
                        {
                            result.Add(new CariBakiyeYaslandirma
                            {
                                MusteriKodu = reader.GetString(0),
                                Unvan = reader.GetString(1),
                                VadesiGecenBakiye = reader.GetDouble(2),
                                VadesiGecmisBakiye = reader.GetDouble(3),
                                ToplamBakiye = reader.GetDouble(4),
                                BakiyeTipi = reader.GetString(5),
                                Gun30 = reader.GetDouble(6),
                                Gun60 = reader.GetDouble(7),
                                Gun90 = reader.GetDouble(8),
                                Gun120 = reader.GetDouble(9),
                                gecmisGun30 = reader.GetDouble(10),
                                gecmisGun60 = reader.GetDouble(11),
                                gecmisGun90 = reader.GetDouble(12),
                                gecmisGun120 = reader.GetDouble(13),
                                GunUstu120 = reader.GetDouble(14),
                                eksi120GunUstu = reader.GetDouble(reader.GetOrdinal("eksi120GunUstu"))


                            });
                        }
                        return result;
                    }
                }
            }

        }
        public IEnumerable<StockMovement> GetStokYaslandirma(string stockCode, DateTime reportDate, int? depoNo = null)
        {
            var stockMovements = new List<StockMovement>();

            try
            {
                var connectionString = _dbSelectorService.GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("dbo.stok_yaslandirma_WEB", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@StokKod", (object)stockCode ?? DBNull.Value);
                        command.Parameters.AddWithValue("@RaporTarihi", reportDate);
                        command.Parameters.AddWithValue("@DepoNo", (object)depoNo ?? DBNull.Value); // Depo numarası parametresi ekleme

                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var stockMovement = new StockMovement
                                {
                                    MsgS0088 = reader.GetGuid(reader.GetOrdinal("#msg_S_0088")),
                                    MsgS0078 = reader.GetString(reader.GetOrdinal("msg_S_0078")),
                                    MsgS0870 = reader.GetString(reader.GetOrdinal("msg_S_0870")),
                                    DepoAdi = reader.GetString(reader.GetOrdinal("dep_adi")),
                                    BirimAdi = reader.GetString(reader.GetOrdinal("sto_birim1_ad")),
                                    MsgS0165 = reader.GetDouble(reader.GetOrdinal("msg_S_0165")),
                                    StokEvraknoSeri = reader.GetString(reader.GetOrdinal("sth_evrakno_seri")),
                                    StokEvraknoSira = reader.GetInt32(reader.GetOrdinal("sth_evrakno_sira")),
                                    StokMiktar = reader.GetDouble(reader.GetOrdinal("sth_miktar")),
                                    StokTutar = reader.GetDouble(reader.GetOrdinal("sth_tutar")),
                                    StokTarih = reader.GetDateTime(reader.GetOrdinal("sth_tarih")),
                                    CumulativeQuantity = reader.GetDouble(reader.GetOrdinal("CumulativeQuantity")),
                                    StoktaGirenMiktar = reader.GetDouble(reader.GetOrdinal("Stokta_Giren_Miktar")),
                                    Days0To30 = reader.GetDouble(reader.GetOrdinal("Days0To30")),
                                    Days31To60 = reader.GetDouble(reader.GetOrdinal("Days31To60")),
                                    Days61To90 = reader.GetDouble(reader.GetOrdinal("Days61To90")),
                                    Days90Plus = reader.GetDouble(reader.GetOrdinal("Days90Plus")),
                                    NumericDate = reader.GetDouble(reader.GetOrdinal("NumericDate")),
                                    AltDovizKuru = reader.GetDouble(reader.GetOrdinal("sth_alt_doviz_kuru"))
                                };

                                stockMovements.Add(stockMovement);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata işlemleri
                Console.WriteLine($"Hata: {ex.Message}");
            }

            return stockMovements;
        }



        public IEnumerable<Depo> GetDepoList()
        {
            var depoList = new List<Depo>();

            try
            {
                var connectionString = _dbSelectorService.GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("SELECT dep_no, dep_adi FROM DEPOLAR", connection))
                    {
                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var depo = new Depo
                                {
                                    DepoNo = reader.GetInt32(reader.GetOrdinal("dep_no")),
                                    DepoAdi = reader.GetString(reader.GetOrdinal("dep_adi"))
                                };

                                depoList.Add(depo);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata işlemleri
                Console.WriteLine($"Hata: {ex.Message}");
            }

            return depoList;
        }



        public List<StockAging> GetStockAging(string stokKod, DateTime? raporTarihi)
        {
            var stockAgingList = new List<StockAging>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand("stok_yaslandirma_WEB", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@StokKod", stokKod ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RaporTarihi", raporTarihi ?? (object)DBNull.Value);

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        // Sütun adlarını kontrol edin
                        int guidIndex = reader.GetOrdinal("#msg_S_0088"); // GUID sütunu
                        int stokKodIndex = reader.GetOrdinal("msg_S_0078");
                        int stokIsimIndex = reader.GetOrdinal("msg_S_0870");
                        int evrakSeriIndex = reader.GetOrdinal("sth_evrakno_seri");
                        int evrakSiraIndex = reader.GetOrdinal("sth_evrakno_sira");
                        int miktarIndex = reader.GetOrdinal("sth_miktar");
                        int tutarIndex = reader.GetOrdinal("sth_tutar");
                        int tarihIndex = reader.GetOrdinal("sth_tarih");
                        int cumulativeQuantityIndex = reader.GetOrdinal("CumulativeQuantity");
                        int stoktaGirenMiktarIndex = reader.GetOrdinal("Stokta_Giren_Miktar");
                        int stokMiktarIndex = reader.GetOrdinal("msg_S_0165");
                        int days0To30Index = reader.GetOrdinal("Days0To30");
                        int days31To60Index = reader.GetOrdinal("Days31To60");
                        int days61To90Index = reader.GetOrdinal("Days61To90");
                        int days90PlusIndex = reader.GetOrdinal("Days90Plus");

                        while (reader.Read())
                        {
                            var stockAging = new StockAging
                            {
                                Guid = reader.IsDBNull(guidIndex) ? Guid.Empty : reader.GetGuid(guidIndex),
                                StokKod = reader.IsDBNull(stokKodIndex) ? null : reader.GetString(stokKodIndex),
                                StokIsim = reader.IsDBNull(stokIsimIndex) ? null : reader.GetString(stokIsimIndex),
                                EvrakSeri = reader.IsDBNull(evrakSeriIndex) ? null : reader.GetString(evrakSeriIndex),
                                EvrakSira = reader.IsDBNull(evrakSiraIndex) ? 0 : reader.GetInt32(evrakSiraIndex),
                                Miktar = reader.IsDBNull(miktarIndex) ? 0f : (float)reader.GetDouble(miktarIndex),
                                Tutar = reader.IsDBNull(tutarIndex) ? 0f : (float)reader.GetDouble(tutarIndex),
                                Tarih = reader.GetDateTime(tarihIndex),
                                CumulativeQuantity = reader.IsDBNull(cumulativeQuantityIndex) ? 0f : (float)reader.GetDouble(cumulativeQuantityIndex),
                                StoktaGirenMiktar = reader.IsDBNull(stoktaGirenMiktarIndex) ? 0f : (float)reader.GetDouble(stoktaGirenMiktarIndex),
                                StokMiktar = reader.IsDBNull(stokMiktarIndex) ? 0f : (float)reader.GetDouble(stokMiktarIndex),
                                Days0To30 = reader.IsDBNull(days0To30Index) ? 0f : (float)reader.GetDouble(days0To30Index),
                                Days31To60 = reader.IsDBNull(days31To60Index) ? 0f : (float)reader.GetDouble(days31To60Index),
                                Days61To90 = reader.IsDBNull(days61To90Index) ? 0f : (float)reader.GetDouble(days61To90Index),
                                Days90Plus = reader.IsDBNull(days90PlusIndex) ? 0f : (float)reader.GetDouble(days90PlusIndex),
                            };

                            stockAgingList.Add(stockAging);
                        }
                    }
                }
            }

            return stockAgingList;
        }



        // Stok isimlerini getiren metot
        //public List<string> GetAllStockNames()
        //{
        //    var stockNames = new List<string>();

        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        using (var command = new SqlCommand("SELECT DISTINCT msg_S_0078 FROM StockTable", connection))
        //        {
        //            connection.Open();
        //            using (var reader = command.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    stockNames.Add(reader.GetString(0));
        //                }
        //            }
        //        }
        //    }

        //    return stockNames;
        //}

        public IEnumerable<StockCodeAndName> GetStockCodesAndNames()
        {
            var stockCodesAndNames = new List<StockCodeAndName>();

            try
            {
                var connectionString = _dbSelectorService.GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand("dbo.GetStockCodesAndNames", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var stockCodeAndName = new StockCodeAndName
                                {
                                    StockCode = reader.GetString(reader.GetOrdinal("StockCode")),
                                    StockName = reader.GetString(reader.GetOrdinal("StockName"))
                                };

                                stockCodesAndNames.Add(stockCodeAndName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                Console.WriteLine("Error: " + ex.Message);
            }

            return stockCodesAndNames;
        }

        public IEnumerable<MusteriCekViewModel> GetMusteriCekleri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {

            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
                    EXEC dbo.DBT_MusteriCekleri_TariheGore 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi
                ";
                return connection.Query<MusteriCekViewModel>(query, parameters);
            }
        }

        // Firma Çekleri Verisini Al
        public IEnumerable<FirmaCekViewModel> GetFirmaCekleri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
                    EXEC dbo.DBT_FirmaCekleri_TariheGore 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi
                ";
                return connection.Query<FirmaCekViewModel>(query, parameters);
            }
        }

        public IEnumerable<VadesiGecmisCekViewModel> VadesiGecmisFirmaCekleri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
                    EXEC dbo.DBT_VadesiGecenFirmaCekleri 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi
                ";
                return connection.Query<VadesiGecmisCekViewModel>(query, parameters);
            }
        }

        public IEnumerable<VadesiGecmisCekViewModel> VadesiGecenMusteriCekler(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
                    EXEC dbo.DBT_VadesiGecenMusteriCekler 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi
                ";
                return connection.Query<VadesiGecmisCekViewModel>(query, parameters);
            }
        }

        public IEnumerable<MusteriKrediKartlari> GetMusteriKrediKartlari(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
            EXEC dbo.DBT_Finans_MusteriKrediKartlari_WEB 
                @BaslamaTarihi = @BaslamaTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<MusteriKrediKartlari>(query, parameters);
            }
        }


        public IEnumerable<FirmaKrediKartlari> GetFirmaKrediKartlari(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
            EXEC dbo.DBT_Finans_FirmaKrediKartlari 
                @BaslamaTarihi = @BaslamaTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<FirmaKrediKartlari>(query, parameters);
            }
        }
        public IEnumerable<ArtiBakiyeFaturaViewModel> GetArtiBakiyeFaturaMusteri(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslangicTarihi = baslamaTarihi, BitisTarihi = bitisTarihi }; // Parametreler doğru isimde ve formatta olmalı
                var query = @"
            EXEC dbo.DBT_Arti_Bakiye_Fatura_Musteri 
                @BaslangicTarihi = @BaslangicTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<ArtiBakiyeFaturaViewModel>(query, parameters);
            }
        }
        public IEnumerable<ArtiBakiyeFaturaViewModel> GetArtiBakiyeFaturaTedarikci(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslangicTarihi = baslamaTarihi, BitisTarihi = bitisTarihi }; // Parametreler doğru isimde ve formatta olmalı
                var query = @"
            EXEC dbo.DBT_Arti_Bakiye_Fatura_Tedarikci 
                @BaslangicTarihi = @BaslangicTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<ArtiBakiyeFaturaViewModel>(query, parameters);
            }
        }
        public IEnumerable<StokFoy> GetStokFoy(string stokkod, DateTime? devirtarih, DateTime? ilktar, DateTime? sontar, byte dovizCinsi, string depolarstr)
        {
            var stokFoyList = new List<StokFoy>();
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("dbo.sp_StokFoy", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@stokkod", stokkod);
                    command.Parameters.AddWithValue("@devirtarih", devirtarih.HasValue ? (object)devirtarih.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@ilktar", ilktar.HasValue ? (object)ilktar.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@sontar", sontar.HasValue ? (object)sontar.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@DovizCinsi", dovizCinsi);
                    command.Parameters.AddWithValue("@Depolarstr", depolarstr);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stokFoyList.Add(new StokFoy
                            {
                                KayitNo = reader["KayitNo"] as Guid?,
                                Tarih = reader["Tarih"] as DateTime?,
                                TarihGunSayisi = reader.GetInt32(reader.GetOrdinal("TarihGunSayisi")),
                                FaturaTarihi = reader["FaturaTarihi"] as DateTime?,
                                SeriNo = reader["SeriNo"].ToString(),
                                SiraNo = reader["SiraNo"] as int?,
                                EvrakTipi = reader["EvrakTipi"].ToString(),
                                HareketCinsi = reader["HareketCinsi"].ToString(),
                                Tipi = reader["Tipi"].ToString(),
                                GirisCikis = reader.GetInt32(reader.GetOrdinal("GirisCikis")),
                                Ni = reader["Ni"].ToString(),
                                BelgeNo = reader["BelgeNo"].ToString(),
                                BelgeTarihi = reader["BelgeTarihi"] as DateTime?,
                                DepoAdi = reader["DepoAdi"].ToString(),
                                NakliyeHedefDepo = reader["NakliyeHedefDepo"].ToString(),
                                KarsiDepoAdi = reader["KarsiDepoAdi"].ToString(),
                                PartiNo = reader["PartiNo"].ToString(),
                                LotNo = reader["LotNo"] as int?,
                                IsMerkeziKodu = reader["IsMerkeziKodu"].ToString(),
                                ProjeKodu = reader["ProjeKodu"].ToString(),
                                ProjeAdi = reader["ProjeAdi"].ToString(),
                                Stokta_Giren_Miktar = Convert.ToDouble(reader["Stokta_Giren_Miktar"]),
                                BirimAdi = reader["BirimAdi"].ToString(),
                                AltDovizKuru = Convert.ToDouble(reader["AltDovizKuru"]),
                                BrutBirimFiyati = Convert.ToDouble(reader["BrutBirimFiyati"]),
                                NetBirimFiyati = Convert.ToDouble(reader["NetBirimFiyati"]),
                                BrutTutar = Convert.ToDouble(reader["BrutTutar"]),
                                NetTutar = Convert.ToDouble(reader["NetTutar"]),
                                GirenMiktar = Convert.ToDouble(reader["GirenMiktar"]),
                                CikanMiktar = Convert.ToDouble(reader["CikanMiktar"]),
                                KalanMiktar = Convert.ToDouble(reader["KalanMiktar"])
                            });
                        }
                    }
                }
            }

            return stokFoyList; // Boş bile olsa her zaman bir liste döndür
        }

        public List<KrediDetayi> GetKrediDetay(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var sql = @"
            SELECT 
                krsoztaksit_sozkodu, 
                krsoztaksit_vade, 
                krsoztaksit_taksit, 
                krsoztaksit_anapara, 
                krsoztaksit_odenen_ana, 
                krsoztaksit_faiz, 
                krsoztaksit_bsmv,
                krsoz_sozbankakodu,
                ban_ismi,
                krsoztaksit_odemeevraksira,
                krsoztaksit_faizorani,
                (krsoztaksit_taksit - krsoztaksit_odenen_ana) AS kalan -- Kalan anapara hesaplaması
            FROM KREDI_SOZLESMESI_TANIMLARI
            LEFT JOIN Bankalar ON ban_kod = krsoz_sozbankakodu
            LEFT JOIN KREDI_SOZLESMESI_TAKSIT_TANIMLARI ON krsoz_kodu = krsoztaksit_sozkodu
            WHERE krsoztaksit_vade BETWEEN @BaslamaTarihi AND @BitisTarihi";

                return connection.Query<KrediDetayi>(sql, new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi }).ToList();
            }
        }

        public IEnumerable<CiroRaporuDepoBazli> GetCiroRaporuDepoBazli(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
            EXEC dbo.DBT_RAPOR_CiroRaporuDepoBazlı 
                @BaslamaTarihi = @BaslamaTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<CiroRaporuDepoBazli>(query, parameters);
            }
        }

        public IEnumerable<EnCokSatilanUrunler> GetEnCokSatilanUrunler(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new { BaslamaTarihi = baslamaTarihi, BitisTarihi = bitisTarihi };
                var query = @"
            EXEC dbo.DBT_MO_SatisMiktarinaGoreEnCokSatilanUrunler 
                @BaslamaTarihi = @BaslamaTarihi,
                @BitisTarihi = @BitisTarihi
        ";
                return connection.Query<EnCokSatilanUrunler>(query, parameters);
            }
        }

        public IEnumerable<SatilanMalinKarlilikveMaliyet> GetSatilanMalinKarlilikveMaliyet(
         DateTime baslamaTarihi,
         DateTime bitisTarihi,
         string depoNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new
                {
                    BaslamaTarihi = baslamaTarihi,
                    BitisTarihi = bitisTarihi,
                    DepoNo = depoNo
                };

                var query = @"
                    EXEC dbo.DBT_RAPOR_SatilanMalinKarlilikveMaliyetRaporu 
                        @BaslamaTarihi = @BaslamaTarihi,
                        @BitisTarihi = @BitisTarihi,
                        @DepoNo = @DepoNo";

                return connection.Query<SatilanMalinKarlilikveMaliyet>(query, parameters);
            }
        }
        public IEnumerable<StokRaporuViewModel> GetStokRaporu(int? anaGrup, int? reyonKodu, int depoNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                const string procedureName = "sp_StokRaporu";

                var parameters = new
                {
                    AnaGrup = anaGrup,
                    ReyonKodu = reyonKodu,
                    DepoNo = depoNo
                };

                try
                {
                    var results = connection.Query<StokRaporuViewModel>(
                        procedureName,
                        parameters,
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stok raporu alınırken hata oluştu.");
                    throw;
                }
            }
        }



        // İş emri durumunu güncelleyen metod
        private string GetCurrentUserNo()
        {
            // Öncelikle session'dan kullanıcı numarasını almayı deneyin
            var userNo = _httpContextAccessor.HttpContext?.Session.GetString("UserNo");

            if (string.IsNullOrEmpty(userNo))
            {
                // Eğer session'da yoksa, kimlik adını kullanın
                userNo = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            }

            _logger.LogInformation($"Mevcut Kullanıcı Numarası: {userNo}");
            return userNo ?? string.Empty;
        }

        // Mevcut GetIsEmirleri metodunu güncelle
        public IEnumerable<IsEmriModel> GetIsEmirleri()
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = HasProductionPermission()
                    ? GetFullProductionQuery()
                    : GetLimitedProductionQuery();

                try
                {
                    return connection.Query<IsEmriModel>(query);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "İş emirleri getirilirken hata oluştu");
                    throw;
                }
            }
        }

        public bool HasProductionPermission()
        {
            var userNo = GetCurrentUserNo();
            return MenuHelper.KullaniciYetkisiVarMi("Fatura", "UretIsEmri", userNo);
        }

        private string GetFullProductionQuery()
        {
            return @"
      SELECT 
    i.is_Guid,
    i.is_Kod,
    i.is_Ismi,
    i.is_EmriDurumu,
    i.is_BaslangicTarihi,
    rtp.RtP_PlanlananIsMerkezi as IsMerkezi,
    im.IsM_Aciklama as IsMerkeziAciklama,
    upl.upl_kodu as UrunKodu,
    s.sto_isim as UrunAdi,
    upl.upl_miktar as Miktar
FROM ISEMIRLERI i
LEFT JOIN URETIM_MALZEME_PLANLAMA upl 
    ON upl.upl_isemri = i.is_Kod 
    AND upl.upl_uretim_tuket = 1
LEFT JOIN STOKLAR s 
    ON s.sto_kod = upl.upl_kodu
LEFT JOIN URETIM_ROTA_PLANLARI rtp
    ON rtp.RtP_IsEmriKodu = i.is_Kod 
    AND rtp.RtP_UrunKodu = upl.upl_kodu 
    AND rtp.RtP_SatirNo = 0
LEFT JOIN IS_MERKEZLERI im
    ON im.IsM_Kodu = rtp.RtP_PlanlananIsMerkezi
WHERE i.is_EmriDurumu IN (0, 1)
    AND upl.upl_kodu IS NOT NULL  -- UrunKodu boş olanları filtrele
ORDER BY upl.upl_kodu ASC, i.is_BaslangicTarihi DESC;  
";
        }

        private string GetLimitedProductionQuery()
        {
            return @"
         SELECT 
    i.is_Guid,
    i.is_Kod,
    i.is_Ismi,
    i.is_EmriDurumu,
    i.is_BaslangicTarihi,
    rtp.RtP_PlanlananIsMerkezi as IsMerkezi,
    im.IsM_Aciklama as IsMerkeziAciklama,
    upl.upl_kodu as UrunKodu,
    s.sto_isim as UrunAdi,
    upl.upl_miktar as Miktar
FROM ISEMIRLERI i
LEFT JOIN URETIM_MALZEME_PLANLAMA upl 
    ON upl.upl_isemri = i.is_Kod 
    AND upl.upl_uretim_tuket = 1
LEFT JOIN STOKLAR s 
    ON s.sto_kod = upl.upl_kodu
LEFT JOIN URETIM_ROTA_PLANLARI rtp
    ON rtp.RtP_IsEmriKodu = i.is_Kod 
    AND rtp.RtP_UrunKodu = upl.upl_kodu 
    AND rtp.RtP_SatirNo = 0
LEFT JOIN IS_MERKEZLERI im
    ON im.IsM_Kodu = rtp.RtP_PlanlananIsMerkezi
WHERE i.is_EmriDurumu IN (0, 1)
AND rtp.RtP_PlanlananIsMerkezi = '010'
    AND upl.upl_kodu IS NOT NULL  
ORDER BY upl.upl_kodu ASC, i.is_BaslangicTarihi DESC;
            
        ";
        }

        // İş emri durumunu güncelleyen metod
        public bool UpdateIsEmriDurumu(string isEmriKodu, int yeniDurum)
        {
            if (yeniDurum != 0 && yeniDurum != 1)
            {
                _logger.LogWarning("Geçersiz iş emri durumu: {YeniDurum}", yeniDurum);
                return false;
            }

            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                UPDATE ISEMIRLERI 
                SET is_EmriDurumu = @YeniDurum
                WHERE is_Kod = @IsEmriKodu";

                try
                {
                    var parameters = new { IsEmriKodu = isEmriKodu, YeniDurum = yeniDurum };
                    var affectedRows = connection.Execute(query, parameters);

                    _logger.LogInformation(
                        "İş emri durumu güncellendi. Kod: {IsEmriKodu}, Yeni Durum: {YeniDurum}, Etkilenen Kayıt: {AffectedRows}",
                        isEmriKodu, yeniDurum, affectedRows);

                    return affectedRows > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "İş emri durumu güncellenirken hata oluştu. Kod: {IsEmriKodu}, Yeni Durum: {YeniDurum}",
                        isEmriKodu, yeniDurum);
                    throw;
                }
            }

        }
        public IEnumerable<MusteriRiskAnalizi> GetMusteriRiskAnalizi(DateTime? raporTarihi = null)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                const string procedureName = "DBT_sp_MusteriRiskAnalizi";
                var parameters = new { RaporTarihi = raporTarihi ?? DateTime.Now };

                try
                {
                    var sonuclar = connection.Query<MusteriRiskAnalizi>(
                        procedureName,
                        parameters,
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    return sonuclar;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Müşteri risk analizi alınırken hata oluştu");
                    throw;
                }
            }
        }

        public IEnumerable<SiparisDetayViewModel> GetSiparisDetay(DateTime? baslamaTarihi = null, DateTime? bitisTarihi = null)
        {
            // Tarihler belirtilmemişse, başlangıç tarihi bugünden 15 gün öncesi, bitiş tarihi bugün olacak
            baslamaTarihi ??= DateTime.Now.AddDays(-15);
            bitisTarihi ??= DateTime.Now;
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                // Önce siparişleri getirelim
                var parameters = new
                {
                    sip_tip = 0,
                    sip_cins = 0,
                    AcikmiKapalimi = "Açık",
                    MusteriKodu = (string)null,
                    DepoNo = 3,
                    BaslamaTarihi = baslamaTarihi,
                    BitisTarihi = bitisTarihi,
                    EvrakSeri = (string)null,
                    EvrakSira = (string)null,
                    BelgeNo = (string)null,
                    yk_kodu = (string)null
                };
                var query = "EXEC DBT_MO_SiparisDetayGetir2 @sip_tip, @sip_cins, @AcikmiKapalimi, @MusteriKodu, @DepoNo, @BaslamaTarihi, @BitisTarihi, @EvrakSeri, @EvrakSira, @BelgeNo, @yk_kodu";
                var siparisler = connection.Query<SiparisDetayViewModel>(query, parameters)
                    .OrderByDescending(s => s.EvrakSira) // Evrak sırasına göre büyükten küçüğe sıralama
                    .ToList();

                // Siparişler boşsa erken dönüş
                if (!siparisler.Any())
                {
                    return siparisler;
                }

                // Gelen siparişlerin evrak numaralarını topla
                var evrakNumaralari = siparisler.Select(s => s.EvrakSira).Distinct().ToList();

                // Sadece bu evrak numaraları için açıklamaları getir
                var evrakListesiQuery = $@"
            SELECT ea.egk_evr_sira, ea.egk_evracik8, ea.egk_evracik9, ea.egk_evracik10 
            FROM EVRAK_ACIKLAMALARI ea 
            WHERE ea.egk_evr_sira IN ({string.Join(",", evrakNumaralari)})";

                var evrakAciklamalari = connection.Query(evrakListesiQuery).ToList();

                // Her sipariş için, aynı evrak numarasına sahip TÜMM açıklamaları kontrol et
                foreach (var siparis in siparisler)
                {
                    // Bu siparişin evrak numarasına sahip TÜM açıklamaları bul
                    var evrakBilgileri = evrakAciklamalari
                        .Where(e => (int)e.egk_evr_sira == siparis.EvrakSira)
                        .ToList();

                    if (evrakBilgileri.Any())
                    {
                        // Öncelikle 'Basladi' durumuna sahip olanı ara
                        var basladiDurumu = evrakBilgileri.FirstOrDefault(e => e.egk_evracik10?.ToString() == "Basladi");

                        if (basladiDurumu != null)
                        {
                            siparis.IslemDurumu = basladiDurumu.egk_evracik10?.ToString();
                            siparis.RampaBilgisi = basladiDurumu.egk_evracik9?.ToString();
                        }
                        else
                        {
                            // 'Basladi' yoksa, herhangi bir değeri al
                            var herhangiAciklama = evrakBilgileri.First();
                            siparis.IslemDurumu = herhangiAciklama.egk_evracik10?.ToString();
                            siparis.RampaBilgisi = herhangiAciklama.egk_evracik9?.ToString();
                        }
                    }
                }

                return siparisler;
            }
        }  
        public IEnumerable<StokHareket> GetStokHareketleri(string siparisGuid)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
    SELECT 
        sh.sth_stok_kod AS StokKodu,
        sh.sth_miktar AS Miktar,
        sh.sth_parti_kodu AS PartiKodu,
        s.sto_isim AS StokAdi,
        sh.sth_cari_kodu AS CariKodu,
        sh.sth_evrakno_seri AS EvrakSeri,
        sh.sth_evrakno_sira AS EvrakSira,
        sh.sth_sip_uid AS SiparisGuid
    FROM STOK_HAREKETLERI sh WITH(NOLOCK)
    LEFT JOIN STOKLAR s WITH(NOLOCK) ON s.sto_kod = sh.sth_stok_kod
    WHERE sh.sth_sip_uid IN (
        SELECT sip_Guid 
        FROM SIPARISLER 
        WHERE sip_evrakno_sira = (
            SELECT sip_evrakno_sira 
            FROM SIPARISLER 
            WHERE sip_Guid = @SiparisGuid
        )
    )";

                try
                {
                    connection.Open();

                    // Doğrudan StokHareket olarak sorgula ve maple
                    var result = connection.Query<StokHareket>(query, new { SiparisGuid = siparisGuid }).ToList();

                    // Sonuçları günlüğe kaydet
                    _logger.LogInformation($"Bulunan stok hareketleri sayısı: {result.Count}");
                    if (result.Any())
                    {
                        _logger.LogDebug($"İlk stok hareketi: {System.Text.Json.JsonSerializer.Serialize(result.First())}");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Stok hareketleri getirilirken hata oluştu. SiparisGuid: {siparisGuid}");
                    throw;
                }
            }
        }

        public RampUpdateResult UpdateSiparisDurum(int evrakSira, Guid siparisGuid, string rampaBilgisi, string islemDurumu)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check if ramp is already in use by another order
                        var checkQuery = @"
                    SELECT COUNT(1) 
                    FROM EVRAK_ACIKLAMALARI 
                    WHERE egk_evracik9 = @rampaBilgisi 
                    AND egk_evr_sira != @evrakSira
                    AND egk_evracik10 != 'Bitti'";

                        var rampInUse = connection.QuerySingle<int>(checkQuery,
                            new { rampaBilgisi, evrakSira },
                            transaction) > 0;

                        if (rampInUse)
                        {
                            transaction.Rollback();
                            return new RampUpdateResult
                            {
                                Success = false,
                                Message = $"Rampa {rampaBilgisi} başka bir sipariş tarafından kullanılıyor."
                            };
                        }

                        // Determine update query based on the process status
                        string updateQuery;
                        object queryParams;

                        if (islemDurumu == "Bitti")
                        {
                            // When status is "Bitti", keep the existing ramp information
                            updateQuery = @"
                        UPDATE EVRAK_ACIKLAMALARI 
                        SET egk_evracik8 = @siparisGuid,
                            egk_evracik10 = @islemDurumu
                        WHERE egk_evr_sira = @evrakSira";

                            queryParams = new
                            {
                                evrakSira,
                                siparisGuid,
                                islemDurumu
                            };
                        }
                        else
                        {
                            // For other statuses, update ramp information
                            updateQuery = @"
                        UPDATE EVRAK_ACIKLAMALARI 
                        SET egk_evracik8 = @siparisGuid,
                            egk_evracik9 = @rampaBilgisi,
                            egk_evracik10 = @islemDurumu
                        WHERE egk_evr_sira = @evrakSira";

                            queryParams = new
                            {
                                evrakSira,
                                siparisGuid,
                                rampaBilgisi,
                                islemDurumu
                            };
                        }

                        // Execute the update
                        var result = connection.Execute(updateQuery, queryParams, transaction);

                        if (result > 0)
                        {
                            transaction.Commit();
                            return new RampUpdateResult
                            {
                                Success = true,
                                Message = "Sipariş durumu başarıyla güncellendi."
                            };
                        }
                        else
                        {
                            transaction.Rollback();
                            return new RampUpdateResult
                            {
                                Success = false,
                                Message = "Güncelleme başarısız. Sipariş bulunamadı."
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new RampUpdateResult
                        {
                            Success = false,
                            Message = $"Hata oluştu: {ex.Message}"
                        };
                    }
                }
            }
        }
        public string UretIsEmri(string isEmriKodu, string urunKodu, int depoNo)
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("videojet2micro2025", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@isemri", isEmriKodu);
                    command.Parameters.AddWithValue("@stokkodu", urunKodu);
                    command.Parameters.AddWithValue("@depo", depoNo);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string barkod = reader.GetString(reader.GetOrdinal("barkod"));
                            string makine = reader.GetString(reader.GetOrdinal("makine"));
                            return $"Barkod: {barkod}, Makine: {makine}";
                        }
                        return "Üretim yapıldı fakat sonuç alınamadı.";
                    }
                }
            }
        }
        // Model sınıfı ekleyin


        // FaturaRepository sınıfına eklenecek metot
       
    }
}