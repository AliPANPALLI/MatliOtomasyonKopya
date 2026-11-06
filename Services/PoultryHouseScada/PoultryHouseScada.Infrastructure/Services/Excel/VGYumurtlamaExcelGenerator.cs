using ClosedXML.Excel;
using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.ViewModels;

namespace PoultryHouseScada.Infrastructure.Excel
{
    /// <summary>
    /// Kullanıcının yüklediği kaynak excel(ler)den tek bir kümes için,
    /// çiftliklerin kendi "Yumurtlama " kayıt kartlarıyla (ör.
    /// T1 127.PARTİ SÜPER NİCK#M2.xls, R5 LOHMANN CL. - V126. PARTİ.xls)
    /// aynı mantıkta bir rapor üretir.
    ///
    /// Kaynak dosya formatı <see cref="IVGYumurtlamaExcelSourceService"/>
    /// tarafından otomatik algılanır (Rahmiye'nin tek dosyalı "Veri
    /// Giriş Tablosu" formatı ya da Verim Gıda'nın "Günlük Yem
    /// Hesapları" + opsiyonel "Kümes Tasnif Sayımları" iki dosyalı
    /// formatı). Hesaplama mantığı (randıman, hafta, oranlar vs.) tek
    /// bir yerden yönetilsin diye <see cref="IVGYumurtlamaService"/>
    /// (aynı motor, veritabanından beslenen Yumurtlama Kartı'nda da
    /// kullanılıyor) üzerinden yapılır.
    ///
    /// Çıktı kolonları iki referans karttaki kolonların BİRLEŞİMİDİR:
    /// bir çiftliğin kaynak dosyasında karşılığı olmayan kolonlar
    /// (ör. Verim Gıda için Canlı Ağırlık/Tek Sıra/VBAP, Rahmiye için
    /// Sağlam/Kırık/Kirli/Küçük Kırık/Fire/HU) boş kalır.
    /// </summary>
    public sealed class VGYumurtlamaExcelGenerator
        : IVGYumurtlamaExcelGenerator
    {
        private readonly IVGYumurtlamaCalculationService _calculationService;
        private readonly IVGYumurtlamaQueryService _queryService;
        private readonly IVgYumurtlamaDonemAppService _donemService;

        public VGYumurtlamaExcelGenerator(
            IVGYumurtlamaCalculationService calculationService,
            IVGYumurtlamaQueryService queryService,
            IVgYumurtlamaDonemAppService donemService)
        {
            _calculationService = calculationService;
            _queryService = queryService;
            _donemService = donemService;
        }

        public async Task<byte[]> GenerateAsync(
            Stream yemHesaplariStream,
            string yemHesaplariFileName,
            Stream? tasnifStream,
            string? tasnifFileName,
            int year,
            string kumesNo,
            string? irk,
            DateTime kumeseGirisTarihi,
            decimal baslangicHayvanSayisi,
            string? icSiparisNo,
            int? girisYasiGun = null,
            CancellationToken cancellationToken = default)
        {
            var hesap = await _calculationService.CalculateAsync(
                yemHesaplariStream,
                yemHesaplariFileName,
                tasnifStream,
                tasnifFileName,
                year,
                kumesNo,
                irk,
                kumeseGirisTarihi,
                baslangicHayvanSayisi,
                icSiparisNo,
                girisYasiGun,
                cancellationToken);

            return Write(hesap, yemHesaplariFileName);
        }

        public async Task<byte[]> GenerateFromDatabaseAsync(
            string kumesNo,
            CancellationToken cancellationToken = default)
        {
            var donem = await _donemService.GetLatestByKumesNo(kumesNo, cancellationToken);

            if (donem == null)
            {
                throw new Exception(
                    $"{kumesNo} için kaydedilmiş dönem bilgisi bulunamadı. Önce " +
                    "\"Yumurtlama Kartı Excel'ini Oluştur\" ile bir kez elle girip " +
                    "excel üretmeniz gerekiyor — sonraki seferlerde bu bilgi hatırlanır.");
            }

            var hesap = await _queryService.GetAsync(
                new VGYumurtlamaQueryRequest
                {
                    KumesNo = kumesNo,
                    Year = null,
                    Irk = donem.Irk,
                    KumeseGirisTarihi = donem.GirisTarihi,
                    BaslangicHayvanSayisi = donem.BaslangicHayvanSayisi,
                    GirisYasiGun = donem.GirisYasiGun,
                    GirisHaftasi = donem.GirisHaftasi,
                },
                cancellationToken);

            return Write(hesap, "Veritabanı");
        }

        private static byte[] Write(
            VGYumurtlamaViewModel hesap,
            string kaynakDosyaAdi)
        {
            using var workbook =
                new XLWorkbook();

            var ws =
                workbook.Worksheets.Add("Yumurtlama");

            // =====================================================
            // ÜST BİLGİ
            // =====================================================

            ws.Cell("A1").Value =
                "YUMURTLAMA RAPORU";

            ws.Range("A1:AV1").Merge();

            ws.Cell("A3").Value = "Kümes";
            ws.Cell("B3").Value = hesap.KumesNo;

            ws.Cell("D3").Value = "Irk";
            ws.Cell("E3").Value =
                hesap.Irk ?? string.Empty;

            ws.Cell("G3").Value =
                "Kümese Giriş";

            ws.Cell("H3").Value =
                hesap.KumeseGirisTarihi;

            ws.Cell("J3").Value =
                "Başlangıç Hayvan";

            ws.Cell("K3").Value =
                hesap.BaslangicHayvanSayisi;

            ws.Cell("M3").Value =
                "İç Sipariş";

            ws.Cell("N3").Value =
                hesap.IcSiparisNo ?? string.Empty;

            ws.Cell("A4").Value = "Kaynak";
            ws.Cell("B4").Value = kaynakDosyaAdi;


            // =====================================================
            // BAŞLIKLAR
            // =====================================================

            const int headerRow = 6;

            // NOT: Gerçek "Yumurtlama " kayıt kartında "Yaş" sütunu YOK
            // (D sütunu orada "KRİTER" — bkz. T1 127.PARTİ SÜPER
            // NİCK#M2.xls). Bu yüzden burada da yok — kümese giriş
            // tarihinden itibaren geçen gün zaten "Gün" sütununda var.
            string[] headers =
            {
                "Gün",              // 1  A
                "Hafta",            // 2  B
                "Tarih",            // 3  C

                "Kabuk Direnci",    // 4  D
                "Gizli Çatlak",     // 5  E

                "",                 // 6  F

                "Günlük Ölüm",      // 7  G
                "Ölüm %",           // 8  H
                "Tavuk Sayısı",     // 9  I

                "Sağlam",           // 10 J
                "Sağlam %",         // 11 K

                "Kırık",            // 12 L
                "Kırık %",          // 13 M

                "Kirli",            // 14 N
                "Kirli %",          // 15 O

                "Küçük Kırık",      // 16 P
                "Küçük Kırık %",    // 17 Q

                "Fire",             // 18 R
                "Fire %",           // 19 S

                // 20-25: ana excel kartının GÜNCEL (T14 135.PARTİ ve
                // sonrası) sütun adlarıyla AYNI değerlerin tekrarı —
                // "Zar Çatlağı"/"Likid" sırasıyla "Küçük Kırık"/"Fire"
                // ile BİREBİR AYNI alan (bkz. VGExcelKumesGunlukData.
                // KucukKirik/Fire — kartlar arasında sadece başlık adı
                // değişmiş), çift sayılmaz, toplam yumurta formülüne
                // ikinci kez dahil edilmez. "Likid - Kabuksuz Zar" ise
                // gerçekten YENİ bir alan (bkz. LikidKabuksuzZar).
                "Zar Çatlağı",              // 20 T
                "Zar Çatlağı %",            // 21 U

                "Likid",                    // 22 V
                "Likid %",                  // 23 W

                "Likid - Kabuksuz Zar",     // 24 X
                "Likid - Kabuksuz Zar %",   // 25 Y

                "Toplam Yumurta",   // 26 Z
                "HD %",             // 27 AA
                "HH",               // 28 AB

                "Haftalık Tavuk Başı",  // 29 AC (kullanılmıyor)
                "Tavuk Başı Toplam",    // 30 AD (kullanılmıyor)

                "Egg/gr",                    // 31 AE
                "Günlük Yumurta Kütlesi",    // 32 AF

                "Yem Kg",                    // 33 AG
                "Hayvan Başı Yem Gr",        // 34 AH

                "Su Lt",             // 35 AI
                "Su / Yem",          // 36 AJ

                "Yumurta Başı Yem Gr",  // 37 AK

                "Canlı Ağırlık",     // 38 AL
                "HU",                // 39 AM

                "Tek Sıra Randıman", // 40 AN
                "VBAP Randıman",     // 41 AO

                "Uygulamalar",       // 42 AP

                "Yaşama Oranı %",    // 43 AQ — gerçek karttaki "G sütunu,
                                     // Yaşama oranı(%) günü" ile aynı bilgi:
                                     // hafta sonu tavuk sayısı / başlangıç
                                     // hayvan sayısı — ayrı, net bir sütunda.

                "Zarlı Yumurta (Kg)", // 44 AR — ana excel kartında T14
                                      // 135.PARTİ'den itibaren eklenen,
                                      // hiçbir kaynaktan beslenmeyen
                                      // (ara sıra elle doldurulan) sütun
                                      // — bkz. VGExcelKumesGunlukData.
                                      // ZarliYumurtaKg.

                "MOBA Kırık Değeri",                 // 45 AS — T14
                "MOBA Kirli Değeri (0,1,2,3)",       // 46 AT — 135.PARTİ'de
                                                      // şu an hiç veri yok
                                                      // (bkz. MobaKirikDegeri/
                                                      // MobaKirliDegeri).

                // DİKKAT: Gerçek ana excel kartında bu ikisinin İÇERİĞİ
                // başlıklarıyla uyuşmuyor (personel/rasyon bilgisi değil,
                // aydınlatma/yem saat aralığı notu) — kart neyse onu
                // olduğu gibi gösteriyoruz, bkz. VGExcelKumesGunlukData.
                "Kümes Personeli",   // 47 AU
                "Yemin Rasyon No",   // 48 AV
            };

            for (var i = 0; i < headers.Length; i++)
            {
                ws.Cell(headerRow, i + 1).Value = headers[i];
            }

            const int lastColumn = 48;


            // =====================================================
            // VERİ — her haftanın günlük satırlarının ardından, o
            // haftanın HaftalikVeriler'den gelen "Toplam" özet satırı
            // eklenir. Bu, gerçek "Yumurtlama " kayıt kartındaki haftalık
            // Toplam satırlarının (bkz. örnek kart formülleri: V15=
            // SUM(U8:U14)/SUM(J8:J14)*100, AC15=AB15/J15*1000/7 vb.)
            // karşılığıdır — aynı GunlukVeriler/HaftalikVeriler motoru
            // zaten bu formülleri uyguluyor (bkz. VGYumurtlamaService),
            // burada sadece doğru satırlara yerleştiriliyor.
            // =====================================================

            var excelRow = headerRow + 1;
            var toplamSatirlari = new List<int>();

            var haftaGruplari = hesap.GunlukVeriler
                .GroupBy(x => x.Hafta)
                .OrderBy(x => x.Key)
                .ToList();

            var haftalikByHafta = hesap.HaftalikVeriler
                .ToDictionary(x => x.Hafta);

            foreach (var haftaGrubu in haftaGruplari)
            {
                foreach (var gun in haftaGrubu.OrderBy(x => x.Tarih))
                {
                    ws.Cell(excelRow, 1).Value = gun.Gun;
                    ws.Cell(excelRow, 2).Value = gun.Hafta;
                    ws.Cell(excelRow, 3).Value = gun.Tarih;

                    if (gun.KabukDirenci.HasValue)
                        ws.Cell(excelRow, 4).Value = gun.KabukDirenci.Value;

                    if (gun.GizliCatlak.HasValue)
                        ws.Cell(excelRow, 5).Value = gun.GizliCatlak.Value;

                    ws.Cell(excelRow, 7).Value = gun.GunlukOlum;
                    ws.Cell(excelRow, 8).Value = gun.OlumOrani;
                    ws.Cell(excelRow, 9).Value = gun.TavukSayisi;

                    ws.Cell(excelRow, 10).Value = gun.SaglamYumurta;
                    ws.Cell(excelRow, 11).Value = gun.SaglamOrani;

                    ws.Cell(excelRow, 12).Value = gun.KirikYumurta;
                    ws.Cell(excelRow, 13).Value = gun.KirikOrani;

                    ws.Cell(excelRow, 14).Value = gun.KirliYumurta;
                    ws.Cell(excelRow, 15).Value = gun.KirliOrani;

                    ws.Cell(excelRow, 16).Value = gun.KucukKirik;
                    ws.Cell(excelRow, 17).Value = gun.KucukKirikOrani;

                    ws.Cell(excelRow, 18).Value = gun.Fire;
                    ws.Cell(excelRow, 19).Value = gun.FireOrani;

                    // Zar Çatlağı/Likid = Küçük Kırık/Fire'ın güncel kart
                    // terminolojisiyle TEKRARI (bkz. başlık dizisindeki not).
                    ws.Cell(excelRow, 20).Value = gun.KucukKirik;
                    ws.Cell(excelRow, 21).Value = gun.KucukKirikOrani;

                    ws.Cell(excelRow, 22).Value = gun.Fire;
                    ws.Cell(excelRow, 23).Value = gun.FireOrani;

                    if (gun.LikidKabuksuzZar.HasValue)
                        ws.Cell(excelRow, 24).Value = gun.LikidKabuksuzZar.Value;

                    if (gun.LikidKabuksuzZarOrani.HasValue)
                        ws.Cell(excelRow, 25).Value = gun.LikidKabuksuzZarOrani.Value;

                    ws.Cell(excelRow, 26).Value = gun.ToplamYumurta;
                    ws.Cell(excelRow, 27).Value = gun.HdRandiman;
                    ws.Cell(excelRow, 28).Value = gun.HhRandiman;

                    if (gun.YumurtaGramaji.HasValue)
                        ws.Cell(excelRow, 31).Value = gun.YumurtaGramaji.Value;

                    if (gun.GunlukYumurtaKutlesi.HasValue)
                        ws.Cell(excelRow, 32).Value = gun.GunlukYumurtaKutlesi.Value;

                    ws.Cell(excelRow, 33).Value = gun.YemKg;
                    ws.Cell(excelRow, 34).Value = gun.HayvanBasiYemGr;

                    ws.Cell(excelRow, 35).Value = gun.SuLitre;
                    ws.Cell(excelRow, 36).Value = gun.SuYemOrani;

                    // DİKKAT: Gerçek ana excel kartında "YUMURTA BAŞI YEM"
                    // SADECE haftalık Toplam satırında doludur, günlük
                    // satırlarda bilinçli olarak boş bırakılır (bkz. T1
                    // 127.PARTİ örneğinde günlük AE hücreleri boş, sadece
                    // Toplam satırının AE63=AB63/U63*1000 formülü var) —
                    // bu yüzden burada günlük satıra YAZILMIYOR.

                    if (gun.CanliAgirlik.HasValue)
                        ws.Cell(excelRow, 38).Value = gun.CanliAgirlik.Value;

                    if (gun.Hu.HasValue)
                        ws.Cell(excelRow, 39).Value = gun.Hu.Value;

                    if (gun.TekSiraRandiman.HasValue)
                        ws.Cell(excelRow, 40).Value = gun.TekSiraRandiman.Value;

                    if (gun.VbapRandiman.HasValue)
                        ws.Cell(excelRow, 41).Value = gun.VbapRandiman.Value;

                    if (!string.IsNullOrWhiteSpace(gun.Uygulama))
                        ws.Cell(excelRow, 42).Value = gun.Uygulama;

                    if (hesap.BaslangicHayvanSayisi > 0)
                    {
                        ws.Cell(excelRow, 43).Value =
                            gun.TavukSayisi / hesap.BaslangicHayvanSayisi * 100m;
                    }

                    if (gun.ZarliYumurtaKg.HasValue)
                        ws.Cell(excelRow, 44).Value = gun.ZarliYumurtaKg.Value;

                    if (gun.MobaKirikDegeri.HasValue)
                        ws.Cell(excelRow, 45).Value = gun.MobaKirikDegeri.Value;

                    if (gun.MobaKirliDegeri.HasValue)
                        ws.Cell(excelRow, 46).Value = gun.MobaKirliDegeri.Value;

                    if (!string.IsNullOrWhiteSpace(gun.KumesPersoneli))
                        ws.Cell(excelRow, 47).Value = gun.KumesPersoneli;

                    if (!string.IsNullOrWhiteSpace(gun.YeminRasyonNo))
                        ws.Cell(excelRow, 48).Value = gun.YeminRasyonNo;

                    excelRow++;
                }

                if (haftalikByHafta.TryGetValue(haftaGrubu.Key, out var hafta))
                {
                    ws.Cell(excelRow, 2).Value = "Toplam";

                    if (hafta.OrtalamaKabukDirenci.HasValue)
                        ws.Cell(excelRow, 4).Value = hafta.OrtalamaKabukDirenci.Value;

                    if (hafta.OrtalamaGizliCatlak.HasValue)
                        ws.Cell(excelRow, 5).Value = hafta.OrtalamaGizliCatlak.Value;

                    ws.Cell(excelRow, 7).Value = hafta.ToplamOlum;
                    ws.Cell(excelRow, 8).Value = hafta.OlumOrani;
                    ws.Cell(excelRow, 9).Value = hafta.DonemSonuTavukSayisi;

                    ws.Cell(excelRow, 10).Value = hafta.ToplamSaglam;
                    ws.Cell(excelRow, 11).Value = hafta.SaglamOrani;

                    ws.Cell(excelRow, 12).Value = hafta.ToplamKirik;
                    ws.Cell(excelRow, 13).Value = hafta.KirikOrani;

                    ws.Cell(excelRow, 14).Value = hafta.ToplamKirli;
                    ws.Cell(excelRow, 15).Value = hafta.KirliOrani;

                    ws.Cell(excelRow, 16).Value = hafta.ToplamKucukKirik;
                    ws.Cell(excelRow, 17).Value = hafta.KucukKirikOrani;

                    ws.Cell(excelRow, 18).Value = hafta.ToplamFire;
                    ws.Cell(excelRow, 19).Value = hafta.FireOrani;

                    // Zar Çatlağı/Likid haftalık toplamı = Küçük Kırık/
                    // Fire toplamının TEKRARI (bkz. başlık dizisi notu).
                    ws.Cell(excelRow, 20).Value = hafta.ToplamKucukKirik;
                    ws.Cell(excelRow, 21).Value = hafta.KucukKirikOrani;

                    ws.Cell(excelRow, 22).Value = hafta.ToplamFire;
                    ws.Cell(excelRow, 23).Value = hafta.FireOrani;

                    if (hafta.ToplamLikidKabuksuzZar.HasValue)
                        ws.Cell(excelRow, 24).Value = hafta.ToplamLikidKabuksuzZar.Value;

                    if (hafta.LikidKabuksuzZarOrani.HasValue)
                        ws.Cell(excelRow, 25).Value = hafta.LikidKabuksuzZarOrani.Value;

                    ws.Cell(excelRow, 26).Value = hafta.ToplamYumurta;
                    ws.Cell(excelRow, 27).Value = hafta.HdRandiman;
                    ws.Cell(excelRow, 28).Value = hafta.HhRandiman;

                    if (hafta.OrtalamaYumurtaGramaji.HasValue)
                        ws.Cell(excelRow, 31).Value = hafta.OrtalamaYumurtaGramaji.Value;

                    if (hafta.YumurtaKutlesi.HasValue)
                        ws.Cell(excelRow, 32).Value = hafta.YumurtaKutlesi.Value;

                    ws.Cell(excelRow, 33).Value = hafta.ToplamYemKg;
                    ws.Cell(excelRow, 34).Value = hafta.OrtalamaHayvanBasiYemGr;

                    ws.Cell(excelRow, 35).Value = hafta.ToplamSuLitre;
                    ws.Cell(excelRow, 36).Value = hafta.SuYemOrani;

                    if (hafta.YumurtaBasiYemGr.HasValue)
                        ws.Cell(excelRow, 37).Value = hafta.YumurtaBasiYemGr.Value;

                    if (hafta.OrtalamaTekSiraRandiman.HasValue)
                        ws.Cell(excelRow, 40).Value = hafta.OrtalamaTekSiraRandiman.Value;

                    if (hafta.OrtalamaVbapRandiman.HasValue)
                        ws.Cell(excelRow, 41).Value = hafta.OrtalamaVbapRandiman.Value;

                    if (hesap.BaslangicHayvanSayisi > 0)
                    {
                        ws.Cell(excelRow, 43).Value =
                            hafta.DonemSonuTavukSayisi / hesap.BaslangicHayvanSayisi * 100m;
                    }

                    if (hafta.ToplamZarliYumurtaKg.HasValue)
                        ws.Cell(excelRow, 44).Value = hafta.ToplamZarliYumurtaKg.Value;

                    if (hafta.OrtalamaMobaKirikDegeri.HasValue)
                        ws.Cell(excelRow, 45).Value = hafta.OrtalamaMobaKirikDegeri.Value;

                    if (hafta.OrtalamaMobaKirliDegeri.HasValue)
                        ws.Cell(excelRow, 46).Value = hafta.OrtalamaMobaKirliDegeri.Value;

                    toplamSatirlari.Add(excelRow);
                    excelRow++;
                }
            }


            // =====================================================
            // FORMAT
            // =====================================================

            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Font.FontSize = 16;

            ws.Row(headerRow).Style.Font.Bold = true;
            ws.Row(headerRow).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            ws.Row(headerRow).Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            var range = ws.Range(headerRow, 1, excelRow - 1, lastColumn);

            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // DİKKAT: Bu formatlar SADECE veri satırlarına (headerRow'dan
            // sonrasına) uygulanmalı — ws.Column(...) kullanırsa 3. satırdaki
            // üst bilgi hücreleriyle (ör. H3 = Kümese Giriş tarihi, aynı
            // sütun 8 = "Ölüm %" veri sütunuyla çakışıyor) ÇAKIŞIR ve H3'teki
            // tarih, "0.00" sayı formatıyla ham Excel seri numarası (ör.
            // 45931) olarak görünür. Bu yüzden ws.Range(dataFirstRow, ...)
            // ile aralık VERİ satırlarıyla sınırlanıyor.
            var dataFirstRow = headerRow + 1;
            var dataLastRow = excelRow - 1;

            ws.Range(dataFirstRow, 3, dataLastRow, 3).Style.DateFormat.Format = "dd.MM.yyyy";

            foreach (var col in new[] { 8, 11, 13, 15, 17, 19, 21, 23, 25, 27 })
            {
                ws.Range(dataFirstRow, col, dataLastRow, col).Style.NumberFormat.Format = "0.00";
            }

            ws.Range(dataFirstRow, 28, dataLastRow, 28).Style.NumberFormat.Format = "0.000";
            ws.Range(dataFirstRow, 31, dataLastRow, 31).Style.NumberFormat.Format = "0.00";
            ws.Range(dataFirstRow, 32, dataLastRow, 32).Style.NumberFormat.Format = "0.00";
            ws.Range(dataFirstRow, 43, dataLastRow, 43).Style.NumberFormat.Format = "0.00";

            // Kapak bilgisindeki "Kümese Giriş" tarihi (H3) — yukarıdaki
            // aralık taşmasından ayrı, kendi tarih formatı açıkça verilir.
            ws.Cell("H3").Style.DateFormat.Format = "dd.MM.yyyy";

            foreach (var toplamRow in toplamSatirlari)
            {
                var toplamRange = ws.Range(toplamRow, 1, toplamRow, lastColumn);
                toplamRange.Style.Font.Bold = true;
                toplamRange.Style.Fill.BackgroundColor = XLColor.FromArgb(0xE9, 0xEF, 0xF7);
            }

            ws.SheetView.FreezeRows(headerRow);

            ws.Columns().AdjustToContents();

            // "Uygulamalar" serbest metin olduğu için otomatik genişlik
            // yetersiz kalabiliyor — lastColumn artık "Zarlı Yumurta (Kg)"
            // olduğundan burada sabit sütun 42 hedefleniyor.
            ws.Column(42).Width = 35;


            // =====================================================
            // ÇIKTI
            // =====================================================

            using var output = new MemoryStream();
            workbook.SaveAs(output);
            return output.ToArray();
        }
    }
}
