namespace MobileUpdate.Application.Interfaces
{
    /// <summary>Diske yazılan APK'nın hash'i ve boyutu.</summary>
    public record ApkKayitSonucu(string Sha256, long Boyut);

    /// <summary>
    /// APK dosyalarının saklandığı yer. APK'lar veritabanına değil diske yazılır
    /// (MobilGuncelleme:ApkKlasoru); bu arayüz Application katmanını dosya sisteminden ayırır.
    /// </summary>
    public interface IApkDepo
    {
        /// <summary>
        /// Dosyayı depoya yazar ve yazarken SHA-256'sını hesaplar. Yazma bitmeden dosya
        /// asıl adıyla görünmez, yarım dosya dağıtılmaz.
        /// </summary>
        Task<ApkKayitSonucu> KaydetAsync(string dosyaAdi, Stream icerik, CancellationToken cancellationToken = default);

        /// <summary>Dosyanın sunucudaki tam yolu.</summary>
        string TamYol(string dosyaAdi);

        bool VarMi(string dosyaAdi);

        /// <summary>Veritabanı kaydı başarısız olduğunda yetim kalan dosyayı temizlemek için.</summary>
        void Sil(string dosyaAdi);
    }
}
