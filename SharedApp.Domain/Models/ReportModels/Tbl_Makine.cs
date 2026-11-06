namespace SharedApp.Domain.Models.ReportModels
{
    public class Tbl_Makine
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Bolum_Id { get; set; }
        public string? Aciklama { get; set; }
    }
}