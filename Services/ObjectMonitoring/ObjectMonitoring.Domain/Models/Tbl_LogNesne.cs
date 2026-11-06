namespace ObjectMonitoring.Domain.Entities;

public class Tbl_LogNesne
{
    public long Id { get; set; }

    public DateTime? Timestamp { get; set; }

    public int Nesne_Id { get; set; }

    public int? Value1_Type { get; set; }

    public decimal? Value1 { get; set; }

    public int? Value2_Type { get; set; }

    public decimal? Value2 { get; set; }

    public int? Value3_Type { get; set; }

    public decimal? Value3 { get; set; }

    public DateTime? NodeRedTimestamp { get; set; }

    public Tbl_Nesne Nesne { get; set; } = null!;

    public Tbl_ValueType? Value1Type { get; set; }

    public Tbl_ValueType? Value2Type { get; set; }

    public Tbl_ValueType? Value3Type { get; set; }
}