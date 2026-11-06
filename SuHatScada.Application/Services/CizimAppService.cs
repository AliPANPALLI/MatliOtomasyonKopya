using MapsterMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuHatScada.Application.Interfaces;
using SuHatScada.Application.Services.Core;
using SuHatScada.Application.ViewModels;
using SuHatScada.Domain.Interfaces;
using SuHatScada.Domain.Models;
using System.Data;
using static SuHatScada.Domain.Models.EtiketKuralJson;

namespace SuHatScada.Application.Services
{
    public class CizimAppService : AppService<Cizim, CizimViewModel>, ICizimAppService
    {
        private readonly ICizimRepository _cizimRepository;
        private readonly DMGunlukCacheService _gunlukCacheService;
        private readonly SekilKuralCacheService _sekilKuralCacheService;

        public CizimAppService(IMapper mapper, ICizimRepository cizimRepository, DMGunlukCacheService gunlukCacheService, SekilKuralCacheService sekilKuralCacheService) : base(mapper, cizimRepository)
        {
            _cizimRepository = cizimRepository;
            _gunlukCacheService = gunlukCacheService;
            _sekilKuralCacheService = sekilKuralCacheService;
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


        //public async Task<List<SekilJson>> GetCizimJsonById(int cizimId)
        //{
        //    var cizim = await _cizimRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Id == cizimId);
        //    if (cizim == null)
        //    {
        //        throw new Exception("Çizim bulunamadı!");
        //    }
        //    //cizim etiket eslemeden etiketleri bul
        //    var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizim.Id);
        //    var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());

        //    //etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
        //    var veriler = await _cizimRepository.GetEtiketLogVeriler(); //TODO
        //    var listElements = new List<SekilJson>();
        //    var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
        //    // Genel akış: Ana mantığı burada basitleştiriyoruz
        //    foreach (var kuralJson in kurallar)
        //    {
        //        var veri = veriler.FirstOrDefault(x => x.EtiketId == kuralJson.EtiketId);
        //        if (veri == null) continue;

        //        // Sensör değeri yoksa ve işleme devam etmemiz gerekmiyorsa
        //        if (veri.Deger == null && !kuralJson.VeriYok.GetValueOrDefault(false))
        //        {
        //            continue;
        //        }

        //        var rules = JsonConvert.DeserializeObject<Rule1>(kuralJson.EtiketKuralJson);
        //        if (rules == null) continue;

        //        // Çocuk kuralları işleme
        //        if (rules.Children != null)
        //        {
        //            ProcessChildRules(rules.Children, jsonObj, veri, kuralJson);
        //        }

        //        // Ana kuralları işleme
        //        if (rules.Rules != null)
        //        {
        //            ProcessRules(rules.Rules, jsonObj, veri, kuralJson);
        //        }
        //    }

        //    return listElements;
        //}

        //public bool EvaluateRuleGroup(RuleGroup group, Dictionary<int, object> etiketValues)
        //{
        //    foreach (var rule in group.Rules)
        //    {
        //        if (rule is RuleGroup subGroup)
        //        {
        //            var result = EvaluateRuleGroup(subGroup, etiketValues);
        //            if (group.Logic == "and" && !result) return false;
        //            if (group.Logic == "or" && result) return true;
        //        }
        //        else if (rule is Rule2 singleRule)
        //        {
        //            var value = etiketValues[singleRule.EtiketId];
        //            var result = EvaluateSingleRule(singleRule, value);
        //            if (group.Logic == "and" && !result) return false;
        //            if (group.Logic == "or" && result) return true;
        //        }
        //    }
        //    return group.Logic == "and";
        //}

        //public bool EvaluateSingleRule(Rule2 rule, object value)
        //{
        //    // Burada rule.Comparison ve değer kıyaslamalarını yapabilirsiniz
        //    if (rule.Comparison == "numeric" && value is decimal numValue)
        //    {
        //        if (numValue < rule.Min) return false;
        //        if (numValue > rule.Max) return false;
        //        return true;
        //    }
        //    if (rule.Comparison == "text" && value is string textValue)
        //    {
        //        return textValue == rule.Equal;
        //    }
        //    return false;
        //}

        //// Çocuk kuralları işleme
        //void ProcessChildRules(List<ChildRule> children, dynamic jsonObj, dynamic veri, dynamic kuralJson)
        //{
        //    foreach (var childRule in children)
        //    {
        //        foreach (var child in jsonObj.Children ?? Enumerable.Empty<dynamic>())
        //        {
        //            if (child.Id != childRule.Id) continue;

        //            foreach (var rule in childRule.Rules)
        //            {
        //                ApplyRule(rule, child, veri, kuralJson);
        //            }
        //        }
        //    }
        //}

        //// Kuralları işleme
        //void ProcessRules(List<Rule> rules, dynamic jsonObj, dynamic veri, dynamic kuralJson)
        //{
        //    foreach (var rule in rules)
        //    {
        //        ApplyRule(rule, jsonObj, veri, kuralJson);
        //    }
        //}

        //// Kuralı uygulama
        //void ApplyRule(Rule rule, dynamic targetObj, dynamic veri, dynamic kuralJson)
        //{
        //    if (rule.PropertyName.Contains("[") && rule.PropertyName.Contains("]"))
        //    {
        //        ProcessListRule(rule, targetObj, veri, kuralJson);
        //    }
        //    else
        //    {
        //        ProcessSinglePropertyRule(rule, targetObj, veri, kuralJson);
        //    }
        //}

        //// Liste tabanlı kurallar
        //void ProcessListRule(Rule rule, dynamic targetObj, dynamic veri, dynamic kuralJson)
        //{
        //    var (listName, indices) = ParseListNameAndIndices(rule.PropertyName);

        //    var listProperty = targetObj.GetType().GetProperty(listName);
        //    if (listProperty == null) return;

        //    var list = listProperty.GetValue(targetObj) as IList<string>;
        //    if (list == null) return;

        //    foreach (var index in indices)
        //    {
        //        if (index >= 0 && index < list.Count)
        //        {
        //            UpdateListItem(list, index, rule, veri, kuralJson);
        //        }
        //    }
        //}

        //// Tekil özellik kuralları
        //void ProcessSinglePropertyRule(Rule rule, dynamic targetObj, dynamic veri, dynamic kuralJson)
        //{
        //    switch (rule.ComparisonType)
        //    {
        //        case "numeric":
        //            ApplyNumericRule(rule, targetObj, veri, kuralJson);
        //            break;
        //        case "text":
        //            ApplyTextRule(rule, targetObj, veri);
        //            break;
        //        case "equal":
        //            targetObj.SetPropertyValue(rule.PropertyName, rule.Equal);
        //            break;
        //            // Diğer kurallar...
        //    }
        //}

        //// Yardımcı metodlar
        //(string listName, List<int> indices) ParseListNameAndIndices(string propertyName)
        //{
        //    var listName = propertyName.Substring(0, propertyName.IndexOf("["));
        //    var indexStr = propertyName.Substring(propertyName.IndexOf("[") + 1,
        //        propertyName.IndexOf("]") - propertyName.IndexOf("[") - 1);

        //    var indices = indexStr.Split(',')
        //        .Select(s => int.TryParse(s, out int idx) ? idx : -1)
        //        .Where(idx => idx >= 0)
        //        .ToList();

        //    return (listName, indices);
        //}

        //void UpdateListItem(IList<string> list, int index, Rule rule, dynamic veri, dynamic kuralJson)
        //{
        //    if (rule.ComparisonType == "numeric")
        //    {
        //        list[index] = veri.Deger < kuralJson.MinDeger ? rule.BelowMin :
        //                      veri.Deger > kuralJson.MaxDeger ? rule.AboveMax :
        //                      rule.WithinRange;
        //    }
        //    else if (rule.ComparisonType == "text")
        //    {
        //        list[index] = rule.DecimalPlaces.HasValue
        //            ? string.Format("{0:N" + rule.DecimalPlaces.Value + "}", veri.Deger)
        //            : veri.Deger.ToString();
        //    }
        //    else if (rule.ComparisonType == "equal")
        //    {
        //        list[index] = rule.Equal;
        //    }
        //}

        //void ApplyNumericRule(Rule2 rule, dynamic targetObj, dynamic veri, dynamic kuralJson)
        //{
        //    if (veri.Deger < kuralJson.MinDeger)
        //    {
        //        targetObj.SetPropertyValue(rule.PropertyName, rule.BelowMin);
        //    }
        //    else if (veri.Deger > kuralJson.MaxDeger)
        //    {
        //        targetObj.SetPropertyValue(rule.PropertyName, rule.AboveMax);
        //    }
        //    else
        //    {
        //        targetObj.SetPropertyValue(rule.PropertyName, rule.WithinRange);
        //    }
        //}

        //void ApplyTextRule(Rule2 rule, dynamic targetObj, dynamic veri)
        //{
        //    var value = rule.DecimalPlaces.HasValue
        //        ? string.Format("{0:N" + rule.DecimalPlaces.Value + "}", veri.Deger)
        //        : veri.Deger.ToString();

        //    targetObj.SetPropertyValue(rule.PropertyName, value);
        //}
        public class RuleProcessor
        {
            public bool Evaluate(RuleSet ruleSet, List<EtiketLog_TempYeni> veriler)
            {
                var logic = ruleSet.Logic?.ToLower() ?? "and"; // "and" veya "or"
                var results = new List<bool>();

                // Kurallar üzerinde dön
                foreach (var rule in ruleSet.Rules)
                {
                    // Tek bir kuralı değerlendir
                    var veri = veriler.OrderByDescending(x => x.Id).FirstOrDefault(v => v.EtiketId == rule.EtiketId);
                    if (veri == null) continue; // Veri bulunamadıysa sonraki kurala geç

                    // Kuralı değerlendir
                    bool result = EvaluateRule(rule, veri);
                    results.Add(result);
                }

                // Logic'e göre sonuçları değerlendir
                return logic switch
                {
                    "and" => results.All(r => r),
                    "or" => results.Any(r => r),
                    _ => false,
                };
            }

            private bool EvaluateRule(Rule2 rule, EtiketLog_TempYeni veri)
            {
                var comparison = rule.Comparison?.ToLower(); // Karşılaştırma türünü al
                var value = veri.Deger ?? 0;
                switch (comparison)
                {
                    case "numeric":
                        if (value < rule.Min) return false; // Min'in altındaysa
                        else if (value > rule.Max) return false; // Max'in üstündeyse
                        else return true;// Range içindeyse
                    case "numericequal":
                        return value == rule.Equal; // Sayı karşılaştırması
                    case "belowmin":
                        return value < rule.BelowMin; // Sayı karşılaştırması
                    case "abovemax":
                        return value > rule.AboveMax; // Sayı karşılaştırması

                    default:
                        return false;
                }
            }
        }

        // Çocuk kuralları işleme
        async Task ProcessChildRules(List<Rule1> children, dynamic jsonObj, dynamic veri, dynamic kuralJson)
        {
            foreach (var childRule in children)
            {
                foreach (var child in jsonObj.Children ?? Enumerable.Empty<dynamic>())
                {
                    if (child.Id != childRule.Id) continue;

                    foreach (var rule in childRule.Rules)
                    {
                        await ApplyRule(rule, child, veri, kuralJson);
                    }
                }
            }
        }

        // Kuralları işleme
        async Task ProcessRules(List<PropertyRule> rules, dynamic jsonObj, dynamic veri, dynamic kuralJson)
        {
            foreach (var rule in rules)
            {
                await ApplyRule(rule, jsonObj, veri, kuralJson);
            }
        }

        // Kuralı uygulama
        async Task ApplyRule(PropertyRule rule, dynamic targetObj, dynamic veri, dynamic kuralJson)
        {
            if (rule.PropertyName.Contains("[") && rule.PropertyName.Contains("]"))
            {
                await ProcessListRule(rule, targetObj, veri, kuralJson);
            }
            else
            {
                await ProcessSinglePropertyRule(rule, targetObj, veri, kuralJson);
            }
        }

        // Liste tabanlı kurallar
        async Task<bool> ProcessListRule(PropertyRule rule, dynamic targetObj, dynamic veri, dynamic kuralJson)
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
                    await UpdateListItem(list, index, rule, veri, kuralJson);
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
                    await UpdateListItem(multiList[rowIndex], colIndex, rule, veri, kuralJson);
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

        async Task UpdateListItem(IList<string> list, int index, PropertyRule rule, dynamic veri, dynamic kuralJson, List<EtiketLogYeni> gunSonuVeriler)
        {
            if (rule.ComparisonType == "numeric")
            {
                if (rule.DecimalPlaces.HasValue)
                {
                    list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(veri.Deger < kuralJson.MinDeger ? rule.BelowMin :
                              veri.Deger > kuralJson.MaxDeger ? rule.AboveMax :
                              rule.WithinRange));
                }
                else
                {
                    list[index] = veri.Deger < kuralJson.MinDeger ? rule.BelowMin :
                              veri.Deger > kuralJson.MaxDeger ? rule.AboveMax :
                              rule.WithinRange;
                }
            }
            else if (rule.ComparisonType == "numericEqual")
            {
                if (veri.Deger == kuralJson.MinDeger)
                {
                    if (rule.DecimalPlaces.HasValue)
                    {
                        list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(rule.BelowMin));
                    }
                    else
                    {
                        list[index] = rule.BelowMin;
                    }
                }
            }
            else if (rule.ComparisonType == "text")
            {
                list[index] = rule.DecimalPlaces.HasValue
                    ? string.Format("{0:F" + rule.DecimalPlaces.Value + "}", veri.Deger)
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
            else if (rule.ComparisonType == "gunlukFark")
            {
                //var oncekiGunSonuEtiketDeger = _cizimRepository.GetEtiketLogGunSonuVeri(kuralJson.EtiketId);
                var oncekiGunSonuEtiketDeger = gunSonuVeriler.Where(x => x.EtiketId == kuralJson.EtiketId).FirstOrDefault();
                if (rule.DecimalPlaces.HasValue)
                {
                    list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", oncekiGunSonuEtiketDeger != null ? (veri.Deger - oncekiGunSonuEtiketDeger.Deger) : 0);
                }
                else
                {
                    list[index] = oncekiGunSonuEtiketDeger != null ? (veri.Deger - oncekiGunSonuEtiketDeger.Deger).ToString() : "0";
                }
            }
            else if (rule.ComparisonType == "equal")
            {
                if (rule.DecimalPlaces.HasValue)
                {
                    list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(rule.Equal));
                }
                else
                {
                    list[index] = rule.Equal;
                }
            }
        }

        // Tekil özellik kuralları
        async Task ProcessSinglePropertyRule(PropertyRule rule, dynamic targetObj, EtiketLog_TempYeni veri, dynamic kuralJson)
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
                case "gunlukFark":
                    //var oncekiGunSonuEtiketDeger = _cizimRepository.GetEtiketLogGunSonuVeri(kuralJson.EtiketId);
                    //var oncekiGunSonuEtiketDeger = gunSonuVeriler.Where(x => x.EtiketId == kuralJson.EtiketId).FirstOrDefault();
                    var oncekiGunSonuEtiketDeger = await _gunlukCacheService.GetGunSonuVerilerForEtiketId(kuralJson.EtiketId);
                    if (rule.DecimalPlaces.HasValue)
                    {
                        targetObj.SetPropertyValue(rule.PropertyName, string.Format("{0:F" + rule.DecimalPlaces.Value + "}", oncekiGunSonuEtiketDeger != null ? (veri.Deger - oncekiGunSonuEtiketDeger.Deger) : 0));
                    }
                    else
                    {
                        targetObj.SetPropertyValue(rule.PropertyName, oncekiGunSonuEtiketDeger != null ? (veri.Deger - oncekiGunSonuEtiketDeger.Deger).ToString() : "0");
                    }
                    break;
                case "equal":
                    if (rule.DecimalPlaces.HasValue)
                    {
                        targetObj.SetPropertyValue(rule.PropertyName, string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(rule.Equal)));
                    }
                    else
                    {
                        targetObj.SetPropertyValue(rule.PropertyName, rule.Equal);
                    }
                    break;
            }
        }
        async Task ApplyNumericEqualRule(PropertyRule rule, dynamic targetObj, dynamic veri, dynamic kuralJson)
        {
            if (veri.Deger == kuralJson.MinDeger)
            {
                if (rule.DecimalPlaces.HasValue)
                {
                    targetObj.SetPropertyValue(rule.PropertyName, string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(rule.BelowMin)));
                }
                else
                {
                    targetObj.SetPropertyValue(rule.PropertyName, rule.BelowMin);
                }
            }
        }

        async Task ApplyNumericRule(PropertyRule rule, dynamic targetObj, dynamic veri, dynamic kuralJson)
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
        async Task ApplyTextRule(PropertyRule rule, dynamic targetObj, dynamic veri)
        {
            var value = rule.DecimalPlaces.HasValue
                ? string.Format("{0:F" + rule.DecimalPlaces.Value + "}", veri.Deger)
                : veri.Deger.ToString();
            if (rule.DecimalPlaces.HasValue)
            {
                targetObj.SetPropertyValue(rule.PropertyName, string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(value)));
            }
            else
            {
                targetObj.SetPropertyValue(rule.PropertyName, value);
            }
        }

        public async Task<List<SekilJson>> GetCizimJsonById(int cizimId)
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
            var veriler = await _cizimRepository.GetEtiketLogVeriler(); //TODO
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
                        ProcessChildRules(rules.Children, jsonObj, veri, kuralJson);
                    }

                    // Ana kuralları işleme
                    if (rules.Rules != null)
                    {
                        ProcessRules(rules.Rules, jsonObj, veri, kuralJson);
                    }
                }
                listElements.Add(jsonObj);
            }
            return listElements;
        }

        public async Task<List<SekilJson>> GetCizimJsonByLokasyonIdAndUserId(int lokasyonId, string userId)
        {
            try
            {
                var ruleProcessor = new RuleProcessor();
                var listElements = new List<SekilJson>();

                var cizimId = await _cizimRepository.GetCizimIdByLokasyonId(lokasyonId);
                //cizim etiket eslemeden etiketleri bul
                var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
                var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
                //şekillere ait etiket kuralları bul ve etiketlerini al
                var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
                //etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
                var veriler = await _cizimRepository.GetEtiketLogVerilerByEtiketIds(etiketKurallar.Select(x => x.EtiketId).Distinct().ToList());

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

                        //And/Or logic bakilir
                        if (kuralJson.EtiketJson != null)
                        {
                            var ruleSet = JsonConvert.DeserializeObject<RuleSet>(kuralJson.EtiketJson);

                            bool result = ruleProcessor.Evaluate(ruleSet, veriler);
                            if (!result)
                            {
                                continue;
                            }
                        }

                        var rules = JsonConvert.DeserializeObject<Rule1>(kuralJson.EtiketKuralJson);
                        if (rules == null) continue;

                        // Çocuk kuralları işleme
                        if (rules.Children != null)
                        {
                            await ProcessChildRules(rules.Children, jsonObj, veri, kuralJson);
                        }

                        // Ana kuralları işleme
                        if (rules.Rules != null)
                        {
                            await ProcessRules(rules.Rules, jsonObj, veri, kuralJson);
                        }
                    }

                    listElements.Add(jsonObj);
                }
                return listElements;
            }
            catch (Exception ex)
            {
                return new List<SekilJson>();
            }
        }

        public async Task<List<SekilJson>> GetCizimJsonByLokasyonId(int lokasyonId)
        {
            try
            {
                var ruleProcessor = new RuleProcessor();

                var cizimId = await _cizimRepository.GetCizimIdByLokasyonId(lokasyonId);
                //cizim etiket eslemeden etiketleri bul
                var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
                var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());

                //şekillere ait etiket kuralları bul ve etiketlerini al
                var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());

                //etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
                //var veriler = await _cizimRepository.GetEtiketLogVerilerByCizimId(cizimId);
                var veriler = await _cizimRepository.GetEtiketLogVerilerByEtiketIds(etiketKurallar.Select(x => x.EtiketId).Distinct().ToList());
                var listElements = new List<SekilJson>();

                //var cizimId = await _cizimRepository.GetCizimIdByLokasyonId(lokasyonId);
                ////cizim etiket eslemeden etiketleri bul
                //var cizimSekilEslemeler = await _sekilKuralCacheService.GetCizimSekilEslemelerAsync(cizimId);
                //var sekiller = await _sekilKuralCacheService.GetSekillerAsync(cizimSekilEslemeler.Select(x => x.SekilId).ToList());

                ////etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
                //var veriler = await _cizimRepository.GetEtiketLogVerilerByCizimId(cizimId);
                //var listElements = new List<SekilJson>();
                //var etiketKurallar = await _sekilKuralCacheService.GetEtiketKurallarAsync(cizimSekilEslemeler.Select(x => x.SekilId).ToList());

                //var asd = etiketKurallar.Where(x => x.EtiketKuralJson.Contains("gunlukFark")).Select(x => x.EtiketId).ToList();
                //var gunlukFarklar = await _cizimRepository.GetEtiketLogGunSonuVeriler(asd);

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

                        //And/Or logic bakilir
                        if (kuralJson.EtiketJson != null)
                        {
                            var ruleSet = JsonConvert.DeserializeObject<RuleSet>(kuralJson.EtiketJson);

                            bool result = ruleProcessor.Evaluate(ruleSet, veriler);
                            if (!result)
                            {
                                continue;
                            }
                        }

                        var rules = JsonConvert.DeserializeObject<Rule1>(kuralJson.EtiketKuralJson);
                        if (rules == null) continue;

                        // Çocuk kuralları işleme
                        if (rules.Children != null)
                        {
                            await ProcessChildRules(rules.Children, jsonObj, veri, kuralJson);
                        }

                        // Ana kuralları işleme
                        if (rules.Rules != null)
                        {
                            await ProcessRules(rules.Rules, jsonObj, veri, kuralJson);
                        }
                    }

                    listElements.Add(jsonObj);
                }
                return listElements;
            }
            catch (Exception ex)
            {
                return new List<SekilJson>();
            }
        }

        async Task UpdateListItem(IList<string> list, int index, PropertyRule rule, EtiketLog_TempYeni veri, SekilEtiketKural kuralJson)
        {
            if (rule.ComparisonType == "numeric")
            {
                if (veri.Deger < kuralJson.MinDeger)
                {
                    if (rule.DecimalPlaces.HasValue)
                    {
                        list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(rule.BelowMin));
                    }
                    else
                    {
                        list[index] = rule.BelowMin;
                    }
                }
                else if (veri.Deger > kuralJson.MaxDeger)
                {
                    if (rule.DecimalPlaces.HasValue)
                    {
                        list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(rule.AboveMax));
                    }
                    else
                    {
                        list[index] = rule.AboveMax;
                    }
                }
                else
                {
                    if (rule.DecimalPlaces.HasValue)
                    {
                        list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(rule.WithinRange));
                    }
                    else
                    {
                        list[index] = rule.WithinRange;
                    }
                }
            }
            else if (rule.ComparisonType == "numericEqual")
            {
                if (veri.Deger == kuralJson.MinDeger)
                {
                    if (rule.DecimalPlaces.HasValue)
                    {
                        list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(rule.BelowMin));
                    }
                    else
                    {
                        list[index] = rule.BelowMin;
                    }
                }
            }
            else if (rule.ComparisonType == "text")
            {
                if (rule.DecimalPlaces.HasValue)
                {
                    list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", veri.Deger);
                }
                else
                {
                    list[index] = veri.Deger.ToString();
                }
            }
            else if (rule.ComparisonType == "timespan")
            {
                var hours = Convert.ToInt32(Math.Floor(veri.Deger.GetValueOrDefault(0) / 3600));
                var minutes = Convert.ToInt32(Math.Floor((veri.Deger.GetValueOrDefault(0) % 3600) / 60));
                var secs = Convert.ToInt32(Math.Floor(veri.Deger.GetValueOrDefault(0) % 60));
                var text = (hours < 10 ? '0' + hours.ToString() : hours.ToString()) + ":" + (minutes < 10 ? '0' + minutes.ToString() : minutes.ToString()) + ":" + (secs < 10 ? '0' + secs.ToString() : secs.ToString());
                list[index] = text;
            }
            else if (rule.ComparisonType == "gunlukFark")
            {
                //var oncekiGunSonuEtiketDeger = _cizimRepository.GetEtiketLogGunSonuVeri(kuralJson.EtiketId);
                //var oncekiGunSonuEtiketDeger = gunSonuVeriler.Where(x => x.EtiketId == kuralJson.EtiketId).FirstOrDefault();
                var oncekiGunSonuEtiketDeger = await _gunlukCacheService.GetGunSonuVerilerForEtiketId(kuralJson.EtiketId);
                if (rule.DecimalPlaces.HasValue)
                {
                    list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", oncekiGunSonuEtiketDeger != null ? (veri.Deger - oncekiGunSonuEtiketDeger.Deger) : 0);
                }
                else
                {
                    list[index] = oncekiGunSonuEtiketDeger != null ? (veri.Deger - oncekiGunSonuEtiketDeger.Deger).ToString() : "0";
                }
            }
            else if (rule.ComparisonType == "equal")
            {
                if (rule.DecimalPlaces.HasValue)
                {
                    list[index] = string.Format("{0:F" + rule.DecimalPlaces.Value + "}", Convert.ToDecimal(rule.Equal));
                }
                else
                {
                    list[index] = rule.Equal;
                }
            }
        }

        public async Task<ScadaData> GetScadaDataByLokasyonIdAndUserId(int lokasyonId, string userId)
        {
            try
            {
                //return new ScadaData();
                var scadaData = new ScadaData();
                var cizimId = await _cizimRepository.GetCizimIdByLokasyonId(lokasyonId);
                //cizim etiket eslemeden etiketleri bul
                var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
                var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
                //şekillere ait etiket kuralları bul ve etiketlerini al
                var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
                //etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
                var veriler = await _cizimRepository.GetEtiketLogVerilerByEtiketIds(etiketKurallar.Select(x => x.EtiketId).Distinct().ToList());

                //scadaData.Sekiller = sekiller;
                scadaData.EtiketDegerleri = veriler;
                //scadaData.Kurallar = etiketKurallar;

                return scadaData;
            }
            catch (Exception ex)
            {
                return new ScadaData();
            }
        }

        public async Task<List<KullaniciSekilYetki>> GetKullaniciSekilYetki(int lokasyonId, string userId)
        {
            //var cizimId = await _cizimRepository.GetCizimIdByLokasyonId(lokasyonId);
            ////cizim etiket eslemeden etiketleri bul
            //var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
            //var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
            return await _cizimRepository.GetKullaniciSekilYetki(lokasyonId, userId);
        }

        public async Task<List<SekilEtiketKural>> GetSekilEtiketKural(int lokasyonId, string userId)
        {
            var cizimId = await _cizimRepository.GetCizimIdByLokasyonId(lokasyonId);
            //cizim etiket eslemeden etiketleri bul
            var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
            var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
            //şekillere ait etiket kuralları bul ve etiketlerini al
            var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
            return etiketKurallar;
        }

        public async Task<List<SekilEtiketKural>> GetSekilEtiketKuralByCizimId(int cizimId, string userId)
        {
            //cizim etiket eslemeden etiketleri bul
            var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
            var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
            //şekillere ait etiket kuralları bul ve etiketlerini al
            var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
            return etiketKurallar;
        }

        public async Task<List<Sekil>> GetSekilByCizimId(int cizimId)
        {
            //cizim etiket eslemeden etiketleri bul
            var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
            var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
            foreach (var sekil in sekiller)
            {
                var jsonObj = JsonConvert.DeserializeObject<SekilJson>(sekil.SekilJson);
                var sekilJson = JsonConvert.SerializeObject(jsonObj);
                sekil.SekilJson = sekilJson;
            }
            return sekiller;
        }

        public async Task<List<Sekil>> GetSekil(int lokasyonId)
        {
            var cizimId = await _cizimRepository.GetCizimIdByLokasyonId(lokasyonId);
            //cizim etiket eslemeden etiketleri bul
            var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
            var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
            foreach (var sekil in sekiller)
            {
                var jsonObj = JsonConvert.DeserializeObject<SekilJson>(sekil.SekilJson);
                var sekilJson = JsonConvert.SerializeObject(jsonObj);
                sekil.SekilJson = sekilJson;
            }
            return sekiller;
        }

        public async Task<List<EtiketLog_TempYeni>> GetEtiketLog(int lokasyonId)
        {
            var cizimId = await _cizimRepository.GetCizimIdByLokasyonId(lokasyonId);
            //cizim etiket eslemeden etiketleri bul
            var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
            var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
            //şekillere ait etiket kuralları bul ve etiketlerini al
            var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
            //etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
            var veriler = await _cizimRepository.GetEtiketLogVerilerByEtiketIds(etiketKurallar.Select(x => x.EtiketId).Distinct().ToList());
            return veriler;
        }

        public async Task<ScadaData> GetScadaData(int lokasyonId)
        {
            try
            {
                var scadaData = new ScadaData();
                var cizimId = await _cizimRepository.GetCizimIdByLokasyonId(lokasyonId);
                //cizim etiket eslemeden etiketleri bul
                var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
                var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
                //şekillere ait etiket kuralları bul ve etiketlerini al
                var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
                //etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
                var veriler = await _cizimRepository.GetEtiketLogVerilerByEtiketIds(etiketKurallar.Select(x => x.EtiketId).Distinct().ToList());

                //scadaData.Sekiller = sekiller;
                //scadaData.Kurallar = etiketKurallar;
                scadaData.EtiketDegerleri = veriler;
                var etiketIds = etiketKurallar
                    .Where(x => x.EtiketKuralJson.Contains("gunlukFark"))
                    .Select(x => x.EtiketId)
                    .Distinct()
                    .ToList();
                scadaData.DMGunlukEtiketDegerleri =
                    await _gunlukCacheService.GetGunSonuVerilerForEtiketIds(etiketIds);

                return scadaData;
            }
            catch (Exception ex)
            {
                return new ScadaData();
            }
        }

        public async Task<ScadaData> GetScadaDataByCizimId(int cizimId)
        {
            try
            {
                var scadaData = new ScadaData();
                //var cizimId = await _cizimRepository.GetCizimIdByLokasyonId(lokasyonId);
                //cizim etiket eslemeden etiketleri bul
                var cizimSekilEslemeler = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
                var sekiller = await _cizimRepository.GetCizimEtiketler(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
                //şekillere ait etiket kuralları bul ve etiketlerini al
                var etiketKurallar = await _cizimRepository.GetEtiketKural(cizimSekilEslemeler.Select(x => x.SekilId).ToList());
                //etiketler icinde donup, etiket degerlerine ait verileri cek, json degisiklikleri yap
                var veriler = await _cizimRepository.GetEtiketLogVerilerByEtiketIds(etiketKurallar.Select(x => x.EtiketId).Distinct().ToList());

                //scadaData.Sekiller = sekiller;
                //scadaData.Kurallar = etiketKurallar;
                scadaData.EtiketDegerleri = veriler;
                var etiketIds = etiketKurallar
                    .Where(x => x.EtiketKuralJson.Contains("gunlukFark"))
                    .Select(x => x.EtiketId)
                    .Distinct()
                    .ToList();
                scadaData.DMGunlukEtiketDegerleri =
                    await _gunlukCacheService.GetGunSonuVerilerForEtiketIds(etiketIds);

                return scadaData;
            }
            catch (Exception ex)
            {
                return new ScadaData();
            }
        }
        public async Task UpdateDMModTuru(int cizimId, string modTuru, string userId)
        {
            try
            {
                await _cizimRepository.UpdateDMModTuru(cizimId, modTuru, userId);
            }
            catch (Exception)
            {

            }
        }

        public async Task UpdateTankModTuru(int cizimId, string modTuru, string userId)
        {
            try
            {
                await _cizimRepository.UpdateTankModTuru(cizimId, modTuru, userId);
            }
            catch (Exception)
            {

            }
        }

        public async Task UpdateSekilModTuru2(string sekilId, string targetSekilId, string modTuru, string userId)
        {
            try
            {
                await _cizimRepository.UpdateSekilModTuru2(sekilId, targetSekilId, modTuru, userId);
            }
            catch (Exception)
            {

            }
        }

        public async Task UpdateSekilPozisyon(string sekilId, int pozisyonX, int pozisyonY, string userId)
        {
            try
            {
                await _cizimRepository.UpdateSekilPozisyon(sekilId, pozisyonX, pozisyonY, userId);
            }
            catch (Exception)
            {

            }
        }

        public async Task UpdateSekilVisible(string sekilId, bool gorunsunMu, string userId)
        {
            try
            {
                await _cizimRepository.UpdateSekilVisible(sekilId, gorunsunMu, userId);
            }
            catch (Exception)
            {

            }
        }

        public async Task<Cizim?> GetCizimById(int cizimId)
        {
            var cizim = await _cizimRepository.GetByIdAsync(cizimId);
            if (cizim != null)
            {
                return cizim;
            }
            else
            {
                return null;
            }
        }

        public async Task<string> GetCizimUrlById(int cizimId)
        {
            var cizim = await _cizimRepository.GetByIdAsync(cizimId);
            if (cizim != null)
            {
                return cizim.CizimUrl == null ? "" : cizim.CizimUrl;
            }
            else
            {
                return "";
            }
        }
    }
}
