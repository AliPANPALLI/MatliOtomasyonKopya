using WeighbridgeTracking.Application.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WeighbridgeTracking.Application.Interfaces
{
    public interface IKantarKpiService
    {
        Task<KantarKpiRaporDto> GetRaporAsync(
            DateOnly startDate,
            DateOnly endDate,
            int? kantarId = null,
            string? plaka = null,
            int? firmaId = null,
            CancellationToken cancellationToken = default);
    }
}