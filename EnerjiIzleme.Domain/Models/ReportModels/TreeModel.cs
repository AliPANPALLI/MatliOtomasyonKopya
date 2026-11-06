namespace EnerjiIzleme.Domain.Models.ReportModels
{
    public class TreeModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Checked { get; set; }
        public bool Expanded { get; set; }
        public string Type { get; set; }
        public bool Write { get; set; }
        public bool Read { get; set; }
        public bool Approved { get; set; }
        public List<TreeModel>? Children { get; set; }
        public List<int>? ChildrenCihazlar { get; set; }
        public int? ParentCihaz { get; set; }
    }
}
