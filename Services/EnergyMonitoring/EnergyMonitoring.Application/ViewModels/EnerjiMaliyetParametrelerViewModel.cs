namespace EnergyMonitoring.Application.ViewModels
{
    public class EnerjiMaliyetParametrelerViewModel
    {
        public int? Yil { get; set; }
        public int? Ay { get; set; }
        public decimal? Yekdem { get; set; }      // TL/MWh
        public decimal? DagitimBedeli { get; set; }     // TL/MWh
        public decimal? AnlasmaOrani { get; set; } // 0.05 = %5     
        public decimal? AktifEnerjiBedeli { get; set; }
        public decimal? GucBedeli { get; set; }
        public decimal? Gunduz { get; set; }
        public decimal? Puant { get; set; }
        public decimal? Gece { get; set; }
    }
}
