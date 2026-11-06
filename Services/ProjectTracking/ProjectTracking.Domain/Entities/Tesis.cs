namespace ProjectTracking.Domain.Entities;

public sealed class Tesis
{
    public int Id { get; set; }
    public int SirketId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? IlKodu { get; set; }
    public int Sira { get; set; }
    public bool Aktif { get; set; } = true;
    public DateTime OlusturulmaUtc { get; set; }
    public DateTime? GuncellemeUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Sirket Sirket { get; set; } = null!;
    public ICollection<IsAdimi> IsAdimlari { get; set; } = [];
}
