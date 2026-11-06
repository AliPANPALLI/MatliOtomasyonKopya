namespace PanoKimliklendirme.Domain.Models.ReportModels
{
    public class NodeYetkiModel
    {
        public int NodeId { get; set; }
        public string NodeType { get; set; }
        public bool Write { get; set; }
        public bool Read { get; set; }
        public bool Responsible { get; set; }
    }
}
