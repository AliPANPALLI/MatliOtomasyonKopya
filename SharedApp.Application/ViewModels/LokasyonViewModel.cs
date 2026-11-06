namespace SharedApp.Application.ViewModels
{
    public class LokasyonViewModel
    {
        public int Id { get; set; }

        /// <summary>
        /// Varsa, SAP kodudur.
        /// </summary>
        public string? LokasyonNo { get; set; }
        public string? Name { get; set; }
        /// <summary>
        /// Lokasyonun ait oldugu sirketin sirket tablosu id bilgisidir.
        /// </summary>
        public int SirketId { get; set; }
        public string? Adres { get; set; }
    }
}
