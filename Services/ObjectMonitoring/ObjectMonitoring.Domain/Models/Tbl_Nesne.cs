namespace ObjectMonitoring.Domain.Entities;

public class Tbl_Nesne
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int Makine_Id { get; set; }
    public string? Aciklama { get; set; }

    public Tbl_Makine Makine { get; set; } = null!;

    public ICollection<Tbl_LogNesne> Loglar { get; set; } = new List<Tbl_LogNesne>();
}