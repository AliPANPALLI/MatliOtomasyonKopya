using PoultryHouseScada.Domain.Models.Excel;

namespace PoultryHouseScada.Application.Interfaces
{
    public interface IVGExcelReader
    {
        Task<VGExcelImportResult> ReadAsync(
            Stream stream,
            string fileName,
            int? year = null,
            CancellationToken cancellationToken = default);
    }
}