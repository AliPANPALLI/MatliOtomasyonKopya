namespace ProjeTakip.Domain.Entities;

public sealed class Sorumlu
{
    public int Id { get; set; }
    public string AdSoyad { get; set; } = string.Empty;
    public string? HariciKullaniciId { get; set; }
    public bool Aktif { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public ICollection<IsAdimiSorumlu> IsAdimiSorumlular { get; set; } = [];
}
