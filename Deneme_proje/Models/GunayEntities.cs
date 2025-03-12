namespace Deneme_proje.Models
{
    public class GunayEntities
    {
        public class OtokocViewModel
        {
            public string sth_stok_kod { get; set; }

            public string sto_isim { get; set; }

            public int tarih_farki { get; set; }
        }
        public class FiloKiralamaViewModel
        {
            public string ChaSrmrkodu { get; set; }
            public DateTime ChaTarihi { get; set; }
            public string ChaKod { get; set; }
            public string ChaKasaHizkod { get; set; }
            public string HizIsim { get; set; }
            public decimal ChaMeblag { get; set; }
            public decimal TotalMiktar { get; set; }
            public string BirGunluk { get; set; }
            public string IkiYediGun { get; set; }
            public string YediOnBesGun { get; set; }
            public string OnBesTenFazla { get; set; }
        }
        public class Sorumlu
        {
            public string SorumluKodu { get; set; }
            public string SorumluAdi { get; set; }
        }
        public class FiloKiralamaViewModelContainer
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Srmrkodu { get; set; }
            public IEnumerable<FiloKiralamaViewModel> Results { get; set; }
        }

        public class WebAyarlarPrim
        {
            public int Id { get; set; }
            public float BirGunluk { get; set; }
            public float IkiYediGunluk { get; set; }
            public float YediOnbesGunluk { get; set; }
            public float OnbesPlusGunluk { get; set; }

            // Daha önce eklenen sütunlar
            public float EkSigortaKapsamiEkstraAylik { get; set; }
            public float EkSurucuGunluk { get; set; }
            public float EkSurucu { get; set; }
            public float BebekKoltuguEkstrasiAylik { get; set; }

            // Yeni eklenen sütunlar
            public float Servis250Alti { get; set; }
            public float Servis250_2500 { get; set; }
            public float Servis2500_5000 { get; set; }
            public float Servis5000Uzeri { get; set; }
            public float ServisEkstraMontaj { get; set; }
            public float SKO_FP60 { get; set; }
            public float SCS { get; set; }
            public float SCU_FP45_FPK_DD { get; set; }
            public float Rayic0_250 { get; set; }
            public float Rayic250_500 { get; set; }
            public float Rayic500Uzeri { get; set; }
            public float Thermoking { get; set; }
            public float valohr { get; set; }
            public float ServisYeniMotorMontaj { get; set; }
            public float gunay_grup_satis { get; set; }
            public float sirket_disi_pesin {  get; set; }
            public float sirket_disi_otuz_gun { get; set; }
            public float sirket_disi_altmis_gun { get; set; }
        }


    }
}
