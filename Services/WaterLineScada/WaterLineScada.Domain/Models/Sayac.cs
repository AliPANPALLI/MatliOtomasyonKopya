namespace WaterLineScada.Domain.Models
{
    public class Sayac
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? FromSuKaynak { get; set; }//todo: ileride nullable olmamalı
        public int? ToSuKaynak { get; set; }//todo: ileride nullable olmamalı
        public byte? SayacTip { get; set; }
        public int? LokasyonId { get; set; }
        public int? RaporSira { get; set; }
        public int? EtiketIdSayacDegeri { get; set; }
        public string? NodeRedGunSonuSayacName { get; set; }
        public bool? Silindi { get; set; }

        ////iliski: bir FromSuKaynakNavigation olabilir
        public virtual SuKaynak? FromSuKaynakNavigation { get; set; }
        ////iliski: bir ToSuKaynakNavigation olabilir
        public virtual SuKaynak? ToSuKaynakNavigation { get; set; }
    }
}