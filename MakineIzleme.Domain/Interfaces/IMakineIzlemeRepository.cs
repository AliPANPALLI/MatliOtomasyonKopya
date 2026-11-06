using MakineIzleme.Domain.Models.ReportModels;

namespace MakineIzleme.Domain.Interfaces
{
    public interface IMakineIzlemeRepository
    {
        Task<List<MachineDashboardDto>> BuildDashboardData(int bolumId, CancellationToken ct);
        Task<List<MachineLogRowDto>> GetMachineLogs(int machineId, int take);
    }
}
