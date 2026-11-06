namespace SharedApp.Application.ViewModels
{
    public class UserKumesYetkiViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public int KumesId { get; set; }
        public string? KumesName { get; set; }       
        public int LokasyonId { get; set; }
        public string? LokasyonName { get; set; }
        public int SirketId { get; set; }
        public string? SirketName { get; set; }
        public bool? Read { get; set; }
        public bool? Write { get; set; }
        public bool? SorumluMu { get; set; }
        public bool? YemRasyonYetki { get; set; }
        public bool? RaporRehberYetki { get; set; }
        /// <summary>
        /// kullanici silinirse silindi true yapilabilir.
        /// </summary>
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        /// <summary>
        /// yetkileri tanımladik sonra yetki kaldirdik aktif false yapilir. yetkiler geri yuklenmek istenirse aktif true geri cekilmesi yeterli olur.
        /// </summary>
        public bool? Aktif { get; set; }
    }
}
