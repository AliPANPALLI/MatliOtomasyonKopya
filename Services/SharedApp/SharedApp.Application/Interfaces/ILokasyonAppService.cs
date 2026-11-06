using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Application.Abstractions;

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
