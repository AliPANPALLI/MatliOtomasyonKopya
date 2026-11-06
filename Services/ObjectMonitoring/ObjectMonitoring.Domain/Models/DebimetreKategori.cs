namespace ObjectMonitoring.Domain.Entities;

public class DebimetreKategori
{
    public byte Id { get; set; }
    public string Ad { get; set; } = null!;
    public bool Aktif { get; set; }

    public ICollection<Debimetre> Debimetreler { get; set; }
        = new List<Debimetre>();
}
