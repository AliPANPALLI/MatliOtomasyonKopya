using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PanelIdentification.Application.Interfaces;
using PanelIdentification.Application.ViewModels;
using PanelIdentification.Application.ViewModels.ReportModels;
using PanelIdentification.Domain.Interfaces;
using PanelIdentification.Domain.Models;
using PanelIdentification.Domain.Models.ReportModels;
using SharedApp.Domain.Models;
using System.Text.Json;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;
using Bolum = PanelIdentification.Domain.Models.Bolum;
using JsonSerializer = System.Text.Json.JsonSerializer;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

namespace PanelIdentification.Application.Services
{
    public class TreeAppService : ITreeAppService
    {
        private readonly ITreeRepository _treeRepository;
        private readonly ICihazRepository _cihazRepository;
        private readonly IPanoRepository _panoRepository;
        private readonly IMakineRepository _makineRepository;
        private readonly IBolumRepository _bolumRepository;
        private readonly IPanoMakineEslemeRepository _panoMakineEslemeRepository;
        private readonly IOnayIslemKayitRepository _onayIslemKayitRepository;
        private readonly ILokasyonDosyaYoluEslemeRepository _lokasyonDosyaYoluEslemeRepository;

        public TreeAppService(ITreeRepository treeRepository,
            ICihazRepository cihazRepository,
            IPanoRepository panoRepository,
            IMakineRepository makineRepository,
            IBolumRepository bolumRepository,
            IPanoMakineEslemeRepository panoMakineEslemeRepository,
            IOnayIslemKayitRepository onayIslemKayitRepository,
            ILokasyonDosyaYoluEslemeRepository lokasyonDosyaYoluEslemeRepository)
        {
            _treeRepository = treeRepository;
            _cihazRepository = cihazRepository;
            _panoRepository = panoRepository;
            _makineRepository = makineRepository;
            _bolumRepository = bolumRepository;
            _panoMakineEslemeRepository = panoMakineEslemeRepository;
            _onayIslemKayitRepository = onayIslemKayitRepository;
            _lokasyonDosyaYoluEslemeRepository = lokasyonDosyaYoluEslemeRepository;
        }

        public async Task<List<TreeModel>> GetTreeByLokasyonId(string userId, int lokasyonId, bool onayliIslemlerDahil)
        {
            var lokasyon = await GetLokasyonByIdWithApi(lokasyonId);
            if (lokasyon == null)
            {
                throw new Exception("Lokasyon bulunamadı!");
            }
            //var userYetki = await GetTreeYetki(userId);
            //return await _treeRepository.GetTreeByLokasyonId(userId, lokasyon, userYetki, onayliIslemlerDahil);
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);
            return await _treeRepository.GetTreeByLokasyonId(userId, lokasyon, nodeYetkiler, onayliIslemlerDahil);
        }
        public async Task<Tuple<List<TreeModel>, TreeModel, int>> GetTreeByPanoKimlikNo2(string userId, string panoKimlikNo, bool onayliIslemlerDahil = false)
        {
            //return await _treeRepository.GetTreeByPanoKimlikNo(userId, panoKimlikNo);
            List<TreeModel> tree = new List<TreeModel>();

            var pano = await _panoRepository.GetAsyncByPredicate(x => EF.Functions.Like(x.PanoKimlikNo, panoKimlikNo) && x.Silindi != true);
            if (pano == null)
            {
                throw new Exception("Pano bulunamadı!");
            }
            //var lokasyon = await GetLokasyonByPanoId(pano.Id);
            //if (lokasyon == null)
            //{
            //    throw new Exception("Lokasyon bulunamadı!");
            //}
            //tree.AddRange(await GetTreeByLokasyonId(userId, lokasyon.Id, onayliIslemlerDahil));
            var lokasyonId = await _panoRepository.FindLokasyonIdByPanoId(pano.Id);
            tree.AddRange(await GetTreeByLokasyonId(userId, lokasyonId, onayliIslemlerDahil));

            //var foundNodes = await FindNodeByName(tree, pano.Name);
            var foundNodes = await FindNodeById(tree, pano.Id);

            List<TreeModel> parents = new List<TreeModel>();
            // Her bir bulunan node için parent'larını bulma
            foreach (TreeModel node in foundNodes)
            {
                FindParentsRecursive(tree, node, parents);
                node.Expanded = true;
            }
            // Bulunan parent'ları genisletme
            foreach (TreeModel parent in parents)
            {
                parent.Expanded = true;
            }
            return Tuple.Create(tree, foundNodes.First(), lokasyonId);
        }
        public async Task<Tuple<List<TreeModel>, TreeModel>> GetTreeByPanoKimlikNo(string userId, string panoKimlikNo, bool onayliIslemlerDahil = false)
        {
            //return await _treeRepository.GetTreeByPanoKimlikNo(userId, panoKimlikNo);
            List<TreeModel> tree = new List<TreeModel>();

            var pano = await _panoRepository.GetAsyncByPredicate(x => EF.Functions.Like(x.PanoKimlikNo, panoKimlikNo) && x.Silindi != true);
            if (pano == null)
            {
                throw new Exception("Pano bulunamadı!");
            }
            //var lokasyon = await GetLokasyonByPanoId(pano.Id);
            //if (lokasyon == null)
            //{
            //    throw new Exception("Lokasyon bulunamadı!");
            //}
            //tree.AddRange(await GetTreeByLokasyonId(userId, lokasyon.Id, onayliIslemlerDahil));
            var lokasyonId = await _panoRepository.FindLokasyonIdByPanoId(pano.Id);
            tree.AddRange(await GetTreeByLokasyonId(userId, lokasyonId, onayliIslemlerDahil));

            //var foundNodes = await FindNodeByName(tree, pano.Name);
            var foundNodes = await FindNodeById(tree, pano.Id);

            List<TreeModel> parents = new List<TreeModel>();
            // Her bir bulunan node için parent'larını bulma
            foreach (TreeModel node in foundNodes)
            {
                FindParentsRecursive(tree, node, parents);
                node.Expanded = true;
            }
            // Bulunan parent'ları genisletme
            foreach (TreeModel parent in parents)
            {
                parent.Expanded = true;
            }
            return Tuple.Create(tree, foundNodes.First());
        }

        public async Task<Tuple<List<TreeModel>, TreeModel>> GetTreeViewByNodeIdAndType(string userId, int nodeId, string nodeType, bool onayliIslemlerDahil = true)
        {
            List<TreeModel> tree = new List<TreeModel>();
            int lokasyonId = 0;
            List<TreeModel> foundNodes = new List<TreeModel>();

            if (nodeType == TreeViewNodeTypes.Pano)
            {
                var pano = await _panoRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Id == nodeId && x.Silindi != true);
                if (pano == null)
                {
                    throw new Exception("Pano bulunamadı!");
                }
                lokasyonId = await _panoRepository.FindLokasyonIdByPanoId(pano.Id);
                tree.AddRange(await GetTreeByLokasyonId(userId, lokasyonId, onayliIslemlerDahil));
                foundNodes = await FindNodeByDynamic(tree, pano.Id, TreeViewNodeTypes.Pano);
            }
            else if (nodeType == TreeViewNodeTypes.Makine)
            {

                var makine = await _makineRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Id == nodeId && x.Silindi != true);
                if (makine == null)
                {
                    throw new Exception("Makine bulunamadı!");
                }
                lokasyonId = await _makineRepository.FindLokasyonIdByMakineId(makine.Id);
                tree.AddRange(await GetTreeByLokasyonId(userId, lokasyonId, onayliIslemlerDahil));
                foundNodes = await FindNodeByDynamic(tree, makine.Id, TreeViewNodeTypes.Makine);
            }
            else if (nodeType == TreeViewNodeTypes.Bolum)
            {
                var bolum = await _bolumRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Id == nodeId && x.Silindi != true);
                if (bolum == null)
                {
                    throw new Exception("Bölüm bulunamadı!");
                }
                lokasyonId = await _bolumRepository.FindLokasyonIdByBolumId(bolum.Id);
                tree.AddRange(await GetTreeByLokasyonId(userId, lokasyonId, onayliIslemlerDahil));
                foundNodes = await FindNodeByDynamic(tree, bolum.Id, TreeViewNodeTypes.Bolum);
            }

            List<TreeModel> parents = new List<TreeModel>();
            // Her bir bulunan node için parent'larını bulma
            foreach (TreeModel node in foundNodes)
            {
                FindParentsRecursive(tree, node, parents);
                node.Expanded = true;
            }
            // Bulunan parent'ları genisletme
            foreach (TreeModel parent in parents)
            {
                parent.Expanded = true;
            }
            return Tuple.Create(tree, foundNodes.First());
        }

        public async Task<Lokasyon?> GetLokasyonByPanoId(int panoId)
        {
            var bolum = await _treeRepository.GetBolumByPanoId(panoId);
            if (bolum == null)
            {
                return null;
            }
            // await _sharedContext.Lokasyon.FirstOrDefaultAsync(l => l.Id == bolum.LokasyonId && l.Silindi != true);
            var lokasyon = await GetLokasyonByIdWithApi(bolum.LokasyonId);
            return lokasyon; // Lokasyon'u döndürüyoruz (bulunamazsa null dönecek)
        }

        async Task<List<TreeModel>> FindNodeByDynamic(List<TreeModel> tree, int nodeId, string nodeType)
        {
            List<TreeModel> foundNodes = new List<TreeModel>();

            foreach (TreeModel node in tree)
            {
                if (node.Id == nodeId && node.Type == nodeType)
                {
                    foundNodes.Add(node);
                }

                if (node.Children != null)
                {
                    // Eğer node'un Children listesi boş değilse, bu liste üzerinde de arama yapılmalı
                    foundNodes.AddRange(await FindNodeByDynamic(node.Children, nodeId, nodeType));
                }
            }

            return foundNodes;
        }

        async Task<List<TreeModel>> FindNodeById(List<TreeModel> tree, int panoId)
        {
            List<TreeModel> foundNodes = new List<TreeModel>();

            foreach (TreeModel node in tree)
            {
                if (node.Id == panoId && node.Type == TreeViewNodeTypes.Pano)
                {
                    foundNodes.Add(node);
                }

                if (node.Children != null)
                {
                    // Eğer node'un Children listesi boş değilse, bu liste üzerinde de arama yapılmalı
                    foundNodes.AddRange(await FindNodeById(node.Children, panoId));
                }
            }

            return foundNodes;
        }

        void FindParentsRecursive(List<TreeModel> treeList, TreeModel node, List<TreeModel> parents)
        {
            // Node'un parent'ını bulma
            TreeModel parent = FindParentNode(treeList, node);

            // Eğer parent varsa ve daha önce eklenmemişse, listeye ekle ve bir üst parent'ı ara
            if (parent != null && !parents.Contains(parent))
            {
                parents.Add(parent);
                FindParentsRecursive(treeList, parent, parents);
            }
        }

        TreeModel FindParentNode(List<TreeModel> treeList, TreeModel childNode)
        {
            foreach (TreeModel treeNode in treeList)
            {
                // Eğer çocuk node, bu node'un children'ları arasında ise, bu node parent'tır
                if (treeNode.Children != null && treeNode.Children.Exists(x => x.Id == childNode.Id && x.Type == childNode.Type))
                {
                    return treeNode;
                }

                // Eğer bu node'un children'ı varsa, bu node'un altındaki children'ları kontrol et
                if (treeNode.Children != null)
                {
                    TreeModel? foundParent = FindParentNode(treeNode.Children, childNode);
                    if (foundParent != null)
                    {
                        return foundParent; // Parent bulunduysa döndür
                    }
                }
            }

            return null; // Parent bulunamadıysa null döndür
        }
        public async Task<UserNodeYetkiModel> GetTreeYetki(string userId)
        {
            UserNodeYetkiModel userNodeYetkiModel = new UserNodeYetkiModel();
            var nodeYetkiList = new List<NodeYetkiModel>();

            var userMakineYetkis = await GetAllUserMakineYetkiByUserIdWithApi(userId);
            var userMakineYetkiler = userMakineYetkis.Where(x => (x.Aktif ?? true) && x.Silindi != true).ToList();

            foreach (var userMakineYetki in userMakineYetkiler)
            {
                // Her iterasyonda yeni bir NodeYetkiModel nesnesi oluştur
                var nodeYetki = new NodeYetkiModel();

                if (userMakineYetki.MakineId > 0)
                {
                    nodeYetki.NodeId = userMakineYetki.MakineId;
                    nodeYetki.NodeType = "makine";
                    nodeYetki.Write = userMakineYetki.Write ?? false;
                    nodeYetki.Read = userMakineYetki.Read ?? false;
                    nodeYetki.Responsible = userMakineYetki.SorumluMu ?? false;
                    nodeYetkiList.Add(nodeYetki);
                }
                else if (userMakineYetki.BolumId > 0)
                {
                    nodeYetki.NodeId = userMakineYetki.BolumId;
                    nodeYetki.NodeType = "bolum";
                    nodeYetki.Write = userMakineYetki.Write ?? false;
                    nodeYetki.Read = userMakineYetki.Read ?? false;
                    nodeYetki.Responsible = userMakineYetki.SorumluMu ?? false;
                    nodeYetkiList.Add(nodeYetki);
                }
                else if (userMakineYetki.LokasyonId > 0)
                {
                    nodeYetki.NodeId = userMakineYetki.LokasyonId;
                    nodeYetki.NodeType = "lokasyon";
                    nodeYetki.Write = userMakineYetki.Write ?? false;
                    nodeYetki.Read = userMakineYetki.Read ?? false;
                    nodeYetki.Responsible = userMakineYetki.SorumluMu ?? false;
                    nodeYetkiList.Add(nodeYetki);
                }
                else if (userMakineYetki.SirketId > 0)
                {
                    var sirketLokasyonlar = await GetAllLokasyonBySirketIdWithApi(userMakineYetki.SirketId);

                    foreach (var lokasyon in sirketLokasyonlar)
                    {
                        // Her lokasyon için yeni bir NodeYetkiModel nesnesi oluştur
                        var lokasyonYetki = new NodeYetkiModel
                        {
                            NodeId = lokasyon.Id,
                            NodeType = "lokasyon",
                            Write = userMakineYetki.Write ?? false,
                            Read = userMakineYetki.Read ?? false,
                            Responsible = userMakineYetki.SorumluMu ?? false
                        };
                        nodeYetkiList.Add(lokasyonYetki);
                    }
                }
            }

            userNodeYetkiModel.NodeYetki = nodeYetkiList;
            return userNodeYetkiModel;
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
                    treeModels.AddRange(await GetSubNodes(userMakineYetki.MakineId, TreeViewNodeTypes.Makine, userMakineYetki.Write ?? false, userMakineYetki.Read ?? false, userMakineYetki.SorumluMu ?? false));
                }
                else if (userMakineYetki.BolumId > 0)
                {
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
                        Responsible = userMakineYetki.SorumluMu ?? false,
                    });
                    var bolumler = await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == userMakineYetki.LokasyonId && x.Silindi != true);
                    foreach (var bolum in bolumler)
                    {
                        treeModels.AddRange(await GetSubNodes(bolum.Id, TreeViewNodeTypes.Bolum, userMakineYetki.Write ?? false, userMakineYetki.Read ?? false, userMakineYetki.SorumluMu ?? false));
                    }
                }
                else if (userMakineYetki.SirketId > 0)
                {
                    var sirketLokasyonlar = await GetAllLokasyonBySirketIdWithApi(userMakineYetki.SirketId);
                    treeModels.Add(new NodeYetkiModel
                    {
                        NodeId = userMakineYetki.SirketId,
                        NodeType = TreeViewNodeTypes.Sirket,
                        Write = userMakineYetki.Write ?? false,
                        Read = userMakineYetki.Read ?? false,
                        Responsible = userMakineYetki.SorumluMu ?? false,
                    });
                    foreach (var lokasyon in sirketLokasyonlar)
                    {
                        treeModels.Add(new NodeYetkiModel
                        {
                            NodeId = lokasyon.Id,
                            NodeType = TreeViewNodeTypes.Lokasyon,
                            Write = userMakineYetki.Write ?? false,
                            Read = userMakineYetki.Read ?? false,
                            Responsible = userMakineYetki.SorumluMu ?? false,
                        });
                        var bolumler = await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyon.Id && x.Silindi != true);
                        foreach (var bolum in bolumler)
                        {
                            treeModels.AddRange(await GetSubNodes(bolum.Id, TreeViewNodeTypes.Bolum, userMakineYetki.Write ?? false, userMakineYetki.Read ?? false, userMakineYetki.SorumluMu ?? false));
                        }
                    }
                }
            }
            return treeModels;
        }

        public async Task<List<NodeYetkiModel>> GetSubNodes(int nodeId, string nodeType, bool write, bool read, bool responsible)
        {
            try
            {
                var subNodes = new List<NodeYetkiModel>();
                subNodes.Add(new NodeYetkiModel
                {
                    NodeId = nodeId,
                    NodeType = nodeType,
                    Write = write,
                    Read = read,
                    Responsible = responsible,
                });
                if (nodeType == TreeViewNodeTypes.Bolum)
                {
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
                    // 1) Alt makineleri al
                    var childMakineler = await _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(x =>
                        x.ParentMakineId == nodeId && (x.Silindi == null || x.Silindi == false));

                    foreach (var child in childMakineler)
                        subNodes.AddRange(await GetSubNodes(child.Id, TreeViewNodeTypes.Makine, write, read, responsible));

                    // Makine altındaki panoları al
                    var panoMakineEslemes = await _panoMakineEslemeRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.MakineId == nodeId && x.Silindi != true);
                    var panolar = await _panoRepository.GetAllAsyncByPredicateWithAsNoTracking(x => panoMakineEslemes.Select(pme => pme.PanoId).ToList().Contains(x.Id) && x.ParentPanoId == 0 && x.Silindi != true);

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
                        //if (childNodes.Any())
                        //{
                        //    subNodes.Last().Children = childNodes;
                        //}
                    }
                }
                return subNodes;
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama yapabilirsiniz
                // Log.Error(ex, "Bölüm ve altındaki öğeler alınırken hata oluştu.");
                throw;
            }
        }
        public async Task<UserNodeYetkiModel> GetExpandedTreeYetki(string userId)
        {
            UserNodeYetkiModel userNodeYetkiModel = new UserNodeYetkiModel();
            var nodeYetkiList = new List<NodeYetkiModel>();

            var userMakineYetkis = await GetAllUserMakineYetkiByUserIdWithApi(userId);
            var userMakineYetkiler = userMakineYetkis.Where(x => (x.Aktif ?? true) && x.Silindi != true).ToList();

            foreach (var userMakineYetki in userMakineYetkiler)
            {
                // Her iterasyonda yeni bir NodeYetkiModel nesnesi oluştur
                var nodeYetki = new NodeYetkiModel();

                if (userMakineYetki.MakineId > 0)
                {
                    nodeYetki.NodeId = userMakineYetki.MakineId;
                    nodeYetki.NodeType = "makine";
                    nodeYetki.Write = userMakineYetki.Write ?? false;
                    nodeYetki.Read = userMakineYetki.Read ?? false;
                    nodeYetki.Responsible = userMakineYetki.SorumluMu ?? false;
                    nodeYetkiList.Add(nodeYetki);


                    var makine = await _makineRepository.GetAsyncByPredicateWithAsNoTracking(m => m.Id == userMakineYetki.MakineId && m.Silindi != true);
                    while (makine != null && makine.ParentMakineId > 0)
                    {
                        makine = await _makineRepository.GetAsyncByPredicateWithAsNoTracking(m => m.Id == makine.ParentMakineId && m.Silindi != true);
                    }
                    //makine altındaki makine ve panolar
                }
                else if (userMakineYetki.BolumId > 0)
                {
                    nodeYetki.NodeId = userMakineYetki.BolumId;
                    nodeYetki.NodeType = "bolum";
                    nodeYetki.Write = userMakineYetki.Write ?? false;
                    nodeYetki.Read = userMakineYetki.Read ?? false;
                    nodeYetki.Responsible = userMakineYetki.SorumluMu ?? false;
                    nodeYetkiList.Add(nodeYetki);
                }
                else if (userMakineYetki.LokasyonId > 0)
                {
                    nodeYetki.NodeId = userMakineYetki.LokasyonId;
                    nodeYetki.NodeType = "lokasyon";
                    nodeYetki.Write = userMakineYetki.Write ?? false;
                    nodeYetki.Read = userMakineYetki.Read ?? false;
                    nodeYetki.Responsible = userMakineYetki.SorumluMu ?? false;
                    nodeYetkiList.Add(nodeYetki);
                }
                else if (userMakineYetki.SirketId > 0)
                {
                    var sirketLokasyonlar = await GetAllLokasyonBySirketIdWithApi(userMakineYetki.SirketId);

                    foreach (var lokasyon in sirketLokasyonlar)
                    {
                        // Her lokasyon için yeni bir NodeYetkiModel nesnesi oluştur
                        var lokasyonYetki = new NodeYetkiModel
                        {
                            NodeId = lokasyon.Id,
                            NodeType = "lokasyon",
                            Write = userMakineYetki.Write ?? false,
                            Read = userMakineYetki.Read ?? false,
                            Responsible = userMakineYetki.SorumluMu ?? false
                        };
                        nodeYetkiList.Add(lokasyonYetki);
                    }
                }
            }

            userNodeYetkiModel.NodeYetki = nodeYetkiList;
            return userNodeYetkiModel;
        }

        public async Task<List<FilesModel>> GetDosyalarByPanoId(int panoId, string dosya)
        {
            var result = await _treeRepository.GetDosyalarByPanoId(panoId, dosya);
            return result;
        }

        public async Task<List<FilesModel>> GetAllDosyalarByPanoId(string userId, int panoId, string dosya, bool onayliIslemlerDahil = false)
        {
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);
            var result = await _treeRepository.GetAllDosyalarByPanoId(nodeYetkiler, userId, panoId, dosya, onayliIslemlerDahil);
            return result;
        }

        public async Task<List<CihazModel>> GetCihazlarByPanoId(int panoId, string userId, bool onayliIslemlerDahil)
        {
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);
            var cihazlar = await _treeRepository.GetCihazlarByPanoId(nodeYetkiler, panoId, userId, onayliIslemlerDahil);
            return cihazlar;//return _mapper.Map<List<CihazViewModel>>(cihazlar);
        }

        public async Task<bool> GetSorumluMuFromTreeNodeId(string userId, int nodeId, string nodeType)
        {
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);

            var nodeYetki = nodeYetkiler.Where(x => x.NodeId == nodeId && x.NodeType == nodeType).FirstOrDefault();
            if (nodeYetki != null)
            {
                return nodeYetki.Responsible;
            }
            return false;
        }

        public async Task<bool> GetPanoKimlikKullaniciYetkiTipiFromTreeNodeId(string userId, int nodeId, string nodeType)
        {
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);

            var nodeYetki = nodeYetkiler.Where(x => x.NodeId == nodeId && x.NodeType == nodeType).FirstOrDefault();
            if (nodeYetki != null)
            {
                return nodeYetki.Responsible;
            }
            return false;
        }

        public async Task<List<FilesModel>> GetDosyalarByDosyaYolu(string userId, string dosyaYolu, int panoId, bool? ustKlasorVarMi, bool onayliIslemlerDahil = false)
        {
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);
            var result = await _treeRepository.GetDosyalarByDosyaYolu(nodeYetkiler, userId, dosyaYolu, panoId, ustKlasorVarMi, onayliIslemlerDahil);
            return result;
        }

        public async Task EditDosyaAdiByDosyaYolu(string dosyaYolu, string eskiDosyaAdi, string yeniDosyaAdi, string dosyaUzanti, string userId, int panoId, int lokasyonId)
        {
            //var userYetkiler = await GetTreeYetki(userId);
            //var tree = (await GetTreeByLokasyonId(userId, Convert.ToInt32(lokasyonId), true)).First();
            //bool yetkiVarMi = false;
            //bool sorumluMu = false;
            //foreach (var userYetki in userYetkiler.NodeYetki)
            //{
            //    yetkiVarMi = IsNodeUnderParent(tree, Convert.ToInt32(panoId), TreeViewNodeTypes.Pano, userYetki.NodeId, userYetki.NodeType);
            //    if (yetkiVarMi)
            //    {
            //        sorumluMu = userYetki.Responsible;
            //        break;
            //    }
            //}
            bool sorumluMu = false;
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);

            var nodeYetki = nodeYetkiler.Where(x => x.NodeId == Convert.ToInt32(panoId) && x.NodeType == TreeViewNodeTypes.Pano).FirstOrDefault();
            if (nodeYetki != null)
            {
                sorumluMu = nodeYetki.Responsible;
            }

            string eskiDosyaYolu = Path.Combine(dosyaYolu, eskiDosyaAdi); // Eski dosya yolu
            string yeniDosyaYolu = Path.Combine(dosyaYolu, yeniDosyaAdi); // Yeni dosya yolu
            string klasorAd = Path.GetFileName(dosyaYolu);
            if (dosyaUzanti == "Klasor" || dosyaUzanti == "UstKlasor")
            {
                if (Directory.Exists(eskiDosyaYolu))
                {
                    // Klasörün adını değiştir
                    Directory.Move(eskiDosyaYolu, yeniDosyaYolu);
                }
            }
            else
            {
                // Eğer eski dosya yolu bir dosya ise
                if (File.Exists(eskiDosyaYolu))
                {
                    var islemVerisi = JsonConvert.SerializeObject(new { EskiDosyaAd = eskiDosyaAdi, YeniDosyaAd = yeniDosyaAdi + dosyaUzanti, DosyaYolu = dosyaYolu });

                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Dosya, lokasyonId, null, PanoKimlikOnayIslemTipi.Duzenleme, islemVerisi, eskiDosyaAdi, klasorAd, panoId, sorumluMu);
                    // Dosyanın adını değiştir
                    if (sorumluMu)
                    {
                        File.Move(eskiDosyaYolu, yeniDosyaYolu + dosyaUzanti);
                    }
                }
            }
        }

        public async Task<bool> AddNodeToTree(NodeModel node, string userId)
        {
            //TODO:if yetki yoksa onaya dusmeli //ex: bolum.onaydurumu=beklemede olmalı
            //var userYetkiler = await GetTreeYetki(userId);
            //var tree = (await GetTreeByLokasyonId(userId, node.LokasyonId, true)).First();
            //bool yetkiVarMi = false;
            bool sorumluMu = false;
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);

            var nodeYetki = nodeYetkiler.Where(x => x.NodeId == Convert.ToInt32(node.OldNodeId) && x.NodeType == node.OldNodeType).FirstOrDefault();
            if (nodeYetki != null)
            {
                sorumluMu = nodeYetki.Responsible;
            }

            //foreach (var userYetki in userYetkiler.NodeYetki)
            //{
            //    yetkiVarMi = IsNodeUnderParent(tree, Convert.ToInt32(node.OldNodeId), node.OldNodeType, userYetki.NodeId, userYetki.NodeType);
            //    if (yetkiVarMi)
            //    {
            //        sorumluMu = userYetki.Responsible;
            //        break;
            //    }
            //}

            if (node.OldNodeType == TreeViewNodeTypes.Lokasyon)
            {
                var oldLokasyonNode = await GetLokasyonByIdWithApi(Convert.ToInt32(node.OldNodeId));
                if (oldLokasyonNode == null)
                {
                    throw new Exception("Lokasyon bulunamadı!");
                }
                else
                {
                    if (oldLokasyonNode.Silindi != true)
                    {
                        if (node.AddedType == TreeViewNodeTypes.Bolum)
                        {
                            var addedNode = new Bolum()
                            {
                                Name = node.Name,
                                LokasyonId = oldLokasyonNode.Id,
                                ParentBolumId = 0,
                                OnayIslemdeMi = !sorumluMu,
                            };
                            await _bolumRepository.AddAsync(addedNode);
                            await _bolumRepository.SaveChangesAsync();
                            await AddOnayIslemKayit(userId, TreeViewNodeTypes.Bolum, node.LokasyonId, addedNode.Id, PanoKimlikOnayIslemTipi.Ekleme, JsonSerializer.Serialize(addedNode), null, null, null, sorumluMu);
                        }
                    }
                }
            }
            else if (node.OldNodeType == TreeViewNodeTypes.Bolum)
            {
                var oldNode = await _bolumRepository.GetAsyncByPredicate(x => x.Id.ToString() == node.OldNodeId && x.Silindi != true);
                if (node.AddedType == TreeViewNodeTypes.Bolum)
                {
                    var addedNode = new Bolum()
                    {
                        Name = node.Name,
                        LokasyonId = oldNode.LokasyonId,
                        ParentBolumId = oldNode.Id,
                        OnayIslemdeMi = !sorumluMu,
                    };
                    await _bolumRepository.AddAsync(addedNode);
                    await _bolumRepository.SaveChangesAsync();
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Bolum, node.LokasyonId, addedNode.Id, PanoKimlikOnayIslemTipi.Ekleme, JsonSerializer.Serialize(addedNode), null, null, null, sorumluMu);
                }
                else if (node.AddedType == TreeViewNodeTypes.Makine)
                {
                    var makineNo = GenerateTimeBasedUniqueNumber(6);
                    var makineNoVarMi = await _makineRepository.GetAsyncByPredicateWithAsNoTracking(x => x.MakineNo == makineNo.ToString());
                    if (makineNoVarMi != null)
                    {
                        while (makineNoVarMi != null)
                        {
                            makineNo = GenerateTimeBasedUniqueNumber(6);
                            makineNoVarMi = await _makineRepository.GetAsyncByPredicateWithAsNoTracking(x => x.MakineNo == makineNo.ToString());
                        }
                    }
                    var addedNode = new Makine()
                    {
                        Name = node.Name,
                        BolumId = oldNode.Id,
                        ParentMakineId = 0,
                        MakineNo = makineNo.ToString(), //node.No,
                        OnayIslemdeMi = !sorumluMu,
                    };
                    await _makineRepository.AddAsync(addedNode);
                    await _makineRepository.SaveChangesAsync();
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Makine, node.LokasyonId, addedNode.Id, PanoKimlikOnayIslemTipi.Ekleme, JsonSerializer.Serialize(addedNode), null, null, null, sorumluMu);
                }
            }
            else if (node.OldNodeType == TreeViewNodeTypes.Makine)
            {
                var oldNode = await _makineRepository.GetAsyncByPredicate(x => x.Id.ToString() == node.OldNodeId && x.Silindi != true);
                if (node.AddedType == TreeViewNodeTypes.Makine)
                {
                    var makineNo = GenerateTimeBasedUniqueNumber(6);
                    var makineNoVarMi = await _makineRepository.GetAsyncByPredicateWithAsNoTracking(x => x.MakineNo == makineNo.ToString());
                    if (makineNoVarMi != null)
                    {
                        while (makineNoVarMi != null)
                        {
                            makineNo = GenerateTimeBasedUniqueNumber(6);
                            makineNoVarMi = await _makineRepository.GetAsyncByPredicateWithAsNoTracking(x => x.MakineNo == makineNo.ToString());
                        }
                    }
                    var addedNode = new Makine()
                    {
                        Name = node.Name,
                        BolumId = oldNode.BolumId,
                        ParentMakineId = oldNode.Id,
                        MakineNo = makineNo.ToString(),//node.No,
                        Aciklama = node.Aciklama,
                        OnayIslemdeMi = !sorumluMu,
                    };
                    await _makineRepository.AddAsync(addedNode);
                    await _makineRepository.SaveChangesAsync();
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Makine, node.LokasyonId, addedNode.Id, PanoKimlikOnayIslemTipi.Ekleme, JsonSerializer.Serialize(addedNode), null, null, null, sorumluMu);
                }
                else if (node.AddedType == TreeViewNodeTypes.Pano)
                {
                    var panoKimlikNo = GenerateTimeBasedUniqueNumber(8);
                    //pano kimlik kontrolu
                    var panoKimlikNoVarMi = await _panoRepository.GetAsyncByPredicateWithAsNoTracking(x => x.PanoKimlikNo == panoKimlikNo.ToString());
                    if (panoKimlikNoVarMi != null)
                    {
                        while (panoKimlikNoVarMi != null)
                        {
                            panoKimlikNo = GenerateTimeBasedUniqueNumber(8);
                            panoKimlikNoVarMi = await _panoRepository.GetAsyncByPredicateWithAsNoTracking(x => x.PanoKimlikNo == panoKimlikNo.ToString());
                        }
                    }
                    var newPano = new Pano()
                    {
                        Name = node.Name,
                        ParentPanoId = 0,
                        DosyaYolu = node.DosyaYolu,
                        PanoKimlikNo = panoKimlikNo.ToString(),//node.No,
                        OnayIslemdeMi = !sorumluMu,
                    };
                    await _panoRepository.AddAsync(newPano);
                    await _panoRepository.SaveChangesAsync();

                    var newPanoMakineEsleme = new PanoMakineEsleme { MakineId = oldNode.Id, PanoId = newPano.Id };
                    await _panoMakineEslemeRepository.AddAsync(newPanoMakineEsleme);
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Pano, node.LokasyonId, newPano.Id, PanoKimlikOnayIslemTipi.Ekleme, JsonSerializer.Serialize(newPano), null, null, null, sorumluMu);
                    var dosyaYolu = await GenerateFolderForPano(newPano.Id, node.LokasyonId, false);
                    newPano.DosyaYolu = dosyaYolu;
                    await _panoRepository.SaveChangesAsync();
                }
            }
            else if (node.OldNodeType == TreeViewNodeTypes.Pano)
            {
                var oldNode = await _panoRepository.GetAsyncByPredicate(x => x.Id.ToString() == node.OldNodeId && x.Silindi != true);
                var panoKimlikNo = GenerateTimeBasedUniqueNumber(8);
                //pano kimlik kontrolu
                var panoKimlikNoVarMi = await _panoRepository.GetAsyncByPredicateWithAsNoTracking(x => x.PanoKimlikNo == panoKimlikNo.ToString());
                if (panoKimlikNoVarMi != null)
                {
                    while (panoKimlikNoVarMi != null)
                    {
                        panoKimlikNo = GenerateTimeBasedUniqueNumber(8);
                        panoKimlikNoVarMi = await _panoRepository.GetAsyncByPredicateWithAsNoTracking(x => x.PanoKimlikNo == panoKimlikNo.ToString());
                    }
                }
                if (node.AddedType == TreeViewNodeTypes.Pano)
                {
                    var addedNode = new Pano()
                    {
                        Name = node.Name,
                        ParentPanoId = oldNode.Id,
                        DosyaYolu = node.DosyaYolu,
                        PanoKimlikNo = panoKimlikNo.ToString() /*node.No*/,
                        OnayIslemdeMi = !sorumluMu,
                    };
                    await _panoRepository.AddAsync(addedNode);
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Pano, node.LokasyonId, addedNode.Id, PanoKimlikOnayIslemTipi.Ekleme, JsonSerializer.Serialize(addedNode), null, null, null, sorumluMu);
                    var dosyaYolu = await GenerateFolderForPano(addedNode.Id, node.LokasyonId, true);
                    addedNode.DosyaYolu = dosyaYolu;
                    await _panoRepository.SaveChangesAsync();
                }
            }
            return true;
        }

        public async Task<string> GenerateFolderForPano(int panoId, int lokasyonId, bool parentPanoVarMi = false)
        {
            var dosyaYolu = "";
            if (parentPanoVarMi)
            {
                var pano = await _panoRepository.GetAsyncByPredicate(p => p.Id == panoId && p.Silindi != true);
                if (pano != null)
                {
                    var parentPano = await _panoRepository.GetAsyncByPredicate(p => p.Id == pano.ParentPanoId && p.Silindi != true);
                    if (parentPano != null)
                    {
                        dosyaYolu = Path.Combine(parentPano.DosyaYolu, pano.Name);
                    }
                    else
                    {
                        throw new Exception("Pano bulunamadı");
                    }
                }
                else
                {
                    throw new Exception("Pano bulunamadı");
                }
            }
            else
            {
                var rootNodes = await _panoRepository.GetParentNodesByPanoIdForDosyaYolu(panoId);
                dosyaYolu = (await _lokasyonDosyaYoluEslemeRepository.GetAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId)).DosyaYolu;

                for (int i = rootNodes.Count() - 1; i >= 0; i--)
                {
                    dosyaYolu = Path.Combine(dosyaYolu, rootNodes[i].Item1);
                }
                if (!Directory.Exists(dosyaYolu))
                {
                    Directory.CreateDirectory(dosyaYolu);
                }
                if (Directory.Exists(dosyaYolu))
                {
                    var temppath = Path.Combine(dosyaYolu, "Resimler");
                    if (!Directory.Exists(temppath))
                    {
                        Directory.CreateDirectory(temppath);
                    }
                    temppath = Path.Combine(dosyaYolu, "PLC");
                    if (!Directory.Exists(temppath))
                    {
                        Directory.CreateDirectory(temppath);
                    }
                    temppath = Path.Combine(dosyaYolu, "HMI");
                    if (!Directory.Exists(temppath))
                    {
                        Directory.CreateDirectory(temppath);
                    }
                    temppath = Path.Combine(dosyaYolu, "Params");
                    if (!Directory.Exists(temppath))
                    {
                        Directory.CreateDirectory(temppath);
                    }
                    temppath = Path.Combine(dosyaYolu, "Elektrik Şeması");
                    if (!Directory.Exists(temppath))
                    {
                        Directory.CreateDirectory(temppath);
                    }
                    temppath = Path.Combine(dosyaYolu, "Temp");
                    if (!Directory.Exists(temppath))
                    {
                        Directory.CreateDirectory(temppath);
                    }
                }
            }

            return dosyaYolu;
        }

        public async Task<bool> AddCihazToTreeView(CihazViewModel cihaz, int lokasyonId, CancellationToken cancellation, string userId)
        {
            //var userYetkiler = await GetTreeYetki(userId);
            //var tree = (await GetTreeByLokasyonId(userId, lokasyonId, true)).First();
            //bool yetkiVarMi = false;
            bool sorumluMu = false;
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);

            var nodeYetki = nodeYetkiler.Where(x => x.NodeId == cihaz.PanoId && x.NodeType == TreeViewNodeTypes.Pano).FirstOrDefault();
            if (nodeYetki != null)
            {
                sorumluMu = nodeYetki.Responsible;
            }

            //foreach (var userYetki in userYetkiler.NodeYetki)
            //{
            //    yetkiVarMi = IsNodeUnderParent(tree, Convert.ToInt32(cihaz.PanoId), TreeViewNodeTypes.Pano, userYetki.NodeId, userYetki.NodeType);
            //    if (yetkiVarMi)
            //    {
            //        sorumluMu = userYetki.Responsible;
            //        break;
            //    }
            //}

            Cihaz newCihaz = new Cihaz()
            {
                IP = cihaz.IP,
                Name = cihaz.Name,
                Marka = cihaz.Marka,
                UrunKodu = cihaz.UrunKodu,
                PanoId = cihaz.PanoId,
                OnayIslemdeMi = !sorumluMu,
            };
            await _cihazRepository.AddAsync(newCihaz);
            await _cihazRepository.SaveChangesAsync();
            await AddOnayIslemKayit(userId, TreeViewNodeTypes.Cihaz, lokasyonId, newCihaz.Id, PanoKimlikOnayIslemTipi.Ekleme, JsonSerializer.Serialize(newCihaz), null, null, newCihaz.PanoId, sorumluMu);

            return true;
        }
        public async Task<bool> DeleteNodeFromTreeView(EditNodeModel node, string userId)
        {
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);
            //var userYetkiler = await GetTreeYetki(userId);
            //var tree = (await GetTreeByLokasyonId(userId, node.LokasyonId, true)).First();
            //bool yetkiVarMi = false;
            bool sorumluMu = false;

            //foreach (var userYetki in userYetkiler.NodeYetki)
            {
                string nodeType = "";
                int nodeId = 0;
                if (node.Bolum != null)
                {
                    nodeType = TreeViewNodeTypes.Bolum;
                    nodeId = node.Bolum.Id;
                }
                else if (node.Makine != null)
                {
                    nodeType = TreeViewNodeTypes.Makine;
                    nodeId = node.Makine.Id;
                }
                else if (node.Pano != null)
                {
                    nodeType = TreeViewNodeTypes.Pano;
                    nodeId = node.Pano.Id;
                }
                else if (node.Cihaz != null)
                {
                    //nodeType = TreeViewNodeTypes.Cihaz;
                    //nodeId = node.Cihaz.Id;
                    var cihaz = await _cihazRepository.GetByIdAsync(node.Cihaz.Id);
                    nodeType = TreeViewNodeTypes.Pano;
                    nodeId = cihaz.PanoId;
                }
                var nodeYetki = nodeYetkiler.Where(x => x.NodeId == nodeId && x.NodeType == nodeType).FirstOrDefault();
                if (nodeYetki != null)
                {
                    sorumluMu = nodeYetki.Responsible;
                    //break;
                }
                //yetkiVarMi = IsNodeUnderParent(tree, nodeId, nodeType, userYetki.NodeId, userYetki.NodeType);
                //if (yetkiVarMi)
                //{
                //    sorumluMu = userYetki.Responsible;
                //    break;
                //}
            }

            if (node.Bolum != null)
            {
                var bolum = await _bolumRepository.GetAsyncByPredicate(x => x.Id == node.Bolum.Id && x.Silindi != true);
                if (bolum == null)
                {
                    throw new Exception("Bölüm bulunamadı");
                }
                else
                {
                    if (sorumluMu)
                    {
                        bolum.Silindi = true;
                        await _bolumRepository.SaveChangesAsync();

                        var onayIslemKayitVarMi = await _onayIslemKayitRepository.GetAllAsyncByPredicate(x => x.OgeId == bolum.Id && x.OgeTipi == TreeViewNodeTypes.Bolum && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                        foreach (var onayIslemKayit in onayIslemKayitVarMi)
                        {
                            onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Red;
                            onayIslemKayit.GuncelleyenUserId = userId;
                            onayIslemKayit.GuncellemeZamani = DateTime.Now;
                        }
                        await _onayIslemKayitRepository.SaveChangesAsync();
                    }
                    else
                    {
                        if (bolum.OnayIslemdeMi.GetValueOrDefault(false))
                        {
                            var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Bolum && x.OgeId == bolum.Id);
                            if (onayIslemKayitSilmeVarMi != null)
                                return true;
                        }
                        bolum.OnayIslemdeMi = true;
                    }
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Bolum, node.LokasyonId, bolum.Id, PanoKimlikOnayIslemTipi.Silme, JsonSerializer.Serialize(bolum), null, null, null, sorumluMu);
                }
            }
            if (node.Makine != null)
            {
                var makine = await _makineRepository.GetAsyncByPredicate(x => x.Id == node.Makine.Id && x.Silindi != true);
                if (makine == null)
                {
                    throw new Exception("Makine bulunamadı");
                }
                else
                {
                    if (sorumluMu)
                    {
                        var panoMakineEslemeler = await _panoMakineEslemeRepository.GetAllAsyncByPredicate(x => x.MakineId == node.Makine.Id && x.Silindi != true).ToListAsync();
                        foreach (var panoMakineEsleme in panoMakineEslemeler)
                        {
                            panoMakineEsleme.Silindi = true;
                        }

                        var panolar = await _panoRepository.GetAllAsyncByPredicate(x => panoMakineEslemeler.Select(y => y.PanoId).ToList().Contains(x.Id) && x.Silindi != true).ToListAsync();
                        foreach (var pano in panolar)
                        {
                            var cihazlar = await _cihazRepository.GetAllAsyncByPredicate(x => x.PanoId == pano.Id && x.Silindi != true).ToListAsync();
                            foreach (var cihaz in cihazlar)
                            {
                                cihaz.Silindi = true;
                                var onayIslemKayitVarMi = await _onayIslemKayitRepository.GetAllAsyncByPredicate(x => x.OgeId == cihaz.Id && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                                foreach (var onayIslemKayit in onayIslemKayitVarMi)
                                {
                                    onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Red;
                                    onayIslemKayit.GuncelleyenUserId = userId;
                                    onayIslemKayit.GuncellemeZamani = DateTime.Now;
                                }
                            }
                            pano.Silindi = true;
                            var onayIslemKayitVarMi2 = await _onayIslemKayitRepository.GetAllAsyncByPredicate(x => x.OgeId == pano.Id && x.OgeTipi == TreeViewNodeTypes.Pano && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                            foreach (var onayIslemKayit in onayIslemKayitVarMi2)
                            {
                                onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Red;
                                onayIslemKayit.GuncelleyenUserId = userId;
                                onayIslemKayit.GuncellemeZamani = DateTime.Now;
                            }
                        }
                        makine.Silindi = true;
                        await _makineRepository.SaveChangesAsync();

                        var onayIslemKayitVarMi3 = await _onayIslemKayitRepository.GetAllAsyncByPredicate(x => x.OgeId == makine.Id && x.OgeTipi == TreeViewNodeTypes.Makine && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                        foreach (var onayIslemKayit in onayIslemKayitVarMi3)
                        {
                            onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Red;
                            onayIslemKayit.GuncelleyenUserId = userId;
                            onayIslemKayit.GuncellemeZamani = DateTime.Now;
                        }
                        await _onayIslemKayitRepository.SaveChangesAsync();
                    }
                    else
                    {
                        if (makine.OnayIslemdeMi.GetValueOrDefault(false))
                        {
                            var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Makine && x.OgeId == makine.Id);
                            if (onayIslemKayitSilmeVarMi != null)
                                return true;
                        }
                        makine.OnayIslemdeMi = true;
                    }
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Makine, node.LokasyonId, makine.Id, PanoKimlikOnayIslemTipi.Silme, JsonSerializer.Serialize(makine), null, null, null, sorumluMu);
                }
            }
            else if (node.Pano != null)
            {
                var pano = await _panoRepository.GetAsyncByPredicate(x => x.Id == node.Pano.Id && x.Silindi != true);
                if (pano == null)
                {
                    throw new Exception("Pano bulunamadı");
                }
                else
                {
                    if (sorumluMu)
                    {
                        pano.Silindi = true;
                        var panoMakineEslemeler = await _panoMakineEslemeRepository.GetAllAsyncByPredicate(x => x.PanoId == node.Pano.Id && x.Silindi != true).ToListAsync();
                        foreach (var panoMakineEsleme in panoMakineEslemeler)
                        {
                            panoMakineEsleme.Silindi = true;
                        }
                        //await _panoMakineEslemeRepository.SaveChangesAsync();

                        var cihazlar = await _cihazRepository.GetAllAsyncByPredicate(x => x.PanoId == node.Pano.Id && x.Silindi != true).ToListAsync();
                        foreach (var cihaz in cihazlar)
                        {
                            cihaz.Silindi = true;
                            var onayIslemKayitVarMi = await _onayIslemKayitRepository.GetAllAsyncByPredicate(x => x.OgeId == cihaz.Id && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                            foreach (var onayIslemKayit in onayIslemKayitVarMi)
                            {
                                onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Red;
                                onayIslemKayit.GuncelleyenUserId = userId;
                                onayIslemKayit.GuncellemeZamani = DateTime.Now;
                            }
                        }
                        await _panoRepository.SaveChangesAsync();
                    }
                    else
                    {
                        if (pano.OnayIslemdeMi.GetValueOrDefault(false))
                        {
                            var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Pano && x.OgeId == pano.Id);
                            if (onayIslemKayitSilmeVarMi != null)
                                return true;
                        }
                        pano.OnayIslemdeMi = true;
                    }
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Pano, node.LokasyonId, pano.Id, PanoKimlikOnayIslemTipi.Silme, JsonSerializer.Serialize(pano), null, null, null, sorumluMu);
                }
            }
            else if (node.Cihaz != null)
            {
                var cihaz = await _cihazRepository.GetAsyncByPredicate(x => x.Id == node.Cihaz.Id && x.Silindi != true);
                if (cihaz == null)
                {
                    throw new Exception("Cihaz bulunamadı");
                }
                else
                {
                    if (sorumluMu)
                    {
                        cihaz.Silindi = true;
                        await _cihazRepository.SaveChangesAsync();

                        if (cihaz.OnayIslemdeMi.GetValueOrDefault(false))
                        {
                            var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.OgeId == cihaz.Id);
                            if (onayIslemKayitSilmeVarMi != null)
                                return true;
                        }
                        var onayIslemKayitVarMi = await _onayIslemKayitRepository.GetAllAsyncByPredicate(x => x.OgeId == cihaz.Id && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                        foreach (var onayIslemKayit in onayIslemKayitVarMi)
                        {
                            onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Red;
                            onayIslemKayit.GuncelleyenUserId = userId;
                            onayIslemKayit.GuncellemeZamani = DateTime.Now;
                        }
                        await _onayIslemKayitRepository.SaveChangesAsync();
                    }
                    else
                    {
                        if (cihaz.OnayIslemdeMi.GetValueOrDefault(false))
                        {
                            var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.OgeId == cihaz.Id);
                            if (onayIslemKayitSilmeVarMi != null)
                                return true;
                        }
                        cihaz.OnayIslemdeMi = true;
                    }
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Cihaz, node.LokasyonId, cihaz.Id, PanoKimlikOnayIslemTipi.Silme, JsonSerializer.Serialize(cihaz), null, null, cihaz.PanoId, sorumluMu);
                }
            }
            return true;
        }

        public async Task<bool> EditNodeFromTreeView(EditNodeModel node, string userId)
        {
            //var userYetkiler = await GetTreeYetki(userId);
            //var tree = (await GetTreeByLokasyonId(userId, node.LokasyonId, true)).First();
            //bool yetkiVarMi = false;
            bool sorumluMu = false;
            var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);



            //foreach (var userYetki in userYetkiler.NodeYetki)
            {
                string nodeType = "";
                int nodeId = 0;
                if (node.Bolum != null)
                {
                    nodeType = TreeViewNodeTypes.Bolum;
                    nodeId = node.Bolum.Id;
                }
                else if (node.Makine != null)
                {
                    nodeType = TreeViewNodeTypes.Makine;
                    nodeId = node.Makine.Id;
                }
                else if (node.Pano != null)
                {
                    nodeType = TreeViewNodeTypes.Pano;
                    nodeId = node.Pano.Id;
                }
                else if (node.Cihaz != null)
                {
                    //nodeType = TreeViewNodeTypes.Cihaz;
                    //nodeId = node.Cihaz.Id;
                    var cihaz = await _cihazRepository.GetByIdAsync(node.Cihaz.Id);
                    nodeType = TreeViewNodeTypes.Pano;
                    nodeId = cihaz.PanoId;
                }
                var nodeYetki = nodeYetkiler.Where(x => x.NodeId == nodeId && x.NodeType == nodeType).FirstOrDefault();
                if (nodeYetki != null)
                {
                    sorumluMu = nodeYetki.Responsible;
                }
            }

            if (node.Bolum != null)
            {
                var bolum = await _bolumRepository.GetAsyncByPredicate(x => x.Id == node.Bolum.Id && x.Silindi != true);
                if (bolum == null)
                {
                    throw new Exception("Bölüm bulunamadı");
                }
                else
                {
                    if (sorumluMu)
                    {
                        bolum.Name = node.Bolum.Name;
                    }
                    else
                    {
                        bolum.OnayIslemdeMi = true;
                    }
                    await _bolumRepository.SaveChangesAsync();
                    var editedNode = new Bolum()
                    {
                        Id = node.Bolum.Id,
                        Name = node.Bolum.Name,
                        LokasyonId = bolum.LokasyonId,
                        ParentBolumId = bolum.ParentBolumId,
                    };
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Bolum, node.LokasyonId, editedNode.Id, PanoKimlikOnayIslemTipi.Duzenleme, JsonSerializer.Serialize(editedNode), null, null, null, sorumluMu);
                }
            }
            else if (node.Makine != null)
            {
                var makine = await _makineRepository.GetAsyncByPredicate(x => x.Id == node.Makine.Id && x.Silindi != true);
                if (makine == null)
                {
                    throw new Exception("Makine bulunamadı");
                }
                else
                {
                    if (sorumluMu)
                    {
                        makine.Name = node.Makine.Name;
                        makine.MakineNo = node.Makine.MakineNo;
                        makine.Aciklama = node.Makine.Aciklama;
                    }
                    else
                    {
                        makine.OnayIslemdeMi = true;
                    }
                    await _makineRepository.SaveChangesAsync();
                    var editedNode = new Makine()
                    {
                        Id = node.Makine.Id,
                        Name = node.Makine.Name,
                        BolumId = makine.BolumId,
                        ParentMakineId = makine.ParentMakineId,
                        MakineNo = node.Makine.MakineNo,
                        Aciklama = node.Makine.Aciklama,
                    };
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Makine, node.LokasyonId, editedNode.Id, PanoKimlikOnayIslemTipi.Duzenleme, JsonSerializer.Serialize(editedNode), null, null, null, sorumluMu);
                }
            }
            else if (node.Pano != null)
            {
                var pano = await _panoRepository.GetAsyncByPredicate(x => x.Id == node.Pano.Id && x.Silindi != true);
                if (pano == null)
                {
                    throw new Exception("Pano bulunamadı");
                }
                else
                {
                    if (sorumluMu)
                    {
                        pano.Name = node.Pano.Name;
                        pano.DosyaYolu = node.Pano.DosyaYolu;
                        pano.PanoKimlikNo = node.Pano.PanoKimlikNo;
                    }
                    else
                    {
                        pano.OnayIslemdeMi = true;
                    }
                    var editedNode = new Pano()
                    {
                        Id = node.Pano.Id,
                        Name = node.Pano.Name,
                        ParentPanoId = pano.ParentPanoId,
                        DosyaYolu = node.Pano.DosyaYolu,
                        PanoKimlikNo = node.Pano.PanoKimlikNo,//TODO:kontrol eklenebilir simdilik on yuzde kapali diye kontrol eklenmedi
                    };
                    await _panoRepository.SaveChangesAsync();
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Pano, node.LokasyonId, editedNode.Id, PanoKimlikOnayIslemTipi.Duzenleme, JsonSerializer.Serialize(editedNode), null, null, null, sorumluMu);
                }
            }
            else if (node.Cihaz != null)
            {
                var cihaz = await _cihazRepository.GetAsyncByPredicate(x => x.Id == node.Cihaz.Id && x.Silindi != true);
                if (cihaz == null)
                {
                    throw new Exception("Cihaz bulunamadı");
                }
                else
                {
                    if (sorumluMu)
                    {
                        cihaz.Name = node.Cihaz.Name;
                        cihaz.IP = node.Cihaz.IP;
                        cihaz.UrunKodu = node.Cihaz.UrunKodu;
                        cihaz.Marka = node.Cihaz.Marka;
                    }
                    else
                    {
                        cihaz.OnayIslemdeMi = true;
                    }
                    var editedNode = new Cihaz()
                    {
                        Id = node.Cihaz.Id,
                        Name = node.Cihaz.Name,
                        Marka = node.Cihaz.Marka,
                        UrunKodu = node.Cihaz.UrunKodu,
                        IP = node.Cihaz.IP,
                        PanoId = cihaz.PanoId,
                    };
                    await _cihazRepository.SaveChangesAsync();
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Cihaz, node.LokasyonId, editedNode.Id, PanoKimlikOnayIslemTipi.Duzenleme, JsonSerializer.Serialize(editedNode), null, null, editedNode.PanoId, sorumluMu);
                }
            }
            return true;
        }
        public static bool IsNodeUnderParent(TreeModel rootNode, int nodeId, string nodeType, int parentId, string parentType)
        {
            // Check if the current node matches the target
            if (rootNode.Id == nodeId && rootNode.Type == nodeType)
            {
                return true;
            }
            // Check all children recursively
            if (rootNode.Children != null)
            {
                foreach (var child in rootNode.Children)
                {
                    if (child.Type != TreeViewNodeTypes.Dosya && child.Type != TreeViewNodeTypes.Cihaz)
                    {
                        if (IsNodeUnderParent(child, nodeId, nodeType, child.Id, child.Type)) // Continue searching recursively
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public async Task<bool> UploadDosya(IFormFile[] files, string savePath, string panoId, string lokasyonId, CancellationToken cancellationToken, string userId)
        {
            try
            {
                if (files == null || files.Length == 0)
                {
                    return false; // Hiç dosya yüklenmedi
                }
                //TODO:if yetki yoksa savepath temppath ile yer değiştirecek
                var userYetkiler = await GetTreeYetki(userId);
                var tree = (await GetTreeByLokasyonId(userId, Convert.ToInt32(lokasyonId), true)).First();
                bool yetkiVarMi = false;
                bool sorumluMu = false;
                foreach (var userYetki in userYetkiler.NodeYetki)
                {
                    yetkiVarMi = IsNodeUnderParent(tree, Convert.ToInt32(panoId), TreeViewNodeTypes.Pano, userYetki.NodeId, userYetki.NodeType);
                    if (yetkiVarMi)
                    {
                        sorumluMu = userYetki.Responsible;
                        break;
                    }
                }
                // Klasör yolu yoksa oluştur
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }
                foreach (var file in files)
                {
                    string newFileName = file.FileName;

                    //if (sorumluMu)
                    {
                        // Dosya adını ve yolunu al
                        var relativePath = file.FileName; // Yüklenen dosyanın tam yolu
                        var fullPath = Path.Combine(savePath, relativePath); // Hedef dosya yolu
                        var klasorAd = Path.GetFileName(savePath);
                        string tempPath = Path.Combine(Path.GetDirectoryName(savePath), "Temp", klasorAd, file.FileName);
                        //var tempPath = Path.Combine(tempDosyaYolu, file.FileName);
                        if (File.Exists(fullPath) || File.Exists(tempPath))
                        {
                            var directoryFinal = Path.GetDirectoryName(fullPath);
                            var directoryTemp = Path.GetDirectoryName(tempPath);

                            string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                            string extension = Path.GetExtension(file.FileName);

                            var finalMax = GetMaxSuffixNumber(directoryFinal, fileName, extension);
                            var tempMax = GetMaxSuffixNumber(directoryTemp, fileName, extension);

                            int maxSuffix = Math.Max(finalMax, tempMax);
                            string directory = Path.GetDirectoryName(fullPath);
                            newFileName = $"{fileName} ({maxSuffix + 1})";
                            fullPath = Path.Combine(directory, newFileName + extension);
                            string tempDirectory = Path.GetDirectoryName(tempPath);
                            tempPath = Path.Combine(tempDirectory, newFileName + extension);


                            //var finalMax = GetUniqueFileNameCounter(fullPath);
                            //var tempMax = GetUniqueFileNameCounter(tempPath);
                            //var maxCounter = Math.Max(finalMax, tempMax);
                            //string directory = Path.GetDirectoryName(fullPath);
                            //string fileName = Path.GetFileNameWithoutExtension(fullPath);
                            //string extension = Path.GetExtension(fullPath);
                            //var newFileName = $"{fileName} ({maxCounter})";
                            //fullPath = Path.Combine(directory, newFileName + extension);
                            //string tempDirectory = Path.GetDirectoryName(tempPath);
                            //tempPath = Path.Combine(tempDirectory, newFileName + extension);
                        }
                        var islemVerisi = JsonConvert.SerializeObject(new { DosyaAd = file.FileName, TempDosyaYolu = tempPath, FinalDosyaYolu = fullPath });
                        await AddOnayIslemKayit(userId, TreeViewNodeTypes.Dosya, Convert.ToInt32(lokasyonId), null, PanoKimlikOnayIslemTipi.Ekleme, islemVerisi, newFileName/*file.FileName*/, klasorAd, Convert.ToInt32(panoId), sorumluMu);

                        // Eğer dosya adında bir klasör yapısı varsa, ilgili dizinleri oluştur

                        if (sorumluMu)
                        {
                            var directoryPath = Path.GetDirectoryName(fullPath);
                            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                            {
                                //islemVerisi = JsonConvert.SerializeObject(new { directoryPath = directoryPath });
                                //await AddOnayIslemKayit(userId, TreeViewNodeTypes.Klasor, null, PanoKimlikOnayIslemTipi.Ekleme, islemVerisi, sorumluMu);

                                Directory.CreateDirectory(directoryPath); // Gerekirse klasörü oluştur
                            }
                            // Dosyayı disk üzerine kaydetme
                            using (var stream = new FileStream(fullPath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream, cancellationToken);
                            }
                        }
                        else
                        {
                            var directoryPath = Path.GetDirectoryName(tempPath);
                            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                            {
                                //islemVerisi = JsonConvert.SerializeObject(new { directoryPath = directoryPath });
                                //await AddOnayIslemKayit(userId, TreeViewNodeTypes.Klasor, null, PanoKimlikOnayIslemTipi.Ekleme, islemVerisi, sorumluMu);

                                Directory.CreateDirectory(directoryPath); // Gerekirse klasörü oluştur
                            }
                            // Dosyayı disk üzerine kaydetme
                            using (var stream = new FileStream(tempPath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream, cancellationToken);
                            }
                            //var directoryPath = Path.GetDirectoryName(fullPath);
                            //if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                            //{
                            //    islemVerisi = JsonConvert.SerializeObject(new { directoryPath = directoryPath });
                            //    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Klasor, null, PanoKimlikOnayIslemTipi.Ekleme, islemVerisi, sorumluMu);
                            //}
                            //using (var stream = new FileStream(tempPanoKimlikPath, FileMode.Create))
                            //{
                            //    if (!File.Exists(Path.Combine(tempPanoKimlikPath, file.FileName)))
                            //    {
                            //        await file.CopyToAsync(stream, cancellationToken);
                            //    }
                            //}
                        }
                    }
                    //byte[] fileBytes = File.ReadAllBytes(fullPath); // Dosyayı byte dizisi olarak oku
                    //var asd = Convert.ToBase64String(fileBytes); // Base64 stringine dönüştür
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private int GetMaxSuffixNumber(string directory, string fileName, string extension)
        {
            int max = 0;

            if (!Directory.Exists(directory))
                return 0;

            var files = Directory.GetFiles(directory, $"{fileName}*{extension}");

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);

                // Sadece aynı isimle başlayanları al
                if (name == fileName)
                {
                    // Asıl dosya → suffix = 0
                    max = Math.Max(max, 0);
                }
                else if (name.StartsWith(fileName + " (") && name.EndsWith(")"))
                {
                    // goruntu (4)
                    int start = name.LastIndexOf("(") + 1;
                    int end = name.LastIndexOf(")");

                    var numberStr = name.Substring(start, end - start);

                    if (int.TryParse(numberStr, out int number))
                    {
                        max = Math.Max(max, number);
                    }
                }
            }

            return max;
        }


        private int GetUniqueFileNameCounter(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            string newPath = filePath;
            int counter = 1;

            while (File.Exists(newPath))
            {
                newPath = Path.Combine(directory, $"{fileName} ({counter}){extension}");
                counter++;
            }

            return counter;
        }

        //private string GetUniqueFilePath(string filePath)
        //{
        //    string directory = Path.GetDirectoryName(filePath);
        //    string fileName = Path.GetFileNameWithoutExtension(filePath);
        //    string extension = Path.GetExtension(filePath);

        //    string newPath = filePath;
        //    int counter = 1;

        //    while (File.Exists(newPath))
        //    {
        //        newPath = Path.Combine(directory, $"{fileName} ({counter}){extension}");
        //        counter++;
        //    }

        //    return newPath;
        //}       

        private async Task AddOnayIslemKayit(string userId, string ogeTipi, int lokasyonId, int? ogeId, byte islemTipi, string? islemVerisi, string? dosyaAd, string? klasorAd, int? panoId, bool sorumluMu = false)
        {
            var now = DateTime.Now;
            var onayIslemKayit = new OnayIslemKayit();
            onayIslemKayit.UserId = userId;
            onayIslemKayit.OgeTipi = ogeTipi;
            if (ogeId != null) onayIslemKayit.OgeId = ogeId;
            onayIslemKayit.IslemTipi = islemTipi;
            if (islemVerisi != null) onayIslemKayit.IslemVerisi = islemVerisi;
            onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Beklemede;
            onayIslemKayit.KayitZamani = now;
            onayIslemKayit.LokasyonId = lokasyonId;
            onayIslemKayit.DosyaAd = dosyaAd;
            onayIslemKayit.KlasorAd = klasorAd;
            if (ogeTipi == TreeViewNodeTypes.Cihaz || ogeTipi == TreeViewNodeTypes.Dosya)
            {
                onayIslemKayit.PanoId = panoId;
            }
            if (sorumluMu)
            {
                onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Kabul;
                onayIslemKayit.GuncellemeZamani = now;
                onayIslemKayit.GuncelleyenUserId = userId;
            }

            await _onayIslemKayitRepository.AddAsync(onayIslemKayit);
            await _onayIslemKayitRepository.SaveChangesAsync();
        }

        public async Task DeleteDosya(string dosya, CancellationToken cancellationToken, string userId, int panoId, int lokasyonId)
        {
            if (System.IO.File.Exists(dosya))
            {
                var userYetkiler = await GetTreeYetki(userId);
                var tree = (await GetTreeByLokasyonId(userId, lokasyonId, true)).First();
                bool yetkiVarMi = false;
                bool sorumluMu = false;
                bool kendiEkledigiDosya = false;
                bool eklemeOnaylandiMi = false;
                foreach (var userYetki in userYetkiler.NodeYetki)
                {
                    yetkiVarMi = IsNodeUnderParent(tree, panoId, TreeViewNodeTypes.Pano, userYetki.NodeId, userYetki.NodeType);
                    if (yetkiVarMi)
                    {
                        sorumluMu = userYetki.Responsible;
                        break;
                    }
                }
                var klasorAd = Path.GetFileName(Path.GetDirectoryName(dosya));
                var islemVerisi = JsonConvert.SerializeObject(new { SilinecekDosyaYolu = dosya });
                var eklemeKaydi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.PanoId == panoId &&
                       x.OgeTipi == TreeViewNodeTypes.Dosya &&
                       x.DosyaAd == Path.GetFileName(dosya) &&
                       x.KlasorAd == klasorAd &&
                       x.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme &&
                       x.Durum == PanoKimlikOnayIslemDurum.Beklemede);

                if (eklemeKaydi != null && eklemeKaydi.UserId == userId)
                {
                    kendiEkledigiDosya = true;
                    eklemeOnaylandiMi = eklemeKaydi.Durum == PanoKimlikOnayIslemDurum.Kabul;
                }
                await AddOnayIslemKayit(userId, TreeViewNodeTypes.Dosya, lokasyonId, null, PanoKimlikOnayIslemTipi.Silme, islemVerisi, Path.GetFileName(dosya), klasorAd, panoId, sorumluMu || kendiEkledigiDosya);

                //TODO:if yetki yoksa savepath temppath ile yer değiştirecek
                cancellationToken.ThrowIfCancellationRequested();
                if (sorumluMu)
                {
                    if (File.Exists(dosya))
                        File.Delete(dosya);
                    if (eklemeKaydi != null)
                    {
                        await _onayIslemKayitRepository.DosyaIslemReddet(eklemeKaydi.Id, userId);
                    }
                }
                else if (kendiEkledigiDosya)
                {
                    if (!eklemeOnaylandiMi)
                    {
                        // Henüz onaylanmamış → direkt sil
                        if (File.Exists(dosya))
                            File.Delete(dosya);
                        if (eklemeKaydi != null)
                        {
                            await _onayIslemKayitRepository.DosyaIslemReddet(eklemeKaydi.Id, userId);
                        }
                    }
                    return;
                }
            }
        }

        public async Task DeleteKlasor(string klasor, CancellationToken cancellationToken, string userId, int panoId, int lokasyonId)
        {
            if (System.IO.Directory.Exists(klasor))
            {
                try
                {
                    var userYetkiler = await GetTreeYetki(userId);
                    var tree = (await GetTreeByLokasyonId(userId, lokasyonId, true)).First();
                    bool yetkiVarMi = false;
                    bool sorumluMu = false;
                    foreach (var userYetki in userYetkiler.NodeYetki)
                    {
                        yetkiVarMi = IsNodeUnderParent(tree, panoId, TreeViewNodeTypes.Pano, userYetki.NodeId, userYetki.NodeType);
                        if (yetkiVarMi)
                        {
                            sorumluMu = userYetki.Responsible;
                            break;
                        }
                    }

                    // Klasördeki tüm dosyaları sil
                    var dosyalar = System.IO.Directory.GetFiles(klasor);
                    var klasorAd = Path.GetFileName(klasor);

                    foreach (var dosya in dosyalar)
                    {
                        var islemVerisi = JsonConvert.SerializeObject(new { currentPath = dosya });
                        await AddOnayIslemKayit(userId, TreeViewNodeTypes.Dosya, lokasyonId, null, PanoKimlikOnayIslemTipi.Silme, islemVerisi, Path.GetFileName(dosya), klasorAd, panoId, sorumluMu);
                        cancellationToken.ThrowIfCancellationRequested();
                        if (sorumluMu)
                        {
                            System.IO.File.Delete(dosya);
                        }
                    }

                    // Klasördeki tüm alt klasörleri sil
                    var altKlasorler = System.IO.Directory.GetDirectories(klasor);
                    foreach (var altKlasor in altKlasorler)
                    {
                        var islemVerisi = JsonConvert.SerializeObject(new { currentPath = altKlasor });
                        //await AddOnayIslemKayit(userId, TreeViewNodeTypes.Klasor, lokasyonId, panoId, PanoKimlikOnayIslemTipi.Silme, islemVerisi, sorumluMu);
                        cancellationToken.ThrowIfCancellationRequested();
                        if (sorumluMu)
                        {
                            await DeleteKlasor(altKlasor, cancellationToken, userId, panoId, lokasyonId);
                        }
                    }

                    // Ana klasörü sil
                    //await AddOnayIslemKayit(userId, TreeViewNodeTypes.Klasor, lokasyonId, panoId, PanoKimlikOnayIslemTipi.Silme, JsonConvert.SerializeObject(new { currentPath = klasor }), null, sorumluMu);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (sorumluMu)
                    {
                        System.IO.Directory.Delete(klasor);
                    }
                }
                catch (System.IO.IOException e)
                {
                }
            }
        }

        public async Task<(string contentType, FileStream file)> GetDocument(string fileName, string filePath)
        {
            try
            {
                var path = Path.Combine(filePath, fileName);
                if (!File.Exists(path))
                {
                    throw new Exception("Dosya bulunamadı");
                }
                string contentType = GetFileContentType(path);

                var dokuman = File.OpenRead(path);

                return (contentType, dokuman);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task<string> ReadTxtDocument(string filePath)
        {
            string fileContent = System.IO.File.ReadAllText(filePath);

            return Task.FromResult(fileContent);
        }

        public async Task EditTxtDocument2(string fileContent, string filePath, string userId)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, fileContent);
            }
            catch (Exception ex)
            {
            }
        }

        public async Task EditTxtDocument(string fileContent, string filePath, string userId, int panoId, int lokasyonId)
        {
            try
            {
                bool sorumluMu = false;
                var nodeYetkiler = await GetAllUserMakineYetkiByUserId(userId);

                var nodeYetki = nodeYetkiler.Where(x => x.NodeId == panoId && x.NodeType == TreeViewNodeTypes.Pano).FirstOrDefault();
                if (nodeYetki != null)
                {
                    sorumluMu = nodeYetki.Responsible;
                }

                var panoPath = (await _panoRepository.GetByIdAsync(panoId))?.DosyaYolu;
                if (panoPath == null)
                {
                    throw new Exception("Pano dosya yolu bulunamadı!");
                }
                //if (sorumluMu)
                //{
                //    await File.WriteAllTextAsync(filePath, fileContent);
                //}
                //else
                {
                    var klasorAd = Path.GetFileName(Path.GetDirectoryName(filePath));
                    var dosyaAd = Path.GetFileName(filePath);
                    var fullPath = Path.Combine(panoPath, klasorAd, dosyaAd); // Hedef dosya yolu
                    var islemVerisi = JsonConvert.SerializeObject(new { DosyaYolu = filePath, DosyaIcerik = fileContent, FinalDosyaYolu = fullPath });
                    await AddOnayIslemKayit(userId, TreeViewNodeTypes.Dosya, lokasyonId, null, PanoKimlikOnayIslemTipi.Duzenleme, islemVerisi, dosyaAd, klasorAd, panoId, sorumluMu);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public async Task<bool> NodeOnayaGonder(EditNodeModel node, string userId)
        {
            var userYetkiler = await GetTreeYetki(userId);
            var tree = (await GetTreeByLokasyonId(userId, node.LokasyonId, true)).First();
            bool yetkiVarMi = false;
            bool sorumluMu = false;

            foreach (var userYetki in userYetkiler.NodeYetki)
            {
                string nodeType = "";
                int nodeId = 0;
                if (node.Bolum != null)
                {
                    nodeType = TreeViewNodeTypes.Bolum;
                    nodeId = node.Bolum.Id;
                }
                else if (node.Makine != null)
                {
                    nodeType = TreeViewNodeTypes.Makine;
                    nodeId = node.Makine.Id;
                }
                else if (node.Pano != null)
                {
                    nodeType = TreeViewNodeTypes.Pano;
                    nodeId = node.Pano.Id;
                }
                else if (node.Cihaz != null)
                {
                    nodeType = TreeViewNodeTypes.Cihaz;
                    nodeId = node.Cihaz.Id;
                }
                yetkiVarMi = IsNodeUnderParent(tree, nodeId, nodeType, userYetki.NodeId, userYetki.NodeType);
                if (yetkiVarMi)
                {
                    sorumluMu = userYetki.Responsible;
                    break;
                }
            }

            if (node.Bolum != null)
            {
                var bolum = await _bolumRepository.GetAsyncByPredicate(x => x.Id == node.Bolum.Id && x.Silindi != true);
                if (bolum == null)
                {
                    throw new Exception("Bölüm bulunamadı");
                }
                else
                {
                    bolum.OnayaGonderildiMi = true;
                    await _bolumRepository.SaveChangesAsync();
                    //todo:alt düğümler de true işaretlenmeli
                }
            }
            else if (node.Makine != null)
            {
                var makine = await _makineRepository.GetAsyncByPredicate(x => x.Id == node.Makine.Id && x.Silindi != true);
                if (makine == null)
                {
                    throw new Exception("Makine bulunamadı");
                }
                else
                {
                    makine.OnayaGonderildiMi = true;
                }
            }
            else if (node.Pano != null)
            {
                var pano = await _panoRepository.GetAsyncByPredicate(x => x.Id == node.Pano.Id && x.Silindi != true);
                if (pano == null)
                {
                    throw new Exception("Pano bulunamadı");
                }
                else
                {
                    pano.OnayaGonderildiMi = true;
                }
            }
            else if (node.Cihaz != null)
            {
                var cihaz = await _cihazRepository.GetAsyncByPredicate(x => x.Id == node.Cihaz.Id && x.Silindi != true);
                if (cihaz == null)
                {
                    throw new Exception("Cihaz bulunamadı");
                }
                else
                {
                    cihaz.OnayaGonderildiMi = true;
                }
            }
            return true;
        }

        public static string GetFileContentType(string url)
        {
            string extension = Path.GetExtension(url);
            string contentType = "";

            switch (extension)
            {
                case ".pdf":
                    contentType = "application/pdf";
                    break;
                case ".docx":
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
                case ".gif":
                    contentType = "image/gif";
                    break;
                case ".jpeg":
                    contentType = "image/jpeg";
                    break;
                case ".jpg":
                    contentType = "image/jpeg";
                    break;
                case ".png":
                    contentType = "image/png";
                    break;
                case ".svg":
                    contentType = "image/svg+xml";
                    break;
                case ".ppt":
                    contentType = "application/vnd.ms-powerpoint";
                    break;
                case ".pptx":
                    contentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                    break;
                case ".txt":
                    contentType = "text/plain";
                    break;
                case ".xls":
                    contentType = "application/vnd.ms-excel";
                    break;
                case ".xlsx":
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;
                case ".exe":
                    contentType = "application/octet-stream";
                    break;
                case ".rar":
                    contentType = "application/x-rar-compressed";
                    break;
                case ".zip":
                    contentType = "application/zip";
                    break;
                // and so on
                default:
                    throw new ArgumentOutOfRangeException($"Unable to find Content Type for file name {extension}.");
            }

            return contentType;
        }

        public async Task<Lokasyon?> GetLokasyonByIdWithApi(int lokasyonId)
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

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
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

        int GenerateTimeBasedUniqueNumber(int basamakSayisi)
        {
            // Zaman damgasını al (Milisaniye)
            long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            // Zaman damgasını string'e çevir
            string timestampStr = timestamp.ToString();

            // İstenilen uzunlukta bir sayı elde etmek için zaman damgasından bir kısmı al
            string numberStr = timestampStr.Length > basamakSayisi
                ? timestampStr.Substring(timestampStr.Length - basamakSayisi) // Son basamakSayisi kadar al
                : timestampStr.PadLeft(basamakSayisi, '0'); // Eğer zaman damgası daha kısa ise başına sıfır ekle

            // Sayının son iki basamağının çift olmasını sağla
            int lastTwoDigits = int.Parse(numberStr.Substring(numberStr.Length - 2, 2));
            if (lastTwoDigits % 2 != 0) // Son iki basamağı tekse
            {
                lastTwoDigits++; // Çift yap
            }

            // Sayıyı geri oluştur
            numberStr = numberStr.Substring(0, numberStr.Length - 2) + lastTwoDigits.ToString("00");

            // Sonuç olarak sayıyı döndür
            return int.Parse(numberStr);
        }
    }
}