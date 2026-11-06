namespace PanoKimliklendirme.Domain.Models.ReportModels
{
    public class NodeModel
    {
        public string Name { get; set; }
        public string No { get; set; }
        public string OldNodeType { get; set; }
        /// <summary>
        /// Edit ise edit yapılacak node id, add ise üst düğümün id değeridir
        /// </summary>
        public string OldNodeId { get; set; }
        public string AddedType { get; set; }
        public string Aciklama { get; set; }
        public string DosyaYolu { get; set; }
        public int LokasyonId { get; set; }

    }
}