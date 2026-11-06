using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;

namespace PoultryHouseScada.Infrastructure.Repositories
{
    public class CizimRepository : Repository<Cizim>, ICizimRepository
    {
        private readonly KumesScadaDbContext _context;
        public CizimRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<List<int>> GetSekilIdsByKumesId(int kumesId)
        {
            var sekilIds = new List<int>();
            var cizimler = await _context.CizimKumesEsleme.AsNoTracking().Where(x => x.KumesId == kumesId && (x.CizimId.GetValueOrDefault(0) > 0) && x.Silindi != true).Select(x => x.CizimId).ToListAsync();
            if (cizimler.Count > 0)
            {
                var cizimSekiller = await _context.CizimSekilEsleme.AsNoTracking().Where(x => cizimler.Contains(x.CizimId) && x.Silindi != true).Select(x => x.SekilId).ToListAsync();
                sekilIds.AddRange(cizimSekiller);
            }
            var sekiller = await _context.CizimKumesEsleme.AsNoTracking().Where(x => x.KumesId == kumesId && (x.SekilId.GetValueOrDefault(0) > 0) && x.Silindi != true).Select(x => x.SekilId.GetValueOrDefault(0)).ToListAsync();
            sekilIds.AddRange(sekiller);

            return sekilIds;
        }

        public async Task<List<CizimSekilEsleme>> GetCizimSekilEslemesByCizimId(int cizimId)
        {
            return await _context.CizimSekilEsleme.AsNoTracking().Where(x => x.CizimId == cizimId && x.Silindi != true).ToListAsync();
        }

        public async Task<List<Sekil>> GetCizimEtiketler(List<int> sekilIds)
        {
            return await _context.Sekil.AsNoTracking().Where(x => sekilIds.Contains(x.Id)).ToListAsync();
        }

        public async Task<List<SekilEtiketKural>> GetEtiketKural()
        {
            return await _context.SekilEtiketKural.AsNoTracking().Where(x => x.Silindi != true).ToListAsync();
        }

        public async Task<List<SekilEtiketKural>> GetEtiketKural(List<int> sekilIds)
        {
            return await _context.SekilEtiketKural.AsNoTracking().Where(x => sekilIds.Contains(x.SekilId) && x.Silindi != true).ToListAsync();
        }

        public async Task<List<Etiket>> GetEtiketler()
        {
            return await _context.Etiket.AsNoTracking()
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<int?> GetSekilIdByJsonId(string shapeJsonId)
        {
            var numericPart = Regex.Replace(shapeJsonId ?? string.Empty, @"\D", string.Empty);
            if (!int.TryParse(numericPart, out var sekilId))
            {
                return null;
            }

            return await _context.Sekil.AsNoTracking()
                .Where(x => x.Id == sekilId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<SekilEtiketKural> SaveEtiketKural(SekilEtiketKural kural)
        {
            if (kural.Id > 0)
            {
                var existing = await _context.SekilEtiketKural.FirstOrDefaultAsync(x => x.Id == kural.Id);
                if (existing == null)
                {
                    throw new Exception("Kural bulunamadı.");
                }

                existing.Name = kural.Name;
                existing.SekilId = kural.SekilId;
                existing.EtiketId = kural.EtiketId;
                existing.MinDeger = kural.MinDeger;
                existing.MaxDeger = kural.MaxDeger;
                existing.VeriYok = kural.VeriYok;
                existing.EtiketKuralJson = kural.EtiketKuralJson;
                existing.Silindi = false;
                kural = existing;
            }
            else
            {
                kural.Silindi = false;
                await _context.SekilEtiketKural.AddAsync(kural);
            }

            await _context.SaveChangesAsync();
            return kural;
        }

        public async Task DeleteEtiketKural(int id)
        {
            var kural = await _context.SekilEtiketKural.FirstOrDefaultAsync(x => x.Id == id);
            if (kural == null)
            {
                throw new Exception("Kural bulunamadı.");
            }

            kural.Silindi = true;
            await _context.SaveChangesAsync();
        }

        //public async Task<List<EtiketLog_Temp>> GetEtiketLogVeriler()
        //{
        //    return await _context.EtiketLog_Temp.AsNoTracking().Where(x => x.KayitZamani.Value >= DateTime.Now.AddMinutes(-5)).OrderByDescending(x => x.Id).ToListAsync();
        //}

        //public async Task<List<EtiketLog_Temp>> GetEtiketLogVerilerByKumesId(int kumesId)
        //{
        //    return await _context.EtiketLog_Temp.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value >= DateTime.Now.AddMinutes(-5)).OrderByDescending(x => x.Id).ToListAsync();
        //}

        public async Task<List<EtiketLog_TempYeni>> GetEtiketLogVeriler()
        {
            return await _context.EtiketLog_TempYeni.AsNoTracking().Where(x => x.KayitZamani.Value >= DateTime.Now.AddMinutes(-5)).OrderByDescending(x => x.Id).ToListAsync();
        }

        public async Task<List<EtiketLog_TempYeni>> GetEtiketLogVerilerByKumesId(int kumesId)
        {
            return await _context.EtiketLog_TempYeni.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value >= DateTime.Now.AddMinutes(-2)).OrderByDescending(x => x.Id).ToListAsync();
        }

        public async Task<List<EtiketLogKonveyor>> GetEtiketLogKonveyorVeriler()
        {
            return await _context.EtiketLogKonveyor.AsNoTracking().Where(x => x.KayitZamani.Value >= DateTime.Now.AddMinutes(-5)).OrderByDescending(x => x.Id).ToListAsync();
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
    }
}
