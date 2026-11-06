namespace HegCoolingScada.Domain.Models
{
    public class KompresorAnlik
    {
        public short KompresorId { get; set; }
        public decimal? KapasiteYuzde { get; set; }
        public decimal? CalismaSaatiHam { get; set; }
        public decimal? CalismaSaatiNet { get; set; }
        public DateTime GuncellemeZamani { get; set; }
    }
}
