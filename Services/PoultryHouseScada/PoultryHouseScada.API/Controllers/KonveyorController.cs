using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace PoultryHouseScada.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KonveyorController : BaseApiController<KonveyorViewModel>
    {
        private readonly IKonveyorAppService _konveyorAppService;

        public KonveyorController(IKonveyorAppService konveyorAppService) : base(konveyorAppService)
        {
            _konveyorAppService = konveyorAppService;
        }

        [HttpGet("GetKonveyorCizimIdByLokasyonId")]
        public async Task<IActionResult> GetKonveyorCizimIdByLokasyonId(int lokasyonId)
        {
            var result = await _konveyorAppService.GetKonveyorCizimIdByLokasyonId(lokasyonId);

            return Ok(result);
        }

        [HttpGet("GetAlarmLogForKonveyor")]
        public async Task<IActionResult> GetAlarmLogForKonveyor(int konveyorId)
        {
            var result = await _konveyorAppService.GetAlarmLogForKonveyor(konveyorId);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("GetAktifAlarmLogForKonveyor")]
        public async Task<IActionResult> GetAktifAlarmLogForKonveyor(int konveyorId)
        {
            var result = await _konveyorAppService.GetAktifAlarmLogForKonveyor(konveyorId);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("GetPasifAlarmLogForKonveyor")]
        public async Task<IActionResult> GetPasifAlarmLogForKumes(int konveyorId)
        {
            var result = await _konveyorAppService.GetPasifAlarmLogForKonveyor(konveyorId);

            return Ok(result);
        }

        [HttpGet("GetZamanCizelgesiFromSecilenAsync")]
        public async Task<IActionResult> GetZamanCizelgesiForKonveyor(string secilenKonveyorIdler, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var konveyorIdler = JsonConvert.DeserializeObject<List<int>>(secilenKonveyorIdler);

            var result = await _konveyorAppService.GetZamanCizelgesiFromSecilenAsync(konveyorIdler, baslangicZamani, bitisZamani);

            return Ok(result);
        }

        [HttpGet("GetKonveyorCalismaDurumuRaporu")]
        public async Task<IActionResult> GetKonveyorCalismaDurumuRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var result = await _konveyorAppService.GetKonveyorCalismaDurumuRaporu(konveyorLokasyonId, baslangicZamani, bitisZamani);
            return Ok(result);
        }

        [HttpGet("GetKonveyorArizaDurumuRaporu")]
        public async Task<IActionResult> GetKonveyorArizaDurumuRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var result = await _konveyorAppService.GetKonveyorArizaDurumuRaporu(konveyorLokasyonId, baslangicZamani, bitisZamani);
            return Ok(result);
        }

        [HttpGet("GetKonveyorKopmaRaporu")]
        public async Task<IActionResult> GetKonveyorKopmaRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var result = await _konveyorAppService.GetKonveyorKopmaRaporu(konveyorLokasyonId, baslangicZamani, bitisZamani);
            return Ok(result);
        }

        [HttpGet("GetZamanListesiFromSecilenAsync")]
        public async Task<IActionResult> GetZamanListesiFromSecilenAsync(string secilenKonveyorIdler, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var konveyorIdler = JsonConvert.DeserializeObject<List<int>>(secilenKonveyorIdler);

            var result = await _konveyorAppService.GetZamanListesiFromSecilenAsync(konveyorIdler, baslangicZamani, bitisZamani);

            return Ok(result);
        }
    }
}