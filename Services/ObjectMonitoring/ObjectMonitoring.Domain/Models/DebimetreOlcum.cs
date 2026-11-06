namespace ObjectMonitoring.Domain.Entities;

public class DebimetreOlcum
{
    public long Id { get; set; }
    public int DebimetreId { get; set; }
    public DateTime OkunmaZamani { get; set; }
    public DateTime KayitZamani { get; set; }
    public decimal? Debi { get; set; }
    public decimal? PozitifToplam { get; set; }
    public decimal? NegatifToplam { get; set; }
    public byte Durum { get; set; }
    public string? HataMesaji { get; set; }

    public Debimetre Debimetre { get; set; } = null!;
}
