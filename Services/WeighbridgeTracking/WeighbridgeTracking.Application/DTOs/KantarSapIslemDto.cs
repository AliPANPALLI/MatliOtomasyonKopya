
using System;

namespace WeighbridgeTracking.Application.DTOs
{
    public sealed class KantarSapIslemDto
    {
        public string? FisNo { get; set; }
        public int? KantarId { get; set; }
        public string? Plaka { get; set; }

        public DateTime? IlkTartimZamani { get; set; }
        public DateTime? IkinciTartimZamani { get; set; }

        public decimal? IlkAgirlik { get; set; }
        public decimal? IkinciAgirlik { get; set; }
        public decimal? NetAgirlik { get; set; }

        public bool Tamamlandi { get; set; }

        public double? IslemSuresiDakika { get; set; }

        public string Durum { get; set; } = "";
    }
}
