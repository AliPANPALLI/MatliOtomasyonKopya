using Authentication.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using PanelIdentification.Application.Interfaces;
using PanelIdentification.Application.Services;
using PanelIdentification.Application.ViewModels;
using PanelIdentification.Application.ViewModels.ReportModels;
using PanelIdentification.Domain.Models;
using PanelIdentification.Domain.Models.ReportModels;
using SharedApp.Domain.Models;
using SharedKernel.Extensions;
using System.Security.Claims;
using System.Text.Json;

namespace PanelIdentification.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TreeController : ControllerBase
    {
        private readonly ITreeAppService _treeAppService;
        private readonly IUserExtension _userExtension;

        public TreeController(ITreeAppService treeAppService, IUserExtension userExtension)
        {
            _treeAppService = treeAppService;
            _userExtension = userExtension;
        }

        [HttpGet("GetTreeViewByLokasyonId")]
        public async Task<IActionResult> GetTreeViewByLokasyonId(int lokasyonId, bool onayliIslemlerDahil = false)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _treeAppService.GetTreeByLokasyonId(userId, lokasyonId, onayliIslemlerDahil);
            return new JsonResult(result);
        }

        [HttpGet("GetTreeViewByPanoKimlikNo")]
        public async Task<IActionResult> GetTreeViewByPanoKimlikNo(string panoKimlikNo, bool onayliIslemlerDahil = false)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.GetTreeByPanoKimlikNo2(userId, panoKimlikNo, onayliIslemlerDahil);

            return new JsonResult(result);
        }

        [HttpGet("GetTreeViewByNodeIdAndType")]
        public async Task<IActionResult> GetTreeViewByNodeIdAndType(int nodeId, string nodeType, bool onayliIslemlerDahil = true)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.GetTreeViewByNodeIdAndType(userId, nodeId, nodeType, onayliIslemlerDahil);

            return new JsonResult(result);
        }

        [HttpGet("GetTreeViewYetki")]
        public async Task<IActionResult> GetTreeViewYetki(string userId)
        {
            var result = await _treeAppService.GetTreeYetki(userId);

            return new JsonResult(result);
        }

        [HttpPost("AddNodeToTreeView")]
        public async Task<IActionResult> AddNodeToTreeView([FromBody] NodeModel node)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.AddNodeToTree(node, userId);

            return new JsonResult(result);
        }

        [HttpPost("AddCihazToTreeView")]
        public async Task<IActionResult> AddCihazToTreeView([FromBody] CihazViewModel cihaz, [FromQuery] int lokasyonId, CancellationToken cancellationToken)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.AddCihazToTreeView(cihaz, lokasyonId, cancellationToken, userId);
            return Ok(result);
        }

        [HttpPut("EditNodeFromTreeView")]
        public async Task<IActionResult> EditNodeFromTreeView([FromBody] EditNodeModel node)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.EditNodeFromTreeView(node, userId);

            return new JsonResult(result);
        }

        [HttpPut("DeleteNodeFromTreeView")]
        public async Task<IActionResult> DeleteNodeFromTreeView([FromBody] EditNodeModel node)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.DeleteNodeFromTreeView(node, userId);

            return new JsonResult(result);
        }

        [HttpGet("GetDosyalarByPanoId")]
        public async Task<IActionResult> GetDosyalarByPanoId(int panoId, string dosya)
        {
            var result = await _treeAppService.GetDosyalarByPanoId(panoId, dosya);

            return new JsonResult(result);
        }

        [HttpGet("GetAllDosyalarByPanoId")]
        public async Task<IActionResult> GetAllDosyalarByPanoId(int panoId, string dosya, bool onayliIslemlerDahil = false)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.GetAllDosyalarByPanoId(userId, panoId, dosya, onayliIslemlerDahil);

            return new JsonResult(result);
        }

        [HttpGet("GetDosyalarByDosyaYolu")]
        public async Task<IActionResult> GetDosyalarByDosyaYolu(string dosyaYolu, int panoId, bool? ustKlasorVarMi, bool onayliIslemlerDahil = false)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.GetDosyalarByDosyaYolu(userId, dosyaYolu, panoId, ustKlasorVarMi, onayliIslemlerDahil);
            return new JsonResult(result);
        }

        [HttpPut("EditDosyaAdiByDosyaYolu")]
        public async Task<IActionResult> EditDosyaAdiByDosyaYolu([FromBody] JsonElement fileData)
        {
            string dosyaYolu = fileData.GetProperty("dosyaYolu").GetString();
            string eskiDosyaAdi = fileData.GetProperty("eskiDosyaAdi").GetString();
            string yeniDosyaAdi = fileData.GetProperty("yeniDosyaAdi").GetString();
            string dosyaUzanti = fileData.GetProperty("dosyaUzanti").GetString();
            int panoId = Convert.ToInt32(fileData.GetProperty("panoId").GetInt32());
            int lokasyonId = Convert.ToInt32(fileData.GetProperty("lokasyonId").GetInt32());
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _treeAppService.EditDosyaAdiByDosyaYolu(dosyaYolu, eskiDosyaAdi, yeniDosyaAdi, dosyaUzanti, userId, panoId, lokasyonId);
            return Ok();
        }

        [HttpPost("UploadDosya")]
        public async Task<IActionResult> UploadDosya([FromForm] IFormFile[] files, [FromForm] string savePath, [FromForm] string panoId, [FromForm] string lokasyonId, CancellationToken cancellationToken)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.UploadDosya(files, savePath, panoId, lokasyonId, cancellationToken, userId);
            return Ok(result);
        }

        [HttpPost("DeleteDosya")]
        public async Task<IActionResult> DeleteDosya([FromBody] JsonElement fileData, CancellationToken cancellationToken)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            string fileName = fileData.GetProperty("fileName").GetString();
            string filePath = fileData.GetProperty("filePath").GetString();
            int panoId = Convert.ToInt32(fileData.GetProperty("panoId").GetInt32());
            int lokasyonId = Convert.ToInt32(fileData.GetProperty("lokasyonId").GetInt32());
            await _treeAppService.DeleteDosya(filePath + '\\' + fileName, cancellationToken, userId, panoId, lokasyonId);

            return Ok();
        }

        [HttpPost("DeleteKlasor")]
        public async Task<IActionResult> DeleteKlasor([FromBody] JsonElement fileData, CancellationToken cancellationToken)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            string fileName = fileData.GetProperty("folderName").GetString();
            string filePath = fileData.GetProperty("folderPath").GetString();
            int panoId = Convert.ToInt32(fileData.GetProperty("panoId").GetString());
            int lokasyonId = Convert.ToInt32(fileData.GetProperty("lokasyonId").GetString());

            await _treeAppService.DeleteKlasor(filePath + '\\' + fileName, cancellationToken, userId, panoId, lokasyonId);

            return Ok();
        }

        [HttpGet("GetDocument")]
        public async Task<IActionResult> GetDocument(string fileName, string filePath)
        {
            var result = await _treeAppService.GetDocument(fileName, filePath);

            return File(result.file, result.contentType, result.file.Name.Split("\\").Last());
        }

        [HttpGet("ReadTxtDocument")]
        public async Task<IActionResult> ReadTxtDocument(string filePath)
        {
            var result = await _treeAppService.ReadTxtDocument(filePath);

            return new JsonResult(result);
        }

        [HttpGet("EditTxtDocument")]
        public async Task<IActionResult> EditTxtDocument(string fileContent, string filePath, int panoId, int lokasyonId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _treeAppService.EditTxtDocument(fileContent, filePath, userId, panoId, lokasyonId);

            return Ok();
        }

        [HttpGet("GetCihazlarByPanoId")]
        public async Task<IActionResult> GetCihazlarByPanoId(int panoId, bool onayliIslemlerDahil = false)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.GetCihazlarByPanoId(panoId, userId, onayliIslemlerDahil);

            return new JsonResult(result);
        }

        [HttpGet("GetSorumluMuFromTreeNodeId")]
        public async Task<IActionResult> GetSorumluMuFromTreeNodeId(int nodeId, string nodeType)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.GetSorumluMuFromTreeNodeId(userId, nodeId, nodeType);
            return Ok(result);
        }

        [HttpPost("NodeOnayaGonder")]
        public async Task<IActionResult> NodeOnayaGonder([FromBody] EditNodeModel node)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _treeAppService.NodeOnayaGonder(node, userId);

            return new JsonResult(result);
        }
        //public static string GetFileContentType(string url)
        //{
        //    string extension = Path.GetExtension(url);
        //    string contentType = "";

        //    switch (extension)
        //    {
        //        case ".pdf":
        //            contentType = "application/pdf";
        //            break;
        //        case ".docx":
        //            contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        //            break;
        //        case ".gif":
        //            contentType = "image/gif";
        //            break;
        //        case ".jpeg":
        //            contentType = "image/jpeg";
        //            break;
        //        case ".jpg":
        //            contentType = "image/jpeg";
        //            break;
        //        case ".png":
        //            contentType = "image/png";
        //            break;
        //        case ".ppt":
        //            contentType = "application/vnd.ms-powerpoint";
        //            break;
        //        case ".pptx":
        //            contentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        //            break;
        //        case ".txt":
        //            contentType = "text/plain";
        //            break;
        //        case ".xls":
        //            contentType = "application/vnd.ms-excel";
        //            break;
        //        case ".xlsx":
        //            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        //            break;
        //        // and so on
        //        default:
        //            throw new ArgumentOutOfRangeException($"Unable to find Content Type for file name {extension}.");
        //    }

        //    return contentType;
        //}
    }
}