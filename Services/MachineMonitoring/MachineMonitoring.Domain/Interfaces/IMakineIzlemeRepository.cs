using MachineMonitoring.Domain.Models.ReportModels;

namespace MachineMonitoring.Domain.Interfaces
{
    public interface IMakineIzlemeRepository
    {
        Task<List<MachineDashboardDto>> BuildDashboardData(int bolumId, CancellationToken ct);
        Task<List<MachineLogRowDto>> GetMachineLogs(int machineId, int take);
    }
}
