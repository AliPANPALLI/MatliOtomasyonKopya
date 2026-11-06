using EnerjiIzleme.Application.Interfaces;
using EnerjiIzleme.Application.Services.Core;
using EnerjiIzleme.Application.ViewModels;
using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Interfaces;
using EnerjiIzleme.Domain.Models;
using EnerjiIzleme.Domain.Models.ReportModels;
using MapsterMapper;
using MongoDB.Bson;
using MongoDB.Driver;
using SharedApp.Domain.Models;
using SharedKernel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using static SharedKernel.ApplicationConstants;
using Bolum = EnerjiIzleme.Domain.Models.Bolum;

namespace EnerjiIzleme.Application.Services
{
    public class TreeAppService : ITreeAppService
    {
        private readonly ITreeRepository _treeRepository;
        //private readonly IAnalizorDegerlerTempRepository _analizorDegerlerTempRepository;
        private readonly ILogsRepository _logsRepository;
        private readonly IBolumRepository _bolumRepository;
        private readonly ICihazRepository _cihazRepository;
        private readonly IMakineRepository _makineRepository;
        private readonly IHayvanSayisiCacheMemory _hayvanSayisiCacheMemory;
        private readonly IMapper _mapper;

        public TreeAppService(ITreeRepository treeRepository,
            //IAnalizorDegerlerTempRepository analizorDegerlerTempRepository,
            IBolumRepository bolumRepository,
            ICihazRepository cihazRepository,
            IMakineRepository makineRepository,
            ILogsRepository logsRepository,
           IHayvanSayisiCacheMemory hayvanSayisiCacheMemory, IMapper mapper)
        {
            _treeRepository = treeRepository;
            //_analizorDegerlerTempRepository = analizorDegerlerTempRepository;
            _bolumRepository = bolumRepository;
            _cihazRepository = cihazRepository;
            _makineRepository = makineRepository;
            _logsRepository = logsRepository;
            _hayvanSayisiCacheMemory = hayvanSayisiCacheMemory;
            _mapper = mapper;
        }
        public async Task<TreeModel> GetNodeTreeViewByCihazId(int cihazId, CancellationToken cancellationToken)
        {
            // Cihazı al
            var cihaz = await _cihazRepository.GetByIdAsync(cihazId);
            if (cihaz == null)
                return null;

            // Makineyi al
            var makine = await _makineRepository.GetByIdAsync(cihaz.MakineId);
            if (makine == null)
                return null;

            // Bölümü al
            var bolum = await _bolumRepository.GetByIdAsync(makine.BolumId);
            if (bolum == null)
                return null;

            var parentCihaz = await _cihazRepository.GetAsyncByPredicate(x => x.Id == cihaz.ParentCihazId);
            var childCihazlar = await _cihazRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ParentCihazId == cihaz.Id);
            // Tree yapısını oluştur (Cihaz → Makine → Bölüm şeklinde)
            var cihazNode = new TreeModel
            {
                Id = cihaz.Id,
                Name = cihaz.Name ?? "Cihaz",
                Type = TreeViewNodeTypes.Cihaz,
                Checked = false,
                Expanded = true,
                Write = false,
                Read = false,
                Children = null,
                ParentCihaz = parentCihaz == null ? null : parentCihaz.Id,
                ChildrenCihazlar = childCihazlar.Count > 0 ? childCihazlar.Select(x => x.Id).ToList() : null,
            };

            var makineNode = new TreeModel
            {
                Id = makine.Id,
                Name = makine.Name ?? "Makine",
                Type = TreeViewNodeTypes.Makine,
                Checked = false,
                Expanded = true,
                Write = false,
                Read = false,
                Children = new List<TreeModel> { cihazNode }
            };

            var bolumNode = new TreeModel
            {
                Id = bolum.Id,
                Name = bolum.Name ?? "Bölüm",
                Type = TreeViewNodeTypes.Bolum,
                Checked = false,
                Expanded = true,
                Write = false,
                Read = false,
                Children = new List<TreeModel> { makineNode }
            };

            return bolumNode;
        }

        public async Task<TreeModel> GetNodeTreeViewByCihazId2(int cihazId, CancellationToken cancellationToken)
        {
            // Cihazı al
            var cihaz = await _cihazRepository.GetByIdAsync(cihazId);
            if (cihaz == null)
                return null;

            // Makineyi al
            var makine = await _makineRepository.GetByIdAsync(cihaz.MakineId);
            if (makine == null)
                return null;

            // Bölümü al
            var bolum = await _bolumRepository.GetByIdAsync(makine.BolumId);
            if (bolum == null)
                return null;

            // Tree yapısını oluştur (Cihaz → Makine → Bölüm şeklinde)
            var cihazNode = new TreeModel
            {
                Id = cihaz.Id,
                Name = cihaz.Name ?? "Cihaz",
                Type = TreeViewNodeTypes.Cihaz,
                Checked = false,
                Expanded = true,
                Write = false,
                Read = false,
                Children = null
            };

            var makineNode = new TreeModel
            {
                Id = makine.Id,
                Name = makine.Name ?? "Makine",
                Type = TreeViewNodeTypes.Makine,
                Checked = false,
                Expanded = true,
                Write = false,
                Read = false,
                Children = new List<TreeModel> { cihazNode }
            };

            var bolumNode = new TreeModel
            {
                Id = bolum.Id,
                Name = bolum.Name ?? "Bölüm",
                Type = TreeViewNodeTypes.Bolum,
                Checked = false,
                Expanded = true,
                Write = false,
                Read = false,
                Children = new List<TreeModel> { makineNode }
            };

            return bolumNode;
        }

        public async Task<List<TreeModel>> GetTreeByLokasyonId(int lokasyonId, bool duzenlemeModu, CancellationToken cancellationToken)
        {
            //var lokasyon = await GetLokasyonByIdWithApi(lokasyonId, cancellationToken);
            var lokasyon = await _treeRepository.GetLokasyonById(lokasyonId);
            if (lokasyon == null)
            {
                throw new Exception("Lokasyon bulunamadı!");
            }
            //return await GetTreeByLokasyonId(lokasyon.Id, lokasyon.Name, duzenlemeModu);
            return await BuildTreeBatch(lokasyon.Id, lokasyon.Name, duzenlemeModu);
        }

        public async Task<List<TreeModel>> BuildTreeBatch(int lokasyonId, string? lokasyonName, bool duzenlemeModu)
        {
            // Tüm veriyi TEK SEFERDE paralel olarak çek
            var bolumTask = duzenlemeModu
                ? _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true)
                : _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true && x.Aktif != false);

            var bolumler = await bolumTask;
            var bolumIds = bolumler.Select(b => b.Id).ToHashSet();

            if (!bolumIds.Any())
            {
                return new List<TreeModel>
                {
                    new() {
                        Id = lokasyonId,
                        Name = lokasyonName,
                        Type = TreeViewNodeTypes.Lokasyon,
                        Approved = true,
                        Children = new List<TreeModel>()
                    }
                };
            }

            // Makine ve cihazları paralel çek
            var makineTask = duzenlemeModu
                ? _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(m => bolumIds.Contains(m.BolumId) && m.Silindi != true)
                : _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(m => bolumIds.Contains(m.BolumId) && m.Silindi != true && m.Aktif != false);

            await Task.WhenAll(makineTask); // paralel genişletebilirsin
            var makineler = await makineTask;
            var makineIds = makineler.Select(m => m.Id).ToHashSet();

            var cihazTask = makineIds.Any()
                ? (duzenlemeModu
                    ? _cihazRepository.GetAllAsyncByPredicateWithAsNoTracking(c => makineIds.Contains(c.MakineId) && c.Silindi != true)
                    : _cihazRepository.GetAllAsyncByPredicateWithAsNoTracking(c => makineIds.Contains(c.MakineId) && c.Silindi != true && c.Aktif != false))
                : Task.FromResult(new List<Cihaz>());

            var cihazlar = await cihazTask;

            // Bellekte hızlı lookup için Dictionary'e al
            var makineByBolum = makineler
                .GroupBy(m => m.BolumId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var cihazByMakine = cihazlar
                .GroupBy(c => c.MakineId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Parent/child cihaz ilişkilerini de bellekte çöz
            var cihazIds = cihazlar.Select(c => c.Id).ToHashSet();
            var childCihazByParent = cihazlar.Where(c => c.ParentCihazId > 0).GroupBy(c => c.ParentCihazId).ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            // Ağacı bellekte kur — sıfır ek DB sorgusu
            var lokasyonNode = new TreeModel
            {
                Id = lokasyonId,
                Name = lokasyonName,
                Type = TreeViewNodeTypes.Lokasyon,
                Expanded = false,
                Checked = false,
                Write = false,
                Read = false,
                Approved = true,
                Children = bolumler.Select(bolum =>
                {
                    var bolumNode = new TreeModel
                    {
                        Id = bolum.Id,
                        Name = bolum.Name ?? "Bölüm",
                        Type = TreeViewNodeTypes.Bolum,
                        Checked = false,
                        Approved = bolum.Onaylandi.GetValueOrDefault(false),
                        Expanded = false,
                        Write = false,
                        Read = false,
                        Children = makineByBolum.GetValueOrDefault(bolum.Id, new())
                            .Select(makine =>
                            {
                                var makineNode = new TreeModel
                                {
                                    Id = makine.Id,
                                    Name = makine.Name,
                                    Type = TreeViewNodeTypes.Makine,
                                    Checked = false,
                                    Approved = makine.Onaylandi == true,
                                    Expanded = false,
                                    Write = false,
                                    Read = false,
                                    Children = cihazByMakine.GetValueOrDefault(makine.Id, new())
                                        .Select(cihaz => new TreeModel
                                        {
                                            Id = cihaz.Id,
                                            Name = cihaz.Name ?? "Cihaz",
                                            Type = TreeViewNodeTypes.Cihaz,
                                            Checked = false,
                                            Approved = cihaz.Onaylandi == true,
                                            Expanded = false,
                                            Write = false,
                                            Read = false,
                                            ParentCihaz = cihaz.ParentCihazId,
                                            ChildrenCihazlar = childCihazByParent
                                                .GetValueOrDefault(cihaz.Id)
                                        })
                                        .ToList()
                                };
                                return makineNode;
                            })
                            .ToList()
                    };
                    return bolumNode;
                })
                .ToList()
            };

            return new List<TreeModel> { lokasyonNode };
        }

        public async Task<List<TreeModel>> GetTreeByLokasyonId(int lokasyonId, string? lokasyonName, bool? duzenlemeModu = false)
        {
            List<TreeModel> tree = new List<TreeModel>();

            tree.Add(new TreeModel
            {
                Id = lokasyonId,
                Name = lokasyonName,
                Expanded = false,
                Checked = false,
                Type = TreeViewNodeTypes.Lokasyon,
                Children = null,
                Write = false,
                Read = false,
                Approved = true,
            });
            List<Bolum> bolumler = new();
            if (duzenlemeModu.GetValueOrDefault(false))
            {
                bolumler = (await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true));
            }
            else
            {
                bolumler = (await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true && x.Aktif != false));
            }
            foreach (var bolum in bolumler)
            {
                if (tree.Last().Children == null)
                {
                    tree.Last().Children = new List<TreeModel>();
                }
                tree.Last().Children.Add(await GetAllNodesRecursive(bolum.Id, bolum.Name, bolum.Onaylandi.GetValueOrDefault(false), duzenlemeModu));
            }
            return tree;
        }

        public async Task<TreeModel> GetAllNodesRecursive(int bolumId, string bolumName, bool bolumOnaylandi, bool? duzenlemeModu = false)
        {
            var rootNode = new TreeModel
            {
                Id = bolumId,
                Name = bolumName ?? "Bölüm",
                Type = TreeViewNodeTypes.Bolum,
                Checked = false,
                Approved = bolumOnaylandi,
                Expanded = false,
                Write = false,
                Read = false,
                Children = new List<TreeModel>()
            };

            // Bölüm altındaki makineleri al
            List<Makine> makineler = new();
            if (duzenlemeModu.GetValueOrDefault(false))
            {
                makineler = await _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(m => m.BolumId == bolumId && m.Silindi != true);
            }
            else
            {
                makineler = await _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(m => m.BolumId == bolumId && m.Silindi != true && m.Aktif != false);
            }

            foreach (var makine in makineler)
            {
                var makineNode = new TreeModel
                {
                    Id = makine.Id,
                    Name = makine.Name,
                    Type = TreeViewNodeTypes.Makine,
                    Checked = false,
                    Approved = makine.Onaylandi == true,
                    Expanded = false,
                    Write = false,
                    Read = false,
                    Children = new List<TreeModel>()
                };

                // Makine altındaki cihazları al
                List<Cihaz> cihazlar = new();
                if (duzenlemeModu.GetValueOrDefault(false))
                {
                    cihazlar = await _cihazRepository.GetAllAsyncByPredicateWithAsNoTracking(c => c.MakineId == makine.Id && c.Silindi != true);
                }
                else
                {
                    cihazlar = await _cihazRepository.GetAllAsyncByPredicateWithAsNoTracking(c => c.MakineId == makine.Id && c.Silindi != true && c.Aktif != false);
                }
                foreach (var cihaz in cihazlar)
                {
                    var parentCihaz = await _cihazRepository.GetAsyncByPredicate(x => x.Id == cihaz.ParentCihazId);
                    var childCihazlar = new List<Cihaz>();
                    if (duzenlemeModu.GetValueOrDefault(false))
                    {
                        childCihazlar = await _cihazRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ParentCihazId == cihaz.Id);
                    }
                    else
                    {
                        childCihazlar = await _cihazRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ParentCihazId == cihaz.Id && x.Aktif != false);
                    }
                    var cihazNode = new TreeModel
                    {
                        Id = cihaz.Id,
                        Name = cihaz.Name ?? "Cihaz",
                        Type = TreeViewNodeTypes.Cihaz,
                        Checked = false,
                        Approved = cihaz.Onaylandi == true,
                        Expanded = false,
                        Write = false,
                        Read = false,
                        ParentCihaz = parentCihaz == null ? null : parentCihaz.Id,
                        ChildrenCihazlar = childCihazlar.Count > 0 ? childCihazlar.Select(x => x.Id).ToList() : null,
                    };
                    makineNode.Children.Add(cihazNode);  // Cihazı makinenin altına ekle
                }
                //bool tumCihazlarOnaylandi = makineNode.Children.Count == 0 || makineNode.Children.All(c => c.Approved == true);
                //makineNode.Approved = makine.Onaylandi == true && tumCihazlarOnaylandi;

                rootNode.Children.Add(makineNode);  // Makineyi bölüme ekle
            }
            //bool tumMakinelerOnaylandi = rootNode.Children.Count == 0 || rootNode.Children.All(m => m.Approved == true);
            //rootNode.Approved = bolumOnaylandi == true && tumMakinelerOnaylandi;
            return rootNode;
        }

        public async Task<Lokasyon?> GetLokasyonByIdWithApi(int lokasyonId, CancellationToken cancellationToken)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var requestUri = new Uri($"{ApiUrls.SharedAppBaseUrl}/Lokasyon/GetById/{lokasyonId}");

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = requestUri
                    };

                    var response = await httpClient.SendAsync(request, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync(cancellationToken);
                        var lokasyon = JsonSerializer.Deserialize<Lokasyon?>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return lokasyon;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<SahaVerileriTabloModel> GetSahaVerileriTabloByNodeIdAndType(int nodeId, string nodeType, CancellationToken cancellationToken)
        {
            try
            {
                SahaVerileriTabloModel sahaVerileriTabloModel = new SahaVerileriTabloModel();
                List<SahaVerileriTabloRaporModel> sahaVerileriTabloRaporModels = new List<SahaVerileriTabloRaporModel>();
                var cihazlar = await _cihazRepository.GetAllCihazByNodeIdAndType(nodeId, nodeType);
                if (cihazlar.Count() == 0)
                    return sahaVerileriTabloModel;
                var analizorDegerlerSonByCihazIds = await _logsRepository.GetToplamVeriByCihazIdsTest(cihazlar.Select(x => x.Id).ToList(), cancellationToken);
                if (analizorDegerlerSonByCihazIds.ToplamVeri == null) return new SahaVerileriTabloModel();
                else
                {
                    sahaVerileriTabloRaporModels.AddRange(AnalizorDegerlerToSahaVerileriTabloRaporByNodeType(analizorDegerlerSonByCihazIds.ToplamVeri.payload.veri, analizorDegerlerSonByCihazIds.ToplamVeri.payload.okunma_zamani, (analizorDegerlerSonByCihazIds.ToplamVeri.payload.state), nodeType, cancellationToken));
                    sahaVerileriTabloModel.SahaVerileriTabloRaporModels = sahaVerileriTabloRaporModels;
                    var uyariCihazlar = string.Join(" •", (await _cihazRepository.GetAllvwCihazByCihazIds(analizorDegerlerSonByCihazIds.State2Cihazlar)).Select(x => x.MakineName + " / " + x.Name));
                    if (analizorDegerlerSonByCihazIds.State2Cihazlar.Count() > 0)
                    {
                        sahaVerileriTabloModel.UyariVerenAnalizorler = "Bazı analizörlerden veri okunamıyor => •" + uyariCihazlar;
                    }
                }
                return sahaVerileriTabloModel;
            }
            catch (Exception ex)
            {
                return new SahaVerileriTabloModel();
            }
        }

        public async Task<SahaVerileriTabloModel> GetSahaVerileriTabloByCihazList(List<int> cihazNodeIds, CancellationToken cancellationToken)
        {
            try
            {
                SahaVerileriTabloModel sahaVerileriTabloModel = new SahaVerileriTabloModel();
                List<SahaVerileriTabloRaporModel> sahaVerileriTabloRaporModels = new List<SahaVerileriTabloRaporModel>();
                var analizorDegerlerSonByCihazIds = await _logsRepository.GetToplamVeriByCihazIdsTest(cihazNodeIds, cancellationToken);
                if (analizorDegerlerSonByCihazIds.ToplamVeri == null) return null;
                else
                {
                    string nodeType = TreeViewNodeTypes.Makine;
                    if (cihazNodeIds.Count == 1)
                    {
                        nodeType = TreeViewNodeTypes.Cihaz;
                    }
                    sahaVerileriTabloRaporModels.AddRange(AnalizorDegerlerToSahaVerileriTabloRaporByNodeType(analizorDegerlerSonByCihazIds.ToplamVeri.payload.veri, analizorDegerlerSonByCihazIds.ToplamVeri.payload.okunma_zamani, (analizorDegerlerSonByCihazIds.ToplamVeri.payload.state), nodeType, cancellationToken));
                    sahaVerileriTabloModel.SahaVerileriTabloRaporModels = sahaVerileriTabloRaporModels;
                    var uyariCihazlar = string.Join(" •", (await _cihazRepository.GetAllvwCihazByCihazIds(analizorDegerlerSonByCihazIds.State2Cihazlar)).Select(x => x.MakineName + " / " + x.Name));
                    if (analizorDegerlerSonByCihazIds.State2Cihazlar.Count() > 0)
                    {
                        sahaVerileriTabloModel.UyariVerenAnalizorler = "Bazı analizörlerden veri okunamıyor => •" + uyariCihazlar;
                    }
                }
                return sahaVerileriTabloModel;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<SahaVerileriTabloRaporModel>> GetSahaVerileriTabloByCihazId(int cihazId, CancellationToken cancellationToken)
        {
            List<SahaVerileriTabloRaporModel> sahaVerileriTabloRaporModels = new List<SahaVerileriTabloRaporModel>();
            var analizorDegerSon = await _logsRepository.GetTuketimSonVeriByCihazId(cihazId, cancellationToken);
            if (analizorDegerSon != null)
            {
                sahaVerileriTabloRaporModels.AddRange(AnalizorDegerlerToSahaVerileriTabloRapor(analizorDegerSon.payload.veri, analizorDegerSon.payload.okunma_zamani, analizorDegerSon.payload.state, cancellationToken));
            }
            return sahaVerileriTabloRaporModels;
        }

        public async Task<List<TuketimEnerjiVerileriTabloRaporModel>> GetTuketimEnerjiVerileriTabloByCihazId(int cihazId, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            List<TuketimEnerjiVerileriTabloRaporModel> sahaVerileriTabloRaporModels = new List<TuketimEnerjiVerileriTabloRaporModel>();
            try
            {
                var sonuc = await _logsRepository.GetTuketimEnerjiVerileriByCihazId(cihazId, baslangicZamani, bitisZamani, cancellationToken);
                // Sonuçları dönüştür
                if (sonuc != null && sonuc.ElementCount > 0)
                {
                    var importDeger = sonuc.GetValue("import_fark").ToDecimal();
                    var exportDeger = sonuc.GetValue("export_fark").ToDecimal();
                    var dakikaFarki = sonuc.GetValue("dakika_farki").ToDecimal();
                    if (dakikaFarki == 0)
                        return sahaVerileriTabloRaporModels;
                    var importAktifEnerji = importDeger / 1000;
                    var importOrtalamaEnerji = importDeger / (dakikaFarki / 60) / 1000;
                    var exportAktifEnerji = exportDeger / 1000;
                    var exportOrtalamaEnerji = exportDeger / (dakikaFarki / 60) / 1000;
                    sahaVerileriTabloRaporModels.AddRange(AnalizorDegerlerToTuketimEnerjiVerileriTabloRapor(importAktifEnerji, importOrtalamaEnerji, exportAktifEnerji, exportOrtalamaEnerji, null, null, cancellationToken));
                }
            }
            catch (Exception ex)
            {

            }
            return sahaVerileriTabloRaporModels;
        }

        public async Task<List<TuketimEnerjiVerileriTabloRaporModel>> GetTuketimEnerjiVerileriTabloByNodeIdAndType(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            List<TuketimEnerjiVerileriTabloRaporModel> sahaVerileriTabloRaporModels = new List<TuketimEnerjiVerileriTabloRaporModel>();
            var cihazlar = await _cihazRepository.GetAllCihazByNodeIdAndType(nodeId, nodeType);
            try
            {
                var sonuclar = await _logsRepository.GetTuketimEnerjiVerileriByCokluCihazId(cihazlar.Select(x => x.Id).ToList(), baslangicZamani, bitisZamani, cancellationToken);
                decimal toplamImportEnerji = 0;
                decimal toplamExportEnerji = 0;
                decimal toplamImportOrtalama = 0;
                decimal toplamExportOrtalama = 0;
                DateTime? ilkOkumaZaman = null;
                DateTime? sonOkumaZaman = null;

                foreach (var sonuc in sonuclar)
                {
                    if (!sonuc.Contains("import_fark") || !sonuc.Contains("export_fark") || !sonuc.Contains("dakika_farki"))
                        continue;
                    var importDeger = sonuc.GetValue("import_fark").ToDecimal();
                    var exportDeger = sonuc.GetValue("export_fark").ToDecimal();
                    var dakikaFarki = sonuc.GetValue("dakika_farki").ToDecimal();
                    ilkOkumaZaman = sonuc.GetValue("ilk_zaman").ToUniversalTime().AddHours(-3);
                    sonOkumaZaman = sonuc.GetValue("son_zaman").ToUniversalTime().AddHours(-3);

                    if (dakikaFarki == 0)
                        continue;

                    // Hesaplamalar
                    var importAktifEnerji = importDeger;
                    var importOrtalamaEnerji = importDeger / (dakikaFarki / 60);

                    var exportAktifEnerji = exportDeger;
                    var exportOrtalamaEnerji = exportDeger / (dakikaFarki / 60);

                    // Toplamlara ekle
                    toplamImportEnerji += importAktifEnerji;
                    toplamExportEnerji += exportAktifEnerji;
                    toplamImportOrtalama += importOrtalamaEnerji;
                    toplamExportOrtalama += exportOrtalamaEnerji;
                }
                sahaVerileriTabloRaporModels.AddRange(AnalizorDegerlerToTuketimEnerjiVerileriTabloRapor(toplamImportEnerji / 1000, toplamImportOrtalama / 1000, toplamExportEnerji / 1000, toplamExportOrtalama / 1000, ilkOkumaZaman, sonOkumaZaman, cancellationToken));
            }
            catch (Exception ex)
            {

            }
            return sahaVerileriTabloRaporModels;
        }

        public async Task<List<TuketimEnerjiVerileriTabloRaporModel>> GetTuketimEnerjiVerileriTabloByCihazList(List<int> cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            List<TuketimEnerjiVerileriTabloRaporModel> sahaVerileriTabloRaporModels = new List<TuketimEnerjiVerileriTabloRaporModel>();
            try
            {
                var sonuclar = await _logsRepository.GetTuketimEnerjiVerileriByCokluCihazId(cihazNodeIds, baslangicZamani, bitisZamani, cancellationToken);
                decimal toplamImportEnerji = 0;
                decimal toplamExportEnerji = 0;
                decimal toplamImportOrtalama = 0;
                decimal toplamExportOrtalama = 0;
                DateTime? ilkOkumaZaman = null;
                DateTime? sonOkumaZaman = null;

                foreach (var sonuc in sonuclar)
                {
                    if (!sonuc.Contains("import_fark") || !sonuc.Contains("export_fark") || !sonuc.Contains("dakika_farki"))
                        continue;
                    var importDeger = sonuc.GetValue("import_fark").ToDecimal();
                    var exportDeger = sonuc.GetValue("export_fark").ToDecimal();
                    var dakikaFarki = sonuc.GetValue("dakika_farki").ToDecimal();
                    ilkOkumaZaman = sonuc.GetValue("ilk_zaman").ToUniversalTime().AddHours(-3);
                    sonOkumaZaman = sonuc.GetValue("son_zaman").ToUniversalTime().AddHours(-3);

                    if (dakikaFarki == 0)
                        continue;

                    // Hesaplamalar
                    var importAktifEnerji = importDeger;
                    var importOrtalamaEnerji = importDeger / (dakikaFarki / 60);

                    var exportAktifEnerji = exportDeger;
                    var exportOrtalamaEnerji = exportDeger / (dakikaFarki / 60);

                    // Toplamlara ekle
                    toplamImportEnerji += importAktifEnerji;
                    toplamExportEnerji += exportAktifEnerji;
                    toplamImportOrtalama += importOrtalamaEnerji;
                    toplamExportOrtalama += exportOrtalamaEnerji;
                }
                sahaVerileriTabloRaporModels.AddRange(AnalizorDegerlerToTuketimEnerjiVerileriTabloRapor(toplamImportEnerji / 1000, toplamImportOrtalama / 1000, toplamExportEnerji / 1000, toplamExportOrtalama / 1000, ilkOkumaZaman, sonOkumaZaman, cancellationToken));
            }
            catch (Exception ex)
            {

            }
            return sahaVerileriTabloRaporModels;
        }

        public async Task<long> GetTuketimBedeliGenel(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken cancellationToken)
        {
            try
            {
                var result = new List<EnerjiMaliyetSonuc>();

                var cihazlar = await _cihazRepository.GetAllCihazByNodeIdAndType(nodeId, nodeType);
                if (cihazlar == null || cihazlar.Count == 0) return 0;

                // 1) TGT + PTF
                var tgt = await GetTGTFromEPIAS(cancellationToken);
                var ptfItems = await GetPtfValues(baslangicZamani, bitisZamani, tgt, cancellationToken);
                if (ptfItems == null)
                {
                    throw new Exception("EPIAS veri çekilirken hata oluştu!");
                }

                // PTF map’le
                // Key: (Date.Date, Date.Hour)
                var ptfMap = ptfItems.GroupBy(x => (x.Date.Date, x.Date.Hour)).ToDictionary(g => g.Key, g => g.First().Price);

                // 2) Parametre ay bazlı
                var paramCache = new Dictionary<(int yil, int ay), EnerjiMaliyetParametreler>();
                var enerjiParametreler = await _treeRepository.GetAllParametrelerByLokasyonId(lokasyonId);
                foreach (var cihaz in cihazlar)
                {
                    // 3) Cihaz için Mongo’dan parçalı tüketimler
                    var segmentler = await _logsRepository.GetSaatlikTuketimFromMongoAsync(
                        cihaz.Id, baslangicZamani, bitisZamani, cancellationToken);

                    foreach (var seg in segmentler)
                    {
                        var baslangic = seg["baslangic"].ToUniversalTime().AddHours(3);
                        var bitis = seg["bitis"].ToUniversalTime().AddHours(3);

                        var dakika = seg["dakika"].ToDouble();
                        var importFarkWh = seg["import_fark"].ToDecimal();
                        var exportFarkWh = seg["export_fark"].ToDecimal();
                        var netTuketimWh = seg.Contains("net_tuketim_wh") ? seg["net_tuketim_wh"].ToDecimal() : (importFarkWh - exportFarkWh);

                        if (dakika <= 0) continue;

                        // 4) Bu segmentin ayına uygun parametreleri bul/cache’le
                        var key = (baslangic.Year, baslangic.Month);
                        if (!paramCache.TryGetValue(key, out var param))
                        {
                            param = enerjiParametreler.FirstOrDefault(x => x.Yil == baslangic.Year && x.Ay == baslangic.Month);
                            if (param == null)
                            {
                                continue;
                            }
                            paramCache[key] = param;
                        }

                        var yekdemTlMWh = param.Yekdem;
                        var dagitimTlMWh = param.DagitimBedeli;
                        var anlasmaOrYuzde = param.AnlasmaOrani;
                        var anlasmaOr = anlasmaOrYuzde / 100m;

                        // 5) İlgili saat için PTF
                        ptfMap.TryGetValue((baslangic.Date, baslangic.Hour), out var ptfTlMWh);

                        // 6) Maliyet hesabı                   
                        var enerjiBirimTlKWh = ((ptfTlMWh + yekdemTlMWh) * (1 + anlasmaOr)) / 1000m;
                        var dagitimBirimTlKWh = dagitimTlMWh / 1000m;
                        var birimFiyatTlKWh = enerjiBirimTlKWh + dagitimBirimTlKWh;

                        var tuketimWh = importFarkWh;

                        var tuketimKWh = tuketimWh / 1000m;
                        var maliyetTl = tuketimKWh * birimFiyatTlKWh;

                        result.Add(new EnerjiMaliyetSonuc
                        {
                            MaliyetTl = maliyetTl
                        });
                    }
                }
                return (long)result.Sum(x => x.MaliyetTl);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<long> GetTuketimBedeliByCihazList(List<int> cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken cancellationToken)
        {
            try
            {
                var result = new List<EnerjiMaliyetSonuc>();

                if (cihazNodeIds == null || cihazNodeIds.Count == 0) return 0;

                // 1) TGT + PTF
                var tgt = await GetTGTFromEPIAS(cancellationToken);
                var ptfItems = await GetPtfValues(baslangicZamani, bitisZamani, tgt, cancellationToken);
                if (ptfItems == null)
                {
                    throw new Exception("EPIAS veri çekilirken hata oluştu!");
                }

                // PTF map’le
                // Key: (Date.Date, Date.Hour)
                var ptfMap = ptfItems.GroupBy(x => (x.Date.Date, x.Date.Hour)).ToDictionary(g => g.Key, g => g.First().Price);

                // 2) Parametre ay bazlı
                var paramCache = new Dictionary<(int yil, int ay), EnerjiMaliyetParametreler>();
                var enerjiParametreler = await _treeRepository.GetAllParametrelerByLokasyonId(lokasyonId);
                foreach (var cihazId in cihazNodeIds)
                {
                    // 3) Cihaz için Mongo’dan parçalı tüketimler
                    var segmentler = await _logsRepository.GetSaatlikTuketimFromMongoAsync(
                        cihazId, baslangicZamani, bitisZamani, cancellationToken);

                    foreach (var seg in segmentler)
                    {
                        var baslangic = seg["baslangic"].ToUniversalTime().AddHours(3);
                        var bitis = seg["bitis"].ToUniversalTime().AddHours(3);

                        var dakika = seg["dakika"].ToDouble();
                        var importFarkWh = seg["import_fark"].ToDecimal();
                        var exportFarkWh = seg["export_fark"].ToDecimal();
                        var netTuketimWh = seg.Contains("net_tuketim_wh") ? seg["net_tuketim_wh"].ToDecimal() : (importFarkWh - exportFarkWh);

                        if (dakika <= 0) continue;

                        // 4) Bu segmentin ayına uygun parametreleri bul/cache’le
                        var key = (baslangic.Year, baslangic.Month);
                        if (!paramCache.TryGetValue(key, out var param))
                        {
                            param = enerjiParametreler.FirstOrDefault(x => x.Yil == baslangic.Year && x.Ay == baslangic.Month);
                            if (param == null)
                            {
                                continue;
                            }
                            paramCache[key] = param;
                        }

                        var yekdemTlMWh = param.Yekdem;
                        var dagitimTlMWh = param.DagitimBedeli;
                        var anlasmaOrYuzde = param.AnlasmaOrani;
                        var anlasmaOr = anlasmaOrYuzde / 100m;

                        // 5) İlgili saat için PTF
                        ptfMap.TryGetValue((baslangic.Date, baslangic.Hour), out var ptfTlMWh);

                        // 6) Maliyet hesabı                   
                        var enerjiBirimTlKWh = ((ptfTlMWh + yekdemTlMWh) * (1 + anlasmaOr)) / 1000m;
                        var dagitimBirimTlKWh = dagitimTlMWh / 1000m;
                        var birimFiyatTlKWh = enerjiBirimTlKWh + dagitimBirimTlKWh;

                        var tuketimWh = importFarkWh;

                        var tuketimKWh = tuketimWh / 1000m;
                        var maliyetTl = tuketimKWh * birimFiyatTlKWh;

                        result.Add(new EnerjiMaliyetSonuc
                        {
                            MaliyetTl = maliyetTl
                        });
                    }
                }
                return (long)result.Sum(x => x.MaliyetTl);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<EnerjiMaliyetSonuc>> GetDetayliTuketimBedeli(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken ct)
        {
            try
            {
                var result = new List<EnerjiMaliyetSonuc>();

                var cihazlar = await _cihazRepository.GetAllCihazByNodeIdAndType(nodeId, nodeType);
                if (cihazlar == null || cihazlar.Count == 0) return result;

                // 1) TGT + PTF
                var tgt = await GetTGTFromEPIAS(ct);
                var ptfItems = await GetPtfValues(baslangicZamani, bitisZamani, tgt, ct);
                if (ptfItems == null)
                {
                    throw new Exception("EPIAS veri çekilirken hata oluştu!");
                }

                // PTF map’le
                // Key: (Date.Date, Date.Hour)
                var ptfMap = ptfItems.GroupBy(x => (x.Date.Date, x.Date.Hour)).ToDictionary(g => g.Key, g => g.First().Price);

                // 2) Parametre ay bazlı
                var paramCache = new Dictionary<(int yil, int ay), EnerjiMaliyetParametreler>();
                var enerjiParametreler = await _treeRepository.GetAllParametrelerByLokasyonId(lokasyonId);
                foreach (var cihaz in cihazlar)
                {
                    // 3) Cihaz için Mongo’dan parçalı tüketimler
                    var segmentler = await _logsRepository.GetSaatlikTuketimFromMongoAsync(
                        cihaz.Id, baslangicZamani, bitisZamani, ct);

                    foreach (var seg in segmentler)
                    {
                        var baslangic = seg["baslangic"].ToUniversalTime().AddHours(3);
                        var bitis = seg["bitis"].ToUniversalTime().AddHours(3);

                        var dakika = seg["dakika"].ToDouble();
                        var importFarkWh = seg["import_fark"].ToDecimal();
                        var exportFarkWh = seg["export_fark"].ToDecimal();
                        var netTuketimWh = seg.Contains("net_tuketim_wh") ? seg["net_tuketim_wh"].ToDecimal() : (importFarkWh - exportFarkWh);

                        if (dakika <= 0) continue;

                        // 4) Bu segmentin ayına uygun parametreleri bul/cache’le
                        var key = (baslangic.Year, baslangic.Month);
                        if (!paramCache.TryGetValue(key, out var param))
                        {
                            param = enerjiParametreler.FirstOrDefault(x => x.Yil == baslangic.Year && x.Ay == baslangic.Month);
                            if (param == null)
                            {
                                continue;
                            }
                            paramCache[key] = param;
                        }

                        var yekdemTlMWh = param.Yekdem;
                        var dagitimTlMWh = param.DagitimBedeli;
                        var anlasmaOrYuzde = param.AnlasmaOrani;
                        var anlasmaOr = anlasmaOrYuzde / 100m;

                        // 5) İlgili saat için PTF
                        ptfMap.TryGetValue((baslangic.Date, baslangic.Hour), out var ptfTlMWh);

                        // 6) Maliyet hesabı                   
                        var enerjiBirimTlKWh = ((ptfTlMWh + yekdemTlMWh) * (1 + anlasmaOr)) / 1000m;
                        var dagitimBirimTlKWh = dagitimTlMWh / 1000m;
                        var birimFiyatTlKWh = enerjiBirimTlKWh + dagitimBirimTlKWh;

                        var tuketimWh = importFarkWh;

                        var tuketimKWh = tuketimWh / 1000m;
                        var maliyetTl = tuketimKWh * birimFiyatTlKWh;

                        result.Add(new EnerjiMaliyetSonuc
                        {
                            CihazId = cihaz.Id,
                            Baslangic = baslangic,
                            Bitis = bitis,
                            Dakika = dakika,

                            ImportFarkWh = importFarkWh,
                            ExportFarkWh = exportFarkWh,
                            NetTuketimWh = netTuketimWh,
                            TuketimKWh = tuketimKWh,

                            PtfTlMWh = ptfTlMWh,
                            YekdemTlMWh = yekdemTlMWh,
                            DagitimTlMWh = dagitimTlMWh,
                            AnlasmaOraniYuzde = anlasmaOrYuzde,

                            BirimFiyatTlKWh = birimFiyatTlKWh,
                            MaliyetTl = maliyetTl
                        });
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        // ─────────────────────────────────────────────────────────────────────────────
        // FATURA HESAPLAMA SERVİSİ
        // ─────────────────────────────────────────────────────────────────────────────

        public static class FaturaHesaplamaServisi
        {
            private const decimal EtvOrani = 0.05m;   // %5  — sadece Aktif Enerji üzerinden
            private const decimal KdvOrani = 0.10m;   // %10 — (ToplamTutar + ETV) üzerinden

            // ── Yardımcılar ───────────────────────────────────────────────────────────

            /// <summary>
            /// Türkiye local saatine (UTC+3) göre zaman dilimini döner.
            ///   Gündüz : 06:00 ≤ saat &lt; 17:00
            ///   Puant  : 17:00 ≤ saat &lt; 22:00
            ///   Gece   : 22:00 ≤ saat VEYA saat &lt; 06:00
            /// </summary>
            public static ZamanDilimi ZamanDilimiSec(DateTime localZaman)
            {
                int saat = localZaman.Hour;
                if (saat >= 6 && saat < 17) return ZamanDilimi.Gunduz;
                if (saat >= 17 && saat < 22) return ZamanDilimi.Puant;
                return ZamanDilimi.Gece;
            }

            /// <summary>
            /// [baslangic, bitis) segmentini saat sınırlarında keserek
            /// her dilime düşen dakikayı döner.
            /// baslangic/bitis → Türkiye local zamanı (UTC+3) olmalıdır.
            /// </summary>
            public static (decimal Gunduz, decimal Puant, decimal Gece)
                SegmentiDilimlereAyir(DateTime baslangic, DateTime bitis)
            {
                double toplamDk = (bitis - baslangic).TotalMinutes;
                if (toplamDk <= 0) return (0, 0, 0);

                double gDk = 0, pDk = 0, geDk = 0;
                var current = baslangic;

                while (current < bitis)
                {
                    var sonrakiSaat = new DateTime(
                        current.Year, current.Month, current.Day,
                        current.Hour, 0, 0).AddHours(1);

                    var segEnd = bitis < sonrakiSaat ? bitis : sonrakiSaat;
                    double dilimDk = (segEnd - current).TotalMinutes;

                    switch (ZamanDilimiSec(current))
                    {
                        case ZamanDilimi.Gunduz: gDk += dilimDk; break;
                        case ZamanDilimi.Puant: pDk += dilimDk; break;
                        case ZamanDilimi.Gece: geDk += dilimDk; break;
                    }
                    current = segEnd;
                }

                return ((decimal)gDk, (decimal)pDk, (decimal)geDk);
            }

            // ── 1) Genel Hesap — EPIAS bazlı, ETV/KDV/Güç YOK ───────────────────────

            /// <summary>
            /// PTF + YEKDEM + Anlaşma oranı ile birim fiyat hesaplar.
            /// Fiyatlar TL/MWh cinsinden gelir, kWh'e çevrilir.
            /// </summary>
            public static GenelMaliyetSonuc GenelHesapla(
                decimal tuketimKwh,
                decimal ptfTlMwh,
                decimal yekdemTlMwh,
                decimal anlasmaOraniYuzde,
                decimal dagitimTlMwh)
            {
                decimal anlasmaOr = anlasmaOraniYuzde / 100m;
                decimal enerjiBirimKwh = ((ptfTlMwh + yekdemTlMwh) * (1 + anlasmaOr)) / 1000m;
                decimal dagitimBirimKwh = dagitimTlMwh / 1000m;
                decimal birimFiyat = enerjiBirimKwh + dagitimBirimKwh;
                decimal enerji = tuketimKwh * enerjiBirimKwh;
                decimal dagitim = tuketimKwh * dagitimBirimKwh;
                decimal toplam = tuketimKwh * birimFiyat;

                return new GenelMaliyetSonuc
                {
                    ToplamTuketimKwh = tuketimKwh,
                    EnerjiBedeli = enerji,
                    DagitimBedeli = dagitim,
                    ToplamTutar = toplam,
                    BirimMaliyet = tuketimKwh > 0 ? toplam / tuketimKwh : 0
                };
            }

            // ── 2) Tek Zamanlı Hesap — DB sabit birim fiyat, ETV + KDV + Güç VAR ─────

            /// <summary>
            /// Toplam tüketim → tek aktif birim fiyat.
            /// ETV (%5 × Aktif) + KDV (%10 × Toplam+ETV) + Güç bedeli uygulanır.
            /// </summary>
            public static TekZamanliSonuc TekZamanliHesapla(
                decimal toplamTuketimKwh,
                decimal aktifBirimTlKwh,
                decimal dagitimBirimTlKwh,
                decimal gucBedeli)
            {
                decimal aktif = toplamTuketimKwh * aktifBirimTlKwh;
                decimal dagitim = toplamTuketimKwh * dagitimBirimTlKwh;
                decimal toplam = aktif + dagitim + gucBedeli;
                decimal etv = aktif * EtvOrani;
                decimal kdv = (toplam + etv) * KdvOrani;
                decimal fatura = toplam + etv + kdv;

                return new TekZamanliSonuc
                {
                    ToplamTuketimKwh = toplamTuketimKwh,
                    AktifEnerjiBedeli = aktif,
                    DagitimBedeli = dagitim,
                    ToplamTutar = toplam,
                    ElektrikTuketimVergisi = etv,
                    Kdv = kdv,
                    FaturaTutari = fatura,
                    BirimMaliyet = toplamTuketimKwh > 0 ? fatura / toplamTuketimKwh : 0
                };
            }

            // ── 3) Üç Zamanlı Hesap — Gündüz/Puant/Gece ayrı birim, ETV + KDV + Güç ─

            /// <summary>
            /// Her dilim ayrı aktif birim fiyat alır, dağıtım ortak.
            /// ETV (%5 × ToplamAktif) + KDV (%10 × Toplam+ETV) + Güç bedeli uygulanır.
            /// </summary>
            public static UcZamanliSonuc UcZamanliHesapla(
                decimal gunduzKwh,
                decimal puantKwh,
                decimal geceKwh,
                decimal gunduzBirimTlKwh,
                decimal puantBirimTlKwh,
                decimal geceBirimTlKwh,
                decimal dagitimBirimTlKwh,
                decimal gucBedeli)
            {
                var gunduz = _ZamanDilimiHesapla(gunduzKwh, gunduzBirimTlKwh, dagitimBirimTlKwh);
                var puant = _ZamanDilimiHesapla(puantKwh, puantBirimTlKwh, dagitimBirimTlKwh);
                var gece = _ZamanDilimiHesapla(geceKwh, geceBirimTlKwh, dagitimBirimTlKwh);

                decimal toplamKwh = gunduzKwh + puantKwh + geceKwh;
                decimal toplamAktif = (gunduz.AktifEnerjiBedeli ?? 0m)
                                      + (puant.AktifEnerjiBedeli ?? 0m)
                                      + (gece.AktifEnerjiBedeli ?? 0m);
                decimal toplamDagitim = (gunduz.DagitimBedeli ?? 0m)
                                      + (puant.DagitimBedeli ?? 0m)
                                      + (gece.DagitimBedeli ?? 0m);
                decimal toplam = toplamAktif + toplamDagitim + gucBedeli;
                decimal etv = toplamAktif * EtvOrani;
                decimal kdv = (toplam + etv) * KdvOrani;
                decimal fatura = toplam + etv + kdv;

                return new UcZamanliSonuc
                {
                    Gunduz = gunduz,
                    Puant = puant,
                    Gece = gece,
                    ToplamTuketimKwh = toplamKwh,
                    ToplamAktifEnerji = toplamAktif,
                    ToplamDagitimBedeli = toplamDagitim,
                    ToplamTutar = toplam,
                    ElektrikTuketimVergisi = etv,
                    Kdv = kdv,
                    FaturaTutari = fatura,
                    BirimMaliyet = toplamKwh > 0 ? fatura / toplamKwh : 0
                };
            }

            private static ZamanDilimiDetay _ZamanDilimiHesapla(
                decimal kwh, decimal aktifBirim, decimal dagitimBirim)
            {
                decimal aktif = kwh * aktifBirim;
                decimal dagitim = kwh * dagitimBirim;
                return new ZamanDilimiDetay
                {
                    TuketimKwh = kwh,
                    AktifEnerjiBedeli = aktif,
                    DagitimBedeli = dagitim,
                    ToplamTutar = aktif + dagitim
                };
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // ORTAK GetTuketimBedeli — 3 tarife türü tek metoddan
        // ─────────────────────────────────────────────────────────────────────────────

        public async Task<long> GetTuketimBedeli(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken cancellationToken)
        {
            try
            {
                var cihazlar = await _cihazRepository.GetAllCihazByNodeIdAndType(nodeId, nodeType);
                if (cihazlar == null || cihazlar.Count == 0) return 0;

                var enerjiParametreler = await _treeRepository.GetAllParametrelerByLokasyonId(lokasyonId);

                var tgt = await GetTGTFromEPIAS(cancellationToken);
                var ptfItems = await GetPtfValues(baslangicZamani, bitisZamani, tgt, cancellationToken)
                               ?? throw new Exception("EPIAS veri çekilirken hata oluştu!");

                // Key: (Date.Date, Date.Hour) → TL/MWh
                var ptfMap = ptfItems
                    .GroupBy(x => (x.Date.Date, x.Date.Hour))
                    .ToDictionary(g => g.Key, g => g.First().Price);

                var paramCache = new Dictionary<(int yil, int ay), EnerjiMaliyetParametreler>();
                var gunduzKwhCache = new Dictionary<(int yil, int ay), decimal>();
                var puantKwhCache = new Dictionary<(int yil, int ay), decimal>();
                var geceKwhCache = new Dictionary<(int yil, int ay), decimal>();
                var genelMaliyetCache = new Dictionary<(int yil, int ay), decimal>();

                foreach (var cihaz in cihazlar)
                {
                    // ── Tarife türünü cihazın ilk parametresinden al ──────────────────
                    // Tek cihaz genellikle tek tarife türüne sahip olduğu için
                    // bu ilk parametre segment döngüsüne girmeden tarifeTuru'nu belirler.
                    // Parametre bulunamazsa Genel varsayılır (MongoDB sorgusu zarar görmez).
                    var ilkParam = enerjiParametreler
                        .FirstOrDefault(x => x.Yil == baslangicZamani.Year && x.Ay == baslangicZamani.Month);

                    var tarifeTuru = ilkParam != null
                        ? TarifeTuruBelirle(ilkParam)
                        : TarifeTuru.Genel;

                    // Tarife türüne uygun zaman noktaları ile segmentleri çek
                    var segmentler = await _logsRepository.GetSaatlikTuketimFromMongoAsync(cihaz.Id, baslangicZamani, bitisZamani, tarifeTuru, cancellationToken);
                    foreach (var seg in segmentler)
                    {
                        // GetSaatlikTuketimFromMongoAsync ToLocalTime() ile yazıyor
                        var baslangic = seg["baslangic"].ToLocalTime();
                        var bitis = seg["bitis"].ToLocalTime();
                        var dakika = seg["dakika"].ToDouble();
                        var importFarkWh = seg["import_fark"].ToDecimal();

                        if (dakika <= 0 || importFarkWh <= 0) continue;

                        var tuketimKwh = importFarkWh / 1000m;

                        // Segmentin ayına uygun parametreyi bul / cache'le
                        var key = (baslangic.Year, baslangic.Month);
                        if (!paramCache.TryGetValue(key, out var param))
                        {
                            param = enerjiParametreler
                                .FirstOrDefault(x => x.Yil == baslangic.Year && x.Ay == baslangic.Month);
                            if (param == null) continue;
                            paramCache[key] = param;
                        }

                        // ── Tarife türüne göre biriktir (if/else if — sadece biri çalışır)
                        if (param.Genel.GetValueOrDefault(false))
                        {
                            // Her segment farklı PTF saatine denk gelebilir → hemen hesapla
                            ptfMap.TryGetValue((baslangic.Date, baslangic.Hour), out var ptfTlMwh);

                            var sonuc = FaturaHesaplamaServisi.GenelHesapla(
                                tuketimKwh,
                                ptfTlMwh,
                                param.Yekdem ?? 0m,
                                param.AnlasmaOrani ?? 0m,
                                param.DagitimBedeli ?? 0m);

                            genelMaliyetCache[key] =
                                genelMaliyetCache.GetValueOrDefault(key) + (sonuc.ToplamTutar ?? 0m);
                        }
                        else if (param.TekZamanliTarimsal.GetValueOrDefault(false))
                        {
                            // Tek birim fiyat — tüm tüketim tek kovaya
                            gunduzKwhCache[key] =
                                gunduzKwhCache.GetValueOrDefault(key) + tuketimKwh;
                        }
                        else if (param.UcZamanliTarimsal.GetValueOrDefault(false))
                        {
                            // ZamanNoktalariniOlustur zaten dilim sınırlarında kesti,
                            // bu yüzden her segment tek dilime denk gelir.
                            // SegmentiDilimlereAyir yine de çağrılır: sorgu noktaları
                            // tam sınırda olsa da sayaç okuma zamanı kaynaklı küçük
                            // kaymalar olabilir; orantısal dağıtım bunu tolere eder.
                            var (gDk, pDk, geDk) =
                                FaturaHesaplamaServisi.SegmentiDilimlereAyir(baslangic, bitis);

                            decimal topDk = gDk + pDk + geDk;
                            if (topDk == 0) continue;   // break değil continue — diğer segmentler devam eder

                            gunduzKwhCache[key] = gunduzKwhCache.GetValueOrDefault(key) + tuketimKwh * (gDk / topDk);
                            puantKwhCache[key] = puantKwhCache.GetValueOrDefault(key) + tuketimKwh * (pDk / topDk);
                            geceKwhCache[key] = geceKwhCache.GetValueOrDefault(key) + tuketimKwh * (geDk / topDk);
                        }
                    }
                }

                // ── Her ay için nihai fatura hesabı ──────────────────────────────────

                decimal toplamFatura = 0;

                foreach (var key in paramCache.Keys)
                {
                    var param = paramCache[key];

                    if (param.Genel.GetValueOrDefault(false))
                    {
                        toplamFatura += genelMaliyetCache.GetValueOrDefault(key);
                    }
                    else if (param.TekZamanliTarimsal.GetValueOrDefault(false))
                    {
                        var sonuc = FaturaHesaplamaServisi.TekZamanliHesapla(
                            gunduzKwhCache.GetValueOrDefault(key),
                            param.AktifEnerjiBedeli ?? 0m,
                            param.DagitimBedeli ?? 0m,
                            param.GucBedeli ?? 0m);

                        toplamFatura += sonuc.FaturaTutari ?? 0m;
                    }
                    else if (param.UcZamanliTarimsal.GetValueOrDefault(false))
                    {
                        var sonuc = FaturaHesaplamaServisi.UcZamanliHesapla(
                            gunduzKwhCache.GetValueOrDefault(key),
                            puantKwhCache.GetValueOrDefault(key),
                            geceKwhCache.GetValueOrDefault(key),
                            param.Gunduz ?? 0m,
                            param.Puant ?? 0m,
                            param.Gece ?? 0m,
                            param.DagitimBedeli ?? 0m,
                            param.GucBedeli ?? 0m);

                        toplamFatura += sonuc.FaturaTutari ?? 0m;
                    }
                }

                return (long)toplamFatura;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<long> GetTuketimBedeliYeni(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken cancellationToken)
        {
            try
            {
                var cihazlar = await _cihazRepository.GetAllCihazByNodeIdAndType(nodeId, nodeType);
                if (cihazlar == null || cihazlar.Count == 0)
                    return 0;

                // 1) Her cihazın hangi maliyet grubundan beslendiğini bul
                var cihazIds = cihazlar.Select(x => x.Id).ToList();

                var cihazGrupMap = await _treeRepository.GetCihazEnerjiMaliyetGrupMap(
                    cihazIds,
                    lokasyonId,
                    cancellationToken);

                if (cihazGrupMap == null || cihazGrupMap.Count == 0)
                    return 0;

                var grupIds = cihazGrupMap.Values
                    .Distinct()
                    .ToList();

                // 2) Sadece kullanılan grupların ilgili tarih aralığındaki parametrelerini çek
                var enerjiParametreler = await _treeRepository.GetParametrelerByGrupIds(
                    grupIds,
                    baslangicZamani,
                    bitisZamani,
                    cancellationToken);

                if (enerjiParametreler == null || enerjiParametreler.Count == 0)
                    return 0;

                var tgt = await GetTGTFromEPIAS(cancellationToken);

                var ptfItems = await GetPtfValues(
                    baslangicZamani,
                    bitisZamani,
                    tgt,
                    cancellationToken)
                    ?? throw new Exception("EPIAS veri çekilirken hata oluştu!");

                var ptfMap = ptfItems
                    .GroupBy(x => (x.Date.Date, x.Date.Hour))
                    .ToDictionary(g => g.Key, g => g.First().Price);

                // 3) Cache artık grup bazlı olmalı
                var paramCache = new Dictionary<(int grupId, int yil, int ay), EnerjiMaliyetParametreler>();

                var gunduzKwhCache = new Dictionary<(int grupId, int yil, int ay), decimal>();
                var puantKwhCache = new Dictionary<(int grupId, int yil, int ay), decimal>();
                var geceKwhCache = new Dictionary<(int grupId, int yil, int ay), decimal>();
                var genelMaliyetCache = new Dictionary<(int grupId, int yil, int ay), decimal>();

                foreach (var cihaz in cihazlar)
                {
                    // Bu cihaz için grup bulunamadıysa hesaplama dışı bırak
                    if (!cihazGrupMap.TryGetValue(cihaz.Id, out var grupId))
                        continue;

                    // Cihazın başlangıç ayındaki parametresinden tarife tipini bul
                    var ilkParam = enerjiParametreler
                        .FirstOrDefault(x =>
                            x.EnerjiMaliyetGrupId == grupId &&
                            x.Yil == baslangicZamani.Year &&
                            x.Ay == baslangicZamani.Month);

                    if (ilkParam == null)
                        continue;

                    var tarifeTuru = TarifeTuruBelirle(ilkParam);

                    var segmentler = await _logsRepository.GetSaatlikTuketimFromMongoAsync(
                        cihaz.Id,
                        baslangicZamani,
                        bitisZamani,
                        tarifeTuru,
                        cancellationToken);

                    foreach (var seg in segmentler)
                    {
                        var baslangic = seg["baslangic"].ToLocalTime();
                        var bitis = seg["bitis"].ToLocalTime();
                        var dakika = seg["dakika"].ToDouble();
                        var importFarkWh = seg["import_fark"].ToDecimal();

                        if (dakika <= 0 || importFarkWh <= 0)
                            continue;

                        var tuketimKwh = importFarkWh / 1000m;

                        var key = (grupId, baslangic.Year, baslangic.Month);

                        if (!paramCache.TryGetValue(key, out var param))
                        {
                            param = enerjiParametreler.FirstOrDefault(x =>
                                x.EnerjiMaliyetGrupId == grupId &&
                                x.Yil == baslangic.Year &&
                                x.Ay == baslangic.Month);

                            if (param == null)
                                continue;

                            paramCache[key] = param;
                        }

                        if (param.Genel.GetValueOrDefault(false))
                        {
                            ptfMap.TryGetValue((baslangic.Date, baslangic.Hour), out var ptfTlMwh);

                            var sonuc = FaturaHesaplamaServisi.GenelHesapla(
                                tuketimKwh,
                                ptfTlMwh,
                                param.Yekdem ?? 0m,
                                param.AnlasmaOrani ?? 0m,
                                param.DagitimBedeli ?? 0m);

                            genelMaliyetCache[key] =
                                genelMaliyetCache.GetValueOrDefault(key) + (sonuc.ToplamTutar ?? 0m);
                        }
                        else if (param.TekZamanliTarimsal.GetValueOrDefault(false))
                        {
                            gunduzKwhCache[key] =
                                gunduzKwhCache.GetValueOrDefault(key) + tuketimKwh;
                        }
                        else if (param.UcZamanliTarimsal.GetValueOrDefault(false))
                        {
                            var (gDk, pDk, geDk) =
                                FaturaHesaplamaServisi.SegmentiDilimlereAyir(baslangic, bitis);

                            decimal topDk = gDk + pDk + geDk;

                            if (topDk == 0)
                                continue;

                            gunduzKwhCache[key] =
                                gunduzKwhCache.GetValueOrDefault(key) + tuketimKwh * (gDk / topDk);

                            puantKwhCache[key] =
                                puantKwhCache.GetValueOrDefault(key) + tuketimKwh * (pDk / topDk);

                            geceKwhCache[key] =
                                geceKwhCache.GetValueOrDefault(key) + tuketimKwh * (geDk / topDk);
                        }
                    }
                }

                decimal toplamFatura = 0;

                foreach (var key in paramCache.Keys)
                {
                    var param = paramCache[key];

                    if (param.Genel.GetValueOrDefault(false))
                    {
                        toplamFatura += genelMaliyetCache.GetValueOrDefault(key);
                    }
                    else if (param.TekZamanliTarimsal.GetValueOrDefault(false))
                    {
                        var sonuc = FaturaHesaplamaServisi.TekZamanliHesapla(
                            gunduzKwhCache.GetValueOrDefault(key),
                            param.AktifEnerjiBedeli ?? 0m,
                            param.DagitimBedeli ?? 0m,
                            param.GucBedeli ?? 0m);

                        toplamFatura += sonuc.FaturaTutari ?? 0m;
                    }
                    else if (param.UcZamanliTarimsal.GetValueOrDefault(false))
                    {
                        var sonuc = FaturaHesaplamaServisi.UcZamanliHesapla(
                            gunduzKwhCache.GetValueOrDefault(key),
                            puantKwhCache.GetValueOrDefault(key),
                            geceKwhCache.GetValueOrDefault(key),
                            param.Gunduz ?? 0m,
                            param.Puant ?? 0m,
                            param.Gece ?? 0m,
                            param.DagitimBedeli ?? 0m,
                            param.GucBedeli ?? 0m);

                        toplamFatura += sonuc.FaturaTutari ?? 0m;
                    }
                }

                return (long)toplamFatura;
            }
            catch
            {
                throw;
            }
        }

        // ── Yardımcı: parametre satırından tarife türünü belirle ─────────────────────

        private static TarifeTuru TarifeTuruBelirle(EnerjiMaliyetParametreler param)
        {
            if (param.UcZamanliTarimsal.GetValueOrDefault(false)) return TarifeTuru.UcZamanli;
            if (param.TekZamanliTarimsal.GetValueOrDefault(false)) return TarifeTuru.TekZamanli;
            return TarifeTuru.Genel;
        }

        //public async Task<decimal> FaturaTutariHesapla(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        var cihazlar = await _cihazRepository.GetAllCihazByNodeIdAndType(nodeId, nodeType);
        //        if (cihazlar == null || cihazlar.Count == 0)
        //            return 0;

        //        // Ay bazlı parametre cache
        //        var enerjiParametreler = await _treeRepository.GetAllParametrelerByLokasyonId(lokasyonId);

        //        var paramCache = new Dictionary<(int yil, int ay), EnerjiMaliyetParametreler>();

        //        // Ay bazlı tüketim cache
        //        var gunduzKwhCache = new Dictionary<(int yil, int ay), decimal>();

        //        var puantKwhCache = new Dictionary<(int yil, int ay), decimal>();

        //        var geceKwhCache = new Dictionary<(int yil, int ay), decimal>();

        //        // ─────────────────────────────────────────────────────────────
        //        // TÜKETİMLERİ TOPLA
        //        // ─────────────────────────────────────────────────────────────
        //        foreach (var cihaz in cihazlar)
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();

        //            var segmentler = await _logsRepository.GetSaatlikTuketimFromMongoAsync(cihaz.Id, baslangicZamani, bitisZamani, cancellationToken);

        //            foreach (var seg in segmentler)
        //            {
        //                cancellationToken.ThrowIfCancellationRequested();

        //                // Mongo metodunda ToLocalTime() yapıldığı için tekrar AddHours(3) yok
        //                var baslangic = seg["baslangic"].ToLocalTime();
        //                var bitis = seg["bitis"].ToLocalTime();

        //                var dakika = seg["dakika"].ToDouble();

        //                if (dakika <= 0)
        //                    continue;

        //                // Export düşülmüş net tüketim kullan
        //                var netTuketimWh = seg.Contains("net_tuketim_wh")
        //                    ? seg["net_tuketim_wh"].ToDecimal()
        //                    : seg["import_fark"].ToDecimal()
        //                      - seg["export_fark"].ToDecimal();

        //                if (netTuketimWh <= 0)
        //                    continue;

        //                var tuketimKwh = netTuketimWh / 1000m;

        //                // Ay parametresi
        //                var key = (baslangic.Year, baslangic.Month);

        //                if (!paramCache.TryGetValue(key, out var param))
        //                {
        //                    param = enerjiParametreler.FirstOrDefault(x => x.Yil == baslangic.Year && x.Ay == baslangic.Month);

        //                    if (param == null)
        //                        continue;

        //                    paramCache[key] = param;
        //                }

        //                // ─────────────────────────────────────────────────────
        //                // ÜÇ ZAMANLI
        //                // ─────────────────────────────────────────────────────

        //                if (param.TarifeTuru == TarifeTuru.UcZamanli)
        //                {
        //                    var (gDak, pDak, geDak) =
        //                        FaturaHesaplamaServisi
        //                            .SegmentiDilimlereAyir(baslangic, bitis);

        //                    decimal toplamDakika = gDak + pDak + geDak;

        //                    if (toplamDakika <= 0)
        //                        continue;

        //                    decimal gunduzKwh = tuketimKwh * (gDak / toplamDakika);

        //                    decimal puantKwh = tuketimKwh * (pDak / toplamDakika);

        //                    decimal geceKwh = tuketimKwh * (geDak / toplamDakika);

        //                    gunduzKwhCache[key] = gunduzKwhCache.GetValueOrDefault(key) + gunduzKwh;

        //                    puantKwhCache[key] = puantKwhCache.GetValueOrDefault(key) + puantKwh;

        //                    geceKwhCache[key] = geceKwhCache.GetValueOrDefault(key) + geceKwh;
        //                }

        //                // ─────────────────────────────────────────────────────
        //                // TEK ZAMANLI
        //                // ─────────────────────────────────────────────────────

        //                else
        //                {
        //                    gunduzKwhCache[key] = gunduzKwhCache.GetValueOrDefault(key) + tuketimKwh;
        //                }
        //            }
        //        }

        //        // ─────────────────────────────────────────────────────────────
        //        // FATURA HESABI
        //        // ─────────────────────────────────────────────────────────────

        //        decimal toplamFaturaTutari = 0;

        //        foreach (var key in paramCache.Keys)
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();

        //            var param = paramCache[key];

        //            var gunduzKwh = gunduzKwhCache.GetValueOrDefault(key);

        //            var puantKwh = puantKwhCache.GetValueOrDefault(key);

        //            var geceKwh = geceKwhCache.GetValueOrDefault(key);

        //            // ─────────────────────────────────────────────────────
        //            // ÜÇ ZAMANLI FATURA
        //            // ─────────────────────────────────────────────────────

        //            if (param.TarifeTuru == TarifeTuru.UcZamanli)
        //            {
        //                var tarife = new UcZamanliTarife
        //                {
        //                    GunduzAktifEnerjiBirimFiyatTlKwh = param.GunduzAktifEnerjiTlKwh,
        //                    PuantAktifEnerjiBirimFiyatTlKwh = param.PuantAktifEnerjiTlKwh,
        //                    GeceAktifEnerjiBirimFiyatTlKwh = param.GeceAktifEnerjiTlKwh,
        //                    DagitimBedeliBirimFiyatTlKwh = param.DagitimBedeliTlKwh,
        //                    GucBedeli = param.GucBedeli
        //                };

        //                var sonuc = FaturaHesaplamaServisi.UcZamanliHesapla(gunduzKwh, puantKwh, geceKwh, tarife);
        //                toplamFaturaTutari += sonuc.FaturaTutari;
        //            }

        //            // ─────────────────────────────────────────────────────
        //            // TEK ZAMANLI FATURA
        //            // ─────────────────────────────────────────────────────

        //            else
        //            {
        //                var toplamKwh = gunduzKwh + puantKwh + geceKwh;

        //                var tarife = new TekZamanliTarife
        //                {
        //                    AktifEnerjiBirimFiyatTlKwh = param.AktifEnerjiTlKwh,
        //                    DagitimBedeliBirimFiyatTlKwh = param.DagitimBedeliTlKwh,
        //                    GucBedeli = param.GucBedeli
        //                };

        //                var sonuc = FaturaHesaplamaServisi.TekZamanliHesapla(toplamKwh, tarife);
        //                toplamFaturaTutari += sonuc.FaturaTutari;
        //            }
        //        }

        //        return Math.Round(toplamFaturaTutari, 2);
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        public async Task<List<EnerjiRaporGrafikModel>>
     GetEnerjiRaporFromSecilenAsync(
         List<string> nodeIdsAndTypes,
         DateTime baslangicZamani,
         DateTime bitisZamani,
         int dakikaInterval,
         CancellationToken cancellationToken)
        {
            var cihazlar = new List<vwCihaz>();

            foreach (var item in nodeIdsAndTypes.Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var parts = item.Split(
                    '-',
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2 ||
                    !int.TryParse(parts[1], out var nodeId))
                {
                    continue;
                }

                var nodeType = parts[0];

                var bulunanCihazlar =
                    await _cihazRepository
                        .GetvwCihazlarByNodeIdAndType(
                            nodeId,
                            nodeType);

                cihazlar.AddRange(bulunanCihazlar);
            }

            cihazlar = cihazlar
                .DistinctBy(x => x.Id)
                .ToList();

            if (cihazlar.Count == 0)
                return new List<EnerjiRaporGrafikModel>();

            if (cihazlar.Count == 1)
            {
                return await _logsRepository
                    .GetTuketimVerileriForEnerjiGrafikRapor(
                        cihazlar[0],
                        baslangicZamani,
                        bitisZamani,
                        dakikaInterval,
                        cancellationToken);
            }

            return await _logsRepository
                .GetTuketimVerileriForEnerjiGrafikRaporByCokluCihazId(
                    cihazlar,
                    baslangicZamani,
                    bitisZamani,
                    dakikaInterval,
                    cancellationToken);
        }

        public async Task<EnerjiHaftalikRaporModel> GetEnerjiDagilimRaporFromSecilenAsync(List<string> nodeIdsAndTypes, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            try
            {
                EnerjiHaftalikRaporModel result = new EnerjiHaftalikRaporModel();
                List<vwCihaz> cihazlar = new List<vwCihaz>();
                List<int> cihazIds = new List<int>();
                foreach (var item in nodeIdsAndTypes)
                {
                    var parts = item.Split('-');
                    var nodeId = int.Parse(parts[1]);
                    var nodeType = parts[0];
                    cihazIds.Add(nodeId);
                    cihazlar.AddRange(await _cihazRepository.GetvwCihazlarByNodeIdAndType(nodeId, nodeType));
                }
                //var cihazlartum = await _cihazRepository.GetAllAktifCihaz();
                var gerekliIds = cihazlar.Select(x => x.Id).Concat(cihazlar.Select(x => x.ParentCihazId)).Distinct().ToList();
                var cihazlartum = await _cihazRepository.GetAllAktifvwCihazByCihazIds(gerekliIds);

                //var asd = await _logsRepository.GetSayacTuketimleri(cihazlar, cihazlar, baslangicZamani, bitisZamani, cancellationToken);
                result = await _logsRepository.GetEnerjiRaporu(cihazlartum, cihazlar.Where(x => x.ParentCihazId == 0).Select(x => x.Id).ToList(), baslangicZamani, bitisZamani, cancellationToken);
                //if (cihazlar.Count() == 1)
                //{
                //    //result.AddRange(await _logsRepository.GetTuketimVerileriForEnerjiGrafikRapor(cihazlar.First(), baslangicZamani, bitisZamani, dakikaInterval, cancellationToken));
                //}
                //else if (cihazlar.Count() > 1)
                //{
                //    //result.AddRange(await _logsRepository.GetTuketimVerileriForEnerjiGrafikRaporByCokluCihazId(cihazlar, baslangicZamani, bitisZamani, dakikaInterval, cancellationToken));
                //}
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<EnerjiHaftalikRaporModel> GetEnerjiDagilimRaporFromSecilenAsync2(List<string> nodeIdsAndTypes, DateTime baslangicZamani, DateTime bitisZamani, DateTime oncekiDonemBaslangicZamani, DateTime oncekiDonemBitisZamani, CancellationToken cancellationToken)
        {
            try
            {
                EnerjiHaftalikRaporModel result = new EnerjiHaftalikRaporModel();
                List<vwCihaz> cihazlar = new List<vwCihaz>();
                List<int> cihazIds = new List<int>();
                foreach (var item in nodeIdsAndTypes)
                {
                    var parts = item.Split('-');
                    var nodeId = int.Parse(parts[1]);
                    var nodeType = parts[0];
                    cihazIds.Add(nodeId);
                    cihazlar.AddRange(await _cihazRepository.GetvwCihazlarByNodeIdAndType(nodeId, nodeType));
                }
                //var cihazlartum = await _cihazRepository.GetAllAktifCihaz();
                var gerekliIds = cihazlar.Select(x => x.Id).Concat(cihazlar.Select(x => x.ParentCihazId)).Distinct().ToList();
                var cihazlartum = await _cihazRepository.GetAllAktifvwCihazByCihazIds(gerekliIds);

                //var asd = await _logsRepository.GetSayacTuketimleri(cihazlar, cihazlar, baslangicZamani, bitisZamani, cancellationToken);
                result = await _logsRepository.GetEnerjiRaporuDonemli(cihazlartum, cihazlar.Where(x => x.ParentCihazId == 0).Select(x => x.Id).ToList(), baslangicZamani, bitisZamani, oncekiDonemBaslangicZamani, oncekiDonemBitisZamani, cancellationToken);
                //if (cihazlar.Count() == 1)
                //{
                //    //result.AddRange(await _logsRepository.GetTuketimVerileriForEnerjiGrafikRapor(cihazlar.First(), baslangicZamani, bitisZamani, dakikaInterval, cancellationToken));
                //}
                //else if (cihazlar.Count() > 1)
                //{
                //    //result.AddRange(await _logsRepository.GetTuketimVerileriForEnerjiGrafikRaporByCokluCihazId(cihazlar, baslangicZamani, bitisZamani, dakikaInterval, cancellationToken));
                //}
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<TuketimVerileriChartRaporModel>> GetTuketimVerileriByCihazId(int cihazId, CancellationToken cancellationToken)
        {
            // 1. Tüketim verilerini getir
            var analizor = await _logsRepository.GetTuketimVerileriByCihazId(cihazId, cancellationToken);

            // 2. Cihaz bilgisini getir (tek cihaz lazım)
            var cihaz = await _cihazRepository.GetByIdAsync(cihazId);
            if (cihaz == null) return new List<TuketimVerileriChartRaporModel>();

            // 3. Sonuçları dönüştür
            var tuketimler = analizor.Select(doc => new TuketimVerileriChartRaporModel
            {
                CihazAd = cihaz.Name, // doğrudan cihaz objesinden
                Yil = doc["yil"].AsInt32,
                Ay = doc["ay"].AsInt32,
                Tuketim = (decimal)doc["tuketim"].ToDecimal()
            }).ToList();

            return tuketimler;
        }

        public async Task<List<EnergyFarmPacket>> BuildEnergyPayloadForFarms(int kumesTipId, CancellationToken ct)
        {
            // Örnek farm config (sende nasıl ise aynı kalabilir)
            var farms = new List<EnergyFarmConfig>()
                        {
                            new EnergyFarmConfig { Id = "0", Name = "Matlı",           NodeIds = new List<int> { 258, 259, 267, 287 } },
                            new EnergyFarmConfig { Id = "1", Name = "Rahmiye Çiftlik", NodeIds = new List<int> { 258,259 } },
                            new EnergyFarmConfig { Id = "3", Name = "Verim Gıda",       NodeIds = new List<int> { 267, 287 } },

                            //new EnergyFarmConfig { Id = "2", Name = "Merkez Çiftlik", NodeIds = new List<int> { 218 } },
                        };

            // kumesTipId’ye göre filtreleyeceksen burada yap
            var cihazIds = farms.SelectMany(f => f.NodeIds ?? Enumerable.Empty<int>()).Distinct().ToList();   // ör: [219]
            if (cihazIds.Count == 0) return new List<EnergyFarmPacket>();

            var now = DateTime.Now;
            var todayStart = now.Date;                 // 2026-01-07 00:00
            var rangeStart = todayStart.AddDays(-9);   // son 10 gün (bugün dahil)

            // ---------------------------------------------------------
            // 1) Hayvan sayısını tek seferde çek (rangeStart..todayStart)
            //    Not: endDate "dahil" istiyorsan todayStart gönderiyoruz.
            // ---------------------------------------------------------
            // IHayvanSayisiCacheMemory: Dictionary<int nodeId, Dictionary<DateTime day, long pop>>
            var animalMap = await _hayvanSayisiCacheMemory.GetRangeAsync(cihazIds, rangeStart, todayStart, ct);

            long GetPop(int nodeId, DateTime day)
            {
                if (animalMap.TryGetValue(nodeId, out var dayMap) &&
                    dayMap.TryGetValue(day.Date, out var pop))
                    return pop;

                return 0L;
            }

            decimal AvgPop10(int nodeId)
            {
                // 10 günün ortalaması (0 olan günleri hariç tutmak istersen şart ekle)
                long sum = 0;
                int n = 0;
                for (int i = 0; i < 10; i++)
                {
                    var d = rangeStart.AddDays(i);
                    var p = GetPop(nodeId, d);
                    if (p <= 0) continue;
                    sum += p;
                    n++;
                }
                return n > 0 ? (decimal)sum / n : 0m;
            }

            long FarmPopForDay(EnergyFarmConfig farm, DateTime day) =>
                farm.NodeIds.Select(id => GetPop(id, day)).DefaultIfEmpty(0L).Max();

            decimal FarmAvgPop10(EnergyFarmConfig farm) =>
                farm.NodeIds.Select(AvgPop10).DefaultIfEmpty(0m).Max();

            // ---------------------------------------------------------
            // 2) Enerji consumption delta hesabı (first-last)
            //    repository'nin döndürdüğü dokümandan enerji okumasını seçmen lazım
            // ---------------------------------------------------------
            decimal ReadEnergyValue(object doc)
            {
                // TODO: Burayı kendi log modeline göre düzenle
                // örnek:
                // var x = (EnerjiLogDoc)doc;
                // return x.ImportKwh ?? 0m;

                // şimdilik:
                return 0m;
            }

            // Bu helper: cihaz bazında (start..end) toplam tüketimi hesaplar (son - ilk)
            Dictionary<int, decimal> CalcTotalByCihaz(IEnumerable<int> ids, List<object> docs)
            {
                // docs içinde cihazId + timestamp + value var varsayımı
                // TODO: docs tipini "object" yerine kendi tipin yap.
                var result = ids.ToDictionary(id => id, _ => 0m);

                // Burada örnek bir yaklaşım veriyorum:
                // docs = her cihaz için ilk ve son kaydı içeriyor varsayımı
                // Eğer repository zaten first/last döndürüyorsa daha basit.
                //
                // En sağlıklısı: repository response'unu şu yapıda döndürmek:
                // List<(int CihazId, decimal First, decimal Last)>

                return result;
            }

            // ✅ Senin mevcut yapın: GetFirstLastByCihazIdsAsync dönüyor + CalcMap var.
            // Ben burada "delta"yı CalcMap'ten alacağını varsayıyorum.
            // CalcMap(dayDocs) => Dictionary<int, (decimal total, decimal per)> gibi bir şeydi sende.
            // Biz per'i artık hayvanMap'ten hesaplayacağız, o yüzden sadece total'ı kullanacağız.

            // ---------------------------------------------------------
            // 3) Bugün ve Son10 toplamlarını çek (log)
            // ---------------------------------------------------------
            var todayDocs = await _logsRepository.GetFirstLastByCihazIdsAsync(cihazIds, todayStart, now, ct);
            var last10Docs = await _logsRepository.GetFirstLastByCihazIdsAsync(cihazIds, rangeStart, now, ct);

            // Senin mevcut CalcMap: cihazId -> (total, per) dönüyordu
            // Burada total'ı alıyoruz
            var todayMap = CalcMap(todayDocs);   // Dictionary<int, (decimal total, decimal per)>
            var last10Map = CalcMap(last10Docs);  // Dictionary<int, (decimal total, decimal per)>

            decimal GetTotalFromMap(Dictionary<int, (decimal total, decimal per)> map, int nodeId)
                => map.TryGetValue(nodeId, out var v) ? v.total : 0m;

            // ---------------------------------------------------------
            // 4) Trend (10 gün): her gün total + per (günlük pop)
            // ---------------------------------------------------------
            var trendTotalByCihaz = cihazIds.ToDictionary(id => id, _ => new List<decimal>(10));

            for (int i = 9; i >= 0; i--)
            {
                var dayStart = todayStart.AddDays(-i);
                var dayEnd = (i == 0) ? now : dayStart.AddDays(1);

                var dayDocs = await _logsRepository.GetFirstLastByCihazIdsAsync(cihazIds, dayStart, dayEnd, ct);
                var dayMap = CalcMap(dayDocs);

                foreach (var cihazId in cihazIds)
                {
                    var dailyTotal = GetTotalFromMap(dayMap, cihazId);
                    trendTotalByCihaz[cihazId].Add(dailyTotal);
                }
            }

            // ---------------------------------------------------------
            // 5) Output paket
            // ---------------------------------------------------------
            var result = new List<EnergyFarmPacket>(farms.Count);
            var subFarms = farms.Where(f => f.Id != "0").ToList();
            foreach (var f in subFarms)
            {
                var todayTotal = f.NodeIds.Sum(id => GetTotalFromMap(todayMap, id));
                var last10Total = f.NodeIds.Sum(id => GetTotalFromMap(last10Map, id));

                var todayPop = FarmPopForDay(f, todayStart);
                var todayPer = (todayPop > 0) ? (todayTotal / todayPop) : 0m;

                // total10Per için iki seçenek:
                // A) Ortalama pop ile böl (pratik)
                var avgPop = FarmAvgPop10(f);
                var total10Per = (avgPop > 0) ? (last10Total / avgPop) : 0m;

                // B) Hen-days yaklaşımı (daha matematiksel):
                // var henDays = trendPerByCihaz[f.NodeId] ... değil!
                // doğru henDays: sum(popDay) -> total10 / (henDays/10) gibi bir yorum,
                // ama çoğu UI "ortalama hayvan" gibi davranmasını ister.
                // İstersen bunu da ekleyebilirsin.
                var farmTrendTotal = new List<decimal>(10);
                var farmTrendPer = new List<decimal>(10);
                for (int i = 0; i < 10; i++)
                {
                    decimal dailyTotalSum = 0m;

                    foreach (var nodeId in f.NodeIds)
                    {
                        if (trendTotalByCihaz.TryGetValue(nodeId, out var totalList))
                            dailyTotalSum += totalList[i];
                    }

                    farmTrendTotal.Add(dailyTotalSum);
                    var dailyPop = FarmPopForDay(f, rangeStart.AddDays(i));
                    farmTrendPer.Add(dailyPop > 0 ? dailyTotalSum * 1000 / dailyPop : 0m);
                }
                result.Add(new EnergyFarmPacket
                {
                    id = f.Id,
                    name = f.Name,
                    energy = new EnergyWidgetDto
                    {
                        today = todayTotal,
                        todayPer = todayPer * 1000,
                        total10 = last10Total,
                        total10Per = total10Per * 1000,
                        trendTotal = farmTrendTotal,
                        trendPer = farmTrendPer
                    }
                });
                //result.Add(new EnergyFarmPacket
                //{
                //    id = f.Id,
                //    name = f.Name,
                //    energy = new EnergyWidgetDto
                //    {
                //        today = todayTotal,
                //        todayPer = todayPer * 1000,
                //        total10 = last10Total,
                //        total10Per = total10Per * 1000,
                //        trendTotal = trendTotalByCihaz[f.NodeId],
                //        trendPer = trendPerByCihaz[f.NodeId]
                //    }
                //});
            }

            // 2. ANA ÇİFTLİĞİ (MATLI) ALT ÇİFTLİKLERİN TOPLAMINDAN OLUŞTUR
            var matliConfig = farms.FirstOrDefault(f => f.Id == "0");
            if (matliConfig != null)
            {
                // Totalleri (Tüketimleri) doğrudan result listesindeki DTO'lardan topluyoruz
                var matliTodayTotal = result.Sum(r => r.energy.today);
                var matliTotal10 = result.Sum(r => r.energy.total10);

                var matliTrendTotal = new List<decimal>(10);
                for (int i = 0; i < 10; i++)
                {
                    matliTrendTotal.Add(result.Sum(r => r.energy.trendTotal[i]));
                }

                // Per (Ortalama) hesapları için toplamları toplam hayvana BÖLMELİYİZ
                var matliTodayPop = subFarms.Sum(f => FarmPopForDay(f, todayStart));
                var matliAvgPop10 = subFarms.Sum(FarmAvgPop10);

                var matliTrendPer = new List<decimal>(10);
                for (int i = 0; i < 10; i++)
                {
                    var dayStart = todayStart.AddDays(-(9 - i)); // Döngü sırasına göre günü bul
                    long dailyPopSum = subFarms.Sum(f => FarmPopForDay(f, dayStart));

                    matliTrendPer.Add(dailyPopSum > 0 ? (matliTrendTotal[i] * 1000 / dailyPopSum) : 0m);
                }

                // Matlı'yı listenin en başına (index: 0) ekliyoruz
                result.Insert(0, new EnergyFarmPacket
                {
                    id = matliConfig.Id,
                    name = matliConfig.Name,
                    energy = new EnergyWidgetDto
                    {
                        today = matliTodayTotal,
                        todayPer = matliTodayPop > 0 ? (matliTodayTotal * 1000 / matliTodayPop) : 0m,
                        total10 = matliTotal10,
                        total10Per = matliAvgPop10 > 0 ? (matliTotal10 * 1000 / matliAvgPop10) : 0m,
                        trendTotal = matliTrendTotal,
                        trendPer = matliTrendPer
                    }
                });
            }

            return result;
        }
        public async Task<List<EnergyFarmPacket>> BuildEnergyPayloadForFarms0(int kumesTipId, CancellationToken ct)
        {
            // Örnek farm config (sende nasıl ise aynı kalabilir)
            var farms = new List<EnergyFarmConfig>()
                        {
                            new EnergyFarmConfig { Id = "0", Name = "Matlı",           NodeIds = new List<int> { 258, 259 } },
                            new EnergyFarmConfig { Id = "1", Name = "Rahmiye Çiftlik", NodeIds = new List<int> { 258, 259 } },
                            new EnergyFarmConfig { Id = "2", Name = "Merkez Çiftlik", NodeIds = new List<int> { 218 } },
                        };

            // kumesTipId’ye göre filtreleyeceksen burada yap
            var cihazIds = farms.SelectMany(f => f.NodeIds ?? Enumerable.Empty<int>()).Distinct().ToList();   // ör: [219]
            if (cihazIds.Count == 0) return new List<EnergyFarmPacket>();

            var now = DateTime.Now;
            var todayStart = now.Date;                 // 2026-01-07 00:00
            var rangeStart = todayStart.AddDays(-9);   // son 10 gün (bugün dahil)

            // ---------------------------------------------------------
            // 1) Hayvan sayısını tek seferde çek (rangeStart..todayStart)
            //    Not: endDate "dahil" istiyorsan todayStart gönderiyoruz.
            // ---------------------------------------------------------
            // IHayvanSayisiCacheMemory: Dictionary<int nodeId, Dictionary<DateTime day, long pop>>
            var animalMap = await _hayvanSayisiCacheMemory.GetRangeAsync(cihazIds, rangeStart, todayStart, ct);

            long GetPop(int nodeId, DateTime day)
            {
                if (animalMap.TryGetValue(nodeId, out var dayMap) &&
                    dayMap.TryGetValue(day.Date, out var pop))
                    return pop;

                return 0L;
            }

            decimal AvgPop10(int nodeId)
            {
                // 10 günün ortalaması (0 olan günleri hariç tutmak istersen şart ekle)
                long sum = 0;
                int n = 0;
                for (int i = 0; i < 10; i++)
                {
                    var d = rangeStart.AddDays(i);
                    var p = GetPop(nodeId, d);
                    if (p <= 0) continue;
                    sum += p;
                    n++;
                }
                return n > 0 ? (decimal)sum / n : 0m;
            }

            long TodayPop(int nodeId) => GetPop(nodeId, todayStart);

            // ---------------------------------------------------------
            // 2) Enerji consumption delta hesabı (first-last)
            //    repository'nin döndürdüğü dokümandan enerji okumasını seçmen lazım
            // ---------------------------------------------------------
            decimal ReadEnergyValue(object doc)
            {
                // TODO: Burayı kendi log modeline göre düzenle
                // örnek:
                // var x = (EnerjiLogDoc)doc;
                // return x.ImportKwh ?? 0m;

                // şimdilik:
                return 0m;
            }

            // Bu helper: cihaz bazında (start..end) toplam tüketimi hesaplar (son - ilk)
            Dictionary<int, decimal> CalcTotalByCihaz(IEnumerable<int> ids, List<object> docs)
            {
                // docs içinde cihazId + timestamp + value var varsayımı
                // TODO: docs tipini "object" yerine kendi tipin yap.
                var result = ids.ToDictionary(id => id, _ => 0m);

                // Burada örnek bir yaklaşım veriyorum:
                // docs = her cihaz için ilk ve son kaydı içeriyor varsayımı
                // Eğer repository zaten first/last döndürüyorsa daha basit.
                //
                // En sağlıklısı: repository response'unu şu yapıda döndürmek:
                // List<(int CihazId, decimal First, decimal Last)>

                return result;
            }

            // ✅ Senin mevcut yapın: GetFirstLastByCihazIdsAsync dönüyor + CalcMap var.
            // Ben burada "delta"yı CalcMap'ten alacağını varsayıyorum.
            // CalcMap(dayDocs) => Dictionary<int, (decimal total, decimal per)> gibi bir şeydi sende.
            // Biz per'i artık hayvanMap'ten hesaplayacağız, o yüzden sadece total'ı kullanacağız.

            // ---------------------------------------------------------
            // 3) Bugün ve Son10 toplamlarını çek (log)
            // ---------------------------------------------------------
            var todayDocs = await _logsRepository.GetFirstLastByCihazIdsAsync(cihazIds, todayStart, now, ct);
            var last10Docs = await _logsRepository.GetFirstLastByCihazIdsAsync(cihazIds, rangeStart, now, ct);

            // Senin mevcut CalcMap: cihazId -> (total, per) dönüyordu
            // Burada total'ı alıyoruz
            var todayMap = CalcMap(todayDocs);   // Dictionary<int, (decimal total, decimal per)>
            var last10Map = CalcMap(last10Docs);  // Dictionary<int, (decimal total, decimal per)>

            decimal GetTotalFromMap(Dictionary<int, (decimal total, decimal per)> map, int nodeId)
                => map.TryGetValue(nodeId, out var v) ? v.total : 0m;

            // ---------------------------------------------------------
            // 4) Trend (10 gün): her gün total + per (günlük pop)
            // ---------------------------------------------------------
            var trendTotalByCihaz = cihazIds.ToDictionary(id => id, _ => new List<decimal>(10));
            var trendPerByCihaz = cihazIds.ToDictionary(id => id, _ => new List<decimal>(10));

            for (int i = 9; i >= 0; i--)
            {
                var dayStart = todayStart.AddDays(-i);
                var dayEnd = (i == 0) ? now : dayStart.AddDays(1);

                var dayDocs = await _logsRepository.GetFirstLastByCihazIdsAsync(cihazIds, dayStart, dayEnd, ct);
                var dayMap = CalcMap(dayDocs);

                foreach (var cihazId in cihazIds)
                {
                    var dailyTotal = GetTotalFromMap(dayMap, cihazId);

                    var pop = GetPop(cihazId, dayStart); // ✅ gün bazlı pop
                    var dailyPer = (pop > 0) ? (dailyTotal * 1000 / pop) : 0m;

                    trendTotalByCihaz[cihazId].Add(dailyTotal);
                    trendPerByCihaz[cihazId].Add(dailyPer);
                }
            }

            // ---------------------------------------------------------
            // 5) Output paket
            // ---------------------------------------------------------
            var result = new List<EnergyFarmPacket>(farms.Count);

            foreach (var f in farms)
            {
                var todayTotal = f.NodeIds.Sum(id => GetTotalFromMap(todayMap, id));
                var last10Total = f.NodeIds.Sum(id => GetTotalFromMap(last10Map, id));

                var todayPop = f.NodeIds.Sum(id => TodayPop(id));
                var todayPer = (todayPop > 0) ? (todayTotal / todayPop) : 0m;

                // total10Per için iki seçenek:
                // A) Ortalama pop ile böl (pratik)
                var avgPop = f.NodeIds.Sum(id => AvgPop10(id));
                var total10Per = (avgPop > 0) ? (last10Total / avgPop) : 0m;

                // B) Hen-days yaklaşımı (daha matematiksel):
                // var henDays = trendPerByCihaz[f.NodeId] ... değil!
                // doğru henDays: sum(popDay) -> total10 / (henDays/10) gibi bir yorum,
                // ama çoğu UI "ortalama hayvan" gibi davranmasını ister.
                // İstersen bunu da ekleyebilirsin.
                var farmTrendTotal = new List<decimal>(10);
                var farmTrendPer = new List<decimal>(10);
                for (int i = 0; i < 10; i++)
                {
                    decimal dailyTotalSum = 0m;
                    decimal dailyPerSum = 0m;

                    foreach (var nodeId in f.NodeIds)
                    {
                        if (trendTotalByCihaz.TryGetValue(nodeId, out var totalList))
                            dailyTotalSum += totalList[i];

                        if (trendPerByCihaz.TryGetValue(nodeId, out var perList))
                            dailyPerSum += perList[i];
                    }

                    farmTrendTotal.Add(dailyTotalSum);
                    farmTrendPer.Add(dailyPerSum);
                }
                result.Add(new EnergyFarmPacket
                {
                    id = f.Id,
                    name = f.Name,
                    energy = new EnergyWidgetDto
                    {
                        today = todayTotal,
                        todayPer = todayPer * 1000,
                        total10 = last10Total,
                        total10Per = total10Per * 1000,
                        trendTotal = farmTrendTotal,
                        trendPer = farmTrendPer
                    }
                });
                //result.Add(new EnergyFarmPacket
                //{
                //    id = f.Id,
                //    name = f.Name,
                //    energy = new EnergyWidgetDto
                //    {
                //        today = todayTotal,
                //        todayPer = todayPer * 1000,
                //        total10 = last10Total,
                //        total10Per = total10Per * 1000,
                //        trendTotal = trendTotalByCihaz[f.NodeId],
                //        trendPer = trendPerByCihaz[f.NodeId]
                //    }
                //});
            }

            return result;
        }

        //    public async Task<List<EnergyFarmPacket>> BuildEnergyPayloadForFarms2(int kumesTipId, CancellationToken ct)
        //    {
        //        // Örnek farm config (sende nasıl ise aynı kalabilir)
        //        var farms = new List<EnergyFarmConfig>()
        //{
        //    new EnergyFarmConfig { Id = "0", Name = "Matlı",           NodeId = 219 },
        //    new EnergyFarmConfig { Id = "1", Name = "Rahmiye Çiftlik", NodeId = 219 },
        //};

        //        // kumesTipId’ye göre filtreleyeceksen burada yap
        //        var cihazIds = farms.Select(f => f.NodeId).Distinct().ToList();   // ör: [219]
        //        if (cihazIds.Count == 0) return new List<EnergyFarmPacket>();

        //        var now = DateTime.Now;
        //        var todayStart = now.Date;                 // 2026-01-07 00:00
        //        var rangeStart = todayStart.AddDays(-9);   // son 10 gün (bugün dahil)

        //        // ---------------------------------------------------------
        //        // 1) Hayvan sayısını tek seferde çek (rangeStart..todayStart)
        //        //    Not: endDate "dahil" istiyorsan todayStart gönderiyoruz.
        //        // ---------------------------------------------------------
        //        // IHayvanSayisiCacheMemory: Dictionary<int nodeId, Dictionary<DateTime day, long pop>>
        //        var animalMap = await _hayvanSayisiCacheMemory.GetRangeAsync(cihazIds, rangeStart, todayStart, ct);

        //        long GetPop(int nodeId, DateTime day)
        //        {
        //            if (animalMap.TryGetValue(nodeId, out var dayMap) &&
        //                dayMap.TryGetValue(day.Date, out var pop))
        //                return pop;

        //            return 0L;
        //        }

        //        decimal AvgPop10(int nodeId)
        //        {
        //            // 10 günün ortalaması (0 olan günleri hariç tutmak istersen şart ekle)
        //            long sum = 0;
        //            int n = 0;
        //            for (int i = 0; i < 10; i++)
        //            {
        //                var d = rangeStart.AddDays(i);
        //                var p = GetPop(nodeId, d);
        //                if (p <= 0) continue;
        //                sum += p;
        //                n++;
        //            }
        //            return n > 0 ? (decimal)sum / n : 0m;
        //        }

        //        long TodayPop(int nodeId) => GetPop(nodeId, todayStart);

        //        // ---------------------------------------------------------
        //        // 2) Enerji consumption delta hesabı (first-last)
        //        //    repository'nin döndürdüğü dokümandan enerji okumasını seçmen lazım
        //        // ---------------------------------------------------------
        //        decimal ReadEnergyValue(object doc)
        //        {
        //            // TODO: Burayı kendi log modeline göre düzenle
        //            // örnek:
        //            // var x = (EnerjiLogDoc)doc;
        //            // return x.ImportKwh ?? 0m;

        //            // şimdilik:
        //            return 0m;
        //        }

        //        // Bu helper: cihaz bazında (start..end) toplam tüketimi hesaplar (son - ilk)
        //        Dictionary<int, decimal> CalcTotalByCihaz(IEnumerable<int> ids, List<object> docs)
        //        {
        //            // docs içinde cihazId + timestamp + value var varsayımı
        //            // TODO: docs tipini "object" yerine kendi tipin yap.
        //            var result = ids.ToDictionary(id => id, _ => 0m);

        //            // Burada örnek bir yaklaşım veriyorum:
        //            // docs = her cihaz için ilk ve son kaydı içeriyor varsayımı
        //            // Eğer repository zaten first/last döndürüyorsa daha basit.
        //            //
        //            // En sağlıklısı: repository response'unu şu yapıda döndürmek:
        //            // List<(int CihazId, decimal First, decimal Last)>

        //            return result;
        //        }

        //        // ✅ Senin mevcut yapın: GetFirstLastByCihazIdsAsync dönüyor + CalcMap var.
        //        // Ben burada "delta"yı CalcMap'ten alacağını varsayıyorum.
        //        // CalcMap(dayDocs) => Dictionary<int, (decimal total, decimal per)> gibi bir şeydi sende.
        //        // Biz per'i artık hayvanMap'ten hesaplayacağız, o yüzden sadece total'ı kullanacağız.

        //        // ---------------------------------------------------------
        //        // 3) Bugün ve Son10 toplamlarını çek (log)
        //        // ---------------------------------------------------------
        //        var todayDocs = await _logsRepository.GetFirstLastByCihazIdsAsync(cihazIds, todayStart, now, ct);
        //        var last10Docs = await _logsRepository.GetFirstLastByCihazIdsAsync(cihazIds, rangeStart, now, ct);

        //        // Senin mevcut CalcMap: cihazId -> (total, per) dönüyordu
        //        // Burada total'ı alıyoruz
        //        var todayMap = CalcMap(todayDocs);   // Dictionary<int, (decimal total, decimal per)>
        //        var last10Map = CalcMap(last10Docs);  // Dictionary<int, (decimal total, decimal per)>

        //        decimal GetTotalFromMap(Dictionary<int, (decimal total, decimal per)> map, int nodeId)
        //            => map.TryGetValue(nodeId, out var v) ? v.total : 0m;

        //        // ---------------------------------------------------------
        //        // 4) Trend (10 gün): her gün total + per (günlük pop)
        //        // ---------------------------------------------------------
        //        var trendTotalByCihaz = cihazIds.ToDictionary(id => id, _ => new List<decimal>(10));
        //        var trendPerByCihaz = cihazIds.ToDictionary(id => id, _ => new List<decimal>(10));

        //        for (int i = 9; i >= 0; i--)
        //        {
        //            var dayStart = todayStart.AddDays(-i);
        //            var dayEnd = (i == 0) ? now : dayStart.AddDays(1);

        //            var dayDocs = await _logsRepository.GetFirstLastByCihazIdsAsync(cihazIds, dayStart, dayEnd, ct);
        //            var dayMap = CalcMap(dayDocs);

        //            foreach (var cihazId in cihazIds)
        //            {
        //                var dailyTotal = GetTotalFromMap(dayMap, cihazId);

        //                var pop = GetPop(cihazId, dayStart); // ✅ gün bazlı pop
        //                var dailyPer = (pop > 0) ? (dailyTotal * 1000 / pop) : 0m;

        //                trendTotalByCihaz[cihazId].Add(dailyTotal);
        //                trendPerByCihaz[cihazId].Add(dailyPer);
        //            }
        //        }

        //        // ---------------------------------------------------------
        //        // 5) Output paket
        //        // ---------------------------------------------------------
        //        var result = new List<EnergyFarmPacket>(farms.Count);

        //        foreach (var f in farms)
        //        {
        //            var todayTotal = GetTotalFromMap(todayMap, f.NodeId);
        //            var last10Total = GetTotalFromMap(last10Map, f.NodeId);

        //            var todayPop = TodayPop(f.NodeId);
        //            var todayPer = (todayPop > 0) ? (todayTotal / todayPop) : 0m;

        //            // total10Per için iki seçenek:
        //            // A) Ortalama pop ile böl (pratik)
        //            var avgPop = AvgPop10(f.NodeId);
        //            var total10Per = (avgPop > 0) ? (last10Total / avgPop) : 0m;

        //            // B) Hen-days yaklaşımı (daha matematiksel):
        //            // var henDays = trendPerByCihaz[f.NodeId] ... değil!
        //            // doğru henDays: sum(popDay) -> total10 / (henDays/10) gibi bir yorum,
        //            // ama çoğu UI "ortalama hayvan" gibi davranmasını ister.
        //            // İstersen bunu da ekleyebilirsin.

        //            result.Add(new EnergyFarmPacket
        //            {
        //                id = f.Id,
        //                name = f.Name,
        //                energy = new EnergyWidgetDto
        //                {
        //                    today = todayTotal,
        //                    todayPer = todayPer * 1000,
        //                    total10 = last10Total,
        //                    total10Per = total10Per * 1000,
        //                    trendTotal = trendTotalByCihaz[f.NodeId],
        //                    trendPer = trendPerByCihaz[f.NodeId]
        //                }
        //            });
        //        }

        //        return result;
        //    }

        // cihazId -> (totalKwh, avgKw)
        private Dictionary<int, (decimal totalKwh, decimal avgKw)> CalcMap(List<BsonDocument> docs)
        {
            var map = new Dictionary<int, (decimal totalKwh, decimal avgKw)>();

            foreach (var d in docs)
            {
                var cihazId = d.GetValue("cihazId").ToInt32();

                if (d.Contains("veri_bulunamadi"))
                {
                    map[cihazId] = (0m, 0m);
                    continue;
                }

                var importWh = d.GetValue("import_fark", 0).ToDecimal();
                var dakika = (decimal)d.GetValue("dakika_farki", 0).ToDouble();

                if (dakika <= 0)
                {
                    map[cihazId] = (0m, 0m);
                    continue;
                }

                if (importWh < 0) importWh = 0; // sayaç reset negatif koruma

                var hours = dakika / 60m;
                var avgW = importWh / hours; // Wh/h = W

                // kWh / kW
                map[cihazId] = (importWh / 1000m, avgW / 1000m);
            }

            return map;
        }
        public async Task<List<EnerjiKumesTesisIzlemeModel>> GetEnerjiKumesTesisIzlemeModelByCihazId(int cihazId, CancellationToken cancellationToken)
        {
            // 1. Tüketim verilerini getir
            var analizor = await _logsRepository.GetTuketimVerileriByCihazId(cihazId, cancellationToken);

            // 2. Cihaz bilgisini getir (tek cihaz lazım)
            var cihaz = await _cihazRepository.GetByIdAsync(cihazId);
            if (cihaz == null) return new List<EnerjiKumesTesisIzlemeModel>();

            // 3. Sonuçları dönüştür
            //var tuketimler = analizor.Select(doc => new EnerjiKumesTesisIzlemeModel
            //{
            //    CihazAd = cihaz.Name, // doğrudan cihaz objesinden
            //    Yil = doc["yil"].AsInt32,
            //    Ay = doc["ay"].AsInt32,
            //    Tuketim = (decimal)doc["tuketim"].ToDecimal()
            //}).ToList();

            return new List<EnerjiKumesTesisIzlemeModel>();
        }

        public static List<SahaVerileriTabloRaporModel> AnalizorDegerlerToSahaVerileriTabloRapor(veri analizor, DateTime okunmaZamani, int durum, CancellationToken cancellationToken)
        {
            var list = new List<SahaVerileriTabloRaporModel>();

            list.Add(new SahaVerileriTabloRaporModel
            {
                Ozellik = "Gerilim (V)",
                L1 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.L1_V),
                L2 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.L2_V),
                L3 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.L3_V),
                Toplam = null,
                Notr = null,
                OkunmaZamani = okunmaZamani,
                Durum = durum,
            });

            list.Add(new SahaVerileriTabloRaporModel
            {
                Ozellik = "Akım (A)",
                L1 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.L1_A),
                L2 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.L2_A),
                L3 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.L3_A),
                Toplam = null,
                Notr = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.Toplam_Akim_A),
                OkunmaZamani = okunmaZamani,
                Durum = durum,
            });

            list.Add(new SahaVerileriTabloRaporModel
            {
                Ozellik = "Import Aktif Enerji (kWh)",
                L1 = null,
                L2 = null,
                L3 = null,
                Toplam = string.Format(new CultureInfo("tr-TR"), "{0:F2}", (decimal?)analizor.Import_Aktif_Enerji_Wh),
                Notr = null,
                OkunmaZamani = okunmaZamani,
                Durum = durum,
            });

            list.Add(new SahaVerileriTabloRaporModel
            {
                Ozellik = "Export Aktif Enerji (kWh)",
                L1 = null,
                L2 = null,
                L3 = null,
                Toplam = string.Format(new CultureInfo("tr-TR"), "{0:F2}", (decimal?)analizor.Export_Aktif_Enerji_Wh),
                Notr = null,
                OkunmaZamani = okunmaZamani,
                Durum = durum,
            });

            list.Add(new SahaVerileriTabloRaporModel
            {
                Ozellik = "Aktif Güç (W)",
                L1 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.AktifGuc_L1_W),
                L2 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.AktifGuc_L2_W),
                L3 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.AktifGuc_L3_W),
                Toplam = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.ToplamAktifGuc_W),
                Notr = null,
                OkunmaZamani = okunmaZamani,
                Durum = durum,
            });

            list.Add(new SahaVerileriTabloRaporModel
            {
                Ozellik = "Reaktif Güç (VAr)",
                L1 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.ReaktifGuc_L1_VAr),
                L2 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.ReaktifGuc_L2_VAr),
                L3 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.ReaktifGuc_L3_VAr),
                Toplam = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.ToplamReaktifGuc_VAr),
                Notr = null,
                OkunmaZamani = okunmaZamani,
                Durum = durum,
            });

            list.Add(new SahaVerileriTabloRaporModel
            {
                Ozellik = "Görünür Güç (VA)",
                L1 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.GorunurGuc_L1_VA),
                L2 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.GorunurGuc_L2_VA),
                L3 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.GorunurGuc_L3_VA),
                Toplam = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.ToplamGorunurGuc_VA),
                Notr = null,
                OkunmaZamani = okunmaZamani,
                Durum = durum,
            });

            list.Add(new SahaVerileriTabloRaporModel
            {
                Ozellik = "Güç Faktörü",
                L1 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.GucFaktoru_L1),
                L2 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.GucFaktoru_L2),
                L3 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.GucFaktoru_L3),
                Toplam = null,
                Notr = null,
                OkunmaZamani = okunmaZamani,
                Durum = durum,
            });

            list.Add(new SahaVerileriTabloRaporModel
            {
                Ozellik = "Cos φ",
                L1 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.Cos_L1),
                L2 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.Cos_L2),
                L3 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.Cos_L3),
                Toplam = null,
                Notr = null,
                OkunmaZamani = okunmaZamani,
                Durum = durum,
            });

            list.Add(new SahaVerileriTabloRaporModel
            {
                Ozellik = "Frekans (Hz)",
                L1 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.Frekans_Hz),
                L2 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.Frekans_Hz),
                L3 = string.Format(new CultureInfo("tr-TR"), "{0:F2}", analizor.Frekans_Hz),
                Toplam = null,
                Notr = null,
                OkunmaZamani = okunmaZamani,
                Durum = durum,
            });

            return list;
        }

        public static List<SahaVerileriTabloRaporModel> AnalizorDegerlerToSahaVerileriTabloRaporByNodeType(veri analizor, DateTime okunmaZamani, int durum, string nodeType, CancellationToken cancellationToken)
        {
            var list = new List<SahaVerileriTabloRaporModel>();
            if (nodeType == TreeViewNodeTypes.Cihaz)
            {
                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Gerilim (V)",
                    L1 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.L1_V),
                    L2 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.L2_V),
                    L3 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.L3_V),
                    Toplam = null,
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Akım (A)",
                    L1 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.L1_A),
                    L2 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.L2_A),
                    L3 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.L3_A),
                    Toplam = null,
                    Notr = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.Toplam_Akim_A),
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Import Aktif Enerji (kWh)",
                    L1 = null,
                    L2 = null,
                    L3 = null,
                    Toplam = string.Format(new CultureInfo("tr-TR"), "{0:N2}", (decimal?)analizor.Import_Aktif_Enerji_Wh.GetValueOrDefault(0) / 1000),
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Export Aktif Enerji (kWh)",
                    L1 = null,
                    L2 = null,
                    L3 = null,
                    Toplam = string.Format(new CultureInfo("tr-TR"), "{0:N2}", (decimal?)analizor.Export_Aktif_Enerji_Wh.GetValueOrDefault(0) / 1000),
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Aktif Güç (kW)",
                    L1 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.AktifGuc_L1_W.GetValueOrDefault(0) / 1000),
                    L2 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.AktifGuc_L2_W.GetValueOrDefault(0) / 1000),
                    L3 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.AktifGuc_L3_W.GetValueOrDefault(0) / 1000),
                    Toplam = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.ToplamAktifGuc_W.GetValueOrDefault(0) / 1000),
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Reaktif Güç (VAr)",
                    L1 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.ReaktifGuc_L1_VAr),
                    L2 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.ReaktifGuc_L2_VAr),
                    L3 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.ReaktifGuc_L3_VAr),
                    Toplam = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.ToplamReaktifGuc_VAr),
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Görünür Güç (VA)",
                    L1 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.GorunurGuc_L1_VA),
                    L2 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.GorunurGuc_L2_VA),
                    L3 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.GorunurGuc_L3_VA),
                    Toplam = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.ToplamGorunurGuc_VA),
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Güç Faktörü",
                    L1 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.GucFaktoru_L1),
                    L2 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.GucFaktoru_L2),
                    L3 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.GucFaktoru_L3),
                    Toplam = null,
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Cos φ",
                    L1 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.Cos_L1),
                    L2 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.Cos_L2),
                    L3 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.Cos_L3),
                    Toplam = null,
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Frekans (Hz)",
                    L1 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.Frekans_Hz),
                    L2 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.Frekans_Hz),
                    L3 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.Frekans_Hz),
                    Toplam = null,
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });
            }
            else
            {
                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Gerilim (V)",
                    L1 = null,
                    L2 = null,
                    L3 = null,
                    Toplam = null,
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Akım (A)",
                    L1 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.L1_A),
                    L2 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.L2_A),
                    L3 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.L3_A),
                    Toplam = null,
                    Notr = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.Toplam_Akim_A),
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Import Aktif Enerji (kWh)",
                    L1 = null,
                    L2 = null,
                    L3 = null,
                    Toplam = string.Format(new CultureInfo("tr-TR"), "{0:N2}", (decimal?)analizor.Import_Aktif_Enerji_Wh.GetValueOrDefault(0) / 1000),
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Export Aktif Enerji (kWh)",
                    L1 = null,
                    L2 = null,
                    L3 = null,
                    Toplam = string.Format(new CultureInfo("tr-TR"), "{0:N2}", (decimal?)analizor.Export_Aktif_Enerji_Wh.GetValueOrDefault(0) / 1000),
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Aktif Güç (kW)",
                    L1 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.AktifGuc_L1_W.GetValueOrDefault(0) / 1000),
                    L2 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.AktifGuc_L2_W.GetValueOrDefault(0) / 1000),
                    L3 = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.AktifGuc_L3_W.GetValueOrDefault(0) / 1000),
                    Toplam = string.Format(new CultureInfo("tr-TR"), "{0:N2}", analizor.ToplamAktifGuc_W.GetValueOrDefault(0) / 1000),
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Reaktif Güç (VAr)",
                    L1 = null,
                    L2 = null,
                    L3 = null,
                    Toplam = null,
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Görünür Güç (VA)",
                    L1 = null,
                    L2 = null,
                    L3 = null,
                    Toplam = null,
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Güç Faktörü",
                    L1 = null,
                    L2 = null,
                    L3 = null,
                    Toplam = null,
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Cos φ",
                    L1 = null,
                    L2 = null,
                    L3 = null,
                    Toplam = null,
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });

                list.Add(new SahaVerileriTabloRaporModel
                {
                    Ozellik = "Frekans (Hz)",
                    L1 = null,
                    L2 = null,
                    L3 = null,
                    Toplam = null,
                    Notr = null,
                    OkunmaZamani = okunmaZamani,
                    Durum = durum,
                });
            }
            return list;
        }

        public static List<TuketimEnerjiVerileriTabloRaporModel> AnalizorDegerlerToTuketimEnerjiVerileriTabloRapor(decimal? importAktifEnerji, decimal? importOrtalamaEnerji, decimal? exportAktifEnerji, decimal? exportOrtalamaEnerji, DateTime? ilkOkumaZaman, DateTime? sonOkumaZaman, CancellationToken cancellationToken)
        {
            var list = new List<TuketimEnerjiVerileriTabloRaporModel>();

            list.Add(new TuketimEnerjiVerileriTabloRaporModel
            {
                Ozellik = "Import (kWh)",
                AktifEnerji = string.Format(new CultureInfo("tr-TR"), "{0:N2}", importAktifEnerji),
                OrtalamaEnerji = string.Format(new CultureInfo("tr-TR"), "{0:N2}", importOrtalamaEnerji),
                IlkOkumaZaman = ilkOkumaZaman,
                SonOkumaZaman = sonOkumaZaman,
            });

            list.Add(new TuketimEnerjiVerileriTabloRaporModel
            {
                Ozellik = "Export (kWh)",
                AktifEnerji = string.Format(new CultureInfo("tr-TR"), "{0:N2}", exportAktifEnerji),
                OrtalamaEnerji = string.Format(new CultureInfo("tr-TR"), "{0:N2}", exportOrtalamaEnerji),
                IlkOkumaZaman = ilkOkumaZaman,
                SonOkumaZaman = sonOkumaZaman,
            });

            return list;
        }
        public async Task<TreeModel> GetNodeTreeViewByNodeIdAndType(int nodeId, string nodeType, CancellationToken cancellationToken)
        {
            TreeModel cihazNode = null;
            TreeModel makineNode = null;
            TreeModel bolumNode = null;
            TreeModel lokasyonNode = null;

            switch (nodeType)
            {
                case TreeViewNodeTypes.Cihaz:
                    var cihaz = await _cihazRepository.GetByIdAsync(nodeId);
                    if (cihaz == null) return null;
                    var parentCihaz = await _cihazRepository.GetAsyncByPredicate(x => x.Id == cihaz.ParentCihazId);
                    var childCihazlar = await _cihazRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ParentCihazId == cihaz.Id);

                    cihazNode = new TreeModel
                    {
                        Id = cihaz.Id,
                        Name = cihaz.Name ?? "Cihaz",
                        Type = TreeViewNodeTypes.Cihaz,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = null,
                        ParentCihaz = parentCihaz == null ? null : parentCihaz.Id,
                        ChildrenCihazlar = childCihazlar.Count > 0 ? childCihazlar.Select(x => x.Id).ToList() : null,
                    };

                    var makineFromCihaz = await _makineRepository.GetByIdAsync(cihaz.MakineId);
                    if (makineFromCihaz == null) return null;

                    makineNode = new TreeModel
                    {
                        Id = makineFromCihaz.Id,
                        Name = makineFromCihaz.Name ?? "Makine",
                        Type = TreeViewNodeTypes.Makine,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = new List<TreeModel> { cihazNode }
                    };

                    var bolumFromMakine1 = await _bolumRepository.GetByIdAsync(makineFromCihaz.BolumId);
                    if (bolumFromMakine1 == null) return null;

                    bolumNode = new TreeModel
                    {
                        Id = bolumFromMakine1.Id,
                        Name = bolumFromMakine1.Name ?? "Bölüm",
                        Type = TreeViewNodeTypes.Bolum,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = new List<TreeModel> { makineNode }
                    };

                    return bolumNode;

                case TreeViewNodeTypes.Makine:
                    var makine = await _makineRepository.GetByIdAsync(nodeId);
                    if (makine == null) return null;

                    makineNode = new TreeModel
                    {
                        Id = makine.Id,
                        Name = makine.Name ?? "Makine",
                        Type = TreeViewNodeTypes.Makine,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = null // İstersen buraya cihazları da çekip koyabilirsin
                    };

                    var bolumFromMakine2 = await _bolumRepository.GetByIdAsync(makine.BolumId);
                    if (bolumFromMakine2 == null) return null;

                    bolumNode = new TreeModel
                    {
                        Id = bolumFromMakine2.Id,
                        Name = bolumFromMakine2.Name ?? "Bölüm",
                        Type = TreeViewNodeTypes.Bolum,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = new List<TreeModel> { makineNode }
                    };

                    return bolumNode;

                case TreeViewNodeTypes.Bolum:
                    var bolum = await _bolumRepository.GetByIdAsync(nodeId);
                    if (bolum == null) return null;

                    bolumNode = new TreeModel
                    {
                        Id = bolum.Id,
                        Name = bolum.Name ?? "Bölüm",
                        Type = TreeViewNodeTypes.Bolum,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = null
                    };

                    return bolumNode;

                case TreeViewNodeTypes.Lokasyon:
                    var lokasyon = await GetLokasyonByIdWithApi(nodeId, cancellationToken);
                    if (lokasyon == null) return null;

                    lokasyonNode = new TreeModel
                    {
                        Id = lokasyon.Id,
                        Name = lokasyon.Name ?? "Lokasyon",
                        Type = TreeViewNodeTypes.Lokasyon,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = null
                    };

                    return lokasyonNode;

                default:
                    return null;
            }
        }
        public async Task<TreeModel> GetNodeTreeViewByNodeIdAndType2(int nodeId, string nodeType, CancellationToken cancellationToken)
        {
            TreeModel cihazNode = null;
            TreeModel makineNode = null;
            TreeModel bolumNode = null;
            TreeModel lokasyonNode = null;

            switch (nodeType)
            {
                case TreeViewNodeTypes.Cihaz:
                    var cihaz = await _cihazRepository.GetByIdAsync(nodeId);
                    if (cihaz == null) return null;

                    cihazNode = new TreeModel
                    {
                        Id = cihaz.Id,
                        Name = cihaz.Name ?? "Cihaz",
                        Type = TreeViewNodeTypes.Cihaz,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = null
                    };

                    var makineFromCihaz = await _makineRepository.GetByIdAsync(cihaz.MakineId);
                    if (makineFromCihaz == null) return null;

                    makineNode = new TreeModel
                    {
                        Id = makineFromCihaz.Id,
                        Name = makineFromCihaz.Name ?? "Makine",
                        Type = TreeViewNodeTypes.Makine,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = new List<TreeModel> { cihazNode }
                    };

                    var bolumFromMakine1 = await _bolumRepository.GetByIdAsync(makineFromCihaz.BolumId);
                    if (bolumFromMakine1 == null) return null;

                    bolumNode = new TreeModel
                    {
                        Id = bolumFromMakine1.Id,
                        Name = bolumFromMakine1.Name ?? "Bölüm",
                        Type = TreeViewNodeTypes.Bolum,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = new List<TreeModel> { makineNode }
                    };

                    return bolumNode;

                case TreeViewNodeTypes.Makine:
                    var makine = await _makineRepository.GetByIdAsync(nodeId);
                    if (makine == null) return null;

                    makineNode = new TreeModel
                    {
                        Id = makine.Id,
                        Name = makine.Name ?? "Makine",
                        Type = TreeViewNodeTypes.Makine,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = null // İstersen buraya cihazları da çekip koyabilirsin
                    };

                    var bolumFromMakine2 = await _bolumRepository.GetByIdAsync(makine.BolumId);
                    if (bolumFromMakine2 == null) return null;

                    bolumNode = new TreeModel
                    {
                        Id = bolumFromMakine2.Id,
                        Name = bolumFromMakine2.Name ?? "Bölüm",
                        Type = TreeViewNodeTypes.Bolum,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = new List<TreeModel> { makineNode }
                    };

                    return bolumNode;

                case TreeViewNodeTypes.Bolum:
                    var bolum = await _bolumRepository.GetByIdAsync(nodeId);
                    if (bolum == null) return null;

                    bolumNode = new TreeModel
                    {
                        Id = bolum.Id,
                        Name = bolum.Name ?? "Bölüm",
                        Type = TreeViewNodeTypes.Bolum,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = null
                    };

                    return bolumNode;

                case TreeViewNodeTypes.Lokasyon:
                    var lokasyon = await GetLokasyonByIdWithApi(nodeId, cancellationToken);
                    if (lokasyon == null) return null;

                    lokasyonNode = new TreeModel
                    {
                        Id = lokasyon.Id,
                        Name = lokasyon.Name ?? "Lokasyon",
                        Type = TreeViewNodeTypes.Lokasyon,
                        Checked = false,
                        Expanded = true,
                        Write = false,
                        Read = false,
                        Children = null
                    };

                    return lokasyonNode;

                default:
                    return null;
            }
        }

        public async Task<string> GetTGTFromEPIAS(CancellationToken cancellationToken)
        {
            using (HttpClient client = new HttpClient())
            {
                // İstek başlıkları
                client.DefaultRequestHeaders.Add("Accept", "text/plain");
                string url = "https://giris.epias.com.tr/cas/v1/tickets";

                // Form verisi (Body)
                var formData = new Dictionary<string, string> { { "username", "beray.bayazit@matli.com.tr" }, { "password", "Bb.300521" } };
                var content = new FormUrlEncodedContent(formData);
                try
                {
                    HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        // Gelen cevabı string olarak al
                        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        //Console.WriteLine("Gelen Veri: " + responseBody);
                        return responseBody;
                    }
                    else
                    {
                        string errorMessage = $"Hata: {response.StatusCode} - {response.ReasonPhrase}";
                        //Console.WriteLine(errorMessage);
                        return null;
                    }
                }
                catch (Exception)
                {
                    //Console.WriteLine("Bir hata oluştu: " + ex.Message);
                    return null;
                }
            }
        }

        public async Task<List<PtfItem>> GetPtfValues(DateTime startDate, DateTime endDate, string tgtToken, CancellationToken cancellationToken)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("TGT", tgtToken);

                    var body = new
                    {
                        startDate = startDate.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                        endDate = endDate.ToString("yyyy-MM-ddTHH:mm:sszzz")
                    };

                    var response = await client.PostAsJsonAsync(
                        "https://seffaflik.epias.com.tr/electricity-service/v1/markets/dam/data/mcp",
                        body, cancellationToken);

                    response.EnsureSuccessStatusCode();

                    var result = await response.Content.ReadFromJsonAsync<PtfResponse>(cancellationToken);

                    return result?.Items ?? new List<PtfItem>();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public byte[] TamRaporOlustur(TamRaporRequest data)
        {
            return _logsRepository.TamRaporOlustur(data);
        }

        //Eski Metotlar
        public async Task AddEnerjiMaliyetParametreler(List<EnerjiMaliyetParametrelerViewModel> enerjiMaliyetParametreler, int seciliYil, int lokasyonId, string tarifeTipi, CancellationToken cancellationToken)
        {
            var enerjiMaliyetParametreler2 = _mapper.Map<List<EnerjiMaliyetParametreler>>(enerjiMaliyetParametreler);
            await _treeRepository.AddEnerjiMaliyetParametreler(enerjiMaliyetParametreler2, seciliYil, lokasyonId, tarifeTipi);
        }

        //Yeni Metotlar
        public async Task<List<EnerjiMaliyetParametreler>> GetEnerjiMaliyetParametreler(int seciliYil, int lokasyonId, CancellationToken cancellationToken)
        {
            return await _treeRepository.GetEnerjiMaliyetParametreler(seciliYil, lokasyonId);
        }

        public async Task<List<EnerjiMaliyetGrup>> GetEnerjiMaliyetGruplar()
        {
            return await _treeRepository.GetEnerjiMaliyetGruplar();
        }

        public async Task<EnerjiMaliyetGrup> AddEnerjiMaliyetGrup(string name)
        {
            return await _treeRepository.AddEnerjiMaliyetGrup(name);
        }

        public async Task<EnerjiMaliyetGrup> UpdateEnerjiMaliyetGrup(int id, string name)
        {
            return await _treeRepository.UpdateEnerjiMaliyetGrup(id, name);
        }

        public async Task DeleteEnerjiMaliyetGrup(int id)
        {
            await _treeRepository.DeleteEnerjiMaliyetGrup(id);
        }

        public async Task<List<EnerjiMaliyetParametreler>> GetEnerjiMaliyetParametreler(int seciliYil, int enerjiMaliyetGrupId)
        {
            return await _treeRepository.GetEnerjiMaliyetParametreler(seciliYil, enerjiMaliyetGrupId);
        }

        public async Task AddEnerjiMaliyetParametreler(List<EnerjiMaliyetParametreler> enerjiMaliyetParametreler, int seciliYil, int enerjiMaliyetGrupId, string tarifeTipi)
        {
            await _treeRepository.AddEnerjiMaliyetParametreler(enerjiMaliyetParametreler, seciliYil, enerjiMaliyetGrupId, tarifeTipi);
        }

        public async Task<EnerjiMaliyetAtamaTreeNodeDto?> GetEnerjiMaliyetAtamaTreeByLokasyonId(int lokasyonId, CancellationToken cancellationToken)
        {
            return await _treeRepository.GetEnerjiMaliyetAtamaTreeByLokasyonId(lokasyonId, cancellationToken);
        }

        public async Task AddEnerjiMaliyetParametreAtama(int enerjiMaliyetGrupId, int nodeId, string nodeType)
        {
            await _treeRepository.AddEnerjiMaliyetParametreAtama(enerjiMaliyetGrupId, nodeId, nodeType);
        }

        public async Task RemoveEnerjiMaliyetParametreAtama(int nodeId, string nodeType)
        {
            await _treeRepository.RemoveEnerjiMaliyetParametreAtama(nodeId, nodeType);
        }

        public async Task<(decimal? ImportAktifEnerjiWh, decimal? ExportAktifEnerjiWh, DateTime? KayitTarihi)> GetAktifEnerjiSayacDegeri(int cihazId, DateTime tarih, CancellationToken ct)
        {
            var ilkKayit = await _logsRepository.GetIlkOkunanImportAktifEnerjiAsync(cihazId, tarih, ct);

            return (
                ImportAktifEnerjiWh: ilkKayit.IlkImportAktifEnerji,
                ExportAktifEnerjiWh: ilkKayit.IlkExportAktifEnerji,
                KayitTarihi: ilkKayit.IlkOkunmaZamani
            );
        }

        //public async Task<ToplamTuketimSonucu?> GetToplamImportAktifEnerjiTuketimiAsync(List<int> cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        //{
        //    var result = await _logsRepository.GetToplamImportAktifEnerjiTuketimiAsync(cihazNodeIds, baslangicZamani, bitisZamani, cancellationToken);

        //    return result;
        //}
        public async Task<ToplamTuketimSonucu?> GetToplamImportAktifEnerjiTuketimi(List<int> cihazIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            if (cihazIds == null || !cihazIds.Any())
            {
                return new ToplamTuketimSonucu();
            }

            if (baslangicZamani > bitisZamani)
            {
                throw new ArgumentException("Başlangıç zamanı bitiş zamanından büyük olamaz.");
            }

            /*
             * MongoDB'deki kayıtlar Türkiye saatini gösteriyor,
             * ancak +00:00 olarak işaretlenmiş.
             *
             * Bu nedenle saat değerini değiştirmeden
             * UTC olarak sorguluyoruz.
             */
            var sorguBaslangic = DateTime.SpecifyKind(                baslangicZamani,                DateTimeKind.Utc);
            var sorguBitis = DateTime.SpecifyKind(                bitisZamani,                DateTimeKind.Utc);

            /*
             * SQL tarafındaki cihazlardan hangilerinin GES
             * olduğunu öğreniyoruz.
             */
            var cihazlar = await _cihazRepository
                .GetAllAsyncByPredicateWithAsNoTracking(
                    x =>
                        cihazIds.Contains(x.Id) &&
                        x.Silindi != true);

            var cihazDictionary = cihazlar.ToDictionary(
                       x => x.Id,
                       x => new
                       {
                           GesMi = x.GES == true,
                           CihazName = x.Name
                       });

            /*
             * Her cihazı paralel hesaplıyoruz.
             */
            var tasks = cihazIds.Select(async cihazId =>
            {
                var cihazBilgisi = cihazDictionary.GetValueOrDefault(cihazId);

                var gesMi = cihazBilgisi?.GesMi ?? false;
                var cihazName = cihazBilgisi?.CihazName;

                if (gesMi)
                {
                    return await _logsRepository.GetGesCihazUretimiAsync(cihazId, cihazName, sorguBaslangic, sorguBitis, cancellationToken);
                }

                return await _logsRepository.GetNormalCihazTuketimiAsync(cihazId, cihazName, sorguBaslangic, sorguBitis, cancellationToken);
            });

            var sonucListesi = (await Task.WhenAll(tasks)).ToList();

            /*
             * GES ÜRETİM
             *
             * GES cihazlarının seçilen aralıktaki
             * Export değerleri toplamı.
             */
            var gesUretim = sonucListesi
                .Where(x =>
                    x.GesMi &&
                    x.GesExportToplami.HasValue)
                .Sum(x => x.GesExportToplami!.Value);


            /*
             * NORMAL IMPORT
             *
             * GES olmayan cihazların
             * Import sayaç farkları toplamı.
             */
            var normalImport = sonucListesi
                .Where(x =>
                    !x.GesMi &&
                    x.ImportTuketim.HasValue)
                .Sum(x => x.ImportTuketim!.Value);

            /*
             * NORMAL EXPORT
             *
             * GES olmayan cihazların
             * Export sayaç farkları toplamı.
             */
            var normalExport = sonucListesi
                .Where(x =>
                    !x.GesMi &&
                    x.ExportTuketim.HasValue)
                .Sum(x => x.ExportTuketim!.Value);

            /*
             * GES üretimin %97,5'i
             */
            var duzeltilmisGesUretim = gesUretim * 0.975m;

            /*
             * FORMÜL
             *
             * (GES Üretim × 0,975)
             * - Normal Export
             * + Normal Import
             */
            var toplamTuketim = duzeltilmisGesUretim - normalExport + normalImport;

            return new ToplamTuketimSonucu
            {
                CihazSonuclari = sonucListesi,

                GesUretim = gesUretim,

                DuzenlenmisGesUretim = duzeltilmisGesUretim,

                NormalImport = normalImport,

                NormalExport = normalExport,

                ToplamTuketim = toplamTuketim
            };
        }

        public async Task<List<TreeModel>> GetGesDahilTreeByLokasyonId(int lokasyonId, bool duzenlemeModu, CancellationToken cancellationToken)
        {
            var lokasyon = await _treeRepository.GetLokasyonById(lokasyonId);

            if (lokasyon == null)
            {
                throw new Exception("Lokasyon bulunamadı!");
            }

            return await BuildGesDahilTreeBatch(
                lokasyon.Id,
                lokasyon.Name,
                duzenlemeModu);
        }
        public async Task<List<TreeModel>> BuildGesDahilTreeBatch(int lokasyonId, string? lokasyonName, bool duzenlemeModu)
        {
            // BÖLÜMLER
            var bolumler = duzenlemeModu
                ? await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(
                    x =>
                        x.LokasyonId == lokasyonId &&
                        x.Silindi != true)
                : await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(
                    x =>
                        x.LokasyonId == lokasyonId &&
                        x.Silindi != true &&
                        (x.Aktif != false || x.GES == true));

            var bolumIds = bolumler
                .Select(x => x.Id)
                .ToHashSet();

            // Bölüm yoksa sadece lokasyon dön
            if (!bolumIds.Any())
            {
                return new List<TreeModel>
        {
            new()
            {
                Id = lokasyonId,
                Name = lokasyonName,
                Type = TreeViewNodeTypes.Lokasyon,
                Expanded = false,
                Checked = false,
                Write = false,
                Read = false,
                Approved = true,
                Children = new List<TreeModel>()
            }
        };
            }

            // MAKİNELER
            var makineler = duzenlemeModu
                ? await _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(
                    x =>
                        bolumIds.Contains(x.BolumId) &&
                        x.Silindi != true)
                : await _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(
                    x =>
                        bolumIds.Contains(x.BolumId) &&
                        x.Silindi != true &&
                        (x.Aktif != false || x.GES == true));

            var makineIds = makineler
                .Select(x => x.Id)
                .ToHashSet();

            // CİHAZLAR
            var cihazlar = makineIds.Any()
                ? duzenlemeModu
                    ? await _cihazRepository.GetAllAsyncByPredicateWithAsNoTracking(
                        x =>
                            makineIds.Contains(x.MakineId) &&
                            x.Silindi != true)
                    : await _cihazRepository.GetAllAsyncByPredicateWithAsNoTracking(
                        x =>
                            makineIds.Contains(x.MakineId) &&
                            x.Silindi != true &&
                            (x.Aktif != false || x.GES == true))
                : new List<Cihaz>();

            // LOOKUP'LAR
            var makineByBolum = makineler
                .GroupBy(x => x.BolumId)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList());

            var cihazByMakine = cihazlar
                .GroupBy(x => x.MakineId)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList());

            var childCihazByParent = cihazlar
                .Where(x => x.ParentCihazId > 0)
                .GroupBy(x => x.ParentCihazId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(c => c.Id).ToList());

            // TREE
            var lokasyonNode = new TreeModel
            {
                Id = lokasyonId,
                Name = lokasyonName,
                Type = TreeViewNodeTypes.Lokasyon,
                Expanded = false,
                Checked = false,
                Write = false,
                Read = false,
                Approved = true,

                Children = bolumler
                    .Select(bolum =>
                    {
                        var bolumNode = new TreeModel
                        {
                            Id = bolum.Id,
                            Name = bolum.Name ?? "Bölüm",
                            Type = TreeViewNodeTypes.Bolum,
                            Checked = false,
                            Approved = bolum.Onaylandi.GetValueOrDefault(false),
                            Expanded = false,
                            Write = false,
                            Read = false,

                            Children = makineByBolum
                                .GetValueOrDefault(bolum.Id, new List<Makine>())
                                .Select(makine =>
                                {
                                    var makineNode = new TreeModel
                                    {
                                        Id = makine.Id,
                                        Name = makine.Name,
                                        Type = TreeViewNodeTypes.Makine,
                                        Checked = false,
                                        Approved = makine.Onaylandi == true,
                                        Expanded = false,
                                        Write = false,
                                        Read = false,

                                        Children = cihazByMakine
                                            .GetValueOrDefault(makine.Id, new List<Cihaz>())
                                            .Select(cihaz => new TreeModel
                                            {
                                                Id = cihaz.Id,
                                                Name = cihaz.Name ?? "Cihaz",
                                                Type = TreeViewNodeTypes.Cihaz,
                                                Checked = false,
                                                Approved = cihaz.Onaylandi == true,
                                                Expanded = false,
                                                Write = false,
                                                Read = false,

                                                ParentCihaz = cihaz.ParentCihazId,

                                                ChildrenCihazlar = childCihazByParent
                                                    .GetValueOrDefault(cihaz.Id)
                                            })
                                            .ToList()
                                    };

                                    return makineNode;
                                })
                                .ToList()
                        };

                        return bolumNode;
                    })
                    .ToList()
            };

            return new List<TreeModel>
    {
        lokasyonNode
    };
        }
    }
}
