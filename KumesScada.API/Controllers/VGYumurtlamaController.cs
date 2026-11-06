using KumesScada.API.RequestModels;
using KumesScada.Application.Interfaces;
using KumesScada.Application.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace KumesScada.API.Controllers
{
    [ApiController]
    [Route("api/vg/yumurtlama")]
    public sealed class VGYumurtlamaController
        : ControllerBase
    {
        private readonly IVGYumurtlamaExcelGenerator _generator;

        private readonly IVGYumurtlamaSaveService _saveService;

        private readonly IVGYumurtlamaQueryService _queryService;

        private readonly IVGYumurtlamaKartExcelReader _kartReader;

        private readonly IVgYumurtlamaDonemAppService _donemService;

        private readonly IVGYumurtlamaGenelVeriService _genelVeriService;

        private readonly IRaporVeriAppService _raporVeriService;

        public VGYumurtlamaController(
            IVGYumurtlamaExcelGenerator generator,
            IVGYumurtlamaSaveService saveService,
            IVGYumurtlamaQueryService queryService,
            IVGYumurtlamaKartExcelReader kartReader,
            IVgYumurtlamaDonemAppService donemService,
            IVGYumurtlamaGenelVeriService genelVeriService,
            IRaporVeriAppService raporVeriService)
        {
            _generator = generator;
            _saveService = saveService;
            _queryService = queryService;
            _kartReader = kartReader;
            _donemService = donemService;
            _genelVeriService = genelVeriService;
            _raporVeriService = raporVeriService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Produces(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> Generate(
            [FromForm] VGYumurtlamaGenerateRequest request,
            CancellationToken cancellationToken)
        {
            if (request.File == null ||
                request.File.Length == 0)
            {
                return BadRequest(
                    "Veri giriş Excel dosyası seçilmedi.");
            }

            if (request.Year < 2000 ||
                request.Year > 2100)
            {
                return BadRequest(
                    "Geçerli bir yıl giriniz.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.KumesNo))
            {
                return BadRequest(
                    "Kümes numarası zorunludur.");
            }

            if (request.KumeseGirisTarihi == default)
            {
                return BadRequest(
                    "Kümese giriş tarihi zorunludur.");
            }

            if (request.BaslangicHayvanSayisi <= 0)
            {
                return BadRequest(
                    "Başlangıç hayvan sayısı sıfırdan büyük olmalıdır.");
            }

            var extension =
                Path.GetExtension(
                    request.File.FileName)
                    .ToLowerInvariant();

            if (extension != ".xlsx")
            {
                return BadRequest(
                    "Sadece .xlsx dosyası desteklenmektedir.");
            }

            if (request.TasnifFile != null &&
                request.TasnifFile.Length > 0)
            {
                var tasnifExtension =
                    Path.GetExtension(
                        request.TasnifFile.FileName)
                        .ToLowerInvariant();

                if (tasnifExtension != ".xlsx")
                {
                    return BadRequest(
                        "Tasnif dosyası için sadece .xlsx desteklenmektedir.");
                }
            }

            await using var stream =
                request.File.OpenReadStream();

            await using var tasnifStream =
                request.TasnifFile != null && request.TasnifFile.Length > 0
                    ? request.TasnifFile.OpenReadStream()
                    : null;

            var result =
                await _generator.GenerateAsync(
                    stream,
                    request.File.FileName,
                    tasnifStream,
                    request.TasnifFile?.FileName,
                    request.Year,
                    request.KumesNo.Trim(),
                    request.Irk,
                    request.KumeseGirisTarihi,
                    request.BaslangicHayvanSayisi,
                    request.IcSiparisNo,
                    request.GirisYasiGun,
                    cancellationToken);

            var fileName =
                $"{request.KumesNo.Trim()}-Yumurtlama-{request.Year}.xlsx";

            return File(
                result,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }


        /// <summary>
        /// Kaynak excel(ler)deki günlük verileri veritabanına kalıcı
        /// olarak yazar. Bir kez kaydedildikten sonra aynı rapor için
        /// excel'i tekrar yüklemeye gerek kalmaz — GET
        /// api/vg/yumurtlama/kart ile veritabanından okunabilir.
        /// </summary>
        [HttpPost("kaydet")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Kaydet(
            [FromForm] VGYumurtlamaSaveRequest request,
            CancellationToken cancellationToken)
        {
            if (request.File == null ||
                request.File.Length == 0)
            {
                return BadRequest(
                    "Veri giriş Excel dosyası seçilmedi.");
            }

            if (request.Year.HasValue &&
                (request.Year.Value < 2000 || request.Year.Value > 2100))
            {
                return BadRequest(
                    "Geçerli bir yıl giriniz.");
            }

            var extension =
                Path.GetExtension(
                    request.File.FileName)
                    .ToLowerInvariant();

            if (extension != ".xlsx")
            {
                return BadRequest(
                    "Sadece .xlsx dosyası desteklenmektedir.");
            }

            if (request.TasnifFile != null &&
                request.TasnifFile.Length > 0)
            {
                var tasnifExtension =
                    Path.GetExtension(
                        request.TasnifFile.FileName)
                        .ToLowerInvariant();

                if (tasnifExtension != ".xlsx")
                {
                    return BadRequest(
                        "Tasnif dosyası için sadece .xlsx desteklenmektedir.");
                }
            }

            await using var stream =
                request.File.OpenReadStream();

            await using var tasnifStream =
                request.TasnifFile != null && request.TasnifFile.Length > 0
                    ? request.TasnifFile.OpenReadStream()
                    : null;

            var result =
                await _saveService.SaveAsync(
                    stream,
                    request.File.FileName,
                    tasnifStream,
                    request.TasnifFile?.FileName,
                    request.Year,
                    string.IsNullOrWhiteSpace(request.KumesNo)
                        ? null
                        : request.KumesNo.Trim(),
                    cancellationToken);

            return Ok(result);
        }


        /// <summary>
        /// Yumurtlama Takip ekranının üstteki yatay kümes seçicisi için,
        /// "Kumesler" tablosundaki kayıtları verilen lokasyona göre döner.
        /// </summary>
        [HttpGet("kumesler")]
        public async Task<IActionResult> GetKumesler(
            [FromQuery] int lokasyonId,
            CancellationToken cancellationToken)
        {
            if (lokasyonId <= 0)
            {
                return BadRequest(
                    "Lokasyon id zorunludur.");
            }

            var result =
                await _queryService.GetKumeslerAsync(
                    lokasyonId,
                    cancellationToken);

            return Ok(result);
        }


        /// <summary>
        /// Veritabanına daha önce kaydedilmiş (bkz. POST
        /// api/vg/yumurtlama/kaydet) günlük verilerden, tek bir kümes
        /// için tam hesaplanmış Yumurtlama Kartı'nı (günlük + haftalık
        /// satırlar) JSON olarak döner. Excel yüklemeye gerek yoktur.
        /// </summary>
        [HttpGet("kart")]
        public async Task<IActionResult> GetKart(
            [FromQuery] VGYumurtlamaQueryRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.KumesNo))
            {
                return BadRequest(
                    "Kümes numarası zorunludur.");
            }

            // Year belirtilmezse kümesin TÜM tarihçesi (takvim yılı sınırı
            // olmadan) getirilir — bkz. VGYumurtlamaQueryRequest.Year
            // üzerindeki not. Grafik sekmesinde birden fazla yıl birleşik
            // (tek, kesintisiz seri) gösterilmek istendiğinde bu kullanılır.
            if (request.Year.HasValue &&
                (request.Year.Value < 2000 || request.Year.Value > 2100))
            {
                return BadRequest(
                    "Geçerli bir yıl giriniz.");
            }

            try
            {
                var result =
                    await _queryService.GetAsync(
                        request,
                        cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }


        /// <summary>
        /// Veritabanına daha önce kaydedilmiş günlük verilerden, tek bir
        /// kümes için Yumurtlama Kartı Excel'ini üretir — excel yüklemeye
        /// VE dönem bilgisini (giriş tarihi/başlangıç hayvan sayısı/ırk)
        /// elle girmeye gerek yoktur; bunlar daha önce "Yumurtlama Kartı
        /// Excel'ini Oluştur" ile kaydedilmiş VgYumurtlamaDonem kaydından
        /// otomatik okunur. Kümesin TÜM tarihçesi (tek takvim yılıyla
        /// sınırlı değil) dahil edilir.
        /// </summary>
        [HttpGet("kart-excel")]
        [Produces(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> GetKartExcel(
            string kumesNo,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(kumesNo))
                return BadRequest("Kümes numarası zorunludur.");

            try
            {
                var result = await _generator.GenerateFromDatabaseAsync(
                    kumesNo.Trim(),
                    cancellationToken);

                var fileName = $"{kumesNo.Trim()}-Yumurtlama.xlsx";

                return File(
                    result,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }


        /// <summary>
        /// Kümesin ana excel'inin ("Yumurtlama Kartı") "Yumurtlama "
        /// sayfasını GEÇMİŞ VERİ KAYNAĞI olarak okuyup veritabanına
        /// kaydeder — SADECE başka hiçbir kaynaktan (Günlük Yem
        /// Hesapları/Tasnif dosyaları arşivlenmemiş) elde edilemeyen
        /// geçmiş yıllar için kullanılır. Normal/güncel veri girişi
        /// için "Veritabanına Kaydet" (api/vg/yumurtlama/kaydet)
        /// kullanılmalı — bu uç ayrı, bilinçli bir "geçmiş veri
        /// yükleme" akışıdır. Aynı gün tekrar yüklenirse üzerine
        /// yazar (upsert), yinelenmez.
        /// </summary>
        [HttpPost("kart/import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportFromKartExcel(
            [FromForm] VGYumurtlamaKartImportRequest request,
            CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Excel dosyası seçilmedi.");

            if (string.IsNullOrWhiteSpace(request.KumesNo))
                return BadRequest("Kümes numarası zorunludur.");

            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest("Sadece .xlsx veya .xls dosyası desteklenmektedir.");

            var kumesNo = request.KumesNo.Trim();

            await using var stream = request.File.OpenReadStream();

            var data = _kartReader.Read(stream, request.File.FileName, kumesNo);

            var warnings = new List<string>(data.Warnings);

            if (data.GunlukVeriler.Count == 0)
            {
                warnings.Add(
                    "\"Yumurtlama \" sayfasında okunabilir günlük satır bulunamadı.");
            }

            // Ana excel'in kapak bilgisinden (varsa) kümese giriş tarihi/
            // başlangıç hayvan sayısı/ırk/girişte yaş otomatik türetildiyse,
            // dönem bilgisini de kaydet — kullanıcı elle girmek zorunda
            // kalmaz (bkz. VGYumurtlamaKartExcelReader.ReadKapakBilgisi).
            // Irk ÖNCELİKLE excel'den okunur (kapakta varsa); yoksa (ör.
            // bazı kartlarda bu hücre boş bırakılmış) TAM OLARAK bu giriş
            // tarihine ait dönem zaten kayıtlıysa onun değeri korunur.
            // Kasıtlı olarak GetLatestByKumesNo (kümesin EN GÜNCEL dönemi)
            // kullanılmıyor — aynı kümesin farklı (eski) dönemlerinde
            // farklı ırklar olabilir; en güncel dönemin ırkını buraya
            // kopyalamak geçmiş bir dönemi yanlış ırkla etiketleyebilirdi.
            if (data.GirisTarihi.HasValue)
            {
                var mevcutDonem = await _donemService.GetByKumesNoAndGirisTarihi(
                    kumesNo, data.GirisTarihi.Value, cancellationToken);

                var baslangicHayvanSayisi =
                    data.BaslangicHayvanSayisi ?? mevcutDonem?.BaslangicHayvanSayisi;

                if (baslangicHayvanSayisi.HasValue && baslangicHayvanSayisi.Value > 0)
                {
                    await _donemService.SaveAsync(
                        new VgYumurtlamaDonemSaveRequest
                        {
                            KumesNo = kumesNo,
                            GirisTarihi = data.GirisTarihi.Value,
                            BaslangicHayvanSayisi = baslangicHayvanSayisi.Value,
                            Irk = data.Irk ?? mevcutDonem?.Irk,
                            GirisYasiGun = data.GirisYasiGun ?? mevcutDonem?.GirisYasiGun,
                            GirisHaftasi = data.GirisHaftasi ?? mevcutDonem?.GirisHaftasi
                        },
                        cancellationToken);
                }
                else
                {
                    warnings.Add(
                        "Ana excel'in kapak bilgisinden kümese giriş tarihi okundu ama " +
                        "başlangıç hayvan sayısı bulunamadı; dönem bilgisi güncellenmedi — " +
                        "gerekiyorsa Dönem Bilgisi formundan elle girebilirsiniz.");
                }
            }

            // Buraya yüklenen dosya genelde ana excel'in KENDİSİ —
            // "Yumurtlama "/"A" sayfalarının yanında "VERİLER" sayfası
            // da (HEDEF %/HEDEF GR vb.) aynı workbook'ta bulunur. Kullanıcı
            // aynı dosyayı ayrıca "Hedef Değerleri" kutusuna da yüklemek
            // zorunda kalmasın diye burada da okunup kaydediliyor —
            // "VERİLER" sayfası yoksa (ör. bazı eski/şablon dosyalarda)
            // RaporVeriAppService sessizce bir uyarı ekler, akışı kesmez.
            await using var hedefStream = request.File.OpenReadStream();

            var hedefSonuc = await _raporVeriService.ImportFromExcelAsync(
                hedefStream, request.File.FileName, kumesNo, cancellationToken);

            warnings.AddRange(hedefSonuc.Warnings);

            var hedefSatirToplam =
                hedefSonuc.HedefYuzdeSatirSayisi + hedefSonuc.HedefGramSatirSayisi;

            if (hedefSatirToplam > 0)
            {
                warnings.Add(
                    $"Hedef değerleri de aktarıldı: {hedefSonuc.HedefYuzdeSatirSayisi} " +
                    $"hafta HEDEF %, {hedefSonuc.HedefGramSatirSayisi} hafta HEDEF GR.");
            }

            var result = await _saveService.SaveDataAsync(
                data.GunlukVeriler,
                warnings,
                year: null,
                kumesNo,
                cancellationToken);

            return Ok(result);
        }


        /// <summary>
        /// Verim Gıda'nın dış entegrasyonu için — veritabanında kayıtlı
        /// günlük verilerden, kümesler tablosundaki id'ye göre tek bir
        /// kümesin tam "Yumurtlama Kartı" verisini (haftalara gruplanmış,
        /// gerçek ana excel kartındaki KRİTER rotasyonu dahil) JSON
        /// paketi olarak döner. Bkz. IVGYumurtlamaGenelVeriService.
        /// </summary>
        [HttpGet("genel-veri")]
        public async Task<IActionResult> GetGenelVeri(
            [FromQuery] int kumesId,
            CancellationToken cancellationToken)
        {
            if (kumesId <= 0)
            {
                return BadRequest("Kümes id zorunludur.");
            }

            try
            {
                var result = await _genelVeriService.GetAsync(kumesId, cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}