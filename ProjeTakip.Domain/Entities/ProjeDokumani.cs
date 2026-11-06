namespace ProjeTakip.Domain.Entities;

public sealed class ProjeDokumani
{
    public int Id { get; set; }
    public int ProjeId { get; set; }
    public string Tur { get; set; } = "diger";
    public string Ad { get; set; } = string.Empty;
    public string BelgeNo { get; set; } = string.Empty;
    public string Revizyon { get; set; } = string.Empty;
    public string Durum { get; set; } = "taslak";
    public DateOnly? BelgeTarihi { get; set; }
    public DateOnly? GecerlilikTarihi { get; set; }
    public decimal? Tutar { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public string? DosyaAdi { get; set; }
    public string? IcerikTipi { get; set; }
    public long DosyaBoyutu { get; set; }
    public byte[]? DosyaIcerigi { get; set; }
    public DateTime OlusturulmaUtc { get; set; }
    public DateTime? GuncellemeUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public IsAdimi Proje { get; set; } = null!;
    public ICollection<IcmalKalemi> IcmalKalemleri { get; set; } = [];
}
