using Azure.Core;
using EnergyMonitoring.Application.Interfaces;
using EnergyMonitoring.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Extensions;
using System.Security.Claims;
using System.Text.Json;

namespace EnergyMonitoring.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TreeEditController : ControllerBase
    {
        private readonly IBolumAppService _bolumAppService;
        private readonly IMakineAppService _makineAppService;
        private readonly ICihazAppService _cihazAppService;
        private readonly IGatewayAppService _gatewayAppService;
        private readonly ICihazModelAppService _cihazModelAppService;
        private readonly IUserExtension _userExtension;

        public TreeEditController(IBolumAppService bolumAppService, IMakineAppService makineAppService, ICihazAppService cihazAppService, IGatewayAppService gatewayAppService, ICihazModelAppService cihazModelAppService, IUserExtension userExtension)
        {
            _bolumAppService = bolumAppService;
            _makineAppService = makineAppService;
            _cihazAppService = cihazAppService;
            _gatewayAppService = gatewayAppService;
            _cihazModelAppService = cihazModelAppService;
            _userExtension = userExtension;
        }
        private string GetUserIdFromContext()
        {
            // Try normal claims principal first
            var claim = _userExtension.GetClaimsIdentity()?.FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier || s.Type == "sub" || s.Type == "id" || s.Type == "userId" || s.Type == "nameid")?.Value;
            if (!string.IsNullOrEmpty(claim)) return claim;

            // Fallback: try to read Authorization header and decode token without validation
            try
            {
                var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(auth))
                {
                    var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth.Substring("Bearer ".Length).Trim() : auth;
                    if (!string.IsNullOrEmpty(token))
                    {
                        // Manual payload decode to avoid dependency on Jwt libs at runtime
                        var parts = token.Split('.');
                        if (parts.Length >= 2)
                        {
                            var payload = parts[1];
                            // Base64Url decode
                            payload = payload.Replace('-', '+').Replace('_', '/');
                            switch (payload.Length % 4)
                            {
                                case 2: payload += "=="; break;
                                case 3: payload += "="; break;
                            }
                            var bytes = Convert.FromBase64String(payload);
                            var json = System.Text.Encoding.UTF8.GetString(bytes);
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            string? val = null;
                            if (root.TryGetProperty(ClaimTypes.NameIdentifier, out var p)) val = p.GetString();
                            if (string.IsNullOrEmpty(val) && root.TryGetProperty("sub", out p)) val = p.GetString();
                            if (string.IsNullOrEmpty(val) && root.TryGetProperty("nameid", out p)) val = p.GetString();
                            if (string.IsNullOrEmpty(val) && root.TryGetProperty("id", out p)) val = p.GetString();
                            if (string.IsNullOrEmpty(val) && root.TryGetProperty("userId", out p)) val = p.GetString();
                            if (!string.IsNullOrEmpty(val)) return val;
                        }
                    }
                }
            }
            catch { }

            return string.Empty;
        }
        [HttpGet("GetAllCihaz")]
        public async Task<IActionResult> GetAllCihaz(int lokasyonId)
        {
            var result = await _cihazAppService.GetAllCihaz(lokasyonId);
            return new JsonResult(result);
        }

        [HttpGet("GetAllCihazModel")]
        public async Task<IActionResult> GetAllCihazModel()
        {
            var result = await _cihazAppService.GetAllCihazModel();
            return new JsonResult(result);
        }

        [HttpGet("GetAllGateway")]
        public async Task<IActionResult> GetAllGateway()
        {
            var result = await _cihazAppService.GetAllGateway();
            return new JsonResult(result);
        }

        [HttpGet("GetCihazById")]
        public async Task<IActionResult> GetCihazById(int cihazId)
        {
            //var result = await _cihazAppService.GetByIdAsync(cihazId);
            var result = await _cihazAppService.GetvwCihazOnayById(cihazId);
            return new JsonResult(result);
        }

        [HttpGet("GetMakineById")]
        public async Task<IActionResult> GetMakineById(int makineId)
        {
            //var result = await _makineAppService.GetByIdAsync(makineId);
            var result = await _makineAppService.GetvwMakineById(makineId);
            return new JsonResult(result);
        }

        [HttpGet("GetAllMakineByLokasyonId")]
        public async Task<IActionResult> GetAllMakineByLokasyonId(int lokasyonId)
        {
            var result = await _makineAppService.GetAllMakineByLokasyonId(lokasyonId);
            return new JsonResult(result);
        }

        [HttpGet("GetBolumById")]
        public async Task<IActionResult> GetBolumById(int bolumId)
        {
            //var result = await _bolumAppService.GetByIdAsync(bolumId);
            var result = await _bolumAppService.GetvwBolumById(bolumId);
            return new JsonResult(result);
        }

        [HttpGet("GetAllBolumByLokasyonId")]
        public async Task<IActionResult> GetAllBolumByLokasyonId(int lokasyonId)
        {
            var result = await _bolumAppService.GetAllBolumByLokasyonId(lokasyonId);
            return new JsonResult(result);
        }

        [HttpGet("GetGatewayById")]
        public async Task<IActionResult> GetGatewayById(int gatewayId)
        {
            var result = await _gatewayAppService.GetByIdAsync(gatewayId);
            return new JsonResult(result);
        }

        [HttpGet("GetCihazModelById")]
        public async Task<IActionResult> GetCihazModelById(int cihazModelId)
        {
            var result = await _cihazModelAppService.GetByIdAsync(cihazModelId);
            return new JsonResult(result);
        }

        [HttpPut("EditBolum")]
        public async Task<IActionResult> EditBolum([FromBody] BolumViewModel bolumViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _bolumAppService.EditBolum(bolumViewModel, userId);
            return Ok();
        }

        [HttpPut("SetBolumSilindi")]
        public async Task<IActionResult> SetBolumSilindi([FromBody] int id)
        {
            await _bolumAppService.SetSilindi(id);
            return Ok();
        }

        [HttpPut("EditMakine")]
        public async Task<IActionResult> EditMakine([FromBody] MakineViewModel makineViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _makineAppService.EditMakine(makineViewModel, userId);
            return Ok();
        }

        [HttpPut("SetMakineSilindi")]
        public async Task<IActionResult> SetMakineSilindi([FromBody] int id)
        {
            await _makineAppService.SetSilindi(id);
            return Ok();
        }

        [HttpPut("EditCihaz")]
        public async Task<IActionResult> EditCihaz([FromBody] CihazViewModel cihazViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _cihazAppService.EditCihaz(cihazViewModel, userId);
            return Ok();
        }

        [HttpPut("SetCihazSilindi")]
        public async Task<IActionResult> SetCihazSilindi([FromBody] int id)
        {
            await _cihazAppService.SetSilindi(id);
            return Ok();
        }

        [HttpPut("EditGateway")]
        public async Task<IActionResult> EditGateway([FromBody] GatewayViewModel gatewayViewModel)
        {
            await _gatewayAppService.EditGateway(gatewayViewModel);
            return Ok();
        }

        [HttpPut("SetGatewaySilindi")]
        public async Task<IActionResult> SetGatewaySilindi([FromBody] int id)
        {
            await _gatewayAppService.SetSilindi(id);
            return Ok();
        }

        [HttpPut("EditCihazModel")]
        public async Task<IActionResult> EditCihazModel([FromBody] CihazModelViewModel cihazModelViewModel)
        {
            await _cihazModelAppService.EditCihazModel(cihazModelViewModel);
            return Ok();
        }

        [HttpPut("SetCihazModelSilindi")]
        public async Task<IActionResult> SetCihazModelSilindi([FromBody] int id)
        {
            await _cihazModelAppService.SetSilindi(id);
            return Ok();
        }

        [HttpPost("AddBolum")]
        public async Task<IActionResult> AddBolum([FromBody] BolumViewModel bolumViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _bolumAppService.AddBolum(bolumViewModel, userId);
            return Ok();
        }

        [HttpPost("AddMakine")]
        public async Task<IActionResult> AddMakine([FromBody] MakineViewModel makineViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _makineAppService.AddMakine(makineViewModel, userId);
            return Ok();
        }

        [HttpPost("AddCihaz")]
        public async Task<IActionResult> AddCihaz([FromBody] CihazViewModel cihazViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _cihazAppService.AddCihaz(cihazViewModel, userId);
            return Ok();
        }

        [HttpPost("AddGateway")]
        public async Task<IActionResult> AddGateway([FromBody] GatewayViewModel gatewayViewModel)
        {
            await _gatewayAppService.AddGateway(gatewayViewModel);
            return Ok();
        }

        [HttpPost("AddCihazModel")]
        public async Task<IActionResult> AddCihazModel([FromBody] CihazModelViewModel cihazModelViewModel)
        {
            await _cihazModelAppService.AddCihazModel(cihazModelViewModel);
            return Ok();
        }

        [HttpPut("OnaylaTreeNode")]
        public async Task<IActionResult> OnaylaTreeNode([FromBody] OnaylaTreeNodeRequest request)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            if (request.NodeType == "bolum")
            {
                await _bolumAppService.OnaylaBolum(request.NodeId, userId);

            }
            else if (request.NodeType == "makine")
            {
                await _makineAppService.OnaylaMakine(request.NodeId, userId);

            }
            else if (request.NodeType == "cihaz")
            {
                await _cihazAppService.OnaylaCihaz(request.NodeId, userId);
            }
            return Ok();
        }
    }
}
public class OnaylaTreeNodeRequest
{
    public int NodeId { get; set; }
    public string NodeType { get; set; }
}