using KumesScada.Application.RequestModels;
using KumesScada.Application.ViewModels;
using SharedKernel.Core;

namespace KumesScada.Application.Interfaces
{
    public interface IRaporVeriAppService : IAppService<RaporVeriViewModel>
    {
        Task<List<RaporVeriViewModel>> GetByKategoriAdi(string kategoriAdi);
        Task SaveGuides(RaporVeriSaveRequest request);
        Task DeleteGuide(int id);
        Task<List<string>> GetKategoriler();

        /// <summary>
        /// Kümesin ana excel'indeki "VERİLER" sayfasını okuyup "HEDEF %" /
        /// "HEDEF GR" hedef eğrilerini bu kümese özel kategoriler olarak
        /// kaydeder (bkz. RaporVeriAppService.ImportFromExcelAsync).
        /// </summary>
        Task<RaporVeriExcelImportResultViewModel> ImportFromExcelAsync(
            Stream stream, string fileName, string kumesNo,
            CancellationToken cancellationToken = default);
    }
}
