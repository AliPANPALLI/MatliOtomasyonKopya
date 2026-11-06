using SharedKernel.Core;
using WaterLineScada.Domain.Models;
using WaterLineScada.Domain.Models.ReportModels;

namespace WaterLineScada.Domain.Interfaces
{
    public interface ISayacRepository : IRepository<Sayac>
    {
        //Task<List<EtiketLog>> GetEtiketLogVerilerFromSuKaynakId(List<int> sayacIds, DateTime baslangicZamani, DateTime bitisZamani);
        //Task<List<SuKaynakLog>> GetSuKaynakLogVerilerFromSayacId(List<int> sayacIds, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<EtiketLogYeni>> GetEtiketLogVerilerFromSuKaynakId(List<int> sayacIds, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<SuKaynakLogYeni>> GetSuKaynakLogVerilerFromSayacId(List<int> sayacIds, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<SayacPeriyotData2>> GetToplamSuDataFromViewToplu2(string periyot, List<int> sayacIds, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<List<TankPeriyotData2>> GetTankSuDataFromViewToplu2(string periyot, List<int> suKaynakIds, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<List<EtiketLogYeni>> GetEtiketLogVeriler2(List<int> suKaynakIds, List<int> etiketIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken = default);
    }
}
