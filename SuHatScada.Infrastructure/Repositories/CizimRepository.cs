using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SuHatScada.Domain.Interfaces;
using SuHatScada.Domain.Models;
using SuHatScada.Infrastructure.Repositories.Core;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SuHatScada.Infrastructure.Repositories
{
    public class CizimRepository : Repository<Cizim>, ICizimRepository
    {
        private readonly SuHatScadaDbContext _context;
        public CizimRepository(SuHatScadaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<CizimSekilEsleme>> GetCizimSekilEslemesByCizimId(int cizimId)
        {
            return await _context.CizimSekilEsleme.AsNoTracking().Where(x => x.CizimId == cizimId && x.Silindi != true).ToListAsync();
        }

        public async Task<List<Sekil>> GetCizimEtiketler(List<int> sekilIds)
        {
            return await _context.Sekil.AsNoTracking().Where(x => sekilIds.Contains(x.Id)).ToListAsync();
        }

        public async Task<List<SekilEtiketKural>> GetEtiketKural(List<int> sekilIds)
        {
            return await _context.SekilEtiketKural.AsNoTracking().Where(x => sekilIds.Contains(x.SekilId) && x.Silindi != true).ToListAsync();
        }

        //public async Task<List<EtiketLog_Temp>> GetEtiketLogVerilerByEtiketIds(List<int> etiketIds)
        //{
        //    return await _context.EtiketLog_Temp.AsNoTracking().Where(x => x.KayitZamani != null && x.KayitZamani >= DateTime.Now.AddSeconds(-10) && x.EtiketId != null && etiketIds.Contains(x.EtiketId.Value)).OrderByDescending(x => x.Id).ToListAsync();
        //}

        //public async Task<List<EtiketLog_Temp>> GetEtiketLogVerilerByCizimId(int cizimId)
        //{
        //    var cizimLokasyonIds = await _context.CizimLokasyonEsleme.Where(x => x.CizimId == cizimId && x.Silindi != true).AsNoTracking().Select(x => x.LokasyonId).ToListAsync();
        //    //şekillere ait etiket kuralları bul ve etiketlerini al
        //    return await _context.EtiketLog_Temp.AsNoTracking().Where(x => x.KayitZamani.Value >= DateTime.Now.AddSeconds(-10) && cizimLokasyonIds.Contains(x.LokasyonId.GetValueOrDefault(0))).OrderByDescending(x => x.Id).ToListAsync();
        //}
        public async Task<List<EtiketLog_TempYeni>> GetEtiketLogVerilerByEtiketIds(List<int> etiketIds)
        {
            return await _context.EtiketLog_TempYeni.AsNoTracking().Where(x => x.KayitZamani != null && x.KayitZamani >= DateTime.Now.AddSeconds(-10) && x.EtiketId != null && etiketIds.Contains(x.EtiketId.Value)).OrderByDescending(x => x.Id).ToListAsync();
        }

        public async Task<List<EtiketLog_TempYeni>> GetEtiketLogVerilerByCizimId(int cizimId)
        {
            var cizimLokasyonIds = await _context.CizimLokasyonEsleme.Where(x => x.CizimId == cizimId && x.Silindi != true).AsNoTracking().Select(x => x.LokasyonId).ToListAsync();
            //şekillere ait etiket kuralları bul ve etiketlerini al
            return await _context.EtiketLog_TempYeni.AsNoTracking().Where(x => x.KayitZamani.Value >= DateTime.Now.AddSeconds(-10) && cizimLokasyonIds.Contains(x.LokasyonId.GetValueOrDefault(0))).OrderByDescending(x => x.Id).ToListAsync();
        }

        public async Task<int> GetCizimIdByLokasyonId(int lokasyonId)
        {
            return await _context.CizimLokasyonEsleme.AsNoTracking().Where(x => x.LokasyonId == lokasyonId).Select(x => x.CizimId).FirstOrDefaultAsync();
        }
        //public async Task<List<EtiketLog_Temp>> GetEtiketLogVeriler()
        //{
        //    return await _context.EtiketLog_Temp.AsNoTracking().Where(x => x.KayitZamani.Value.Date == DateTime.Now.Date).OrderByDescending(x => x.Id).ToListAsync();
        //}

        //public EtiketLog? GetEtiketLogGunSonuVeri(int etiketId)
        //{
        //    var result = _context.EtiketLog.Where(x => x.EtiketId == etiketId && x.KayitZamani.Value.Date == DateTime.Now.Date.AddDays(-1)).OrderByDescending(x => x.Id).FirstOrDefault();
        //    return result;
        //}
        //public async Task<List<EtiketLog>> GetEtiketLogGunSonuVeriler(List<int> etiketIds)
        //{
        //    try
        //    {
        //        List<EtiketLog> result = new List<EtiketLog>();

        //        foreach (var etiketId in etiketIds)
        //        {
        //            var asd = _context.EtiketLog.AsNoTracking().Where(x => x.EtiketId == etiketId && x.KayitZamani.Value.Date == DateTime.Now.Date.AddDays(-1)).OrderByDescending(x => x.Id).FirstOrDefault();
        //            if (asd != null)
        //                result.Add(asd);
        //        }
        //        //var result = await _context.EtiketLog
        //        //               .Where(x => etiketIds.Contains(x.EtiketId.GetValueOrDefault(0)) && x.KayitZamani.Value.Date == DateTime.Now.Date.AddDays(-1))
        //        //               .GroupBy(x => x.EtiketId)
        //        //               .Select(g => g.OrderByDescending(x => x.Id).FirstOrDefault())
        //        //               .ToListAsync();
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }

        //}
        public async Task<List<EtiketLog_TempYeni>> GetEtiketLogVeriler()
        {
            return await _context.EtiketLog_TempYeni.AsNoTracking().Where(x => x.KayitZamani.Value.Date == DateTime.Now.Date).OrderByDescending(x => x.Id).ToListAsync();
        }

        public EtiketLogYeni? GetEtiketLogGunSonuVeri(int etiketId)
        {
            var result = _context.EtiketLogYeni.Where(x => x.EtiketId == etiketId && x.KayitZamani.Value.Date == DateTime.Now.Date.AddDays(-1)).OrderByDescending(x => x.Id).FirstOrDefault();
            return result;
        }

        public async Task<List<EtiketLogYeni>> GetEtiketLogGunSonuVeriler(List<int> etiketIds)
        {
            try
            {
                List<EtiketLogYeni> result = new List<EtiketLogYeni>();

                foreach (var etiketId in etiketIds)
                {
                    var asd = _context.EtiketLogYeni.AsNoTracking().Where(x => x.EtiketId == etiketId && x.KayitZamani.Value.Date == DateTime.Now.Date.AddDays(-1)).OrderByDescending(x => x.Id).FirstOrDefault();
                    if (asd != null)
                        result.Add(asd);
                }
                return result;
            }
            catch (Exception ex)
            {

                throw;
            }

        }
        public async Task EditSekil(SekilJson sekilModel)
        {
            //eger g ise bulundugu children degistirecegiz id ile bulup.
            var sekilDb = await _context.Sekil.Where(x => x.Id.ToString() == Regex.Replace(sekilModel.Id, @"\D", "")).FirstOrDefaultAsync();//\D, rakam olmayan karakterleri temsil eder
            if (sekilDb != null)
            {
                var sekil = JsonConvert.DeserializeObject<SekilJson>(sekilDb.SekilJson);
                if (sekil.Type == "g")
                {
                    var child = sekil.Children.Where(x => x.Id == sekilModel.Id).FirstOrDefault();
                    var properties = typeof(SekilJson).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var property in properties)
                    {
                        if (property.Name != "Id")
                        {
                            var newValue = property.GetValue(sekilModel);
                            property.SetValue(child, newValue);
                        }
                    }
                }
                else
                {
                    sekil = sekilModel;
                }
                sekilDb.SekilJson = CustomJsonFormat(sekil);
                //  sekilDb.SekilJson = JsonConvert.SerializeObject(sekil, new JsonSerializerSettings
                //{
                //    NullValueHandling = NullValueHandling.Ignore // Null değerleri yok say,
                //});
                await _context.SaveChangesAsync();
            }
        }

        public string CustomJsonFormat(SekilJson sekil)
        {
            var properties = new List<string>();

            if (sekil.Id != null)
            {
                properties.Add($"id: '{sekil.Id}'");
            }
            if (sekil.Type != null)
            {
                properties.Add($"type: '{sekil.Type}'");
            }
            if (sekil.X.HasValue)
            {
                properties.Add($"x: {sekil.X.Value}");
            }
            if (sekil.Y.HasValue)
            {
                properties.Add($"y: {sekil.Y.Value}");
            }
            if (sekil.CX.HasValue)
            {
                properties.Add($"cX: {sekil.CX.Value}");
            }
            if (sekil.CY.HasValue)
            {
                properties.Add($"cY: {sekil.CY.Value}");
            }
            if (sekil.R.HasValue)
            {
                properties.Add($"r: {sekil.R.Value}");
            }
            if (sekil.Width.HasValue)
            {
                properties.Add($"width: {sekil.Width.Value}");
            }
            if (sekil.Height.HasValue)
            {
                properties.Add($"height: {sekil.Height.Value}");
            }
            if (sekil.Yuzde != null)
            {
                properties.Add($"yuzde: {sekil.Yuzde}");
            }
            if (sekil.Fill != null)
            {
                properties.Add($"fill: '{sekil.Fill}'");
            }
            if (sekil.Text != null)
            {
                properties.Add($"text: '{sekil.Text}'");
            }
            if (sekil.FontSize.HasValue)
            {
                properties.Add($"fontSize: {sekil.FontSize.Value}");
            }
            if (sekil.FontFamily != null)
            {
                properties.Add($"fontFamily: '{sekil.FontFamily}'");
            }
            if (sekil.FontWeight != null)
            {
                properties.Add($"fontWeight: '{sekil.FontWeight}'");
            }
            if (sekil.Href != null)
            {
                properties.Add($"href: '{sekil.Href}'");
            }
            if (sekil.Stroke != null)
            {
                properties.Add($"stroke: '{sekil.Stroke}'");
            }
            if (sekil.StrokeWidth != null)
            {
                properties.Add($"strokeWidth: '{sekil.StrokeWidth}'");
            }
            if (sekil.Opacity != null)
            {
                properties.Add($"opacity: '{sekil.Opacity}'");
            }
            if (sekil.StrokeDasharray != null)
            {
                properties.Add($"strokeDasharray: '{sekil.StrokeDasharray}'");
            }
            if (sekil.Rotation.HasValue)
            {
                properties.Add($"rotation: {sekil.Rotation.Value}");
            }
            if (sekil.ZIndex.HasValue)
            {
                properties.Add($"zIndex: {sekil.ZIndex.Value}");
            }
            if (sekil.Value != null)
            {
                properties.Add($"value: {sekil.Value}");
            }
            if (sekil.D != null)
            {
                properties.Add($"d: '{sekil.D}'");
            }
            if (sekil.AnimationFrom != null)
            {
                properties.Add($"animationFrom: '{sekil.AnimationFrom}'");
            }
            if (sekil.AnimationTo != null)
            {
                properties.Add($"animationTo: '{sekil.AnimationTo}'");
            }
            if (sekil.AnimationDuration != null)
            {
                properties.Add($"animationDuration: '{sekil.AnimationDuration}'");
            }

            // Children kısmını ekleme
            if (sekil.Children != null && sekil.Children.Count > 0)
            {
                var childrenJson = sekil.Children.Select(child => CustomJsonFormat(child)); // Recursive çağrı
                properties.Add($"children: [{string.Join(", ", childrenJson)}]");
            }

            return $"{{ {string.Join(", ", properties)} }}";
        }

        public async Task<List<KullaniciSekilYetki>> GetKullaniciSekilYetki2(List<int> sekilIds, string userId)
        {
            return await _context.KullaniciSekilYetki.Where(x => sekilIds.Contains(x.SekilId.GetValueOrDefault(0)) && x.UserId == userId).ToListAsync();
        }

        public async Task<List<KullaniciSekilYetki>> GetKullaniciSekilYetki(int cizimId, string userId)
        {
            List<KullaniciSekilYetki> kullaniciSekilYetkiler = new List<KullaniciSekilYetki>();
            var dmYetkiler = await _context.KullaniciSekilYetki.Where(x => x.CizimId == cizimId && x.UserId == userId).ToListAsync();
            //var lokasyonCizimler = await _context.CizimLokasyonEsleme.Where(x => x.LokasyonId == cizimId && x.Silindi != true).Select(x => x.CizimId).ToListAsync();
            var cizimSekiller = await _context.CizimSekilEsleme.Where(x => x.CizimId == cizimId && x.Silindi != true).Select(x => x.SekilId).ToListAsync();
            var sekilYetkiler = await _context.KullaniciSekilYetki.Where(x => cizimSekiller.Contains(x.SekilId.GetValueOrDefault(0)) && x.UserId == userId).ToListAsync();
            kullaniciSekilYetkiler.AddRange(dmYetkiler);
            kullaniciSekilYetkiler.AddRange(sekilYetkiler);
            return kullaniciSekilYetkiler;
        }

        public async Task UpdateDMModTuru(int cizimId, string modTuru, string userId)
        {
            var kullaniciSekilYetki = await _context.KullaniciSekilYetki.Where(x => x.CizimId == cizimId && x.UserId == userId && x.ModTuru != null && (x.ModTuru == "gunluk" || x.ModTuru == "anlik")).FirstOrDefaultAsync();
            if (kullaniciSekilYetki != null)
            {
                kullaniciSekilYetki.ModTuru = modTuru;
                _context.KullaniciSekilYetki.Update(kullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
            else
            {
                kullaniciSekilYetki = new KullaniciSekilYetki
                {
                    CizimId = cizimId,
                    UserId = userId,
                    ModTuru = modTuru
                };
                await _context.KullaniciSekilYetki.AddAsync(kullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateTankModTuru(int cizimId, string modTuru, string userId)
        {
            var kullaniciSekilYetki = await _context.KullaniciSekilYetki.Where(x => x.CizimId == cizimId && x.UserId == userId && x.ModTuru != null && (x.ModTuru == "miktar" || x.ModTuru == "seviye")).FirstOrDefaultAsync();
            if (kullaniciSekilYetki != null)
            {
                kullaniciSekilYetki.ModTuru = modTuru;
                _context.KullaniciSekilYetki.Update(kullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
            else
            {
                kullaniciSekilYetki = new KullaniciSekilYetki
                {
                    CizimId = cizimId,
                    UserId = userId,
                    ModTuru = modTuru
                };
                await _context.KullaniciSekilYetki.AddAsync(kullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateSekilModTuru2(string sekilId, string targetSekilId, string modTuru, string userId)
        {
            string sekilIdSayisi = new string(sekilId.Where(char.IsDigit).ToArray());
            int sekilIdSayi = int.Parse(sekilIdSayisi);
            string targetSekilIdSayisi = new string(targetSekilId.Where(char.IsDigit).ToArray());
            int targetSekilIdSayi = int.Parse(targetSekilIdSayisi);
            var kullaniciSekilYetki = await _context.KullaniciSekilYetki.Where(x => x.UserId == userId && x.SekilId == targetSekilIdSayi && x.ModTuru != null).FirstOrDefaultAsync();
            if (kullaniciSekilYetki != null)
            {
                kullaniciSekilYetki.ModTuru = modTuru;
                _context.KullaniciSekilYetki.Update(kullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
            else
            {
                kullaniciSekilYetki = new KullaniciSekilYetki
                {
                    SekilId = targetSekilIdSayi,
                    UserId = userId,
                    ModTuru = modTuru
                };
                await _context.KullaniciSekilYetki.AddAsync(kullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
            var radyoButonKullaniciSekilYetki = await _context.KullaniciSekilYetki.Where(x => x.UserId == userId && x.SekilId == sekilIdSayi && x.RadioButonModTuru != null).FirstOrDefaultAsync();
            if (radyoButonKullaniciSekilYetki != null)
            {
                radyoButonKullaniciSekilYetki.RadioButonModTuru = modTuru;
                _context.KullaniciSekilYetki.Update(radyoButonKullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
            else
            {
                radyoButonKullaniciSekilYetki = new KullaniciSekilYetki
                {
                    SekilId = sekilIdSayi,
                    UserId = userId,
                    RadioButonModTuru = modTuru
                };
                await _context.KullaniciSekilYetki.AddAsync(radyoButonKullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
        }
        public async Task UpdateSekilPozisyon(string sekilId, int pozisyonX, int pozisyonY, string userId)
        {
            string sekilIdSayisi = new string(sekilId.Where(char.IsDigit).ToArray());
            int sekilIdSayi = int.Parse(sekilIdSayisi);
            var kullaniciSekilYetki = await _context.KullaniciSekilYetki.Where(x => x.UserId == userId && x.SekilId == sekilIdSayi && x.PozisyonX != null && x.PozisyonY != null).FirstOrDefaultAsync();
            if (kullaniciSekilYetki != null)
            {
                kullaniciSekilYetki.PozisyonX = pozisyonX;
                kullaniciSekilYetki.PozisyonY = pozisyonY;
                _context.KullaniciSekilYetki.Update(kullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
            else
            {
                kullaniciSekilYetki = new KullaniciSekilYetki
                {
                    SekilId = sekilIdSayi,
                    UserId = userId,
                    PozisyonX = pozisyonX,
                    PozisyonY = pozisyonY
                };
                await _context.KullaniciSekilYetki.AddAsync(kullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateSekilVisible(string sekilId, bool gorunsunMu, string userId)
        {
            string sekilIdSayisi = new string(sekilId.Where(char.IsDigit).ToArray());
            int sekilIdSayi = int.Parse(sekilIdSayisi);
            var kullaniciSekilYetki = await _context.KullaniciSekilYetki.Where(x => x.UserId == userId && x.SekilId == sekilIdSayi && x.Visible != null).FirstOrDefaultAsync();
            if (kullaniciSekilYetki != null)
            {
                kullaniciSekilYetki.Visible = gorunsunMu;
                _context.KullaniciSekilYetki.Update(kullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
            else
            {
                kullaniciSekilYetki = new KullaniciSekilYetki
                {
                    SekilId = sekilIdSayi,
                    UserId = userId,
                    Visible = gorunsunMu,
                };
                await _context.KullaniciSekilYetki.AddAsync(kullaniciSekilYetki);
                await _context.SaveChangesAsync();
            }
        }
    }
}