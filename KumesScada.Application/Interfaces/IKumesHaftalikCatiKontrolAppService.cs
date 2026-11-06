using KumesScada.Application.ViewModels;
using SharedKernel.Core;

namespace KumesScada.Application.Interfaces
{
    public interface IKumesHaftalikCatiKontrolAppService : IAppService<KumesHaftalikCatiKontrolViewModel>
    {
        Task<List<KumesHaftalikCatiKontrolViewModel>> GetKumesCatiKontrolHaftalari(int kumesId);
        Task SaveKumesHaftalikCatiKontrol(KumesHaftalikCatiKontrolViewModel model, string? userId);
        //Task<KumesHaftalikCatiKontrolViewModel?> GetKumesHaftalikCatiKontrol(int kumesId, int? donemId, int haftaSayisi);
        //Task SaveKumesHaftalikCatiKontrol(KumesHaftalikCatiKontrolViewModel model, string userId);
    }
}