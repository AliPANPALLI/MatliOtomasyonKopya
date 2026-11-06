namespace ObjectMonitoring.Domain.Entities;

public class DebimetreAnlikDurum
{
    public int DebimetreId { get; set; }
    public DateTime? OkunmaZamani { get; set; }
    public DateTime? SonBasariliOkunmaZamani { get; set; }
    public decimal? Debi { get; set; }
    public decimal? PozitifToplam { get; set; }
    public decimal? NegatifToplam { get; set; }
    public DateOnly? TuketimTarihi { get; set; }
    public decimal? BugunTuketim { get; set; }
    public byte Durum { get; set; }
    public bool TuketimGecerli { get; set; }
    public bool SayacSifirlandi { get; set; }
    public DateTime GuncellemeZamani { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public decimal? DununSonPozitifToplam { get; set; }

    public Debimetre Debimetre { get; set; } = null!;
}
