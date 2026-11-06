using WeighbridgeTracking.Application.DTOs;

namespace WeighbridgeTracking.Application.Interfaces
{
    public interface ISapKantarService
    {
        Task<List<SapKantarDto>> GetByDateAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default);

        Task<List<SapKantarDto>> GetByFisNoAsync(
            string fisNo,
            CancellationToken cancellationToken = default);
    }
}