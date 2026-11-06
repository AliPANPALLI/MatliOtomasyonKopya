using KumesScada.Domain.Models.Excel;

namespace KumesScada.Application.Interfaces
{
    public interface IRaporVeriExcelReader
    {
        RaporVeriExcelOkumaSonucu Read(Stream stream, string fileName);
    }
}
