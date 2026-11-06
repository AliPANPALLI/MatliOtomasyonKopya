using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Models;
using EnerjiIzleme.Domain.Models.ReportModels;
using MongoDB.Bson;
using static SharedKernel.ApplicationConstants;

namespace EnerjiIzleme.Domain.Interfaces
{
    public interface ILogsRepository
    {
        Task<List<BsonDocument>> GetTuketimVerileriByCihazId(int cihazId, CancellationToken cancellationToken);
        Task<BsonDocument> GetTuketimEnerjiVerileriByCihazId(int cihazId, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
        Task<List<BsonDocument>> GetTuketimEnerjiVerileriByCokluCihazId(List<int> cihazIdList, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
        Task<Logs> GetTuketimSonVeriByCihazId(int cihazId, CancellationToken cancellationToken);
        Task<(Logs? ToplamVeri, List<int> State2Cihazlar)> GetToplamVeriByCihazIds(List<int> cihazIds, CancellationToken cancellationToken);
        Task<(Logs? ToplamVeri, List<int> State2Cihazlar)> GetToplamVeriByCihazIdsTest(List<int> cihazIds, CancellationToken cancellationToken);
        Task<List<EnerjiRaporGrafikModel>> GetTuketimVerileriForEnerjiGrafikRapor(vwCihaz cihaz, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval, CancellationToken ct);
        Task<List<EnerjiRaporGrafikModel>> GetTuketimVerileriForEnerjiGrafikRaporByCokluCihazId(List<vwCihaz> cihazlar, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval, CancellationToken cancellationToken);
        Task<List<BsonDocument>> GetSaatlikTuketimFromMongoAsync(int cihazId, DateTime baslangic, DateTime bitis, CancellationToken cancellationToken);
        Task<List<BsonDocument>> GetSaatlikTuketimFromMongoAsync(int cihazId, DateTime baslangic, DateTime bitis, TarifeTuru tarifeTuru, CancellationToken cancellationToken);
        Task<List<BsonDocument>> GetFirstLastByCihazIdsAsync(List<int> cihazIds, DateTime start, DateTime end, CancellationToken ct);
        Task<List<HaftalikSayacTuketimModel>> GetSayacTuketimleri(List<vwCihaz> secilenCihazlar, List<vwCihaz> tumCihazlar, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
        Task<(decimal? IlkImportAktifEnerji, decimal? IlkExportAktifEnerji, DateTime? IlkOkunmaZamani)> GetIlkOkunanImportAktifEnerjiAsync(int cihazId, DateTime tarih, CancellationToken cancellationToken);
        //Task<ToplamTuketimSonucu> GetToplamImportAktifEnerjiTuketimiAsync(List<int> cihazIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
        Task<EnerjiHaftalikRaporModel> GetEnerjiRaporu(List<vwCihaz> tumCihazlar, List<int> secilenParentCihazIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken);
        Task<EnerjiHaftalikRaporModel> GetEnerjiRaporuDonemli(List<vwCihaz> tumCihazlar, List<int> secilenParentCihazIds, DateTime baslangicZamani, DateTime bitisZamani, DateTime oncekiDonemBaslangicZamani, DateTime oncekiDonemBitisZamani, CancellationToken cancellationToken);
        byte[] TamRaporOlustur(TamRaporRequest data);
        Task<CihazTuketimSonucu> GetGesCihazUretimiAsync(int cihazId, string? cihazName, DateTime sorguBaslangic, DateTime sorguBitis, CancellationToken cancellationToken);
        Task<CihazTuketimSonucu> GetNormalCihazTuketimiAsync(int cihazId, string? cihazName, DateTime sorguBaslangic, DateTime sorguBitis, CancellationToken cancellationToken);
    }
}