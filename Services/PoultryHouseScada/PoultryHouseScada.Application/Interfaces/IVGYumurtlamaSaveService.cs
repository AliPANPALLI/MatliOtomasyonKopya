using PoultryHouseScada.Application.ViewModels;
using PoultryHouseScada.Domain.Models.Excel;

namespace PoultryHouseScada.Application.Interfaces
{
    /// <summary>
    /// Kaynak excel(ler)i okuyup (format otomatik algılanır — Verim Gıda
    /// ya da Rahmiye) kümes/tarih bazlı günlük değerleri veritabanına
    /// kalıcı olarak yazar. Böylece aynı rapor için excel'i her seferinde
    /// yeniden yüklemeye gerek kalmaz; veriler bir kez kaydedilir,
    /// sonrasında <see cref="IVGYumurtlamaQueryService"/> ile
    /// veritabanından okunabilir.
    /// </summary>
    public interface IVGYumurtlamaSaveService
    {
        Task<VGYumurtlamaSaveResultViewModel> SaveAsync(
            Stream yemHesaplariStream,
            string yemHesaplariFileName,
            Stream? tasnifStream,
            string? tasnifFileName,
            int? year,
            string? kumesNo,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// SaveAsync ile aynı upsert çekirdeği — kaynağı zaten okunmuş
        /// bir satır listesi olan çağıranlar için (bkz.
        /// VGYumurtlamaKartExcelReader, ana excel'den geçmiş veri
        /// aktarımı).
        /// </summary>
        Task<VGYumurtlamaSaveResultViewModel> SaveDataAsync(
            List<VGExcelKumesGunlukData> data,
            List<string> initialWarnings,
            int? year,
            string? kumesNo,
            CancellationToken cancellationToken = default);
    }
}
