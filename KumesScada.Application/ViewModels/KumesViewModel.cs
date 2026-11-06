namespace KumesScada.Application.ViewModels
{
    public class KumesViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? KumesNo { get; set; }
        public string? KumesTcId { get; set; }
        public int LokasyonId { get; set; }
        /// <summary>
        /// Ticari yumurta, damızlık vb. kümes tipleri belirtilmelidir.
        /// </summary>
        public int KumesTip { get; set; }
        public int? Kapasite { get; set; }
        public bool? Silindi { get; set; }
        public string? PlcIpAdres { get; set; }
        public bool? PlcAutoConnect { get; set; }
        public string? ServisApiUrl { get; set; }
    }
}
