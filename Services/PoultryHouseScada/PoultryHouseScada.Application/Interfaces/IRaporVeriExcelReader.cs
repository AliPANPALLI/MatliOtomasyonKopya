using PoultryHouseScada.Domain.Models.Excel;

namespace PoultryHouseScada.Application.Interfaces
{
    public interface IRaporVeriExcelReader
    {
        RaporVeriExcelOkumaSonucu Read(Stream stream, string fileName);
    }
}
