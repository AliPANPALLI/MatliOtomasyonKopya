namespace KumesScada.Domain.Models
{
    public class Uygulama
    {
        public int UygulamalarId { get; set; }

        public DateTime UygulamalarTarih { get; set; }

        public string? UygulamaAciklama { get; set; }

        // Bu tabloyu Uygulamalar dışındaki serbest metin alanlarıyla
        // (ör. "Kümes Personeli", "Yemin Rasyon No" — bkz. ana excel'in
        // "Yumurtlama " sayfası) paylaşmak için ayırt edici sütun. NULL
        // = eski/mevcut "Uygulama" kayıtları (geriye dönük uyumluluk
        // için "UYGULAMA" ile eşdeğer sayılır, bkz. VGYumurtlamaQueryService).
        public string? Tip { get; set; }

        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;
    }
}
