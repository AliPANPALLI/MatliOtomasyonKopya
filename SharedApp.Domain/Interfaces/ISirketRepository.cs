using SharedApp.Domain.Models;
using SharedApp.Domain.Models.ReportModels;
using SharedKernel.Core;

namespace SharedApp.Domain.Interfaces
{
    public interface ISirketRepository : IRepository<Sirket>
    {
        Task<List<int>> GetLokasyonIdsBySirketIds(List<int> sirketIds);
        Task<List<Tesis>> GetTesislerByLokasyonId(int lokasyonId);
        Task<List<Bolum>> GetBolumlerByTesisId(int tesisId);
        Task<List<Tbl_Makine>> GetMakinelerByBolumId(int bolumId);
        Task<List<Tbl_Nesne>> GetNesnelerByMakineId(int makineId);
        Task AddLokasyon(Lokasyon lokasyon);
        Task AddTesis(Tesis tesis);
        Task AddBolum(Bolum bolum);
        Task AddMakine(Tbl_Makine tbl_Makine, CancellationToken ct = default);
        Task AddNesne(Tbl_Nesne tbl_Nesne, CancellationToken ct = default);
    }
}
