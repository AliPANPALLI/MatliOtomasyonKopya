using KumesScada.Domain.Models.Excel;

namespace KumesScada.Application.Interfaces
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