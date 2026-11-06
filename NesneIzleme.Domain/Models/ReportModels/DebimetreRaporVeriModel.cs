using Microsoft.EntityFrameworkCore;

namespace NesneIzleme.Domain.Models.ReportModels
{
    [Keyless]
    public class DebimetreRaporVeriModel
    {
        public int DebimetreId { get; set; }

        public DateTime PeriyotBaslangic { get; set; }

        public decimal? Toplam { get; set; }
    }
}
