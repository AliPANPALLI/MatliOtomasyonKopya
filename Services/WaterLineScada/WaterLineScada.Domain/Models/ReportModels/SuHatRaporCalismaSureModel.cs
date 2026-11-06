namespace WaterLineScada.Domain.Models.ReportModels
{
    public class SuHatRaporCalismaSureModel
    {
        public int SuKaynakId { get; set; }
        public DateTime Periyot { get; set; }
        public decimal ToplamSaat { get; set; }
        public DateTime? IlkZaman { get; set; }
        public DateTime? SonZaman { get; set; }
    }
}
