using ObjectMonitoring.Domain.Models.ReportModels;

namespace ObjectMonitoring.Application.Interfaces
{

    public interface IDebimetreAppService
    {
        Task<List<SuKaynakRaporModel>> GetDebimetreRaporByLokasyonIds(List<int> lokasyonIds, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip, CancellationToken cancellationToken);
    }
}
