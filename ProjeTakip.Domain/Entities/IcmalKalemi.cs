namespace ProjeTakip.Domain.Entities;

public sealed class IcmalKalemi
{
    public int Id { get; set; }
    public int ProjeDokumaniId { get; set; }
    public int? IsAdimiId { get; set; }
    public int SiraNo { get; set; }
    public string PozNo { get; set; } = string.Empty;
    public string Tanim { get; set; } = string.Empty;
    public string Birim { get; set; } = "adet";
    public decimal Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public decimal Tutar { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public DateTime OlusturulmaUtc { get; set; }
    public DateTime? GuncellemeUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ProjeDokumani ProjeDokumani { get; set; } = null!;
    public IsAdimi? IsAdimi { get; set; }
}
