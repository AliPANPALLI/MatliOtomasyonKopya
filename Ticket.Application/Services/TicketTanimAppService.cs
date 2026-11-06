using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Mvc;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Extensions;
using System.Globalization;
using System.Text.Json;
using Ticket.Application.Interfaces;
using Ticket.Application.Services.Core;
using Ticket.Application.ViewModels;
using Ticket.Domain.DbViewModels;
using Ticket.Domain.Interfaces;
using Ticket.Domain.Models;
using Ticket.Domain.Models.ReportModels;
using static SharedKernel.ApplicationConstants;

namespace Ticket.Application.Services
{
    public class TicketTanimAppService : AppService<TicketTanim, TicketTanimViewModel>, ITicketTanimAppService
    {
        private readonly ITicketTanimRepository _ticketTanimRepository;
        private readonly ITicketSorumluUserEslemeRepository _ticketSorumluUserEslemeRepository;
        private readonly ITicketDurumRepository _ticketDurumRepository;
        private readonly ITicketMatliTakipciEslemeRepository _ticketMatliTakipciEslemeRepository;
        private readonly ITedarikciRepository _tedarikciRepository;
        private readonly ITicketLogsRepository _ticketLogsRepository;
        public TicketTanimAppService(IMapper mapper, ITicketTanimRepository ticketTanimRepository, ITicketMatliTakipciEslemeRepository ticketMatliTakipciEslemeRepository,
            ITicketSorumluUserEslemeRepository ticketSorumluUserEslemeRepository, ITicketDurumRepository ticketDurumRepository, ITedarikciRepository tedarikciRepository, ITicketLogsRepository ticketLogsRepository) : base(mapper, ticketTanimRepository)
        {
            _ticketTanimRepository = ticketTanimRepository;
            _ticketMatliTakipciEslemeRepository = ticketMatliTakipciEslemeRepository;
            _ticketDurumRepository = ticketDurumRepository;
            _ticketSorumluUserEslemeRepository = ticketSorumluUserEslemeRepository;
            _tedarikciRepository = tedarikciRepository;
            _ticketLogsRepository = ticketLogsRepository;
        }
        public async Task AddTicketTanim(int formId, string kisaTanim, int tedarikciId, int lokasyonId, List<string> secilenMatliTakipciler, string tanim, string olusturanUserId, IFormFile[]? files)
        {
            TicketTanim ticketTanim = new TicketTanim
            {
                OlusturanUserId = olusturanUserId,
                FormId = formId,
                TedarikciId = tedarikciId,
                LokasyonId = lokasyonId,
                Tanim = tanim,
                KisaTanim = kisaTanim,
                KayitZamani = DateTime.Now,
                DurumId = 1,
                DurumIdTedarikci = 1,
                TeklifSabitle = false
            };
            await _ticketTanimRepository.AddAsync(ticketTanim);
            await _ticketTanimRepository.SaveChangesAsync();

            List<TicketMatliTakipciEsleme> ticketMatliTakipciEslemeList = new List<TicketMatliTakipciEsleme>();
            foreach (var matliTakipci in secilenMatliTakipciler)
            {
                ticketMatliTakipciEslemeList.Add(new TicketMatliTakipciEsleme { TicketId = ticketTanim.Id, MatliUserId = matliTakipci });
            }

            await _ticketMatliTakipciEslemeRepository.AddRangeAsync(ticketMatliTakipciEslemeList);
            await _ticketMatliTakipciEslemeRepository.SaveChangesAsync();

            var vwTicket = await _ticketTanimRepository.GetvwTicketTanimById(ticketTanim.Id);
            if (vwTicket != null)
            {
                string ekYolu = "";
                if (files.Length > 0)
                {
                    //dosyalari kaydet
                    ekYolu = await SaveFiles(files, vwTicket.Id);
                }

                var sorumlular = (await _ticketTanimRepository.GetSorumlularForMailByTedarikciId(tedarikciId)).DistinctBy(x => x.Email).ToList();
                var takipciler = (await _ticketTanimRepository.GetTakipcilerForMailByTicketId(ticketTanim.Id)).DistinctBy(x => x.Email).ToList();

                string mailIcerikHtml = $@"
                    <p>Sn. İlgililer,</p>

                    <p><strong>{vwTicket.OlusturanUserFullName}</strong> tarafından <strong>'{kisaTanim}'</strong> konulu Ticket açılmıştır. (ID: {vwTicket.Id})</p>

                    <ul>
                        <li>Lokasyon:<strong> {vwTicket.LokasyonName}</strong></li>
                        <li>İş Tanımı:<strong> {vwTicket.Tanim}</strong></li>       
                    </ul>

                    <p>Saygılarımızla.</p>
                    <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                    <p> © {DateTime.Now.Year} Matlı Otomasyon.</p>";

                var toList = sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).ToList();
                var ccList = takipciler.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).ToList();
                var mailGonderileceklerTo = string.Join(", ", toList.Distinct());//sorumlular
                var mailGonderileceklerCc = string.Join(", ", ccList.Distinct());//takipciler
                var gonderimBasarili = await PostAddMailGonderimTakip("Ticket Açıldı", mailIcerikHtml, string.IsNullOrEmpty(ekYolu) ? null : ekYolu, true, "", mailGonderileceklerTo, mailGonderileceklerCc, true);
            }
        }

        public async Task<bool> AddTicketTanim2(int formId, string kisaTanim, int tedarikciId, int lokasyonId, List<string> secilenMatliTakipciler, string tanim, string olusturanUserId, IFormFile[] files)
        {
            try
            {
                TicketTanim ticketTanim = new TicketTanim
                {
                    OlusturanUserId = olusturanUserId,
                    FormId = formId,
                    TedarikciId = tedarikciId,
                    LokasyonId = lokasyonId,
                    Tanim = tanim,
                    KisaTanim = kisaTanim,
                    KayitZamani = DateTime.Now,
                    DurumId = 1
                };
                await _ticketTanimRepository.AddAsync(ticketTanim);
                await _ticketTanimRepository.SaveChangesAsync();

                List<TicketMatliTakipciEsleme> ticketMatliTakipciEslemeList = new List<TicketMatliTakipciEsleme>();
                foreach (var matliTakipci in secilenMatliTakipciler)
                {
                    ticketMatliTakipciEslemeList.Add(new TicketMatliTakipciEsleme { TicketId = ticketTanim.Id, MatliUserId = matliTakipci });
                }

                await _ticketMatliTakipciEslemeRepository.AddRangeAsync(ticketMatliTakipciEslemeList);
                await _ticketMatliTakipciEslemeRepository.SaveChangesAsync();

                var vwTicket = await _ticketTanimRepository.GetvwTicketTanimById(ticketTanim.Id);
                if (vwTicket != null)
                {
                    string ekYolu = "";
                    if (files.Length > 0)
                    {
                        //dosyalari kaydet
                        ekYolu = await SaveFiles(files, vwTicket.Id);
                    }

                    var sorumlular = (await _ticketTanimRepository.GetSorumlularForMailByTedarikciId(tedarikciId)).DistinctBy(x => x.Email).ToList();
                    var takipciler = (await _ticketTanimRepository.GetTakipcilerForMailByTicketId(ticketTanim.Id)).DistinctBy(x => x.Email).ToList();

                    string mailIcerikHtml = $@"
                    <p>Sn. İlgililer,</p>

                    <p><strong>{vwTicket.OlusturanUserFullName}</strong> tarafından <strong>'{kisaTanim}'</strong> konulu Ticket açılmıştır. (ID: {vwTicket.Id})</p>

                    <ul>
                        <li>Lokasyon:<strong> {vwTicket.LokasyonName}</strong></li>
                        <li>İş Tanımı:<strong> {vwTicket.Tanim}</strong></li>       
                    </ul>

                    <p>Saygılarımızla.</p>
                    <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                    <p> © {DateTime.Now.Year} Matlı Otomasyon.</p>";

                    var mailGonderileceklerTo = string.Join(", ", sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email));//sorumlular
                    var mailGonderileceklerCc = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email));//takipciler
                    var gonderimBasarili = await PostAddMailGonderimTakip("Ticket Açıldı", mailIcerikHtml, string.IsNullOrEmpty(ekYolu) ? null : ekYolu, true, "", mailGonderileceklerTo, mailGonderileceklerCc, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new BusinessException(ex.Message);
            }
        }

        public async Task EditTicketBySorumlu(int ticketTanimId, List<int> secilenSorumlular, string? aciklama, int durumId, string olusturanUserId, decimal? teklifFiyat, int? paraBirimiId, string duzenleyenUserFullName)
        {
            List<TicketLogs> ticketLogs = new List<TicketLogs>();
            var ticketTanim = await _ticketTanimRepository.GetByIdAsync(ticketTanimId);
            List<TicketDurum> ticketDurumList = new();
            string? eskiDurum = null;
            string? yeniDurum = null;
            bool durumDegisti = false;
            if (ticketTanim.DurumIdTedarikci != durumId)
            {
                durumDegisti = true;
                ticketDurumList = await _ticketDurumRepository.GetAllListAsync();
                eskiDurum = ticketDurumList.Where(x => x.Id == ticketTanim.DurumIdTedarikci).FirstOrDefault()?.Name;
                yeniDurum = ticketDurumList.Where(x => x.Id == durumId).FirstOrDefault()?.Name;
                await _ticketTanimRepository.AddTicketDurumLog(ticketTanim.Id, ticketTanim.DurumIdTedarikci, durumId, true, null, olusturanUserId);
                ticketLogs.Add(new TicketLogs
                {
                    TicketId = ticketTanimId,
                    MatliUserId = olusturanUserId,
                    KayitZamani = DateTime.Now,
                    YapilanDegisiklik = $"Ticket tedarikçi durumu değiştirildi. Eski Tedarikçi Durum Id: {ticketTanim.DurumIdTedarikci}. Yeni Tedarikçi Durum Id: {durumId}."
                });
            }
            //todo:sorumlu degilse(tedarikciUserMatliUserVarMi null ise) return olmali
            if (aciklama != null)
            {
                var tedarikciUserMatliUserVarMi = await _ticketTanimRepository.GetvwTedarikciUserForMatliUser(olusturanUserId, ticketTanim.TedarikciId);
                await _ticketTanimRepository.AddTicketAciklamaEsleme(ticketTanim.Id, aciklama, olusturanUserId, tedarikciUserMatliUserVarMi?.Id, null);
                ticketLogs.Add(new TicketLogs
                {
                    TicketId = ticketTanimId,
                    MatliUserId = olusturanUserId,
                    KayitZamani = DateTime.Now,
                    YapilanDegisiklik = $"Ticket açıklaması girildi. Açıklama: {aciklama}. Kullanıcı: {duzenleyenUserFullName}."
                });
            }
            if (ticketTanim.TeklifFiyat != teklifFiyat || ticketTanim.TeklifParaBirimiId != paraBirimiId)
            {
                await _ticketTanimRepository.AddTicketTeklifFiyatLog(ticketTanim.Id, (decimal)ticketTanim.TeklifFiyat.GetValueOrDefault(0), ticketTanim.TeklifParaBirimiId.GetValueOrDefault(0), (decimal)teklifFiyat.GetValueOrDefault(0), paraBirimiId.GetValueOrDefault(0), null, olusturanUserId);
            }

            bool teklifFiyatDegisti = false;
            bool ilkTeklifVerildi = false;
            bool teklifSilindi = false;
            decimal? eskiTeklifFiyat = null;
            decimal? yeniTeklifFiyat = null;
            eskiTeklifFiyat = ticketTanim.TeklifFiyat;
            yeniTeklifFiyat = teklifFiyat;
            if (ticketTanim.TeklifFiyat != teklifFiyat)
            {
                if (ticketTanim.TeklifFiyat == null)
                {
                    ilkTeklifVerildi = true;
                }
                if (teklifFiyat == null)
                {
                    teklifSilindi = true;
                }
                teklifFiyatDegisti = true;
                //eskiTeklifFiyat = ticketTanim.TeklifFiyat;
                //yeniTeklifFiyat = teklifFiyat;
                ticketLogs.Add(new TicketLogs
                {
                    TicketId = ticketTanimId,
                    MatliUserId = olusturanUserId,
                    KayitZamani = DateTime.Now,
                    YapilanDegisiklik = $"Ticket teklif fiyatı değiştirildi. Eski Fiyat: {ticketTanim.TeklifFiyat}. Yeni Fiyat: {teklifFiyat}. Kullanıcı: {duzenleyenUserFullName}."
                });
                ticketTanim.TeklifKararDurumu = null;
                if (ticketTanim.TeklifFiyat != null)
                {
                    //await _ticketTanimRepository.AddTicketTeklifFiyatLog(ticketTanim.Id, (decimal)eskiTeklifFiyat.GetValueOrDefault(0), ticketTanim.TeklifParaBirimiId.GetValueOrDefault(0), (decimal)yeniTeklifFiyat.GetValueOrDefault(0), paraBirimiId.GetValueOrDefault(0), null, olusturanUserId);
                    var teklifVerildiDurumId = await _ticketDurumRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Name.Contains("Teklif verildi"));
                    if (ticketTanim.DurumIdTedarikci != (teklifVerildiDurumId != null ? teklifVerildiDurumId.Id : 8))//Teklif Verildi
                    {
                        durumDegisti = true;
                        ticketDurumList = await _ticketDurumRepository.GetAllListAsync();
                        eskiDurum = ticketDurumList.Where(x => x.Id == ticketTanim.DurumIdTedarikci).FirstOrDefault()?.Name;
                        await _ticketTanimRepository.AddTicketDurumLog(ticketTanim.Id, ticketTanim.DurumIdTedarikci, durumId, true, null, olusturanUserId);
                        ticketLogs.Add(new TicketLogs
                        {
                            TicketId = ticketTanimId,
                            MatliUserId = olusturanUserId,
                            KayitZamani = DateTime.Now,
                            YapilanDegisiklik = $"Ticket tedarikçi durumu fiyat değiştirildiği için değiştirildi. Eski Tedarikçi Durum Id: {ticketTanim.DurumIdTedarikci}. Seçili Tedarikçi Durum Id: {durumId}. Yeni Tedarikçi Durum Id: {(teklifVerildiDurumId != null ? teklifVerildiDurumId.Id : 8)}."
                        });
                        durumId = teklifVerildiDurumId != null ? teklifVerildiDurumId.Id : 8;
                        yeniDurum = ticketDurumList.Where(x => x.Id == durumId).FirstOrDefault()?.Name;
                    }
                }
            }
            bool teklifFiyatBirimDegisti = false;
            List<ParaBirimi> paraBirimiList = await GetAllParaBirimis();
            string? eskiTeklifParaBirimi = null;
            string? yeniTeklifParaBirimi = null;
            eskiTeklifParaBirimi = paraBirimiList.Where(x => x.Id == ticketTanim.TeklifParaBirimiId).FirstOrDefault()?.Birim;
            yeniTeklifParaBirimi = paraBirimiList.Where(x => x.Id == paraBirimiId).FirstOrDefault()?.Birim;
            if (ticketTanim.TeklifParaBirimiId != paraBirimiId)
            {
                teklifFiyatBirimDegisti = true;
                //eskiTeklifParaBirimi = paraBirimiList.Where(x => x.Id == ticketTanim.TeklifParaBirimiId).FirstOrDefault()?.Birim;
                //yeniTeklifParaBirimi = paraBirimiList.Where(x => x.Id == paraBirimiId).FirstOrDefault()?.Birim;
                ticketLogs.Add(new TicketLogs
                {
                    TicketId = ticketTanimId,
                    MatliUserId = olusturanUserId,
                    KayitZamani = DateTime.Now,
                    YapilanDegisiklik = $"Ticket teklif para birimi değiştirildi. Eski Birim Id: {ticketTanim.TeklifParaBirimiId}. Yeni Birim Id: {paraBirimiId}. Kullanıcı: {duzenleyenUserFullName}."
                });
                ticketTanim.TeklifKararDurumu = null;
                //await _ticketTanimRepository.AddTicketTeklifFiyatLog(ticketTanim.Id, (decimal)eskiTeklifFiyat.GetValueOrDefault(0), ticketTanim.TeklifParaBirimiId.GetValueOrDefault(0), (decimal)yeniTeklifFiyat.GetValueOrDefault(0), paraBirimiId.GetValueOrDefault(0), null, olusturanUserId);
                if (ticketTanim.TeklifFiyat != null)
                {
                    var teklifVerildiDurumId = await _ticketDurumRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Name.Contains("Teklif verildi"));
                    if (ticketTanim.DurumIdTedarikci != (teklifVerildiDurumId != null ? teklifVerildiDurumId.Id : 8))//Teklif Verildi
                    {
                        durumDegisti = true;
                        ticketDurumList = await _ticketDurumRepository.GetAllListAsync();
                        eskiDurum = ticketDurumList.Where(x => x.Id == ticketTanim.DurumIdTedarikci).FirstOrDefault()?.Name;
                        await _ticketTanimRepository.AddTicketDurumLog(ticketTanim.Id, ticketTanim.DurumIdTedarikci, durumId, true, null, olusturanUserId);
                        ticketLogs.Add(new TicketLogs
                        {
                            TicketId = ticketTanimId,
                            MatliUserId = olusturanUserId,
                            KayitZamani = DateTime.Now,
                            YapilanDegisiklik = $"Ticket tedarikçi durumu fiyat değiştirildiği için değiştirildi. Eski Tedarikçi Durum Id: {ticketTanim.DurumIdTedarikci}. Seçili Tedarikçi Durum Id: {durumId}. Yeni Tedarikçi Durum Id: {(teklifVerildiDurumId != null ? teklifVerildiDurumId.Id : 8)}."
                        });
                        durumId = teklifVerildiDurumId != null ? teklifVerildiDurumId.Id : 8;
                        yeniDurum = ticketDurumList.Where(x => x.Id == durumId).FirstOrDefault()?.Name;
                    }
                }
            }

            ticketTanim.DurumIdTedarikci = durumId;
            ticketTanim.TeklifFiyat = teklifFiyat;
            ticketTanim.TeklifParaBirimiId = (teklifFiyat != null) ? (paraBirimiId == null) ? 2 : paraBirimiId : null;
            ticketTanim.GuncellemeZamani = DateTime.Now;
            ticketTanim.GuncelleyenUserId = olusturanUserId;

            var ticketSorumlular = await _ticketSorumluUserEslemeRepository.GetAllAsyncByPredicate(x => x.TicketId == ticketTanimId && x.Silindi != true).ToListAsync();
            foreach (var ticketSorumlu in ticketSorumlular)
            {
                ticketSorumlu.Silindi = true;
            }

            List<TicketSorumluUserEsleme> TicketSorumluUserEslemeList = new List<TicketSorumluUserEsleme>();
            foreach (var ticketSorumlu in secilenSorumlular)
            {
                TicketSorumluUserEslemeList.Add(new TicketSorumluUserEsleme { TicketId = ticketTanim.Id, TedarikciUserId = (int)ticketSorumlu });
            }

            await _ticketSorumluUserEslemeRepository.AddRangeAsync(TicketSorumluUserEslemeList);
            await _ticketLogsRepository.AddRangeAsync(ticketLogs);
            await _ticketSorumluUserEslemeRepository.SaveChangesAsync();

            var vwTicket = await _ticketTanimRepository.GetvwTicketTanimById(ticketTanim.Id);
            if (aciklama != null || durumDegisti || teklifFiyatBirimDegisti || teklifFiyatDegisti || ilkTeklifVerildi)
            {
                var sorumlular = await _ticketSorumluUserEslemeRepository.GetAllvwTicketSorumluUserEslemeByTicketId(ticketTanimId);
                var takipciler = await _ticketMatliTakipciEslemeRepository.GetAllvwTicketMatliTakipciEslemesByTicketId(ticketTanimId);
                string mailIcerikHtml = string.Empty;

                var toList = sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).ToList();
                var ccList = takipciler.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).ToList();
                toList.Add("okaniskol@piotomasyon.com.tr");
                var mailGonderileceklerTo = string.Join(", ", toList.Distinct());//sorumlular
                var mailGonderileceklerCc = string.Join(", ", ccList.Distinct());//takipciler

                if (aciklama != null)
                {
                    mailIcerikHtml = $@"
                    <p>Sn. İlgililer,</p>

                    <p><strong>{duzenleyenUserFullName}</strong> tarafından açıklama girilmiştir. (ID: {vwTicket.Id})</p>

                    <ul>
                        <li>Açıklama:<strong> {aciklama}</strong></li>       
                        <li>Lokasyon:<strong> {vwTicket.LokasyonName}</strong></li>
                        <li>Kısa Tanımı:<strong> {vwTicket.KisaTanim}</strong></li>       
                        <li>İş Tanımı:<strong> {vwTicket.Tanim}</strong></li>       
                    </ul>

                    <p>Saygılarımızla.</p>
                    <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                    <p> © {DateTime.Now.Year} Matlı Otomasyon.</p>";
                    var gonderimBasarili = await PostAddMailGonderimTakip("Ticket Açıklaması Girildi", mailIcerikHtml, null, true, "", mailGonderileceklerTo, mailGonderileceklerCc, true);
                }

                if (durumDegisti && !ilkTeklifVerildi && !teklifFiyatDegisti)
                {
                    mailIcerikHtml = $@"
                    <p>Sn. İlgililer,</p>

                     <p><strong>{duzenleyenUserFullName}</strong> tarafından ticket tedarikçi durumu <strong>{eskiDurum}</strong> yerine <strong>{vwTicket.DurumNameTedarikci}</strong> olarak değiştirilmiştir. (ID: {vwTicket.Id})</p>

                    <ul>
                        <li>Lokasyon:<strong> {vwTicket.LokasyonName}</strong></li>
                        <li>Kısa Tanımı:<strong> {vwTicket.KisaTanim}</strong></li>       
                        <li>İş Tanımı:<strong> {vwTicket.Tanim}</strong></li>       
                    </ul>

                    <p>Saygılarımızla.</p>
                    <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                    <p> © {DateTime.Now.Year} Matlı Otomasyon.</p>";
                    var gonderimBasarili = await PostAddMailGonderimTakip("Ticket Tedarikçi Durumu Değiştirildi", mailIcerikHtml, null, true, "", mailGonderileceklerTo, mailGonderileceklerCc, true);
                }
                if (ilkTeklifVerildi)
                {
                    if (eskiTeklifParaBirimi == null)
                    {
                        eskiTeklifParaBirimi = "--";
                    }
                    if (yeniTeklifParaBirimi == null)
                    {
                        yeniTeklifParaBirimi = "--";
                    }
                    var tr = new CultureInfo("tr-TR");
                    string teklifFiyatText = vwTicket.TeklifFiyat.HasValue ? vwTicket.TeklifFiyat.Value.ToString("N2", tr) : "--";
                    string eskiTeklifFiyatText = eskiTeklifFiyat.HasValue ? eskiTeklifFiyat.Value.ToString("N2", tr) : "--";

                    mailIcerikHtml = $@"
                    <p>Sn. İlgililer,</p>

                     <p><strong>{duzenleyenUserFullName}</strong> tarafından teklif fiyatı <strong>{teklifFiyatText} {yeniTeklifParaBirimi}</strong> olarak girilmiştir. (ID: {vwTicket.Id})</p>

                    <ul>
                        <li>Lokasyon:<strong> {vwTicket.LokasyonName}</strong></li>
                        <li>Kısa Tanımı:<strong> {vwTicket.KisaTanim}</strong></li>       
                        <li>İş Tanımı:<strong> {vwTicket.Tanim}</strong></li>       
                    </ul>

                    <p>Saygılarımızla.</p>
                    <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                    <p> © {DateTime.Now.Year} Matlı Otomasyon.</p>";
                    var gonderimBasarili = await PostAddMailGonderimTakip("Ticket için Teklif Verildi", mailIcerikHtml, null, true, "", mailGonderileceklerTo, mailGonderileceklerCc, true);
                }
                else if (teklifSilindi)
                {
                    if (eskiTeklifParaBirimi == null)
                    {
                        eskiTeklifParaBirimi = "--";
                    }

                    var tr = new CultureInfo("tr-TR");
                    string eskiTeklifFiyatText = eskiTeklifFiyat.HasValue ? eskiTeklifFiyat.Value.ToString("N2", tr) : "--";

                    mailIcerikHtml = $@"
                    <p>Sn. İlgililer,</p>

                     <p><strong>{duzenleyenUserFullName}</strong> tarafından teklif fiyatı geri çekilmiştir. (ID: {vwTicket.Id})</p>

                    <ul>
                        <li>Eski Teklif Fiyatı:<strong> {eskiTeklifFiyatText} {eskiTeklifParaBirimi}</strong></li>       
                        <li>Lokasyon:<strong> {vwTicket.LokasyonName}</strong></li>
                        <li>Kısa Tanımı:<strong> {vwTicket.KisaTanim}</strong></li>       
                        <li>İş Tanımı:<strong> {vwTicket.Tanim}</strong></li>       
                    </ul>

                    <p>Saygılarımızla.</p>
                    <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                    <p> © {DateTime.Now.Year} Matlı Otomasyon.</p>";
                    var gonderimBasarili = await PostAddMailGonderimTakip("Ticket için Teklif Silindi", mailIcerikHtml, null, true, "", mailGonderileceklerTo, mailGonderileceklerCc, true);
                }
                else if (teklifFiyatDegisti || teklifFiyatBirimDegisti)
                {
                    if (eskiTeklifParaBirimi == null)
                    {
                        eskiTeklifParaBirimi = "--";
                    }
                    if (yeniTeklifParaBirimi == null)
                    {
                        yeniTeklifParaBirimi = "--";
                    }
                    var tr = new CultureInfo("tr-TR");
                    string teklifFiyatText = vwTicket.TeklifFiyat.HasValue ? vwTicket.TeklifFiyat.Value.ToString("N2", tr) : "--";
                    string eskiTeklifFiyatText = eskiTeklifFiyat.HasValue ? eskiTeklifFiyat.Value.ToString("N2", tr) : "--";

                    mailIcerikHtml = $@"
                    <p>Sn. İlgililer,</p>

                     <p><strong>{duzenleyenUserFullName}</strong> tarafından teklif fiyatı <strong>{teklifFiyatText} {yeniTeklifParaBirimi}</strong> olarak değiştirilmiştir. (ID: {vwTicket.Id})</p>

                    <ul>
                        <li>Eski Teklif Fiyatı:<strong> {eskiTeklifFiyatText} {eskiTeklifParaBirimi}</strong></li>       
                        <li>Lokasyon:<strong> {vwTicket.LokasyonName}</strong></li>
                        <li>Kısa Tanımı:<strong> {vwTicket.KisaTanim}</strong></li>       
                        <li>İş Tanımı:<strong> {vwTicket.Tanim}</strong></li>       
                    </ul>

                    <p>Saygılarımızla.</p>
                    <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                    <p> © {DateTime.Now.Year} Matlı Otomasyon.</p>";
                    var gonderimBasarili = await PostAddMailGonderimTakip("Ticket Teklif Fiyatı Değiştirildi", mailIcerikHtml, null, true, "", mailGonderileceklerTo, mailGonderileceklerCc, true);
                }
            }
        }

        public async Task EditTicketByTakipci(int ticketTanimId, int formId, string kisaTanim, int durumId, List<string> secilenMatliTakipciler, string tanim, bool teklifSabitle, string? sasNo, string? aciklama, bool? faturaKesildi, string olusturanUserId, IFormFile[] files, string duzenleyenUserFullName)
        {
            try
            {
                List<TicketLogs> ticketLogs = new List<TicketLogs>();
                var ticketTanim = await _ticketTanimRepository.GetByIdAsync(ticketTanimId);
                List<TicketDurum> ticketDurumList = new();
                string? eskiDurum = null;
                string? yeniDurum = null;
                bool durumDegisti = false;
                if (ticketTanim.DurumId != durumId)
                {
                    ticketDurumList = await _ticketDurumRepository.GetAllListAsync();
                    eskiDurum = ticketDurumList.Where(x => x.Id == ticketTanim.DurumId).FirstOrDefault()?.Name;
                    yeniDurum = ticketDurumList.Where(x => x.Id == durumId).FirstOrDefault()?.Name;
                    durumDegisti = true;
                    await _ticketTanimRepository.AddTicketDurumLog(ticketTanimId, ticketTanim.DurumId, durumId, false, null, olusturanUserId);
                }

                if (aciklama != null)
                {
                    await _ticketTanimRepository.AddTicketAciklamaEsleme(ticketTanim.Id, aciklama, olusturanUserId, null, olusturanUserId);
                    ticketLogs.Add(new TicketLogs
                    {
                        TicketId = ticketTanimId,
                        MatliUserId = olusturanUserId,
                        KayitZamani = DateTime.Now,
                        YapilanDegisiklik = $"Ticket açıklaması girildi. Açıklama: {aciklama}. Kullanıcı: {duzenleyenUserFullName}."
                    });
                }
                if (ticketTanim.DurumId != durumId)
                {
                    ticketLogs.Add(new TicketLogs
                    {
                        TicketId = ticketTanimId,
                        MatliUserId = olusturanUserId,
                        KayitZamani = DateTime.Now,
                        YapilanDegisiklik = $"Ticket durumu değiştirildi. Eski Durum Id: {ticketTanim.DurumId}. Yeni Durum Id: {durumId}."
                    });
                    if (ticketTanim.DurumId == 2)//durumu tamamlandi yapildiysa fiyat sabitle
                    {
                        ticketTanim.TeklifSabitle = true;
                        ticketLogs.Add(new TicketLogs
                        {
                            TicketId = ticketTanimId,
                            MatliUserId = olusturanUserId,
                            KayitZamani = DateTime.Now,
                            YapilanDegisiklik = $"Ticket Sabitleme değiştirildi(Tamamlandı). Eski Değer: {ticketTanim.TeklifSabitle}. Yeni Değer: {true}. Kullanıcı: {duzenleyenUserFullName}."
                        });
                    }
                }
                if (faturaKesildi != null)
                {
                    ticketLogs.Add(new TicketLogs
                    {
                        TicketId = ticketTanimId,
                        MatliUserId = olusturanUserId,
                        KayitZamani = DateTime.Now,
                        YapilanDegisiklik = $"Ticket fatura kesildi değiştirildi. Eski Değer: {ticketTanim.FaturaKesildi}. Yeni Değer: {faturaKesildi}. Kullanıcı: {duzenleyenUserFullName}."
                    });
                }

                ticketTanim.FormId = formId;
                ticketTanim.DurumId = durumId;
                ticketTanim.KisaTanim = kisaTanim;
                ticketTanim.Tanim = tanim;
                ticketTanim.FaturaKesildi = faturaKesildi;

                //ticketTanim.TeklifSabitle = teklifSabitle;
                if (ticketTanim.TeklifKararDurumu == TicketTeklifKararDurumu.Onaylandi)
                {
                    teklifSabitle = true;
                }
                if (ticketTanim.TeklifSabitle != teklifSabitle)
                {

                    ticketLogs.Add(new TicketLogs
                    {
                        TicketId = ticketTanimId,
                        MatliUserId = olusturanUserId,
                        KayitZamani = DateTime.Now,
                        YapilanDegisiklik = $"Ticket Sabitleme değiştirildi. Eski Değer: {ticketTanim.TeklifSabitle}. Yeni Değer: {teklifSabitle}. Kullanıcı: {duzenleyenUserFullName}."
                    });
                }
                ticketTanim.TeklifSabitle = teklifSabitle;

                if (ticketTanim.SasNo != sasNo)
                {
                    ticketLogs.Add(new TicketLogs
                    {
                        TicketId = ticketTanimId,
                        MatliUserId = olusturanUserId,
                        KayitZamani = DateTime.Now,
                        YapilanDegisiklik = $"Ticket SAS No değiştirildi. Eski Değer: {ticketTanim.SasNo}. Yeni Değer: {sasNo}. Kullanıcı: {duzenleyenUserFullName}."
                    });
                }
                ticketTanim.SasNo = sasNo;
                ticketTanim.GuncellemeZamani = DateTime.Now;
                ticketTanim.GuncelleyenUserId = olusturanUserId;

                var matliTakipciler = await _ticketMatliTakipciEslemeRepository.GetAllAsyncByPredicate(x => x.TicketId == ticketTanimId && x.Silindi != true).ToListAsync();
                foreach (var matliTakipci in matliTakipciler)
                {
                    matliTakipci.Silindi = true;
                }

                List<TicketMatliTakipciEsleme> ticketMatliTakipciEslemeList = new List<TicketMatliTakipciEsleme>();
                foreach (var matliTakipci in secilenMatliTakipciler)
                {
                    ticketMatliTakipciEslemeList.Add(new TicketMatliTakipciEsleme { TicketId = ticketTanim.Id, MatliUserId = matliTakipci });
                }

                string ekYolu = "";
                if (files.Length > 0)
                {
                    //dosyalari kaydet
                    ekYolu = await SaveFiles(files, ticketTanim.Id);
                }

                await _ticketMatliTakipciEslemeRepository.AddRangeAsync(ticketMatliTakipciEslemeList);
                await _ticketLogsRepository.AddRangeAsync(ticketLogs);
                await _ticketMatliTakipciEslemeRepository.SaveChangesAsync();
                var vwTicket = await _ticketTanimRepository.GetvwTicketTanimById(ticketTanim.Id);

                if (aciklama != null || durumDegisti)
                {
                    var sorumlular = await _ticketSorumluUserEslemeRepository.GetAllvwTicketSorumluUserEslemeByTicketId(ticketTanimId);
                    var takipciler = await _ticketMatliTakipciEslemeRepository.GetAllvwTicketMatliTakipciEslemesByTicketId(ticketTanimId);
                    string mailIcerikHtml = string.Empty;
                    var toList = sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).ToList();
                    var ccList = takipciler.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).ToList();
                    toList.Add("okaniskol@piotomasyon.com.tr");
                    var mailGonderileceklerTo = string.Join(", ", toList.Distinct());//sorumlular
                    var mailGonderileceklerCc = string.Join(", ", ccList.Distinct());//takipciler

                    if (aciklama != null)
                    {
                        mailIcerikHtml = $@"
                    <p>Sn. İlgililer,</p>

                     <p><strong>{duzenleyenUserFullName}</strong> tarafından açıklama girilmiştir. (ID: {vwTicket.Id})</p>

                    <ul>
                        <li>Açıklama:<strong> {aciklama}</strong></li>       
                        <li>Lokasyon:<strong> {vwTicket.LokasyonName}</strong></li>
                        <li>Kısa Tanımı:<strong> {vwTicket.KisaTanim}</strong></li>       
                        <li>İş Tanımı:<strong> {vwTicket.Tanim}</strong></li>       
                    </ul>

                    <p>Saygılarımızla.</p>
                    <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                    <p> © {DateTime.Now.Year} Matlı Otomasyon.</p>";

                        var gonderimBasarili = await PostAddMailGonderimTakip("Ticket Açıklaması Girildi", mailIcerikHtml, string.IsNullOrEmpty(ekYolu) ? null : ekYolu, true, "", mailGonderileceklerTo, mailGonderileceklerCc, true);
                    }
                    if (durumDegisti)
                    {
                        mailIcerikHtml = $@"
                    <p>Sn. İlgililer,</p>

                     <p><strong>{duzenleyenUserFullName}</strong> tarafından ticket durumu <strong>{eskiDurum}</strong> yerine <strong>{vwTicket.DurumName}</strong> olarak değiştirilmiştir. (ID: {vwTicket.Id})</p>

                    <ul>
                        <li>Lokasyon:<strong> {vwTicket.LokasyonName}</strong></li>
                        <li>Kısa Tanımı:<strong> {vwTicket.KisaTanim}</strong></li>       
                        <li>İş Tanımı:<strong> {vwTicket.Tanim}</strong></li>       
                    </ul>

                    <p>Saygılarımızla.</p>
                    <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                    <p> © {DateTime.Now.Year} Matlı Otomasyon.</p>";

                        var gonderimBasarili = await PostAddMailGonderimTakip("Ticket Durumu Değiştirildi", mailIcerikHtml, string.IsNullOrEmpty(ekYolu) ? null : ekYolu, true, "", mailGonderileceklerTo, mailGonderileceklerCc, true);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new BusinessException(ex.Message);
            }
        }

        public async Task<bool> TeklifDurumDegistir(int ticketTanimId, byte teklifDurumId, string? aciklama, string olusturanUserId, string duzenleyenUserFullName)
        {
            try
            {
                var now = DateTime.Now;
                List<TicketLogs> ticketLogs = new List<TicketLogs>();
                var ticketTanim = await _ticketTanimRepository.GetByIdAsync(ticketTanimId);
                ticketLogs.Add(new TicketLogs
                {
                    TicketId = ticketTanimId,
                    MatliUserId = olusturanUserId,
                    KayitZamani = now,
                    YapilanDegisiklik = $"Ticket teklif karar durumu değiştirildi. Eski Teklif Karar Durumu: {ticketTanim.TeklifKararDurumu}. Yeni Teklif Karar Durumu: {teklifDurumId}. Kullanıcı: {duzenleyenUserFullName}."
                });

                ticketTanim.TeklifKararDurumu = teklifDurumId;
                ticketTanim.TeklifKararDurumGuncelleyenUserId = olusturanUserId;
                ticketTanim.TeklifKararDurumGuncellemeZamani = now;
                if (teklifDurumId == TicketTeklifKararDurumu.Onaylandi)
                {
                    ticketTanim.TeklifSabitle = true;
                }
                await _ticketLogsRepository.AddRangeAsync(ticketLogs);
                await _ticketLogsRepository.SaveChangesAsync();
                var vwTicket = await _ticketTanimRepository.GetvwTicketTanimById(ticketTanim.Id);
                var sorumlular = await _ticketSorumluUserEslemeRepository.GetAllvwTicketSorumluUserEslemeByTicketId(ticketTanimId);
                var takipciler = await _ticketMatliTakipciEslemeRepository.GetAllvwTicketMatliTakipciEslemesByTicketId(ticketTanimId);
                var teklifKararMesaj = "onaylanmıştır";
                if (teklifDurumId == TicketTeklifKararDurumu.Reddedildi)
                {
                    teklifKararMesaj = "reddedilmiştir";
                }
                string aciklamaHtml = string.IsNullOrWhiteSpace(aciklama) ? "" : $"<li>Açıklama:<strong> {aciklama}</strong></li>";

                var tr = new CultureInfo("tr-TR");
                string teklifFiyatText = vwTicket.TeklifFiyat.HasValue ? vwTicket.TeklifFiyat.Value.ToString("N2", tr) : "-";
                string mailIcerikHtml = $@"
                    <p>Sn. İlgililer,</p>

                     <p><strong>{duzenleyenUserFullName}</strong> tarafından fiyat teklifi <strong>{teklifKararMesaj}</strong>. (ID: {vwTicket.Id})</p>

                    <ul>
                        <li>Teklif Fiyatı:<strong> {teklifFiyatText} {vwTicket.ParaBirimi}</strong></li>
                        {aciklamaHtml}     
                        <li>Lokasyon:<strong> {vwTicket.LokasyonName}</strong></li>
                        <li>Kısa Tanımı:<strong> {vwTicket.KisaTanim}</strong></li>       
                        <li>İş Tanımı:<strong> {vwTicket.Tanim}</strong></li>       
                    </ul>

                    <p>Saygılarımızla.</p>
                    <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                    <p> © {now.Year} Matlı Otomasyon.</p>";

                var toList = sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).ToList();
                var ccList = takipciler.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).ToList();
                toList.Add("okaniskol@piotomasyon.com.tr");
                var mailGonderileceklerTo = string.Join(", ", toList.Distinct());//sorumlular
                var mailGonderileceklerCc = string.Join(", ", ccList.Distinct());//takipciler
                var gonderimBasarili = await PostAddMailGonderimTakip("Ticket Fiyat Teklifi Durumu", mailIcerikHtml, null, true, "", mailGonderileceklerTo, mailGonderileceklerCc, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<TicketDurum>> GetAllTicketDurums()
        {
            return (await _ticketDurumRepository.GetAllListAsync()).OrderBy(x => x.Sira).ToList();
        }

        public async Task<List<vwTicketDurumLog>> GetAllvwTicketDurumLogs(int ticketTanimId)
        {
            var result = await _ticketTanimRepository.GetAllvwTicketDurumLogsByTicketId(ticketTanimId);
            return result;
        }

        public async Task<List<vwTicketTeklifFiyatLog>> GetAllvwTicketTeklifFiyatLogs(int ticketTanimId)
        {
            var result = await _ticketTanimRepository.GetAllvwTicketTeklifFiyatLogs(ticketTanimId);
            return result;
        }

        public async Task<List<ParaBirimi>> GetAllParaBirimis()
        {
            return await _ticketTanimRepository.GetAllParaBirimis();
        }

        public async Task<List<TicketTanimReportModel>> GetAllTicketTanims()
        {
            var ticketTanims = await _ticketTanimRepository.GetAllvwTicketTanim();
            var result = new List<TicketTanimReportModel>();
            int sira = 0;
            var allSorumlular = await _ticketSorumluUserEslemeRepository.GetAllvwTicketSorumluUserEslemes();
            var allTakipciler = await _ticketMatliTakipciEslemeRepository.GetAllvwTicketMatliTakipciEslemes();
            var allAciklamalar = await _ticketTanimRepository.GetAllvwTicketAciklamaEsleme();
            foreach (var ticket in ticketTanims.OrderByDescending(x => x.KayitZamani).ToList())
            {
                sira += 1;
                var sorumlular = allSorumlular.Where(x => x.TicketId == ticket.Id && x.Silindi != true);
                var takipciler = allTakipciler.Where(x => x.TicketId == ticket.Id && x.Silindi != true);
                var aciklama = allAciklamalar.Where(x => x.TicketId == ticket.Id);
                TicketTanimReportModel model = new TicketTanimReportModel
                {
                    Sira = sira,
                    Id = ticket.Id,
                    FormId = ticket.FormId,
                    TedarikciId = ticket.TedarikciId,
                    TedarikciName = ticket.TedarikciName,
                    LokasyonId = ticket.LokasyonId,
                    LokasyonName = ticket.LokasyonName,
                    Tanim = ticket.Tanim,
                    KisaTanim = ticket.KisaTanim,
                    OlusturanUserId = ticket.OlusturanUserId,
                    OlusturanUserFullName = ticket.OlusturanUserFullName,
                    KayitZamani = ticket.KayitZamani,
                    KayitZamaniFormatted = ticket.KayitZamani.ToShortDateString() + " " + ticket.KayitZamani.ToShortTimeString(),
                    DurumId = ticket.DurumId,
                    DurumName = ticket.DurumName,
                    DurumIdTedarikci = ticket.DurumIdTedarikci,
                    DurumNameTedarikci = ticket.DurumNameTedarikci,
                    TeklifFiyat = ticket.TeklifFiyat,
                    TeklifParaBirimiId = ticket.TeklifParaBirimiId,
                    ParaBirimi = ticket.ParaBirimi,
                    Takipciler = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName)),
                    TakipcilerUserIds = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.MatliUserId)).Select(x => x.MatliUserId)),
                    Sorumlular = string.Join(", ", sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName)),
                    SorumlularUserIds = string.Join(", ", sorumlular.Select(x => x.TedarikciUserId).Distinct().Where(id => id > 0)),
                    Aciklama = aciklama.Count() > 0 ? "..." : "",
                    TeklifSabitle = ticket.TeklifSabitle,
                    SasNo = ticket.SasNo,
                    TeklifKararDurumu = ticket.TeklifKararDurumu,
                    TeklifKararDurumGuncellemeZamani = ticket.TeklifKararDurumGuncellemeZamani,
                    TeklifKararDurumGuncelleyenUserFullName = ticket.TeklifKararDurumGuncelleyenUserFullName,
                    FaturaKesildi = ticket.FaturaKesildi
                };
                result.Add(model);
            }
            return result;
        }

        //public async Task<List<TicketTanimReportModel>> GetAllTicketTanims(string userId)
        //{
        //    List<vwTicketTanim> ticketTanims = new List<vwTicketTanim>();
        //    var sorumluMu = await GetUserSorumluMu(userId);
        //    if (sorumluMu != null)
        //    {
        //        ticketTanims = await _ticketTanimRepository.GetAllFilteredvwTicketTanimByTedarikciId(sorumluMu.TedarikciId);
        //    }
        //    else
        //    {
        //        ticketTanims = await _ticketTanimRepository.GetAllvwTicketTanim();
        //    }
        //    var result = new List<TicketTanimReportModel>();
        //    int sira = 0;
        //    var allSorumlular = await _ticketSorumluUserEslemeRepository.GetAllvwTicketSorumluUserEslemes();
        //    var allTakipciler = await _ticketMatliTakipciEslemeRepository.GetAllvwTicketMatliTakipciEslemes();
        //    var allAciklamalar = await _ticketTanimRepository.GetAllvwTicketAciklamaEsleme();

        //    foreach (var ticket in ticketTanims.OrderByDescending(x => x.KayitZamani).ToList())
        //    {
        //        sira += 1;
        //        var sorumlular = allSorumlular.Where(x => x.TicketId == ticket.Id && x.Silindi != true);
        //        var takipciler = allTakipciler.Where(x => x.TicketId == ticket.Id && x.Silindi != true);
        //        var aciklama = allAciklamalar.Where(x => x.TicketId == ticket.Id);
        //        TicketTanimReportModel model = new TicketTanimReportModel
        //        {
        //            Sira = sira,
        //            Id = ticket.Id,
        //            FormId = ticket.FormId,
        //            TedarikciId = ticket.TedarikciId,
        //            TedarikciName = ticket.TedarikciName,
        //            LokasyonId = ticket.LokasyonId,
        //            LokasyonName = ticket.LokasyonName,
        //            Tanim = ticket.Tanim,
        //            KisaTanim = ticket.KisaTanim,
        //            OlusturanUserId = ticket.OlusturanUserId,
        //            OlusturanUserFullName = ticket.OlusturanUserFullName,
        //            KayitZamani = ticket.KayitZamani,
        //            KayitZamaniFormatted = ticket.KayitZamani.ToShortDateString() + " " + ticket.KayitZamani.ToShortTimeString(),
        //            DurumId = ticket.DurumId,
        //            DurumName = ticket.DurumName,
        //            DurumIdTedarikci = ticket.DurumIdTedarikci,
        //            DurumNameTedarikci = ticket.DurumNameTedarikci,
        //            TeklifFiyat = ticket.TeklifFiyat,
        //            TeklifParaBirimiId = ticket.TeklifParaBirimiId,
        //            ParaBirimi = ticket.ParaBirimi,
        //            Takipciler = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName)),
        //            TakipcilerUserIds = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.MatliUserId)).Select(x => x.MatliUserId)),
        //            Sorumlular = string.Join(", ", sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName)),
        //            SorumlularUserIds = string.Join(", ", sorumlular.Select(x => x.TedarikciUserId).Distinct().Where(id => id > 0)),
        //            Aciklama = aciklama.Count() > 0 ? "..." : "",
        //            TeklifSabitle = ticket.TeklifSabitle,
        //            SasNo = ticket.SasNo,
        //            TeklifKararDurumu = ticket.TeklifKararDurumu,
        //            TeklifKararDurumGuncellemeZamani = ticket.TeklifKararDurumGuncellemeZamani,
        //            TeklifKararDurumGuncelleyenUserFullName = ticket.TeklifKararDurumGuncelleyenUserFullName,
        //            FaturaKesildi = ticket.FaturaKesildi,
        //        };
        //        result.Add(model);
        //    }
        //    return result;
        //}
        public async Task<TicketTanimListResultModel> GetAllTicketTanims(string userId)
        {
            var sorumluMu = await GetUserSorumluMu(userId);

            List<vwTicketTanim> ticketTanims;

            if (sorumluMu != null)
            {
                ticketTanims = await _ticketTanimRepository.GetAllFilteredvwTicketTanimByTedarikciId(sorumluMu.TedarikciId);
            }
            else
            {
                ticketTanims = await _ticketTanimRepository.GetAllvwTicketTanim();
            }

            if (ticketTanims == null || ticketTanims.Count == 0)
                return new TicketTanimListResultModel { Items = new List<TicketTanimReportModel>(), Sorumlu = sorumluMu };

            var orderedTickets = ticketTanims.OrderByDescending(x => x.KayitZamani).ToList();

            // Eşleme tabloları artık DB seviyesinde ekrandaki ticketId'lere göre filtreleniyor.
            var ticketIdList = orderedTickets.Select(x => x.Id).ToList();

            var allSorumlular = await _ticketSorumluUserEslemeRepository.GetAllvwTicketSorumluUserEslemesByTicketIds(ticketIdList);

            var allTakipciler = await _ticketMatliTakipciEslemeRepository.GetAllvwTicketMatliTakipciEslemesByTicketIds(ticketIdList);

            // Açıklama içeriğine ihtiyaç yok, açıklaması var mı bilgisi yeterli
            var aciklamasiOlanTicketIdSet = (await _ticketTanimRepository.GetTicketIdsWithAciklamaByTicketIds(ticketIdList)).ToHashSet();

            // Yalnızca ekrana getirilecek ticketların kayıtlarını bellekte tut
            var sorumlularLookup = allSorumlular.ToLookup(x => x.TicketId);

            var takipcilerLookup = allTakipciler.ToLookup(x => x.TicketId);

            var result = new List<TicketTanimReportModel>(orderedTickets.Count);

            for (var index = 0; index < orderedTickets.Count; index++)
            {
                var ticket = orderedTickets[index];

                // Lookup erişimi tüm listeyi taramaz
                var sorumlular = sorumlularLookup[ticket.Id].ToList();
                var takipciler = takipcilerLookup[ticket.Id].ToList();

                var model = new TicketTanimReportModel
                {
                    Sira = index + 1,

                    Id = ticket.Id,
                    FormId = ticket.FormId,

                    TedarikciId = ticket.TedarikciId,
                    TedarikciName = ticket.TedarikciName,

                    LokasyonId = ticket.LokasyonId,
                    LokasyonName = ticket.LokasyonName,

                    Tanim = ticket.Tanim,
                    KisaTanim = ticket.KisaTanim,

                    OlusturanUserId = ticket.OlusturanUserId,
                    OlusturanUserFullName = ticket.OlusturanUserFullName,

                    KayitZamani = ticket.KayitZamani,
                    KayitZamaniFormatted = ticket.KayitZamani.ToString("dd.MM.yyyy HH:mm"),

                    DurumId = ticket.DurumId,
                    DurumName = ticket.DurumName,

                    DurumIdTedarikci = ticket.DurumIdTedarikci,

                    DurumNameTedarikci = ticket.DurumNameTedarikci,

                    TeklifFiyat = ticket.TeklifFiyat,
                    TeklifParaBirimiId = ticket.TeklifParaBirimiId,

                    ParaBirimi = ticket.ParaBirimi,

                    Takipciler = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName).Distinct()),

                    TakipcilerUserIds = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.MatliUserId)).Select(x => x.MatliUserId).Distinct()),

                    Sorumlular = string.Join(", ", sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName).Distinct()),

                    SorumlularUserIds = string.Join(", ", sorumlular.Where(x => x.TedarikciUserId > 0).Select(x => x.TedarikciUserId).Distinct()),

                    Aciklama = aciklamasiOlanTicketIdSet.Contains(ticket.Id) ? "..." : string.Empty,

                    TeklifSabitle = ticket.TeklifSabitle,
                    SasNo = ticket.SasNo,

                    TeklifKararDurumu = ticket.TeklifKararDurumu,

                    TeklifKararDurumGuncellemeZamani = ticket.TeklifKararDurumGuncellemeZamani,

                    TeklifKararDurumGuncelleyenUserFullName = ticket.TeklifKararDurumGuncelleyenUserFullName,

                    FaturaKesildi = ticket.FaturaKesildi
                };
                result.Add(model);
            }
            return new TicketTanimListResultModel { Items = result, Sorumlu = sorumluMu };
        }

        public async Task<LoadResult> GetAllTicketTanimsDx(DataSourceLoadOptions loadOptions, DateTime? baslangicZamani, DateTime? bitisZamani, string userId)
        {
            var sorumluMu = await GetUserSorumluMu(userId);

            var baseQuery = _ticketTanimRepository.GetvwTicketTanimQueryable(sorumluMu?.TedarikciId, baslangicZamani, bitisZamani);

            var projected = baseQuery.Select(x => new TicketTanimReportModel
            {
                Id = x.Id,
                FormId = x.FormId,

                TedarikciId = x.TedarikciId,
                TedarikciName = x.TedarikciName,

                LokasyonId = x.LokasyonId,
                LokasyonName = x.LokasyonName,

                Tanim = x.Tanim,
                KisaTanim = x.KisaTanim,

                OlusturanUserId = x.OlusturanUserId,
                OlusturanUserFullName = x.OlusturanUserFullName,

                KayitZamani = x.KayitZamani,

                DurumId = x.DurumId,
                DurumName = x.DurumName,

                DurumIdTedarikci = x.DurumIdTedarikci,
                DurumNameTedarikci = x.DurumNameTedarikci,

                TeklifFiyat = x.TeklifFiyat,
                TeklifParaBirimiId = x.TeklifParaBirimiId,
                ParaBirimi = x.ParaBirimi,

                TeklifSabitle = x.TeklifSabitle,
                SasNo = x.SasNo,

                TeklifKararDurumu = x.TeklifKararDurumu,
                TeklifKararDurumGuncellemeZamani = x.TeklifKararDurumGuncellemeZamani,
                TeklifKararDurumGuncelleyenUserFullName = x.TeklifKararDurumGuncelleyenUserFullName,

                FaturaKesildi = x.FaturaKesildi,

                /*
                 * Bu alanlar ayrı eşleme tablolarından geldiği için SQL'e çevrilemez.
                 * Sabit değerlere bağlanıyor: bir filtre/sort/group bu üyelere değerse
                 * (client tarafında allowFiltering=false ile engellense de, savunma
                 * amaçlı) EF Core bağlanmamış bir üyeye karşı çeviri hatası fırlatabilir;
                 * sabit değer bunu her zaman güvenle no-op'a çevirir.
                 */
                Sira = 0,
                KayitZamaniFormatted = string.Empty,
                Sorumlular = null,
                SorumlularUserIds = null,
                Takipciler = null,
                TakipcilerUserIds = null,
                Aciklama = null,
            });

            SanitizeLoadOptionsAgainstComputedFields(loadOptions);

            /*
             * Kullanıcı grid'de bir sütuna tıklayıp sıralama seçmediği sürece
             * loadOptions.Sort boş gelir. Eski (sayfalamasız) davranışla aynı
             * olması için varsayılan sıralamayı DataSourceLoader'ın kendi sort
             * mekanizmasına (loadOptions.Sort) yazıyoruz — queryable üzerinde
             * elle OrderBy uygulamak DataSourceLoader'ın kendi ürettiği
             * OrderBy/Skip/Take zinciriyle güvenilir şekilde etkileşmiyor.
             */
            if (loadOptions.Sort == null || loadOptions.Sort.Length == 0)
            {
                loadOptions.Sort = new[]
                {
                    new SortingInfo
                    {
                        Selector = nameof(TicketTanimReportModel.KayitZamani),
                        Desc = true,
                    },
                };
            }

            var loadResult = await DataSourceLoader.LoadAsync(projected, loadOptions);

            var ids = new List<int>();
            CollectLeafIds(loadResult.data, ids);

            if (ids.Count > 0)
            {
                var allSorumlular = await _ticketSorumluUserEslemeRepository.GetAllvwTicketSorumluUserEslemesByTicketIds(ids);
                var allTakipciler = await _ticketMatliTakipciEslemeRepository.GetAllvwTicketMatliTakipciEslemesByTicketIds(ids);
                var aciklamaSet = (await _ticketTanimRepository.GetTicketIdsWithAciklamaByTicketIds(ids)).ToHashSet();

                var sorumlularLookup = allSorumlular.ToLookup(x => x.TicketId);
                var takipcilerLookup = allTakipciler.ToLookup(x => x.TicketId);

                var ordinal = loadOptions.Skip;
                EnrichLeafRows(loadResult.data, sorumlularLookup, takipcilerLookup, aciklamaSet, ref ordinal);
            }

            return loadResult;
        }

        private static readonly HashSet<string> ComputedOnlyFieldNames = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(TicketTanimReportModel.Sorumlular),
            nameof(TicketTanimReportModel.Takipciler),
            nameof(TicketTanimReportModel.Aciklama),
        };

        private static void SanitizeLoadOptionsAgainstComputedFields(DataSourceLoadOptions loadOptions)
        {
            if (loadOptions.Sort != null)
            {
                loadOptions.Sort = loadOptions.Sort
                    .Where(s => !ComputedOnlyFieldNames.Contains(s.Selector ?? string.Empty))
                    .ToArray();
            }

            if (loadOptions.Group != null)
            {
                loadOptions.Group = loadOptions.Group
                    .Where(g => !ComputedOnlyFieldNames.Contains(g.Selector ?? string.Empty))
                    .ToArray();
            }

            loadOptions.Filter = SanitizeFilterExpression(loadOptions.Filter) as System.Collections.IList;
        }

        private static object? SanitizeFilterExpression(object? filter)
        {
            if (filter is not System.Collections.IList list || filter is string)
                return filter;

            // Basit "selector, operator, value" üçlüsü mü?
            if (list.Count == 3 && list[0] is string selector && ComputedOnlyFieldNames.Contains(selector))
                return null;

            var sanitized = new List<object?>();
            foreach (var item in list)
            {
                var cleaned = SanitizeFilterExpression(item);
                if (cleaned != null)
                    sanitized.Add(cleaned);
            }

            return sanitized.Count > 0 ? sanitized : null;
        }

        private static void CollectLeafIds(object dataNode, List<int> ids)
        {
            if (dataNode is not System.Collections.IEnumerable enumerable || dataNode is string)
                return;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                if (item is TicketTanimReportModel row)
                {
                    ids.Add(row.Id);
                }
                else if (item is Group group && group.items != null)
                {
                    CollectLeafIds(group.items, ids);
                }
            }
        }

        private static void EnrichLeafRows(
            object dataNode,
            ILookup<int, vwTicketSorumluUserEsleme> sorumlularLookup,
            ILookup<int, vwTicketMatliTakipciEsleme> takipcilerLookup,
            HashSet<int> aciklamaSet,
            ref int ordinal)
        {
            if (dataNode is not System.Collections.IEnumerable enumerable || dataNode is string)
                return;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                if (item is TicketTanimReportModel row)
                {
                    ordinal++;
                    row.Sira = ordinal;
                    row.KayitZamaniFormatted = row.KayitZamani.ToString("dd.MM.yyyy HH:mm");

                    var sorumlular = sorumlularLookup[row.Id].ToList();
                    var takipciler = takipcilerLookup[row.Id].ToList();

                    row.Sorumlular = string.Join(", ", sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName).Distinct());
                    row.SorumlularUserIds = string.Join(", ", sorumlular.Where(x => x.TedarikciUserId > 0).Select(x => x.TedarikciUserId).Distinct());
                    row.Takipciler = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName).Distinct());
                    row.TakipcilerUserIds = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.MatliUserId)).Select(x => x.MatliUserId).Distinct());
                    row.Aciklama = aciklamaSet.Contains(row.Id) ? "..." : string.Empty;
                }
                else if (item is Group group && group.items != null)
                {
                    EnrichLeafRows(group.items, sorumlularLookup, takipcilerLookup, aciklamaSet, ref ordinal);
                }
            }
        }

        public async Task<List<TicketTanimReportModel>> GetAllFilteredTicketTanims(DateTime baslangicZamani, DateTime bitisZamani, string userId)
        {
            var sorumluMu = await GetUserSorumluMu(userId);
            var ticketTanims = await _ticketTanimRepository.GetAllFilteredvwTicketTanim(baslangicZamani, bitisZamani, sorumluMu == null ? null : sorumluMu.TedarikciId);
            var result = new List<TicketTanimReportModel>();
            int sira = 0;

            var orderedTickets = ticketTanims.OrderByDescending(x => x.KayitZamani).ToList();
            var ticketIdList = orderedTickets.Select(x => x.Id).ToList();

            var allSorumlular = await _ticketSorumluUserEslemeRepository.GetAllvwTicketSorumluUserEslemesByTicketIds(ticketIdList);
            var allTakipciler = await _ticketMatliTakipciEslemeRepository.GetAllvwTicketMatliTakipciEslemesByTicketIds(ticketIdList);
            var aciklamasiOlanTicketIdSet = (await _ticketTanimRepository.GetTicketIdsWithAciklamaByTicketIds(ticketIdList)).ToHashSet();

            var sorumlularLookup = allSorumlular.ToLookup(x => x.TicketId);
            var takipcilerLookup = allTakipciler.ToLookup(x => x.TicketId);

            foreach (var ticket in orderedTickets)
            {
                sira += 1;
                var sorumlular = sorumlularLookup[ticket.Id];
                var takipciler = takipcilerLookup[ticket.Id];
                TicketTanimReportModel model = new TicketTanimReportModel
                {
                    Sira = sira,
                    Id = ticket.Id,
                    FormId = ticket.FormId,
                    TedarikciId = ticket.TedarikciId,
                    TedarikciName = ticket.TedarikciName,
                    LokasyonId = ticket.LokasyonId,
                    LokasyonName = ticket.LokasyonName,
                    Tanim = ticket.Tanim,
                    KisaTanim = ticket.KisaTanim,
                    OlusturanUserId = ticket.OlusturanUserId,
                    OlusturanUserFullName = ticket.OlusturanUserFullName,
                    KayitZamani = ticket.KayitZamani,
                    KayitZamaniFormatted = ticket.KayitZamani.ToShortDateString() + " " + ticket.KayitZamani.ToShortTimeString(),
                    DurumId = ticket.DurumId,
                    DurumName = ticket.DurumName,
                    DurumIdTedarikci = ticket.DurumIdTedarikci,
                    DurumNameTedarikci = ticket.DurumNameTedarikci,
                    TeklifFiyat = ticket.TeklifFiyat,
                    TeklifParaBirimiId = ticket.TeklifParaBirimiId,
                    ParaBirimi = ticket.ParaBirimi,
                    Takipciler = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName)),
                    TakipcilerUserIds = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.MatliUserId)).Select(x => x.MatliUserId)),
                    Sorumlular = string.Join(", ", sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName)),
                    SorumlularUserIds = string.Join(", ", sorumlular.Select(x => x.TedarikciUserId).Distinct().Where(id => id > 0)),
                    Aciklama = aciklamasiOlanTicketIdSet.Contains(ticket.Id) ? "..." : "",
                    TeklifSabitle = ticket.TeklifSabitle,
                    SasNo = ticket.SasNo,
                    TeklifKararDurumu = ticket.TeklifKararDurumu,
                    TeklifKararDurumGuncellemeZamani = ticket.TeklifKararDurumGuncellemeZamani,
                    TeklifKararDurumGuncelleyenUserFullName = ticket.TeklifKararDurumGuncelleyenUserFullName,
                    FaturaKesildi = ticket.FaturaKesildi,
                };
                result.Add(model);
            }
            return result;
        }

        public async Task<TicketTanimReportModel> GetTicketById(int ticketTanimId, string userId)
        {
            var result = new TicketTanimReportModel();

            var sorumlular = await _ticketSorumluUserEslemeRepository.GetAllvwTicketSorumluUserEslemeByTicketId(ticketTanimId);
            var takipciler = await _ticketMatliTakipciEslemeRepository.GetAllvwTicketMatliTakipciEslemesByTicketId(ticketTanimId);
            var aciklama = await _ticketTanimRepository.GetAllvwTicketAciklamaEslemeByTicketId(ticketTanimId);

            var ticket = await _ticketTanimRepository.GetvwTicketTanimById(ticketTanimId);
            if (ticket != null)
            {
                TicketTanimReportModel model = new TicketTanimReportModel
                {
                    Id = ticket.Id,
                    FormId = ticket.FormId,
                    TedarikciId = ticket.TedarikciId,
                    TedarikciName = ticket.TedarikciName,
                    LokasyonId = ticket.LokasyonId,
                    LokasyonName = ticket.LokasyonName,
                    Tanim = ticket.Tanim,
                    KisaTanim = ticket.KisaTanim,
                    OlusturanUserId = ticket.OlusturanUserId,
                    OlusturanUserFullName = ticket.OlusturanUserFullName,
                    KayitZamani = ticket.KayitZamani,
                    KayitZamaniFormatted = ticket.KayitZamani.ToShortDateString() + " " + ticket.KayitZamani.ToShortTimeString(),
                    DurumId = ticket.DurumId,
                    DurumName = ticket.DurumName,
                    DurumIdTedarikci = ticket.DurumIdTedarikci,
                    DurumNameTedarikci = ticket.DurumNameTedarikci,
                    TeklifFiyat = ticket.TeklifFiyat,
                    TeklifParaBirimiId = ticket.TeklifParaBirimiId,
                    ParaBirimi = ticket.ParaBirimi,
                    Takipciler = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName)),
                    TakipcilerUserIds = string.Join(", ", takipciler.Where(x => !string.IsNullOrWhiteSpace(x.MatliUserId)).Select(x => x.MatliUserId)),
                    Sorumlular = string.Join(", ", sorumlular.Where(x => !string.IsNullOrWhiteSpace(x.UserFullName)).Select(x => x.UserFullName)),
                    SorumlularUserIds = string.Join(", ", sorumlular.Select(x => x.TedarikciUserId).Distinct().Where(id => id > 0)),
                    Aciklama = aciklama.Count() > 0 ? "..." : "",
                    TeklifSabitle = ticket.TeklifSabitle,
                    SasNo = ticket.SasNo,
                    TeklifKararDurumu = ticket.TeklifKararDurumu,
                    TeklifKararDurumGuncellemeZamani = ticket.TeklifKararDurumGuncellemeZamani,
                    TeklifKararDurumGuncelleyenUserFullName = ticket.TeklifKararDurumGuncelleyenUserFullName,
                    FaturaKesildi = ticket.FaturaKesildi,
                };
                return model;
            }
            return result;
        }

        public async Task<List<vwTedarikciUser>> GetAllvwTedarikciUser()
        {
            var result = await _ticketTanimRepository.GetAllvwTedarikciUser();
            return result.Where(x => x.Silindi != true).ToList();
        }

        public async Task<List<vwTicketAciklamaEsleme>> GetAllSorumluAciklamalarByTicketId(int ticketTanimId)
        {
            var result = await _ticketTanimRepository.GetAllSorumluAciklamalarByTicketId(ticketTanimId);
            return result;
        }

        public async Task<List<vwTicketAciklamaEsleme>> GetAllTakipciAciklamalarByTicketId(int ticketTanimId)
        {
            var result = await _ticketTanimRepository.GetAllTakipciAciklamalarByTicketId(ticketTanimId);
            return result;
        }
        public async Task<List<Tedarikci>> GetAllTedarikciForTicketTanim()
        {
            return await _tedarikciRepository.GetAllListAsync();
        }


        public async Task<vwTedarikciUser> GetUserSorumluMu(string userId)
        {
            var result = await _ticketTanimRepository.GetUserSorumluMuByLoginUserId(userId);
            return result;
        }

        public async Task<bool> GetTakipciUserKisitliYetki(string userId)
        {
            var result = await _ticketTanimRepository.GetTakipciUserKisitliYetki(userId);
            return result;
        }
        public async Task<bool> GetTakipciUserYetkisiz(string userId)
        {
            var result = await _ticketTanimRepository.GetTakipciUserYetkisiz(userId);
            return result;
        }

        public async Task<string> SaveFiles(IFormFile[]? files, int ticketTanimId)
        {
            var uploadPath = Path.Combine(ApiUrls.TicketDosyalarPath, ticketTanimId.ToString());

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var filePath = Path.Combine(uploadPath, file.FileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }
            return uploadPath;
        }

        public async Task<bool?> PostAddMailGonderimTakip(string? mailBasligi, string? mailIcerik, string? ekYolu, bool? ekYoluKlasorMu, string? hataMesaji, string? gonderilecekTo, string? gonderilecekCc, bool? isHtml)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var requestUri = new Uri($"{ApiUrls.SharedAppBaseUrl + ApiUrls.SharedAppUrls.AddMailGonderimTakip}");

                    var keyValues = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("mailBasligi", mailBasligi ?? string.Empty),
                new KeyValuePair<string, string>("mailIcerik", mailIcerik ?? string.Empty),
                new KeyValuePair<string, string>("ekYolu", ekYolu ?? string.Empty),
                new KeyValuePair<string, string>("hataMesaji", hataMesaji ?? string.Empty),
                new KeyValuePair<string, string>("gonderilecekTo", gonderilecekTo ?? string.Empty),
                new KeyValuePair<string, string>("gonderilecekCc", gonderilecekCc ?? string.Empty),
                new KeyValuePair<string, string>("isHtml", (isHtml ?? true).ToString().ToLower()),
                new KeyValuePair<string, string>("ekYoluKlasorMu", (ekYoluKlasorMu ?? false).ToString().ToLower())
   };

                    var content = new FormUrlEncodedContent(keyValues);

                    var response = await httpClient.PostAsync(requestUri, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var parsedResult = JsonSerializer.Deserialize<bool?>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return parsedResult;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task<List<TicketDosyaModel>> GetAllFilesByTicketId(int ticketTanimId)
        {
            var uploadPath = Path.Combine(ApiUrls.TicketDosyalarPath, ticketTanimId.ToString());

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var dosyaListesi = new List<TicketDosyaModel>();
            var files = Directory.GetFiles(uploadPath);

            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);

                dosyaListesi.Add(new TicketDosyaModel
                {
                    DosyaAdi = fileInfo.Name,
                    DosyaBoyut = fileInfo.Length,
                    DosyaYolu = Path.Combine(ApiUrls.TicketDosyalarPath, ticketTanimId.ToString(), fileInfo.Name)
                });
            }
            return Task.FromResult(dosyaListesi);
        }

        public async Task<(string contentType, FileStream file)> GetDocument(string fileName, string filePath)
        {
            try
            {
                //var path = Path.Combine(filePath, fileName);
                if (!File.Exists(filePath))
                {
                    throw new Exception("Dosya bulunamadı");
                }
                string contentType = GetFileContentType(filePath);

                var dokuman = File.OpenRead(filePath);

                return (contentType, dokuman);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DeleteTicketFile(string filePath)
        {
            try
            {
                //var path = Path.Combine(filePath, fileName);
                if (!File.Exists(filePath))
                {
                    throw new Exception("Dosya bulunamadı");
                }
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string GetFileContentType(string url)
        {
            string extension = Path.GetExtension(url);
            string contentType = "";

            switch (extension)
            {
                case ".pdf":
                    contentType = "application/pdf";
                    break;
                case ".docx":
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
                case ".gif":
                    contentType = "image/gif";
                    break;
                case ".jpeg":
                    contentType = "image/jpeg";
                    break;
                case ".jpg":
                    contentType = "image/jpeg";
                    break;
                case ".png":
                    contentType = "image/png";
                    break;
                case ".svg":
                    contentType = "image/svg+xml";
                    break;
                case ".ppt":
                    contentType = "application/vnd.ms-powerpoint";
                    break;
                case ".pptx":
                    contentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                    break;
                case ".txt":
                    contentType = "text/plain";
                    break;
                case ".xls":
                    contentType = "application/vnd.ms-excel";
                    break;
                case ".xlsx":
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;
                case ".exe":
                    contentType = "application/octet-stream";
                    break;
                case ".rar":
                    contentType = "application/x-rar-compressed";
                    break;
                case ".zip":
                    contentType = "application/zip";
                    break;
                // and so on
                default:
                    throw new ArgumentOutOfRangeException($"Unable to find Content Type for file name {extension}.");
            }

            return contentType;
        }
    }
}