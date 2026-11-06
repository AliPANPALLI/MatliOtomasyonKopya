using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LokasyonController : BaseApiController<LokasyonViewModel>
    {
        private readonly ILokasyonAppService _lokasyonAppService;

        public LokasyonController(ILokasyonAppService lokasyonAppService) : base(lokasyonAppService)
        {
            _lokasyonAppService = lokasyonAppService;
        }

        [HttpGet("GetAllLokasyonForTicketTanim")]
        public async Task<IActionResult> GetAllLokasyonForTicketTanim()
        {
            var result = await _lokasyonAppService.GetAllLokasyonForTicketTanim();
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllLokasyonBySirketId")]
        public async Task<IActionResult> GetAllLokasyonBySirketId(int sirketId)
        {
            var result = await _lokasyonAppService.GetAllLokasyonBySirketId(sirketId);

            return Ok(result);
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _lokasyonAppService.SetSilindi(id);

            return Ok();
        }

        [HttpGet("GetAllLokasyonForYetki")]
        public async Task<IActionResult> GetAllMakineForYetki()
        {
            var result = await _lokasyonAppService.GetAllLokasyonForYetki();

            return Ok(result);
        }

        private static string AesEncrypt(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16]; // 16 byte boş IV

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs)) sw.Write(plainText);

            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
