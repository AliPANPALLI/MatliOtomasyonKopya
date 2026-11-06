using KumesScada.Application.Interfaces;
using KumesScada.Application.RequestModels;
using KumesScada.Application.Services.Core;
using KumesScada.Application.ViewModels;
using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using KumesScada.Domain.Models.Excel;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KumesScada.Infrastructure.Services
{
    /// <summary>
    /// "RaporKategori"/"RaporVeri" tablolarında, kategori adı + hafta
    /// sayısı başına tek bir hedef değer tutar (lokasyon/ırk ayrımı yok —
    /// tek genel eğri). Örn. "HEDEF %" / "HEDEF GR" haftalık hedef
    /// eğrileri için kullanılır.
    /// </summary>
    public class RaporVeriAppService
        : AppService<RaporVeri, RaporVeriViewModel>, IRaporVeriAppService
    {
        private readonly IRaporVeriRepository _raporVeriRepository;
        private readonly KumesScadaDbContext _db;
        private readonly IRaporVeriExcelReader _excelReader;
        private readonly IVgYumurtlamaDonemAppService _donemService;
        private readonly IVGYumurtlamaKartExcelReader _kartReader;

        public RaporVeriAppService(
            IMapper mapper,
            IRaporVeriRepository raporVeriRepository,
            KumesScadaDbContext db,
            IRaporVeriExcelReader excelReader,
            IVgYumurtlamaDonemAppService donemService,
            IVGYumurtlamaKartExcelReader kartReader) : base(mapper, raporVeriRepository)
        {
            _raporVeriRepository = raporVeriRepository;
            _db = db;
            _excelReader = excelReader;
            _donemService = donemService;
            _kartReader = kartReader;
        }

        public async Task<List<RaporVeriViewModel>> GetByKategoriAdi(string kategoriAdi)
        {
            if (string.IsNullOrWhiteSpace(kategoriAdi))
                return new List<RaporVeriViewModel>();

            var veriler = await _raporVeriRepository.GetByKategoriAdi(kategoriAdi);

            return veriler.Select(x => new RaporVeriViewModel
            {
                Id = x.RaporVerilerId,
                RaporKategoriId = x.RaporKategoriId,
                RaporKategoriAd = x.RaporKategori?.RaporKategoriAd,
                HaftaSayisi = x.HaftaSayisi,
                RaporHedefDeger = x.RaporHedefDeger
            }).ToList();
        }

        public async Task<List<string>> GetKategoriler()
        {
            return await _db.Set<RaporKategori>()
                .OrderBy(x => x.RaporKategoriAd)
                .Select(x => x.RaporKategoriAd)
                .ToListAsync();
        }

        public async Task SaveGuides(RaporVeriSaveRequest request)
        {
            if (request == null)
                throw new Exception("Kayıt isteği boş olamaz.");

            if (string.IsNullOrWhiteSpace(request.RaporKategoriAd))
                throw new Exception("Kategori adı zorunludur.");

            request.Guides ??= new List<RaporVeriViewModel>();

            ValidateGuides(request.Guides);

            await SaveGuidesCoreAsync(request.RaporKategoriAd, request.Guides);
        }

        /// <summary>
        /// Kümesin ana excel'indeki "VERİLER" sayfasından "HEDEF %" /
        /// "HEDEF GR" (ve diğer 4 bölümün) satırlarını okuyup bu kümese
        /// ÖZEL, bu DÖNEME ÖZEL kategoriler olarak kaydeder —
        /// "HEDEF % - {KumesNo} [{GirisTarihi:yyyy-MM-dd}]" gibi. Aynı
        /// kümesin FARKLI partilerinde hedefler farklı olabileceği için
        /// (ör. T14'ün 135.PARTİ'si bitip 136.PARTİ başladığında hedefler
        /// değişebilir) kategori adına dönemin kümese giriş tarihi de
        /// eklenir — aksi halde yeni bir parti yüklendiğinde önceki
        /// partinin hedefleri sessizce ÜZERİNE YAZILIRDI (bkz. proje
        /// notları). Dönem bilgisi henüz kaydedilmemişse (ör. Hedef
        /// Değerleri, Geçmiş Veri Yükle'den ÖNCE yüklendiyse) dönemsiz
        /// eski biçime düşülür — Grafik sekmesi de (bkz.
        /// YumurtlamaTakipService.getRaporHedefKategoriAdi) önce dönemli
        /// adı arar, bulamazsa aynı eski biçime düşer.
        ///
        /// Hedef eğrisi çiftlik/ırka göre de farklılık gösterdiği için
        /// (bkz. T1/R5 örnek dosyaları) TEK bir ortak kategori yerine
        /// kümes bazında ayrı tutulur. SaveGuides ile aynı upsert+prune
        /// mantığını kullanır — AYNI dönem için dosya tekrar yüklenirse
        /// yinelenen kayıt oluşmaz, üzerine yazılır.
        ///
        /// Buraya yüklenen dosya genelde ana excel'in KENDİSİDİR (aynı
        /// workbook'ta "VERİLER" ile "Yumurtlama" sayfaları birlikte
        /// bulunur) — bu yüzden "Geçmiş Veri Yükle" ile aynı mantıkla
        /// (bkz. VGYumurtlamaController.ImportFromKartExcel) kapak
        /// bilgisinden (varsa) giriş tarihi/başlangıç hayvan sayısı da
        /// okunup dönem bilgisi güncellenir — kullanıcı aynı bilgiyi
        /// ayrıca "Geçmiş Veri Yükle" ile veya elle girmek zorunda
        /// kalmaz, ve kategoriEtiketi kümesin EN GÜNCEL (ilgisiz olabilecek)
        /// dönemi yerine excel'in kendi dönemine göre belirlenir.
        /// </summary>
        public async Task<RaporVeriExcelImportResultViewModel> ImportFromExcelAsync(
            Stream stream, string fileName, string kumesNo,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(kumesNo))
                throw new Exception("Kümes numarası zorunludur.");

            kumesNo = kumesNo.Trim();

            // Aynı dosya hem "VERİLER" (hedefler) hem "Yumurtlama" (kapak)
            // sayfası için iki ayrı okuyucuyla okunuyor — NPOI stream'i
            // tükettiğinden, her okuyucuya belleğe alınmış kendi kopyası
            // veriliyor.
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            var dosyaBaytlari = buffer.ToArray();

            var okuma = _excelReader.Read(new MemoryStream(dosyaBaytlari), fileName);

            var donem = await GuncelleDonemBilgisiFromKapakAsync(
                new MemoryStream(dosyaBaytlari), fileName, kumesNo, cancellationToken);

            var kategoriEtiketi = donem != null
                ? $"{kumesNo} [{donem.GirisTarihi:yyyy-MM-dd}]"
                : kumesNo;

            var result = new RaporVeriExcelImportResultViewModel
            {
                KumesNo = kumesNo,
                Warnings = okuma.Warnings
            };

            if (okuma.HedefYuzde.Count > 0)
            {
                await SaveGuidesCoreAsync(
                    $"HEDEF % - {kategoriEtiketi}",
                    okuma.HedefYuzde.Select(ToViewModel).ToList());

                result.HedefYuzdeSatirSayisi = okuma.HedefYuzde.Count;
            }

            if (okuma.HedefGram.Count > 0)
            {
                await SaveGuidesCoreAsync(
                    $"HEDEF GR - {kategoriEtiketi}",
                    okuma.HedefGram.Select(ToViewModel).ToList());

                result.HedefGramSatirSayisi = okuma.HedefGram.Count;
            }

            // Bkz. RaporVeriExcelReader üzerindeki not — %/GR dışındaki
            // 4 hedef bölümü de aynı upsert+prune mantığıyla kaydediliyor,
            // ama henüz Grafik sekmesinde hiçbir yerde gösterilmiyor.
            if (okuma.HedefAdet.Count > 0)
            {
                await SaveGuidesCoreAsync(
                    $"HEDEF ADET - {kategoriEtiketi}",
                    okuma.HedefAdet.Select(ToViewModel).ToList());

                result.HedefAdetSatirSayisi = okuma.HedefAdet.Count;
            }

            if (okuma.HedefYasam.Count > 0)
            {
                await SaveGuidesCoreAsync(
                    $"HEDEF YAŞAM - {kategoriEtiketi}",
                    okuma.HedefYasam.Select(ToViewModel).ToList());

                result.HedefYasamSatirSayisi = okuma.HedefYasam.Count;
            }

            if (okuma.HedefHh.Count > 0)
            {
                await SaveGuidesCoreAsync(
                    $"HEDEF HH - {kategoriEtiketi}",
                    okuma.HedefHh.Select(ToViewModel).ToList());

                result.HedefHhSatirSayisi = okuma.HedefHh.Count;
            }

            if (okuma.HedefEggMass.Count > 0)
            {
                await SaveGuidesCoreAsync(
                    $"HEDEF EGG MASS - {kategoriEtiketi}",
                    okuma.HedefEggMass.Select(ToViewModel).ToList());

                result.HedefEggMassSatirSayisi = okuma.HedefEggMass.Count;
            }

            return result;
        }

        /// <summary>
        /// Ana excel'in "Yumurtlama" sayfasının kapak bilgisinden (varsa)
        /// giriş tarihi/başlangıç hayvan sayısı/ırk okuyup dönem bilgisini
        /// günceller, güncel/bulunan dönemi döner — bkz. ImportFromExcelAsync
        /// üzerindeki not. Irk ÖNCELİKLE kapaktan okunur; yoksa TAM OLARAK
        /// bu giriş tarihine ait dönem zaten kayıtlıysa onun değeri korunur
        /// (bkz. VGYumurtlamaController.ImportFromKartExcel'deki aynı kural
        /// — kümesin EN GÜNCEL dönemi değil, kendi dönemi kullanılır, aksi
        /// halde farklı dönemlerin ırkı birbirine karışabilirdi).
        /// </summary>
        private async Task<VgYumurtlamaDonemViewModel?> GuncelleDonemBilgisiFromKapakAsync(
            Stream stream, string fileName, string kumesNo,
            CancellationToken cancellationToken)
        {
            var kapak = _kartReader.ReadKapakBilgisi(stream, fileName);

            if (!kapak.GirisTarihi.HasValue)
                return await _donemService.GetLatestByKumesNo(kumesNo, cancellationToken);

            var mevcutDonem = await _donemService.GetByKumesNoAndGirisTarihi(
                kumesNo, kapak.GirisTarihi.Value, cancellationToken);

            var baslangicHayvanSayisi =
                kapak.BaslangicHayvanSayisi ?? mevcutDonem?.BaslangicHayvanSayisi;

            if (!baslangicHayvanSayisi.HasValue || baslangicHayvanSayisi.Value <= 0)
                return mevcutDonem ?? await _donemService.GetLatestByKumesNo(kumesNo, cancellationToken);

            return await _donemService.SaveAsync(
                new VgYumurtlamaDonemSaveRequest
                {
                    KumesNo = kumesNo,
                    GirisTarihi = kapak.GirisTarihi.Value,
                    BaslangicHayvanSayisi = baslangicHayvanSayisi.Value,
                    Irk = kapak.Irk ?? mevcutDonem?.Irk,
                    GirisYasiGun = kapak.GirisYasiGun ?? mevcutDonem?.GirisYasiGun,
                    GirisHaftasi = kapak.GirisHaftasi ?? mevcutDonem?.GirisHaftasi
                },
                cancellationToken);
        }

        private static RaporVeriViewModel ToViewModel(RaporVeriHaftalikDeger deger)
        {
            return new RaporVeriViewModel
            {
                HaftaSayisi = deger.HaftaSayisi,
                RaporHedefDeger = deger.RaporHedefDeger
            };
        }

        /// <summary>
        /// SaveGuides (manuel/JSON) ile ImportFromExcelAsync (excel'den
        /// okunan) arasında paylaşılan upsert+prune mantığı: gönderilen
        /// haftalar güncellenir/eklenir, listede OLMAYAN haftalar
        /// silinir — bu yüzden aynı kategori tekrar kaydedilirse
        /// yinelenen satır oluşmaz.
        /// </summary>
        private async Task SaveGuidesCoreAsync(
            string kategoriAdi, List<RaporVeriViewModel> guides)
        {
            ValidateGuides(guides);

            var kategoriId = await GetOrCreateKategoriIdAsync(kategoriAdi);

            var mevcutVeriler = await _db.Set<RaporVeri>()
                .Where(x => x.RaporKategoriId == kategoriId)
                .ToListAsync();

            var gelenHaftalar = guides.Select(x => x.HaftaSayisi).Distinct().ToList();

            foreach (var item in guides)
            {
                var mevcut = mevcutVeriler.FirstOrDefault(x => x.HaftaSayisi == item.HaftaSayisi);

                if (mevcut == null)
                {
                    _db.Set<RaporVeri>().Add(new RaporVeri
                    {
                        RaporKategoriId = kategoriId,
                        HaftaSayisi = item.HaftaSayisi,
                        RaporHedefDeger = item.RaporHedefDeger
                    });
                }
                else
                {
                    mevcut.RaporHedefDeger = item.RaporHedefDeger;
                }
            }

            // Grid'den/kaynak excel'den kaldırılan haftaları kalıcı
            // olarak siler (bu tabloda soft-delete alanı yok).
            var silinecekler = mevcutVeriler
                .Where(x => !gelenHaftalar.Contains(x.HaftaSayisi))
                .ToList();

            _db.Set<RaporVeri>().RemoveRange(silinecekler);

            await _db.SaveChangesAsync();
        }

        public async Task DeleteGuide(int id)
        {
            var veri = await _db.Set<RaporVeri>()
                .FirstOrDefaultAsync(x => x.RaporVerilerId == id);

            if (veri == null)
                throw new Exception("Hedef kaydı bulunamadı.");

            _db.Set<RaporVeri>().Remove(veri);
            await _db.SaveChangesAsync();
        }

        private async Task<int> GetOrCreateKategoriIdAsync(string kategoriAdi)
        {
            var kategori = await _db.Set<RaporKategori>()
                .FirstOrDefaultAsync(x => x.RaporKategoriAd == kategoriAdi);

            if (kategori != null)
                return kategori.RaporKategoriId;

            var yeni = new RaporKategori
            {
                RaporKategoriAd = kategoriAdi
            };

            _db.Set<RaporKategori>().Add(yeni);
            await _db.SaveChangesAsync();

            return yeni.RaporKategoriId;
        }

        private static void ValidateGuides(List<RaporVeriViewModel> guides)
        {
            var haftaSet = new HashSet<int>();

            foreach (var item in guides)
            {
                if (item.HaftaSayisi <= 0)
                    throw new Exception("Hafta sayısı 0'dan büyük olmalıdır.");

                if (haftaSet.Contains(item.HaftaSayisi))
                    throw new Exception($"{item.HaftaSayisi}. hafta birden fazla kez girilmiş.");

                haftaSet.Add(item.HaftaSayisi);
            }
        }
    }
}
