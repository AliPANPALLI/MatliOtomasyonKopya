using EnergyMonitoring.Application.ViewModels;
using EnergyMonitoring.Domain.Models;
using EnergyMonitoring.Domain.Models.ReportModels;

namespace EnergyMonitoring.Application.Interfaces
{
    public interface ITreeAppService
    {
        Task<List<TreeModel>> GetTreeByLokasyonId(int lokasyonId, bool duzenlemeModu, CancellationToken ct);
        Task<TreeModel> GetNodeTreeViewByCihazId(int cihazId, CancellationToken ct);
        Task<TreeModel> GetNodeTreeViewByCihazId2(int cihazId, CancellationToken ct);
        Task<TreeModel> GetNodeTreeViewByNodeIdAndType(int nodeId, string nodeType, CancellationToken ct);
        Task<TreeModel> GetNodeTreeViewByNodeIdAndType2(int nodeId, string nodeType, CancellationToken ct);
        Task<List<SahaVerileriTabloRaporModel>> GetSahaVerileriTabloByCihazId(int cihazId, CancellationToken ct);
        //Task<List<SahaVerileriTabloRaporModel>?> GetSahaVerileriTabloByNodeIdAndType(int nodeId, string nodeType);
        Task<SahaVerileriTabloModel> GetSahaVerileriTabloByNodeIdAndType(int nodeId, string nodeType, CancellationToken ct);
        Task<SahaVerileriTabloModel> GetSahaVerileriTabloByCihazList(List<int> cihazNodeIds, CancellationToken ct);
        //Task<List<SahaVerileriTabloRaporModel>> TEST();
        Task<List<TuketimEnerjiVerileriTabloRaporModel>> GetTuketimEnerjiVerileriTabloByCihazId(int cihazId, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken ct);
        Task<List<TuketimEnerjiVerileriTabloRaporModel>> GetTuketimEnerjiVerileriTabloByNodeIdAndType(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken ct);
        Task<List<TuketimEnerjiVerileriTabloRaporModel>> GetTuketimEnerjiVerileriTabloByCihazList(List<int> cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken ct);
        Task<List<EnerjiRaporGrafikModel>> GetEnerjiRaporFromSecilenAsync(List<string> nodeIdsAndTypes, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval, CancellationToken ct);
        Task<EnerjiHaftalikRaporModel> GetEnerjiDagilimRaporFromSecilenAsync(List<string> nodeIdsAndTypes, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken ct);
        Task<EnerjiHaftalikRaporModel> GetEnerjiDagilimRaporFromSecilenAsync2(List<string> nodeIdsAndTypes, DateTime baslangicZamani, DateTime bitisZamani, DateTime oncekiDonemBaslangicZamani, DateTime oncekiDonemBitisZamani, CancellationToken ct);
        //Task<List<EnerjiRaporGrafikModel>> GetEnerjiRaporByNodeIdAndType(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval);
        //Task<List<TuketimVerileriChartRaporModel>> GetTuketimVerileriByCihazIdEski(int cihazId);
        Task<List<TuketimVerileriChartRaporModel>> GetTuketimVerileriByCihazId(int cihazId, CancellationToken ct);
        Task<List<EnerjiKumesTesisIzlemeModel>> GetEnerjiKumesTesisIzlemeModelByCihazId(int cihazId, CancellationToken ct);
        Task<List<EnergyFarmPacket>> BuildEnergyPayloadForFarms(int kumesTipId, CancellationToken ct);
        Task<List<EnerjiMaliyetSonuc>> GetDetayliTuketimBedeli(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken ct);
        Task<long> GetTuketimBedeli(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken ct);
        Task<long> GetTuketimBedeliYeni(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken ct);
        Task<long> GetTuketimBedeliByCihazList(List<int> cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken ct);
        byte[] TamRaporOlustur(TamRaporRequest data);

        //Eski Metotlar
        Task AddEnerjiMaliyetParametreler(List<EnerjiMaliyetParametrelerViewModel> enerjiMaliyetParametreler, int seciliYil, int lokasyonId, string tarifeTipi, CancellationToken ct);
        Task<List<EnerjiMaliyetParametreler>> GetEnerjiMaliyetParametreler(int seciliYil, int lokasyonId, CancellationToken ct);

        //Yeni Metotlar
        Task<List<EnerjiMaliyetGrup>> GetEnerjiMaliyetGruplar();
        Task<EnerjiMaliyetGrup> AddEnerjiMaliyetGrup(string name);
        Task<List<EnerjiMaliyetParametreler>> GetEnerjiMaliyetParametreler(int seciliYil, int enerjiMaliyetGrupId);
        Task AddEnerjiMaliyetParametreler(List<EnerjiMaliyetParametreler> enerjiMaliyetParametreler, int seciliYil, int enerjiMaliyetGrupId, string tarifeTipi);
        Task<EnerjiMaliyetGrup> UpdateEnerjiMaliyetGrup(int id, string name);
        Task DeleteEnerjiMaliyetGrup(int id);
        //
        Task<EnerjiMaliyetAtamaTreeNodeDto?> GetEnerjiMaliyetAtamaTreeByLokasyonId(int lokasyonId, CancellationToken cancellationToken);
        Task AddEnerjiMaliyetParametreAtama(int enerjiMaliyetGrupId, int nodeId, string nodeType);
        Task RemoveEnerjiMaliyetParametreAtama(int nodeId, string nodeType);
        Task<(decimal? ImportAktifEnerjiWh, decimal? ExportAktifEnerjiWh, DateTime? KayitTarihi)> GetAktifEnerjiSayacDegeri(int cihazId, DateTime tarih, CancellationToken ct);
        //Task<ToplamTuketimSonucu?> GetToplamImportAktifEnerjiTuketimiAsync(List<int> cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
        Task<ToplamTuketimSonucu?> GetToplamImportAktifEnerjiTuketimi(List<int> cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
        Task<List<TreeModel>> GetGesDahilTreeByLokasyonId(int lokasyonId, bool duzenlemeModu, CancellationToken cancellationToken);
    }
}
