using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface ILokasyonAppService : IAppService<LokasyonViewModel>
    {
        Task<List<Lokasyon>> GetAllLokasyonBySirketId(int sirketId);
        Task SetSilindi(int id);
        Task<List<Lokasyon>> GetAllLokasyonForYetki();
        Task<List<Lokasyon>> GetAllLokasyonForTicketTanim();
    }
}
