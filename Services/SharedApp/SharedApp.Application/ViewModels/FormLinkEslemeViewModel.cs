namespace SharedApp.Application.ViewModels
{
    public class FormLinkEslemeViewModel
    {
        public int Id { get; set; }
        public int FormId { get; set; }
        public string? FormName { get; set; }
        public string? ModulName { get; set; }
        public string Link { get; set; }
        public string? Ad { get; set; }
        public int? SirketId { get; set; }
        public string? SirketName { get; set; }
        public bool? SidebarGoster { get; set; }
        public int? Sira { get; set; }
        public bool? Silindi { get; set; }
    }
}
