using KumesScada.Application.ViewModels;

namespace KumesScada.Application.Interfaces
{
    public interface IVGYumurtlamaReportService
    {
        Task<VGYumurtlamaAllReportViewModel> BuildAsync(
            Stream yemHesaplariStream,
            string yemHesaplariFileName,
            Stream? tasnifStream,
            string? tasnifFileName,
            int year,
            CancellationToken cancellationToken = default);
    }
}