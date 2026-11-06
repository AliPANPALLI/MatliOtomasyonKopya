using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Models;
using EnerjiIzleme.Domain.Models.ReportModels;

namespace EnerjiIzleme.Domain.Interfaces
{
    public interface ITreeRepository
    {
        Task<vwLokasyon> GetLokasyonById(int lokasyonId);
        //Task<List<TreeModel>> GetTreeByLokasyonId(int lokasyonId, string? lokasyonName);
        //Task AddEnerjiMaliyetParametreler(List<EnerjiMaliyetParametreler> enerjiMaliyetParametreler, int seciliYil, int lokasyonId, string tarifeTipi);
        Task<List<EnerjiMaliyetParametreler>> GetAllParametrelerByLokasyonId(int lokasyonId);
        //Task<List<EnerjiMaliyetParametreler>> GetEnerjiMaliyetParametreler(int seciliYil, int lokasyonId);
        Task<List<EnerjiMaliyetGrup>> GetEnerjiMaliyetGruplar();
        Task<EnerjiMaliyetGrup> AddEnerjiMaliyetGrup(string name);
        Task<EnerjiMaliyetGrup> UpdateEnerjiMaliyetGrup(int id, string name);
        Task DeleteEnerjiMaliyetGrup(int id);
        Task<List<EnerjiMaliyetParametreler>> GetEnerjiMaliyetParametreler(int seciliYil, int enerjiMaliyetGrupId);
        Task AddEnerjiMaliyetParametreler(List<EnerjiMaliyetParametreler> enerjiMaliyetParametreler, int seciliYil, int enerjiMaliyetGrupId, string tarifeTipi);
        Task<EnerjiMaliyetAtamaTreeNodeDto?> GetEnerjiMaliyetAtamaTreeByLokasyonId(int lokasyonId, CancellationToken cancellationToken);
        Task AddEnerjiMaliyetParametreAtama(int enerjiMaliyetGrupId, int nodeId, string nodeType);
        Task RemoveEnerjiMaliyetParametreAtama(int nodeId, string nodeType);
        //TuketimBedeli Hesaplama Metotlar
        Task<Dictionary<int, int>> GetCihazEnerjiMaliyetGrupMap(List<int> cihazIds, int lokasyonId, CancellationToken cancellationToken);
        Task<List<EnerjiMaliyetParametreler>> GetParametrelerByGrupIds(List<int> grupIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
    }
}