namespace SharedApp.Domain.Models.ReportModels
{
    public class Tbl_Nesne
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Makine_Id { get; set; }
        public string? Aciklama { get; set; }
    }
}
