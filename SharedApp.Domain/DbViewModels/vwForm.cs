namespace SharedApp.Domain.DbViewModels
{
    public class vwForm
    {
        public int FormId { get; set; }
        public string? FormName { get; set; }
        public string? FormLink { get; set; }
        public int ModulId { get; set; }
        public bool? TopbarNesnesi { get; set; }
        public int? FormSira { get; set; }
        public bool? FormSilindi { get; set; }
        public int? ParentId { get; set; }
        public bool? SingleTopbarNesnesi { get; set; }
        public string? ModulName { get; set; }
        public string? ModulLink { get; set; }
        public int? ModulSira { get; set; }
        public string? YetkiTablosu { get; set; }
        public bool? ModulSilindi { get; set; }
    }
}
