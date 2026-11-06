namespace HegCoolingScada.Domain.Models
{
    public class OdaVanasi
    {
        public short Id { get; set; }
        public short? OdaId { get; set; }
        public string PlcTag { get; set; } = string.Empty;
        public string? PlcAdres { get; set; }
        public string? Aciklama { get; set; }
        public string? EkipmanKodu { get; set; }
    }
}
