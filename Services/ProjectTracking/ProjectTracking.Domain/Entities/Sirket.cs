namespace ProjectTracking.Domain.Entities;

public sealed class Sirket
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public bool Aktif { get; set; } = true;
    public DateTime OlusturulmaUtc { get; set; }
    public DateTime? GuncellemeUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<Tesis> Tesisler { get; set; } = [];
}
