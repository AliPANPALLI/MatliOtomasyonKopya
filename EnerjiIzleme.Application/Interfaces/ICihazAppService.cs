using EnerjiIzleme.Application.ViewModels;
using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Models;
using SharedKernel.Core;

namespace EnerjiIzleme.Application.Interfaces
{
    public interface ICihazAppService : IAppService<CihazViewModel>
    {
        Task SetSilindi(int id);
        Task EditCihaz(CihazViewModel cihazViewModel, string userId);
        Task AddCihaz(CihazViewModel cihazViewModel, string userId);
        Task<List<vwCihaz>> GetAllCihaz(int lokasyonId);
        Task<List<CihazModel>> GetAllCihazModel();
        Task<List<Gateway>> GetAllGateway();
        Task<CihazViewModel> GetvwCihazOnayById(int id);
        Task<bool> OnaylaCihaz(int id, string userId);
    }
}