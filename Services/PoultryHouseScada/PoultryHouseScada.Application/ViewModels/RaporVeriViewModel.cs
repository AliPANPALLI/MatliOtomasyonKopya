namespace PoultryHouseScada.Application.ViewModels
{
    public class RaporVeriViewModel
    {
        public int Id { get; set; }

        public int RaporKategoriId { get; set; }

        public string? RaporKategoriAd { get; set; }

        public int HaftaSayisi { get; set; }

        public decimal? RaporHedefDeger { get; set; }
    }
}
