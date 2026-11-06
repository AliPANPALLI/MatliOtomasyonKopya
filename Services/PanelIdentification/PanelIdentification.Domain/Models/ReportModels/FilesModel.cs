namespace PanelIdentification.Domain.Models.ReportModels
{
    public class FilesModel
    {
        public int PanoId { get; set; }
        public string FileName { get; set; }
        public DateTime? FileDate { get; set; }
        public string FileExtension { get; set; }
        public string? FileDirectory { get; set; }
        public string FileUnicode { get; set; }
        public bool? OnayIslemdeMi { get; set; }
        public int? OnayIslemKayitId { get; set; }
        public bool? ShowOnaylaButton { get; set; }
    }
}