namespace KumesScada.Domain.Models
{
    public class KumesHayvan
    {
        public int Id { get; set; }
        public int? KumesId { get; set; }
        public int? DonemId { get; set; }
        public string? Cins { get; set; }
        public int? Yas { get; set; }
        public short? SaglikDurumu { get; set; }
        public int? Adet { get; set; }
        public bool? Aktif { get; set; }
        public bool? Silindi { get; set; }
    }
}