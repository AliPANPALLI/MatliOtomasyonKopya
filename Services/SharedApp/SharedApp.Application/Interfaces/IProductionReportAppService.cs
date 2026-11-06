using SharedApp.Application.ViewModels.ReportViewModels;
using SharedApp.Application.ViewModels.RequestModel;

namespace SharedApp.Application.Interfaces
{
    public interface IProductionReportAppService
    {
        Task<List<ProductionReportViewModel>> GetProductionReportAsync(ProductionReportRequestModel request, CancellationToken cancellationToken);
    }
}
