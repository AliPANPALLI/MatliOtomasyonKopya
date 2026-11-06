namespace SharedApp.Application.ViewModels
{
    public class FormViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Aciklama { get; set; }
        public string Link { get; set; }
        public int ModulId { get; set; }
        public string? ModulName { get; set; }
        public bool? TopbarNesnesi { get; set; }
        public int? Sira { get; set; }
        public bool? Silindi { get; set; }
    }
}
