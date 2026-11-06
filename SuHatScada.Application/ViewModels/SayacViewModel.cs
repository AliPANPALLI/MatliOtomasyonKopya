namespace SuHatScada.Application.ViewModels
{
    public class SayacViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? FromSuKaynak { get; set; }
        public int? ToSuKaynak { get; set; }
        public byte? SayacTip { get; set; }
        public bool? Silindi { get; set; }
    }
}