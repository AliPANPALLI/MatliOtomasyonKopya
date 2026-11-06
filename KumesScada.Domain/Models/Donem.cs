namespace KumesScada.Domain.Models
{
    public class Donem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? KumesId { get; set; }
        public DateTime? GirisTarihi { get; set; }
        public DateTime? CikisTarihi { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
        public string? GuncelleyenUserId { get; set; }
        public string? GuncellemeIslemi { get; set; }
        public string? Aciklama { get; set; }
        public bool? Aktif { get; set; }
        public bool? Silindi { get; set; }
        public int? HayvanAdet { get; set; }
        public string? Cins { get; set; }
        public int? HayvanCinsId { get; set; }
        public int? Yas { get; set; }
        public short? SaglikDurumu { get; set; }
    }
}