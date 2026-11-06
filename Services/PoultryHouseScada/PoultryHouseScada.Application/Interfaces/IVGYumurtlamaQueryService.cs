using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.ViewModels;

namespace PoultryHouseScada.Application.Interfaces
{
    public interface IVGYumurtlamaQueryService
    {
        Task<VGYumurtlamaViewModel> GetAsync(
            VGYumurtlamaQueryRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Yumurtlama Takip ekranının üstteki yatay kümes seçicisi için,
        /// "Kumesler" tablosundaki (VG modülüne özel) kayıtları verilen
        /// lokasyona göre döner.
        /// </summary>
        Task<List<VGYumurtlamaKumesSecenegiViewModel>> GetKumeslerAsync(
            int lokasyonId,
            CancellationToken cancellationToken = default);
    }
}   