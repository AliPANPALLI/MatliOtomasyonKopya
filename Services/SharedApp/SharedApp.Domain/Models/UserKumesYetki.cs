namespace SharedApp.Domain.Models
{
    public class UserKumesYetki
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        /// <summary>
        /// Kumes tablosundaki Id degeridir.
        /// </summary>
        public int KumesId { get; set; }
        public int LokasyonId { get; set; }
        public int SirketId { get; set; }
        public bool? Read { get; set; }
        public bool? Write { get; set; }
        public bool? SorumluMu { get; set; }
        /// <summary>
        /// kullanici silinirse silindi true yapilabilir.
        /// </summary>
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        public bool? YemRasyonYetki { get; set; }
        public bool? RaporRehberYetki { get; set; }
        /// <summary>
        /// yetkileri tanımladik sonra yetki kaldirdik aktif false yapilir. yetkiler geri yuklenmek istenirse aktif true geri cekilmesi yeterli olur.
        /// </summary>
        public bool? Aktif { get; set; }
    }
}
