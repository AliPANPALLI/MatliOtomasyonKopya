using System.Text.Json.Serialization;

namespace SharedApp.Domain.Models
{
    // Bir Form'un altında GERÇEKTEN var olan, kullanıcıya atanabilecek
    // link'lerin kataloğu. UserYetki.Link bugüne kadar serbest metin olarak
    // giriliyordu (ör. admin ekranında elle yazılan/hardcoded liste) — bu
    // tablo o listeyi veritabanına taşır. SidebarGoster alanı, bir link'in
    // kullanıcı yetkiliyse bile sidebar'da AYRI bir menü öğesi olarak
    // gösterilip gösterilmeyeceğini belirtir — ör. "kumes-scada/kumes-scada"
    // gibi bazı linkler sidebar'da görünmez, sadece kümes içi dashboard'dan
    // erişilir; "kumes-scada/kumesicidashboard" gibi linkler ise görünür.
    public class FormLinkEsleme
    {
        public int Id { get; set; }
        public int FormId { get; set; }
        public string Link { get; set; }

        // Admin ekranında/sidebar'da gösterilecek okunabilir isim — boşsa
        // Link'in kendisi gösterilir.
        public string? Ad { get; set; }

        // Sidebar'da bu link'in hangi şirket grubunun (eski Sirket/Lokasyon
        // ağacındaki "amblem" seviyesinin) altında gösterileceği — Pano
        // Kimlik/Kümes Scada/Su Hattı Scada gibi, sidebar'ı şirket bazlı
        // gruplanan modüllerde kullanılır. Boş bırakılırsa üst grup olmadan
        // doğrudan listelenir.
        public int? SirketId { get; set; }

        public bool? SidebarGoster { get; set; }
        public int? Sira { get; set; }
        public bool? Silindi { get; set; }

        [JsonIgnore] public virtual Form Form { get; set; }
        [JsonIgnore] public virtual Sirket? Sirket { get; set; }
    }
}
