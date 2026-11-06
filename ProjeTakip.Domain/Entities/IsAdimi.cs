namespace ProjeTakip.Domain.Entities;

public sealed class IsAdimi
{
    public int Id { get; set; }
    public int TesisId { get; set; }
    public int? UstId { get; set; }
    public string IsNo { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public string Durum { get; set; } = "guncellenecek";
    public string Oncelik { get; set; } = "orta";
    public DateOnly? Baslangic { get; set; }
    public DateOnly? HedefBitis { get; set; }
    public byte Ilerleme { get; set; }
    public string Notlar { get; set; } = string.Empty;
    public string Risk { get; set; } = string.Empty;
    public string SonrakiAdim { get; set; } = string.Empty;
    public bool Aktif { get; set; } = true;
    public DateTime OlusturulmaUtc { get; set; }
    public DateTime? GuncellemeUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Tesis Tesis { get; set; } = null!;
    public IsAdimi? UstAdim { get; set; }
    public ICollection<IsAdimi> AltAdimlar { get; set; } = [];
    public ICollection<IsAdimiSorumlu> IsAdimiSorumlular { get; set; } = [];
    public ICollection<ProjeDokumani> Dokumanlar { get; set; } = [];
    public ICollection<IcmalKalemi> IcmalKalemleri { get; set; } = [];
}
