using Microsoft.AspNetCore.Mvc.Rendering;

namespace Deneme_proje.Models
{
    public class Entities
    {
        public class StokHareket
        {
            public string CariKodu { get; set; }
            public string EvrakSeri { get; set; }
            public int EvrakSira { get; set; }
            public string StokKodu { get; set; }
            public string StokAdi { get; set; }
            public string PartiKodu { get; set; }
            public decimal Miktar { get; set; }
        }

        public class RampUpdateResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
        public class SiparisDetayViewModel
        {
            public Guid SiparisGuid { get; set; }
            public string FirmaAdi { get; set; }
            public string CariAdi { get; set; }
            public DateTime SiparisTarihi { get; set; }
            public string EvrakSeri { get; set; }
            public int EvrakSira { get; set; }
            public string IslemDurumu { get; set; }
            public string RampaBilgisi { get; set; }
        }
        public class MusteriRiskAnalizi
        {
            public string MusteriKodu { get; set; }
            public string MusteriAdi { get; set; }
            public decimal TicariAlacakBakiyesi { get; set; }
            public decimal VadesiGecmisPortfoyCekler { get; set; }
            public decimal VadesiGecmisCiroluCekler { get; set; }
            public decimal VadesiGelmemisCiroluCekler { get; set; }
            public decimal VadesiGelmemisPortfoyCekler { get; set; }
            public decimal TeminatTutari { get; set; }
            public decimal MusteriRiski { get; set; }
        }
        public class IsEmriModel
        {
            public Guid is_Guid { get; set; }
            public string is_Kod { get; set; }
            public string is_Ismi { get; set; }
            public int is_EmriDurumu { get; set; }
            public DateTime? is_BaslangicTarihi { get; set; }
            public string UrunKodu { get; set; }
            public string UrunAdi { get; set; }
            public decimal? Miktar { get; set; }
            public string IsMerkezi { get; set; }
            public string DurumText { get; set; }
        }

        public class CiroRaporuDepoBazli
        {
            public string DepoKodu { get; set; }
            public string DepoAdı { get; set; }
            public decimal SatışTutarı { get; set; }
        }

        public class EnCokSatilanUrunler
        {
            public string StokKodu { get; set; }
            public string StokAdı { get; set; }
            public decimal SatışFiyatı { get; set; }
            public int ToplamSatışMiktarı { get; set; }

            public decimal ToplamSatışDeğeri { get; set; }


        }

        public class StokRaporuViewModel
        {
            public string StokKodu { get; set; }
            public string StokAdı { get; set; }
            public string ReyonKodu { get; set; }
            public string DepoAdı { get; set; }
            public decimal DepoMiktarı { get; set; }
            public decimal SiparişMiktarı { get; set; }
            public decimal? SipVerilmesiGerekenMiktar { get; set; }
        }

        public class SatilanMalinKarlilikveMaliyet
        {
            public string StokKodu { get; set; }
            public string StokAdı { get; set; }
            public decimal AlışFiyatı { get; set; }
            public decimal SatışFiyatı { get; set; }
            public decimal SatışBirimFiyatı { get; set; }
            public decimal SatışMiktarı { get; set; }
            public decimal SatışTutarı { get; set; }
            public decimal ToplamKar { get; set; }
            public decimal KarOranı { get; set; }
        }

        public List<CariAlacakModel> CariAlacaklar { get; set; }
        public List<CariVerecekModel> CariVerecekler { get; set; }
        // CariAlacakModel: Cari hesaplarda alacakları temsil eder

        public class CariBakiyeYaslandirma
        {
            public string MusteriKodu { get; set; }
            public string Unvan { get; set; }
            public DateTime Vade { get; set; }
            public double VadesiGecenBakiye { get; set; }
            public double VadesiGecmisBakiye { get; set; }
            public double ToplamBakiye { get; set; }
            public string BakiyeTipi { get; set; }
            public double Gun30 { get; set; }
            public double Gun60 { get; set; }
            public double Gun90 { get; set; }
            public double Gun120 { get; set; }
            public double gecmisGun30 { get; set; }
            public double gecmisGun60 { get; set; }
            public double gecmisGun90 { get; set; }
            public double gecmisGun120 { get; set; }
            public double eksi120GunUstu { get; set; }
            public double GunUstu120 { get; set; }
        }
        public class CariAlacakModel
        {
            public string CariKodu { get; set; }
            public string CariUnvani { get; set; }
            public decimal TL { get; set; }
            public decimal USD { get; set; }
            public decimal EUR { get; set; }
            public decimal GBP { get; set; }
            public decimal UPB { get; set; }
            public string PB { get; set; }
            public decimal TL_UPB { get; set; }
            public decimal USD_UPB { get; set; }
            public decimal EUR_UPB { get; set; }
            public decimal GBP_UPB { get; set; }
        }

        // CariVerecekModel: Cari hesaplarda verecekleri temsil eder
        public class CariVerecekModel
        {
            public string CariKodu { get; set; }
            public string CariUnvani { get; set; }
            public decimal TL { get; set; }
            public decimal USD { get; set; }
            public decimal EUR { get; set; }
            public decimal GBP { get; set; }
            public decimal UPB { get; set; }
            public string PB { get; set; }
            public decimal TL_UPB { get; set; }
            public decimal USD_UPB { get; set; }
            public decimal EUR_UPB { get; set; }
            public decimal GBP_UPB { get; set; }
        }

        public class CariViewModel
        {
            public IEnumerable<CariAlacakModel> CariAlacaklar { get; set; }
            public IEnumerable<CariVerecekModel> CariVerecekler { get; set; }
            public string SektorKodu { get; set; }
            public string BolgeKodu { get; set; }
            public string GrupKodu { get; set; }
            public string TemsilciKodu { get; set; }
            public int UPB_PB { get; set; }
            public SelectList UPBOptions { get; set; }
            public SelectList SektorOptions { get; set; }
            public SelectList BolgeOptions { get; set; }
            public SelectList GrupOptions { get; set; }
            public SelectList TemsilciOptions { get; set; }
        }

        public class FaturaViewModel
        {
            public string EvrakNo { get; set; }
            public string CariKodu { get; set; }
            public string CariUnvani { get; set; }
            public DateTime FaturaTarihi { get; set; }
            public decimal FaturaTarihiSayi { get; set; }
            public DateTime AlacakVade { get; set; }
            public DateTime FaturaVadeTarihi { get; set; }
            public decimal FaturaVadesi { get; set; }
            public decimal FaturaTutari { get; set; }
            public decimal TaksitTutar { get; set; }
            public decimal AlacakVadeTarihiSayi { get; set; }
            public decimal FaizOrani { get; set; }
            public decimal BorcTutari { get; set; } // Yeni alan
            public decimal Faott { get; set; } // Faott'i decimal yapalım
            public decimal Faots { get; set; } // Faots'u decimal yapalım
            public decimal Vs { get; set; }
            public decimal Fm { get; set; }
            public decimal Fgg { get; set; }
        }


        // Models/StockMovement.cs
        public class StockMovement
        {
            public Guid MsgS0088 { get; set; }
            public string MsgS0078 { get; set; }
            public string MsgS0870 { get; set; }
            public string DepoAdi { get; set; }
            public string BirimAdi { get; set; }
            public int DepoNo { get; set; }
            public double MsgS0165 { get; set; }
            public string StokEvraknoSeri { get; set; }
            public int StokEvraknoSira { get; set; }
            public double StokMiktar { get; set; }
            public double StokTutar { get; set; }
            public DateTime StokTarih { get; set; }
            public double CumulativeQuantity { get; set; }
            public double StoktaGirenMiktar { get; set; }
            public double Days0To30 { get; set; }
            public double Days31To60 { get; set; }
            public double Days61To90 { get; set; }
            public double Days90Plus { get; set; }
            public double NumericDate { get; set; }

            // Döviz kuru alanı
            public double AltDovizKuru { get; set; }
        }

        public class Depo
        {
            public int DepoNo { get; set; }
            public string DepoAdi { get; set; }
        }

        public class StokYaslandirmaViewModel
        {
            public string StokKod { get; set; }
            public string StokIsmi { get; set; }
            public int StokYasi { get; set; }
            public string YasGrubu { get; set; }
            public float Days0To30 { get; set; }
            public float Days31To60 { get; set; }
            public float Days61To90 { get; set; }
            public float Days90Plus { get; set; }
            public float StoktaGirenMiktar { get; set; }
        }

        public class StockAging
        {
            public Guid Guid { get; set; }
            public string StokKod { get; set; }
            public string StokIsim { get; set; }
            public string EvrakSeri { get; set; }
            public int EvrakSira { get; set; }
            public float Miktar { get; set; }
            public float Tutar { get; set; }
            public DateTime Tarih { get; set; }
            public float CumulativeQuantity { get; set; }
            public float StoktaGirenMiktar { get; set; }
            public float StokMiktar { get; set; } // [msg_S_0165]
            public float Days0To30 { get; set; }
            public float Days31To60 { get; set; }
            public float Days61To90 { get; set; }
            public float Days90Plus { get; set; }
        }

        public class StokViewModel
        {
            public List<string> StockCodes { get; set; }
            public IEnumerable<StokYaslandirmaViewModel> StokYaslandirmaData { get; set; }
        }
        public class StokFoy
        {
            public Guid? KayitNo { get; set; }
            public DateTime? Tarih { get; set; }
            public int TarihGunSayisi { get; set; }
            public DateTime? FaturaTarihi { get; set; }
            public string SeriNo { get; set; }
            public int? SiraNo { get; set; }
            public string EvrakTipi { get; set; }
            public string HareketCinsi { get; set; }
            public string Tipi { get; set; }
            public int GirisCikis { get; set; }
            public string Ni { get; set; }
            public string BelgeNo { get; set; }
            public DateTime? BelgeTarihi { get; set; }
            public string DepoAdi { get; set; }
            public string NakliyeHedefDepo { get; set; }
            public string KarsiDepoAdi { get; set; }
            public string PartiNo { get; set; }
            public int? LotNo { get; set; }
            public string IsMerkeziKodu { get; set; }
            public string ProjeKodu { get; set; }
            public string ProjeAdi { get; set; }
            public double Stokta_Giren_Miktar { get; set; }
            public string BirimAdi { get; set; }
            public double AltDovizKuru { get; set; }
            public double BrutBirimFiyati { get; set; }
            public double NetBirimFiyati { get; set; }
            public double BrutTutar { get; set; }
            public double NetTutar { get; set; }
            public double GirenMiktar { get; set; }
            public double CikanMiktar { get; set; }
            public double KalanMiktar { get; set; }
        }

        public class StockYaslandirmaViewModel
        {
            public IEnumerable<StockMovement> StockMovements { get; set; }
            public IEnumerable<string> StockCodes { get; set; }
            public string SelectedStockCode { get; set; }
            public DateTime? ReportDate { get; set; }
        }

        public class StockCodeAndName
        {
            public string StockCode { get; set; }
            public string StockName { get; set; }
        }

        public class TedarikciFaturaViewModel
        {
            public string EvrakNo { get; set; }
            public string CariKodu { get; set; }
            public string CariUnvani { get; set; }
            public DateTime FaturaTarihi { get; set; }
            public decimal FaturaTarihiSayi { get; set; }
            public DateTime BorcVade { get; set; }
            public DateTime FaturaVadeTarihi { get; set; }
            public decimal FaturaVadesi { get; set; }
            public decimal FaturaTutari { get; set; }
            public decimal OdemeTutar { get; set; }
            public decimal BorcVadeTarihiSayi { get; set; }
            public decimal FaizOrani { get; set; }
            public decimal BorcTutari { get; set; }
        }

        public class CustomerAnalysisViewModel
        {
            public string CariUnvani { get; set; }
            public string CariKodu { get; set; }
            public decimal ToplamBorc { get; set; }
            public float AgirlikliOrtalamaVade { get; set; }
            public float AgirlikliOrtalamaTahsilatSuresi { get; set; }
            public float VadedenSapma { get; set; }
            public float FonlamaMaliyeti { get; set; }
            public decimal FaizGelirGider { get; set; }

        }
        public class KrediDetayViewModel
        {
            public string Banka { get; set; }
            public string BankaKodu { get; set; }
            public decimal Toplam { get; set; }
            public decimal AnaPara { get; set; }  // Corrected property name
            public decimal BSMV { get; set; }
            public decimal Faiz { get; set; }

        }

        public class KrediDetay
        {
            public string Banka { get; set; }
            public decimal AnaPara { get; set; }
            public decimal Aokf { get; set; }
        }

        public class KrediDetayi
        {
            public string krsoztaksit_sozkodu { get; set; }
            public DateTime krsoztaksit_vade { get; set; }
            public decimal krsoztaksit_taksit { get; set; }
            public decimal krsoztaksit_anapara { get; set; }
            public decimal krsoztaksit_faiz { get; set; }
            public decimal krsoztaksit_bsmv { get; set; }
            public decimal kalan { get; set; }
            public decimal krsoztaksit_faizorani { get; set; }
            public string krsoz_sozbankakodu { get; set; }
            public string ban_ismi { get; set; }
            public decimal krsoztaksit_kalananapara { get; set; }
            public decimal krsoztaksit_odemeevraksira { get; set; }

            // Ay adını döndüren özellik
            public string AyAd
            {
                get
                {
                    return krsoztaksit_vade.ToString("MMMM", new System.Globalization.CultureInfo("tr-TR"));
                }
            }
        }

        public class KrediDetayModel
        {
            public int Yıl { get; set; }
            public string Ay { get; set; }
            public string SozlesmeKodu { get; set; }

            public DateTime VadeTarihi { get; set; }
            public float TaksitAnapara { get; set; }
            public float TaksitFaiz { get; set; }
            public float TaksitBSMV { get; set; }
            public string BankaKodu { get; set; }
        }


        public class MusteriCekViewModel
        {
            public DateTime Vade { get; set; }
            public decimal Kalan { get; set; }
            public string Pozisyonu { get; set; }
            public string SahipCariAdi { get; set; }
            public string NeredeCariAdi { get; set; }
        }




        public class VadesiGecmisCekViewModel
        {



            public string SahipCariAdi { get; set; }

            public decimal KalanTutar { get; set; }

            public int cekAdedi { get; set; }

        }

        public class FirmaCekViewModel
        {
            public DateTime Vade { get; set; }
            public decimal Kalan { get; set; }
            public string Pozisyonu { get; set; }
            public string SahipCariAdi { get; set; }
            public string NeredeCariAdi { get; set; }


        }
        public class MusteriKrediKartlari
        {
            public DateTime VadeTarihi { get; set; }    // Kredi kartının vade tarihi
            public string Borçlu { get; set; }          // Borçlu kişi veya kurum
            public string Nerede { get; set; }          // Kredi kartının nerede olduğu (banka ya da firma)
            public decimal Kalan { get; set; }          // Kalan borç tutarı
            public string DovizCinsi { get; set; }      // Döviz cinsi (TL, USD, EUR)
        }

        public class FirmaKrediKartlari
        {
            public DateTime VadeTarihi { get; set; }    // Kredi kartının vade tarihi
            public string Borçlu { get; set; }          // Borçlu kişi veya kurum
            public string Nerede { get; set; }          // Kredi kartının nerede olduğu (banka ya da firma)
            public decimal Kalan { get; set; }          // Kalan borç tutarı
            public string DovizCinsi { get; set; }      // Döviz cinsi (TL, USD, EUR)
        }


        public class ArtiBakiyeFaturaViewModel
        {
            public string MusteriKodu { get; set; }
            public string Unvan { get; set; }
            public string FaturaNo { get; set; }
            public decimal Bakiye { get; set; }
            public decimal ToplamBakiye { get; set; }
            public DateTime Vade { get; set; }
        }


        public class CekDurumuViewModel
        {
            public DateTime BaslamaTarihi { get; set; }
            public DateTime BitisTarihi { get; set; }
            public List<MusteriCekViewModel> MusteriCekleri { get; set; }
            public List<FirmaCekViewModel> FirmaCekleri { get; set; }
            public List<MusteriKrediKartlari> MusteriKrediKartlari { get; set; }  // Yeni alan
            public List<FirmaKrediKartlari> FirmaKrediKartlari { get; set; }  // Yeni alan
            public List<ArtiBakiyeFaturaViewModel> ArtiBakiyeFaturaMusteri { get; set; }  // Müşteri cari bilgileri
            public List<ArtiBakiyeFaturaViewModel> ArtiBakiyeFaturaTedarikci { get; set; }  // Müşteri cari bilgileri
            public List<KrediDetayi> KrediDetaylari { get; set; }
            public List<VadesiGecmisCekViewModel> VadesiGecmisFirmaCekleri { get; set; }
            public List<VadesiGecmisCekViewModel> VadesiGecmisMusteriCekleri { get; set; }

        }
    }


    public class StockFoy
    {
        public Guid? KayitNo { get; set; }
        public DateTime? Tarih { get; set; }
        public int? TarihGunSayisi { get; set; }
        public DateTime? FaturaTarihi { get; set; }
        public string SeriNo { get; set; }
        public int? SiraNo { get; set; }
        public string EvrakTipi { get; set; }
        public string HareketCinsi { get; set; }
        public string Tipi { get; set; }
        public int? GirisCikis { get; set; }
        public string Ni { get; set; }
        public string BelgeNo { get; set; }
        public DateTime? BelgeTarihi { get; set; }
        public string DepoAdi { get; set; }
        public string NakliyeHedefDepo { get; set; }
        public string KarsiDepoAdi { get; set; }
        public string PartiNo { get; set; }
        public int? LotNo { get; set; }
        public string IsMerkeziKodu { get; set; }
        public string ProjeKodu { get; set; }
        public string ProjeAdi { get; set; }
        public double? StoktaGirenMiktar { get; set; }
        public string BirimAdi { get; set; }
        public double? AltDovizKuru { get; set; }
        public double? BrutBirimFiyati { get; set; }
        public double? NetBirimFiyati { get; set; }
        public double? BrutTutar { get; set; }
        public double? NetTutar { get; set; }
        public double? GirenMiktar { get; set; }
        public double? CikanMiktar { get; set; }
        public double? KalanMiktar { get; set; }
    }

 


}


