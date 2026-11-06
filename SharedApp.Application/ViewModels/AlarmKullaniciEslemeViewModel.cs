namespace SharedApp.Application.ViewModels
{
    public class AlarmKullaniciEslemeViewModel
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int? AlarmId { get; set; }
        public int? AlarmGrupId { get; set; }
        public bool? Silindi { get; set; }
    }
}
