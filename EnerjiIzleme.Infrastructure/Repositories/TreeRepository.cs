using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Interfaces;
using EnerjiIzleme.Domain.Models;
using EnerjiIzleme.Domain.Models.ReportModels;
using Microsoft.EntityFrameworkCore;
using static SharedKernel.ApplicationConstants;

namespace EnerjiIzleme.Infrastructure.Repositories
{
    public class TreeRepository : ITreeRepository
    {
        private readonly EnerjiIzlemeDbContext _context;

        public TreeRepository(EnerjiIzlemeDbContext context)
        {
            _context = context;
        }
        //Grup bazlı parametre getirme
        public async Task<List<EnerjiMaliyetParametreler>> GetAllParametrelerByGrupId(int enerjiMaliyetGrupId)
        {
            return await _context.EnerjiMaliyetParametreler.Where(x => x.EnerjiMaliyetGrupId == enerjiMaliyetGrupId).OrderBy(x => x.Yil).ThenBy(x => x.Ay).ToListAsync();
        }
        //Yıl + grup bazlı parametre getirme
        public async Task<List<EnerjiMaliyetParametreler>> GetEnerjiMaliyetParametreler(int seciliYil, int enerjiMaliyetGrupId)
        {
            return await _context.EnerjiMaliyetParametreler.Where(x => x.Yil == seciliYil && x.EnerjiMaliyetGrupId == enerjiMaliyetGrupId).OrderBy(x => x.Ay).ToListAsync();
        }
        //Kaydetme metodu
        public async Task AddEnerjiMaliyetParametreler(List<EnerjiMaliyetParametreler> enerjiMaliyetParametreler, int seciliYil, int enerjiMaliyetGrupId, string tarifeTipi)
        {
            var eskiKayitlar = await _context.EnerjiMaliyetParametreler.Where(x => x.Yil == seciliYil && x.EnerjiMaliyetGrupId == enerjiMaliyetGrupId).ToListAsync();

            if (eskiKayitlar.Any())
                _context.RemoveRange(eskiKayitlar);

            for (int i = 0; i < enerjiMaliyetParametreler.Count; i++)
            {
                var item = enerjiMaliyetParametreler[i];

                item.Id = 0;
                item.Yil = seciliYil;
                item.Ay = item.Ay <= 0 ? i + 1 : item.Ay;

                // Yeni sistemde lokasyon bağlantısı yok.
                item.LokasyonId = null;

                // Asıl bağlantı grup üzerinden.
                item.EnerjiMaliyetGrupId = enerjiMaliyetGrupId;

                // TEK ZAMANLI
                if (tarifeTipi == "TEK")
                {
                    item.Gunduz = null;
                    item.Puant = null;
                    item.Gece = null;
                    item.Yekdem = null;
                    item.AnlasmaOrani = null;

                    item.TekZamanliTarimsal = true;
                    item.UcZamanliTarimsal = false;
                    item.Genel = false;
                }

                // 3 ZAMANLI
                else if (tarifeTipi == "UC")
                {
                    item.AktifEnerjiBedeli = null;
                    item.Yekdem = null;
                    item.AnlasmaOrani = null;

                    item.TekZamanliTarimsal = false;
                    item.UcZamanliTarimsal = true;
                    item.Genel = false;
                }

                // GENEL
                else
                {
                    item.AktifEnerjiBedeli = null;
                    item.Gunduz = null;
                    item.Puant = null;
                    item.Gece = null;
                    item.GucBedeli = null;

                    item.TekZamanliTarimsal = false;
                    item.UcZamanliTarimsal = false;
                    item.Genel = true;
                }
            }

            await _context.AddRangeAsync(enerjiMaliyetParametreler);
            await _context.SaveChangesAsync();
        }
        //////////////////
        //GRUP METOTLARI//
        public async Task<List<EnerjiMaliyetGrup>> GetEnerjiMaliyetGruplar()
        {
            return await _context.EnerjiMaliyetGrup.OrderBy(x => x.Name).ToListAsync();
        }
        public async Task<EnerjiMaliyetGrup> AddEnerjiMaliyetGrup(string name)
        {
            name = name.Trim();

            var mevcut = await _context.EnerjiMaliyetGrup.FirstOrDefaultAsync(x => x.Name == name);

            if (mevcut != null)
                return mevcut;

            var grup = new EnerjiMaliyetGrup
            {
                Name = name
            };

            await _context.EnerjiMaliyetGrup.AddAsync(grup);
            await _context.SaveChangesAsync();

            return grup;
        }

        public async Task<EnerjiMaliyetGrup> UpdateEnerjiMaliyetGrup(int id, string name)
        {
            name = name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Grup adı boş olamaz.");

            var grup = await _context.EnerjiMaliyetGrup
                .FirstOrDefaultAsync(x => x.Id == id);

            if (grup == null)
                throw new Exception("Enerji maliyet grubu bulunamadı.");

            var ayniIsimliGrupVarMi = await _context.EnerjiMaliyetGrup
                .AnyAsync(x => x.Id != id && x.Name == name);

            if (ayniIsimliGrupVarMi)
                throw new Exception("Bu isimde başka bir maliyet grubu zaten var.");

            grup.Name = name;

            await _context.SaveChangesAsync();

            return grup;
        }

        public async Task DeleteEnerjiMaliyetGrup(int id)
        {
            var grup = await _context.EnerjiMaliyetGrup
                .FirstOrDefaultAsync(x => x.Id == id);

            if (grup == null)
                throw new Exception("Enerji maliyet grubu bulunamadı.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var parametreler = await _context.EnerjiMaliyetParametreler
                    .Where(x => x.EnerjiMaliyetGrupId == id)
                    .ToListAsync();

                if (parametreler.Any())
                    _context.EnerjiMaliyetParametreler.RemoveRange(parametreler);

                var atamalar = await _context.EnerjiMaliyetParametreAtama
                    .Where(x => x.EnerjiMaliyetGrupId == id)
                    .ToListAsync();

                if (atamalar.Any())
                    _context.EnerjiMaliyetParametreAtama.RemoveRange(atamalar);

                _context.EnerjiMaliyetGrup.Remove(grup);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        //////////////////

        //Atama Metodu
        //public async Task AddEnerjiMaliyetParametreAtama(int enerjiMaliyetGrupId, int nodeId, string nodeType)
        //{
        //    var eskiAtamalar = await _context.EnerjiMaliyetParametreAtama
        //        .Where(x =>
        //            x.NodeId == nodeId &&
        //            x.NodeType == nodeType &&
        //            x.Aktif == true &&
        //            x.Silindi != true)
        //        .ToListAsync();

        //    foreach (var eski in eskiAtamalar)
        //    {
        //        eski.Aktif = false;
        //    }

        //    var yeniAtama = new EnerjiMaliyetParametreAtama
        //    {
        //        EnerjiMaliyetGrupId = enerjiMaliyetGrupId,
        //        NodeId = nodeId,
        //        NodeType = nodeType,
        //        KayitZamani = DateTime.Now,
        //        Aktif = true,
        //        Silindi = false
        //    };

        //    await _context.EnerjiMaliyetParametreAtama.AddAsync(yeniAtama);
        //    await _context.SaveChangesAsync();
        //}

        public async Task<EnerjiMaliyetAtamaTreeNodeDto?> GetEnerjiMaliyetAtamaTreeByLokasyonId(int lokasyonId, CancellationToken cancellationToken)
        {
            var lokasyon = await GetLokasyonById(lokasyonId);
            //var lokasyon = await _context.Lokasyon
            //    .Where(x => x.Id == lokasyonId && x.Silindi != true)
            //    .Select(x => new
            //    {
            //        x.Id,
            //        x.Name
            //    })
            //    .FirstOrDefaultAsync(cancellationToken);

            if (lokasyon == null)
                return null;

            var bolumler = await _context.Bolum
                .Where(x => x.LokasyonId == lokasyonId && x.Aktif != false && x.Silindi != true)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.LokasyonId
                })
                .ToListAsync(cancellationToken);

            var bolumIds = bolumler.Select(x => x.Id).ToList();

            var makineler = await _context.Makine
                .Where(x => bolumIds.Contains(x.BolumId) && x.Silindi != true)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.BolumId
                })
                .ToListAsync(cancellationToken);

            var makineIds = makineler.Select(x => x.Id).ToList();

            var cihazlar = await _context.Cihaz
                .Where(x => makineIds.Contains(x.MakineId) && x.Aktif != false && x.Silindi != true)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.MakineId
                })
                .ToListAsync(cancellationToken);

            var atamalar = await _context.EnerjiMaliyetParametreAtama
                .Where(x => x.Aktif == true && x.Silindi != true)
                .ToListAsync(cancellationToken);

            var grupIds = atamalar
                .Select(x => x.EnerjiMaliyetGrupId)
                .Distinct()
                .ToList();

            var gruplar = await _context.EnerjiMaliyetGrup
                .Where(x => grupIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

            EnerjiMaliyetParametreAtama? GetDirektAtama(int nodeId, string nodeType)
            {
                return atamalar
                    .Where(x =>
                        x.NodeId == nodeId &&
                        x.NodeType == nodeType &&
                        x.Aktif == true &&
                        x.Silindi != true)
                    .OrderByDescending(x => x.KayitZamani)
                    .FirstOrDefault();
            }

            EnerjiMaliyetAtamaTreeNodeDto BuildNode(int id, string name, string nodeType, int? inheritedGrupId)
            {
                var direktAtama = GetDirektAtama(id, nodeType);

                var aktifGrupId = direktAtama?.EnerjiMaliyetGrupId ?? inheritedGrupId;

                var direktAtamaMi = direktAtama != null;
                var mirastanMi = direktAtama == null && inheritedGrupId.HasValue;

                return new EnerjiMaliyetAtamaTreeNodeDto
                {
                    Id = id,
                    Name = name,
                    NodeType = nodeType,

                    EnerjiMaliyetGrupId = aktifGrupId,

                    EnerjiMaliyetGrupName =
                        aktifGrupId.HasValue && gruplar.ContainsKey(aktifGrupId.Value)
                            ? gruplar[aktifGrupId.Value]
                            : null,

                    DirektAtamaMi = direktAtamaMi,
                    MirastanMi = mirastanMi
                };
            }

            var root = BuildNode(
                lokasyon.Id,
                lokasyon.Name,
                TreeViewNodeTypes.Lokasyon,
                null);

            foreach (var bolum in bolumler.OrderBy(x => x.Name))
            {
                var bolumNode = BuildNode(
                    bolum.Id,
                    bolum.Name,
                    TreeViewNodeTypes.Bolum,
                    root.EnerjiMaliyetGrupId);

                var bolumeAitMakineler = makineler
                    .Where(x => x.BolumId == bolum.Id)
                    .OrderBy(x => x.Name)
                    .ToList();

                foreach (var makine in bolumeAitMakineler)
                {
                    var makineNode = BuildNode(
                        makine.Id,
                        makine.Name,
                        TreeViewNodeTypes.Makine,
                        bolumNode.EnerjiMaliyetGrupId);

                    var makineyeAitCihazlar = cihazlar
                        .Where(x => x.MakineId == makine.Id)
                        .OrderBy(x => x.Name)
                        .ToList();

                    foreach (var cihaz in makineyeAitCihazlar)
                    {
                        var cihazNode = BuildNode(
                            cihaz.Id,
                            cihaz.Name,
                            TreeViewNodeTypes.Cihaz,
                            makineNode.EnerjiMaliyetGrupId);

                        makineNode.Children.Add(cihazNode);
                    }

                    bolumNode.Children.Add(makineNode);
                }

                root.Children.Add(bolumNode);
            }

            return root;
        }
        public async Task AddEnerjiMaliyetParametreAtama(int enerjiMaliyetGrupId, int nodeId, string nodeType)
        {
            nodeType = nodeType?.Trim();

            if (string.IsNullOrWhiteSpace(nodeType))
                throw new Exception("NodeType boş olamaz.");

            var grupVarMi = await _context.EnerjiMaliyetGrup
                .AnyAsync(x => x.Id == enerjiMaliyetGrupId);

            if (!grupVarMi)
                throw new Exception("Enerji maliyet grubu bulunamadı.");

            var eskiAtamalar = await _context.EnerjiMaliyetParametreAtama
                .Where(x =>
                    x.NodeId == nodeId &&
                    x.NodeType == nodeType &&
                    x.Aktif == true &&
                    x.Silindi != true)
                .ToListAsync();

            foreach (var eski in eskiAtamalar)
            {
                eski.Aktif = false;
            }

            var yeniAtama = new EnerjiMaliyetParametreAtama
            {
                EnerjiMaliyetGrupId = enerjiMaliyetGrupId,
                NodeId = nodeId,
                NodeType = nodeType,
                KayitZamani = DateTime.Now,
                Aktif = true,
                Silindi = false
            };

            await _context.EnerjiMaliyetParametreAtama.AddAsync(yeniAtama);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveEnerjiMaliyetParametreAtama(int nodeId, string nodeType)
        {
            nodeType = nodeType?.Trim();

            var atamalar = await _context.EnerjiMaliyetParametreAtama
                .Where(x =>
                    x.NodeId == nodeId &&
                    x.NodeType == nodeType &&
                    x.Aktif == true &&
                    x.Silindi != true)
                .ToListAsync();

            foreach (var atama in atamalar)
            {
                atama.Aktif = false;
                atama.Silindi = true;
            }

            await _context.SaveChangesAsync();
        }
        //TuketimBedeli Hesaplama Metotlar
        public async Task<Dictionary<int, int>> GetCihazEnerjiMaliyetGrupMap(List<int> cihazIds, int lokasyonId, CancellationToken cancellationToken)
        {
            var result = new Dictionary<int, int>();

            if (cihazIds == null || cihazIds.Count == 0)
                return result;

            var cihazAgaclari = await _context.Cihaz
                .Where(x => cihazIds.Contains(x.Id) && x.Silindi != true)
                .Select(x => new
                {
                    CihazId = x.Id,
                    MakineId = x.MakineId,
                    BolumId = x.Makine.BolumId,
                    LokasyonId = x.Makine.Bolum.LokasyonId
                })
                .ToListAsync(cancellationToken);

            var nodeIds = new List<(int NodeId, string NodeType)>();

            foreach (var cihaz in cihazAgaclari)
            {
                nodeIds.Add((cihaz.CihazId, TreeViewNodeTypes.Cihaz));

                nodeIds.Add((cihaz.MakineId, TreeViewNodeTypes.Makine));

                nodeIds.Add((cihaz.BolumId, TreeViewNodeTypes.Bolum));
                nodeIds.Add((cihaz.LokasyonId, TreeViewNodeTypes.Lokasyon));
            }

            nodeIds = nodeIds
                .Distinct()
                .ToList();

            var atamalar = await _context.EnerjiMaliyetParametreAtama
                .Where(x =>
                    x.Aktif == true &&
                    x.Silindi != true)
                .ToListAsync(cancellationToken);

            foreach (var cihaz in cihazAgaclari)
            {
                var cihazAtama = atamalar.FirstOrDefault(x =>
                    x.NodeType == TreeViewNodeTypes.Cihaz &&
                    x.NodeId == cihaz.CihazId);

                if (cihazAtama != null)
                {
                    result[cihaz.CihazId] = cihazAtama.EnerjiMaliyetGrupId;
                    continue;
                }

                {
                    var makineAtama = atamalar.FirstOrDefault(x =>
                        x.NodeType == TreeViewNodeTypes.Makine &&
                        x.NodeId == cihaz.MakineId);

                    if (makineAtama != null)
                    {
                        result[cihaz.CihazId] = makineAtama.EnerjiMaliyetGrupId;
                        continue;
                    }
                }

                var bolumAtama = atamalar.FirstOrDefault(x =>
                    x.NodeType == TreeViewNodeTypes.Bolum &&
                    x.NodeId == cihaz.BolumId);

                if (bolumAtama != null)
                {
                    result[cihaz.CihazId] = bolumAtama.EnerjiMaliyetGrupId;
                    continue;
                }

                var lokasyonAtama = atamalar.FirstOrDefault(x =>
                    x.NodeType == TreeViewNodeTypes.Lokasyon &&
                    x.NodeId == cihaz.LokasyonId);

                if (lokasyonAtama != null)
                {
                    result[cihaz.CihazId] = lokasyonAtama.EnerjiMaliyetGrupId;
                }
            }

            return result;
        }

        public async Task<List<EnerjiMaliyetParametreler>> GetParametrelerByGrupIds(List<int> grupIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            if (grupIds == null || grupIds.Count == 0)
                return new List<EnerjiMaliyetParametreler>();

            return await _context.EnerjiMaliyetParametreler
                .Where(x =>
                    x.EnerjiMaliyetGrupId.HasValue &&
                    grupIds.Contains(x.EnerjiMaliyetGrupId.Value) &&
                    (
                        x.Yil > baslangicZamani.Year ||
                        x.Yil == baslangicZamani.Year && x.Ay >= baslangicZamani.Month
                    ) &&
                    (
                        x.Yil < bitisZamani.Year ||
                        x.Yil == bitisZamani.Year && x.Ay <= bitisZamani.Month
                    ))
                .ToListAsync(cancellationToken);
        }

        //Eski Metotlar
        public async Task<List<EnerjiMaliyetParametreler>> GetAllParametrelerByLokasyonId(int lokasyonId)
        {
            return await _context.EnerjiMaliyetParametreler.Where(x => x.LokasyonId == lokasyonId).ToListAsync();
        }

        //public async Task AddEnerjiMaliyetParametreler(List<EnerjiMaliyetParametreler> enerjiMaliyetParametreler, int seciliYil, int lokasyonId, string tarifeTipi)
        //{
        //    var eskiKayitlar = await _context.EnerjiMaliyetParametreler.Where(x => x.Yil == seciliYil && x.LokasyonId == lokasyonId).ToListAsync();
        //    if (eskiKayitlar.Any())
        //        _context.RemoveRange(eskiKayitlar);
        //    foreach (var item in enerjiMaliyetParametreler)
        //    {
        //        item.Yil = seciliYil;
        //        item.LokasyonId = lokasyonId;
        //        // =========================================
        //        // TEK ZAMANLI
        //        // =========================================
        //        if (tarifeTipi == "TEK")
        //        {
        //            item.Gunduz = null;
        //            item.Puant = null;
        //            item.Gece = null;
        //            item.Yekdem = null;
        //            item.AnlasmaOrani = null;
        //            item.TekZamanliTarimsal = true;
        //            item.UcZamanliTarimsal = false;
        //            item.Genel = false;
        //        }

        //        // =========================================
        //        // 3 ZAMANLI
        //        // =========================================
        //        else if (tarifeTipi == "UC")
        //        {
        //            item.AktifEnerjiBedeli = null;
        //            item.Yekdem = null;
        //            item.AnlasmaOrani = null;
        //            item.TekZamanliTarimsal = false;
        //            item.UcZamanliTarimsal = true;
        //            item.Genel = false;
        //        }

        //        // =========================================
        //        // GENEL
        //        // =========================================
        //        else
        //        {
        //            item.AktifEnerjiBedeli = null;
        //            item.Gunduz = null;
        //            item.Puant = null;
        //            item.Gece = null;
        //            item.GucBedeli = null;
        //            item.TekZamanliTarimsal = false;
        //            item.UcZamanliTarimsal = false;
        //            item.Genel = true;
        //        }
        //    }
        //    await _context.AddRangeAsync(enerjiMaliyetParametreler);
        //    await _context.SaveChangesAsync();
        //}

        //public async Task<List<EnerjiMaliyetParametreler>> GetEnerjiMaliyetParametreler(int seciliYil, int lokasyonId)
        //{
        //    return await _context.EnerjiMaliyetParametreler.Where(x => x.Yil == seciliYil && x.LokasyonId == lokasyonId).ToListAsync();
        //}

        //public async Task<List<TreeModel>> GetTreeByLokasyonId(int lokasyonId, string? lokasyonName)
        //{
        //    List<TreeModel> tree = new List<TreeModel>();

        //    //tree.Add(new TreeModel
        //    //{
        //    //    Id = lokasyonId,
        //    //    Name = lokasyonName,
        //    //    Expanded = false,
        //    //    Checked = false,
        //    //    Type = TreeViewNodeTypes.Lokasyon,
        //    //    Children = null,
        //    //    Write = false,
        //    //    Read = false,
        //    //});
        //    ////TODO:ileride lokasyon ve sirket de dinamik olmali
        //    //var bolumler = await _context.Bolum.Where(x => x.LokasyonId == lokasyonId && x.Silindi != true).ToListAsync();//lokasyona ait bolumler secilir
        //    //foreach (var bolum in bolumler)
        //    //{
        //    //    if (tree.Last().Children == null)
        //    //    {
        //    //        tree.Last().Children = new List<TreeModel>();
        //    //    }
        //    //    tree.Last().Children.Add(await GetAllNodesRecursive(bolum.Id, bolum.Name));
        //    //}

        //    return tree;
        //}

        public async Task<vwLokasyon> GetLokasyonById(int lokasyonId)
        {
            return await _context.vwLokasyon.AsNoTracking().FirstOrDefaultAsync(x => x.Id == lokasyonId);
        }
    }
}