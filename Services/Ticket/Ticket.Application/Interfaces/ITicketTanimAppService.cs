using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Http;
using Ticket.Application.ViewModels;
using Ticket.Domain.DbViewModels;
using Ticket.Domain.Models;
using Ticket.Domain.Models.ReportModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace Ticket.Application.Interfaces
{
    public interface ITicketTanimAppService : IAppService<TicketTanimViewModel>
    {
        Task AddTicketTanim(int formId, string kisaTanim, int tedarikciId, int lokasyonId, List<string> secilenMatliTakipciler, string tanim, string olusturanUserId, IFormFile[]? files);
        Task<bool> AddTicketTanim2(int formId, string kisaTanim, int tedarikciId, int lokasyonId, List<string> secilenMatliTakipciler, string tanim, string olusturanUserId, IFormFile[]? files);
        Task EditTicketByTakipci(int ticketTanimId, int formId, string kisaTanim, int durumId, List<string> secilenMatliTakipciler, string tanim, bool teklifSabitle, string? sasNo, string? aciklama, bool? faturaKesildi, string olusturanUserId, IFormFile[]? files, string duzenleyenUserFullName);
        Task EditTicketBySorumlu(int ticketTanimId, List<int> secilenSorumlular, string? aciklama, int durumId, string olusturanUserId, decimal? teklifFiyat, int? paraBirimiId, string duzenleyenUserFullName);
        Task<List<TicketTanimReportModel>> GetAllTicketTanims();
        Task<TicketTanimListResultModel> GetAllTicketTanims(string userId);
        Task<List<TicketTanimReportModel>> GetAllFilteredTicketTanims(DateTime baslangicZamani, DateTime bitisZamani, string userId);
        Task<LoadResult> GetAllTicketTanimsDx(DataSourceLoadOptions loadOptions, DateTime? baslangicZamani, DateTime? bitisZamani, string userId);
        Task<TicketTanimReportModel> GetTicketById(int ticketTanimId, string userId);
        Task<List<Tedarikci>> GetAllTedarikciForTicketTanim();
        Task<List<TicketDurum>> GetAllTicketDurums();
        Task<List<vwTicketDurumLog>> GetAllvwTicketDurumLogs(int ticketTanimId);
        Task<List<vwTicketTeklifFiyatLog>> GetAllvwTicketTeklifFiyatLogs(int ticketTanimId);
        Task<List<ParaBirimi>> GetAllParaBirimis();
        Task<List<vwTedarikciUser>> GetAllvwTedarikciUser();
        Task<List<vwTicketAciklamaEsleme>> GetAllSorumluAciklamalarByTicketId(int ticketTanimId);
        Task<List<vwTicketAciklamaEsleme>> GetAllTakipciAciklamalarByTicketId(int ticketTanimId);
        Task<vwTedarikciUser> GetUserSorumluMu(string userId);
        Task<bool> GetTakipciUserKisitliYetki(string userId);
        Task<bool> GetTakipciUserYetkisiz(string userId);
        Task<bool> TeklifDurumDegistir(int ticketTanimId, byte teklifDurumId, string? aciklama, string userId, string duzenleyenUserFullName);
        Task<List<TicketDosyaModel>> GetAllFilesByTicketId(int ticketTanimId);
        Task<(string contentType, FileStream file)> GetDocument(string fileName, string filePath);
        void DeleteTicketFile(string filePath);
    }
}