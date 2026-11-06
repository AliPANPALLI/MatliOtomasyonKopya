using MachineMonitoring.Domain.Models.ReportModels;

namespace MachineMonitoring.Application.Interfaces
{
    public interface IMakineIzlemeAppService
    {
        Task<List<MachineDashboardDto>> BuildDashboardData(int bolumId, CancellationToken ct);
        Task<List<MachineLogRowDto>> GetMachineLogs(int machineId, int take);
    }
}
