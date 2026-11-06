namespace ObjectMonitoring.Domain.Entities;

public class DebimetreOzet
{
    public long Id { get; set; }
    public int DebimetreId { get; set; }
    public byte PeriyotTipi { get; set; }
    public DateTime PeriyotBaslangic { get; set; }
    public DateTime PeriyotBitis { get; set; }
    public decimal? IlkPozitifToplam { get; set; }
    public decimal? SonPozitifToplam { get; set; }
    public decimal? Tuketim { get; set; }
    public decimal? OrtalamaDebi { get; set; }
    public decimal? MinimumDebi { get; set; }
    public decimal? MaksimumDebi { get; set; }
    public decimal? SonDebi { get; set; }
    public int BasariliOkumaSayisi { get; set; }
    public int HataliOkumaSayisi { get; set; }
    public DateTime HesaplamaZamani { get; set; }

    public Debimetre Debimetre { get; set; } = null!;
}
