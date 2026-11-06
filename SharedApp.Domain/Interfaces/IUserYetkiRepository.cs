using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Domain.Interfaces
{
    public interface IUserYetkiRepository : IRepository<UserYetki>
    {
        // UserLinkYetkiRepository.AddUserLinkYetki ile birebir aynı mantık
        // (upsert — var olan (UserId, FormId, Link) kaydı günceller, yoksa
        // ekler) ama herhangi bir modül/form için kullanılabilir.
        Task AddUserYetki(List<string> secilenKullanicilar, List<string> secilenLinks, int formId, bool okumaYetkisi, bool yazmaYetkisi, bool degistirmeYetkisi, bool silmeYetkisi, bool onaylayabilirMi, bool sorumluMu, bool silindi, bool aktif);

        Task<List<UserYetki>> GetAllByUserId(string userId);
        Task<List<UserYetki>> GetKantarYetkiByUserId(string userId);

        Task<List<UserYetki>> GetAllByFormId(int formId);

        // Form (ve Form üzerinden Modul) ile join edilmiş, modül bazında
        // filtrelenebilen liste — admin ekranının "bu modülün yetkileri"
        // görünümü için.
        Task<List<UserYetkiDetayModel>> GetAllByModulId(int modulId);

        // Modül filtresi olmadan TÜM UserYetki kayıtları, Form/Modul
        // adlarıyla zenginleştirilmiş — tek, modülden bağımsız "Kullanıcı
        // Yetki Tanımlama" admin ekranının listesi için.
        Task<List<UserYetkiDetayModel>> GetAllEnriched();

        // Kaynak kullanıcının, verilen modüldeki TÜM UserYetki satırlarını
        // (Form+Link+Okuma/Yazma/Degistirme/Silme/Onaylayabilir/SorumluMu)
        // hedef kullanıcı(lar)a aynen kopyalar (upsert — AddUserYetki ile
        // birebir aynı mantık). Dönen değer kopyalanan satır sayısıdır.
        Task<int> TransferUserYetkiByModul(string kaynakUserId, List<string> hedefUserIdler, int modulId);
    }

    // vwUserLinkYetki'nin sadeleştirilmiş karşılığı — kullanıcı adı/soyadı
    // Authentication servisinin kendi veritabanında olduğu için (cross-db
    // SQL view gerektirir) burada YOK; UserId dönüyor, görünen ad gerekiyorsa
    // frontend zaten ayrıca çektiği kullanıcı listesiyle eşleştiriyor.
    public class UserYetkiDetayModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ModulId { get; set; }
        public string ModulName { get; set; }
        public int FormId { get; set; }
        public string FormName { get; set; }
        public string Link { get; set; }
        public bool? Okuma { get; set; }
        public bool? Yazma { get; set; }
        public bool? Degistirme { get; set; }
        public bool? Silme { get; set; }
        public bool? Onaylayabilir { get; set; }
        public bool? SorumluMu { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        public bool? Aktif { get; set; }
    }
}
