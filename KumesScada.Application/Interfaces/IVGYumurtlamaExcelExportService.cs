using KumesScada.Application.ViewModels;

namespace KumesScada.Application.Interfaces
{
    public interface IVGYumurtlamaExcelExportService
    {
        byte[] Export(VGYumurtlamaAllReportViewModel report);
    }
}