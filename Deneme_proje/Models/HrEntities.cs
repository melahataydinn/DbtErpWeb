namespace Deneme_proje.Models
{
    public class HrEntities
    {
        public class IzinTalepModel
        {
            public Guid Guid { get; set; }
            public string PersonelKodu { get; set; }
            public string PersonelAdSoyad { get; set; }
            public string IdariAmirKodu { get; set; }
            public string IdariAmirAdi { get; set; }
            public DateTime TalepTarihi { get; set; }
            public byte IzinTipi { get; set; }
            public byte GunSayisi { get; set; }
            public DateTime BaslangicTarihi { get; set; }
            public double BaslamaSaati { get; set; }
            public float BitisSaati { get; set; }
            public string Amac { get; set; }
            public byte IzinDurumu { get; set; }
            public DateTime OlusturmaTarihi { get; set; }
            public string OnaylayanKullanici { get; set; }
            public string OnaylayanKullaniciAdi { get; set; }
            public string ReddetmeNedeni { get; set; }  // Corresponds to pit_aciklama1

            public string IzinTipiAdi =>
                IzinTipi switch
                {
                    1 => "Yıllık İzin",
                    2 => "Mazeret İzni",
                    3 => "Hastalık İzni",
                    4 => "Ücretsiz İzin",
                    _ => "Bilinmeyen"
                };

            public string IzinDurumuAdi =>
                IzinDurumu switch
                {
                    0 => "Beklemede",
                    1 => "Onaylandı",
                    2 => "Reddedildi",
                    _ => "Bilinmeyen"
                };
        }
    }
}