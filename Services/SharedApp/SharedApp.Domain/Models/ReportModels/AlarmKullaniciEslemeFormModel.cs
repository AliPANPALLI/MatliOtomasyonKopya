namespace SharedApp.Domain.Models.ReportModels
{
    public class AlarmKullaniciEslemeFormModel
    {
        public int Id { get; set; }
        public int? AlarmGrupId { get; set; }
        public List<string>? SecilenKullanicilarSeviye1 { get; set; }
        public List<string>? SecilenKullanicilarSeviye2 { get; set; }
        public List<string>? SecilenKullanicilarSeviye3 { get; set; }
        public string? BildirimYontemi { get; set; }
        public bool? Aktif { get; set; }
        public TimeSpan? BildirimSuresiSeviye1 { get; set; }
        public TimeSpan? BildirimSuresiSeviye2 { get; set; }
        public TimeSpan? BildirimSuresiSeviye3 { get; set; }
        public bool? TekrarlansinMi { get; set; }
        public bool? Silindi { get; set; }
        public TimeSpan? BildirimSuresi { get; set; }
        public bool? SeviyeAktif1 { get; set; }
        public bool? SeviyeAktif2 { get; set; }
        public bool? SeviyeAktif3 { get; set; }
    }
}