using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.ViewModels;

namespace PoultryHouseScada.Application.Interfaces
{
    public interface IVGExcelImportService
    {
        Task<VGExcelImportViewModel> ImportAsync(
            VGExcelImportRequest request,
            CancellationToken cancellationToken = default);
    }
}