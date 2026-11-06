namespace PoultryHouseScada.Application.Interfaces
{
    public interface IVGYumurtlamaExcelGenerator
    {
        Task<byte[]> GenerateAsync(
            Stream yemHesaplariStream,
            string yemHesaplariFileName,
            Stream? tasnifStream,
            string? tasnifFileName,
            int year,
            string kumesNo,
            string? irk,
            DateTime kumeseGirisTarihi,
            decimal baslangicHayvanSayisi,
            string? icSiparisNo,
            int? girisYasiGun = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Excel yüklemeden, veritabanına daha önce kaydedilmiş günlük
        /// verilerden Yumurtlama Kartı Excel'ini üretir. Dönem bilgisi
        /// (giriş tarihi/başlangıç hayvan sayısı/ırk) VgYumurtlamaDonem
        /// kaydından otomatik okunur — bulunamazsa hata fırlatılır
        /// (kullanıcı önce "Yumurtlama Kartı Excel'ini Oluştur" ile bir
        /// kez elle girmeli).
        /// </summary>
        Task<byte[]> GenerateFromDatabaseAsync(
            string kumesNo,
            CancellationToken cancellationToken = default);
    }
}