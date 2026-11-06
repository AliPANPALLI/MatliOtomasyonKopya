namespace SharedApp.Domain.DbViewModels
{
    public class vwAlarmKullaniciEslemeForNotification
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        /// <summary>
        /// Alarmlar tablosundan gelen Id değeri
        /// </summary>
        public int? AlarmId { get; set; }
        /// <summary>
        /// AlarmGruplar tablosundan gelen Id değeri
        /// </summary>
        public int? AlarmGrupId { get; set; }
        public bool? Aktif { get; set; }
        public bool? Silindi { get; set; }
        public int? SeviyeNo { get; set; }  // Seviyeyi belirler (örneğin, 1. Seviye, 2. Seviye vb.)
        public TimeSpan BildirimSuresi { get; set; }  // Her seviye için geçen süre
        public bool? SeviyeAktif { get; set; }  // Seviye aktif mi değil mi?
        public string? BildirimYontemi { get; set; }  // E-posta, SMS, Sesli Mesaj vb.
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UserFullName { get; set; }
        public bool? TekrarlansinMi { get; set; }
        public TimeSpan? TekrarlamaSuresi { get; set; }
    }
}