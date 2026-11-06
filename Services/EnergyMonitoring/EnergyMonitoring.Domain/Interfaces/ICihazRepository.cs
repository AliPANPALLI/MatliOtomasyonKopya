using EnergyMonitoring.Domain.DbViewModels;
using EnergyMonitoring.Domain.Models;
using SharedKernel.Core;

namespace EnergyMonitoring.Domain.Interfaces
{
    public interface ICihazRepository : IRepository<Cihaz>
    {
        Task<List<Cihaz>> GetAllCihazByCihazIds(List<int> cihazIds);
        Task<List<vwCihaz>> GetAllvwCihazByCihazIds(List<int> cihazIds);
        Task<List<vwCihaz>> GetAllAktifvwCihazByCihazIds(List<int> cihazIds);
        Task<List<Cihaz>> GetAllCihazByNodeIdAndType(int nodeId, string nodeType);
        Task<List<vwCihaz>> GetvwCihazlarByNodeIdAndType(int nodeId, string nodeType);
        Task<List<Cihaz>> GetAllCihazByNodeIdAndType2(int nodeId, string nodeType);
        Task<List<vwCihaz>> GetAllCihaz(int lokasyonId);
        Task<List<vwCihaz>> GetAllAktifCihaz();
        Task<List<CihazModel>> GetAllCihazModel();
        Task<List<Gateway>> GetAllGateway();
        Task<vwCihaz_Onay> GetvwCihazOnayById(int id);
        Task AddCihaz(Cihaz cihaz);
    }
}
