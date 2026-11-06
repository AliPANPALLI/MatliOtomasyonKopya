namespace SharedApp.Domain.Models.ReportModels
{
    public class TreeModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Checked { get; set; }
        public bool Expanded { get; set; }
        public string Type { get; set; }
        public string? Info { get; set; }
        public List<TreeModel>? Children { get; set; }
    }
}