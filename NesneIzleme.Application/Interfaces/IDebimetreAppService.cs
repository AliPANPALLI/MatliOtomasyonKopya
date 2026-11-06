using NesneIzleme.Domain.Models.ReportModels;

namespace NesneIzleme.Application.Interfaces
{

    public interface IDebimetreAppService
    {
        Task<List<SuKaynakRaporModel>> GetDebimetreRaporByLokasyonIds(List<int> lokasyonIds, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip, CancellationToken cancellationToken);
    }
}
