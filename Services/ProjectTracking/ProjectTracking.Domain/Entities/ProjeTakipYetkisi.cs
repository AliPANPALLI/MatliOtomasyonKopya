namespace ProjectTracking.Domain.Entities;

public sealed class ProjeTakipYetkisi
{
    public int Id { get; set; }
    public string KullaniciId { get; set; } = string.Empty;
    public string KullaniciAdi { get; set; } = string.Empty;
    public bool Goruntule { get; set; }
    public bool Duzenle { get; set; }
    public bool MaliyetIcmalGor { get; set; }
    public bool IcmalDuzenle { get; set; }
    public bool SilArsivle { get; set; }
    public bool YetkiYonet { get; set; }
    public bool NotDuzenle { get; set; }
    public DateTime OlusturulmaUtc { get; set; }
    public DateTime? GuncellemeUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
