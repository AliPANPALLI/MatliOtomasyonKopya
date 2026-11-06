using PoultryHouseScada.Application.ViewModels;

namespace PoultryHouseScada.Application.Interfaces
{
    /// <summary>
    /// Kaynak excel(ler)i okuyup (format otomatik algılanır — Verim Gıda
    /// ya da Rahmiye, bkz. <see cref="IVGYumurtlamaExcelSourceService"/>)
    /// tek bir kümes için tam hesaplanmış Yumurtlama Kartı verisini
    /// (günlük + haftalık satırlar) üretir. Hem excel çıktısı
    /// (<see cref="IVGYumurtlamaExcelGenerator"/>) hem de JSON/satır
    /// bazlı API çıktısı bu ortak hesaplamayı kullanır.
    /// </summary>
    public interface IVGYumurtlamaCalculationService
    {
        Task<VGYumurtlamaViewModel> CalculateAsync(
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
    }
}
