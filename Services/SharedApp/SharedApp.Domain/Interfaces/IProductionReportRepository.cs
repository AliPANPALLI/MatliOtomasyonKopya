using SharedApp.Domain.Models.ReportModels;

namespace SharedApp.Domain.Interfaces
{
    public interface IProductionReportRepository
    {
        Task<List<ProductionReport>> GetProductionReportAsync(string lokasyon, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
    }
}
