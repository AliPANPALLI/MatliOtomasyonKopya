using KumesScada.Domain.Models.Excel;

namespace KumesScada.Application.Services
{
    /// <summary>
    /// "Günlük Yem Hesapları" ve "Kümes Tasnif Sayımları" dosyalarından
    /// okunan iki ayrı veri listesini (Tarih, KümesNo) anahtarına göre
    /// tek bir listede birleştirir. İki dosya birbirini tamamlayan farklı
    /// alanlar taşıdığı için (biri ölüm/yem/su, diğeri yumurta kırılımı),
    /// çakışan alan yoksa basitçe alan bazında birleştirilir.
    /// </summary>
    public static class VGExcelKumesGunlukMerger
    {
        public static List<VGExcelKumesGunlukData> Merge(
            IEnumerable<VGExcelKumesGunlukData> yemHesaplari,
            IEnumerable<VGExcelKumesGunlukData> tasnifSayimlari)
        {
            var dictionary =
                new Dictionary<(DateTime Tarih, string KumesNo), VGExcelKumesGunlukData>();

            foreach (var item in yemHesaplari)
                MergeInto(dictionary, item);

            foreach (var item in tasnifSayimlari)
                MergeInto(dictionary, item);

            return dictionary.Values
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.KumesNo)
                .ToList();
        }

        private static void MergeInto(
            Dictionary<(DateTime Tarih, string KumesNo), VGExcelKumesGunlukData> dictionary,
            VGExcelKumesGunlukData item)
        {
            var key = (item.Tarih.Date, item.KumesNo.Trim().ToUpperInvariant());

            if (!dictionary.TryGetValue(key, out var existing))
            {
                dictionary.Add(key, Clone(item));
                return;
            }

            existing.Olum ??= item.Olum;
            existing.YumurtaAdeti ??= item.YumurtaAdeti;
            existing.YumurtaGramaji ??= item.YumurtaGramaji;
            existing.CanliAgirlik ??= item.CanliAgirlik;
            existing.YemTuketim ??= item.YemTuketim;
            existing.YemTuketimHayvanBasi ??= item.YemTuketimHayvanBasi;
            existing.SuTuketim ??= item.SuTuketim;
            existing.Randiman ??= item.Randiman;
            existing.VbapRandiman ??= item.VbapRandiman;
            existing.Uygulama ??= item.Uygulama;
            existing.SaglamYumurta ??= item.SaglamYumurta;
            existing.KirikYumurta ??= item.KirikYumurta;
            existing.KirliYumurta ??= item.KirliYumurta;
            existing.KucukKirik ??= item.KucukKirik;
            existing.Fire ??= item.Fire;
            existing.LikidKabuksuzZar ??= item.LikidKabuksuzZar;
            existing.ZarliYumurtaKg ??= item.ZarliYumurtaKg;
            existing.MobaKirikDegeri ??= item.MobaKirikDegeri;
            existing.MobaKirliDegeri ??= item.MobaKirliDegeri;
            existing.KumesPersoneli ??= item.KumesPersoneli;
            existing.YeminRasyonNo ??= item.YeminRasyonNo;
            existing.GizliCatlak ??= item.GizliCatlak;
            existing.Klavuz ??= item.Klavuz;
            existing.Small ??= item.Small;
            existing.Medium ??= item.Medium;
            existing.Large ??= item.Large;
            existing.Jumbo ??= item.Jumbo;
            existing.SJumbo ??= item.SJumbo;
            existing.CiftSari ??= item.CiftSari;
            existing.HayvanYasiHafta ??= item.HayvanYasiHafta;
            existing.CekimSuresiDk ??= item.CekimSuresiDk;
            existing.SaatteCekilenYumurtaAdet ??= item.SaatteCekilenYumurtaAdet;
            existing.FiiliHayvanSayisi ??= item.FiiliHayvanSayisi;
            existing.FiiliYumurtlayanHayvanSayisi ??= item.FiiliYumurtlayanHayvanSayisi;
        }

        private static VGExcelKumesGunlukData Clone(VGExcelKumesGunlukData item)
        {
            return new VGExcelKumesGunlukData
            {
                Tarih = item.Tarih,
                KumesNo = item.KumesNo,
                Olum = item.Olum,
                YumurtaAdeti = item.YumurtaAdeti,
                YumurtaGramaji = item.YumurtaGramaji,
                CanliAgirlik = item.CanliAgirlik,
                YemTuketim = item.YemTuketim,
                YemTuketimHayvanBasi = item.YemTuketimHayvanBasi,
                SuTuketim = item.SuTuketim,
                Randiman = item.Randiman,
                VbapRandiman = item.VbapRandiman,
                Uygulama = item.Uygulama,
                SaglamYumurta = item.SaglamYumurta,
                KirikYumurta = item.KirikYumurta,
                KirliYumurta = item.KirliYumurta,
                KucukKirik = item.KucukKirik,
                Fire = item.Fire,
                LikidKabuksuzZar = item.LikidKabuksuzZar,
                ZarliYumurtaKg = item.ZarliYumurtaKg,
                MobaKirikDegeri = item.MobaKirikDegeri,
                MobaKirliDegeri = item.MobaKirliDegeri,
                KumesPersoneli = item.KumesPersoneli,
                YeminRasyonNo = item.YeminRasyonNo,
                GizliCatlak = item.GizliCatlak,
                Klavuz = item.Klavuz,
                Small = item.Small,
                Medium = item.Medium,
                Large = item.Large,
                Jumbo = item.Jumbo,
                SJumbo = item.SJumbo,
                CiftSari = item.CiftSari,
                HayvanYasiHafta = item.HayvanYasiHafta,
                CekimSuresiDk = item.CekimSuresiDk,
                SaatteCekilenYumurtaAdet = item.SaatteCekilenYumurtaAdet,
                FiiliHayvanSayisi = item.FiiliHayvanSayisi,
                FiiliYumurtlayanHayvanSayisi = item.FiiliYumurtlayanHayvanSayisi
            };
        }
    }
}
