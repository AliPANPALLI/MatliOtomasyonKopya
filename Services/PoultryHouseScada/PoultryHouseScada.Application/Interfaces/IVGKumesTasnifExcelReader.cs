using PoultryHouseScada.Domain.Models.Excel;

namespace PoultryHouseScada.Application.Interfaces
{
    /// <summary>
    /// "Kümes Tasnif Sayımları" dosyasını okur. Bu dosyada her gün ayrı
    /// bir sayfadadır (sayfa adı "gg.aa.yy"); sayfa içinde satırlar sabit
    /// etiketli metrikler, sütunlar ise kümeslerdir (MOBA toplam/boş
    /// sütunlar atlanır).
    /// </summary>
    public interface IVGKumesTasnifExcelReader
    {
        Task<VGExcelImportResult> ReadAsync(
            Stream stream,
            string fileName,
            int? year = null,
            CancellationToken cancellationToken = default);
    }
}
