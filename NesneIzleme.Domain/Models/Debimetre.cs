namespace NesneIzleme.Domain.Entities;

public class Debimetre
{
    public int Id { get; set; }
    public int NodeRedSayacId { get; set; }
    public int LokasyonId { get; set; }
    public string Ad { get; set; } = null!;
    public byte? KategoriId { get; set; }
    public int? RaporSira { get; set; }
    public string? GatewayIp { get; set; }
    public short? SlaveId { get; set; }
    public string? Model { get; set; }
    public string? DebiBirimi { get; set; }
    public string? ToplamBirimi { get; set; }
    public bool Aktif { get; set; }
    public bool Silindi { get; set; }
    public DateTime EklenmeZamani { get; set; }

    public DebimetreKategori? Kategori { get; set; }
    public DebimetreAnlikDurum? AnlikDurum { get; set; }

    public ICollection<DebimetreOlcum> Olcumler { get; set; } = new List<DebimetreOlcum>();

    public ICollection<DebimetreOzet> Ozetler { get; set; } = new List<DebimetreOzet>();
}
