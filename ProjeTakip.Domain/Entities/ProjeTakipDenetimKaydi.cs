namespace ProjeTakip.Domain.Entities;

public sealed class ProjeTakipDenetimKaydi
{
    public long Id { get; set; }
    public string KullaniciId { get; set; } = string.Empty;
    public string KullaniciAdi { get; set; } = string.Empty;
    public string Islem { get; set; } = string.Empty;
    public string KayitTuru { get; set; } = string.Empty;
    public string KayitId { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string IpAdresi { get; set; } = string.Empty;
    public DateTime IslemUtc { get; set; }
}
