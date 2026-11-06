using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Domain.Interfaces
{
    public interface IAlarmKullaniciEslemeRepository : IRepository<AlarmKullaniciEsleme>
    {
        Task<List<vwAlarmKullaniciEsleme>> GetAllvwAlarmKullaniciEsleme();
    }
}
