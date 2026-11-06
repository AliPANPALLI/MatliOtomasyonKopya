using System.Globalization;
using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.Services;
using PoultryHouseScada.Domain.Models.Excel;
using NPOI.SS.UserModel;

namespace PoultryHouseScada.Infrastructure.Excel
{
    /// <summary>
    /// Kümesin ana excel'indeki ("Yumurtlama Kartı", örn. "T1 127.PARTİ
    /// SÜPER NİCK#M2.xls" / "R5 LOHMANN CL. - V126. PARTİ.xls") "Yumurtlama "
    /// sayfasını OKUR — bu sayfa normalde bizim kendi ÇIKTIMIZ olduğu için
    /// günlük veri kaynağı olarak kullanılmaz (bkz. VgYumurtlamaDonemAppService
    /// üzerindeki not), AMA başka hiçbir kaynakta (Günlük Yem Hesapları/
    /// Tasnif dosyaları arşivlenmemiş) bulunmayan GEÇMİŞ YILLARA ait veriyi
    /// içe aktarmanın tek yolu bu olduğu için, kullanıcı AÇIKÇA "geçmiş
    /// veri yükle" dediğinde kullanılır.
    ///
    /// NPOI kullanılır (ClosedXML değil) — bu dosyalar genelde eski .xls
    /// formatında arşivleniyor (bkz. RaporVeriExcelReader ile aynı gerekçe).
    ///
    /// Sütun düzeni (0-indeksli, gerçek dosyanın formülleri okunarak
    /// doğrulandı — bkz. proje notları):
    ///   0 Gün   1 Hafta   2 Tarih   3 Kriter (haftalık dönüşümlü etiket,
    ///   kullanılmaz)   4 Kabuk Direnci   5 Gizli Çatlak   7 Günlük Ölüm
    ///   10 Sağlam   12 Kırık   14 kirli   16 Küçük kırık   18 FİRE
    ///   (VerimGıda/"T" tipi kartlarda) — bu sekizi hep aynı sabit
    ///   konumda kalmış (farklı kart sürümleri arasında doğrulandı).
    ///
    /// GRAMAJ/YEM/SU/UYGULAMALAR sütunları ise SABİT DEĞİL — kart
    /// şablonu güncellendiğinde (ör. "T14 135.PARTİ" kartında "Likid -
    /// Kabuksuz Zar" sütunu eklenince) bu dördü birden kayıyor (25→27,
    /// 27→29, 29→31, 33→35 — bkz. proje notları, T1 ile T14 karşılaştırması).
    /// Bu yüzden sabit indeks yerine 5. satırdaki BAŞLIK METNİ aranarak
    /// bulunuyor ("EGG/gr", "YEM(kg)", "SU(LT)", "UYGULAMALAR" — bkz.
    /// BulBaslikSutunu), eski (25/27/29/33) sabit değerler sadece başlık
    /// hiç bulunamazsa son çare olarak kullanılıyor. Bu arama hem eski
    /// hem yeni kart biçiminde aynı başlık metnini bulduğu için iki
    /// biçim arasında ayrım yapmaya gerek kalmıyor.
    ///
    /// Rahmiye ("R" ile başlayan KumesNo) kartlarında 10/12/14/16/18
    /// sütunları hep 0 yazılı duruyor — gerçek günlük toplam yumurta
    /// sayısı "Toplam" etiketli sütunda tutuluyor; bu yüzden Rahmiye
    /// için ayrıca bu sütun etikete göre aranıp YumurtaAdeti alanına
    /// okunuyor (bkz. Read içindeki rahmiyeTipiKumes dalı).
    ///
    /// Haftalık "Toplam" özet satırları (col A boş) ATLANIR — bu
    /// değerler zaten günlük satırlardan yeniden hesaplanıyor
    /// (bkz. VGYumurtlamaService), tekrar içe aktarılırsa çift sayım
    /// olur.
    /// </summary>
    public class VGYumurtlamaKartExcelReader : IVGYumurtlamaKartExcelReader
    {
        private const string SheetName = "Yumurtlama";

        // "A" sayfası — bkz. ReadBoyutSiniflandirmaVerileri üzerindeki not.
        private const string BoyutSheetName = "A";
        private const int BoyutIlkGunSutunu = 3; // D sütunu (0-indeksli)
        private const int BoyutGunSutunSayisi = 7;

        // Her haftalık blokta 7 sınıf satırından SONRA kart modeline göre
        // değişen sayıda özet satırı gelebiliyor (bkz.
        // ReadBoyutSiniflandirmaVerileri üzerindeki not) — bu, bir
        // sonraki "TARİH" başlığına kadar taramayı sınırlayan bir
        // güvenlik üst sınırı, sabit blok boyutu değil.
        private const int BoyutBlokGuvenlikSiniri = 60;

        /// <summary>
        /// Kapak bilgisi (giriş tarihi/miktar/kriter etiketleri) sadece
        /// sayfanın İLK birkaç satırında olur — etiket arama bu satırlarla
        /// sınırlı tutulur ki günlük veri satırlarındaki rastgele metin
        /// hücreleri yanlışlıkla eşleşmesin.
        /// </summary>
        private const int KapakSatirSinir = 5;

        // Eski kart biçimindeki (bkz. "T1 127.PARTİ") sabit sütunlar —
        // BulBaslikSutunu başlık metnini bulamazsa son çare olarak
        // kullanılır; normalde her iki kart biçiminde de başlık metninden
        // doğru sütun bulunur (bkz. sınıf üstü not).
        private const int GramajSutunuVarsayilan = 25;
        private const int YemSutunuVarsayilan = 27;
        private const int SuSutunuVarsayilan = 29;
        private const int UygulamaSutunuVarsayilan = 33;

        // "Likid - Kabuksuz Zar" / "ZARLI YUMURTA (KG)" sütunlarının
        // eski kart biçiminde hiç karşılığı yok — bulunamazsa (eski
        // dosyalarda hep böyle olacak) ilgili alan null kalsın diye
        // varsayılan YOK, bkz. Read.

        // Başlık metni aranacak satır aralığı — "Yumurtlama " sayfasında
        // sütun başlıkları genelde 5-6. satırlardadır (0-indeksli), ama
        // kart şablonları arasında küçük kaymalar olabileceğinden arama
        // biraz geniş tutuluyor.
        private const int BaslikAramaIlkSatir = 0;
        private const int BaslikAramaSonSatir = 10;

        // Sabit sütun 16/18'in (bkz. sınıf üstü not) o konumda GERÇEKTEN
        // beklediğimiz alanı taşıdığını doğrulamak için — pozisyon aynı
        // kalsa da başlık metni kart sürümüne göre değişebiliyor
        // (Küçük Kırık ↔ Zar Çatlağı, FİRE ↔ Likid). Üçüncü bir kart
        // biçiminde bu pozisyon FARKLI bir şeye kayarsa (ör. araya yeni
        // bir sütun girerse) burada hiçbiri eşleşmez ve BeklenenEtiketKontrolu
        // bunu VGKartImportResult.Warnings'e ekler — okuma yine de devam
        // eder (pozisyondan okumayı durdurmaz), sadece kullanıcıyı uyarır.
        private static readonly HashSet<string> KucukKirikEtiketleri =
            new() { "KUCUKKIRIK", "ZARCATLAGI" };

        private static readonly HashSet<string> FireEtiketleri =
            new() { "FIRE", "LIKID" };

        public VGKartImportResult Read(Stream stream, string fileName, string kumesNo)
        {
            var sonuc = new VGKartImportResult();

            IWorkbook workbook;

            try
            {
                workbook = WorkbookFactory.Create(stream);
            }
            catch
            {
                return sonuc;
            }

            var worksheet = FindYumurtlamaSheet(workbook);

            if (worksheet == null)
                return sonuc;

            var gunlukVeriler = new List<VGExcelKumesGunlukData>();

            ReadKapakBilgisi(worksheet, sonuc);

            /*
             * Rahmiye ("R" ile başlayan KumesNo) kartlarında Sağlam/Kırık/
             * Kirli/Küçük Kırık/FİRE sütunları (10/12/14/16/18) her zaman
             * 0 yazılı duruyor — gerçek günlük toplam yumurta sayısı bu
             * kartlarda ayrı bir "Toplam" sütununda tutuluyor ve ham
             * YumurtaAdeti olarak buradan okunmalı (bkz.
             * VGYumurtlamaReportService — kırılım HasValue ise ondan
             * toplar, aksi halde YumurtaAdeti'ni kullanır; kırılım sütunları
             * 0 olarak dolu okunursa bu "aksi halde" dalı hiç çalışmaz).
             * VerimGıda ("T" vb.) kartlarında hiçbir davranış değişmiyor:
             * kırılım sütunları eskisi gibi sabit indeksten okunuyor.
             */
            var rahmiyeTipiKumes = kumesNo.Trim().StartsWith("R", StringComparison.OrdinalIgnoreCase);

            int? toplamSutunu = null;

            if (rahmiyeTipiKumes)
            {
                toplamSutunu = BulBaslikSutunu(worksheet, "TOPLAM");
            }
            else
            {
                // Rahmiye'de bu sütunlar hep 0 (anlamsız) olduğu için
                // kontrol sadece VerimGıda tipi kartlarda yapılıyor.
                BeklenenEtiketKontrolu(
                    worksheet, 16, KucukKirikEtiketleri,
                    "Küçük Kırık / Zar Çatlağı", sonuc);

                BeklenenEtiketKontrolu(
                    worksheet, 18, FireEtiketleri,
                    "FİRE / Likid", sonuc);
            }

            // GRAMAJ/YEM/SU/UYGULAMALAR sütunları kart şablonu
            // güncellendiğinde kayabiliyor (bkz. sınıf üstü not) — her
            // iki kart biçiminde de (VerimGıda VE Rahmiye) sabit indeks
            // yerine başlık metni aranıyor, bulunamazsa eski sabit
            // sütuna düşülüyor.
            var gramajSutunu = BulBaslikSutunu(worksheet, "EGG/GR") ?? GramajSutunuVarsayilan;
            var yemSutunu = BulBaslikSutunu(worksheet, "YEM(KG)") ?? YemSutunuVarsayilan;
            var suSutunu = BulBaslikSutunu(worksheet, "SU(LT)") ?? SuSutunuVarsayilan;
            var uygulamaSutunu = BulBaslikSutunu(worksheet, "UYGULAMALAR") ?? UygulamaSutunuVarsayilan;

            // Eski kart biçiminde hiç yok — bulunamazsa null kalır,
            // sabit bir varsayılana düşülmez (bkz. yukarıdaki not).
            var likidKabuksuzZarSutunu = BulBaslikSutunu(worksheet, "LIKID-KABUKSUZZAR");
            var zarliYumurtaKgSutunu = BulBaslikSutunu(worksheet, "ZARLIYUMURTA(KG)");
            var mobaKirikSutunu = BulBaslikSutunu(worksheet, "MOBAKIRIKDEGERI");
            var mobaKirliSutunu = BulBaslikSutunu(worksheet, "MOBAKIRLIDEGERI(0,1,2,3)");
            var kumesPersoneliSutunu = BulBaslikSutunu(worksheet, "KUMESPERSONELI");
            var yeminRasyonNoSutunu = BulBaslikSutunu(worksheet, "YEMINRASYONNO");

            var lastRowIndex = worksheet.LastRowNum;

            // GirisTarihi'nin (kapaktan okunduysa) sayfadaki hangi GÜN
            // satırına denk geldiğini bulmak için — bkz. ReadGirisYasiGun.
            int? girisYasiGunBulunan = null;

            // HAFTA (B sütunu) sadece o haftanın İLK satırında (Pazartesi)
            // dolu, diğer günlerde boş — bu yüzden son görülen değer
            // bir sonraki Pazartesi'ye kadar TAŞINIR (bkz. T1 127.PARTİ:
            // GÜN 101 Pzt=HAFTA 15, GÜN 102-107 boş ama hâlâ HAFTA 15'in
            // içinde). GirisTarihi'ne denk gelindiğinde bu taşınan değer
            // = o haftanın gerçek numarası (bkz. GirisHaftasi).
            int? sonBilinenHafta = null;
            int? girisHaftasiBulunan = null;

            for (var rowIndex = 0; rowIndex <= lastRowIndex; rowIndex++)
            {
                var row = worksheet.GetRow(rowIndex);
                if (row == null) continue;

                // Haftalık "Toplam" satırlarında A sütunu (Gün) boştur —
                // sadece gerçek günlük satırları alıyoruz.
                if (!TryGetNumeric(row.GetCell(0), out var gunSayisal))
                    continue;

                if (!TryGetDate(row.GetCell(2), out var tarih))
                    continue;

                if (TryGetNumeric(row.GetCell(1), out var haftaSayisal))
                    sonBilinenHafta = (int)Math.Round(haftaSayisal);

                // Bu satırın TARİH'i, kapaktan okunan KÜMESE GİR.TAR.
                // ile aynıysa, bu satırın GÜN değeri = hayvanın giriş
                // günündeki yaşı (bkz. VGKartImportResult.GirisYasiGun),
                // o anki taşınan HAFTA değeri de GirisHaftasi'dır.
                if (sonuc.GirisTarihi.HasValue &&
                    tarih.Date == sonuc.GirisTarihi.Value.Date)
                {
                    girisYasiGunBulunan = (int)Math.Round(gunSayisal);
                    girisHaftasiBulunan = sonBilinenHafta;
                }

                var data = new VGExcelKumesGunlukData
                {
                    Tarih = tarih.Date,
                    KumesNo = kumesNo,
                };

                if (TryGetDecimal(row.GetCell(4), out var kabukDirenci))
                    data.KabukDirenci = kabukDirenci;

                if (TryGetDecimal(row.GetCell(5), out var gizliCatlak))
                    data.GizliCatlak = gizliCatlak;

                if (TryGetDecimal(row.GetCell(7), out var olum))
                    data.Olum = olum;

                if (rahmiyeTipiKumes)
                {
                    if (toplamSutunu.HasValue &&
                        TryGetDecimal(row.GetCell(toplamSutunu.Value), out var toplamYumurta))
                        data.YumurtaAdeti = toplamYumurta;
                }
                else
                {
                    if (TryGetDecimal(row.GetCell(10), out var saglam))
                        data.SaglamYumurta = saglam;

                    if (TryGetDecimal(row.GetCell(12), out var kirik))
                        data.KirikYumurta = kirik;

                    if (TryGetDecimal(row.GetCell(14), out var kirli))
                        data.KirliYumurta = kirli;

                    if (TryGetDecimal(row.GetCell(16), out var kucukKirik))
                        data.KucukKirik = kucukKirik;

                    if (TryGetDecimal(row.GetCell(18), out var fire))
                        data.Fire = fire;
                }

                if (TryGetDecimal(row.GetCell(gramajSutunu), out var gramaj))
                    data.YumurtaGramaji = gramaj;

                if (TryGetDecimal(row.GetCell(yemSutunu), out var yem))
                    data.YemTuketim = yem;

                if (TryGetDecimal(row.GetCell(suSutunu), out var su))
                    data.SuTuketim = su;

                if (likidKabuksuzZarSutunu.HasValue &&
                    TryGetDecimal(row.GetCell(likidKabuksuzZarSutunu.Value), out var likidKabuksuzZar))
                    data.LikidKabuksuzZar = likidKabuksuzZar;

                if (zarliYumurtaKgSutunu.HasValue &&
                    TryGetDecimal(row.GetCell(zarliYumurtaKgSutunu.Value), out var zarliYumurtaKg))
                    data.ZarliYumurtaKg = zarliYumurtaKg;

                if (mobaKirikSutunu.HasValue &&
                    TryGetDecimal(row.GetCell(mobaKirikSutunu.Value), out var mobaKirik))
                    data.MobaKirikDegeri = mobaKirik;

                if (mobaKirliSutunu.HasValue &&
                    TryGetDecimal(row.GetCell(mobaKirliSutunu.Value), out var mobaKirli))
                    data.MobaKirliDegeri = mobaKirli;

                // "0" kontrolü: boş uygulama hücreleri bazı kartlarda
                // formül sonucu olarak sayısal 0 döndürüyor (gerçek bir
                // ilaç/aşı adı hiçbir zaman sadece "0" olmaz).
                var uygulama = GetCellText(row.GetCell(uygulamaSutunu));
                if (!string.IsNullOrWhiteSpace(uygulama) && uygulama != "0")
                    data.Uygulama = uygulama;

                if (kumesPersoneliSutunu.HasValue)
                {
                    var kumesPersoneli = GetCellText(row.GetCell(kumesPersoneliSutunu.Value));
                    if (!string.IsNullOrWhiteSpace(kumesPersoneli) && kumesPersoneli != "0")
                        data.KumesPersoneli = kumesPersoneli;
                }

                if (yeminRasyonNoSutunu.HasValue)
                {
                    var yeminRasyonNo = GetCellText(row.GetCell(yeminRasyonNoSutunu.Value));
                    if (!string.IsNullOrWhiteSpace(yeminRasyonNo) && yeminRasyonNo != "0")
                        data.YeminRasyonNo = yeminRasyonNo;
                }

                // Hiçbir alan dolu değilse (tamamen boş satır) atla.
                if (data.Olum == null && data.SaglamYumurta == null &&
                    data.KirikYumurta == null && data.KirliYumurta == null &&
                    data.KucukKirik == null && data.Fire == null &&
                    data.YumurtaAdeti == null &&
                    data.YemTuketim == null && data.SuTuketim == null &&
                    data.YumurtaGramaji == null && data.GizliCatlak == null &&
                    data.KabukDirenci == null && data.LikidKabuksuzZar == null &&
                    data.ZarliYumurtaKg == null && data.MobaKirikDegeri == null &&
                    data.MobaKirliDegeri == null &&
                    string.IsNullOrWhiteSpace(data.Uygulama) &&
                    string.IsNullOrWhiteSpace(data.KumesPersoneli) &&
                    string.IsNullOrWhiteSpace(data.YeminRasyonNo))
                {
                    continue;
                }

                gunlukVeriler.Add(data);
            }

            var boyutVerileri = ReadBoyutSiniflandirmaVerileri(workbook, kumesNo);
            sonuc.GunlukVeriler.AddRange(
                VGExcelKumesGunlukMerger.Merge(gunlukVeriler, boyutVerileri));

            sonuc.GirisYasiGun = girisYasiGunBulunan;
            sonuc.GirisHaftasi = girisHaftasiBulunan;

            return sonuc;
        }

        /// <summary>
        /// "A" sayfası — "Yumurtlama " sayfasındaki günlük verinin HAFTALIK
        /// bloklar halinde yumurta boyut sınıflandırmasına (Kılavuz/Small/
        /// Medium/Large/Jumbo/S.Jumbo/Çift Sarı) göre yeniden düzenlenmiş
        /// hali — bkz. proje notları. Her blok "TARİH " etiketli bir başlık
        /// satırıyla başlar (D:J sütunları o haftanın 7 gününün tarihi),
        /// altındaki satırlar aynı D:J sütunlarında o günün adedini taşır.
        /// "Kümes Tasnif Sayımları" dosyasının aynı alanlarını
        /// (VGExcelKumesGunlukData.Small/Medium/Large/...) GEÇMİŞ yıllar
        /// için de doldurmayı sağlar — o dosyalar arşivlenmemiş olsa bile
        /// bu sayfadan çıkarılabilir. Sayfa bulunamazsa (ör. Rahmiye tipi
        /// kartlarda) sessizce boş liste döner, hata vermez.
        ///
        /// Blok boyutu kartlar arasında SABİT DEĞİL — ör. "T1 127.PARTİ"
        /// kartında 7 sınıf satırından sonra SAĞLAM/KİRLİ/K.KIRIK/KIRIK/
        /// FİRE gelirken, "T14 135.PARTİ" kartında belli bir haftadan
        /// itibaren bunlara "Zar Çatlağı"/"Likid"/"Likid - Kabuksuz Zar"
        /// gibi ek özet satırları da eklenmiş (blok 19 satırdan 20 satıra
        /// çıkmış). Bu yüzden her bloktan SABİT 7 satır okumak yerine,
        /// bir sonraki "TARİH" başlığına kadar (bkz.
        /// <see cref="BoyutBlokGuvenlikSiniri"/>) taranır; sadece bilinen
        /// 7 sınıf etiketiyle eşleşen satırlar işlenir (bkz.
        /// BoyutAlaniSecici), tanınmayan özet satırları konumlarından
        /// bağımsız olarak yoksayılır. Böylece ileride araya yeni bir özet
        /// satırı daha eklenip bilinen sınıflar kaysa da hiçbiri atlanmaz.
        /// </summary>
        private static List<VGExcelKumesGunlukData> ReadBoyutSiniflandirmaVerileri(
            IWorkbook workbook,
            string kumesNo)
        {
            var dictionary =
                new Dictionary<(DateTime Tarih, string KumesNo), VGExcelKumesGunlukData>();

            ISheet? worksheet = null;

            for (var i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);

                if (sheet.SheetName.Trim().Equals(BoyutSheetName, StringComparison.OrdinalIgnoreCase))
                {
                    worksheet = sheet;
                    break;
                }
            }

            if (worksheet == null)
                return new List<VGExcelKumesGunlukData>();

            var lastRowIndex = worksheet.LastRowNum;

            for (var rowIndex = 0; rowIndex <= lastRowIndex; rowIndex++)
            {
                var headerRow = worksheet.GetRow(rowIndex);
                if (headerRow == null) continue;

                if (Normalize(GetCellText(headerRow.GetCell(2))) != "TARIH")
                    continue;

                var gunTarihleri = new DateTime?[BoyutGunSutunSayisi];

                for (var i = 0; i < BoyutGunSutunSayisi; i++)
                {
                    if (TryGetDate(headerRow.GetCell(BoyutIlkGunSutunu + i), out var tarih))
                        gunTarihleri[i] = tarih.Date;
                }

                for (var offset = 1; offset <= BoyutBlokGuvenlikSiniri; offset++)
                {
                    var dataRowIndex = rowIndex + offset;
                    if (dataRowIndex > lastRowIndex) break;

                    var dataRow = worksheet.GetRow(dataRowIndex);
                    if (dataRow == null) continue;

                    var label = Normalize(GetCellText(dataRow.GetCell(2)));

                    // Sıradaki haftanın "TARİH" başlığına ulaşıldı —
                    // bu blok burada bitti, aksi halde bir sonraki
                    // haftanın sınıf satırları bu haftanın tarihlerine
                    // yanlışlıkla eklenir.
                    if (label == "TARIH") break;

                    var setter = BoyutAlaniSecici(label);

                    if (setter == null) continue;

                    for (var i = 0; i < BoyutGunSutunSayisi; i++)
                    {
                        if (gunTarihleri[i] == null) continue;

                        if (!TryGetDecimal(dataRow.GetCell(BoyutIlkGunSutunu + i), out var deger))
                            continue;

                        var data = VGExcelReaderHelpers.GetOrCreate(
                            dictionary, gunTarihleri[i].Value, kumesNo);

                        setter(data, deger);
                    }
                }
            }

            return dictionary.Values.ToList();
        }

        private static Action<VGExcelKumesGunlukData, decimal>? BoyutAlaniSecici(
            string normalizedLabel)
        {
            return normalizedLabel switch
            {
                "KILAVUZ" => (d, v) => d.Klavuz = v,
                "SMALL" => (d, v) => d.Small = v,
                "MEDIUM" => (d, v) => d.Medium = v,
                "LARGE" => (d, v) => d.Large = v,
                "JUMBO" => (d, v) => d.Jumbo = v,
                "SJUMBO" => (d, v) => d.SJumbo = v,
                "CIFTSARI" => (d, v) => d.CiftSari = v,
                _ => null,
            };
        }

        /// <summary>
        /// Kapak satırındaki (bkz. T1 127.PARTİ SÜPER NİCK#M2.xls, satır
        /// 5: "KÜMESE GİR.TAR." / "MİKTAR:" etiketleri) giriş tarihi ve
        /// başlangıç hayvan sayısını, sabit sütun numarası yerine
        /// ETİKET ARAYARAK okur — farklı kartlarda etiketlerin tam
        /// sütun pozisyonu kayabilir, metin her zaman aynı kalır.
        /// </summary>
        /// <summary>
        /// Bkz. IVGYumurtlamaKartExcelReader.ReadKapakBilgisi üzerindeki not
        /// — günlük veri ayrıştırmasının maliyetine girmeden, sadece kapak
        /// bilgisini okur. "Hedef Değerleri" akışı tarafından kullanılır
        /// (bkz. RaporVeriAppService.ImportFromExcelAsync).
        /// </summary>
        public VGKartImportResult ReadKapakBilgisi(Stream stream, string fileName)
        {
            var sonuc = new VGKartImportResult();

            IWorkbook workbook;

            try
            {
                workbook = WorkbookFactory.Create(stream);
            }
            catch
            {
                return sonuc;
            }

            var worksheet = FindYumurtlamaSheet(workbook);

            if (worksheet != null)
                ReadKapakBilgisi(worksheet, sonuc);

            return sonuc;
        }

        private static ISheet? FindYumurtlamaSheet(IWorkbook workbook)
        {
            for (var i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);

                if (sheet.SheetName.Trim().StartsWith(SheetName, StringComparison.OrdinalIgnoreCase))
                    return sheet;
            }

            return null;
        }

        /// <summary>
        /// Giriş tarihi/başlangıç hayvan sayısı ETİKET aranarak okunur
        /// (bkz. sınıf üstü not). Irk ise FARKLI bir desen — kendi
        /// etiketi yok, "KÜMES: T5" hücresinden sabit 2 sütun sonra
        /// (aradaki sütun hep boş) serbest metin olarak duruyor; bu
        /// yüzden aşağıdaki döngüde ayrı bir dal olarak okunuyor.
        /// </summary>
        private static void ReadKapakBilgisi(ISheet worksheet, VGKartImportResult sonuc)
        {
            var sinirSatir = Math.Min(KapakSatirSinir, worksheet.LastRowNum);

            for (var rowIndex = 0; rowIndex <= sinirSatir; rowIndex++)
            {
                var row = worksheet.GetRow(rowIndex);
                if (row == null) continue;

                var lastCell = row.LastCellNum;

                for (var colIndex = 0; colIndex < lastCell; colIndex++)
                {
                    var etiket = Normalize(GetCellText(row.GetCell(colIndex)));

                    if (etiket.Length == 0)
                        continue;

                    if (!sonuc.GirisTarihi.HasValue &&
                        etiket.Contains("GIR") && etiket.Contains("TAR"))
                    {
                        if (TryFindDateInNextCells(row, colIndex, 5, out var tarih))
                            sonuc.GirisTarihi = tarih.Date;
                    }
                    else if (!sonuc.BaslangicHayvanSayisi.HasValue &&
                             etiket.Contains("MIKTAR"))
                    {
                        if (TryFindDecimalInNextCells(row, colIndex, 5, out var miktar) &&
                            miktar > 0)
                            sonuc.BaslangicHayvanSayisi = miktar;
                    }
                    else if (sonuc.Irk == null &&
                             etiket.Contains("KUMES") &&
                             !etiket.Contains("GIR") &&
                             !etiket.Contains("TAR"))
                    {
                        // "KÜMES: T5" hücresinin KENDİSİNDE etiket YOK —
                        // ırk ("SÜPER NİCK" gibi) bu hücreden 2 sütun
                        // sonra (aradaki hücre hep boş) hiç etiketlenmeden
                        // duruyor (T1/T5/T14 kartlarında doğrulandı — bkz.
                        // proje notları). Normal etiket arama deseni
                        // (TryFindDateInNextCells/Decimal) burada
                        // kullanılamaz çünkü aranan metin bir SAYI/TARİH
                        // değil serbest metin; bu yüzden sabit ofset.
                        var irkMetni = GetCellText(row.GetCell(colIndex + 2)).Trim();

                        if (irkMetni.Length > 0)
                            sonuc.Irk = irkMetni;
                    }
                }
            }
        }

        /// <summary>
        /// "Yumurtlama " sayfasının başlık satırlarında (bkz.
        /// <see cref="BaslikAramaIlkSatir"/>/<see cref="BaslikAramaSonSatir"/>)
        /// normalize edilmiş metni <paramref name="hedefEtiket"/> ile
        /// eşleşen hücrenin sütununu arar. Hem VerimGıda hem Rahmiye
        /// kartlarında GRAMAJ/YEM/SU/UYGULAMALAR (ve Rahmiye'de ayrıca
        /// TOPLAM) sütunlarını bulmak için kullanılır (bkz. Read) —
        /// bulunamazsa çağıran taraf sabit bir varsayılana düşer.
        /// </summary>
        private static int? BulBaslikSutunu(ISheet worksheet, string hedefEtiket)
        {
            var sonSatir = Math.Min(BaslikAramaSonSatir, worksheet.LastRowNum);

            for (var rowIndex = BaslikAramaIlkSatir; rowIndex <= sonSatir; rowIndex++)
            {
                var row = worksheet.GetRow(rowIndex);
                if (row == null) continue;

                var lastCell = row.LastCellNum;

                for (var colIndex = 0; colIndex < lastCell; colIndex++)
                {
                    if (Normalize(GetCellText(row.GetCell(colIndex))) == hedefEtiket)
                        return colIndex;
                }
            }

            return null;
        }

        /// <summary>
        /// Sabit bir sütunun (<paramref name="sutunIndex"/>) başlık
        /// metnini okuyup <paramref name="beklenenEtiketler"/> kümesinde
        /// değilse (ör. hiç tanımadığımız üçüncü bir kart biçimi) sonuca
        /// bir uyarı ekler — bkz. KucukKirikEtiketleri/FireEtiketleri
        /// üzerindeki not. Okumayı DURDURMAZ, sadece bilgilendirir.
        /// </summary>
        private static void BeklenenEtiketKontrolu(
            ISheet worksheet,
            int sutunIndex,
            HashSet<string> beklenenEtiketler,
            string alanAciklamasi,
            VGKartImportResult sonuc)
        {
            var sonSatir = Math.Min(BaslikAramaSonSatir, worksheet.LastRowNum);
            string? bulunanHamEtiket = null;

            for (var rowIndex = BaslikAramaIlkSatir; rowIndex <= sonSatir; rowIndex++)
            {
                var row = worksheet.GetRow(rowIndex);
                if (row == null) continue;

                var hamEtiket = GetCellText(row.GetCell(sutunIndex));
                if (string.IsNullOrWhiteSpace(hamEtiket)) continue;

                if (beklenenEtiketler.Contains(Normalize(hamEtiket)))
                    return;

                bulunanHamEtiket = hamEtiket;
            }

            if (bulunanHamEtiket != null)
            {
                sonuc.Warnings.Add(
                    $"\"Yumurtlama \" sayfasında {sutunIndex + 1}. sütunun başlığı " +
                    $"(\"{bulunanHamEtiket}\") {alanAciklamasi} için beklenenlerden " +
                    "farklı — bu sütun yanlış okunmuş olabilir, lütfen kontrol edin.");
            }
        }

        private static bool TryFindDateInNextCells(
            IRow row, int etiketColIndex, int araSutunSayisi, out DateTime value)
        {
            var sonColIndex = Math.Min(etiketColIndex + araSutunSayisi, row.LastCellNum);

            for (var colIndex = etiketColIndex + 1; colIndex <= sonColIndex; colIndex++)
            {
                if (TryGetDate(row.GetCell(colIndex), out value))
                    return true;
            }

            value = default;
            return false;
        }

        private static bool TryFindDecimalInNextCells(
            IRow row, int etiketColIndex, int araSutunSayisi, out decimal value)
        {
            var sonColIndex = Math.Min(etiketColIndex + araSutunSayisi, row.LastCellNum);

            for (var colIndex = etiketColIndex + 1; colIndex <= sonColIndex; colIndex++)
            {
                if (TryGetDecimal(row.GetCell(colIndex), out value))
                    return true;
            }

            value = default;
            return false;
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .ToUpperInvariant()
                // ToUpperInvariant() Türkçe noktasız "ı"yı büyütmüyor — bkz.
                // VGYumurtlamaQueryService.Normalize üzerindeki not.
                .Replace("ı", "I")
                .Replace("İ", "I")
                .Replace("Ş", "S")
                .Replace("Ğ", "G")
                .Replace("Ü", "U")
                .Replace("Ö", "O")
                .Replace("Ç", "C")
                .Replace(".", "")
                .Replace(":", "")
                .Replace(" ", "");
        }

        private static bool TryGetDate(ICell? cell, out DateTime value)
        {
            value = default;

            if (cell == null) return false;

            if (!TryGetNumeric(cell, out var serial))
                return false;

            try
            {
                value = DateUtil.GetJavaDate(serial);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetCellText(ICell? cell)
        {
            if (cell == null) return string.Empty;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Numeric => cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
                CellType.Formula when cell.CachedFormulaResultType == CellType.String =>
                    cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Formula when cell.CachedFormulaResultType == CellType.Numeric =>
                    cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Gerçek "Yumurtlama " sayfasında GÜN/TARİH dahil hücrelerin
        /// çoğu FORMÜL (ör. "A9=A8+1", "C8=+C9-1") — ClosedXML/NPOI'nin
        /// ham CellType.Numeric kontrolü bunları atlardı. Formül
        /// hücrelerinde NPOI'nin CACHED (son hesaplanmış) sonucunu
        /// okuyoruz.
        /// </summary>
        private static bool TryGetNumeric(ICell? cell, out double value)
        {
            value = 0;

            if (cell == null) return false;

            if (cell.CellType == CellType.Numeric)
            {
                value = cell.NumericCellValue;
                return true;
            }

            if (cell.CellType == CellType.Formula &&
                cell.CachedFormulaResultType == CellType.Numeric)
            {
                value = cell.NumericCellValue;
                return true;
            }

            // Bazı yeni sütunlarda (ör. "ZARLI YUMURTA (KG)") sayısal
            // görünen değerler METİN hücresi olarak girilmiş oluyor
            // (bkz. proje notları — T14 135.PARTİ'de '13'/'35' gibi
            // string hücreler) — bu yüzden METİN hücrelerinde de sayı
            // ayrıştırma denenir (bkz. RaporVeriExcelReader'daki aynı
            // yedek mantık).
            if (cell.CellType == CellType.String)
            {
                var text = cell.StringCellValue?.Trim();
                return double.TryParse(
                    text,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value);
            }

            return false;
        }

        private static bool TryGetDecimal(ICell? cell, out decimal value)
        {
            if (TryGetNumeric(cell, out var numeric))
            {
                value = (decimal)numeric;
                return true;
            }

            value = 0;
            return false;
        }
    }
}
