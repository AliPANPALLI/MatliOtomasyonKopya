namespace KumesScada.Application.ViewModels
{
    /// <summary>
    /// Yumurtlama Takip ekranındaki üst kümes seçici (chip list) için
    /// "Kumesler" tablosundan, lokasyona göre dönen sade liste satırı.
    /// </summary>
    public sealed class VGYumurtlamaKumesSecenegiViewModel
    {
        public int Id { get; set; }

        public int? KumesId { get; set; }

        public string? KumesName { get; set; }

        public string? KumesNo { get; set; }

        public bool? Aktif { get; set; }
    }
}
