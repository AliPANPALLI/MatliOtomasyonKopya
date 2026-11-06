namespace ProjeTakip.Domain.Entities;

public sealed class AdimBasligi
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public DateTime OlusturulmaUtc { get; set; }
    public DateTime? GuncellemeUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
