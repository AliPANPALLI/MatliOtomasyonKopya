using System.ComponentModel.DataAnnotations.Schema;

namespace SharedApp.Domain.DbViewModels
{
    public class vwUserLinkYetki
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserFullName { get; set; }
        public string? UserName { get; set; }
        public int? ModulId { get; set; }
        public string? ModulName { get; set; }
        public bool? ModulSilindi { get; set; }
        public int? FormId { get; set; }
        public string? FormName { get; set; }
        public bool? FormSilindi { get; set; }
        public string? FormAciklama { get; set; }
        public string? Link { get; set; }
        [NotMapped] public string? LinkPathText { get; set; } = default!;
        public bool? Read { get; set; }
        public bool? Write { get; set; }
        public bool? SorumluMu { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        public bool? Aktif { get; set; }
    }
}