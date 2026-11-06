namespace EnerjiIzleme.Domain.Models
{
    public class payload
    {
        public int okunan_analizor_id { get; set; }
        public DateTime okunma_zamani { get; set; }
        public veri veri { get; set; }
        public int state { get; set; }
    }
}
