using MapsterMapper;
using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.ViewModels;
using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PanelIdentification.Application.Services.Core;
using System.Data;
using System.Globalization;
using static PoultryHouseScada.Domain.Models.EtiketKuralJson;

namespace PoultryHouseScada.Application.Services
{
    public class CizimAppService : AppService<Cizim, CizimViewModel>, ICizimAppService
    {
        private readonly ICizimRepository _cizimRepository;
        private readonly IKumesRepository _kumesRepository;
        //private readonly RedisEtiketService _redisEtiketService;

        public CizimAppService(IMapper mapper, ICizimRepository cizimRepository, IKumesRepository kumesRepository/*, RedisEtiketService redisEtiketService*/) : base(mapper, cizimRepository)
        {
            _cizimRepository = cizimRepository;
            _kumesRepository = kumesRepository;
            //_redisEtiketService = redisEtiketService;
        }

        public async Task<List<SekilJson>> GetCizimJsonByNameWithValue(string cizimName)
        {
            var cizim = await _cizimRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Name == cizimName);
            if (cizim == null)
            {
                throw new Exception("Çizim bulunamadı!");
            }
            //cizim etiket eslemeden etiketleri bul
            var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizim.Id);
            var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());

            //etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
            var veriler = await _cizimRepository.GetEtiketLogVeriler(); //TODO
            var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
            var listElements = new List<SekilJson>();
            foreach (var sekil in sekiller)
            {
                var jsonObj = JsonConvert.DeserializeObject<SekilJson>(sekil.SekilJson);
                var kuralJsons = etiketKurallar.Where(x => x.SekilId == sekil.Id).ToList();

                foreach (var kuralJson in kuralJsons)
                {
                    var veri = veriler.Where(x => x.EtiketId == kuralJson.EtiketId).FirstOrDefault();
                    if (veri != null)
                    {
                        var rules = JsonConvert.DeserializeObject<Rule1>(kuralJson.EtiketKuralJson);
                        if (rules == null)
                        {
                            continue;
                        }
                        if (rules.Children != null)
                        {
                            foreach (var childRule in rules.Children)
                            {
                                if (jsonObj.Children != null)
                                {
                                    foreach (var child in jsonObj.Children)
                                    {
                                        if (child.Id == childRule.Id) // Örneğin, '491c' id'sine sahip çocuğu kontrol et
                                        {
                                            foreach (var rule in childRule.Rules)
                                            {
                                                if (rule.ComparisonType == "numeric")
                                                {
                                                    if (veri.Deger < kuralJson.MinDeger)
                                                    {
                                                        //rule.BelowMin
                                                        child.SetPropertyValue(rule.PropertyName, rule.BelowMin);
                                                    }
                                                    else if (veri.Deger > kuralJson.MaxDeger)
                                                    {
                                                        //rule.AboveMax
                                                        child.SetPropertyValue(rule.PropertyName, rule.AboveMax);
                                                    }
                                                    else if (veri.Deger >= kuralJson.MinDeger && veri.Deger <= kuralJson.MaxDeger)
                                                    {
                                                        //rule.WithinRange
                                                        child.SetPropertyValue(rule.PropertyName, rule.WithinRange);
                                                    }
                                                    else
                                                    {
                                                        //true - false yonetimi
                                                    }
                                                }
                                                else if (rule.ComparisonType == "text")
                                                {
                                                    child.SetPropertyValue(rule.PropertyName, veri.Deger.ToString());
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (childRule.Rules == null)
                                    {
                                        continue;
                                    }
                                    foreach (var rule in childRule.Rules)
                                    {
                                        if (rule.ComparisonType == "numeric")
                                        {
                                            {
                                                jsonObj.SetPropertyValue(rule.PropertyName, veri.Deger.ToString());
                                            }
                                            if (veri.Deger < kuralJson.MinDeger)
                                            {
                                                //rule.BelowMin
                                                jsonObj.SetPropertyValue(rule.PropertyName, rule.BelowMin);
                                            }
                                            else if (veri.Deger > kuralJson.MaxDeger)
                                            {
                                                //rule.AboveMax
                                                jsonObj.SetPropertyValue(rule.PropertyName, rule.AboveMax);
                                            }
                                            else if (veri.Deger >= kuralJson.MinDeger && veri.Deger <= kuralJson.MaxDeger)
                                            {
                                                //rule.WithinRange
                                                jsonObj.SetPropertyValue(rule.PropertyName, rule.WithinRange);
                                            }
                                            else
                                            {
                                                //true - false yonetimi
                                            }
                                        }
                                        else if (rule.ComparisonType == "text")
                                        {
                                            jsonObj.SetPropertyValue(rule.PropertyName, veri.Deger.ToString());
                                        }
                                    }
                                }
                            }
                        }
                        if (rules.Rules == null)
                        {
                            continue;
                        }
                        foreach (var rule in rules.Rules)
                        {
                            if (rule.ComparisonType == "numeric")
                            {
                                {
                                    jsonObj.SetPropertyValue(rule.PropertyName, veri.Deger.ToString());
                                }
                                if (veri.Deger < kuralJson.MinDeger)
                                {
                                    //rule.BelowMin
                                    jsonObj.SetPropertyValue(rule.PropertyName, rule.BelowMin);
                                }
                                else if (veri.Deger > kuralJson.MaxDeger)
                                {
                                    //rule.AboveMax
                                    jsonObj.SetPropertyValue(rule.PropertyName, rule.AboveMax);
                                }
                                else if (veri.Deger >= kuralJson.MinDeger && veri.Deger <= kuralJson.MaxDeger)
                                {
                                    //rule.WithinRange
                                    jsonObj.SetPropertyValue(rule.PropertyName, rule.WithinRange);
                                }
                                else
                                {
                                    //true - false yonetimi
                                }
                            }
                            else if (rule.ComparisonType == "text")
                            {
                                jsonObj.SetPropertyValue(rule.PropertyName, veri.Deger.ToString());
                            }
                        }
                    }
                }
                //if (jsonObj.Type == "g")
                //    {
                //        if (jsonObj.Children != null)
                //        {
                //            jsonObj.Children.Last().Stroke = "green";
                //            jsonObj.Children.Last().Value = "1";
                //        }
                //    }
                listElements.Add(jsonObj);
            }
            //string json = JsonConvert.SerializeObject(listElements, Formatting.Indented);

            return listElements;
        }

        static JToken FindElementById(JToken token, string id)
        {
            if (token == null)
                return null;

            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                if (obj["id"] != null && obj["id"].ToString() == id)
                {
                    return obj;
                }

                foreach (var property in obj.Properties())
                {
                    var found = FindElementById(property.Value, id);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)token)
                {
                    var found = FindElementById(item, id);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        public async Task EditSekil(SekilJson sekil)
        {
            await _cizimRepository.EditSekil(sekil);
        }

        public async Task<List<ShapeEditorEtiketViewModel>> GetShapeEditorEtiketler()
        {
            var etiketler = await _cizimRepository.GetEtiketler();
            return etiketler.Select(x => new ShapeEditorEtiketViewModel
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();
        }

        public async Task<List<ShapeEditorRuleViewModel>> GetShapeEditorKurallar(string shapeJsonId)
        {
            var sekilId = await _cizimRepository.GetSekilIdByJsonId(shapeJsonId);
            if (!sekilId.HasValue)
            {
                return new List<ShapeEditorRuleViewModel>();
            }

            var kurallar = await _cizimRepository.GetEtiketKural(new List<int> { sekilId.Value });
            return kurallar.Select(ToShapeEditorRule).ToList();
        }

        public async Task<ShapeEditorRuleViewModel> SaveShapeEditorRule(SaveShapeEditorRuleViewModel model)
        {
            var sekilId = await _cizimRepository.GetSekilIdByJsonId(model.ShapeJsonId);
            if (!sekilId.HasValue)
            {
                throw new Exception("Kuralın bağlanacağı şekil bulunamadı.");
            }

            if (model.EtiketId < 1 || !(await _cizimRepository.GetEtiketler()).Any(x => x.Id == model.EtiketId))
            {
                throw new Exception("Geçerli bir etiket seçin.");
            }

            if (model.MinDeger > model.MaxDeger)
            {
                throw new Exception("Minimum değer maksimum değerden büyük olamaz.");
            }

            Rule1? parsedRule;
            try
            {
                parsedRule = JsonConvert.DeserializeObject<Rule1>(model.EtiketKuralJson);
            }
            catch (JsonException)
            {
                throw new Exception("Kural JSON biçimi geçersiz.");
            }

            if (parsedRule == null || (parsedRule.Rules?.Count > 0) != true && (parsedRule.Children?.Count > 0) != true)
            {
                throw new Exception("En az bir özellik kuralı tanımlayın.");
            }

            var saved = await _cizimRepository.SaveEtiketKural(new SekilEtiketKural
            {
                Id = model.Id.GetValueOrDefault(),
                Name = model.Name?.Trim(),
                SekilId = sekilId.Value,
                EtiketId = model.EtiketId,
                MinDeger = model.MinDeger,
                MaxDeger = model.MaxDeger,
                VeriYok = model.VeriYok,
                EtiketKuralJson = JsonConvert.SerializeObject(parsedRule)
            });

            return ToShapeEditorRule(saved);
        }

        public Task DeleteShapeEditorRule(int id)
        {
            return _cizimRepository.DeleteEtiketKural(id);
        }

        private static ShapeEditorRuleViewModel ToShapeEditorRule(SekilEtiketKural rule)
        {
            return new ShapeEditorRuleViewModel
            {
                Id = rule.Id,
                Name = rule.Name,
                SekilId = rule.SekilId,
                EtiketId = rule.EtiketId,
                MinDeger = rule.MinDeger,
                MaxDeger = rule.MaxDeger,
                VeriYok = rule.VeriYok.GetValueOrDefault(),
                EtiketKuralJson = rule.EtiketKuralJson
            };
        }

        public async Task<List<SekilJson>> GetCizimJsonById(int cizimId)
        {
            try
            {


                var cizim = await _cizimRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Id == cizimId);
                if (cizim == null)
                {
                    throw new Exception("Çizim bulunamadı!");
                }
                //cizim etiket eslemeden etiketleri bul
                var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizim.Id);
                var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());

                //etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
                var veriler = await _cizimRepository.GetEtiketLogKonveyorVeriler(); //TODO
                var listElements = new List<SekilJson>();
                var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
                foreach (var sekil in sekiller)
                {
                    var jsonObj = JsonConvert.DeserializeObject<SekilJson>(sekil.SekilJson);
                    var kuralJsons = etiketKurallar.Where(x => x.SekilId == sekil.Id).ToList();

                    // JSON objesi ve veriler işleniyor
                    foreach (var kuralJson in kuralJsons)
                    {
                        var veri = veriler.FirstOrDefault(x => x.EtiketId == kuralJson.EtiketId);
                        if (veri == null) continue;

                        // Sensör değeri yoksa ve işleme devam etmemiz gerekmiyorsa
                        if (veri.Deger == null && !kuralJson.VeriYok.GetValueOrDefault(false))
                        {
                            continue;
                        }

                        var rules = JsonConvert.DeserializeObject<Rule1>(kuralJson.EtiketKuralJson);
                        if (rules == null) continue;

                        // Çocuk kuralları işleme
                        if (rules.Children != null)
                        {
                            ProcessChildRules(rules.Children, jsonObj, null, veri, kuralJson);
                        }

                        // Ana kuralları işleme
                        if (rules.Rules != null)
                        {
                            ProcessRules(rules.Rules, jsonObj, null, veri, kuralJson);
                        }
                    }
                    listElements.Add(jsonObj);
                }
                return listElements;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        //public class RuleProcessor
        //{
        //    public bool Evaluate(RuleSet ruleSet, List<EtiketLog_Temp> veriler)
        //    {
        //        var logic = ruleSet.Logic?.ToLower() ?? "and"; // "and" veya "or"
        //        var results = new List<bool>();

        //        // Kurallar üzerinde dön
        //        foreach (var rule in ruleSet.Rules)
        //        {
        //            // Tek bir kuralı değerlendir
        //            var veri = veriler.OrderByDescending(x => x.Id).FirstOrDefault(v => v.EtiketId == rule.EtiketId);
        //            if (veri == null) continue; // Veri bulunamadıysa sonraki kurala geç

        //            // Kuralı değerlendir
        //            bool result = EvaluateRule(rule, veri);
        //            results.Add(result);
        //        }

        //        // Logic'e göre sonuçları değerlendir
        //        return logic switch
        //        {
        //            "and" => results.All(r => r),
        //            "or" => results.Any(r => r),
        //            _ => false,
        //        };
        //    }

        //    private bool EvaluateRule(Rule2 rule, EtiketLog_Temp veri)
        //    {
        //        var comparison = rule.Comparison?.ToLower(); // Karşılaştırma türünü al
        //        var value = veri.Deger ?? 0;
        //        switch (comparison)
        //        {
        //            case "numeric":
        //                if (value < rule.Min) return false; // Min'in altındaysa
        //                else if (value > rule.Max) return false; // Max'in üstündeyse
        //                else return true;// Range içindeyse
        //            case "numericequal":
        //                return value == rule.Equal; // Sayı karşılaştırması
        //            case "belowmin":
        //                return value < rule.BelowMin; // Sayı karşılaştırması
        //            case "abovemax":
        //                return value > rule.AboveMax; // Sayı karşılaştırması

        //            default:
        //                return false;
        //        }
        //    }
        //}

        // Çocuk kuralları işleme
        void ProcessChildRules(List<Rule1> children, dynamic jsonObj, dynamic? veri, dynamic? veriKonveyor, dynamic kuralJson)
        {
            foreach (var childRule in children)
            {
                foreach (var child in jsonObj.Children ?? Enumerable.Empty<dynamic>())
                {
                    if (child.Id != childRule.Id) continue;

                    foreach (var rule in childRule.Rules)
                    {
                        ApplyRule(rule, child, veri, veriKonveyor, kuralJson);
                    }
                }
            }
        }

        // Kuralları işleme
        void ProcessRules(List<PropertyRule> rules, dynamic jsonObj, dynamic? veri, dynamic? veriKonveyor, dynamic kuralJson)
        {
            foreach (var rule in rules)
            {
                ApplyRule(rule, jsonObj, veri, veriKonveyor, kuralJson);
            }
        }

        // Kuralı uygulama
        void ApplyRule(PropertyRule rule, dynamic targetObj, dynamic? veri, dynamic? veriKonveyor, dynamic kuralJson)
        {
            if (rule.PropertyName.Contains("[") && rule.PropertyName.Contains("]"))
            {
                ProcessListRule(rule, targetObj, veri, veriKonveyor, kuralJson);
            }
            else
            {
                ProcessSinglePropertyRule(rule, targetObj, veri, veriKonveyor, kuralJson);
            }
        }

        // Liste tabanlı kurallar
        bool ProcessListRule(PropertyRule rule, dynamic targetObj, dynamic? veri, dynamic? veriKonveyor, dynamic kuralJson)
        {
            var (listName, indices) = ParseListNameAndIndices(rule.PropertyName);

            var listProperty = targetObj.GetType().GetProperty(listName);
            if (listProperty == null) return false;

            // Tek boyutlu liste durumu
            if (indices.Count == 1) // Burada .Length yerine .Count kullanılmalı
            {
                var list = (List<string>)listProperty.GetValue(targetObj);
                int index = indices[0];

                if (index >= 0 && index < list.Count)
                {
                    UpdateListItem(list, index, rule, veri, kuralJson);
                }
            }
            // Çok boyutlu liste durumu
            else if (indices.Count == 2) // Burada da .Length yerine .Count kullanılmalı
            {
                var multiList = (List<List<string>>)listProperty.GetValue(targetObj);
                int rowIndex = indices[0];
                int colIndex = indices[1];

                if (rowIndex >= 0 && rowIndex < multiList.Count &&
                    colIndex >= 0 && colIndex < multiList[rowIndex].Count)
                {
                    UpdateListItem(multiList[rowIndex], colIndex, rule, veri, kuralJson);
                }
            }
            else
            {
                // Diğer durumlar için uygun işlemleri ekleyebilirsiniz
            }

            return true;
        }

        // Yardımcı metodlar
        (string listName, List<int> indices) ParseListNameAndIndices(string propertyName)
        {
            var listName = propertyName.Substring(0, propertyName.IndexOf("["));
            var indexStr = propertyName.Substring(propertyName.IndexOf("[") + 1,
                propertyName.IndexOf("]") - propertyName.IndexOf("[") - 1);

            var indices = indexStr.Split(',')
                .Select(s => int.TryParse(s, out int idx) ? idx : -1)
                .Where(idx => idx >= 0)
                .ToList();

            return (listName, indices);
        }

        void UpdateListItem(IList<string> list, int index, PropertyRule rule, dynamic veri, dynamic kuralJson)
        {
            if (rule.ComparisonType == "numeric")
            {
                list[index] = veri.Deger < kuralJson.MinDeger ? rule.BelowMin :
                              veri.Deger > kuralJson.MaxDeger ? rule.AboveMax :
                              rule.WithinRange;
            }
            else if (rule.ComparisonType == "numericEqual")
            {
                if (veri.Deger == kuralJson.MinDeger)
                {
                    list[index] = rule.BelowMin;
                }
            }
            else if (rule.ComparisonType == "text")
            {
                list[index] = rule.DecimalPlaces.HasValue
                    ? string.Format(new CultureInfo("tr-TR"), "{0:N" + rule.DecimalPlaces.Value + "}", veri.Deger)
                    : veri.Deger.ToString();
            }
            else if (rule.ComparisonType == "timespan")
            {
                var hours = Convert.ToInt32(Math.Floor(veri.Deger.GetValueOrDefault(0) / 3600));
                var minutes = Convert.ToInt32(Math.Floor((veri.Deger.GetValueOrDefault(0) % 3600) / 60));
                var secs = Convert.ToInt32(Math.Floor(veri.Deger.GetValueOrDefault(0) % 60));
                var text = (hours < 10 ? '0' + hours.ToString() : hours.ToString()) + ":" + (minutes < 10 ? '0' + minutes.ToString() : minutes.ToString()) + ":" + (secs < 10 ? '0' + secs.ToString() : secs.ToString());
                list[index] = text;
            }
            else if (rule.ComparisonType == "equal")
            {
                list[index] = rule.Equal;
            }
        }

        // Tekil özellik kuralları
        void ProcessSinglePropertyRule(PropertyRule rule, dynamic targetObj, EtiketLog_TempYeni? veri, EtiketLogKonveyor? veriKonveyor, dynamic kuralJson)
        {
            if (veri != null)
            {
                switch (rule.ComparisonType)
                {
                    case "numeric":
                        ApplyNumericRule(rule, targetObj, veri, kuralJson);
                        break;
                    case "numericEqual":
                        ApplyNumericEqualRule(rule, targetObj, veri, kuralJson);
                        break;
                    case "text":
                        ApplyTextRule(rule, targetObj, veri);
                        break;
                    case "timespan":
                        var hours = Convert.ToInt32(Math.Floor(veri.Deger.GetValueOrDefault(0) / 3600));
                        var minutes = Convert.ToInt32(Math.Floor((veri.Deger.GetValueOrDefault(0) % 3600) / 60));
                        var secs = Convert.ToInt32(Math.Floor(veri.Deger.GetValueOrDefault(0) % 60));
                        var text = (hours < 10 ? '0' + hours.ToString() : hours.ToString()) + ":" + (minutes < 10 ? '0' + minutes.ToString() : minutes.ToString()) + ":" + (secs < 10 ? '0' + secs.ToString() : secs.ToString());
                        targetObj.SetPropertyValue(rule.PropertyName, text);
                        break;
                    case "equal":
                        targetObj.SetPropertyValue(rule.PropertyName, rule.Equal);
                        break;
                        // Diğer kurallar...
                }
            }
            else if (veriKonveyor != null)
            {
                switch (rule.ComparisonType)
                {
                    case "numeric":
                        ApplyNumericRule(rule, targetObj, veriKonveyor, kuralJson);
                        break;
                    case "numericEqual":
                        ApplyNumericEqualRule(rule, targetObj, veriKonveyor, kuralJson);
                        break;
                    case "text":
                        ApplyTextRule(rule, targetObj, veriKonveyor);
                        break;
                    case "timespan":
                        var hours = Convert.ToInt32(Math.Floor(veriKonveyor.Deger.GetValueOrDefault(0) / 3600));
                        var minutes = Convert.ToInt32(Math.Floor((veriKonveyor.Deger.GetValueOrDefault(0) % 3600) / 60));
                        var secs = Convert.ToInt32(Math.Floor(veriKonveyor.Deger.GetValueOrDefault(0) % 60));
                        var text = (hours < 10 ? '0' + hours.ToString() : hours.ToString()) + ":" + (minutes < 10 ? '0' + minutes.ToString() : minutes.ToString()) + ":" + (secs < 10 ? '0' + secs.ToString() : secs.ToString());
                        targetObj.SetPropertyValue(rule.PropertyName, text);
                        break;
                    case "equal":
                        targetObj.SetPropertyValue(rule.PropertyName, rule.Equal);
                        break;
                        // Diğer kurallar...
                }
            }
        }
        void ApplyNumericEqualRule(PropertyRule rule, dynamic targetObj, dynamic? veri, dynamic kuralJson)
        {
            if (veri.Deger == kuralJson.MinDeger)
            {
                targetObj.SetPropertyValue(rule.PropertyName, rule.BelowMin);
            }
        }
        void ApplyNumericRule(PropertyRule rule, dynamic targetObj, dynamic veri, dynamic kuralJson)
        {
            if (veri.Deger < kuralJson.MinDeger)
            {
                targetObj.SetPropertyValue(rule.PropertyName, rule.BelowMin);
            }
            else if (veri.Deger > kuralJson.MaxDeger)
            {
                targetObj.SetPropertyValue(rule.PropertyName, rule.AboveMax);
            }
            else
            {
                targetObj.SetPropertyValue(rule.PropertyName, rule.WithinRange);
            }
        }
        void ApplyTextRule(PropertyRule rule, dynamic targetObj, dynamic veri)
        {
            var value = rule.DecimalPlaces.HasValue
                ? string.Format(new CultureInfo("tr-TR"), "{0:N" + rule.DecimalPlaces.Value + "}", veri.Deger)
                : veri.Deger.ToString();

            targetObj.SetPropertyValue(rule.PropertyName, value);
        }

        public async Task<List<SekilJson>> GetCizimJsonByKumesId(int kumesId)
        {
            var listElements = new List<SekilJson>();

            //var kumes = await _kumesRepository.GetByIdAsync(kumesId);
            //if (kumes == null)
            //{
            //    throw new Exception("Kümes bulunamadı!");
            //}
            var sekilIds = await _cizimRepository.GetSekilIdsByKumesId(kumesId);


            //cizim etiket eslemeden etiketleri bul
            var sekiller = await _cizimRepository.GetCizimEtiketler(sekilIds);

            //etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
            var veriler = await _cizimRepository.GetEtiketLogVerilerByKumesId(kumesId); //TODO
                                                                                        //var veriler = await _redisEtiketService.GetEtiketLogVerilerByKumesId(kumesId);
            var etiketKurallar = await _cizimRepository.GetEtiketKural(sekilIds);
            foreach (var sekil in sekiller)
            {
                var jsonObj = JsonConvert.DeserializeObject<SekilJson>(sekil.SekilJson);
                var kuralJsons = etiketKurallar.Where(x => x.SekilId == sekil.Id).ToList();

                // JSON objesi ve veriler işleniyor
                foreach (var kuralJson in kuralJsons)
                {
                    var veri = veriler.FirstOrDefault(x => x.EtiketId == kuralJson.EtiketId);
                    if (veri == null) continue;

                    // Sensör değeri yoksa ve işleme devam etmemiz gerekmiyorsa
                    if (veri.Deger == null && !kuralJson.VeriYok.GetValueOrDefault(false))
                    {
                        continue;
                    }

                    var rules = JsonConvert.DeserializeObject<Rule1>(kuralJson.EtiketKuralJson);
                    if (rules == null) continue;

                    // Çocuk kuralları işleme
                    if (rules.Children != null)
                    {
                        ProcessChildRules(rules.Children, jsonObj, veri, null, kuralJson);
                    }

                    // Ana kuralları işleme
                    if (rules.Rules != null)
                    {
                        ProcessRules(rules.Rules, jsonObj, veri, null, kuralJson);
                    }
                }
                listElements.Add(jsonObj);
            }
            return listElements;
        }
    }
}
