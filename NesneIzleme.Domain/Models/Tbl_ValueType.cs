namespace NesneIzleme.Domain.Entities;

public class Tbl_ValueType
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int EnumDegeri { get; set; }

    public string? Aciklama { get; set; }

    public ICollection<Tbl_LogNesne> Value1Loglari { get; set; } = new List<Tbl_LogNesne>();

    public ICollection<Tbl_LogNesne> Value2Loglari { get; set; } = new List<Tbl_LogNesne>();

    public ICollection<Tbl_LogNesne> Value3Loglari { get; set; } = new List<Tbl_LogNesne>();
}
