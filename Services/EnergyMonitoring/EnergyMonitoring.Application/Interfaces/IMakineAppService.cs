using EnergyMonitoring.Application.ViewModels;
using EnergyMonitoring.Domain.Models;
using SharedKernel.Core;

namespace EnergyMonitoring.Application.Interfaces
{
    public interface IMakineAppService : IAppService<MakineViewModel>
    {
        Task SetSilindi(int id);
        Task EditMakine(MakineViewModel makineViewModel, string userId);
        Task AddMakine(MakineViewModel makineViewModel, string userId);
        Task<List<Makine>> GetAllMakineByLokasyonId(int lokasyonId);
        Task<MakineViewModel> GetvwMakineById(int id);
        Task<bool> OnaylaMakine(int id, string userId);
    }
}