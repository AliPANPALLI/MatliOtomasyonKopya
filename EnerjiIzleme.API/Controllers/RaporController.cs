using EnerjiIzleme.Application.Interfaces;
using EnerjiIzleme.Application.Services;
using EnerjiIzleme.Domain.Models.ReportModels;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using System.Drawing;

namespace EnerjiIzleme.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RaporController : ControllerBase
    {
        private readonly ITreeAppService _treeAppService;

        public RaporController(ITreeAppService treeAppService)
        {
            _treeAppService = treeAppService;
        }       

        [HttpPost("excel")]
        public IActionResult TamRapor([FromBody] TamRaporRequest data)
        {
            var bytes = _treeAppService.TamRaporOlustur(data);
            var fileName = $"Enerji_Raporu_{data.FromDate}_{data.ToDate}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpPost("pastagrafik")]
        public IActionResult PastaGrafik([FromBody] RaporRequest data)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Analizör Pasta Grafik");

            // ── Başlıklar ──────────────────────────────
            ws.Cells[1, 1].Value = "ANALİZÖR";
            ws.Cells[1, 2].Value = "TÜKETİM (kWh)";

            var headerRange = ws.Cells[1, 1, 1, 2];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.Color.SetColor(Color.White);
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0x1a, 0x23, 0x7e));
            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // ── Veriler ────────────────────────────────
            for (int i = 0; i < data.Analizorler.Count; i++)
            {
                var a = data.Analizorler[i];
                ws.Cells[i + 2, 1].Value = a.CihazName;
                ws.Cells[i + 2, 2].Value = Math.Round(a.TuketimKwh, 2);
                ws.Cells[i + 2, 2].Style.Numberformat.Format = "#,##0.00";
            }

            ws.Column(1).Width = 30;
            ws.Column(2).Width = 20;

            // ── Pasta Grafik ───────────────────────────
            var chart = ws.Drawings.AddChart("PastaGrafik", eChartType.Pie) as ExcelPieChart;
            chart!.Title.Text = "Analizör Tüketim Dağılımı";

            var lastRow = data.Analizorler.Count + 1;
            var series = chart.Series.Add(
                ws.Cells[2, 2, lastRow, 2],   // values
                ws.Cells[2, 1, lastRow, 1]    // labels
            );
            series.DataLabel.ShowPercent = true;
            series.DataLabel.ShowCategory = true;

            chart.Legend.Position = eLegendPosition.Right;
            chart.SetPosition(1, 0, 3, 0);    // D2 hücresinden başla
            chart.SetSize(500, 400);

            // ── Döndür ────────────────────────────────
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Enerji_Pasta_{data.FromDate}_{data.ToDate}.xlsx";
            return File(
                stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        /// <summary>
        /// Elektrik faturası hesaplama modelleri
        /// </summary>

        // ─── INPUT MODELLERİ ──────────────────────────────────────────────────────────

        /// <summary>
        /// Çift Terim Tek Zamanlı tarife için veritabanından gelen yeşil alan değerleri
        /// </summary>
        public class TekZamanliTarife
        {
            /// <summary>Aktif enerji birim fiyatı (TL/kWh)</summary>
            public decimal AktifEnerjiBirimFiyat { get; set; }

            /// <summary>Dağıtım bedeli birim fiyatı (TL/kWh)</summary>
            public decimal DagitimBedeliBirimFiyat { get; set; }

            /// <summary>Sabit güç bedeli (TL)</summary>
            public decimal GucBedeli { get; set; }
        }

        /// <summary>
        /// Çift Terim Üç Zamanlı tarife için veritabanından gelen yeşil alan değerleri.
        /// Gündüz / Puant / Gece için ayrı aktif enerji birim fiyatları bulunur;
        /// dağıtım bedeli tek bir birim fiyat üzerinden hesaplanır.
        /// </summary>
        public class UcZamanliTarife
        {
            // --- Gündüz (06:00–17:00) ---
            /// <summary>Gündüz aktif enerji birim fiyatı (TL/kWh)</summary>
            public decimal GunduzAktifEnerjiBirimFiyat { get; set; }

            // --- Puant (17:00–22:00) ---
            /// <summary>Puant aktif enerji birim fiyatı (TL/kWh)</summary>
            public decimal PuantAktifEnerjiBirimFiyat { get; set; }

            // --- Gece (22:00–06:00) ---
            /// <summary>Gece aktif enerji birim fiyatı (TL/kWh)</summary>
            public decimal GeceAktifEnerjiBirimFiyat { get; set; }

            /// <summary>
            /// Tüm zaman dilimleri için ortak dağıtım bedeli birim fiyatı (TL/kWh).
            /// Toplam tüketim üzerinden hesaplanır.
            /// </summary>
            public decimal DagitimBedeliBirimFiyat { get; set; }

            /// <summary>Sabit güç bedeli (TL)</summary>
            public decimal GucBedeli { get; set; }
        }

        // ─── SONUÇ MODELLERİ ─────────────────────────────────────────────────────────

        public class TekZamanliSonuc
        {
            public decimal AktifEnerjiBedeli { get; set; }
            public decimal DagitimBedeli { get; set; }
            public decimal ToplamTutar { get; set; }   // Aktif + Dağıtım + Güç
            public decimal ElektrikTuketimVergisi { get; set; } // ETV  (%5 × Aktif)
            public decimal Kdv { get; set; }   // %10 × (ToplamTutar + ETV)
            public decimal FaturaTutari { get; set; }   // ToplamTutar + ETV + KDV
            public decimal BirimMaliyet { get; set; }   // FaturaTutari / ToplamTuketim
        }

        public class ZamanDilimiDetay
        {
            public decimal Tuketim { get; set; }
            public decimal AktifEnerjiBedeli { get; set; }
            public decimal DagitimBedeli { get; set; }
            public decimal ToplamTutar { get; set; }   // Aktif + Dağıtım (güç dahil değil)
        }

        public class UcZamanliSonuc
        {
            public ZamanDilimiDetay Gunduz { get; set; }
            public ZamanDilimiDetay Puant { get; set; }
            public ZamanDilimiDetay Gece { get; set; }

            public decimal ToplamTuketim { get; set; }
            public decimal ToplamAktifEnerji { get; set; }
            public decimal ToplamDagitimBedeli { get; set; }
            public decimal ToplamTutar { get; set; }   // ToplamAktif + ToplamDağıtım + GüçBedeli
            public decimal ElektrikTuketimVergisi { get; set; } // ETV  (%5 × ToplamAktif)
            public decimal Kdv { get; set; }   // %10 × (ToplamTutar + ETV)
            public decimal FaturaTutari { get; set; }   // ToplamTutar + ETV + KDV
            public decimal BirimMaliyet { get; set; }   // FaturaTutari / ToplamTuketim
        }

        // ─── HESAPLAMA SERVİSİ ────────────────────────────────────────────────────────

        public static class FaturaHesaplamaServisi
        {
            private const decimal EtvOrani = 0.05m;   // Elektrik Tüketim Vergisi: %5 (Aktif Enerji üzerinden)
            private const decimal KdvOrani = 0.10m;   // KDV: %10 (ToplamTutar + ETV üzerinden)

            /// <summary>
            /// Çift Terim Tek Zamanlı Tarımsal Faaliyet fatura tutarını hesaplar.
            /// </summary>
            /// <param name="toplamTuketim">Dönem toplam tüketimi (kWh) — sabit/hesaplanmış değer</param>
            /// <param name="tarife">Veritabanından gelen yeşil alan birim fiyatları</param>
            public static TekZamanliSonuc TekZamanliHesapla(
                decimal toplamTuketim,
                TekZamanliTarife tarife)
            {
                // 1. Aktif Enerji Bedeli
                decimal aktifEnerjiBedeli = toplamTuketim * tarife.AktifEnerjiBirimFiyat;

                // 2. Dağıtım Bedeli
                decimal dagitimBedeli = toplamTuketim * tarife.DagitimBedeliBirimFiyat;

                // 3. Toplam Tutar (Aktif + Dağıtım + Güç)
                decimal toplamTutar = aktifEnerjiBedeli + dagitimBedeli + tarife.GucBedeli;

                // 4. ETV — sadece Aktif Enerji Bedeli üzerinden %5
                decimal etv = aktifEnerjiBedeli * EtvOrani;

                // 5. KDV — (ToplamTutar + ETV) üzerinden %10
                decimal kdv = (toplamTutar + etv) * KdvOrani;

                // 6. Fatura Tutarı
                decimal faturaTutari = toplamTutar + etv + kdv;

                return new TekZamanliSonuc
                {
                    AktifEnerjiBedeli = aktifEnerjiBedeli,
                    DagitimBedeli = dagitimBedeli,
                    ToplamTutar = toplamTutar,
                    ElektrikTuketimVergisi = etv,
                    Kdv = kdv,
                    FaturaTutari = faturaTutari,
                    BirimMaliyet = toplamTuketim > 0 ? faturaTutari / toplamTuketim : 0
                };
            }

            /// <summary>
            /// Çift Terim Üç Zamanlı Tarımsal Faaliyet fatura tutarını hesaplar.
            /// Güç bedeli her zaman dilimine tüketim oranında paylaştırılır;
            /// ancak ETV ve KDV toplam bazda hesaplanır.
            /// </summary>
            /// <param name="gunduzTuketim">Gündüz (06:00–17:00) tüketimi (kWh)</param>
            /// <param name="puantTuketim">Puant (17:00–22:00) tüketimi (kWh)</param>
            /// <param name="geceTuketim">Gece (22:00–06:00) tüketimi (kWh)</param>
            /// <param name="tarife">Veritabanından gelen yeşil alan birim fiyatları</param>
            public static UcZamanliSonuc UcZamanliHesapla(
                decimal gunduzTuketim,
                decimal puantTuketim,
                decimal geceTuketim,
                UcZamanliTarife tarife)
            {
                decimal toplamTuketim = gunduzTuketim + puantTuketim + geceTuketim;

                // ── Zaman dilimi bazlı hesaplar ──────────────────────────────────────

                ZamanDilimiDetay gunduz = HesaplaZamanDilimi(
                    gunduzTuketim,
                    tarife.GunduzAktifEnerjiBirimFiyat,
                    tarife.DagitimBedeliBirimFiyat);

                ZamanDilimiDetay puant = HesaplaZamanDilimi(
                    puantTuketim,
                    tarife.PuantAktifEnerjiBirimFiyat,
                    tarife.DagitimBedeliBirimFiyat);

                ZamanDilimiDetay gece = HesaplaZamanDilimi(
                    geceTuketim,
                    tarife.GeceAktifEnerjiBirimFiyat,
                    tarife.DagitimBedeliBirimFiyat);

                // ── Toplam kalemler ──────────────────────────────────────────────────

                decimal toplamAktif = gunduz.AktifEnerjiBedeli + puant.AktifEnerjiBedeli + gece.AktifEnerjiBedeli;
                decimal toplamDagitim = gunduz.DagitimBedeli + puant.DagitimBedeli + gece.DagitimBedeli;

                // Toplam Tutar = TümDilimler(Aktif+Dağıtım) + GüçBedeli
                decimal toplamTutar = toplamAktif + toplamDagitim + tarife.GucBedeli;

                // ETV — sadece toplam Aktif Enerji Bedeli üzerinden %5
                decimal etv = toplamAktif * EtvOrani;

                // KDV — (ToplamTutar + ETV) üzerinden %10
                decimal kdv = (toplamTutar + etv) * KdvOrani;

                // Fatura Tutarı
                decimal faturaTutari = toplamTutar + etv + kdv;

                return new UcZamanliSonuc
                {
                    Gunduz = gunduz,
                    Puant = puant,
                    Gece = gece,
                    ToplamTuketim = toplamTuketim,
                    ToplamAktifEnerji = toplamAktif,
                    ToplamDagitimBedeli = toplamDagitim,
                    ToplamTutar = toplamTutar,
                    ElektrikTuketimVergisi = etv,
                    Kdv = kdv,
                    FaturaTutari = faturaTutari,
                    BirimMaliyet = toplamTuketim > 0 ? faturaTutari / toplamTuketim : 0
                };
            }

            // ── Yardımcı metot ───────────────────────────────────────────────────────

            private static ZamanDilimiDetay HesaplaZamanDilimi(
                decimal tuketim,
                decimal aktifBirim,
                decimal dagitimBirim)
            {
                decimal aktif = tuketim * aktifBirim;
                decimal dagitim = tuketim * dagitimBirim;

                return new ZamanDilimiDetay
                {
                    Tuketim = tuketim,
                    AktifEnerjiBedeli = aktif,
                    DagitimBedeli = dagitim,
                    ToplamTutar = aktif + dagitim   // Güç bedeli dilime dahil edilmez
                };
            }
        }

        // ─── ÖRNEK KULLANIM ──────────────────────────────────────────────────────────
        /*
        // ── Tek Zamanlı (Keskinoğlu) ─────────────────────────────────────────────────
        var tekTarife = new TekZamanliTarife
        {
            AktifEnerjiBirimFiyat    = 2.077761m,
            DagitimBedeliBirimFiyat  = 1.040417m,
            GucBedeli                = 25112.93m
        };
        // toplamTuketim hesaplanmış sabit: 46201.2 kWh
        var tekSonuc = FaturaHesaplamaServisi.TekZamanliHesapla(46201.2m, tekTarife);
        // tekSonuc.FaturaTutari → 191.373,87 TL ✓

        // ── Üç Zamanlı (Verim Gıda) ──────────────────────────────────────────────────
        var ucTarife = new UcZamanliTarife
        {
            GunduzAktifEnerjiBirimFiyat = 2.035594m,
            PuantAktifEnerjiBirimFiyat  = 3.501303m,
            GeceAktifEnerjiBirimFiyat   = 0.864353m,
            DagitimBedeliBirimFiyat     = 0.773544m,
            GucBedeli                   = 117829.85m
        };
        // Her dilim tüketimi hesaplanmış sabit değerler:
        var ucSonuc = FaturaHesaplamaServisi.UcZamanliHesapla(
            gunduzTuketim: 77997.6m,
            puantTuketim:  68128.875m,
            geceTuketim:   82665.45m,
            tarife: ucTarife);
        // ucSonuc.FaturaTutari → 865.713,26 TL ✓
        */
    }
}