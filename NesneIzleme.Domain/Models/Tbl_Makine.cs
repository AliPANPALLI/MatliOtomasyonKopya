namespace NesneIzleme.Domain.Entities;

public class Tbl_Makine
{
    public int Id { get; set; }
    public int? Bolum_Id { get; set; }
    public string? Name { get; set; }
    public string? Aciklama { get; set; }
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public string? UretimTarihi { get; set; }
    public string? MontajTarihi { get; set; }

    public ICollection<Tbl_Nesne> Nesneler { get; set; }
        = new List<Tbl_Nesne>();
}
