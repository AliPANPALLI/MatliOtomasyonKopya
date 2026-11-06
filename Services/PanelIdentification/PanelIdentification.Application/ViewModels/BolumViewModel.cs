namespace PanelIdentification.Application.ViewModels
{
    public class BolumViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int LokasyonId { get; set; }
        /// <summary>
        /// Parent varsa Bolum Id bilgisi, aksi halde 0
        /// </summary>
        public int ParentBolumId { get; set; }
    }
}
