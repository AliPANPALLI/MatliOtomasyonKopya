namespace EnergyMonitoring.Domain.Models
{
    public class EnerjiMaliyetParametreler
    {
        public int Id { get; set; }
        public int Yil { get; set; }
        public int Ay { get; set; }
        public decimal? Yekdem { get; set; }      // TL/MWh
        public decimal? DagitimBedeli { get; set; }     // TL/MWh
        public decimal? AnlasmaOrani { get; set; } // 0.05 = %5      
        public int? LokasyonId { get; set; }
        public decimal? AktifEnerjiBedeli { get; set; }
        public decimal? GucBedeli { get; set; }
        public decimal? Gunduz { get; set; }
        public decimal? Puant { get; set; }
        public decimal? Gece { get; set; }
        public bool? TekZamanliTarimsal { get; set; }
        public bool? UcZamanliTarimsal { get; set; }
        public bool? Genel { get; set; }
        public int? EnerjiMaliyetGrupId { get; set; }
    }
}