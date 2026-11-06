using ObjectMonitoring.Domain.DbViewModels;
using ObjectMonitoring.Domain.Entities;
using ObjectMonitoring.Domain.Models.ReportModels;

namespace ObjectMonitoring.Domain.Interfaces
{
    public interface IDebimetreRepository
    {
        Task OzetGuncelleAsync(byte periyotTipi, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
        Task<List<DebimetreRaporVeriModel>> GetDebimetreRaporVerileriAsync(List<int> lokasyonIds, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip, CancellationToken cancellationToken);
        Task<List<Debimetre>> GetAktifDebimetrelerByLokasyonIdsAsync(List<int> lokasyonIds, CancellationToken cancellationToken);
        Task<List<vwLokasyon>> GetLokasyonlarFromLokasyonIds(List<int> lokasyonIds);
    }
}
