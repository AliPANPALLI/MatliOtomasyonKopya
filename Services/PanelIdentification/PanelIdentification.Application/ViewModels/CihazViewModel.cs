namespace PanelIdentification.Application.ViewModels
{
    public class CihazViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Marka { get; set; }
        public string? UrunKodu { get; set; }
        public string? IP { get; set; }//nullable
        public bool? OnayIslemdeMi { get; set; }

        /// <summary>
        /// Cihazin bulundugu panonun id bilgisidir
        /// </summary>
        public int PanoId { get; set; }
        public string Type { get; set; } = "cihaz";
    }
}
