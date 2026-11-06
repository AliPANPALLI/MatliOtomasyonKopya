namespace SharedApp.Application.ViewModels
{
    public class MenuItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Icon { get; set; } // Eğer ikonu tabloda tutmak istemiyorsanız burayı kaldırabilirsiniz.
        public bool? Active { get; set; }
        public string? Type { get; set; }
        public string? Link { get; set; }
        public bool? TopbarNesnesi { get; set; }
        public string? MenuIdType { get; set; }

        public List<MenuItem>? SubItems { get; set; }
    }
}
