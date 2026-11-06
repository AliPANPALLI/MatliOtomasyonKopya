using KumesScada.Domain.Models.Excel;

namespace KumesScada.Application.Interfaces
{
    /// <summary>
    /// Kümesin ana excel'indeki ("Yumurtlama Kartı") "Yumurtlama "
    /// sayfasını günlük veri kaynağı olarak okur — geçmiş yıllara ait,
    /// başka hiçbir kaynaktan (Günlük Yem Hesapları/Tasnif) elde
    /// edilemeyen veriyi veritabanına aktarmak için kullanılır. Bkz.
    /// VGYumurtlamaKartExcelReader.
    /// </summary>
    public interface IVGYumurtlamaKartExcelReader
    {
        VGKartImportResult Read(Stream stream, string fileName, string kumesNo);

        /// <summary>
        /// Günlük verileri atlayıp SADECE kapak bilgisini (giriş tarihi/
        /// başlangıç hayvan sayısı) okur — "Hedef Değerleri" gibi aynı ana
        /// excel'i farklı bir amaçla (başka bir sayfasından) işleyen
        /// akışların, günlük veri ayrıştırmasının (kumesNo'ya bağlı
        /// Rahmiye/VerimGıda ayrımı dahil) maliyetine girmeden dönem
        /// bilgisini de çıkarabilmesi için.
        /// </summary>
        VGKartImportResult ReadKapakBilgisi(Stream stream, string fileName);
    }
}
