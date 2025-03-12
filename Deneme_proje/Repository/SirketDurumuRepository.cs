using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;
using static Deneme_proje.Models.SirketDurumuEntites;
using static Deneme_proje.Models.Entities;

namespace Deneme_proje.Repository
{
    public class SirketDurumuRepository
    {
        private readonly DatabaseSelectorService _dbSelectorService;
        private readonly ILogger<SirketDurumuRepository> _logger;

        public SirketDurumuRepository(DatabaseSelectorService dbSelectorService, ILogger<SirketDurumuRepository> logger)
        {
            _dbSelectorService = dbSelectorService;
            _logger = logger;
        }

        public IEnumerable<CekAnalizi> GetFirmaCekleri(string sck_sonpoz, string projeKodu, string srmMerkeziKodu, DateTime? baslamaTarihi, DateTime? bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@sck_sonpoz", sck_sonpoz);
                parameters.Add("@ProjeKodu", projeKodu);
                parameters.Add("@SrmMerkeziKodu", srmMerkeziKodu);
                parameters.Add("@BaslamaTarihi", baslamaTarihi);
                parameters.Add("@BitisTarihi", bitisTarihi);

                try
                {
                    var result = connection.Query<CekAnalizi>(
                        "dbo.DBT_Finans_FirmaCekleri_WEB",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching firm checks");
                    throw;
                }
            }
        }

        // Bankaları al
        public IEnumerable<BankModel> GetBanks()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UPB_PB", 0);
                parameters.Add("@ban_hesap_tip", 0);

                return connection.Query<BankModel>("dbo.DBT_BankalariGetir", parameters, commandType: CommandType.StoredProcedure);
            }
        }

        // Banka detaylarını al
        public IEnumerable<BankDetailModel> GetBankDetails(string bankKodu, DateTime baslamaTarihi, DateTime bitisTarihi, int upbPb = 0, string projeKodu = null, string srmMerkeziKodu = null)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@cari_kod", bankKodu);
                parameters.Add("@baslamatarihi", baslamaTarihi);
                parameters.Add("@bitistarihi", bitisTarihi);
                parameters.Add("@UPB_PB", upbPb);
                parameters.Add("@ProjeKodu", projeKodu);
                parameters.Add("@SrmMerkeziKodu", srmMerkeziKodu);

                return connection.Query<BankDetailModel>(
                    "dbo.DBT_BankaFoyu_BankaKodaTariheGore",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
        }


        public IEnumerable<Stok> GetStoklar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT sto_kod AS StokKodu, sto_isim AS StokAdi FROM Stoklar";
                var result = connection.Query<Stok>(query).ToList();

                if (result == null || result.Count == 0)
                {
                    throw new Exception("Stok verisi bulunamadı.");
                }

                return result;
            }
        }



        // GetStokHareketFoyu metodu - Stok hareketlerini getirir
        public IEnumerable<StokHareketFoyu> GetStokHareketFoyu(string stokKodu, DateTime baslamaTarihi, DateTime bitisTarihi, int paraBirimi = 0, string depolar = "")
        {
            var connectionString = _dbSelectorService.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@StokKodu", stokKodu);
                    parameters.Add("@BaslamaTarihi", baslamaTarihi);
                    parameters.Add("@BitisTarihi", bitisTarihi);
                    parameters.Add("@ParaBirimi", paraBirimi);

                    // Depo parametresinin null veya boş string olma durumunu ayrı ayrı ele al
                    if (depolar == null)
                    {
                        parameters.Add("@Depolar", DBNull.Value);
                    }
                    else if (string.IsNullOrWhiteSpace(depolar))
                    {
                        parameters.Add("@Depolar", DBNull.Value);
                    }
                    else
                    {
                        parameters.Add("@Depolar", depolar);
                    }

                    _logger.LogInformation("Gönderilen depolar parametresi: {depolar}", depolar ?? "NULL");

                    var result = connection.Query<StokHareketFoyu>(
                        "dbo.DBT_STOK_StokHareketFoyu",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 120
                    );

                    return result.ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stok hareket verileri alınırken hata oluştu. StokKodu: {StokKodu}, Depolar: {Depolar}",
                        stokKodu, depolar ?? "NULL");
                    throw new Exception($"Stok hareket verileri alınırken bir hata oluştu: {ex.Message}", ex);
                }
            }
        }
        // GetDepolar metodu - Depo bilgilerini getirir
        public IEnumerable<Depo> GetDepolar()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT dep_no AS DepoNo, dep_adi AS DepoAdi FROM Depolar";
                var result = connection.Query<Depo>(query).ToList();

                if (result == null || result.Count == 0)
                {
                    throw new Exception("Depo verisi bulunamadı.");
                }

                return result;
            }
        }

        // GetCariKodlari metodu - Cari kodları getirir
        public IEnumerable<CariHesap> GetCariKodlari()
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT DISTINCT cari_kod AS CariKod, cari_unvan1 AS CariUnvan1 FROM CARI_HESAPLAR";
                var result = connection.Query<CariHesap>(query).ToList();

                if (result == null || result.Count == 0)
                {
                    throw new Exception("Cari kodları bulunamadı.");
                }

                return result;
            }
        }

        // GetCariHareketFoyu metodu - Cari hareketlerini getirir
        public IEnumerable<CariHareketFoyu> GetCariHareketFoyu(string firmalar, int cariCins, string cariKod, int? grupNo, DateTime? devirTar, DateTime? ilkTar, DateTime? sonTar, int odemeEmriDegerlemeDok, string somStr)
        {
            var connectionString = _dbSelectorService.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@firmalar", firmalar);
                parameters.Add("@caricins", cariCins);
                parameters.Add("@carikod", cariKod);
                parameters.Add("@grupno", grupNo);
                parameters.Add("@devirtar", devirTar);
                parameters.Add("@ilktar", ilkTar);
                parameters.Add("@sontar", sonTar);
                parameters.Add("@odemeemridegerlemedok", odemeEmriDegerlemeDok);
                parameters.Add("@SomStr", somStr);

                try
                {
                    var result = connection.Query<CariHareketFoyu>(
                        "dbo.DBT_CariHareketFoyu",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching cari hareket foyu");
                    throw;
                }
            }
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

        public IEnumerable<MusteriKrediKartlari> GetMusteriKrediKartlari(DateTime baslamaTarihi, DateTime bitisTarihi)
        {
            var connectionString = _dbSelectorService.GetConnectionString(); // Bağlantı dizesini al

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
            var connectionString = _dbSelectorService.GetConnectionString(); // Bağlantı dizesini al

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

    }
}

// Diğer metotlar aynı şekilde dinamik bağlantı ile