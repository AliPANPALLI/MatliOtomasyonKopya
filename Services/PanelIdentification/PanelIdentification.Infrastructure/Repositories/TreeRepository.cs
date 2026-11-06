using Authentication.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PanelIdentification.Domain.Interfaces;
using PanelIdentification.Domain.Models;
using PanelIdentification.Domain.Models.ReportModels;
using SharedApp.Domain.Models;
using System.Linq;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;
using Bolum = PanelIdentification.Domain.Models.Bolum;

namespace PanelIdentification.Infrastructure.Repositories
{
    public class TreeRepository : ITreeRepository
    {
        private readonly PanoKimliklendirmeDbContext _context;

        public TreeRepository(PanoKimliklendirmeDbContext context)
        {
            _context = context;
        }

        public async Task<List<TreeModel>> GetTreeByLokasyonId(string userId, Lokasyon lokasyon, List<NodeYetkiModel> nodeYetkiler, bool onayliIslemlerDahil)
        {
            //var userYetki = await  GetTreeYetki(userId);
            List<TreeModel> tree = new List<TreeModel>();

            //var onayIslemKayitlar = await _
            var yetki = nodeYetkiler.Where(x => x.NodeId == lokasyon.Id && x.NodeType == TreeViewNodeTypes.Lokasyon).FirstOrDefault();
            tree.Add(new TreeModel
            {
                Id = lokasyon.Id,
                Name = lokasyon.Name,
                Expanded = false,
                Checked = false,
                Type = TreeViewNodeTypes.Lokasyon,
                Children = null,
                Write = yetki == null ? false : yetki.Write,
                Read = yetki == null ? false : yetki.Read,
                Responsible = yetki == null ? false : yetki.Responsible,
            });
            //TODO:ileride lokasyon ve sirket de dinamik olmali
            if (lokasyon != null)
            {
                var bolumler = await _context.Bolum.Where(x => x.LokasyonId == lokasyon.Id && x.ParentBolumId == 0 && x.Silindi != true /*&& (onayliIslemlerDahil || x.OnayIslemdeMi != true)*/).ToListAsync();//lokasyona ait parent bolumler secilir
                foreach (var bolum in bolumler)
                {
                    if (tree.Last().Children == null)
                    {
                        tree.Last().Children = new List<TreeModel>();
                    }
                    tree.Last().Children.AddRange(await GetAllNodesRecursive("bolum", bolum.Id, bolum.Name, nodeYetkiler, tree.Last().Write, tree.Last().Read, bolum.OnayIslemdeMi.GetValueOrDefault(false), userId, onayliIslemlerDahil));
                }
            }

            return tree;
        }


        public async Task<List<TreeModel>> GetAllNodesRecursive(string nodeType, int parentNodeId, string nodeName, List<NodeYetkiModel> nodeYetkiler, bool parentIsWrite, bool parentIsRead, bool parentOnayIslemDurum, string userId, bool onayliIslemlerDahil)
        {
            try
            {
                var yetki = nodeYetkiler.Where(x => x.NodeId == parentNodeId && x.NodeType == nodeType).FirstOrDefault();
                var isWrite = parentIsWrite || (yetki == null ? false : yetki.Write);
                var isRead = parentIsRead || (yetki == null ? false : yetki.Read);

                var operations = new List<TreeModel>();
                var newNode = new TreeModel();
                if (nodeType == "bolum")
                {
                    var subBolumler = await _context.Bolum.Where(x => x.ParentBolumId == parentNodeId && x.Silindi != true /*&& (onayliIslemlerDahil || x.OnayIslemdeMi != true)*/).ToListAsync();
                    if (subBolumler.Count() == 0)
                    {
                        var bekleyenOnayliIslemVarMi = await _context.OnayIslemKayit.Where(x => x.OgeId == parentNodeId && x.OgeTipi == nodeType && x.OnayaGonderildiMi != true && x.UserId == userId).ToListAsync();
                        operations.Add(new TreeModel
                        {
                            Id = parentNodeId,
                            Name = nodeName,
                            Expanded = false,
                            Checked = bekleyenOnayliIslemVarMi.Count() > 0 ? true : false,
                            Type = "bolum",
                            Children = null,
                            Write = isWrite,
                            Read = isRead,
                            OnayIslemdeMi = parentOnayIslemDurum
                        });
                    }
                    else
                    {
                        var bekleyenOnayliIslemVarMi = await _context.OnayIslemKayit.Where(x => x.OgeId == parentNodeId && x.OgeTipi == nodeType && x.OnayaGonderildiMi != true && x.UserId == userId).ToListAsync();

                        operations.Add(new TreeModel
                        {
                            Id = parentNodeId,
                            Name = nodeName,
                            Expanded = false,
                            Checked = bekleyenOnayliIslemVarMi.Count() > 0 ? true : false,
                            Type = "bolum",
                            Children = null,
                            Write = isWrite,
                            Read = isRead,
                            OnayIslemdeMi = parentOnayIslemDurum
                        });
                        foreach (var subBolum in subBolumler)
                        {
                            if (subBolum.ParentBolumId == 0)
                            {

                            }
                            else
                            {
                                var asd = await GetAllNodesRecursive("bolum", subBolum.Id, subBolum.Name, nodeYetkiler, isWrite, isRead, subBolum.OnayIslemdeMi.GetValueOrDefault(false), userId, onayliIslemlerDahil);
                                if (asd != null)
                                {
                                    if (operations.Last().Children == null)
                                    {
                                        operations.Last().Children = new List<TreeModel>();
                                    }
                                    operations.Last().Children.AddRange(asd);
                                }
                            }
                        }
                    }
                    //bolum altindaki makineler parent olan bulunur
                    var makineler = await _context.Makine.Where(x => x.BolumId == parentNodeId && x.ParentMakineId == 0 && x.Silindi != true /*&& (onayliIslemlerDahil || x.OnayIslemdeMi != true)*/).ToListAsync();
                    foreach (var makine in makineler)
                    {
                        var asd = await GetAllNodesRecursive("makine", makine.Id, makine.Name, nodeYetkiler, isWrite, isRead, makine.OnayIslemdeMi.GetValueOrDefault(false), userId, onayliIslemlerDahil);
                        if (asd != null)
                        {
                            if (operations.Last().Children == null)
                            {
                                operations.Last().Children = new List<TreeModel>();
                            }
                            operations.Last().Children.AddRange(asd);
                        }
                    }
                }
                else if (nodeType == "makine")
                {
                    var subMakineler = await _context.Makine.Where(x => x.ParentMakineId == parentNodeId && x.Silindi != true /*&& (onayliIslemlerDahil || x.OnayIslemdeMi != true)*/).ToListAsync();
                    if (subMakineler.Count() == 0)
                    {
                        var bekleyenOnayliIslemVarMi = await _context.OnayIslemKayit.Where(x => x.OgeId == parentNodeId && x.OgeTipi == nodeType && x.OnayaGonderildiMi != true && x.UserId == userId).ToListAsync();

                        operations.Add(new TreeModel
                        {
                            Id = parentNodeId,
                            Name = nodeName,
                            Expanded = false,
                            Checked = bekleyenOnayliIslemVarMi.Count() > 0 ? true : false,
                            Type = "makine",
                            Children = null,
                            Write = isWrite,
                            Read = isRead,
                            OnayIslemdeMi = parentOnayIslemDurum
                        });
                    }
                    else
                    {
                        var bekleyenOnayliIslemVarMi = await _context.OnayIslemKayit.Where(x => x.OgeId == parentNodeId && x.OgeTipi == nodeType && x.OnayaGonderildiMi != true && x.UserId == userId).ToListAsync();

                        operations.Add(new TreeModel
                        {
                            Id = parentNodeId,
                            Name = nodeName,
                            Expanded = false,
                            Checked = bekleyenOnayliIslemVarMi.Count() > 0 ? true : false,
                            Type = "makine",
                            Children = null,
                            Write = isWrite,
                            Read = isRead,
                            OnayIslemdeMi = parentOnayIslemDurum
                        });
                        foreach (var subMakine in subMakineler)
                        {
                            if (subMakine.ParentMakineId == 0)
                            {

                            }
                            else
                            {
                                var asd = await GetAllNodesRecursive("makine", subMakine.Id, subMakine.Name, nodeYetkiler, isWrite, isRead, subMakine.OnayIslemdeMi.GetValueOrDefault(false), userId, onayliIslemlerDahil);
                                if (asd != null)
                                {
                                    if (operations.Last().Children == null)
                                    {
                                        operations.Last().Children = new List<TreeModel>();
                                    }
                                    operations.Last().Children.AddRange(asd);
                                }
                            }
                        }
                    }
                    //makine altindaki panolar parent olan bulunur
                    var panoMakineEslemes = await _context.PanoMakineEsleme.Where(x => x.MakineId == parentNodeId && x.Silindi != true).ToListAsync();
                    var panolar = await _context.Pano.Where(x => panoMakineEslemes.Select(x => x.PanoId).ToList().Contains(x.Id) && x.ParentPanoId == 0 && x.Silindi != true /*&& (onayliIslemlerDahil || x.OnayIslemdeMi != true)*/).ToListAsync();
                    foreach (var pano in panolar)
                    {
                        var asd = await GetAllNodesRecursive("pano", pano.Id, pano.Name, nodeYetkiler, isWrite, isRead, pano.OnayIslemdeMi.GetValueOrDefault(false), userId, onayliIslemlerDahil);
                        if (asd != null)
                        {
                            if (operations.Last().Children == null)
                            {
                                operations.Last().Children = new List<TreeModel>();
                            }
                            operations.Last().Children.AddRange(asd);
                        }
                    }
                }
                else if (nodeType == "pano")
                {
                    var subPanolar = await _context.Pano.Where(x => x.ParentPanoId == parentNodeId && x.Silindi != true /*&& (onayliIslemlerDahil || x.OnayIslemdeMi != true)*/).ToListAsync();
                    if (subPanolar.Count() == 0)
                    {
                        var bekleyenPanoOnayliIslemVarMi = await _context.OnayIslemKayit.Where(x => x.OgeId == parentNodeId && x.OgeTipi == TreeViewNodeTypes.Pano && x.Durum == PanoKimlikOnayIslemDurum.Beklemede /*&& x.OnayaGonderildiMi != true*/).AsNoTracking().ToListAsync();
                        var bekleyenDosyaOnayliIslemVarMi = await _context.OnayIslemKayit.Where(x => x.PanoId == parentNodeId && x.OgeTipi == TreeViewNodeTypes.Dosya && x.Durum == PanoKimlikOnayIslemDurum.Beklemede/*&& x.OnayaGonderildiMi != true*/).AsNoTracking().ToListAsync();
                        var bekleyenCihazOnayliIslemVarMi = await _context.OnayIslemKayit.Where(x => x.PanoId == parentNodeId && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.Durum == PanoKimlikOnayIslemDurum.Beklemede/*&& x.OnayaGonderildiMi != true*/).AsNoTracking().ToListAsync();

                        operations.Add(new TreeModel
                        {
                            Id = parentNodeId,
                            Name = nodeName,
                            Expanded = false,
                            Checked = bekleyenPanoOnayliIslemVarMi.Where(x => x.OnayaGonderildiMi != true && x.UserId == userId).Count() > 0 ? true : false,
                            Type = "pano",
                            Write = isWrite,
                            Read = isRead,
                            OnayIslemdeMi = parentOnayIslemDurum,
                            Children = new List<TreeModel>{
                                new TreeModel(){
                                      Id = parentNodeId,
                                      Name = "Cihazlar",
                                      Checked = bekleyenCihazOnayliIslemVarMi.Where(x=> x.OnayaGonderildiMi!=true && x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "cihaz",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi = bekleyenCihazOnayliIslemVarMi.Where(x=>(x.OnayaGonderildiMi ?? false) || x.UserId==userId).Count()>0?true:false,
                                },
                                new TreeModel(){
                                      Id= parentNodeId,
                                      Name= "Resimler",
                                      Checked= bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="Resimler" && x.OnayaGonderildiMi!=true && x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "dosya",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi=bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="Resimler" && ((x.OnayaGonderildiMi ?? false) || x.UserId==userId)).Count()>0 ?true:false,
                                },
                                new TreeModel(){
                                      Id= parentNodeId,
                                      Name= "PLC",
                                      Checked= bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="PLC" && x.OnayaGonderildiMi!=true && x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "dosya",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi=bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="PLC" && ((x.OnayaGonderildiMi ?? false) || x.UserId==userId)).Count()>0 ?true:false,
                                },
                                new TreeModel(){
                                      Id= parentNodeId,
                                      Name= "HMI",
                                      Checked= bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="HMI" && x.OnayaGonderildiMi!=true && x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "dosya",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi=bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="HMI" && ((x.OnayaGonderildiMi ?? false) || x.UserId==userId)).Count()>0 ?true:false,
                                },
                                new TreeModel(){
                                      Id= parentNodeId,
                                      Name= "Elektrik Şeması",
                                      Checked= bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd == "Eletrik Şeması" && x.OnayaGonderildiMi != true && x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "dosya",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi=bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="Elektrik Şeması" && ((x.OnayaGonderildiMi ?? false) || x.UserId==userId)).Count()>0 ?true:false,
                                },
                                new TreeModel(){
                                      Id= parentNodeId,
                                      Name= "Parametreler",
                                      Checked= bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd == "Parametreler" && x.OnayaGonderildiMi != true && x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "dosya",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi=bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="Parametreler" && ((x.OnayaGonderildiMi ?? false) || x.UserId==userId)).Count()>0 ?true:false,
                                }
                            },
                        });
                    }
                    else
                    {
                        var bekleyenPanoOnayliIslemVarMi = await _context.OnayIslemKayit.Where(x => x.OgeId == parentNodeId && x.OgeTipi == TreeViewNodeTypes.Pano && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).AsNoTracking().ToListAsync();
                        var bekleyenDosyaOnayliIslemVarMi = await _context.OnayIslemKayit.Where(x => x.PanoId == parentNodeId && x.OgeTipi == TreeViewNodeTypes.Dosya && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).AsNoTracking().ToListAsync();
                        var bekleyenCihazOnayliIslemVarMi = await _context.OnayIslemKayit.Where(x => x.PanoId == parentNodeId && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).AsNoTracking().ToListAsync();

                        operations.Add(new TreeModel
                        {
                            Id = parentNodeId,
                            Name = nodeName,
                            Expanded = false,
                            Checked = bekleyenPanoOnayliIslemVarMi.Count() > 0 ? true : false,
                            Type = "pano",
                            Write = isWrite,
                            Read = isRead,
                            OnayIslemdeMi = parentOnayIslemDurum,//kontrol et
                            Children = new List<TreeModel>{
                                new TreeModel(){ Id = parentNodeId,
                                Name = "Cihazlar",
                                Checked = bekleyenCihazOnayliIslemVarMi.Where(x=>x.OnayaGonderildiMi!=true && x.UserId==userId).Count()>0?true:false,
                                Expanded= false,
                                Type= "cihaz",
                                Write = isWrite,
                                Read = isRead,
                                OnayIslemdeMi = bekleyenCihazOnayliIslemVarMi.Where(x=>(x.OnayaGonderildiMi ?? false) || x.UserId==userId).Count()>0?true:false,
                                },
                                new TreeModel(){
                                      Id= parentNodeId,
                                      Name= "Resimler",
                                      Checked= bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="Resimler" && x.OnayaGonderildiMi != true && x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "dosya",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi=bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="Resimler" && ((x.OnayaGonderildiMi ?? false) || x.UserId==userId)).Count()>0?true:false,
                                },
                                new TreeModel(){
                                      Id= parentNodeId,
                                      Name= "PLC",
                                      Checked= bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="PLC" && x.OnayaGonderildiMi!=true&& x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "dosya",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi=bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="PLC" && ((x.OnayaGonderildiMi ?? false) || x.UserId==userId)).Count()>0?true:false,
                                },
                                new TreeModel(){
                                      Id= parentNodeId,
                                      Name= "HMI",
                                      Checked= bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="HMI"&& x.OnayaGonderildiMi!=true&& x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "dosya",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi=bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="HMI" && ((x.OnayaGonderildiMi ?? false) || x.UserId==userId)).Count()>0?true:false,
                                },
                                new TreeModel(){
                                      Id= parentNodeId,
                                      Name= "Elektrik Şeması",
                                      Checked= bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="Elektrik Şeması"&& x.OnayaGonderildiMi!=true&& x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "dosya",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi=bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="Elektrik Şeması" && ((x.OnayaGonderildiMi ?? false) || x.UserId==userId)).Count()>0?true:false,
                                },
                                new TreeModel(){
                                      Id= parentNodeId,
                                      Name= "Parametreler",
                                      Checked= bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="Parametreler"&& x.OnayaGonderildiMi!=true&& x.UserId==userId).Count()>0?true:false,
                                      Expanded= false,
                                      Type= "dosya",
                                      Write = isWrite,
                                      Read = isRead,
                                      OnayIslemdeMi=bekleyenDosyaOnayliIslemVarMi.Where(x=>x.KlasorAd=="Parametreler" && ((x.OnayaGonderildiMi ?? false) || x.UserId==userId)).Count()>0?true:false,
                                }
                            },
                        });
                        foreach (var subMakine in subPanolar)
                        {
                            if (subMakine.ParentPanoId == 0)
                            {

                            }
                            else
                            {
                                var asd = await GetAllNodesRecursive("pano", subMakine.Id, subMakine.Name, nodeYetkiler, isWrite, isRead, subMakine.OnayIslemdeMi.GetValueOrDefault(false), userId, onayliIslemlerDahil);
                                if (asd != null)
                                {
                                    if (operations.Last().Children == null)
                                    {
                                        operations.Last().Children = new List<TreeModel>();
                                    }
                                    operations.Last().Children.AddRange(asd);
                                }
                            }
                        }
                    }
                }
                if (operations.Last().Children != null)
                {
                    operations.Last().Children = operations.Last().Children
                        .OrderBy(node => node.Type == "pano" ? 0 : 1) // 'pano' önce gelir, sonra 'makine' gelir
                        .ToList();
                }

                return operations;
            }
            catch (Exception ex)
            {

                throw;
            }
            // Burada sıralamayı yapıyoruz: 'pano' önce gelsin, sonra 'makine' gelsin

        }

        public async Task<Bolum?> GetBolumByPanoId(int panoId)
        {
            // İlk olarak Pano'yu ve onun parent'ını kontrol ediyoruz
            var pano = await _context.Pano.FirstOrDefaultAsync(p => p.Id == panoId && p.Silindi != true);
            if (pano == null)
            {
                return null; // Pano bulunamadıysa null döndürüyoruz
            }

            while (pano.ParentPanoId != 0)
            {
                pano = await _context.Pano.FirstOrDefaultAsync(p => p.Id == pano.ParentPanoId && p.Silindi != true);
                if (pano == null)
                {
                    return null; // Parent Pano bulunamadıysa null döndürüyoruz
                }
            }

            // Pano'dan Makine'yi ve onun parent'ını kontrol ediyoruz
            var panoMakineEsleme = await _context.PanoMakineEsleme.FirstOrDefaultAsync(pme => pme.PanoId == pano.Id && pme.Silindi != true);
            if (panoMakineEsleme == null)
            {
                return null; // Makine bulunamadıysa null döndürüyoruz
            }

            var makine = await _context.Makine.FirstOrDefaultAsync(m => m.Id == panoMakineEsleme.MakineId && m.Silindi != true);
            if (makine == null)
            {
                return null; // Makine bulunamadıysa null döndürüyoruz
            }

            while (makine.ParentMakineId != 0)
            {
                makine = await _context.Makine.FirstOrDefaultAsync(m => m.Id == makine.ParentMakineId && m.Silindi != true);
                if (makine == null)
                {
                    return null; // Parent Makine bulunamadıysa null döndürüyoruz
                }
            }

            // Makine'den Bölüm'ü ve onun parent'ını kontrol ediyoruz
            var bolum = await _context.Bolum.FirstOrDefaultAsync(b => b.Id == makine.BolumId && b.Silindi != true);
            if (bolum == null)
            {
                return null; // Bölüm bulunamadıysa null döndürüyoruz
            }

            while (bolum.ParentBolumId != 0)
            {
                bolum = await _context.Bolum.FirstOrDefaultAsync(b => b.Id == bolum.ParentBolumId && b.Silindi != true);
                if (bolum == null)
                {
                    return null; // Parent Bolum bulunamadıysa null döndürüyoruz
                }
            }
            return bolum;
        }

        public async Task<List<FilesModel>> GetDosyalarByPanoId(int panoId, string dosya)
        {
            var files = new List<FilesModel>();
            var dosyaYolu = (await _context.Pano.Where(x => x.Id == panoId && x.Silindi != true).FirstOrDefaultAsync()).DosyaYolu;
            if (!string.IsNullOrEmpty(dosyaYolu))
            {

                dosyaYolu += "\\" + dosya;
                var dosyalar = Directory.GetFiles(dosyaYolu);
                foreach (var dosy in dosyalar)
                {
                    var fileImg = "";
                    FileInfo f = new FileInfo(dosy);
                    if (f.Extension == ".txt")
                        fileImg = txtImg;
                    else if (f.Extension == ".jpg" || f.Extension == ".jpeg" || f.Extension == ".gif" || f.Extension == ".svg" || f.Extension == ".png")
                        fileImg = imageImg;
                    else if (f.Extension == ".pdf")
                        fileImg = pdfImg;
                    else if (f.Extension == ".rar" || f.Extension == ".zip")
                        fileImg = rarImg;
                    else if (f.Extension == ".exe")
                        fileImg = exeImg;
                    else
                        fileImg = otherImg;

                    files.Add(new FilesModel
                    {
                        PanoId = panoId,
                        FileName = f.Name,
                        FileDate = new DateTime(f.CreationTimeUtc.Year, f.CreationTimeUtc.Month, f.CreationTimeUtc.Day),
                        FileDirectory = f.DirectoryName,
                        FileExtension = f.Extension,
                        FileUnicode = fileImg
                    });
                }
                var klasörler = Directory.GetDirectories(dosyaYolu);
                foreach (var klasör in klasörler)
                {
                    FileInfo f = new FileInfo(klasör);
                    files.Add(new FilesModel
                    {
                        PanoId = panoId,
                        FileName = f.Name,
                        FileDate = new DateTime(f.CreationTimeUtc.Year, f.CreationTimeUtc.Month, f.CreationTimeUtc.Day),
                        FileDirectory = f.DirectoryName,
                        FileExtension = "Klasor",
                        FileUnicode = folderImg
                    });
                }
            }

            return files;
        }
        public async Task<List<FilesModel>> GetAllDosyalarByPanoId(List<NodeYetkiModel> nodeYetkiler, string userId, int panoId, string dosya, bool onayliIslemlerDahil = false)
        {
            string ResolveIcon(string ext)
            {
                ext = (ext ?? "").ToLowerInvariant();
                if (ext == ".txt") return txtImg;
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".svg" || ext == ".png") return imageImg;
                if (ext == ".pdf") return pdfImg;
                if (ext == ".rar" || ext == ".zip") return rarImg;
                if (ext == ".exe") return exeImg;
                return otherImg;
            }
            var files = new List<FilesModel>();

            var dosyaYolu = (await _context.Pano.Where(x => x.Id == panoId && x.Silindi != true).FirstOrDefaultAsync()).DosyaYolu;
            var folderPath = dosyaYolu;
            var folderName = dosya;            // "Resimler", "PLC" vb
            var fullFolderPath = !string.IsNullOrEmpty(folderPath) ? Path.Combine(folderPath, folderName) : null;
            bool sorumluMu = false;
            var yetki = nodeYetkiler.Where(x => x.NodeType == TreeViewNodeTypes.Pano && x.NodeId == panoId).FirstOrDefault();
            if (yetki != null)
            {
                sorumluMu = yetki.Responsible;
            }
            // 1) DISKTEN: sözlük oluştur
            var dict = new Dictionary<string, FilesModel>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(fullFolderPath) && Directory.Exists(fullFolderPath))
            {
                foreach (var file in Directory.GetFiles(fullFolderPath))
                {
                    var fi = new FileInfo(file);
                    var key = fi.DirectoryName + "|" + fi.Name;

                    dict[key] = new FilesModel
                    {
                        PanoId = panoId,
                        FileName = fi.Name,
                        FileDate = new DateTime(fi.CreationTimeUtc.Year, fi.CreationTimeUtc.Month, fi.CreationTimeUtc.Day),
                        FileDirectory = fi.DirectoryName,
                        FileExtension = fi.Extension,
                        FileUnicode = ResolveIcon(fi.Extension),
                        OnayIslemdeMi = false,
                        ShowOnaylaButton = false
                    };
                }

                // klasörleri ekle (onay mantığı klasörlere uygulanmıyor)
                foreach (var dir in Directory.GetDirectories(fullFolderPath))
                {
                    var di = new DirectoryInfo(dir);
                    var key = di.Parent?.FullName + "|" + di.Name;
                    dict[key] = new FilesModel
                    {
                        PanoId = panoId,
                        FileName = di.Name,
                        FileDate = new DateTime(di.CreationTimeUtc.Year, di.CreationTimeUtc.Month, di.CreationTimeUtc.Day),
                        FileDirectory = di.Parent?.FullName,
                        FileExtension = "Klasor",
                        FileUnicode = folderImg,
                        OnayIslemdeMi = false,
                        ShowOnaylaButton = false
                    };
                }
            }
            if (onayliIslemlerDahil)
            {
                // 2) ONAY KAYITLARI: tek sorgu, tek yerden yönet
                var bekleyenOps = await _context.OnayIslemKayit
                    .AsNoTracking()
                    .Where(x => x.PanoId == panoId
                        && x.OgeTipi == TreeViewNodeTypes.Dosya
                        && x.Durum == PanoKimlikOnayIslemDurum.Beklemede
                        && x.KlasorAd == folderName)                 // sadece ilgili klasör
                    .OrderByDescending(x => x.KayitZamani)           // en yeni en başta
                    .ToListAsync();

                // Grup = aynı dosya
                var groups = bekleyenOps.GroupBy(x => new { x.DosyaAd, x.KlasorAd });

                foreach (var g in groups)
                {
                    var dosyaAd = g.Key.DosyaAd;           // "testmetin.txt"
                    var klasorAd = g.Key.KlasorAd;         // "Resimler"
                                                           // En son Ekleme/Düzenleme/Silme
                    var ekleme = g.FirstOrDefault(x => x.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme);
                    var silme = g.FirstOrDefault(x => x.IslemTipi == PanoKimlikOnayIslemTipi.Silme);
                    var duzenleme = g.FirstOrDefault(x => x.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme);

                    // Diskte var mı?
                    var expectedDir = fullFolderPath;                      // klasörün tam yolu
                    var existsKey = (expectedDir ?? "") + "|" + dosyaAd;
                    bool existsOnDisk = dict.ContainsKey(existsKey) && dict[existsKey].FileExtension != "Klasor";

                    // -- EKLEME: dosya henüz diskte yoksa hayalet satır ekle
                    if (ekleme != null && !existsOnDisk)
                    {
                        var data = JsonConvert.DeserializeObject<DosyaOnayIslemModel>(ekleme.IslemVerisi);
                        var tempPath = data?.TempDosyaYolu;
                        var fi = !string.IsNullOrEmpty(tempPath) ? new FileInfo(tempPath) : new FileInfo(Path.Combine(expectedDir ?? "", dosyaAd));

                        var key = (fi.DirectoryName ?? expectedDir ?? "") + "|" + fi.Name;
                        dict[key] = new FilesModel
                        {
                            PanoId = panoId,
                            FileName = fi.Name,
                            FileDate = new DateTime(fi.CreationTimeUtc.Year, fi.CreationTimeUtc.Month, fi.CreationTimeUtc.Day),
                            FileDirectory = fi.DirectoryName ?? expectedDir,
                            FileExtension = fi.Extension,
                            FileUnicode = ResolveIcon(fi.Extension),
                            OnayIslemdeMi = true,
                            OnayIslemKayitId = ekleme.Id,
                            ShowOnaylaButton = true
                        };

                        // Bir dosyada birden fazla ekleme kaydı varsa (örnek verilerindeki 156/159), en yenisini aldık; diğerlerini yok sayıyoruz.
                        continue; // ekleme varsa ve dosya yoksa diğerleri görsel olarak anlamlı değil
                    }

                    // -- SILME: diskte varsa dosyanın satırını işaretle
                    if (silme != null && existsOnDisk)
                    {
                        var row = dict[existsKey];
                        row.OnayIslemdeMi = true;
                        row.OnayIslemKayitId = silme.Id;
                        row.ShowOnaylaButton = true;
                        continue; // silme baskın olsun (düzenlemeyi ezsin)
                    }

                    // -- DÜZENLEME: varsa dosyayı işaretle (rename/content)
                    if (duzenleme != null)
                    {
                        var data = JsonConvert.DeserializeObject<DosyaOnayIslemModel>(duzenleme.IslemVerisi);

                        // rename senaryosu
                        var oldName = data?.EskiDosyaAd;
                        var newName = data?.YeniDosyaAd;
                        var targetDir = data?.DosyaYolu ?? expectedDir;

                        if (!string.IsNullOrEmpty(oldName))
                        {
                            var oldKey = (targetDir ?? expectedDir ?? "") + "|" + oldName;
                            if (dict.ContainsKey(oldKey))
                            {
                                var row = dict[oldKey];
                                row.OnayIslemdeMi = true;
                                row.OnayIslemKayitId = duzenleme.Id;
                                row.ShowOnaylaButton = sorumluMu;
                            }
                            else if (!string.IsNullOrEmpty(newName))
                            {
                                // eski ad sözlükte yoksa (dosya taşınmış/yok), yeni adla hayalet oluştur
                                var newKey = (targetDir ?? expectedDir ?? "") + "|" + newName;
                                var fiGhost = new FileInfo(Path.Combine(targetDir ?? expectedDir ?? "", newName));
                                dict[newKey] = new FilesModel
                                {
                                    PanoId = panoId,
                                    FileName = fiGhost.Name,
                                    FileDate = DateTime.Now.Date,
                                    FileDirectory = targetDir ?? expectedDir,
                                    FileExtension = fiGhost.Extension,
                                    FileUnicode = ResolveIcon(fiGhost.Extension),
                                    OnayIslemdeMi = true,
                                    OnayIslemKayitId = duzenleme.Id,
                                    ShowOnaylaButton = true
                                };
                            }
                        }
                        else
                        {
                            // içerik düzenleme: mevcut adıyla işaretle
                            if (existsOnDisk)
                            {
                                var row = dict[existsKey];
                                row.OnayIslemdeMi = true;
                                row.OnayIslemKayitId = duzenleme.Id;
                                row.ShowOnaylaButton = true;
                            }
                            else
                            {
                                // dosya görünmüyorsa (ör: üstte yanlış yerde oluşturulmuşsa) hayalet ekle
                                var fiGhost = new FileInfo(Path.Combine(expectedDir ?? "", dosyaAd));
                                dict[existsKey] = new FilesModel
                                {
                                    PanoId = panoId,
                                    FileName = fiGhost.Name,
                                    FileDate = DateTime.Now.Date,
                                    FileDirectory = expectedDir,
                                    FileExtension = fiGhost.Extension,
                                    FileUnicode = ResolveIcon(fiGhost.Extension),
                                    OnayIslemdeMi = true,
                                    OnayIslemKayitId = duzenleme.Id,
                                    ShowOnaylaButton = true
                                };
                            }
                        }
                    }
                }
            }
            // Sonuç listesi
            files = dict.Values.ToList();

            return files;
        }

        public async Task<List<FilesModel>> GetDosyalarByDosyaYolu(List<NodeYetkiModel> nodeYetkiler, string userId, string dosyaYolu, int panoId, bool? ustKlasorVarMi, bool onayliIslemlerDahil = false)
        {
            var files = new List<FilesModel>();

            // 🔹 Üst klasör satırı ekle
            if (ustKlasorVarMi.GetValueOrDefault(true))
            {
                files.Add(new FilesModel
                {
                    FileName = "..",
                    FileDate = null,
                    FileDirectory = dosyaYolu,
                    FileExtension = "UstKlasor",
                    FileUnicode = folderImg,
                    OnayIslemdeMi = false,
                    ShowOnaylaButton = false
                });
            }

            // 🔹 Kullanıcının sorumlu olup olmadığını bul
            bool sorumluMu = nodeYetkiler.Any(x => x.NodeType == TreeViewNodeTypes.Pano && x.NodeId == panoId && x.Responsible);

            // 🔹 Dosyaları sözlük şeklinde topla
            var dict = new Dictionary<string, FilesModel>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(dosyaYolu) && Directory.Exists(dosyaYolu))
            {
                foreach (var dosy in Directory.GetFiles(dosyaYolu))
                {
                    var f = new FileInfo(dosy);
                    dict[f.Name] = new FilesModel
                    {
                        PanoId = panoId,
                        FileName = f.Name,
                        FileDate = f.CreationTimeUtc,
                        FileDirectory = f.DirectoryName,
                        FileExtension = f.Extension,
                        FileUnicode = ResolveFileIcon(f.Extension),
                        OnayIslemdeMi = false,
                        ShowOnaylaButton = false
                    };
                }

                foreach (var klasor in Directory.GetDirectories(dosyaYolu))
                {
                    var f = new DirectoryInfo(klasor);
                    dict[f.Name] = new FilesModel
                    {
                        PanoId = panoId,
                        FileName = f.Name,
                        FileDate = f.CreationTimeUtc,
                        FileDirectory = f.Parent?.FullName ?? dosyaYolu,
                        FileExtension = "Klasor",
                        FileUnicode = folderImg,
                        OnayIslemdeMi = false,
                        ShowOnaylaButton = false
                    };
                }
            }

            // 🔹 Onaylı işlemleri dahil etmek istiyorsak, bekleyen işlemleri çek
            if (onayliIslemlerDahil)
            {
                var bekleyenIslemler = await _context.OnayIslemKayit
                    .AsNoTracking()
                    .Where(x =>
                        x.PanoId == panoId &&
                        x.OgeTipi == TreeViewNodeTypes.Dosya &&
                        x.Durum == PanoKimlikOnayIslemDurum.Beklemede &&
                        (x.UserId == userId || (x.OnayaGonderildiMi ?? false && sorumluMu)))
                    .OrderByDescending(x => x.KayitZamani)
                    .ToListAsync();

                // Aynı dosya adlarını grupla → sadece son işlemi al
                var grouped = bekleyenIslemler
                    .GroupBy(x => (x.DosyaAd?.ToLowerInvariant() ?? ""))
                    .Select(g => g.First())
                    .ToList();

                foreach (var islem in grouped)
                {
                    try
                    {
                        var actionData = JsonConvert.DeserializeObject<DosyaOnayIslemModel>(islem.IslemVerisi);
                        string? tempPath = actionData?.TempDosyaYolu;
                        string? finalPath = actionData?.FinalDosyaYolu ?? actionData?.DosyaYolu;

                        var fileName = islem.DosyaAd ?? Path.GetFileName(finalPath ?? tempPath ?? "");
                        if (string.IsNullOrEmpty(fileName))
                            continue;

                        // 🔸 Dosya zaten listede varsa, güncelle
                        if (dict.ContainsKey(fileName))
                        {
                            var item = dict[fileName];
                            item.OnayIslemdeMi = true;
                            item.OnayIslemKayitId = islem.Id;
                            item.ShowOnaylaButton = true;
                        }
                        else
                        {
                            // 🔸 Dosya diskte yok ama onay bekliyorsa hayalet satır olarak ekle
                            var f = !string.IsNullOrEmpty(tempPath) && File.Exists(tempPath)
                                ? new FileInfo(tempPath)
                                : new FileInfo(finalPath ?? Path.Combine(dosyaYolu, fileName));

                            dict[fileName] = new FilesModel
                            {
                                PanoId = panoId,
                                FileName = f.Name,
                                FileDate = DateTime.Now,
                                FileDirectory = f.DirectoryName ?? dosyaYolu,
                                FileExtension = f.Extension,
                                FileUnicode = ResolveFileIcon(f.Extension),
                                OnayIslemdeMi = true,
                                OnayIslemKayitId = islem.Id,
                                ShowOnaylaButton = true
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        // JSON parse hatası varsa devam et (loglanabilir)
                        continue;
                    }
                }
            }

            return dict.Values
                .OrderByDescending(f => f.FileExtension != "Klasor") // klasörler önce
                .ThenBy(f => f.FileName)
                .ToList();
        }

        // 🔹 Yardımcı ikon çözüm metodu
        private string ResolveFileIcon(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return otherImg;
            extension = extension.ToLowerInvariant();

            return extension switch
            {
                ".txt" => txtImg,
                ".jpg" or ".jpeg" or ".gif" or ".svg" or ".png" => imageImg,
                ".pdf" => pdfImg,
                ".rar" or ".zip" => rarImg,
                ".exe" => exeImg,
                _ => otherImg
            };
        }

        public async Task<List<CihazModel>> GetCihazlarByPanoId(List<NodeYetkiModel> nodeYetkiler, int panoId, string userId, bool onayliIslemlerDahil)
        {
            var result = new List<CihazModel>();

            // Ortak: pano ve silinmemiş
            var cihazlarBase = _context.Cihaz
                .Where(x => x.PanoId == panoId && x.Silindi != true);

            if (onayliIslemlerDahil)
            {
                // Düzenleme modu: tüm cihazları getir, butonu bekleyene göre aç
                var cihazlar = await cihazlarBase.ToListAsync();

                // Kullanıcının kendi bekleyenleri
                var bekleyenOgeIds = await _context.OnayIslemKayit
                    .Where(x =>
                        x.UserId == userId &&
                        x.OgeTipi == TreeViewNodeTypes.Cihaz &&
                        x.Durum == PanoKimlikOnayIslemDurum.Beklemede)
                    .Select(x => x.OgeId.Value)
                    .ToListAsync();
                var bekleyenSet = new HashSet<int>(bekleyenOgeIds);

                // Sorumlu ise onaya gönderilmiş olanlar (Beklemede + OnayaGonderildiMi)
                var sorumluPanoIdler = nodeYetkiler
                    .Where(y => y.NodeType == TreeViewNodeTypes.Pano && y.Responsible)
                    .Select(y => y.NodeId)
                    .ToList();

                var onayaGonderilmisOgeIds = await _context.OnayIslemKayit
                    .Where(x =>
                        x.OgeTipi == TreeViewNodeTypes.Cihaz &&
                        x.Durum == PanoKimlikOnayIslemDurum.Beklemede &&
                        (x.OnayaGonderildiMi ?? false) &&
                        sorumluPanoIdler.Contains(x.PanoId.GetValueOrDefault(0)))
                    .Select(x => x.OgeId.Value)
                    .ToListAsync();
                var onayaGonderilmisSet = new HashSet<int>(onayaGonderilmisOgeIds);

                foreach (var c in cihazlar)
                {
                    result.Add(new CihazModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Marka = c.Marka,
                        UrunKodu = c.UrunKodu,
                        IP = c.IP,
                        OnayIslemdeMi = c.OnayIslemdeMi,
                        PanoId = c.PanoId,
                        Type = "cihaz",
                        ShowOnaylaButton = bekleyenSet.Contains(c.Id) || onayaGonderilmisSet.Contains(c.Id)
                    });
                }
            }
            else
            {
                // DÜZENLEME MODU DEĞİL: sadece ONAYLANMIŞ cihazlar (OnayIslemdeMi != true)
                // Not: Ekleme onayı bekleyen satırlar genelde OnayIslemdeMi = true olur, bu filtre ile gelmezler.
                var onaylanmisCihazlar = await cihazlarBase
                    .Where(x => x.OnayIslemdeMi != true)  // sadece onaylanmışlar
                    .ToListAsync();

                foreach (var c in onaylanmisCihazlar)
                {
                    result.Add(new CihazModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Marka = c.Marka,
                        UrunKodu = c.UrunKodu,
                        IP = c.IP,
                        OnayIslemdeMi = c.OnayIslemdeMi,
                        PanoId = c.PanoId,
                        Type = "cihaz",
                        // Düzenleme modu değilken onay butonu göstermiyoruz
                        ShowOnaylaButton = false
                    });
                }
            }

            return result;
        }

        //public async Task<bool> DeleteNodeFromTree(NodeModel node)
        //{
        //    if (node.OldNodeType == TreeViewNodeTypes.Bolum)
        //    {
        //        var oldNode = await _context.Bolum.Where(x => x.Id.ToString() == node.OldNodeId && x.Silindi != true).FirstOrDefaultAsync();
        //    }
        //    return true;
        //}
    }
}