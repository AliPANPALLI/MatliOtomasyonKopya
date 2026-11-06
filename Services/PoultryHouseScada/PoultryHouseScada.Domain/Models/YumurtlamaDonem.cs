namespace PoultryHouseScada.Domain.Models
{
    /// <summary>
    /// Kümesin ana excel'inin ("Yumurtlama Kartı") kapak sayfasından
    /// okunan dönem bilgisi — kümese giriş tarihi, başlangıç hayvan
    /// sayısı, ırk (bkz. VgYumurtlamaDonemExcelReader).
    ///
    /// BİLİNÇLİ OLARAK mevcut "Kumes Dönem Bilgisi" sisteminden
    /// (Kumes/GetKumesDonemBilgisiByKumesId) AYRI tutulur: o sistem
    /// PLC/canlı hayvan takibi gibi operasyonel bir akışa bağlı ve
    /// dönem başlat/bitir ile elle yönetiliyor. Bu tablo ise sadece
    /// Yumurtlama Takip'in ana excel yüklemesinden beslediği, o akışa
    /// hiç dokunmayan referans bilgisidir.
    ///
    /// Bir kümesin birden fazla partisi (dönemi) olabilir — aynı
    /// (KumeslerId, GirisTarihi) tekrar yüklenirse üzerine yazılır,
    /// farklı bir giriş tarihiyle yüklenirse yeni bir dönem satırı
    /// eklenir. "Hangi dönem aktif" sorusu MAX(GirisTarihi) ile
    /// cevaplanır (bkz. VgYumurtlamaDonemAppService.GetLatestByKumesNo).
    /// </summary>
    public class YumurtlamaDonem
    {
        public int YumurtlamaDonemId { get; set; }

        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;

        public DateTime GirisTarihi { get; set; }

        public decimal BaslangicHayvanSayisi { get; set; }

        public string? Irk { get; set; }

        /// <summary>
        /// Hayvanın GirisTarihi günündeki yaşı (gün) — gerçek ana
        /// excel'de "GÜN" sütunu kümese giriş tarihinden DEĞİL, hayvanın
        /// yaşından başlar (ör. T1 127.PARTİ'de giriş günü GÜN=103'tür,
        /// GÜN=1 değil). Belirtilmezse GÜN, GirisTarihi'nde 1'den
        /// başlayan basit bir sayaç olarak hesaplanır (bkz.
        /// VGYumurtlamaService.Calculate).
        /// </summary>
        public int? GirisYasiGun { get; set; }

        /// <summary>
        /// Gerçek ana excel'in GirisTarihi günündeki satırında yazan
        /// HAFTA değeri (bkz. VGYumurtlamaKartExcelReader). HAFTA,
        /// GirisYasiGun'dan CEILING(gün/7) ile türetilemez — bu formül
        /// hayvanın 1. günü hangi haftanın hangi gününe denk geldiğine
        /// bağlı olarak karttan karta kayıyor (ör. "T1 127.PARTİ"de
        /// GÜN 118 = HAFTA 18 iken "T14 135.PARTİ"de GÜN 118 = HAFTA 17
        /// olurdu — kartın kendi HAFTA sütunu her zaman 18 diyor). Bu
        /// yüzden doğrudan karttan okunup saklanır; belirtilmezse
        /// (Rahmiye gibi kaynaklarda veya eski kayıtlarda) eskisi gibi
        /// CEILING(GirisYasiGun/7) formülüne düşülür (bkz.
        /// VGYumurtlamaService.Calculate).
        /// </summary>
        public int? GirisHaftasi { get; set; }

        public DateTime GuncellemeTarihi { get; set; }
    }
}
