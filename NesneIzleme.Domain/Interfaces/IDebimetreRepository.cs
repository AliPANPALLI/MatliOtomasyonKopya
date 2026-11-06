using NesneIzleme.Domain.DbViewModels;
using NesneIzleme.Domain.Entities;
using NesneIzleme.Domain.Models.ReportModels;

namespace NesneIzleme.Domain.Interfaces
{
    public interface IDebimetreRepository
    {
        Task OzetGuncelleAsync(byte periyotTipi, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
        Task<List<DebimetreRaporVeriModel>> GetDebimetreRaporVerileriAsync(List<int> lokasyonIds, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip, CancellationToken cancellationToken);
        Task<List<Debimetre>> GetAktifDebimetrelerByLokasyonIdsAsync(List<int> lokasyonIds, CancellationToken cancellationToken);
        Task<List<vwLokasyon>> GetLokasyonlarFromLokasyonIds(List<int> lokasyonIds);
    }
}
