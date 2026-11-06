namespace SuHatScada.Application.ViewModels
{
    public class SuKaynakViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int LokasyonId { get; set; }
        public int SuKaynakTip { get; set; }
        public int SuKaynakDetayTip { get; set; }
        public bool? Silindi { get; set; }
    }
}