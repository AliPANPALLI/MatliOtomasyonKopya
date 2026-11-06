using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Application.ViewModels.ReportViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Domain.Models.ReportModels;
using static SharedKernel.ApplicationConstants;

namespace SharedApp.Application.Services
{
    public class SirketAppService : AppService<Sirket, SirketViewModel>, ISirketAppService
    {
        private readonly ISirketRepository _sirketRepository;
        private readonly ILokasyonRepository _lokasyonRepository;

        public SirketAppService(IMapper mapper, ISirketRepository sirketRepository, ILokasyonRepository lokasyonRepository) : base(mapper, sirketRepository)
        {
            _sirketRepository = sirketRepository;
            _lokasyonRepository = lokasyonRepository;
        }

        public async Task SetSilindi(int id)
        {
            var bolum = await _sirketRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            bolum.Silindi = true;
            await _sirketRepository.SaveChangesAsync();
        }

        public async Task<List<Sirket>> GetAllSirketForYetki()
        {
            var listSirket = new List<Sirket>();

            var sirketler = await _sirketRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true);
            listSirket.AddRange(sirketler);

            return listSirket;
        }

        public async Task<List<TreeModel>> GetTesisTreeView()
        {
            List<TreeModel> tree = new List<TreeModel>();
            var sirketler = await _sirketRepository.GetAllAsyncByPredicateWithAsNoTracking(x => !x.Name.Contains("Matlı") && !x.Name.Contains("Tedarikçi") && x.Silindi != true);
            List<Lokasyon> lokasyonlar = new();
            foreach (var sirket in sirketler)
            {
                var sirketNode = new TreeModel
                {
                    Id = sirket.Id,
                    Name = sirket.Name,
                    Expanded = false,
                    Checked = false,
                    Type = NesneIzlemeDBHiyerarsi.Sirket,
                    Info = null,
                    Children = new List<TreeModel>()
                };
                tree.Add(sirketNode);

                lokasyonlar = (await _lokasyonRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.SirketId == sirket.Id && x.Silindi != true));
                foreach (var lokasyon in lokasyonlar)
                {
                    var lokasyonNode = new TreeModel
                    {
                        Id = lokasyon.Id,
                        Name = lokasyon.Name,
                        Expanded = false,
                        Checked = false,
                        Type = NesneIzlemeDBHiyerarsi.Lokasyon,
                        Info = null,
                        Children = new List<TreeModel>()
                    };
                    List<Tesis> tesisler = new();
                    tesisler = await _sirketRepository.GetTesislerByLokasyonId(lokasyon.Id);
                    foreach (var tesis in tesisler)
                    {
                        if (tree.Last().Children == null)
                        {
                            tree.Last().Children = new List<TreeModel>();
                        }
                        lokasyonNode.Children.Add(await GetAllNodesRecursive(tesis.Id, tesis.Name));
                    }
                    sirketNode.Children.Add(lokasyonNode);
                }
            }
            return tree;
        }
        public async Task<TreeModel> GetAllNodesRecursive(int tesisId, string tesisName)
        {
            var rootNode = new TreeModel
            {
                Id = tesisId,
                Name = tesisName ?? "Tesis",
                Type = NesneIzlemeDBHiyerarsi.Tesis,
                Checked = false,
                Expanded = false,
                Info = null,
                Children = new List<TreeModel>()
            };
            // Tesis altindaki bolumleri al
            List<Bolum> bolumler = new();
            bolumler = await _sirketRepository.GetBolumlerByTesisId(tesisId);

            foreach (var bolum in bolumler)
            {
                var bolumNode = new TreeModel
                {
                    Id = bolum.Id,
                    Name = bolum.Name,
                    Type = NesneIzlemeDBHiyerarsi.Bolum,
                    Checked = false,
                    Expanded = false,
                    Info = null,
                    Children = new List<TreeModel>()
                };

                // Bolum altindaki makineleri al
                List<Tbl_Makine> makineler = new();
                makineler = await _sirketRepository.GetMakinelerByBolumId(bolum.Id);
                foreach (var makine in makineler)
                {
                    var makineNode = new TreeModel
                    {
                        Id = makine.Id,
                        Name = makine.Name ?? "Makine",
                        Type = NesneIzlemeDBHiyerarsi.Makine,
                        Checked = false,
                        Expanded = false,
                        Info = makine.Aciklama,
                        Children = new List<TreeModel>()
                    };

                    // Tesis altindaki bolumleri al
                    List<Tbl_Nesne> nesneler = new();
                    nesneler = await _sirketRepository.GetNesnelerByMakineId(makine.Id);
                    foreach (var nesne in nesneler)
                    {
                        var nesneNode = new TreeModel
                        {
                            Id = nesne.Id,
                            Name = nesne.Name,
                            Type = NesneIzlemeDBHiyerarsi.Nesne,
                            Checked = false,
                            Expanded = false,
                            Info = nesne.Aciklama,
                            Children = new List<TreeModel>()
                        };
                        makineNode.Children.Add(nesneNode);
                    }
                    bolumNode.Children.Add(makineNode);
                }
                rootNode.Children.Add(bolumNode);
            }
            return rootNode;
        }
        public async Task AddLokasyon(LokasyonViewModel lokasyonViewModel)
        {
            var model = _mapper.Map<Lokasyon>(lokasyonViewModel);
            model.Silindi = false;
            await _sirketRepository.AddLokasyon(model);
        }

        public async Task AddTesis(TesisViewModel tesisViewModel)
        {
            var model = _mapper.Map<Tesis>(tesisViewModel);
            await _sirketRepository.AddTesis(model);
        }

        public async Task AddBolum(BolumViewModel bolumViewModel)
        {
            var model = _mapper.Map<Bolum>(bolumViewModel);
            await _sirketRepository.AddBolum(model);
        }

        public async Task AddMakine(Tbl_MakineViewModel tbl_MakineViewModel)
        {
            var model = _mapper.Map<Tbl_Makine>(tbl_MakineViewModel);
            await _sirketRepository.AddMakine(model);
        }

        public async Task AddNesne(Tbl_NesneViewModel tbl_NesneViewModel)
        {
            var model = _mapper.Map<Tbl_Nesne>(tbl_NesneViewModel);
            await _sirketRepository.AddNesne(model);
        }
    }
}