using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.ViewModels;
using SharedKernel.Core;

namespace PoultryHouseScada.Application.Interfaces
{
    public interface IKumesRaporRehberAppService : IAppService<KumesRaporRehberViewModel>
    {
        Task<List<KumesRaporRehberViewModel>> GetByLokasyonId(int lokasyonId);
        Task<List<KumesRaporRehberViewModel>> GetByLokasyonIdAndGuideTipi(int lokasyonId, string guideTipi,int hayvanCinsId);
        Task SaveGuides(KumesRapoRehberSaveRequest request, string userId);
        Task DeleteGuide(int id, string userId);
        List<string> GetGuideTipleri();
    }
}