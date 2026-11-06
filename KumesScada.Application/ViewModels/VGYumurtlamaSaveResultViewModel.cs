namespace KumesScada.Application.ViewModels
{
    public sealed class VGYumurtlamaSaveResultViewModel
    {
        /// <summary>Kaydedilen satırlarda bulunan takvim yılı/yılları.</summary>
        public List<int> Years { get; set; } = new();

        public int KumesCount { get; set; }

        public int GunSatiriSayisi { get; set; }

        public int YazilanDegerSayisi { get; set; }

        /// <summary>
        /// Excel'de bulunan ama "Kumesler" tablosunda KumesNo'su eşleşen
        /// kayıt bulunamadığı için atlanan kümes kodları. Bu kümesler
        /// için önce Kümes Tanımlama ekranından kümes kaydı açılmalı.
        /// </summary>
        public List<string> BulunamayanKumesler { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }
}
