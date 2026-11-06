using KumesScada.Application.ViewModels;

namespace KumesScada.Application.Interfaces
{
    /// <summary>
    /// Verim Gıda'nın dış entegrasyonu için, veritabanında kayıtlı günlük
    /// verilerden tek bir kümesin tam "Yumurtlama Kartı" verisini Verim
    /// Gıda'nın beklediği JSON paketi şeklinde üretir — bkz.
    /// VGYumurtlamaGenelVeriViewModel.
    /// </summary>
    public interface IVGYumurtlamaGenelVeriService
    {
        Task<VGYumurtlamaGenelVeriViewModel> GetAsync(
            int kumesId,
            CancellationToken cancellationToken = default);
    }
}
