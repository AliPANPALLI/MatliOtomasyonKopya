using PanelIdentification.Application.ViewModels;
using PanelIdentification.Domain.Models;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace PanelIdentification.Application.Interfaces
{
    public interface IMakineAppService : IAppService<MakineViewModel>
    {
        Task SetSilindi(int id, string userId);
        Task<List<int>> GetLokasyonIdsByMakineIds(List<int> makineIds);
        Task EditMakine(MakineViewModel makineViewModel, string userId);
        Task<List<Makine>> GetAllMakineForYetki();
    }
}
