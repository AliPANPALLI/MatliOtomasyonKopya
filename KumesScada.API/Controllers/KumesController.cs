using KumesScada.Application.Interfaces;
using KumesScada.Application.ViewModels;
using KumesScada.Domain.Models.ReportModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Extensions;
using System.Security.Claims;

namespace KumesScada.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KumesController : BaseApiController<KumesViewModel>
    {
        private readonly IKumesAppService _kumesAppService;
        private readonly IUserExtension _userExtension;

        public KumesController(IKumesAppService kumesAppService, IUserExtension userExtension) : base(kumesAppService)
        {
            _kumesAppService = kumesAppService;
            _userExtension = userExtension;
        }
        [HttpGet("GetKarsilastirilabilirKumeslerByKumesId")]
        public async Task<IActionResult> GetKarsilastirilabilirKumeslerByKumesId(int kumesId)
        {
            var result = await _kumesAppService.GetKarsilastirilabilirKumeslerByKumesId(kumesId);
            return Ok(result);
        }

        [HttpGet("GetAllKumesForYetki")]
        public async Task<IActionResult> GetAllKumesForYetki()
        {
            var result = await _kumesAppService.GetAllKumesForYetki();

            return Ok(result);
        }

        [HttpGet("GetLokasyonIdsByKumesIds")]
        public async Task<IActionResult> GetLokasyonIdsByKumesIds([FromQuery] List<int> kumesIds)
        {
            var result = await _kumesAppService.GetLokasyonIdsByKumesIds(kumesIds);

            return Ok(result);
        }

        [HttpGet("GetKumesIdsByLokasyonIds")]
        public async Task<IActionResult> GetKumesIdsByLokasyonIds([FromQuery] List<int> lokasyonIds)
        {
            var result = await _kumesAppService.GetKumesIdsByLokasyonIds(lokasyonIds);

            return Ok(result);
        }

        [HttpGet("GetKumeslerOzetRaporByLokasyonId")]
        public async Task<IActionResult> GetKumeslerOzetRaporByLokasyonId(int LokasyonId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _kumesAppService.GetKumeslerOzetRaporByLokasyonId(LokasyonId, userId);

            return new JsonResult(result);
        }

        [HttpGet("GetPlcBaglantiKesintiRaporuByLokasyonId")]
        public async Task<IActionResult> GetPlcBaglantiKesintiRaporuByLokasyonId(int lokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var result = await _kumesAppService.GetPlcBaglantiKesintiRaporuByLokasyonId(lokasyonId, baslangicZamani, bitisZamani);

            return Ok(result);
        }

        [HttpGet("GetPlcBaglantiKesintiDetayByKumesId")]
        public async Task<IActionResult> GetPlcBaglantiKesintiDetayByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var result = await _kumesAppService.GetPlcBaglantiKesintiDetayByKumesId(kumesId, baslangicZamani, bitisZamani);

            return Ok(result);
        }

        [HttpGet("GetTesisGenelIzlemeByLokasyonId")]
        public async Task<IActionResult> GetTesisGenelIzlemeByLokasyonId(int LokasyonId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _kumesAppService.GetTesisGenelIzlemeByLokasyonId(LokasyonId, userId);

            return new JsonResult(result);
        }

        [HttpGet("GetTesisGenelKartByLokasyonId")]
        public async Task<IActionResult> GetTesisGenelKartByLokasyonId(int LokasyonId)
        {
            var result = await _kumesAppService.GetTesisGenelKartByLokasyonId(LokasyonId);
            return new JsonResult(result);
        }

        [HttpGet("GetTesisGenelSnapshotByLokasyonId")]
        public async Task<IActionResult> GetTesisGenelSnapshotByLokasyonId(int LokasyonId)
        {
            if (LokasyonId <= 0)
                return BadRequest("Geçersiz lokasyon bilgisi.");

            var result = await _kumesAppService.GetTesisGenelSnapshotByLokasyonId(LokasyonId);

            return new JsonResult(result);
        }

        [HttpGet("GetTesisGenelIzlemeYetkiByLokasyonId")]
        public async Task<IActionResult> GetTesisGenelIzlemeYetkiByLokasyonId(int LokasyonId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Kullanıcı bilgisi bulunamadı.");

            if (LokasyonId <= 0)
                return BadRequest(
                    "Geçersiz lokasyon bilgisi.");

            var result = await _kumesAppService.GetTesisGenelIzlemeYetkiByLokasyonId(LokasyonId, userId);

            if (!result.ErisimVar)
                return Forbid();

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpGet("GetKumesRaporByKumesId")]
        public async Task<IActionResult> GetKumesRaporByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval)
        {
            var result = await _kumesAppService.GetKumesRaporByKumesId(kumesId, baslangicZamani, bitisZamani, dakikaInterval);

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpGet("GetKumesYumurtaRaporByKumesId")]
        public async Task<IActionResult> GetKumesYumurtaRaporByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval)
        {
            var result = await _kumesAppService.GetKumesYumurtaRaporByKumesId(kumesId, baslangicZamani, bitisZamani, dakikaInterval);

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpGet("GetKumesKarsilastirmaRaporu")]
        public async Task<IActionResult> GetKumesKarsilastirmaRaporu([FromQuery] List<int> kumesIds, [FromQuery] DateTime baslangicZamani, [FromQuery] DateTime bitisZamani, [FromQuery] int dakikaInterval)
        {
            var result = await _kumesAppService.GetKumesKarsilastirmaRaporu(kumesIds, baslangicZamani, bitisZamani, dakikaInterval);

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpPost("GetKumesKarsilastirmaRaporu")]
        public async Task<IActionResult> GetKumesKarsilastirmaRaporu([FromBody] KumesKarsilastirmaRaporRequest request)
        {
            if (request.KumesIds.Count == 0)
                return BadRequest("En az bir kümes seçilmelidir.");

            if (request.DakikaInterval <= 0)
                return BadRequest("Dakika aralığı sıfırdan büyük olmalıdır.");

            if (request.BitisZamani < request.BaslangicZamani)
                return BadRequest("Bitiş zamanı başlangıç zamanından önce olamaz.");

            var result = await _kumesAppService.GetKumesKarsilastirmaRaporu(
                request.KumesIds.Distinct().ToList(),
                request.BaslangicZamani,
                request.BitisZamani,
                request.DakikaInterval);

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpGet("GetKumesKarsilastirmaYumurtaRaporu")]
        public async Task<IActionResult> GetKumesKarsilastirmaYumurtaRaporu([FromQuery] List<int> kumesIds, [FromQuery] DateTime baslangicZamani, [FromQuery] DateTime bitisZamani, [FromQuery] int dakikaInterval)
        {
            var result = await _kumesAppService.GetKumesKarsilastirmaYumurtaRaporu(kumesIds, baslangicZamani, bitisZamani, dakikaInterval);

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpPost("GetKumesKarsilastirmaYumurtaRaporu")]
        public async Task<IActionResult> GetKumesKarsilastirmaYumurtaRaporu([FromBody] KumesKarsilastirmaRaporRequest request)
        {
            if (request.KumesIds.Count == 0)
                return BadRequest("En az bir kümes seçilmelidir.");

            if (request.DakikaInterval <= 0)
                return BadRequest("Dakika aralığı sıfırdan büyük olmalıdır.");

            if (request.BitisZamani < request.BaslangicZamani)
                return BadRequest("Bitiş zamanı başlangıç zamanından önce olamaz.");

            var result = await _kumesAppService.GetKumesKarsilastirmaYumurtaRaporu(
                request.KumesIds.Distinct().ToList(),
                request.BaslangicZamani,
                request.BitisZamani,
                request.DakikaInterval);

            return new JsonResult(result);
        }

        [HttpGet("GetKumesBilgileriByKumesId")]
        public async Task<IActionResult> GetKumesBilgileriByKumesId(int kumesId)
        {
            var result = await _kumesAppService.GetKumesBilgileriByKumesId(kumesId);

            return Ok(result);
        }

        [HttpGet("GetAlarmLogForKumes")]
        public async Task<IActionResult> GetAlarmLogForKumes(int kumesId)
        {
            var result = await _kumesAppService.GetAlarmLogForKumes(kumesId);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("GetAktifAlarmLogForKumes")]
        public async Task<IActionResult> GetAktifAlarmLogForKumes(int kumesId)
        {
            var result = await _kumesAppService.GetAktifAlarmLogForKumes(kumesId);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("GetPasifAlarmLogForKumes")]
        public async Task<IActionResult> GetPasifAlarmLogForKumes(int kumesId)
        {
            var result = await _kumesAppService.GetPasifAlarmLogForKumes(kumesId);

            return Ok(result);
        }

        [HttpGet("GetKumesHaberlesmeBilgileriByKumesId")]
        public async Task<IActionResult> GetKumesHaberlesmeBilgileriByKumesId(int kumesId)
        {
            var result = await _kumesAppService.GetKumesHaberlesmeBilgileriByKumesId(kumesId);

            return Ok(result);
        }

        [HttpPut("KumesHaberlesmeBilgileriDegistir")]
        public async Task<IActionResult> KumesHaberlesmeBilgileriDegistir(int kumesId, string plcIpAdres, bool plcAutoConnect, bool plcKumesAktif)
        {
            await _kumesAppService.KumesHaberlesmeBilgileriDegistir(kumesId, plcIpAdres, plcAutoConnect, plcKumesAktif);

            return Ok();
        }

        //[HttpGet("GetKumesAktifByKumesId")]
        //public async Task<IActionResult> GetKumesAktifByKumesId(int kumesId)
        //{
        //    var result = await _kumesAppService.GetKumesAktifByKumesId(kumesId);

        //    return Ok(result);
        //}

        [HttpGet("GetKumesDonemBilgisiByKumesId")]
        public async Task<IActionResult> GetKumesDonemBilgisiByKumesId(int kumesId)
        {
            var result = await _kumesAppService.GetKumesDonemBilgisiByKumesId(kumesId);

            return Ok(result);
        }

        [HttpGet("GetOlenHayvanSayisiByDate")]
        public async Task<IActionResult> GetOlenHayvanSayisiByDate(int kumesId, DateTime tarih)
        {
            var result = await _kumesAppService.GetOlenHayvanSayisiByDate(kumesId, tarih);

            return Ok(result);
        }

        [HttpPut("AddKumesHayvanLog")]
        public async Task<IActionResult> AddKumesHayvanLog(int kumesId, int oluHayvanAdet, int saglikliHayvanAdet, DateTime tarih)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            await _kumesAppService.AddKumesHayvanLog(kumesId, oluHayvanAdet, saglikliHayvanAdet, tarih, userId);
            return Ok();
        }

        [HttpPut("KumesDonemBitir")]
        public async Task<IActionResult> KumesDonemBitir(int kumesId, string kumesDonemName)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            await _kumesAppService.KumesDonemBitir(kumesId, kumesDonemName, userId);
            return Ok();
        }

        [HttpPut("KumesDonemBaslat")]
        public async Task<IActionResult> KumesDonemBaslat(int kumesId, DateTime girisTarihi, string cins, int hayvanAdet, string donemAdi, int girisHaftaYas)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            await _kumesAppService.KumesDonemBaslat(kumesId, girisTarihi, cins, hayvanAdet, donemAdi, girisHaftaYas, userId);
            return Ok();
        }

        [HttpGet("GetAllHayvanCins")]
        public async Task<IActionResult> GetAllHayvanCins()
        {
            var result = await _kumesAppService.GetAllHayvanCins();

            return Ok(result);
        }

        [HttpPost("range")]
        public async Task<IActionResult> GetTicariYumurtaKumesleriHayvanSayisi([FromBody] AnimalCountRangeRequest req, CancellationToken cancellationToken)
        {
            if (req.NodeIds == null || req.NodeIds.Length == 0)
                return BadRequest("NodeIds boş olamaz.");

            var start = req.StartDate.Date;
            var end = req.EndDate.Date;
            if (end < start) return BadRequest("EndDate < StartDate olamaz.");

            var items = await _kumesAppService.GetTicariYumurtaKumesleriHayvanSayisi(req.NodeIds, start, end, cancellationToken);
            return Ok(items);
        }

        [HttpGet("GetTesisYemRasyonAyarlariByLokasyonId")]
        public async Task<IActionResult> GetTesisYemRasyonAyarlariByLokasyonId(int LokasyonId, CancellationToken cancellationToken)
        {
            var result = await _kumesAppService.GetTesisYemRasyonAyarlariByLokasyonId(LokasyonId, cancellationToken);
            return new JsonResult(result);
        }

        [HttpPost("SaveTesisYemRasyonAyarlariByLokasyonId")]
        public async Task<IActionResult> SaveTesisYemRasyonAyarlariByLokasyonId([FromBody] TesisYemRasyonAyarModel tesisYemRasyonAyarModel, CancellationToken cancellationToken)
        {
            var result = await _kumesAppService.SaveTesisYemRasyonAyarlariByLokasyonId(tesisYemRasyonAyarModel, cancellationToken);
            return Ok(result);
        }
    }
}
