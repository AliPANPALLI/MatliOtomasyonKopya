namespace KumesScada.Domain.Models
{
    public class KumesEnerjiLog
    {
        public int Id { get; set; }
        public int? LokasyonId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public decimal? ImportAktifEnerji { get; set; }
        public decimal? TuketimBedeli{ get; set; }
        public bool? Silindi { get; set; }
    }
}