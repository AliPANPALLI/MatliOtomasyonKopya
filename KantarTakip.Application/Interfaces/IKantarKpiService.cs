using KantarTakip.Application.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KantarTakip.Application.Interfaces
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