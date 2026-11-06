namespace SharedApp.Domain.DbViewModels
{
    public class vwUserFormYetki
    {
        public string UserId { get; set; }
        public int ModulId { get; set; }
        public string ModulName { get; set; }
        public string ModulLink { get; set; }
        public int FormId { get; set; }
        public string FormName { get; set; }
        public string FormLink { get; set; }
        public bool? Degistirme { get; set; }
        public bool? Okuma { get; set; }
        public bool? Silme { get; set; }
        public bool? Yazma { get; set; }
        public bool? Silindi { get; set; }
        public bool? FormSilindi { get; set; }
        public bool? ModulSilindi { get; set; }
    }
}
