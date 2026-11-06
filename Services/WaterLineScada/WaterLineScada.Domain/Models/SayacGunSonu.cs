namespace WaterLineScada.Domain.Models
{
    public class SayacGunSonu
    {
        public int Id { get; set; }
        public string SayacAdi { get; set; }
        public DateTime? KayitZamani { get; set; }
        public decimal? SayacDegeri { get; set; }
    }
}