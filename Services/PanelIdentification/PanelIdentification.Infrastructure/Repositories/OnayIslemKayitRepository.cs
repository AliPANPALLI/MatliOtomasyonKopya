using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using PanelIdentification.Domain.DbViewModels;
using PanelIdentification.Domain.Interfaces;
using PanelIdentification.Domain.Models;
using PanelIdentification.Domain.Models.ReportModels;
using PanelIdentification.Infrastructure.Repositories.Core;
using static SharedKernel.ApplicationConstants;

namespace PanelIdentification.Infrastructure.Repositories
{
    public class OnayIslemKayitRepository : Repository<OnayIslemKayit>, IOnayIslemKayitRepository
    {
        private readonly PanoKimliklendirmeDbContext _context;
        public OnayIslemKayitRepository(PanoKimliklendirmeDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> IslemOnayla(int id, string userId)
        {
            //Üst düğümlerde bekleyen işlem var mı diye kontrol edilir. Önce üstteki düğümlerin işlemleri kapatılmalı.
            var islem = await _context.OnayIslemKayit.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (islem == null || islem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");
            var parentNodes = new List<TreeModel>();
            if (islem.OgeTipi == TreeViewNodeTypes.Dosya || islem.OgeTipi == TreeViewNodeTypes.Cihaz)
            {
                parentNodes.AddRange(await GetParentNodesById((int)islem.PanoId, TreeViewNodeTypes.Pano));
            }
            else
            {
                parentNodes.AddRange(await GetParentNodesById((int)islem.OgeId, islem.OgeTipi));
            }
            foreach (var node in parentNodes)
            {
                var islemVarMi = await _context.OnayIslemKayit.Where(x => x.OgeId == node.Id && x.OgeTipi == node.Type && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                if (islemVarMi.Count() > 0)
                {
                    throw new Exception("Önce üst düğümlerdeki bekleyen değişiklikler kapatılmalıdır!");
                }
            }

            if (islem.OgeTipi == TreeViewNodeTypes.Bolum)
            {

                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                {
                    var bolum = JsonConvert.DeserializeObject<Bolum>(islem.IslemVerisi);
                    //await _context.Bolum.AddAsync(bolum);
                    var dbBolum = await _context.Bolum.Where(x => x.Id == bolum.Id).FirstOrDefaultAsync();
                    if (dbBolum != null)
                    {
                        dbBolum.OnayIslemdeMi = false;
                    }
                }
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                {
                    var bolum = await _context.Bolum.FindAsync(islem.OgeId);
                    if (bolum != null)
                    {
                        var updatedData = JsonConvert.DeserializeObject<Bolum>(islem.IslemVerisi);
                        _context.Update(updatedData);
                        bolum.OnayIslemdeMi = false;
                    }
                }
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                {
                    var bolum = await _context.Bolum.FindAsync(islem.OgeId);
                    if (bolum != null)
                    {
                        var bolumVarMi = await _context.Bolum.Where(x => x.ParentBolumId == bolum.Id && x.Silindi != true).ToListAsync();

                        //alt ögesi varsa silinemez
                        var makineVarMi = await _context.Makine.Where(x => x.BolumId == bolum.Id && x.Silindi != true).ToListAsync();
                        if (makineVarMi.Count() == 0 && bolumVarMi.Count() == 0)
                        {
                            bolum.Silindi = true;
                            bolum.OnayIslemdeMi = false;
                        }
                        else
                        {
                            throw new Exception("Bölüm silinemez! Çünkü önce alt düğümleri silinmelidir!");
                        }
                    }
                }
            }
            if (islem.OgeTipi == TreeViewNodeTypes.Makine)
            {
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                {
                    var makine = JsonConvert.DeserializeObject<Makine>(islem.IslemVerisi);
                    //await _context.Makine.AddAsync(makine);
                    var dbMakine = await _context.Makine.Where(x => x.Id == makine.Id).FirstOrDefaultAsync();
                    if (dbMakine != null)
                    {
                        dbMakine.OnayIslemdeMi = false;
                    }
                }
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                {
                    var makine = await _context.Makine.FindAsync(islem.OgeId);
                    if (makine != null)
                    {
                        var updatedData = JsonConvert.DeserializeObject<Makine>(islem.IslemVerisi);
                        _context.Entry(makine).CurrentValues.SetValues(updatedData);

                        //makine = updatedData;
                        //_context.Update(updatedData);
                        makine.OnayIslemdeMi = false;
                    }
                }
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                {
                    var makine = await _context.Makine.FindAsync(islem.OgeId);
                    if (makine != null)
                    {
                        //alt ögesi varsa silinemez
                        var makineVarMi = await _context.Makine.Where(x => x.ParentMakineId == makine.Id && x.Silindi != true).ToListAsync();
                        var panoVarMi = await _context.PanoMakineEsleme.Where(x => x.MakineId == makine.Id && x.Silindi != true).ToListAsync();

                        if (makineVarMi.Count() == 0 && panoVarMi.Count() == 0)
                        {
                            makine.Silindi = true;
                            makine.OnayIslemdeMi = false;
                        }
                        else
                        {
                            throw new Exception("Makine silinemez! Çünkü önce alt düğümleri silinmelidir!");
                        }
                    }
                }
            }
            if (islem.OgeTipi == TreeViewNodeTypes.Pano)
            {
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                {
                    var pano = JsonConvert.DeserializeObject<Pano>(islem.IslemVerisi);
                    //await _context.Pano.AddAsync(pano);
                    var dbPano = await _context.Pano.Where(x => x.Id == pano.Id).FirstOrDefaultAsync();
                    if (dbPano != null)
                    {
                        dbPano.OnayIslemdeMi = false;
                    }
                }
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                {
                    var pano = await _context.Pano.FindAsync(islem.OgeId);
                    if (pano != null)
                    {
                        var updatedData = JsonConvert.DeserializeObject<Pano>(islem.IslemVerisi);
                        _context.Entry(pano).CurrentValues.SetValues(updatedData);
                        pano.OnayIslemdeMi = false;
                    }
                }
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                {
                    var pano = await _context.Pano.FindAsync(islem.OgeId);
                    if (pano != null)
                    {
                        //alt ögesi varsa silinemez
                        var panoVarMi = await _context.Pano.Where(x => x.ParentPanoId == pano.Id && x.Silindi != true).ToListAsync();
                        if (panoVarMi.Count() == 0)
                        {
                            pano.Silindi = true;
                            pano.OnayIslemdeMi = false;
                            var panoMakineEslemeler = await _context.PanoMakineEsleme.Where(x => x.PanoId == pano.Id && x.Silindi != true).ToListAsync();
                            foreach (var panoMakineEsleme in panoMakineEslemeler)
                            {
                                panoMakineEsleme.Silindi = true;
                            }
                            var cihazlar = await _context.Cihaz.Where(x => x.PanoId == id && x.Silindi != true).ToListAsync();
                            foreach (var cihaz in cihazlar)
                            {
                                cihaz.Silindi = true;
                            }
                        }
                        else
                        {
                            throw new Exception("Pano silinemez! Çünkü önce alt düğümleri silinmelidir!");
                        }
                    }
                }
            }
            if (islem.OgeTipi == TreeViewNodeTypes.Cihaz)
            {
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                {
                    var cihaz = JsonConvert.DeserializeObject<Cihaz>(islem.IslemVerisi);
                    //await _context.Cihaz.AddAsync(cihaz);
                    var dbCihaz = await _context.Cihaz.Where(x => x.Id == cihaz.Id).FirstOrDefaultAsync();
                    if (dbCihaz != null)
                    {
                        dbCihaz.OnayIslemdeMi = false;
                    }
                }
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                {
                    var cihaz = await _context.Cihaz.FindAsync(islem.OgeId);
                    if (cihaz != null)
                    {
                        var updatedData = JsonConvert.DeserializeObject<Cihaz>(islem.IslemVerisi);
                        _context.Entry(cihaz).CurrentValues.SetValues(updatedData);
                        cihaz.OnayIslemdeMi = false;
                    }
                }
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                {
                    var cihaz = await _context.Cihaz.FindAsync(islem.OgeId);
                    if (cihaz != null)
                    {
                        cihaz.Silindi = true;
                        cihaz.OnayIslemdeMi = false;
                    }
                }
            }
            else if (islem.OgeTipi == TreeViewNodeTypes.Dosya)
            {
                await DosyaIslemOnayla(islem.Id, userId);
            }

            islem.Durum = PanoKimlikOnayIslemDurum.Kabul;
            islem.GuncellemeZamani = DateTime.Now;
            islem.GuncelleyenUserId = userId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AltDugumlerSil(List<TreeModel> subNodes)
        {
            //TODO:kullan veya sil
            //Include Kullanımı: EF Core'da Include ile ilişkili veriler bir seferde çekilebilir. Örneğin:
            //var subNodesData = await _context.Bolum
            //    .Include(b => b.Makine)
            //    .ThenInclude(m => m.Pano)
            //    .ToListAsync();
            //Toplu Güncelleme: Tek tek güncelleme yerine toplu bir işlem yapılabilir. Örneğin:           
            //await _context.Bolum
            //    .Where(b => subNodeIds.Contains(b.Id))
            //    .UpdateAsync(b => new Bolum { Silindi = true });

            try
            {
                foreach (var subNode in subNodes)
                {
                    if (subNode.Type == TreeViewNodeTypes.Bolum)
                    {
                        var bolum = await _context.Bolum.Where(x => x.Id == subNode.Id).FirstOrDefaultAsync();
                        bolum.Silindi = true;
                    }
                    else if (subNode.Type == TreeViewNodeTypes.Makine)
                    {
                        var makine = await _context.Makine.Where(x => x.Id == subNode.Id).FirstOrDefaultAsync();
                        if (makine != null)
                        {
                            var panoMakineEslemeler = await _context.PanoMakineEsleme.Where(x => x.MakineId == makine.Id && x.Silindi != true).ToListAsync();
                            foreach (var panoMakineEsleme in panoMakineEslemeler)
                            {
                                panoMakineEsleme.Silindi = true;
                            }

                            var panolar = await _context.Pano.Where(x => panoMakineEslemeler.Select(y => y.PanoId).ToList().Contains(x.Id) && x.Silindi != true).ToListAsync();
                            foreach (var pano in panolar)
                            {
                                var cihazlar = await _context.Cihaz.Where(x => x.PanoId == pano.Id && x.Silindi != true).ToListAsync();
                                foreach (var cihaz in cihazlar)
                                {
                                    cihaz.Silindi = true;
                                }
                            }
                            makine.Silindi = true;
                        }
                    }
                    else if (subNode.Type == TreeViewNodeTypes.Pano)
                    {
                        var pano = await _context.Pano.Where(x => x.Id == subNode.Id).FirstOrDefaultAsync();
                        if (pano != null)
                        {
                            var panoMakineEslemeler = await _context.PanoMakineEsleme.Where(x => x.PanoId == pano.Id && x.Silindi != true).ToListAsync();
                            foreach (var panoMakineEsleme in panoMakineEslemeler)
                            {
                                panoMakineEsleme.Silindi = true;
                            }

                            var cihazlar = await _context.Cihaz.Where(x => x.PanoId == pano.Id && x.Silindi != true).ToListAsync();
                            foreach (var cihaz in cihazlar)
                            {
                                cihaz.Silindi = true;
                            }
                            pano.Silindi = true;
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task AltDugumlerBekleyenReddet(List<TreeModel> subNodes, string userId, IDbContextTransaction? transaction = null)
        {
            try
            {
                foreach (var subNode in subNodes)
                {
                    if (subNode.Type == TreeViewNodeTypes.Bolum)
                    {
                        var bekleyenIslemlerForSubNode = await _context.OnayIslemKayit.Where(x => x.OgeTipi == TreeViewNodeTypes.Bolum && x.OgeId == subNode.Id && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                        foreach (var bekleyenIslem in bekleyenIslemlerForSubNode)
                        {
                            await IslemReddet(bekleyenIslem.Id, userId, transaction);
                        }
                    }
                    else if (subNode.Type == TreeViewNodeTypes.Makine)
                    {
                        var bekleyenIslemlerForSubNode = await _context.OnayIslemKayit.Where(x => x.OgeTipi == TreeViewNodeTypes.Makine && x.OgeId == subNode.Id && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                        foreach (var bekleyenIslem in bekleyenIslemlerForSubNode)
                        {
                            await IslemReddet(bekleyenIslem.Id, userId, transaction);
                        }
                    }
                    else if (subNode.Type == TreeViewNodeTypes.Pano)
                    {
                        var bekleyenIslemlerForSubNode = await _context.OnayIslemKayit.Where(x => x.OgeTipi == TreeViewNodeTypes.Pano && x.OgeId == subNode.Id && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                        foreach (var bekleyenIslem in bekleyenIslemlerForSubNode)
                        {
                            await IslemReddet(bekleyenIslem.Id, userId, transaction);
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task AltDugumlerBekleyenleriOnayaGonder(List<TreeModel> subNodes, string userId)
        {
            foreach (var subNode in subNodes)
            {
                if (subNode.Type == TreeViewNodeTypes.Bolum)
                {
                    var bekleyenIslemlerForSubNode = await _context.OnayIslemKayit.Where(x => x.UserId == userId && x.OgeTipi == TreeViewNodeTypes.Bolum && x.OgeId == subNode.Id && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.OnayaGonderildiMi != true).ToListAsync();
                    foreach (var bekleyenIslem in bekleyenIslemlerForSubNode)
                    {
                        bekleyenIslem.OnayaGonderildiMi = true;
                        bekleyenIslem.GuncellemeZamani = DateTime.Now;
                        bekleyenIslem.GuncelleyenUserId = userId;
                    }
                }
                else if (subNode.Type == TreeViewNodeTypes.Makine)
                {
                    var bekleyenIslemlerForSubNode = await _context.OnayIslemKayit.Where(x => x.UserId == userId && x.OgeTipi == TreeViewNodeTypes.Makine && x.OgeId == subNode.Id && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.OnayaGonderildiMi != true).ToListAsync();
                    foreach (var bekleyenIslem in bekleyenIslemlerForSubNode)
                    {
                        bekleyenIslem.OnayaGonderildiMi = true;
                        bekleyenIslem.GuncellemeZamani = DateTime.Now;
                        bekleyenIslem.GuncelleyenUserId = userId;
                    }
                }
                else if (subNode.Type == TreeViewNodeTypes.Pano)
                {
                    var bekleyenIslemlerForSubNode = await _context.OnayIslemKayit.Where(x => x.UserId == userId && (x.OgeTipi == TreeViewNodeTypes.Pano) && x.OgeId == subNode.Id && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.OnayaGonderildiMi != true).ToListAsync();
                    foreach (var bekleyenIslem in bekleyenIslemlerForSubNode)
                    {
                        bekleyenIslem.OnayaGonderildiMi = true;
                        bekleyenIslem.GuncellemeZamani = DateTime.Now;
                        bekleyenIslem.GuncelleyenUserId = userId;
                    }

                    var bekleyenIslemlerForSubNodeCihaz = await _context.OnayIslemKayit.Where(x => x.UserId == userId && (x.OgeTipi == TreeViewNodeTypes.Cihaz || x.OgeTipi == TreeViewNodeTypes.Dosya) && x.PanoId == subNode.Id && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.OnayaGonderildiMi != true).ToListAsync();
                    foreach (var bekleyenIslem in bekleyenIslemlerForSubNodeCihaz)
                    {
                        bekleyenIslem.OnayaGonderildiMi = true;
                        bekleyenIslem.GuncellemeZamani = DateTime.Now;
                        bekleyenIslem.GuncelleyenUserId = userId;
                    }
                }
                await _context.SaveChangesAsync();
                if (subNode.Children != null)
                {
                    await AltDugumlerBekleyenleriOnayaGonder(subNode.Children, userId);
                }
            }
        }

        public async Task DugumeAitBekleyenIslemleriReddet(int nodeId, string nodeType, string userId, int? hariciOnayIslemId = null, IDbContextTransaction? transaction = null)
        {
            try
            {
                if (nodeType == TreeViewNodeTypes.Bolum)
                {
                    var bekleyenIslemlerForNode = await _context.OnayIslemKayit.Where(x => x.OgeTipi == TreeViewNodeTypes.Bolum && x.OgeId == nodeId && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.Id != hariciOnayIslemId).ToListAsync();
                    foreach (var bekleyenIslem in bekleyenIslemlerForNode)
                    {
                        await IslemReddet(bekleyenIslem.Id, userId, transaction);
                    }
                }
                else if (nodeType == TreeViewNodeTypes.Makine)
                {
                    var bekleyenIslemlerForNode = await _context.OnayIslemKayit.Where(x => x.OgeTipi == TreeViewNodeTypes.Makine && x.OgeId == nodeId && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.Id != hariciOnayIslemId).ToListAsync();
                    foreach (var bekleyenIslem in bekleyenIslemlerForNode)
                    {
                        await IslemReddet(bekleyenIslem.Id, userId, transaction);
                    }
                }
                else if (nodeType == TreeViewNodeTypes.Pano)
                {
                    var bekleyenIslemlerForNode = await _context.OnayIslemKayit.Where(x => x.OgeTipi == TreeViewNodeTypes.Pano && x.OgeId == nodeId && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.Id != hariciOnayIslemId).ToListAsync();
                    foreach (var bekleyenIslem in bekleyenIslemlerForNode)
                    {
                        await IslemReddet(bekleyenIslem.Id, userId, transaction);
                    }
                }
                else if (nodeType == TreeViewNodeTypes.Cihaz)
                {
                    var bekleyenIslemlerForNode = await _context.OnayIslemKayit.Where(x => x.OgeTipi == TreeViewNodeTypes.Cihaz && x.OgeId == nodeId && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.Id != hariciOnayIslemId).ToListAsync();
                    foreach (var bekleyenIslem in bekleyenIslemlerForNode)
                    {
                        await IslemReddet(bekleyenIslem.Id, userId, transaction);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //public async Task<bool> IslemReddet(int id, string userId)
        //{
        //    IDbContextTransaction? transaction = null;
        //    bool ownsTransaction = false;
        //    if (_context.Database.CurrentTransaction == null)
        //    {
        //        transaction = await _context.Database.BeginTransactionAsync();
        //        ownsTransaction = true;
        //    }
        //    else
        //    {
        //        transaction = _context.Database.CurrentTransaction;
        //        ownsTransaction = false;
        //    }
        //    try
        //    {
        //        var islem = await _context.OnayIslemKayit.Where(x => x.Id == id).FirstOrDefaultAsync();
        //        //Kural: Üst dügümde bekleyen onay varsa önce üst düğümlerin onaylanması gerekli!
        //        var parentNodes = new List<TreeModel>();
        //        if (islem.OgeTipi == TreeViewNodeTypes.Dosya || islem.OgeTipi == TreeViewNodeTypes.Cihaz)
        //        {
        //            parentNodes.AddRange(await GetParentNodesById((int)islem.PanoId, TreeViewNodeTypes.Pano));
        //        }
        //        else
        //        {
        //            parentNodes.AddRange(await GetParentNodesById((int)islem.OgeId, islem.OgeTipi));
        //        }
        //        foreach (var node in parentNodes)
        //        {
        //            var islemVarMi = await _context.OnayIslemKayit.Where(x => x.OgeId == node.Id && x.OgeTipi == node.Type && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
        //            if (islemVarMi.Count() > 0)
        //            {
        //                throw new Exception("Önce üst düğümlerdeki bekleyen değişiklikler kapatılmalıdır!");
        //            }
        //        }
        //        if (islem == null || islem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
        //            throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");

        //        if (islem.OgeTipi == TreeViewNodeTypes.Bolum)
        //        {
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
        //            {
        //                var bolum = JsonConvert.DeserializeObject<Bolum>(islem.IslemVerisi);
        //                //await _context.Bolum.AddAsync(bolum);
        //                var dbBolum = await _context.Bolum.Where(x => x.Id == bolum.Id).FirstOrDefaultAsync();
        //                if (dbBolum != null)
        //                {
        //                    dbBolum.Silindi = true;
        //                    dbBolum.OnayIslemdeMi = false;
        //                }
        //                //Kural: Ekleme işlemi reddedilirse, altındaki düğümlerin hepsi silinmeli, yeni eklendi çünkü!
        //                var subNodes = await this.GetSubNodesById(bolum.Id, TreeViewNodeTypes.Bolum);
        //                await AltDugumlerSil(subNodes);

        //                //Kural: Ekleme islemi reddedilirse, diger onay islemleri de red olmalı!
        //                await AltDugumlerBekleyenReddet(subNodes, userId);
        //            }
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
        //            {
        //                var bolum = await _context.Bolum.FindAsync(islem.OgeId);
        //                if (bolum != null)
        //                {
        //                    bolum.OnayIslemdeMi = false;
        //                }
        //            }
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
        //            {
        //                var bolum = await _context.Bolum.FindAsync(islem.OgeId);
        //                if (bolum != null)
        //                {
        //                    bolum.OnayIslemdeMi = false;
        //                }
        //            }
        //        }
        //        else if (islem.OgeTipi == TreeViewNodeTypes.Makine)
        //        {
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
        //            {
        //                var makine = JsonConvert.DeserializeObject<Makine>(islem.IslemVerisi);
        //                //await _context.Makine.AddAsync(makine);
        //                var dbMakine = await _context.Makine.Where(x => x.Id == makine.Id).FirstOrDefaultAsync();
        //                if (dbMakine != null)
        //                {
        //                    dbMakine.Silindi = true;
        //                    dbMakine.OnayIslemdeMi = false;
        //                }
        //                //Kural: Ekleme işlemi reddedilirse, altındaki düğümlerin hepsi silinmeli, yeni eklendi çünkü!
        //                var subNodes = await this.GetSubNodesById(makine.Id, TreeViewNodeTypes.Makine);
        //                await AltDugumlerSil(subNodes);

        //                //Kural: Ekleme islemi reddedilirse, diger onay islemleri de red olmalı!
        //                await AltDugumlerBekleyenReddet(subNodes, userId);
        //            }
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
        //            {
        //                var makine = await _context.Makine.FindAsync(islem.OgeId);
        //                if (makine != null)
        //                {
        //                    makine.OnayIslemdeMi = false;
        //                }
        //            }
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
        //            {
        //                var makine = await _context.Makine.FindAsync(islem.OgeId);
        //                if (makine != null)
        //                {
        //                    makine.OnayIslemdeMi = false;
        //                }
        //            }
        //        }
        //        else if (islem.OgeTipi == TreeViewNodeTypes.Pano)
        //        {
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
        //            {
        //                var pano = JsonConvert.DeserializeObject<Pano>(islem.IslemVerisi);
        //                //await _context.Pano.AddAsync(pano);
        //                var dbPano = await _context.Pano.Where(x => x.Id == pano.Id).FirstOrDefaultAsync();
        //                if (dbPano != null)
        //                {
        //                    dbPano.Silindi = true;
        //                    dbPano.OnayIslemdeMi = false;
        //                }
        //                //Kural: Ekleme işlemi reddedilirse, altındaki düğümlerin hepsi silinmeli, yeni eklendi çünkü!
        //                var subNodes = await this.GetSubNodesById(pano.Id, TreeViewNodeTypes.Pano);
        //                await AltDugumlerSil(subNodes);

        //                //Kural: Ekleme islemi reddedilirse, diger onay islemleri de red olmalı!
        //                await AltDugumlerBekleyenReddet(subNodes, userId);
        //            }
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
        //            {
        //                var pano = await _context.Pano.FindAsync(islem.OgeId);
        //                if (pano != null)
        //                {
        //                    pano.OnayIslemdeMi = false;
        //                }
        //            }
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
        //            {
        //                var pano = await _context.Pano.FindAsync(islem.OgeId);
        //                if (pano != null)
        //                {
        //                    pano.OnayIslemdeMi = false;
        //                }
        //            }
        //        }
        //        else if (islem.OgeTipi == TreeViewNodeTypes.Cihaz)
        //        {
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
        //            {
        //                var cihaz = JsonConvert.DeserializeObject<Cihaz>(islem.IslemVerisi);
        //                //await _context.Cihaz.AddAsync(cihaz);
        //                var dbCihaz = await _context.Cihaz.Where(x => x.Id == cihaz.Id).FirstOrDefaultAsync();
        //                if (dbCihaz != null)
        //                {
        //                    dbCihaz.Silindi = true;
        //                    dbCihaz.OnayIslemdeMi = false;
        //                }
        //                //TODO: dugume ait diger islemleri reddetmeli
        //                await DugumeAitBekleyenIslemleriReddet(cihaz.Id, TreeViewNodeTypes.Cihaz, userId, islem.Id);
        //            }
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
        //            {
        //                var cihaz = await _context.Pano.FindAsync(islem.OgeId);
        //                if (cihaz != null)
        //                {
        //                    cihaz.OnayIslemdeMi = false;
        //                }
        //            }
        //            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
        //            {
        //                var cihaz = await _context.Pano.FindAsync(islem.OgeId);
        //                if (cihaz != null)
        //                {
        //                    cihaz.OnayIslemdeMi = false;
        //                }
        //            }
        //        }
        //        if (islem.OgeTipi == TreeViewNodeTypes.Dosya)
        //        {
        //            await DosyaIslemReddet(islem.Id, userId);
        //        }
        //        islem.Durum = PanoKimlikOnayIslemDurum.Red;
        //        islem.GuncellemeZamani = DateTime.Now;
        //        islem.GuncelleyenUserId = userId;
        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (ownsTransaction && transaction != null)
        //            await transaction.RollbackAsync();
        //        throw new Exception(ex.Message);
        //    }
        //    finally
        //    {
        //        if (ownsTransaction && transaction != null)
        //            transaction.Dispose();
        //    }
        //}

        private async Task<bool> BekleyenOnayIslemKaydiKontrol(int onayIslemKayitId, string ogeTipi, int ogeId)
        {
            return await _context.OnayIslemKayit.AnyAsync(x => x.Id != onayIslemKayitId && x.OgeTipi == ogeTipi && x.OgeId == ogeId && x.Durum == PanoKimlikOnayIslemDurum.Beklemede);
        }

        public async Task<bool> IslemReddet(int id, string userId, IDbContextTransaction? transaction = null)
        {
            bool ownsTransaction = false;
            try
            {
                if (transaction == null)
                {
                    transaction = await _context.Database.BeginTransactionAsync();
                    ownsTransaction = true;
                }
                var islem = await _context.OnayIslemKayit.Where(x => x.Id == id).FirstOrDefaultAsync();
                //Kural: Üst dügümde bekleyen onay varsa önce üst düğümlerin onaylanması gerekli!
                var parentNodes = new List<TreeModel>();
                if (islem.OgeTipi == TreeViewNodeTypes.Dosya || islem.OgeTipi == TreeViewNodeTypes.Cihaz)
                {
                    parentNodes.AddRange(await GetParentNodesById((int)islem.PanoId, TreeViewNodeTypes.Pano));
                }
                else
                {
                    parentNodes.AddRange(await GetParentNodesById((int)islem.OgeId, islem.OgeTipi));
                }
                foreach (var node in parentNodes)
                {
                    var islemVarMi = await _context.OnayIslemKayit.Where(x => x.OgeId == node.Id && x.OgeTipi == node.Type && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                    if (islemVarMi.Count() > 0)
                    {
                        throw new Exception("Önce üst düğümlerdeki bekleyen değişiklikler kapatılmalıdır!");
                    }
                }
                if (islem == null || islem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                    throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");

                if (islem.OgeTipi == TreeViewNodeTypes.Bolum)
                {
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                    {
                        var bolum = JsonConvert.DeserializeObject<Bolum>(islem.IslemVerisi);
                        //await _context.Bolum.AddAsync(bolum);
                        var dbBolum = await _context.Bolum.Where(x => x.Id == bolum.Id).FirstOrDefaultAsync();
                        if (dbBolum != null)
                        {
                            dbBolum.Silindi = true;
                            dbBolum.OnayIslemdeMi = false;
                        }
                        //Kural: Ekleme işlemi reddedilirse, altındaki düğümlerin hepsi silinmeli, yeni eklendi çünkü!
                        var subNodes = await this.GetSubNodesById(bolum.Id, TreeViewNodeTypes.Bolum);
                        await AltDugumlerSil(subNodes);

                        //Kural: Ekleme islemi reddedilirse, diger onay islemleri de red olmalı!
                        await AltDugumlerBekleyenReddet(subNodes, userId, transaction);
                    }
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                    {
                        var bolum = await _context.Bolum.FindAsync(islem.OgeId);
                        if (bolum != null)
                        {
                            bolum.OnayIslemdeMi = false;
                        }
                    }
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                    {
                        var bolum = await _context.Bolum.FindAsync(islem.OgeId);
                        if (bolum != null)
                        {
                            bolum.OnayIslemdeMi = false;
                        }
                    }
                }
                else if (islem.OgeTipi == TreeViewNodeTypes.Makine)
                {
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                    {
                        var makine = JsonConvert.DeserializeObject<Makine>(islem.IslemVerisi);
                        //await _context.Makine.AddAsync(makine);
                        var dbMakine = await _context.Makine.Where(x => x.Id == makine.Id).FirstOrDefaultAsync();
                        if (dbMakine != null)
                        {
                            dbMakine.Silindi = true;
                            dbMakine.OnayIslemdeMi = false;
                        }
                        //Kural: Ekleme işlemi reddedilirse, altındaki düğümlerin hepsi silinmeli, yeni eklendi çünkü!
                        var subNodes = await this.GetSubNodesById(makine.Id, TreeViewNodeTypes.Makine);
                        await AltDugumlerSil(subNodes);

                        //Kural: Ekleme islemi reddedilirse, diger onay islemleri de red olmalı!
                        await AltDugumlerBekleyenReddet(subNodes, userId, transaction);
                    }
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                    {
                        var makine = await _context.Makine.FindAsync(islem.OgeId);
                        if (makine != null)
                        {
                            makine.OnayIslemdeMi = false;
                        }
                    }
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                    {
                        var makine = await _context.Makine.FindAsync(islem.OgeId);
                        if (makine != null)
                        {
                            makine.OnayIslemdeMi = false;
                        }
                    }
                }
                else if (islem.OgeTipi == TreeViewNodeTypes.Pano)
                {
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                    {
                        var pano = JsonConvert.DeserializeObject<Pano>(islem.IslemVerisi);
                        //await _context.Pano.AddAsync(pano);
                        var dbPano = await _context.Pano.Where(x => x.Id == pano.Id).FirstOrDefaultAsync();
                        if (dbPano != null)
                        {
                            dbPano.Silindi = true;
                            dbPano.OnayIslemdeMi = false;
                        }
                        //Kural: Ekleme işlemi reddedilirse, altındaki düğümlerin hepsi silinmeli, yeni eklendi çünkü!
                        var subNodes = await this.GetSubNodesById(pano.Id, TreeViewNodeTypes.Pano);
                        await AltDugumlerSil(subNodes);

                        //Kural: Ekleme islemi reddedilirse, diger onay islemleri de red olmalı!
                        await AltDugumlerBekleyenReddet(subNodes, userId, transaction);
                    }
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                    {
                        var pano = await _context.Pano.FindAsync(islem.OgeId);
                        if (pano != null)
                        {
                            pano.OnayIslemdeMi = false;
                        }
                    }
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                    {
                        var pano = await _context.Pano.FindAsync(islem.OgeId);
                        if (pano != null)
                        {
                            pano.OnayIslemdeMi = false;
                        }
                    }
                }
                else if (islem.OgeTipi == TreeViewNodeTypes.Cihaz)
                {
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                    {
                        var cihaz = JsonConvert.DeserializeObject<Cihaz>(islem.IslemVerisi);
                        //await _context.Cihaz.AddAsync(cihaz);
                        var dbCihaz = await _context.Cihaz.Where(x => x.Id == cihaz.Id).FirstOrDefaultAsync();
                        if (dbCihaz != null)
                        {
                            dbCihaz.Silindi = true;
                            dbCihaz.OnayIslemdeMi = false;
                        }
                        //TODO: dugume ait diger islemleri reddetmeli
                        await DugumeAitBekleyenIslemleriReddet(cihaz.Id, TreeViewNodeTypes.Cihaz, userId, islem.Id, transaction);
                    }
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                    {
                        var cihaz = await _context.Pano.FindAsync(islem.OgeId);
                        if (cihaz != null)
                        {
                            cihaz.OnayIslemdeMi = false;
                        }
                    }
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                    {
                        var cihaz = await _context.Pano.FindAsync(islem.OgeId);
                        if (cihaz != null)
                        {
                            cihaz.OnayIslemdeMi = false;
                        }
                    }
                }
                if (islem.OgeTipi == TreeViewNodeTypes.Dosya)
                {
                    await DosyaIslemReddet(islem.Id, userId);
                }

                islem.Durum = PanoKimlikOnayIslemDurum.Red;
                islem.GuncellemeZamani = DateTime.Now;
                islem.GuncelleyenUserId = userId;
                await _context.SaveChangesAsync();
                if (ownsTransaction)
                    await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                if (ownsTransaction && transaction != null)
                    await transaction.RollbackAsync();
                throw new Exception(ex.Message);
            }
            finally
            {
                if (ownsTransaction && transaction != null)
                    transaction.Dispose();
            }
        }

        public async Task DosyaIslemReddet(int islemId, string userId)
        {
            var islem = await _context.OnayIslemKayit.Where(x => x.Id == islemId).FirstOrDefaultAsync();
            if (islem == null || islem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");
            if (islem.OgeTipi == TreeViewNodeTypes.Dosya)
            {
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                {
                    // ActionData'dan geçici dosya yolunu al
                    var actionData = JsonConvert.DeserializeObject<DosyaOnayIslemModel>(islem.IslemVerisi);
                    string tempPath = actionData.TempDosyaYolu;

                    // Dosyayı geçici klasörden sil
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);  // Dosyayı sil
                    }
                    // Temel eşleşme: aynı pano + klasör + dosya adı
                    var relatedPending = await _context.OnayIslemKayit
                        .Where(x =>
                            x.Id != islem.Id &&
                            x.OgeTipi == TreeViewNodeTypes.Dosya &&
                            x.PanoId == islem.PanoId &&
                            x.KlasorAd == islem.KlasorAd &&
                            x.Durum == PanoKimlikOnayIslemDurum.Beklemede &&
                            x.DosyaAd == islem.DosyaAd) // temel dosya adı eşleşmesi
                        .ToListAsync();

                    // Bulduğumuz tüm bekleyenleri RED'e çek
                    foreach (var r in relatedPending.DistinctBy(x => x.Id))
                    {
                        // Eğer bunlardan biri de EKLEME ise, onun temp'ini de temizleyelim
                        if (r.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme && !string.IsNullOrWhiteSpace(r.IslemVerisi))
                        {
                            try
                            {
                                var rData = JsonConvert.DeserializeObject<DosyaOnayIslemModel>(r.IslemVerisi);
                                var tPath = rData?.TempDosyaYolu;
                                if (!string.IsNullOrEmpty(tPath) && File.Exists(tPath))
                                    File.Delete(tPath);
                            }
                            catch { }
                        }

                        r.Durum = PanoKimlikOnayIslemDurum.Red;
                        r.GuncellemeZamani = DateTime.Now;
                        r.GuncelleyenUserId = userId;
                    }

                    if (relatedPending.Count > 0)
                        await _context.SaveChangesAsync();
                }
                else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                {
                    //isim degistirme iptal oldugu icin islem yapilmaz
                }
                else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                {
                    //silme iptal oldugu icin islem yapilmaz
                }
            }
            //else if (islem.OgeTipi == TreeViewNodeTypes.Klasor)
            //{
            //    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
            //    {
            //        // ActionData'dan geçici dosya yolunu al
            //        var actionData = JsonConvert.DeserializeObject<DosyaOnayIslemModel>(islem.IslemVerisi);
            //        string directoryPath = actionData.DosyaYolu;

            //        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            //        {
            //            Directory.CreateDirectory(directoryPath); // Gerekirse klasörü oluştur
            //        }
            //    }
            //}

            // Action reddet
            islem.Durum = PanoKimlikOnayIslemDurum.Red;
            islem.GuncellemeZamani = DateTime.Now;
            islem.GuncelleyenUserId = userId;

            await _context.SaveChangesAsync();
        }
        public async Task DosyaIslemOnayla(int islemId, string userId)
        {
            //Dosya onaylandı ve nihai klasöre taşındı
            var islem = await _context.OnayIslemKayit.Where(x => x.Id == islemId).FirstOrDefaultAsync();
            if (islem == null || islem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");

            // ActionData'dan dosya yollarını al
            var actionData = JsonConvert.DeserializeObject<DosyaOnayIslemModel>(islem.IslemVerisi);

            if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
            {
                string tempPath = actionData.TempDosyaYolu;
                string finalPath = actionData.FinalDosyaYolu;

                // Dosyayı geçici klasörden nihai klasöre taşı
                if (System.IO.File.Exists(tempPath))
                {
                    var finalFullPath = Path.Combine(/*_environment.ContentRootPath,*/ finalPath);
                    System.IO.File.Move(tempPath, finalFullPath, true);
                }
            }
            else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
            {
                if (actionData.DosyaIcerik != null)
                {
                    await File.WriteAllTextAsync(actionData.FinalDosyaYolu, actionData.DosyaIcerik);
                }
                else
                {
                    if (File.Exists(Path.Combine(actionData.DosyaYolu, actionData.EskiDosyaAd)))
                    {
                        File.Move(Path.Combine(actionData.DosyaYolu, actionData.EskiDosyaAd), Path.Combine(actionData.DosyaYolu, actionData.YeniDosyaAd));
                    }
                }
            }
            else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
            {
                string currentPath = actionData.SilinecekDosyaYolu;

                // Dosyayı geçici klasörden sil
                if (System.IO.File.Exists(currentPath))
                {
                    System.IO.File.Delete(currentPath);  // Dosyayı sil
                }

                //başka bekleyen veya onaya gonderilmiş işlem varsa onları da sil
            }

            // Action onayla ve güncelle
            islem.Durum = PanoKimlikOnayIslemDurum.Kabul;
            islem.GuncellemeZamani = DateTime.UtcNow;
            islem.GuncelleyenUserId = userId;

            await _context.SaveChangesAsync();
        }
        public async Task CihazIslemReddet(int cihazId, string userId)
        {
            var islem = await _context.OnayIslemKayit.Where(x => x.OgeTipi == TreeViewNodeTypes.Cihaz && x.OgeId == cihazId).FirstOrDefaultAsync();
            if (islem == null || islem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");

            islem.Durum = PanoKimlikOnayIslemDurum.Red;
            islem.GuncellemeZamani = DateTime.UtcNow;
            islem.GuncelleyenUserId = userId;

            var cihaz = await _context.Cihaz.Where(x => x.Id == cihazId).FirstOrDefaultAsync();
            if (cihaz != null)
            {
                cihaz.OnayIslemdeMi = false;
                cihaz.OnayaGonderildiMi = false;
                cihaz.Silindi = true;
            }
            await _context.SaveChangesAsync();
        }
        public async Task CihazIslemOnayla(int cihazId, string userId)
        {
            var islem = await _context.OnayIslemKayit.Where(x => x.OgeTipi == TreeViewNodeTypes.Cihaz && x.OgeId == cihazId).FirstOrDefaultAsync();
            if (islem == null || islem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");

            islem.Durum = PanoKimlikOnayIslemDurum.Kabul;
            islem.GuncellemeZamani = DateTime.UtcNow;
            islem.GuncelleyenUserId = userId;

            var cihaz = await _context.Cihaz.Where(x => x.Id == cihazId).FirstOrDefaultAsync();
            if (cihaz != null)
            {
                cihaz.OnayIslemdeMi = false;
                cihaz.OnayaGonderildiMi = false;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetOnayIslemKayitFromTreeNodeId(int id, string nodeType)
        {
            List<string> islemAciklama = new List<string>();
            if (nodeType == TreeViewNodeTypes.Bolum)
            {
                var islemler = await _context.vwOnayIslemKayit.Where(x => x.OgeId == id && x.OgeTipi == TreeViewNodeTypes.Bolum && x.Durum == PanoKimlikOnayIslemDurum.Beklemede /*&& (x.OnayaGonderildiMi ?? false)*/).OrderBy(x => x.Id).ToListAsync();
                //if (islemler == null || islemler.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                //    throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");
                foreach (var islem in islemler)
                {
                    var actionData = System.Text.Json.JsonSerializer.Deserialize<Bolum>(islem.IslemVerisi);
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Ekleme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                    else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Düzenleme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                    else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Silme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                }
            }
            else if (nodeType == TreeViewNodeTypes.Makine)
            {
                var islemler = await _context.vwOnayIslemKayit.Where(x => x.OgeId == id && x.OgeTipi == TreeViewNodeTypes.Makine && x.Durum == PanoKimlikOnayIslemDurum.Beklemede /*&& (x.OnayaGonderildiMi ?? false)*/).OrderBy(x => x.Id).ToListAsync();
                //if (islem == null || islem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                //    throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");
                foreach (var islem in islemler)
                {
                    var actionData = System.Text.Json.JsonSerializer.Deserialize<Makine>(islem.IslemVerisi);
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Ekleme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                    else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Düzenleme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                    else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Silme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                }
            }
            else if (nodeType == TreeViewNodeTypes.Pano)
            {
                var islemler = await _context.vwOnayIslemKayit.Where(x => x.OgeId == id && x.OgeTipi == TreeViewNodeTypes.Pano && x.Durum == PanoKimlikOnayIslemDurum.Beklemede /*&& (x.OnayaGonderildiMi ?? false)*/).OrderBy(x => x.Id).ToListAsync();
                //if (islem == null || islem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                //    throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");
                foreach (var islem in islemler)
                {
                    var actionData = System.Text.Json.JsonSerializer.Deserialize<Pano>(islem.IslemVerisi);
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Ekleme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                    else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Düzenleme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                    else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Silme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                }
            }
            else if (nodeType == TreeViewNodeTypes.Cihaz)
            {
                var islemler = await _context.vwOnayIslemKayit.Where(x => x.OgeId == id && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.Durum == PanoKimlikOnayIslemDurum.Beklemede/* && (x.OnayaGonderildiMi ?? false)*/).OrderBy(x => x.Id).ToListAsync();
                //if (islem == null || islem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                //    throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");
                foreach (var islem in islemler)
                {
                    var actionData = System.Text.Json.JsonSerializer.Deserialize<Cihaz>(islem.IslemVerisi);
                    if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Ekleme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                    else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Düzenleme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                    else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                    {
                        islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Silme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                    }
                }
            }
            return islemAciklama;
        }

        public async Task<List<string>> GetOnayIslemKayitlarByIdForDosya(int onayIslemKayitId)
        {
            List<string> islemAciklama = new List<string>();

            var onayKayitIslem = await _context.OnayIslemKayit.Where(x => x.Id == onayIslemKayitId && x.OgeTipi == TreeViewNodeTypes.Dosya).FirstOrDefaultAsync();
            if (onayKayitIslem == null || onayKayitIslem.Durum != PanoKimlikOnayIslemDurum.Beklemede)
                throw new Exception("İşlem bulunamadı veya zaten onaylandı/reddedildi.");
            var islemler = await _context.vwOnayIslemKayit.Where(x => x.DosyaAd == onayKayitIslem.DosyaAd && x.PanoId == onayKayitIslem.PanoId && x.OgeTipi == TreeViewNodeTypes.Dosya && x.Durum == PanoKimlikOnayIslemDurum.Beklemede /*&& (x.OnayaGonderildiMi ?? false)*/).OrderBy(x => x.Id).ToListAsync();

            foreach (var islem in islemler)
            {
                var actionData = System.Text.Json.JsonSerializer.Deserialize<DosyaOnayIslemModel>(islem.IslemVerisi);
                if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Ekleme)
                {
                    islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Ekleme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                }
                else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Duzenleme)
                {
                    islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Düzenleme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                }
                else if (islem.IslemTipi == PanoKimlikOnayIslemTipi.Silme)
                {
                    islemAciklama.Add(System.Text.Json.JsonSerializer.Serialize(new { islemTipi = "Silme İşlemi", data = actionData, onayIslemKayitId = islem.Id, userName = islem.UserFullName, onayaGonderildiMi = islem.OnayaGonderildiMi }));
                }
            }
            return islemAciklama;
        }

        public async Task<List<TreeModel>> GetSubNodesById(int nodeId, string nodeType)
        {
            var subNodes = new List<TreeModel>();

            if (nodeType == TreeViewNodeTypes.Bolum)
            {
                // Bölüm altındaki bölümleri al
                var subBolumler = await _context.Bolum.Where(x => x.ParentBolumId == nodeId && x.Silindi != true).ToListAsync();
                foreach (var subBolum in subBolumler)
                {
                    subNodes.Add(new TreeModel
                    {
                        Id = subBolum.Id,
                        Name = subBolum.Name,
                        Expanded = false,
                        Checked = false,
                        Type = TreeViewNodeTypes.Bolum,
                        Children = null
                    });

                    // Rekürsif olarak alt düğümleri getir
                    var childNodes = await GetSubNodesById(subBolum.Id, TreeViewNodeTypes.Bolum);
                    if (childNodes.Any())
                    {
                        subNodes.Last().Children = childNodes;
                    }
                }

                // Bölüm altındaki makineleri al
                var makineler = await _context.Makine.Where(x => x.BolumId == nodeId && x.ParentMakineId == 0 && x.Silindi != true).ToListAsync();
                foreach (var makine in makineler)
                {
                    subNodes.Add(new TreeModel
                    {
                        Id = makine.Id,
                        Name = makine.Name,
                        Expanded = false,
                        Checked = false,
                        Type = TreeViewNodeTypes.Makine,
                        Children = null
                    });

                    // Rekürsif olarak alt düğümleri getir
                    var childNodes = await GetSubNodesById(makine.Id, TreeViewNodeTypes.Makine);
                    if (childNodes.Any())
                    {
                        subNodes.Last().Children = childNodes;
                    }
                }
            }
            else if (nodeType == TreeViewNodeTypes.Makine)
            {
                // Makine altındaki panoları al
                var panoMakineEslemes = await _context.PanoMakineEsleme.Where(x => x.MakineId == nodeId && x.Silindi != true).ToListAsync();
                var panolar = await _context.Pano.Where(x => panoMakineEslemes.Select(pme => pme.PanoId).Contains(x.Id) && x.ParentPanoId == 0 && x.Silindi != true).ToListAsync();

                foreach (var pano in panolar)
                {
                    subNodes.Add(new TreeModel
                    {
                        Id = pano.Id,
                        Name = pano.Name,
                        Expanded = false,
                        Checked = false,
                        Type = TreeViewNodeTypes.Pano,
                        Children = null
                    });

                    // Rekürsif olarak alt düğümleri getir
                    var childNodes = await GetSubNodesById(pano.Id, TreeViewNodeTypes.Pano);
                    if (childNodes.Any())
                    {
                        subNodes.Last().Children = childNodes;
                    }
                }
            }
            else if (nodeType == TreeViewNodeTypes.Pano)
            {
                // Pano altındaki panoları al
                var subPanolar = await _context.Pano.Where(x => x.ParentPanoId == nodeId && x.Silindi != true).ToListAsync();

                foreach (var subPano in subPanolar)
                {
                    subNodes.Add(new TreeModel
                    {
                        Id = subPano.Id,
                        Name = subPano.Name,
                        Expanded = false,
                        Checked = false,
                        Type = TreeViewNodeTypes.Pano,
                        Children = null
                    });

                    // Rekürsif olarak alt düğümleri getir
                    var childNodes = await GetSubNodesById(subPano.Id, TreeViewNodeTypes.Pano);
                    if (childNodes.Any())
                    {
                        subNodes.Last().Children = childNodes;
                    }
                }
            }

            return subNodes;
        }
        public async Task<List<TreeModel>> GetParentNodesById(int nodeId, string nodeType)
        {
            var parentNodes = new List<TreeModel>();

            if (nodeType == TreeViewNodeTypes.Pano)
            {
                // Pano'nun üst parent'ını al
                var pano = await _context.Pano.FirstOrDefaultAsync(x => x.Id == nodeId && x.Silindi != true);
                if (pano != null && pano.ParentPanoId > 0)
                {
                    var parentPano = await _context.Pano.FirstOrDefaultAsync(x => x.Id == pano.ParentPanoId && x.Silindi != true);
                    if (parentPano != null)
                    {
                        parentNodes.Add(new TreeModel
                        {
                            Id = parentPano.Id,
                            Name = parentPano.Name,
                            Expanded = true,
                            Checked = false,
                            Type = TreeViewNodeTypes.Pano,
                            Children = null
                        });

                        // Rekürsif olarak üst düğümleri getir
                        var parentParents = await GetParentNodesById(parentPano.Id, TreeViewNodeTypes.Pano);
                        parentNodes.AddRange(parentParents);
                    }
                }
                else if (pano != null && pano.ParentPanoId == 0)
                {
                    // Eğer bu Pano'nun parent'ı yoksa (en üst seviyedeyse), Makine'sini bul
                    var panoMakineEsleme = await _context.PanoMakineEsleme.FirstOrDefaultAsync(x => x.PanoId == pano.Id && x.Silindi != true);
                    if (panoMakineEsleme != null)
                    {
                        var parentMakine = await _context.Makine.FirstOrDefaultAsync(x => x.Id == panoMakineEsleme.MakineId && x.Silindi != true);
                        if (parentMakine != null)
                        {
                            parentNodes.Add(new TreeModel
                            {
                                Id = parentMakine.Id,
                                Name = parentMakine.Name,
                                Expanded = true,
                                Checked = false,
                                Type = TreeViewNodeTypes.Makine,
                                Children = null
                            });

                            // Rekürsif olarak üst düğümleri getir
                            var parentParents = await GetParentNodesById(parentMakine.Id, TreeViewNodeTypes.Makine);
                            parentNodes.AddRange(parentParents);
                        }
                    }
                }
            }
            else if (nodeType == TreeViewNodeTypes.Makine)
            {
                // Makine'nin üst Bölüm'ünü al
                var makine = await _context.Makine.FirstOrDefaultAsync(x => x.Id == nodeId && x.Silindi != true);
                if (makine != null && makine.BolumId > 0)
                {
                    var parentBolum = await _context.Bolum.FirstOrDefaultAsync(x => x.Id == makine.BolumId && x.Silindi != true);
                    if (parentBolum != null)
                    {
                        parentNodes.Add(new TreeModel
                        {
                            Id = parentBolum.Id,
                            Name = parentBolum.Name,
                            Expanded = true,
                            Checked = false,
                            Type = TreeViewNodeTypes.Bolum,
                            Children = null
                        });

                        // Rekürsif olarak üst düğümleri getir
                        var parentParents = await GetParentNodesById(parentBolum.Id, TreeViewNodeTypes.Bolum);
                        parentNodes.AddRange(parentParents);
                    }
                }
            }
            else if (nodeType == TreeViewNodeTypes.Bolum)
            {
                // Bölüm'ün üst Parent Bölüm'ünü al
                var bolum = await _context.Bolum.FirstOrDefaultAsync(x => x.Id == nodeId && x.Silindi != true);
                if (bolum != null && bolum.ParentBolumId > 0)
                {
                    var parentBolum = await _context.Bolum.FirstOrDefaultAsync(x => x.Id == bolum.ParentBolumId && x.Silindi != true);
                    if (parentBolum != null)
                    {
                        parentNodes.Add(new TreeModel
                        {
                            Id = parentBolum.Id,
                            Name = parentBolum.Name,
                            Expanded = true,
                            Checked = false,
                            Type = TreeViewNodeTypes.Bolum,
                            Children = null
                        });

                        // Rekürsif olarak üst düğümleri getir
                        var parentParents = await GetParentNodesById(parentBolum.Id, TreeViewNodeTypes.Bolum);
                        parentNodes.AddRange(parentParents);
                    }
                }
            }

            return parentNodes;
        }

        public async Task<List<vwDistinctOnayIslemKayit>> GetAllvwDistinctOnayIslemKayit(List<NodeYetkiModel> nodeYetkiler, string userId)
        {
            var onayIslemKayitList = await _context.vwDistinctOnayIslemKayit.ToListAsync();
            var result = onayIslemKayitList.Where(onay => nodeYetkiler.Any(yetki => (onay.OgeTipi == "cihaz" || onay.OgeTipi == "dosya" ? yetki.NodeId == onay.PanoId : yetki.NodeId == onay.OgeId)
                         && (onay.OgeTipi == "cihaz" || onay.OgeTipi == "dosya" ? yetki.NodeType == "pano" : yetki.NodeType == onay.OgeTipi) && yetki.Responsible == true))
                                            .Select(onay => new vwDistinctOnayIslemKayit
                                            {
                                                UserId = onay.UserId,
                                                OgeId = onay.OgeId,
                                                OgeTipi = onay.OgeTipi,
                                                OgeName = onay.OgeTipi == TreeViewNodeTypes.Dosya ? ((onay.KlasorAd == null ? "" : onay.KlasorAd + "\\") + (onay.DosyaAd == null ? "" : onay.DosyaAd)) : onay.OgeName,
                                                UserFullName = onay.UserFullName,
                                                LokasyonId = onay.LokasyonId,
                                                PanoId = onay.PanoId,
                                                LokasyonName = onay.LokasyonName,
                                                SirketName = onay.SirketName,
                                                DosyaAd = onay.DosyaAd,
                                                KlasorAd = onay.KlasorAd,
                                            }).ToList();
            return result;/*await _context.vwOnayIslemKayit.ToListAsync()*/
        }

        public async Task<bool> OnayaGonderByNodeIdAndType(List<NodeYetkiModel> nodeYetkiler, int nodeId, string nodeType, string nodeName, string userId)
        {
            if (nodeType == TreeViewNodeTypes.Bolum)
            {
                var bekleyenIslemlerForSubNode = await _context.OnayIslemKayit.Where(x => x.UserId == userId && x.OgeTipi == TreeViewNodeTypes.Bolum && x.OgeId == nodeId && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.OnayaGonderildiMi != true).ToListAsync();
                foreach (var bekleyenIslem in bekleyenIslemlerForSubNode)
                {
                    bekleyenIslem.OnayaGonderildiMi = true;
                    bekleyenIslem.GuncellemeZamani = DateTime.Now;
                    bekleyenIslem.GuncelleyenUserId = userId;
                }
                await _context.SaveChangesAsync();
                var subNodes = await this.GetSubNodesById(nodeId, TreeViewNodeTypes.Bolum);
                await AltDugumlerBekleyenleriOnayaGonder(subNodes, userId);
            }
            else if (nodeType == TreeViewNodeTypes.Makine)
            {
                var bekleyenIslemlerForSubNode = await _context.OnayIslemKayit.Where(x => x.UserId == userId && x.OgeTipi == TreeViewNodeTypes.Makine && x.OgeId == nodeId && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.OnayaGonderildiMi != true).ToListAsync();
                foreach (var bekleyenIslem in bekleyenIslemlerForSubNode)
                {
                    bekleyenIslem.OnayaGonderildiMi = true;
                    bekleyenIslem.GuncellemeZamani = DateTime.Now;
                    bekleyenIslem.GuncelleyenUserId = userId;
                }
                await _context.SaveChangesAsync();
                var subNodes = await this.GetSubNodesById(nodeId, TreeViewNodeTypes.Makine);
                await AltDugumlerBekleyenleriOnayaGonder(subNodes, userId);
            }
            else if (nodeType == TreeViewNodeTypes.Pano)
            {
                var bekleyenIslemlerForNode = await _context.OnayIslemKayit.Where(x => x.UserId == userId && (x.OgeTipi == TreeViewNodeTypes.Pano) && x.OgeId == nodeId && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.OnayaGonderildiMi != true).ToListAsync();
                foreach (var bekleyenIslem in bekleyenIslemlerForNode)
                {
                    bekleyenIslem.OnayaGonderildiMi = true;
                    bekleyenIslem.GuncellemeZamani = DateTime.Now;
                    bekleyenIslem.GuncelleyenUserId = userId;
                }

                var bekleyenIslemlerForNodeCihaz = await _context.OnayIslemKayit.Where(x => x.UserId == userId && (x.OgeTipi == TreeViewNodeTypes.Cihaz || x.OgeTipi == TreeViewNodeTypes.Dosya) && x.PanoId == nodeId && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.OnayaGonderildiMi != true).ToListAsync();
                foreach (var bekleyenIslem in bekleyenIslemlerForNodeCihaz)
                {
                    bekleyenIslem.OnayaGonderildiMi = true;
                    bekleyenIslem.GuncellemeZamani = DateTime.Now;
                    bekleyenIslem.GuncelleyenUserId = userId;
                }
                await _context.SaveChangesAsync();
                var subNodes = await this.GetSubNodesById(nodeId, TreeViewNodeTypes.Pano);
                await AltDugumlerBekleyenleriOnayaGonder(subNodes, userId);
            }
            else if (nodeType == TreeViewNodeTypes.Cihaz)
            {
                var bekleyenIslemlerForNode = await _context.OnayIslemKayit.Where(x => x.UserId == userId && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.PanoId == nodeId && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.OnayaGonderildiMi != true).ToListAsync();
                foreach (var bekleyenIslem in bekleyenIslemlerForNode)
                {
                    bekleyenIslem.OnayaGonderildiMi = true;
                    bekleyenIslem.GuncellemeZamani = DateTime.Now;
                    bekleyenIslem.GuncelleyenUserId = userId;
                }
                await _context.SaveChangesAsync();
            }
            else if (nodeType == TreeViewNodeTypes.Dosya)
            {
                var bekleyenIslemlerForNode = await _context.OnayIslemKayit.Where(x => x.UserId == userId && x.OgeTipi == TreeViewNodeTypes.Dosya && x.PanoId == nodeId && x.KlasorAd == nodeName && x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.OnayaGonderildiMi != true).ToListAsync();
                foreach (var bekleyenIslem in bekleyenIslemlerForNode)
                {
                    bekleyenIslem.OnayaGonderildiMi = true;
                    bekleyenIslem.GuncellemeZamani = DateTime.Now;
                    bekleyenIslem.GuncelleyenUserId = userId;
                }
                await _context.SaveChangesAsync();
            }
            return true;
        }
    }
}