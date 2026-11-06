namespace HegSogutmaScada.Domain.Models
{
    public class Kompresor
    {
        public short Id { get; set; }
        public string TagNo { get; set; } = string.Empty;
        public string Ad { get; set; } = string.Empty;
        public string PlcKapasiteTag { get; set; } = string.Empty;
        public string PlcCalismaSaatiTag { get; set; } = string.Empty;
        public string? PlcKapasiteAdres { get; set; }
        public string? PlcCalismaSaatiAdres { get; set; }
    }
}
