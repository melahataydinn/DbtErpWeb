using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Deneme_proje.Models.DiokiEntities;

namespace Deneme_proje.Repository
{
    public class DiokiRepository
    {
		private readonly DatabaseSelectorService _dbSelectorService;
		private readonly ILogger<DiokiRepository> _logger;

		public DiokiRepository(DatabaseSelectorService dbSelectorService, ILogger<DiokiRepository> logger)
		{
			_dbSelectorService = dbSelectorService;
			_logger = logger;
		}


		// Örnek bir method: Markaları getiren bir sorgu
		public IEnumerable<string> GetMarkalar()
		{
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
				var sqlQuery = @"
        SELECT DISTINCT sto_marka_kodu
        FROM STOKLAR
        WHERE sto_cins = 4 AND TRIM(sto_marka_kodu) <> ''";

				try
				{
					// Burada connection doğru şekilde kullanılıyor
					return connection.Query<string>(sqlQuery);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "An error occurred while retrieving Marka data.");
					throw;
				}
			}
		}

		public IEnumerable<string> GetModeller(string markaKodu)
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var sqlQuery = @"
		SELECT DISTINCT sto_model_kodu
		FROM STOKLAR
		WHERE sto_cins = 4 AND sto_marka_kodu = @MarkaKodu AND sto_pasif_fl=0 AND TRIM(sto_model_kodu) <> ''";

                var parameters = new { MarkaKodu = markaKodu };

                try
                {
                    return connection.Query<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Model data.");
                    throw;
                }
            }
        }
        public IEnumerable<string> GetAmbalajKodlari(string markaKodu, string modelKodu)
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var sqlQuery = @"
            SELECT DISTINCT sto_ambalaj_kodu
            FROM STOKLAR
            WHERE sto_cins = 4 
              AND sto_marka_kodu = @MarkaKodu 
              AND sto_model_kodu = @ModelKodu 
AND sto_pasif_fl=0
              AND TRIM(sto_ambalaj_kodu) <> ''";

                var parameters = new { MarkaKodu = markaKodu, ModelKodu = modelKodu };

                try
                {
                    return connection.Query<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Ambalaj Kodları data.");
                    throw;
                }
            }
        }
        public IEnumerable<string> GetKisaIsimler(string markaKodu, string modelKodu, string ambalajKodu)
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var sqlQuery = @"
            SELECT DISTINCT sto_kisa_ismi
            FROM STOKLAR
            WHERE sto_cins = 4 
              AND sto_marka_kodu = @MarkaKodu 
              AND sto_model_kodu = @ModelKodu 
              AND TRIM(sto_kisa_ismi) <> '' 
              AND sto_pasif_fl = 0 
              AND sto_ambalaj_kodu = @AmbalajKodu";

                var parameters = new { MarkaKodu = markaKodu, ModelKodu = modelKodu, AmbalajKodu = ambalajKodu };

                try
                {
                    return connection.Query<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Kısa İsim data.");
                    throw;
                }
            }
        }





        public string GetStokKodByKisaIsim(string kisaIsim)
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var sqlQuery = @"
		SELECT sto_kod
		FROM STOKLAR
		WHERE sto_kisa_ismi = @KisaIsim AND TRIM(sto_kod) <> ''";

                var parameters = new { KisaIsim = kisaIsim };

                try
                {
                    return connection.QuerySingleOrDefault<string>(sqlQuery, parameters);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Stok Kod data.");
                    throw;
                }
            }
        }
        public (string Barkod, string Makine) ExecuteVideojet2Micro(string isemri, string stokkodu, int depo, int miktar, int lotNo)
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var parameters = new DynamicParameters();
                parameters.Add("@isemri", isemri);     // İş emri parametresi eklendi
                parameters.Add("@stokkodu", stokkodu); // Stok kodu parametresi eklendi
                parameters.Add("@depo", depo);         // Depo parametresi eklendi
                parameters.Add("@miktar", miktar);     // Miktar parametresi eklendi
                parameters.Add("@lot_no", lotNo);      // Lot no parametresi eklendi

                try
                {
                    var result = connection.QuerySingle(@"EXEC dbo.videojet2micro @isemri, @stokkodu, @depo, @miktar, @lot_no", parameters);
                    return (result.barkod, result.makine);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while executing videojet2micro procedure.");
                    throw;
                }
            }
        }



        public string GetIsemriByFn(string stokkodu)
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open(); // Bağlantıyı açıyoruz

                using (var command = new SqlCommand(@"
            SELECT TOP 1 [msg_S_0349] 
            FROM dbo.fn_IsEmriOperasyon(255, NULL, NULL, 0, 2, N'', N'', N'', N'', N'', N'') 
            WHERE msg_S_0352 = @StokKodu AND #msg_S_0355 = 'Aktif'
            ORDER BY [msg_S_0351], [msg_S_0352]", (SqlConnection)connection))
                {
                    // Parametreyi ekliyoruz
                    command.Parameters.AddWithValue("@StokKodu", stokkodu);

                    try
                    {
                        // İş emrini çekiyoruz
                        var result = command.ExecuteScalar();
                        return result?.ToString();
                    }
                    catch (Exception ex)
                    {
                        // Hata loglama
                        _logger.LogError(ex, "An error occurred while retrieving İş Emri using fn_IsEmriOperasyon.");
                        throw;
                    }
                }
            }
        }

        public IEnumerable<BarkodTanimi> GetBarkodTanimi()
        {
			var connectionString = _dbSelectorService.GetConnectionString();

			using (var connection = new SqlConnection(connectionString))
			{
                var sqlQuery = @"
            SELECT bar_kodu, bar_stokkodu, bar_partikodu, bar_lotno
            FROM BARKOD_TANIMLARI";

                try
                {
                    return connection.Query<BarkodTanimi>(sqlQuery);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while retrieving Barkod Tanımı data.");
                    throw;
                }
            }
        }
    }
}

        

