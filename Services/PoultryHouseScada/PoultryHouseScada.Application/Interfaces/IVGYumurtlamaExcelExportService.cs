using PoultryHouseScada.Application.ViewModels;

namespace PoultryHouseScada.Application.Interfaces
{
    public interface IVGYumurtlamaExcelExportService
    {
        byte[] Export(VGYumurtlamaAllReportViewModel report);
    }
}