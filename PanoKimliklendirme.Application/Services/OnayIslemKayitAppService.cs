using MapsterMapper;
using PanoKimliklendirme.Application.Interfaces;
using PanoKimliklendirme.Application.Services.Core;
using PanoKimliklendirme.Application.ViewModels;
using PanoKimliklendirme.Domain.DbViewModels;
using PanoKimliklendirme.Domain.Interfaces;
using PanoKimliklendirme.Domain.Models;
using PanoKimliklendirme.Domain.Models.ReportModels;
using SharedApp.Domain.Models;
using SharedKernel;
using System.Text.Json;
using static SharedKernel.ApplicationConstants;

namespace PanoKimliklendirme.Application.Services
{
    public class OnayIslemKayitAppService : AppService<OnayIslemKayit, OnayIslemKayitViewModel>, IOnayIslemKayitAppService
    {
        private readonly IOnayIslemKayitRepository _onayIslemKayitRepository;
        private readonly IPanoRepository _panoRepository;
        private readonly IBolumRepository _bolumRepository;
        private readonly IMakineRepository _makineRepository;
        private readonly IPanoMakineEslemeRepository _panoMakineEslemeRepository;
        public OnayIslemKayitAppService(IMapper mapper, IOnayIslemKayitRepository onayIslemKayitRepository, IPanoRepository panoRepository, IBolumRepository bolumRepository, IMakineRepository makineRepository, IPanoMakineEslemeRepository panoMakineEslemeRepository) : base(mapper, onayIslemKayitRepository)
        {
            _onayIslemKayitRepository = onayIslemKayitRepository;
            _panoRepository = panoRepository;
            _bolumRepository = bolumRepository;
            _makineRepository = makineRepository;
            _panoMakineEslemeRepository = panoMakineEslemeRepository;
        }
        public async Task<List<OnayIslemKayit>> GetAllOnayIslemKayit()
        {
            var onayIslemKayitlar = await _onayIslemKayitRepository.GetAllListAsync();
            return onayIslemKayitlar;
        }

        public async Task<List<vwDistinctOnayIslemKayit>> GetAllvwDistinctOnayIslemKayit(string userId)
        {
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);
            var onayIslemKayitlar = await _onayIslemKayitRepository.GetAllvwDistinctOnayIslemKayit(nodeYetkiler, userId);
            return onayIslemKayitlar;
        }

        public async Task<List<string>> GetOnayIslemKayitFromTreeNodeId(int id, string nodeType)
        {
            return await _onayIslemKayitRepository.GetOnayIslemKayitFromTreeNodeId(id, nodeType);
        }

        public async Task<List<string>> GetOnayIslemKayitlarByIdForDosya(int onayIslemKayitId)
        {
            return await _onayIslemKayitRepository.GetOnayIslemKayitlarByIdForDosya(onayIslemKayitId);
        }

        public async Task<bool> IslemOnayla(int id, string userId)
        {
            return await _onayIslemKayitRepository.IslemOnayla(id, userId);
        }

        public async Task<bool> IslemReddet(int id, string userId)
        {
            return await _onayIslemKayitRepository.IslemReddet(id, userId);
        }
        public async Task DosyaIslemReddet(int islemId, string userId)
        {
            await _onayIslemKayitRepository.DosyaIslemReddet(islemId, userId);
        }
        public async Task DosyaIslemOnayla(int islemId, string userId)
        {
            await _onayIslemKayitRepository.DosyaIslemOnayla(islemId, userId);
        }

        public async Task CihazIslemReddet(int cihazId, string userId)
        {
            await _onayIslemKayitRepository.CihazIslemReddet(cihazId, userId);
        }
        public async Task CihazIslemOnayla(int cihazId, string userId)
        {
            await _onayIslemKayitRepository.CihazIslemOnayla(cihazId, userId);
        }

        public async Task<bool> OnayaGonderByNodeIdAndType(int nodeId, string nodeType, string nodeName, string userId)
        {
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);
            return await _onayIslemKayitRepository.OnayaGonderByNodeIdAndType(nodeYetkiler, nodeId, nodeType, nodeName, userId);
        }

        public async Task<List<NodeYetkiModel>> GetAllUserMakineYetkiByUserId(string userId)
        {
            List<NodeYetkiModel> treeModels = new List<NodeYetkiModel>();

            var userMakineYetkis = await GetAllUserMakineYetkiByUserIdWithApi(userId);
            var userMakineYetkiler = userMakineYetkis.Where(x => (x.Aktif ?? true) && x.Silindi != true).ToList();

            foreach (var userMakineYetki in userMakineYetkiler)
            {
                // Her iterasyonda yeni bir NodeYetkiModel nesnesi oluştur
                var nodeYetki = new NodeYetkiModel();

                if (userMakineYetki.MakineId > 0)
                {
                    treeModels.Add(new NodeYetkiModel
                    {
                        NodeId = userMakineYetki.MakineId,
                        NodeType = TreeViewNodeTypes.Makine,
                        Write = userMakineYetki.Write ?? false,
                        Read = userMakineYetki.Read ?? false,
                        Responsible = userMakineYetki.SorumluMu ?? false
                    });
                    treeModels.AddRange(await GetSubNodes(userMakineYetki.MakineId, TreeViewNodeTypes.Makine, userMakineYetki.Write ?? false, userMakineYetki.Read ?? false, userMakineYetki.SorumluMu ?? false));
                }
                else if (userMakineYetki.BolumId > 0)
                {
                    treeModels.Add(new NodeYetkiModel
                    {
                        NodeId = userMakineYetki.BolumId,
                        NodeType = TreeViewNodeTypes.Bolum,
                        Write = userMakineYetki.Write ?? false,
                        Read = userMakineYetki.Read ?? false,
                        Responsible = userMakineYetki.SorumluMu ?? false
                    });
                    treeModels.AddRange(await GetSubNodes(userMakineYetki.BolumId, TreeViewNodeTypes.Bolum, userMakineYetki.Write ?? false, userMakineYetki.Read ?? false, userMakineYetki.SorumluMu ?? false));
                }
                else if (userMakineYetki.LokasyonId > 0)
                {
                    treeModels.Add(new NodeYetkiModel
                    {
                        NodeId = userMakineYetki.LokasyonId,
                        NodeType = TreeViewNodeTypes.Lokasyon,
                        Write = userMakineYetki.Write ?? false,
                        Read = userMakineYetki.Read ?? false,
                        Responsible = userMakineYetki.SorumluMu ?? false
                    });
                    var bolumler = await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == userMakineYetki.LokasyonId && x.Silindi != true);
                    foreach (var bolum in bolumler)
                    {
                        treeModels.AddRange(await GetSubNodes(bolum.Id, TreeViewNodeTypes.Bolum, userMakineYetki.Write ?? false, userMakineYetki.Read ?? false, userMakineYetki.SorumluMu ?? false));
                    }
                }
                else if (userMakineYetki.SirketId > 0)
                {
                    treeModels.Add(new NodeYetkiModel
                    {
                        NodeId = userMakineYetki.SirketId,
                        NodeType = TreeViewNodeTypes.Sirket,
                        Write = userMakineYetki.Write ?? false,
                        Read = userMakineYetki.Read ?? false,
                        Responsible = userMakineYetki.SorumluMu ?? false
                    });
                    var sirketLokasyonlar = await GetAllLokasyonBySirketIdWithApi(userMakineYetki.SirketId);
                    foreach (var lokasyon in sirketLokasyonlar)
                    {
                        var bolumler = await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyon.Id && x.Silindi != true);
                        foreach (var bolum in bolumler)
                        {
                            treeModels.AddRange(await GetSubNodes(bolum.Id, TreeViewNodeTypes.Bolum, userMakineYetki.Write ?? false, userMakineYetki.Read ?? false, userMakineYetki.SorumluMu ?? false));
                        }
                    }
                }
            }

            treeModels = treeModels.GroupBy(x => new { x.NodeId, x.NodeType }).Select(g => new NodeYetkiModel
            {
                NodeId = g.Key.NodeId,
                NodeType = g.Key.NodeType,
                Write = g.Any(y => y.Write),
                Read = g.Any(y => y.Read),
                Responsible = g.Any(y => y.Responsible)
            }).ToList();

            return treeModels;
        }

        public async Task<List<NodeYetkiModel>> GetSubNodes(int nodeId, string nodeType, bool write, bool read, bool responsible)
        {
            try
            {
                var subNodes = new List<NodeYetkiModel>();

                if (nodeType == TreeViewNodeTypes.Bolum)
                {
                    subNodes.Add(new NodeYetkiModel
                    {
                        NodeId = nodeId,
                        NodeType = TreeViewNodeTypes.Bolum,
                        Write = write,
                        Read = responsible,
                        Responsible = responsible
                    });
                    // Bölüm altındaki bölümleri al
                    var subBolumler = await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ParentBolumId == nodeId && x.Silindi != true);
                    foreach (var subBolum in subBolumler)
                    {
                        subNodes.Add(new NodeYetkiModel
                        {
                            NodeId = subBolum.Id,
                            NodeType = TreeViewNodeTypes.Bolum,
                            Write = write,
                            Read = read,
                            Responsible = responsible,
                        });

                        // Rekürsif olarak alt düğümleri getir
                        var childNodes = await GetSubNodes(subBolum.Id, TreeViewNodeTypes.Bolum, write, read, responsible);
                        //if (childNodes.Any())
                        //{
                        //    subNodes.Last().Children = childNodes;
                        //}
                        subNodes.AddRange(childNodes);
                    }

                    // Bölüm altındaki makineleri al
                    var makineler = await _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.BolumId == nodeId && x.ParentMakineId == 0 && x.Silindi != true);
                    foreach (var makine in makineler)
                    {
                        subNodes.Add(new NodeYetkiModel
                        {
                            NodeId = makine.Id,
                            NodeType = TreeViewNodeTypes.Makine,
                            Write = write,
                            Read = read,
                            Responsible = responsible,
                        });

                        // Rekürsif olarak alt düğümleri getir
                        var childNodes = await GetSubNodes(makine.Id, TreeViewNodeTypes.Makine, write, read, responsible);
                        //if (childNodes.Any())
                        //{
                        //    subNodes.Last().Children = childNodes;
                        //}
                        subNodes.AddRange(childNodes);
                    }
                }
                else if (nodeType == TreeViewNodeTypes.Makine)
                {
                    subNodes.Add(new NodeYetkiModel
                    {
                        NodeId = nodeId,
                        NodeType = TreeViewNodeTypes.Makine,
                        Write = write,
                        Read = read,
                        Responsible = responsible,
                    });
                    // Makine altındaki panoları al
                    var panoMakineEslemes = await _panoMakineEslemeRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.MakineId == nodeId && x.Silindi != true);
                    var panolar = await _panoRepository.GetAllAsyncByPredicateWithAsNoTracking(x => panoMakineEslemes.Select(pme => pme.PanoId).Contains(x.Id) && x.ParentPanoId == 0 && x.Silindi != true);

                    foreach (var pano in panolar)
                    {
                        subNodes.Add(new NodeYetkiModel
                        {
                            NodeId = pano.Id,
                            NodeType = TreeViewNodeTypes.Pano,
                            Write = write,
                            Read = read,
                            Responsible = responsible,
                        });

                        // Rekürsif olarak alt düğümleri getir
                        var childNodes = await GetSubNodes(pano.Id, TreeViewNodeTypes.Pano, write, read, responsible);
                        //if (childNodes.Any())
                        //{
                        //    subNodes.Last().Children = childNodes;
                        //}
                        subNodes.AddRange(childNodes);
                    }
                }
                else if (nodeType == TreeViewNodeTypes.Pano)
                {
                    // Pano altındaki panoları al
                    var subPanolar = await _panoRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ParentPanoId == nodeId && x.Silindi != true);

                    foreach (var subPano in subPanolar)
                    {
                        subNodes.Add(new NodeYetkiModel
                        {
                            NodeId = subPano.Id,
                            NodeType = TreeViewNodeTypes.Pano,
                            Write = write,
                            Read = read,
                            Responsible = responsible,
                        });
                        // Rekürsif olarak alt düğümleri getir
                        var childNodes = await GetSubNodes(subPano.Id, TreeViewNodeTypes.Pano, write, read, responsible);
                        subNodes.AddRange(childNodes);
                    }
                }
                return subNodes;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<UserMakineYetki?>> GetAllUserMakineYetkiByUserIdWithApi(string userId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(ApiUrls.SharedAppBaseUrl + "/UserMakineYetki/GetAllUserMakineYetkiByUserId")
                    };

                    // Parametreleri Query String'e eklemek için
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["userId"] = userId;
                    uriBuilder.Query = query.ToString();
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var userMakineYetkis = JsonSerializer.Deserialize<List<UserMakineYetki>>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return userMakineYetkis;
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

        public async Task<List<Lokasyon?>> GetAllLokasyonBySirketIdWithApi(int sirketId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(ApiUrls.SharedAppBaseUrl + "/Lokasyon/GetAllLokasyonBySirketId")
                    };

                    // Parametreleri Query String'e eklemek için
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["sirketId"] = sirketId.ToString();
                    uriBuilder.Query = query.ToString();
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var lokasyon = JsonSerializer.Deserialize<List<Lokasyon?>>(result, new JsonSerializerOptions
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
    }
}