using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.ViewModels;

namespace PoultryHouseScada.Application.Interfaces
{
    /// <summary>
    /// Kümenin "Yumurtlama Kartı Excel'ini Oluştur" formunda elle
    /// girilen dönem bilgisini (kümese giriş tarihi, başlangıç hayvan
    /// sayısı, ırk) kaydeder/okur — bkz. PoultryHouseScada.Domain.Models.
    /// VgYumurtlamaDonem üzerindeki açıklama: bu, mevcut "Kumes Dönem
    /// Bilgisi" (Kumes/GetKumesDonemBilgisiByKumesId) sisteminden
    /// BİLİNÇLİ OLARAK ayrı, salt Yumurtlama Takip'e özel bir kayıttır.
    ///
    /// Not: Şu an için SADECE elle girilen değerlerden besleniyor.
    /// İleride geçmiş yıllara ait eski "Yumurtlama Kartı" excel'leri
    /// toplu içeri alınmak istenirse, o dosyaların "Yumurtlama " kapak
    /// sayfasından (KÜMES/IRK/KÜMESE GİR.TAR./MİKTAR hücreleri) OKUNARAK
    /// besleme ayrıca eklenebilir — ama normal/güncel akışta BU SAYFA
    /// bizim kendi ÇIKTIMIZ olduğu için kaynak olarak kullanılmaz.
    /// </summary>
    public interface IVgYumurtlamaDonemAppService
    {
        Task<VgYumurtlamaDonemViewModel> SaveAsync(
            VgYumurtlamaDonemSaveRequest request,
            CancellationToken cancellationToken = default);

        Task<VgYumurtlamaDonemViewModel?> GetLatestByKumesNo(
            string kumesNo,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kümenin TAM OLARAK bu giriş tarihine ait dönem kaydını getirir
        /// (GetLatestByKumesNo'nun aksine en güncel değil, o tarihe ait
        /// olanı) — "Geçmiş Veri Yükle" (kart/import) bir excel'den okuduğu
        /// giriş tarihi için ırk/hayvan sayısı gibi alanları o döneme ait
        /// olmayan "en güncel" bir dönemden değil, varsa kendi kaydından
        /// devralmak için kullanır. Aynı kümesin farklı dönemlerinde farklı
        /// ırklar olabileceğinden, bu ayrım önemlidir.
        /// </summary>
        Task<VgYumurtlamaDonemViewModel?> GetByKumesNoAndGirisTarihi(
            string kumesNo,
            DateTime girisTarihi,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kümesin, verilen giriş tarihinden SONRA başlayan en yakın
        /// (varsa) sonraki dönemin giriş tarihini getirir — null ise bu
        /// dönem kümesin bilinen EN GÜNCEL dönemidir. Bir dönemin günlük
        /// verisinin hangi tarihe kadar geçerli olduğunu (bir sonraki
        /// parti başlayana kadar) sınırlamak için kullanılır — bkz.
        /// VGYumurtlamaQueryService.GetAsync üzerindeki "donemBaslangici"
        /// alt sınırıyla simetrik üst sınır.
        /// </summary>
        Task<DateTime?> GetNextGirisTarihiByKumesNo(
            string kumesNo,
            DateTime girisTarihi,
            CancellationToken cancellationToken = default);
    }
}
