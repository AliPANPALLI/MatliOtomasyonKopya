namespace SuHatScada.Domain.Models
{
    public class KullaniciSekilYetki
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int? SekilId { get; set; }
        public int? CizimId { get; set; }
        public string? ModTuru { get; set; }
        public string? RadioButonModTuru { get; set; }
        public double? PozisyonX { get; set; }
        public double? PozisyonY { get; set; }
        public bool? Visible { get; set; }
        public bool? GorebilirMi { get; set; }//unvisible //aksiyonId, //aksiyonlar table 
    }
}