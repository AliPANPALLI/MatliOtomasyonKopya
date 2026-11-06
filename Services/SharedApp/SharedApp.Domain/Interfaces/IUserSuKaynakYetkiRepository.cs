using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Domain.Interfaces
{
    public interface IUserSuKaynakYetkiRepository : IRepository<UserSuKaynakYetki>
    {
        Task<List<vwSuHatCizimSirketForYetki>> GetvwSuHatCizimSirketForYetkiByCizimIds(List<int> cizimIds);
        Task<List<vwSuHatCizimSirketForYetki>> GetAllvwSuHatCizimSirket();
        Task<List<vwUserSuKaynakYetki>> GetAllvwUserSuKaynakYetki();
    }
}
