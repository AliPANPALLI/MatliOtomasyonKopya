using EnergyMonitoring.Application.Interfaces;
using EnergyMonitoring.Application.ViewModels.RequestModel;
using EnergyMonitoring.Domain.Models;
using EnergyMonitoring.Domain.Models.ReportModels;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EnergyMonitoring.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TreeController : ControllerBase
    {
        private readonly ITreeAppService _treeAppService;

        public TreeController(ITreeAppService treeAppService)
        {
            _treeAppService = treeAppService;
        }

        [HttpGet("GetTreeViewByLokasyonId")]
        public async Task<IActionResult> GetTreeViewByLokasyonId(int lokasyonId, CancellationToken ct, bool duzenlemeModu = false)
        {
            var result = await _treeAppService.GetTreeByLokasyonId(lokasyonId, duzenlemeModu, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetNodeTreeViewByCihazId")]
        public async Task<IActionResult> GetNodeTreeViewByCihazId(int cihazId, CancellationToken ct)
        {
            var result = await _treeAppService.GetNodeTreeViewByCihazId(cihazId, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetNodeTreeViewByCihazId2")]
        public async Task<IActionResult> GetNodeTreeViewByCihazId2(int cihazId, CancellationToken ct)
        {
            var result = await _treeAppService.GetNodeTreeViewByCihazId2(cihazId, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetNodeTreeViewByNodeIdAndType")]
        public async Task<IActionResult> GetNodeTreeViewByNodeIdAndType(int nodeId, string nodeType, CancellationToken ct)
        {
            var result = await _treeAppService.GetNodeTreeViewByNodeIdAndType(nodeId, nodeType, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetNodeTreeViewByNodeIdAndType2")]
        public async Task<IActionResult> GetNodeTreeViewByNodeIdAndType2(int nodeId, string nodeType, CancellationToken ct)
        {
            var result = await _treeAppService.GetNodeTreeViewByNodeIdAndType2(nodeId, nodeType, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetSahaVerileriTabloByCihazId")]
        public async Task<IActionResult> GetSahaVerileriTabloByCihazId(int cihazId, CancellationToken ct)
        {
            var result = await _treeAppService.GetSahaVerileriTabloByCihazId(cihazId, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetSahaVerileriTabloByNodeIdAndType")]
        public async Task<IActionResult> GetSahaVerileriTabloByNodeIdAndType(int nodeId, string nodeType, CancellationToken ct)
        {
            var result = await _treeAppService.GetSahaVerileriTabloByNodeIdAndType(nodeId, nodeType, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetSahaVerileriTabloByCihazList")]
        public async Task<IActionResult> GetSahaVerileriTabloByCihazList(string cihazNodeIds, CancellationToken ct)
        {
            var secilenCihazNodeIds = JsonConvert.DeserializeObject<List<int>>(cihazNodeIds);
            var result = await _treeAppService.GetSahaVerileriTabloByCihazList(secilenCihazNodeIds, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetTuketimVerileriByCihazId")]
        public async Task<IActionResult> GetTuketimVerileriByCihazId(int cihazId, CancellationToken ct)
        {
            var result = await _treeAppService.GetTuketimVerileriByCihazId(cihazId, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetTuketimEnerjiVerileriTabloByCihazId")]
        public async Task<IActionResult> GetTuketimEnerjiVerileriTabloByCihazId(int cihazId, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken ct)
        {
            var result = await _treeAppService.GetTuketimEnerjiVerileriTabloByCihazId(cihazId, baslangicZamani, bitisZamani, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetTuketimEnerjiVerileriTabloByNodeIdAndType")]
        public async Task<IActionResult> GetTuketimEnerjiVerileriTabloByCihazIds(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken ct)
        {
            var result = await _treeAppService.GetTuketimEnerjiVerileriTabloByNodeIdAndType(nodeId, nodeType, baslangicZamani, bitisZamani, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetTuketimEnerjiVerileriTabloByCihazList")]
        public async Task<IActionResult> GetTuketimEnerjiVerileriTabloByCihazList(DateTime baslangicZamani, DateTime bitisZamani, string cihazNodeIds, CancellationToken ct)
        {
            var secilenCihazNodeIds = JsonConvert.DeserializeObject<List<int>>(cihazNodeIds);
            if (secilenCihazNodeIds == null) { return new JsonResult(null); }
            var result = await _treeAppService.GetTuketimEnerjiVerileriTabloByCihazList(secilenCihazNodeIds, baslangicZamani, bitisZamani, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetTuketimBedeli")]
        public async Task<IActionResult> GetTuketimBedeliByCihazIds(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken ct)
        {
            try
            {
                var result = await _treeAppService.GetTuketimBedeliYeni(nodeId, nodeType, baslangicZamani, bitisZamani, lokasyonId, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(0);
            }
        }

        [HttpGet("GetTuketimBedeliByCihazList")]
        public async Task<IActionResult> GetTuketimBedeliByCihazList(string cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken ct)
        {
            var secilenCihazNodeIds = JsonConvert.DeserializeObject<List<int>>(cihazNodeIds);
            if (secilenCihazNodeIds == null) { return new JsonResult(null); }
            var result = await _treeAppService.GetTuketimBedeliByCihazList(secilenCihazNodeIds, baslangicZamani, bitisZamani, lokasyonId, ct);
            return Ok(result);
        }

        [HttpGet("GetDetayliTuketimBedeli")]
        public async Task<IActionResult> GetDetayliTuketimBedeliByCihazIds(int nodeId, string nodeType, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, CancellationToken ct)
        {
            var result = await _treeAppService.GetDetayliTuketimBedeli(nodeId, nodeType, baslangicZamani, bitisZamani, lokasyonId, ct);
            return new JsonResult(result);
        }

        [HttpGet("GetEnerjiRaporFromSecilenAsync")]
        public async Task<IActionResult> GetEnerjiRaporFromSecilenAsync(string secilenNodeIdsAndTypes, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval, CancellationToken ct)
        {
            var secilenNodeIdsAndTypesList = JsonConvert.DeserializeObject<List<string>>(secilenNodeIdsAndTypes);
            var result = await _treeAppService.GetEnerjiRaporFromSecilenAsync(secilenNodeIdsAndTypesList, baslangicZamani, bitisZamani, dakikaInterval, ct);
            return Ok(result);
        }

        [HttpGet("GetEnerjiDagilimRaporFromSecilenAsync")]
        [RequestTimeout(600_000)]
        public async Task<IActionResult> GetEnerjiDagilimRaporFromSecilenAsync(string secilenNodeIdsAndTypes, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken ct)
        {
            var secilenNodeIdsAndTypesList = JsonConvert.DeserializeObject<List<string>>(secilenNodeIdsAndTypes);
            var result = await _treeAppService.GetEnerjiDagilimRaporFromSecilenAsync(secilenNodeIdsAndTypesList, baslangicZamani, bitisZamani, ct);
            return Ok(result);
        }

        [HttpPost("GetEnerjiDagilimRaporFromSecilenAsync2")]
        [RequestTimeout(600_000)]
        public async Task<IActionResult> GetEnerjiDagilimRaporFromSecilenAsync2([FromBody] EnerjiDagilimRaporRequest request, CancellationToken ct)
        {
            var result = await _treeAppService.GetEnerjiDagilimRaporFromSecilenAsync2(request.SecilenNodeIdsAndTypes, request.BaslangicZamani, request.BitisZamani, request.OncekiDonemBaslangicZamani, request.OncekiDonemBitisZamani, ct);
            return Ok(result);
        }

        [HttpGet("GetEnerjiKumesTesisIzlemeModelByCihazId")]
        public async Task<IActionResult> GetEnerjiKumesTesisIzlemeModelByCihazId(int cihazId, CancellationToken ct)
        {
            var result = await _treeAppService.GetEnerjiKumesTesisIzlemeModelByCihazId(cihazId, ct);
            return new JsonResult(result);
        }
        //Eski metotlar
        //[HttpPost("AddEnerjiMaliyetParametreler")]
        //public async Task<IActionResult> AddEnerjiMaliyetParametreler([FromBody] EnerjiMaliyetParametrelerRequest request, CancellationToken cancellationToken)
        //{
        //    await _treeAppService.AddEnerjiMaliyetParametreler(request.EnerjiMaliyetParametreler, request.SeciliYil, request.LokasyonId, request.TarifeTipi, cancellationToken);
        //    return Ok();
        //}

        //[HttpGet("GetEnerjiMaliyetParametreler")]
        //public async Task<IActionResult> GetEnerjiMaliyetParametreler(int seciliYil, int lokasyonId, CancellationToken ct)
        //{
        //    var result = await _treeAppService.GetEnerjiMaliyetParametreler(seciliYil, lokasyonId, ct);
        //    return Ok(result);
        //}

        //Yeni Metotlar
        [HttpGet("GetEnerjiMaliyetGruplar")]
        public async Task<IActionResult> GetEnerjiMaliyetGruplar()
        {
            var result = await _treeAppService.GetEnerjiMaliyetGruplar();
            return Ok(result);
        }

        [HttpPost("AddEnerjiMaliyetGrup")]
        public async Task<IActionResult> AddEnerjiMaliyetGrup([FromBody] EnerjiMaliyetGrup model)
        {
            var result = await _treeAppService.AddEnerjiMaliyetGrup(model.Name);
            return Ok(result);
        }
        [HttpGet("GetEnerjiMaliyetParametreler")]
        public async Task<IActionResult> GetEnerjiMaliyetParametreler(int seciliYil, int enerjiMaliyetGrupId)
        {
            var result = await _treeAppService.GetEnerjiMaliyetParametreler(seciliYil, enerjiMaliyetGrupId);
            return Ok(result);
        }

        [HttpPost("AddEnerjiMaliyetParametreler")]
        public async Task<IActionResult> AddEnerjiMaliyetParametreler([FromBody] EnerjiMaliyetParametreKaydetModel model)
        {
            await _treeAppService.AddEnerjiMaliyetParametreler(model.EnerjiMaliyetParametreler, model.SeciliYil, model.EnerjiMaliyetGrupId, model.TarifeTipi);
            return Ok();
        }

        [HttpPost("UpdateEnerjiMaliyetGrup")]
        public async Task<IActionResult> UpdateEnerjiMaliyetGrup([FromBody] EnerjiMaliyetGrup model)
        {
            var result = await _treeAppService.UpdateEnerjiMaliyetGrup(model.Id, model.Name);

            return Ok(result);
        }

        [HttpPost("DeleteEnerjiMaliyetGrup")]
        public async Task<IActionResult> DeleteEnerjiMaliyetGrup([FromBody] int enerjiMaliyetGrupId)
        {
            await _treeAppService.DeleteEnerjiMaliyetGrup(enerjiMaliyetGrupId);

            return Ok();
        }

        [HttpGet("GetEnerjiMaliyetAtamaTree")]
        public async Task<IActionResult> GetEnerjiMaliyetAtamaTree(int lokasyonId, CancellationToken cancellationToken)
        {
            var result = await _treeAppService.GetEnerjiMaliyetAtamaTreeByLokasyonId(lokasyonId, cancellationToken);
            return Ok(result);
        }

        [HttpPost("AddEnerjiMaliyetParametreAtama")]
        public async Task<IActionResult> AddEnerjiMaliyetParametreAtama([FromBody] EnerjiMaliyetParametreAtamaRequest model)
        {
            await _treeAppService.AddEnerjiMaliyetParametreAtama(model.EnerjiMaliyetGrupId, model.NodeId, model.NodeType);
            return Ok();
        }

        [HttpPost("RemoveEnerjiMaliyetParametreAtama")]
        public async Task<IActionResult> RemoveEnerjiMaliyetParametreAtama([FromBody] EnerjiMaliyetParametreAtamaRemoveRequest model)
        {
            await _treeAppService.RemoveEnerjiMaliyetParametreAtama(model.NodeId, model.NodeType);
            return Ok();
        }

        [HttpGet("GetAktifEnerjiSayacDegeri")]
        public async Task<IActionResult> GetAktifEnerjiSayacDegeri(int cihazId, DateTime tarih, CancellationToken ct)
        {
            var result = await _treeAppService.GetAktifEnerjiSayacDegeri(cihazId, tarih, ct);
            return Ok(new
            {
                result.ImportAktifEnerjiWh,
                result.ExportAktifEnerjiWh,
                result.KayitTarihi
            });
        }

        //[HttpGet("GetToplamImportAktifEnerjiTuketimiAsync")]
        //public async Task<IActionResult> GetToplamImportAktifEnerjiTuketimiAsync(string cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken ct)
        //{
        //    var secilenCihazNodeIds = JsonConvert.DeserializeObject<List<int>>(cihazNodeIds);
        //    var result = await _treeAppService.GetToplamImportAktifEnerjiTuketimiAsync(secilenCihazNodeIds, baslangicZamani, bitisZamani, ct);
        //    return Ok(result);
        //}

        [HttpGet("GetToplamImportAktifEnerjiTuketimiAsync")]
        public async Task<IActionResult> GetToplamImportAktifEnerjiTuketimiAsync(string cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken ct)
        {
            var secilenCihazNodeIds = JsonConvert.DeserializeObject<List<int>>(cihazNodeIds);
            var result = await _treeAppService.GetToplamImportAktifEnerjiTuketimi(secilenCihazNodeIds, baslangicZamani, bitisZamani, ct);
            return Ok(result);
        }

        [HttpGet("GetGesDahilTreeByLokasyonId")]
        public async Task<IActionResult> GetGesDahilTreeByLokasyonId(int lokasyonId, CancellationToken ct, bool duzenlemeModu = false)
        {
            var result = await _treeAppService.GetGesDahilTreeByLokasyonId(lokasyonId, duzenlemeModu, ct);
            return new JsonResult(result);
        }
    }
}
