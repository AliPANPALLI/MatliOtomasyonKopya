using KumesScada.Application.RequestModels;
using KumesScada.Application.ViewModels;

namespace KumesScada.Application.Interfaces
{
    public interface IVGExcelImportService
    {
        Task<VGExcelImportViewModel> ImportAsync(
            VGExcelImportRequest request,
            CancellationToken cancellationToken = default);
    }
}