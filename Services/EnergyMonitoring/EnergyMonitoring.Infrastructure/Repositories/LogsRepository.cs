using EnergyMonitoring.Domain.DbViewModels;
using EnergyMonitoring.Domain.Interfaces;
using EnergyMonitoring.Domain.Models;
using EnergyMonitoring.Domain.Models.ReportModels;
using EnergyMonitoring.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Text.Json;
using static SharedKernel.ApplicationConstants;

namespace EnergyMonitoring.Infrastructure.Repositories
{
    public class LogsRepository : ILogsRepository
    {
        private readonly EnerjiIzlemeDbContext _context;
        private readonly IMongoCollection<Logs> _logs;
        private readonly IMongoCollection<LogsActual> _logsActual;

        public LogsRepository(EnerjiIzlemeDbContext context, EnerjiIzlemeMongoDbContext mongoContext)
        {
            _context = context;
            _logs = mongoContext.Logs;
            _logsActual = mongoContext.LogsActual;
        }

        public async Task<Logs> GetTuketimSonVeriByCihazId(int cihazId, CancellationToken cancellationToken)
        {
            return await _logs.Find(x => x.payload.okunan_analizor_id == cihazId).SortByDescending(x => x.payload.okunma_zamani).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<(Logs? ToplamVeri, List<int> State2Cihazlar)> GetToplamVeriByCihazIds(List<int> cihazIds, CancellationToken cancellationToken)
        {
            if (cihazIds == null || cihazIds.Count == 0)
                return (null, new List<int>());

            var fromColl = _logs.CollectionNamespace.CollectionName; // aynı koleksiyondan lookup

            var pipeline = new List<BsonDocument>
    {
        // 1) İlgili cihazları daralt
        new BsonDocument("$match",
            new BsonDocument("payload.okunan_analizor_id",
                new BsonDocument("$in", new BsonArray(cihazIds)))),

        // 2) Her cihaz için en son okunma zamanını bul
        new BsonDocument("$group", new BsonDocument
        {
            { "_id", "$payload.okunan_analizor_id" },
            { "LastTs", new BsonDocument("$max", "$payload.okunma_zamani") }
        }),

        // 3) (cihazId, LastTs) ile o tek son kaydı getir (sort yok!)
        new BsonDocument("$lookup", new BsonDocument
        {
            { "from", fromColl },
            { "let", new BsonDocument { { "cid", "$_id" }, { "ts", "$LastTs" } } },
            { "pipeline", new BsonArray
                {
                    new BsonDocument("$match",
                        new BsonDocument("$expr",
                            new BsonDocument("$and", new BsonArray
                            {
                                new BsonDocument("$eq", new BsonArray { "$payload.okunan_analizor_id", "$$cid" }),
                                new BsonDocument("$eq", new BsonArray { "$payload.okunma_zamani", "$$ts" })
                            }))),
                    new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { "payload", 1 } }),
                    new BsonDocument("$limit", 1)
                }
            },
            { "as", "last" }
        }),

        // 4) Tek kayda indir
        new BsonDocument("$unwind", "$last"),

        // 5) Son kayıtlardan toplama + en güncel zamanı/state'i üret
        new BsonDocument("$facet", new BsonDocument
        {
            {
                "toplamVeri", new BsonArray
                {  new BsonDocument("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },

            // --- Toplamlar (null güvenli) ---
            { "L1_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.L1_V", 0 })) },
            { "L2_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.L2_V", 0 })) },
            { "L3_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.L3_V", 0 })) },
            { "L1_A", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.L1_A", 0 })) },
            { "L2_A", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.L2_A", 0 })) },
            { "L3_A", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.L3_A", 0 })) },
            { "Toplam_Akim_A", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.Toplam_Akim_A", 0 })) },
            { "AktifGuc_L1_W", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.AktifGuc_L1_W", 0 })) },
            { "AktifGuc_L2_W", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.AktifGuc_L2_W", 0 })) },
            { "AktifGuc_L3_W", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.AktifGuc_L3_W", 0 })) },
            { "ReaktifGuc_L1_VAr", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.ReaktifGuc_L1_VAr", 0 })) },
            { "ReaktifGuc_L2_VAr", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.ReaktifGuc_L2_VAr", 0 })) },
            { "ReaktifGuc_L3_VAr", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.ReaktifGuc_L3_VAr", 0 })) },
            { "GorunurGuc_L1_VA", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.GorunurGuc_L1_VA", 0 })) },
            { "GorunurGuc_L2_VA", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.GorunurGuc_L2_VA", 0 })) },
            { "GorunurGuc_L3_VA", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.GorunurGuc_L3_VA", 0 })) },
            { "GucFaktoru_L1", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.GucFaktoru_L1", 0 })) },
            { "GucFaktoru_L2", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.GucFaktoru_L2", 0 })) },
            { "GucFaktoru_L3", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.GucFaktoru_L3", 0 })) },
            { "Cos_L1", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.Cos_L1", 0 })) },
            { "Cos_L2", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.Cos_L2", 0 })) },
            { "Cos_L3", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.Cos_L3", 0 })) },
            { "Gerilim_L12_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.Gerilim_L12_V", 0 })) },
            { "Gerilim_L23_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.Gerilim_L23_V", 0 })) },
            { "Gerilim_L31_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.Gerilim_L31_V", 0 })) },
            { "Frekans_Hz", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.Frekans_Hz", 0 })) },
            { "ToplamAktifGuc_W", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.ToplamAktifGuc_W", 0 })) },
            { "ToplamReaktifGuc_VAr", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.ToplamReaktifGuc_VAr", 0 })) },
            { "ToplamGorunurGuc_VA", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.ToplamGorunurGuc_VA", 0 })) },
            { "Import_Aktif_Enerji_Wh", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.Import_Aktif_Enerji_Wh", 0 })) },
            { "Export_Aktif_Enerji_Wh", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$last.payload.veri.Export_Aktif_Enerji_Wh", 0 })) },

            // --- Özet zaman & state ---
            { "LastOkunmaZamani", new BsonDocument("$max", "$last.payload.okunma_zamani") },
            { "LastState", new BsonDocument("$max", "$last.payload.state") }
             })
                }
            },
            {
                "state2Cihazlar", new BsonArray
                {
                    new BsonDocument("$match",
                        new BsonDocument("last.payload.state", 2)),

                    new BsonDocument("$project",
                        new BsonDocument("_id", "$_id"))
                }
            }
        })
    };

            var result = await _logs.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync(cancellationToken);
            if (result == null) return (null, new List<int>());

            // toplamVeri kısmını çıkar
            var toplamDoc = result["toplamVeri"].AsBsonArray.FirstOrDefault()?.AsBsonDocument;
            Logs? toplam = null;

            if (toplamDoc != null)
            {
                toplam = new Logs
                {
                    payload = new payload
                    {
                        okunma_zamani = toplamDoc.GetValue("LastOkunmaZamani", BsonNull.Value).IsBsonNull
                            ? DateTime.MinValue
                            : toplamDoc["LastOkunmaZamani"].ToUniversalTime(),
                        state = toplamDoc.TryGetValue("LastState", out var s) && !s.IsBsonNull ? s.ToInt32() : 0,
                        veri = new veri
                        {
                            L1_V = toplamDoc.GetValue("L1_V", 0).ToDoubleSafe(),
                            L2_V = toplamDoc.GetValue("L2_V", 0).ToDoubleSafe(),
                            L3_V = toplamDoc.GetValue("L3_V", 0).ToDoubleSafe(),
                            L1_A = toplamDoc.GetValue("L1_A", 0).ToDoubleSafe(),
                            L2_A = toplamDoc.GetValue("L2_A", 0).ToDoubleSafe(),
                            L3_A = toplamDoc.GetValue("L3_A", 0).ToDoubleSafe(),
                            Toplam_Akim_A = toplamDoc.GetValue("Toplam_Akim_A", 0).ToDoubleSafe(),
                            AktifGuc_L1_W = toplamDoc.GetValue("AktifGuc_L1_W", 0).ToDoubleSafe(),
                            AktifGuc_L2_W = toplamDoc.GetValue("AktifGuc_L2_W", 0).ToDoubleSafe(),
                            AktifGuc_L3_W = toplamDoc.GetValue("AktifGuc_L3_W", 0).ToDoubleSafe(),
                            ReaktifGuc_L1_VAr = toplamDoc.GetValue("ReaktifGuc_L1_VAr", 0).ToDoubleSafe(),
                            ReaktifGuc_L2_VAr = toplamDoc.GetValue("ReaktifGuc_L2_VAr", 0).ToDoubleSafe(),
                            ReaktifGuc_L3_VAr = toplamDoc.GetValue("ReaktifGuc_L3_VAr", 0).ToDoubleSafe(),
                            GorunurGuc_L1_VA = toplamDoc.GetValue("GorunurGuc_L1_VA", 0).ToDoubleSafe(),
                            GorunurGuc_L2_VA = toplamDoc.GetValue("GorunurGuc_L2_VA", 0).ToDoubleSafe(),
                            GorunurGuc_L3_VA = toplamDoc.GetValue("GorunurGuc_L3_VA", 0).ToDoubleSafe(),
                            GucFaktoru_L1 = toplamDoc.GetValue("GucFaktoru_L1", 0).ToDoubleSafe(),
                            GucFaktoru_L2 = toplamDoc.GetValue("GucFaktoru_L2", 0).ToDoubleSafe(),
                            GucFaktoru_L3 = toplamDoc.GetValue("GucFaktoru_L3", 0).ToDoubleSafe(),
                            Cos_L1 = toplamDoc.GetValue("Cos_L1", 0).ToDoubleSafe(),
                            Cos_L2 = toplamDoc.GetValue("Cos_L2", 0).ToDoubleSafe(),
                            Cos_L3 = toplamDoc.GetValue("Cos_L3", 0).ToDoubleSafe(),
                            Gerilim_L12_V = toplamDoc.GetValue("Gerilim_L12_V", 0).ToDoubleSafe(),
                            Gerilim_L23_V = toplamDoc.GetValue("Gerilim_L23_V", 0).ToDoubleSafe(),
                            Gerilim_L31_V = toplamDoc.GetValue("Gerilim_L31_V", 0).ToDoubleSafe(),
                            Frekans_Hz = toplamDoc.GetValue("Frekans_Hz", 0).ToDoubleSafe(),
                            ToplamAktifGuc_W = toplamDoc.GetValue("ToplamAktifGuc_W", 0).ToDoubleSafe(),
                            ToplamReaktifGuc_VAr = toplamDoc.GetValue("ToplamReaktifGuc_VAr", 0).ToDoubleSafe(),
                            ToplamGorunurGuc_VA = toplamDoc.GetValue("ToplamGorunurGuc_VA", 0).ToDoubleSafe(),
                            Import_Aktif_Enerji_Wh = toplamDoc.GetValue("Import_Aktif_Enerji_Wh", 0).ToDecimalSafe(),
                            Export_Aktif_Enerji_Wh = toplamDoc.GetValue("Export_Aktif_Enerji_Wh", 0).ToDecimalSafe(),
                        }
                    }
                };
            }

            // state=2 cihaz Id'leri
            var state2List = result["state2Cihazlar"]
                .AsBsonArray
                .Select(x => x["_id"].ToInt32())
                .ToList();

            return (toplam, state2List);
        }

        public async Task<(Logs? ToplamVeri, List<int> State2Cihazlar)> GetToplamVeriByCihazIdsTest(List<int> cihazIds, CancellationToken cancellationToken)
        {
            if (cihazIds == null || cihazIds.Count == 0)
                return (null, new List<int>());

            var logsActualBson = _logsActual.Database.GetCollection<BsonDocument>(
                _logsActual.CollectionNamespace.CollectionName);

            var pipeline = new List<BsonDocument>
    {
        // 1) Filtrele
        new BsonDocument("$match",
            new BsonDocument("okunan_analizor_id",
                new BsonDocument("$in", new BsonArray(cihazIds)))),

        // 2) Direkt facet - her cihaz için zaten tek kayıt var
        new BsonDocument("$facet", new BsonDocument
        {
            {
                "toplamVeri", new BsonArray
                {
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", BsonNull.Value },

                        { "L1_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.L1_V", 0 })) },
                        { "L2_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.L2_V", 0 })) },
                        { "L3_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.L3_V", 0 })) },
                        { "L1_A", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.L1_A", 0 })) },
                        { "L2_A", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.L2_A", 0 })) },
                        { "L3_A", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.L3_A", 0 })) },
                        { "Toplam_Akim_A", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.Toplam_Akim_A", 0 })) },
                        { "AktifGuc_L1_W", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.AktifGuc_L1_W", 0 })) },
                        { "AktifGuc_L2_W", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.AktifGuc_L2_W", 0 })) },
                        { "AktifGuc_L3_W", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.AktifGuc_L3_W", 0 })) },
                        { "ReaktifGuc_L1_VAr", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.ReaktifGuc_L1_VAr", 0 })) },
                        { "ReaktifGuc_L2_VAr", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.ReaktifGuc_L2_VAr", 0 })) },
                        { "ReaktifGuc_L3_VAr", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.ReaktifGuc_L3_VAr", 0 })) },
                        { "GorunurGuc_L1_VA", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.GorunurGuc_L1_VA", 0 })) },
                        { "GorunurGuc_L2_VA", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.GorunurGuc_L2_VA", 0 })) },
                        { "GorunurGuc_L3_VA", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.GorunurGuc_L3_VA", 0 })) },
                        { "GucFaktoru_L1", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.GucFaktoru_L1", 0 })) },
                        { "GucFaktoru_L2", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.GucFaktoru_L2", 0 })) },
                        { "GucFaktoru_L3", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.GucFaktoru_L3", 0 })) },
                        { "Cos_L1", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.Cos_L1", 0 })) },
                        { "Cos_L2", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.Cos_L2", 0 })) },
                        { "Cos_L3", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.Cos_L3", 0 })) },
                        { "Gerilim_L12_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.Gerilim_L12_V", 0 })) },
                        { "Gerilim_L23_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.Gerilim_L23_V", 0 })) },
                        { "Gerilim_L31_V", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.Gerilim_L31_V", 0 })) },
                        { "Frekans_Hz", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.Frekans_Hz", 0 })) },
                        { "ToplamAktifGuc_W", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.ToplamAktifGuc_W", 0 })) },
                        { "ToplamReaktifGuc_VAr", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.ToplamReaktifGuc_VAr", 0 })) },
                        { "ToplamGorunurGuc_VA", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.ToplamGorunurGuc_VA", 0 })) },
                        { "Import_Aktif_Enerji_Wh", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.Import_Aktif_Enerji_Wh", 0 })) },
                        { "Export_Aktif_Enerji_Wh", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$veri.Export_Aktif_Enerji_Wh", 0 })) },

                        { "LastOkunmaZamani", new BsonDocument("$max", "$okunma_zamani") },
                        { "LastState", new BsonDocument("$max", "$state") }
                    })
                }
            },
            {
                "state2Cihazlar", new BsonArray
                {
                    new BsonDocument("$match",
                        new BsonDocument("state", 2)),

                    new BsonDocument("$project",
                        new BsonDocument("_id", "$okunan_analizor_id"))
                }
            }
        })
    };

            var result = await logsActualBson.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync(cancellationToken);
            if (result == null) return (null, new List<int>());

            var toplamDoc = result["toplamVeri"].AsBsonArray.FirstOrDefault()?.AsBsonDocument;
            Logs? toplam = null;

            if (toplamDoc != null)
            {
                toplam = new Logs
                {
                    payload = new payload
                    {
                        okunma_zamani = toplamDoc.GetValue("LastOkunmaZamani", BsonNull.Value).IsBsonNull
                            ? DateTime.MinValue
                            : toplamDoc["LastOkunmaZamani"].ToUniversalTime(),
                        state = toplamDoc.TryGetValue("LastState", out var s) && !s.IsBsonNull ? s.ToInt32() : 0,
                        veri = new veri
                        {
                            L1_V = toplamDoc.GetValue("L1_V", 0).ToDoubleSafe(),
                            L2_V = toplamDoc.GetValue("L2_V", 0).ToDoubleSafe(),
                            L3_V = toplamDoc.GetValue("L3_V", 0).ToDoubleSafe(),
                            L1_A = toplamDoc.GetValue("L1_A", 0).ToDoubleSafe(),
                            L2_A = toplamDoc.GetValue("L2_A", 0).ToDoubleSafe(),
                            L3_A = toplamDoc.GetValue("L3_A", 0).ToDoubleSafe(),
                            Toplam_Akim_A = toplamDoc.GetValue("Toplam_Akim_A", 0).ToDoubleSafe(),
                            AktifGuc_L1_W = toplamDoc.GetValue("AktifGuc_L1_W", 0).ToDoubleSafe(),
                            AktifGuc_L2_W = toplamDoc.GetValue("AktifGuc_L2_W", 0).ToDoubleSafe(),
                            AktifGuc_L3_W = toplamDoc.GetValue("AktifGuc_L3_W", 0).ToDoubleSafe(),
                            ReaktifGuc_L1_VAr = toplamDoc.GetValue("ReaktifGuc_L1_VAr", 0).ToDoubleSafe(),
                            ReaktifGuc_L2_VAr = toplamDoc.GetValue("ReaktifGuc_L2_VAr", 0).ToDoubleSafe(),
                            ReaktifGuc_L3_VAr = toplamDoc.GetValue("ReaktifGuc_L3_VAr", 0).ToDoubleSafe(),
                            GorunurGuc_L1_VA = toplamDoc.GetValue("GorunurGuc_L1_VA", 0).ToDoubleSafe(),
                            GorunurGuc_L2_VA = toplamDoc.GetValue("GorunurGuc_L2_VA", 0).ToDoubleSafe(),
                            GorunurGuc_L3_VA = toplamDoc.GetValue("GorunurGuc_L3_VA", 0).ToDoubleSafe(),
                            GucFaktoru_L1 = toplamDoc.GetValue("GucFaktoru_L1", 0).ToDoubleSafe(),
                            GucFaktoru_L2 = toplamDoc.GetValue("GucFaktoru_L2", 0).ToDoubleSafe(),
                            GucFaktoru_L3 = toplamDoc.GetValue("GucFaktoru_L3", 0).ToDoubleSafe(),
                            Cos_L1 = toplamDoc.GetValue("Cos_L1", 0).ToDoubleSafe(),
                            Cos_L2 = toplamDoc.GetValue("Cos_L2", 0).ToDoubleSafe(),
                            Cos_L3 = toplamDoc.GetValue("Cos_L3", 0).ToDoubleSafe(),
                            Gerilim_L12_V = toplamDoc.GetValue("Gerilim_L12_V", 0).ToDoubleSafe(),
                            Gerilim_L23_V = toplamDoc.GetValue("Gerilim_L23_V", 0).ToDoubleSafe(),
                            Gerilim_L31_V = toplamDoc.GetValue("Gerilim_L31_V", 0).ToDoubleSafe(),
                            Frekans_Hz = toplamDoc.GetValue("Frekans_Hz", 0).ToDoubleSafe(),
                            ToplamAktifGuc_W = toplamDoc.GetValue("ToplamAktifGuc_W", 0).ToDoubleSafe(),
                            ToplamReaktifGuc_VAr = toplamDoc.GetValue("ToplamReaktifGuc_VAr", 0).ToDoubleSafe(),
                            ToplamGorunurGuc_VA = toplamDoc.GetValue("ToplamGorunurGuc_VA", 0).ToDoubleSafe(),
                            Import_Aktif_Enerji_Wh = toplamDoc.GetValue("Import_Aktif_Enerji_Wh", 0).ToDecimalSafe(),
                            Export_Aktif_Enerji_Wh = toplamDoc.GetValue("Export_Aktif_Enerji_Wh", 0).ToDecimalSafe(),
                        }
                    }
                };
            }

            var state2List = result["state2Cihazlar"]
                .AsBsonArray
                .Select(x => x["_id"].ToInt32())
                .ToList();

            return (toplam, state2List);
        }

        public async Task<List<BsonDocument>> GetTuketimVerileriByCihazId(int cihazId, CancellationToken cancellationToken)
        {
            try
            {
                //1. Ay ve cihaz bazında ilk ve son kayıtları çek (aggregate ile)
                var pipeline = new BsonDocument[]
                 {
                    new BsonDocument("$match", new BsonDocument("payload.okunan_analizor_id", cihazId)),

                    new BsonDocument("$project", new BsonDocument
                    {
                        { "cihazId", "$payload.okunan_analizor_id" },
                        { "export", "$payload.veri.Export_Aktif_Enerji_Wh" },
                        { "zaman", "$payload.okunma_zamani" },
                        { "yil", new BsonDocument("$year", "$payload.okunma_zamani") },
                        { "ay", new BsonDocument("$month", "$payload.okunma_zamani") },
                    }),

                    new BsonDocument("$sort", new BsonDocument
                    {
                        { "zaman", 1 }
                    }),

                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", new BsonDocument
                            {
                                { "cihazId", "$cihazId" },
                                { "yil", "$yil" },
                                { "ay", "$ay" }
                            }
                        },
                        { "ilk", new BsonDocument("$first", "$export") },
                        { "son", new BsonDocument("$last", "$export") }
                    }),

                    new BsonDocument("$project", new BsonDocument
                    {
                        { "cihazId", "$_id.cihazId" },
                        { "yil", "$_id.yil" },
                        { "ay", "$_id.ay" },
                        { "tuketim", new BsonDocument("$subtract", new BsonArray { "$son", "$ilk" }) }
                    })
                 };

                var result = await _logs.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BsonDocument> GetTuketimEnerjiVerileriByCihazId(int cihazId, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            try
            {
                //var baslangicZamani2 = DateTime.SpecifyKind(baslangicZamani, DateTimeKind.Local).ToUniversalTime();
                //var bitisZamani2 = DateTime.SpecifyKind(bitisZamani, DateTimeKind.Local).ToUniversalTime();
                // İlk kayıt
                var ilkVeri = await _logs.Find(x => x.payload.okunan_analizor_id == cihazId
                    && x.payload.okunma_zamani >= baslangicZamani.AddHours(3)
                    && x.payload.okunma_zamani <= bitisZamani.AddHours(3))
                    .SortBy(x => x.payload.okunma_zamani)
                    .FirstOrDefaultAsync(cancellationToken);

                // Son kayıt
                var sonVeri = await _logs.Find(x => x.payload.okunan_analizor_id == cihazId
                    && x.payload.okunma_zamani >= baslangicZamani.AddHours(3)
                    && x.payload.okunma_zamani <= bitisZamani.AddHours(3))
                    .SortByDescending(x => x.payload.okunma_zamani)
                    .FirstOrDefaultAsync(cancellationToken);

                if (ilkVeri == null || sonVeri == null)
                {
                    // Kayıt yoksa boş döndür
                    return new BsonDocument();
                }

                // Değerleri al
                var ilkImport = (decimal)ilkVeri.payload.veri.Import_Aktif_Enerji_Wh.GetValueOrDefault(0);
                var sonImport = (decimal)sonVeri.payload.veri.Import_Aktif_Enerji_Wh.GetValueOrDefault(0);

                var ilkExport = (decimal)ilkVeri.payload.veri.Export_Aktif_Enerji_Wh.GetValueOrDefault(0);
                var sonExport = (decimal)sonVeri.payload.veri.Export_Aktif_Enerji_Wh.GetValueOrDefault(0);

                var ilkZaman = ilkVeri.payload.okunma_zamani;
                var sonZaman = sonVeri.payload.okunma_zamani;

                // Farklar
                var importFark = sonImport - ilkImport;
                var exportFark = sonExport - ilkExport;

                // Zaman farkı dakika cinsinden
                var dakikaFarki = (sonZaman - ilkZaman).TotalMinutes;

                // Sonucu döndür
                var result = new BsonDocument
        {
            { "cihazId", cihazId },
            { "import_fark", importFark },
            { "export_fark", exportFark },
            { "ilk_zaman", ilkZaman },
            { "son_zaman", sonZaman },
            { "dakika_farki", dakikaFarki }
        };

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<BsonDocument>> GetTuketimEnerjiVerileriByCokluCihazId(List<int> cihazIdList, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            var sonucListesi = new List<BsonDocument>();

            foreach (var cihazId in cihazIdList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var ilkVeri = await _logs.Find(x => x.payload.okunan_analizor_id == cihazId && x.payload.okunma_zamani >= baslangicZamani.AddHours(3) && x.payload.okunma_zamani <= bitisZamani.AddHours(3) && x.payload.state == EnerjiIzlemeDurum.Normal).SortBy(x => x.payload.okunma_zamani).FirstOrDefaultAsync(cancellationToken);

                    var sonVeri = await _logs.Find(x => x.payload.okunan_analizor_id == cihazId && x.payload.okunma_zamani >= baslangicZamani.AddHours(3) && x.payload.okunma_zamani <= bitisZamani.AddHours(3) && x.payload.state == EnerjiIzlemeDurum.Normal).SortByDescending(x => x.payload.okunma_zamani).FirstOrDefaultAsync(cancellationToken);

                    if (ilkVeri == null || sonVeri == null)
                    {
                        // Kayıt yoksa cihazId ile birlikte boş veri döndür
                        sonucListesi.Add(new BsonDocument
                                         {
                                             { "cihazId", cihazId },
                                             { "veri_bulunamadi", true }
                                         });
                        continue;
                    }

                    var ilkImport = (decimal)ilkVeri.payload.veri.Import_Aktif_Enerji_Wh.GetValueOrDefault(0);
                    var sonImport = (decimal)sonVeri.payload.veri.Import_Aktif_Enerji_Wh.GetValueOrDefault(0);

                    var ilkExport = (decimal)ilkVeri.payload.veri.Export_Aktif_Enerji_Wh.GetValueOrDefault(0);
                    var sonExport = (decimal)sonVeri.payload.veri.Export_Aktif_Enerji_Wh.GetValueOrDefault(0);

                    var ilkZaman = ilkVeri.payload.okunma_zamani;
                    var sonZaman = sonVeri.payload.okunma_zamani;

                    var importFark = sonImport - ilkImport;
                    var exportFark = sonExport - ilkExport;

                    var dakikaFarki = (sonZaman - ilkZaman).TotalMinutes;

                    var result = new BsonDocument
                                 {
                                     { "cihazId", cihazId },
                                     { "import_fark", importFark },
                                     { "export_fark", exportFark },
                                     { "ilk_zaman", ilkZaman },
                                     { "son_zaman", sonZaman },
                                     { "dakika_farki", dakikaFarki }
                                 };

                    sonucListesi.Add(result);
                }
                catch (Exception ex)
                {
                    // Hata durumunda cihazId ile birlikte hata bilgisi eklenebilir
                    sonucListesi.Add(new BsonDocument
                                     {
                                         { "cihazId", cihazId },
                                         { "hata", ex.Message }
                                     });
                }
            }
            return sonucListesi;
        }

        public async Task<List<BsonDocument>> GetSaatlikTuketimFromMongoAsync(int cihazId, DateTime baslangic, DateTime bitis, CancellationToken cancellationToken)
        {
            var sonuc = new List<BsonDocument>();

            // 1️⃣ Zaman noktalarını oluştur
            // (tarihleri UTC'ye dönüştür, çünkü Mongo UTC tutuyor)
            DateTime baslangicUtc = DateTime.SpecifyKind(baslangic, DateTimeKind.Local).ToUniversalTime();
            DateTime bitisUtc = DateTime.SpecifyKind(bitis, DateTimeKind.Local).ToUniversalTime();

            var zamanNoktalar = new List<DateTime>();
            var current = new DateTime(baslangicUtc.Year, baslangicUtc.Month, baslangicUtc.Day, baslangicUtc.Hour, 0, 0, DateTimeKind.Utc);

            if (current < baslangicUtc)
                current = current.AddHours(1);

            zamanNoktalar.Add(baslangicUtc);
            while (current < bitisUtc)
            {
                zamanNoktalar.Add(current);
                current = current.AddHours(1);
            }
            zamanNoktalar.Add(bitisUtc);

            // 2️⃣ Her kontrol noktası için MongoDB'den en yakın kaydı bul
            var noktalarVeri = new List<(DateTime Zaman, decimal ImportWh, decimal ExportWh, ObjectId)>();

            foreach (var z in zamanNoktalar)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Artık AddHours(3) GEREK YOK, çünkü z zaten UTC
                var veri = await _logs.Find(x =>
                        x.payload.state == EnerjiIzlemeDurum.Normal &&
                        x.payload.okunan_analizor_id == cihazId &&
                        x.payload.okunma_zamani <= z.AddHours(3).ToUniversalTime())
                    .SortByDescending(x => x.payload.okunma_zamani)
                    .FirstOrDefaultAsync(cancellationToken);

                if (veri != null)
                {
                    var import = (decimal)veri.payload.veri.Import_Aktif_Enerji_Wh.GetValueOrDefault(0);
                    var export = (decimal)veri.payload.veri.Export_Aktif_Enerji_Wh.GetValueOrDefault(0);

                    noktalarVeri.Add((z, import, export, veri._id));
                }
            }

            // 3️⃣ Fark hesapla
            for (int i = 0; i < noktalarVeri.Count - 1; i++)
            {
                var onceki = noktalarVeri[i];
                var sonraki = noktalarVeri[i + 1];

                var importFark = sonraki.ImportWh - onceki.ImportWh;
                var exportFark = sonraki.ExportWh - onceki.ExportWh;
                var netTuketim = importFark - exportFark;
                var dakika = (sonraki.Zaman - onceki.Zaman).TotalMinutes;

                sonuc.Add(new BsonDocument
        {
            { "cihazId", cihazId },
            // Local zamana geri dönüştürerek raporda Türkiye saatiyle göster
            { "baslangic", onceki.Zaman.ToLocalTime() },
            { "bitis", sonraki.Zaman.ToLocalTime() },
            { "dakika", dakika },
            { "import_fark", importFark },
            { "export_fark", exportFark },
            { "net_tuketim_wh", netTuketim }
        });
            }

            return sonuc;
        }

        public async Task<List<BsonDocument>> GetSaatlikTuketimFromMongoAsync(int cihazId, DateTime baslangic, DateTime bitis, TarifeTuru tarifeTuru, CancellationToken cancellationToken)
        {
            var sonuc = new List<BsonDocument>();

            // 1️⃣ Zaman noktalarını oluştur
            // (tarihleri UTC'ye dönüştür, çünkü Mongo UTC tutuyor)
            DateTime baslangicUtc = DateTime.SpecifyKind(baslangic, DateTimeKind.Local).ToUniversalTime();
            DateTime bitisUtc = DateTime.SpecifyKind(bitis, DateTimeKind.Local).ToUniversalTime();

            //var zamanNoktalar = new List<DateTime>();
            //var current = new DateTime(baslangicUtc.Year, baslangicUtc.Month, baslangicUtc.Day, baslangicUtc.Hour, 0, 0, DateTimeKind.Utc);

            //if (current < baslangicUtc)
            //    current = current.AddHours(1);

            //zamanNoktalar.Add(baslangicUtc);
            //while (current < bitisUtc)
            //{
            //    zamanNoktalar.Add(current);
            //    current = current.AddHours(1);
            //}
            //zamanNoktalar.Add(bitisUtc);

            //// 2️⃣ Her kontrol noktası için MongoDB'den en yakın kaydı bul
            //var noktalarVeri = new List<(DateTime Zaman, decimal ImportWh, decimal ExportWh, ObjectId)>();
            var zamanNoktalar = ZamanNoktalariniOlustur(baslangicUtc, bitisUtc, tarifeTuru);
            var noktalarVeri = new List<(DateTime Zaman, decimal ImportWh, decimal ExportWh, ObjectId Id)>();

            foreach (var z in zamanNoktalar)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Artık AddHours(3) GEREK YOK, çünkü z zaten UTC
                DateTime sinirZamani = z.AddHours(3).ToUniversalTime();
                var veri = await _logs.Find(x =>
                        x.payload.state == EnerjiIzlemeDurum.Normal &&
                        x.payload.okunan_analizor_id == cihazId &&
                        x.payload.okunma_zamani <= sinirZamani)
                    .SortByDescending(x => x.payload.okunma_zamani)
                    .FirstOrDefaultAsync(cancellationToken);

                if (veri != null)
                {
                    var import = (decimal)veri.payload.veri.Import_Aktif_Enerji_Wh.GetValueOrDefault(0);
                    var export = (decimal)veri.payload.veri.Export_Aktif_Enerji_Wh.GetValueOrDefault(0);

                    noktalarVeri.Add((z, import, export, veri._id));
                }
            }

            // 3️⃣ Fark hesapla
            for (int i = 0; i < noktalarVeri.Count - 1; i++)
            {
                var onceki = noktalarVeri[i];
                var sonraki = noktalarVeri[i + 1];

                var importFark = sonraki.ImportWh - onceki.ImportWh;
                var exportFark = sonraki.ExportWh - onceki.ExportWh;
                var netTuketim = importFark - exportFark;
                var dakika = (sonraki.Zaman - onceki.Zaman).TotalMinutes;

                sonuc.Add(new BsonDocument
        {
            { "cihazId", cihazId },
            // Local zamana geri dönüştürerek raporda Türkiye saatiyle göster
            { "baslangic", onceki.Zaman.ToLocalTime() },
            { "bitis", sonraki.Zaman.ToLocalTime() },
            { "dakika", dakika },
            { "import_fark", importFark },
            { "export_fark", exportFark },
            { "net_tuketim_wh", netTuketim }
        });
            }

            return sonuc;
        }

        // =============================================================================
        // ZamanNoktalariniOlustur
        //
        //  Üç Zamanlı dilimler (TR saati):
        //    Gece   22:00 – 06:00   (gece yarısını geçer)
        //    Gündüz 06:00 – 17:00
        //    Puant  17:00 – 22:00
        //
        //  Her gün için üretilen 3 sınır noktası (UTC = TR - 3):
        //    06:00 TR = gun.AddHours( 3)   → gece bitti, gündüz başladı
        //    17:00 TR = gun.AddHours(14)   → gündüz bitti, puant başladı
        //    22:00 TR = gun.AddHours(19)   → puant bitti, gece başladı
        //
        //  00:00 TR (gün başı = gun) zaten döngü başında ekleniyor,
        //  bir sonraki günün gün başı da otomatik geldiği için
        //  22:00-06:00 arası gece segmenti eksiksiz kapanıyor.
        //
        //  Örnek — 1 Ocak 00:00 → 2 Ocak 00:00 arası noktalar (UTC):
        //    1 Oca 00:00 UTC  (= 1 Oca 03:00 TR → gece devam)   ← baslangicUtc
        //    1 Oca 03:00 UTC  (= 1 Oca 06:00 TR → gündüz başı)
        //    1 Oca 14:00 UTC  (= 1 Oca 17:00 TR → puant  başı)
        //    1 Oca 19:00 UTC  (= 1 Oca 22:00 TR → gece   başı)
        //    2 Oca 00:00 UTC  (= 2 Oca 03:00 TR → gündüz başı ertesi gün) ← bitisUtc
        //
        //  Üretilen segmentler:
        //    00:00–03:00 UTC = 03:00–06:00 TR → Gece      ✓
        //    03:00–14:00 UTC = 06:00–17:00 TR → Gündüz    ✓
        //    14:00–19:00 UTC = 17:00–22:00 TR → Puant     ✓
        //    19:00–24:00 UTC = 22:00–03:00 TR → Gece      ✓
        // =============================================================================

        private static List<DateTime> ZamanNoktalariniOlustur(DateTime baslangicUtc, DateTime bitisUtc, TarifeTuru tarifeTuru)
        {
            var zamanNoktalar = new List<DateTime>();

            switch (tarifeTuru)
            {
                // ── GENEL: her tam saat → ayrı PTF değeri ────────────────────────────
                case TarifeTuru.Genel:
                    {
                        var current = new DateTime(baslangicUtc.Year, baslangicUtc.Month, baslangicUtc.Day, baslangicUtc.Hour, 0, 0, DateTimeKind.Utc);

                        if (current < baslangicUtc)
                            current = current.AddHours(1);

                        zamanNoktalar.Add(baslangicUtc);
                        while (current < bitisUtc)
                        {
                            zamanNoktalar.Add(current);
                            current = current.AddHours(1);
                        }
                        zamanNoktalar.Add(bitisUtc);
                        break;
                    }

                // ── ÜÇ ZAMANLI: günlük 3 sınır noktası ───────────────────────────────
                // Gece(22-06) / Gündüz(06-17) / Puant(17-22)
                case TarifeTuru.UcZamanli:
                    {
                        zamanNoktalar.Add(baslangicUtc);

                        // UTC gün başından başla
                        var gun = new DateTime(
                            baslangicUtc.Year, baslangicUtc.Month, baslangicUtc.Day,
                            0, 0, 0, DateTimeKind.Utc);

                        while (gun < bitisUtc)
                        {
                            // 06:00 TR = gun + 3h UTC  → Gece bitti,   Gündüz başladı
                            // 17:00 TR = gun + 14h UTC → Gündüz bitti, Puant  başladı
                            // 22:00 TR = gun + 19h UTC → Puant  bitti, Gece   başladı
                            // (Gün sonu 00:00 TR = bir sonraki gun, döngü başında gelir)
                            var sinirlar = new[]
                            {
                    gun.AddHours(3),    // 06:00 TR
                    gun.AddHours(14),   // 17:00 TR
                    gun.AddHours(19),   // 22:00 TR
                };

                            foreach (var s in sinirlar)
                            {
                                if (s > baslangicUtc && s < bitisUtc)
                                    zamanNoktalar.Add(s);
                            }

                            gun = gun.AddDays(1);  // ertesi günün 00:00 UTC → 03:00 TR (bir sonraki gündüz başı)
                        }

                        zamanNoktalar.Add(bitisUtc);
                        break;
                    }

                // ── TEK ZAMANLI: 2 nokta yeterli ────────────────────────────────────
                case TarifeTuru.TekZamanli:
                    {
                        zamanNoktalar.Add(baslangicUtc);
                        zamanNoktalar.Add(bitisUtc);
                        break;
                    }
            }

            return zamanNoktalar
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public async Task<List<EnerjiRaporGrafikModel>> GetTuketimVerileriForEnerjiGrafikRapor(vwCihaz cihaz, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval, CancellationToken cancellationToken)
        {
            var intervalMs = 1000 * 60 * dakikaInterval;

            var matchStage = new BsonDocument("$match", new BsonDocument
    {
        { "payload.okunan_analizor_id", cihaz.Id },
        { "payload.okunma_zamani", new BsonDocument {
            { "$gte", baslangicZamani.AddHours(3) },
            { "$lte", bitisZamani.AddHours(3) }
        }},
        { "payload.state", 1 } // gerekliyse
    });

            // interval anahtarı + gerekli alanlar
            var projectStage = new BsonDocument("$project", new BsonDocument
    {
        { "minuteGroup", new BsonDocument("$subtract", new BsonArray {
            new BsonDocument("$toLong", "$payload.okunma_zamani"),
            new BsonDocument("$mod", new BsonArray {
                new BsonDocument("$toLong", "$payload.okunma_zamani"),
                intervalMs
            })
        }) },

        { "okunmaZamani", "$payload.okunma_zamani" },
        { "cihazId", "$payload.okunan_analizor_id" },
        { "ImportWh", "$payload.veri.Import_Aktif_Enerji_Wh" },
        { "ExportWh", "$payload.veri.Export_Aktif_Enerji_Wh" },

        // Gerilimler
        { "L1_V", "$payload.veri.L1_V" },
        { "L2_V", "$payload.veri.L2_V" },
        { "L3_V", "$payload.veri.L3_V" },
        { "Gerilim_L12_V", "$payload.veri.Gerilim_L12_V" },
        { "Gerilim_L23_V", "$payload.veri.Gerilim_L23_V" },
        { "Gerilim_L31_V", "$payload.veri.Gerilim_L31_V" },

        // Akımlar
        { "L1_A", "$payload.veri.L1_A" },
        { "L2_A", "$payload.veri.L2_A" },
        { "L3_A", "$payload.veri.L3_A" },
        { "Toplam_Akim_A", "$payload.veri.Toplam_Akim_A" },

        // Aktif güçler
        { "AktifGuc_L1_W", "$payload.veri.AktifGuc_L1_W" },
        { "AktifGuc_L2_W", "$payload.veri.AktifGuc_L2_W" },
        { "AktifGuc_L3_W", "$payload.veri.AktifGuc_L3_W" },
        { "ToplamAktifGuc_W", "$payload.veri.ToplamAktifGuc_W" },

        // Reaktif güçler
        { "ReaktifGuc_L1_VAr", "$payload.veri.ReaktifGuc_L1_VAr" },
        { "ReaktifGuc_L2_VAr", "$payload.veri.ReaktifGuc_L2_VAr" },
        { "ReaktifGuc_L3_VAr", "$payload.veri.ReaktifGuc_L3_VAr" },
        { "ToplamReaktifGuc_VAr", "$payload.veri.ToplamReaktifGuc_VAr" },
        
        // Güç faktörleri
        { "GucFaktoru_L1", "$payload.veri.GucFaktoru_L1" },
        { "GucFaktoru_L2", "$payload.veri.GucFaktoru_L2" },
        { "GucFaktoru_L3", "$payload.veri.GucFaktoru_L3" },
        
        // Cos değerleri
        { "Cos_L1", "$payload.veri.Cos_L1" },
        { "Cos_L2", "$payload.veri.Cos_L2" },
        { "Cos_L3", "$payload.veri.Cos_L3" },

        // Frekans
        { "Frekans_Hz", "$payload.veri.Frekans_Hz" }
    });

            // ilk/son kaydı almak için global zaman sırası
            var sortByTime = new BsonDocument("$sort", new BsonDocument("okunmaZamani", 1));

            // interval içinde ilk/son import ve zaman
            var groupStage = new BsonDocument("$group", new BsonDocument
    {
        { "_id", "$minuteGroup" },

        { "CihazId", new BsonDocument("$first", "$cihazId") },

        { "FirstTime", new BsonDocument("$first", "$okunmaZamani") },
        { "LastTime",  new BsonDocument("$last",  "$okunmaZamani") },
        { "FirstImport", new BsonDocument("$first", "$ImportWh") },
        { "LastImport",  new BsonDocument("$last",  "$ImportWh") },
        { "FirstExport", new BsonDocument("$first", "$ExportWh") },
        { "LastExport",  new BsonDocument("$last",  "$ExportWh") },

        // Diğerleri için "ilk" (istersen $avg’a dönebilirsin)
        { "L1_V", new BsonDocument("$first", "$L1_V") },
        { "L2_V", new BsonDocument("$first", "$L2_V") },
        { "L3_V", new BsonDocument("$first", "$L3_V") },
        { "Gerilim_L12_V", new BsonDocument("$first", "$Gerilim_L12_V") },
        { "Gerilim_L23_V", new BsonDocument("$first", "$Gerilim_L23_V") },
        { "Gerilim_L31_V", new BsonDocument("$first", "$Gerilim_L31_V") },

        { "L1_A", new BsonDocument("$first", "$L1_A") },
        { "L2_A", new BsonDocument("$first", "$L2_A") },
        { "L3_A", new BsonDocument("$first", "$L3_A") },
        { "Toplam_Akim_A", new BsonDocument("$first", "$Toplam_Akim_A") },

        { "AktifGuc_L1_W", new BsonDocument("$first", "$AktifGuc_L1_W") },
        { "AktifGuc_L2_W", new BsonDocument("$first", "$AktifGuc_L2_W") },
        { "AktifGuc_L3_W", new BsonDocument("$first", "$AktifGuc_L3_W") },
        { "ToplamAktifGuc_W", new BsonDocument("$first", "$ToplamAktifGuc_W") },

        { "ReaktifGuc_L1_VAr", new BsonDocument("$first", "$ReaktifGuc_L1_VAr") },
        { "ReaktifGuc_L2_VAr", new BsonDocument("$first", "$ReaktifGuc_L2_VAr") },
        { "ReaktifGuc_L3_VAr", new BsonDocument("$first", "$ReaktifGuc_L3_VAr") },
        { "ToplamReaktifGuc_VAr", new BsonDocument("$first", "$ToplamReaktifGuc_VAr") },

        { "GucFaktoru_L1", new BsonDocument("$first", "$GucFaktoru_L1") },
        { "GucFaktoru_L2", new BsonDocument("$first", "$GucFaktoru_L2") },
        { "GucFaktoru_L3", new BsonDocument("$first", "$GucFaktoru_L3") },

        { "Cos_L1", new BsonDocument("$first", "$Cos_L1") },
        { "Cos_L2", new BsonDocument("$first", "$Cos_L2") },
        { "Cos_L3", new BsonDocument("$first", "$Cos_L3") },

        { "Frekans_Hz", new BsonDocument("$first", "$Frekans_Hz") }
    });

            // delta ve süre (saat) hesapları
            var addFieldsStage = new BsonDocument("$addFields", new BsonDocument
    {
        { "DeltaWh", new BsonDocument("$max", new BsonArray {
            new BsonDocument("$subtract", new BsonArray { "$LastImport", "$FirstImport" }),
            0
        })},
        { "DeltaExportWh", new BsonDocument("$max", new BsonArray {
            new BsonDocument("$subtract", new BsonArray { "$LastExport", "$FirstExport" }),
            0
        })},
        { "DurationHours", new BsonDocument("$divide", new BsonArray {
            new BsonDocument("$subtract", new BsonArray { "$LastTime", "$FirstTime" }),
            1000 * 60 * 60
        })}
    });

            // final alanlar (kWh ve ortalama kW)
            var projectFinal = new BsonDocument("$project", new BsonDocument
    {
        { "_id", 0 },
        { "CihazId", 1 },

        // interval zaman damgası: istersen "$FirstTime" veya "$LastTime"
        { "OkunmaZamani", "$LastTime" },

        { "L1_V", 1 }, { "L2_V", 1 }, { "L3_V", 1 },
        { "Gerilim_L12_V", 1 }, { "Gerilim_L23_V", 1 }, { "Gerilim_L31_V", 1 },
        { "L1_A", 1 }, { "L2_A", 1 }, { "L3_A", 1 },
        { "Toplam_Akim_A", 1 },

        { "AktifGuc_L1_W", new BsonDocument("$divide", new BsonArray { "$AktifGuc_L1_W", 1000 }) },
        { "AktifGuc_L2_W", new BsonDocument("$divide", new BsonArray { "$AktifGuc_L2_W", 1000 }) },
        { "AktifGuc_L3_W", new BsonDocument("$divide", new BsonArray { "$AktifGuc_L3_W", 1000 }) },
        { "ToplamAktifGuc_W", new BsonDocument("$divide", new BsonArray { "$ToplamAktifGuc_W", 1000 }) },

        { "ReaktifGuc_L1_VAr", new BsonDocument("$divide", new BsonArray { "$ReaktifGuc_L1_VAr", 1000 }) },
        { "ReaktifGuc_L2_VAr", new BsonDocument("$divide", new BsonArray { "$ReaktifGuc_L2_VAr", 1000 }) },
        { "ReaktifGuc_L3_VAr", new BsonDocument("$divide", new BsonArray { "$ReaktifGuc_L3_VAr", 1000 }) },
        { "ToplamReaktifGuc_VAr", new BsonDocument("$divide", new BsonArray { "$ToplamReaktifGuc_VAr", 1000 }) },

        { "GucFaktoru_L1", 1 }, { "GucFaktoru_L2", 1 }, { "GucFaktoru_L3", 1 },
        { "Cos_L1", 1 }, { "Cos_L2", 1 }, { "Cos_L3", 1 },
        { "Frekans_Hz", 1 },

        { "ImportAktifEnerji_kWh", new BsonDocument("$divide", new BsonArray { "$DeltaWh", 1000 }) },
        { "ExportAktifEnerji_kWh", new BsonDocument("$divide", new BsonArray { "$DeltaExportWh", 1000 }) },
        { "ImportOrtalamaEnerji_kWh", new BsonDocument("$cond", new BsonArray {
            new BsonDocument("$gt", new BsonArray { "$DurationHours", 0 }),
            new BsonDocument("$divide", new BsonArray {
                new BsonDocument("$divide", new BsonArray { "$DeltaWh", "$DurationHours" }), // Wh/h = W
                1000 // W -> kW
            }),
            0
        })}
    });

            var sortFinal = new BsonDocument("$sort", new BsonDocument("OkunmaZamani", 1));

            var pipeline = new[] { matchStage, projectStage, sortByTime, groupStage, addFieldsStage, projectFinal, sortFinal };

            var aggregateOptions = new AggregateOptions
            {
                AllowDiskUse = true,        // büyük veri setleri için kritik
                MaxTime = TimeSpan.FromSeconds(120),
                BatchSize = 1000
            };

            var results = await _logs.Aggregate<BsonDocument>(pipeline, aggregateOptions).ToListAsync(cancellationToken);

            // Mapping
            var mapped = results.Select(doc =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new EnerjiRaporGrafikModel
                {

                    CihazId = doc["CihazId"].AsInt32,
                    CihazName = cihaz.Name,
                    MakineName = cihaz.MakineName,
                    BolumName = cihaz.BolumName,
                    OkunmaZamani = doc["OkunmaZamani"].ToUniversalTime().AddHours(-3),

                    L1_V = doc.GetValue("L1_V", 0).ToDoubleSafe(),
                    L2_V = doc.GetValue("L2_V", 0).ToDoubleSafe(),
                    L3_V = doc.GetValue("L3_V", 0).ToDoubleSafe(),

                    Gerilim_L12_V = doc.GetValue("Gerilim_L12_V", 0).ToDoubleSafe(),
                    Gerilim_L23_V = doc.GetValue("Gerilim_L23_V", 0).ToDoubleSafe(),
                    Gerilim_L31_V = doc.GetValue("Gerilim_L31_V", 0).ToDoubleSafe(),

                    L1_A = doc.GetValue("L1_A", 0).ToDoubleSafe(),
                    L2_A = doc.GetValue("L2_A", 0).ToDoubleSafe(),
                    L3_A = doc.GetValue("L3_A", 0).ToDoubleSafe(),
                    Notr_A = doc.GetValue("Toplam_Akim_A", 0).ToDoubleSafe(),

                    AktifGuc_L1_W = doc.GetValue("AktifGuc_L1_W", 0).ToDoubleSafe(),
                    AktifGuc_L2_W = doc.GetValue("AktifGuc_L2_W", 0).ToDoubleSafe(),
                    AktifGuc_L3_W = doc.GetValue("AktifGuc_L3_W", 0).ToDoubleSafe(),
                    ToplamAktifGuc_W = doc.GetValue("ToplamAktifGuc_W", 0).ToDoubleSafe(),

                    ReaktifGuc_L1_VAr = doc.GetValue("ReaktifGuc_L1_VAr", 0).ToDoubleSafe(),
                    ReaktifGuc_L2_VAr = doc.GetValue("ReaktifGuc_L2_VAr", 0).ToDoubleSafe(),
                    ReaktifGuc_L3_VAr = doc.GetValue("ReaktifGuc_L3_VAr", 0).ToDoubleSafe(),
                    ToplamReaktifGuc_VAr = doc.GetValue("ToplamReaktifGuc_VAr", 0).ToDoubleSafe(),

                    GucFaktoru_L1 = doc.GetValue("GucFaktoru_L1", 0).ToDoubleSafe(),
                    GucFaktoru_L2 = doc.GetValue("GucFaktoru_L2", 0).ToDoubleSafe(),
                    GucFaktoru_L3 = doc.GetValue("GucFaktoru_L3", 0).ToDoubleSafe(),

                    Cos_L1 = doc.GetValue("Cos_L1", 0).ToDoubleSafe(),
                    Cos_L2 = doc.GetValue("Cos_L2", 0).ToDoubleSafe(),
                    Cos_L3 = doc.GetValue("Cos_L3", 0).ToDoubleSafe(),

                    Frekans_Hz = doc.GetValue("Frekans_Hz", 0).ToDoubleSafe(),

                    ImportAktifEnerji_kWh = (decimal)doc.GetValue("ImportAktifEnerji_kWh", 0).ToDecimalSafe(),
                    ImportOrtalamaEnerji_kWh = (decimal)doc.GetValue("ImportOrtalamaEnerji_kWh", 0).ToDecimalSafe(),
                    ExportAktifEnerji_kWh = (decimal)doc.GetValue("ExportAktifEnerji_kWh", 0).ToDecimalSafe()
                };
            }).ToList();

            return mapped;
        }

        public async Task<List<EnerjiRaporGrafikModel>> GetTuketimVerileriForEnerjiGrafikRaporByCokluCihazId(List<vwCihaz> cihazlar, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval, CancellationToken cancellationToken)
        {
            if (cihazlar == null || cihazlar.Count == 0)
                return new List<EnerjiRaporGrafikModel>();

            if (dakikaInterval <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(dakikaInterval),
                    "Dakika interval değeri sıfırdan büyük olmalıdır.");

            cihazlar = cihazlar
                .DistinctBy(x => x.Id)
                .ToList();

            var cihazIds = cihazlar
                .Select(x => x.Id)
                .ToList();

            // Int overflow oluşmaması için long hesaplanıyor.
            var intervalMs = checked((long)1000 * 60 * dakikaInterval);

            var matchStage = new BsonDocument(
                "$match",
                new BsonDocument
                {
            {
                "payload.okunan_analizor_id",
                new BsonDocument("$in", new BsonArray(cihazIds))
            },
            {
                "payload.okunma_zamani",
                new BsonDocument
                {
                    { "$gte", baslangicZamani.AddHours(3) },
                    { "$lte", bitisZamani.AddHours(3) }
                }
            },
            {
                "payload.state",
                new BsonInt32((int)EnerjiIzlemeDurum.Normal)
            },
            {
                "payload.veri.Import_Aktif_Enerji_Wh",
                new BsonDocument("$ne", BsonNull.Value)
            }
                });

            var projectStage = new BsonDocument(
                "$project",
                new BsonDocument
                {
            {
                "minuteGroup",
                new BsonDocument(
                    "$subtract",
                    new BsonArray
                    {
                        new BsonDocument(
                            "$toLong",
                            "$payload.okunma_zamani"),

                        new BsonDocument(
                            "$mod",
                            new BsonArray
                            {
                                new BsonDocument(
                                    "$toLong",
                                    "$payload.okunma_zamani"),

                                new BsonInt64(intervalMs)
                            })
                    })
            },

            { "okunmaZamani", "$payload.okunma_zamani" },
            { "cihazId", "$payload.okunan_analizor_id" },

            { "L1_A", "$payload.veri.L1_A" },
            { "L2_A", "$payload.veri.L2_A" },
            { "L3_A", "$payload.veri.L3_A" },
            { "Toplam_Akim_A", "$payload.veri.Toplam_Akim_A" },
            { "ToplamAktifGuc_W", "$payload.veri.ToplamAktifGuc_W" },

            {
                "Import_Aktif_Enerji_Wh",
                "$payload.veri.Import_Aktif_Enerji_Wh"
            },
            {
                "Export_Aktif_Enerji_Wh",
                "$payload.veri.Export_Aktif_Enerji_Wh"
            }
                });

            /*
             * $first ve $last kullanılacağı için sıralama zorunludur.
             *
             * Önce cihaz, sonra interval, sonra zaman sıralanıyor.
             */
            //var sortBeforeGroupStage = new BsonDocument(
            //    "$sort",
            //    new BsonDocument
            //    {
            //{ "cihazId", 1 },
            //{ "minuteGroup", 1 },
            //{ "okunmaZamani", 1 }
            //    });
            var sortBeforeProjectStage = new BsonDocument(
             "$sort",
             new BsonDocument
             {
                 { "payload.okunan_analizor_id", 1 },
                 { "payload.okunma_zamani", 1 }
             });

            /*
             * Her cihazın her interval içindeki gerçek ilk ve son
             * sayaç kayıtları alınıyor.
             */
            var groupStage1 = new BsonDocument(
                "$group",
                new BsonDocument
                {
            {
                "_id",
                new BsonDocument
                {
                    { "minuteGroup", "$minuteGroup" },
                    { "cihazId", "$cihazId" }
                }
            },

            {
                "FirstTime",
                new BsonDocument("$first", "$okunmaZamani")
            },
            {
                "LastTime",
                new BsonDocument("$last", "$okunmaZamani")
            },

            {
                "FirstImport",
                new BsonDocument(
                    "$first",
                    "$Import_Aktif_Enerji_Wh")
            },
            {
                "LastImport",
                new BsonDocument(
                    "$last",
                    "$Import_Aktif_Enerji_Wh")
            },
            {
                "FirstExport",
                new BsonDocument(
                    "$first",
                    "$Export_Aktif_Enerji_Wh")
            },
            {
                "LastExport",
                new BsonDocument(
                    "$last",
                    "$Export_Aktif_Enerji_Wh")
            },

            { "L1_A", new BsonDocument("$avg", "$L1_A") },
            { "L2_A", new BsonDocument("$avg", "$L2_A") },
            { "L3_A", new BsonDocument("$avg", "$L3_A") },

            {
                "Toplam_Akim_A",
                new BsonDocument("$avg", "$Toplam_Akim_A")
            },
            {
                "ToplamAktifGuc_W",
                new BsonDocument("$avg", "$ToplamAktifGuc_W")
            }
                });

            /*
             * Aynı interval içindeki bütün cihazlar tek satırda toplanıyor.
             */
            var groupStage2 = new BsonDocument(
                "$group",
                new BsonDocument
                {
            { "_id", "$_id.minuteGroup" },

            {
                "OkunmaZamani",
                new BsonDocument("$max", "$LastTime")
            },

            { "L1_A", new BsonDocument("$sum", "$L1_A") },
            { "L2_A", new BsonDocument("$sum", "$L2_A") },
            { "L3_A", new BsonDocument("$sum", "$L3_A") },

            {
                "Toplam_Akim_A",
                new BsonDocument("$sum", "$Toplam_Akim_A")
            },
            {
                "ToplamAktifGuc_W",
                new BsonDocument("$sum", "$ToplamAktifGuc_W")
            },

            {
                "Cihazlar",
                new BsonDocument(
                    "$push",
                    new BsonDocument
                    {
                        { "CihazId", "$_id.cihazId" },

                        { "FirstImport", "$FirstImport" },
                        { "LastImport", "$LastImport" },
                        { "FirstExport", "$FirstExport" },
                        { "LastExport", "$LastExport" },

                        { "FirstTime", "$FirstTime" },
                        { "LastTime", "$LastTime" },

                        { "L1_A", "$L1_A" },
                        { "L2_A", "$L2_A" },
                        { "L3_A", "$L3_A" },
                        { "Toplam_Akim_A", "$Toplam_Akim_A" },
                        { "ToplamAktifGuc_W", "$ToplamAktifGuc_W" }
                    })
            }
                });

            var sortFinalStage = new BsonDocument(
                "$sort",
                new BsonDocument("_id", 1));

            //        var pipeline = new[]
            //        {
            //    matchStage,
            //    projectStage,
            //    sortBeforeGroupStage,
            //    groupStage1,
            //    groupStage2,
            //    sortFinalStage
            //};
            var pipeline = new[]
                            {
                        matchStage,
                    
                        // İndeks tarafından karşılanabilecek sıralama
                        sortBeforeProjectStage,
                    
                        // minuteGroup bu aşamada hesaplanıyor
                        projectStage,

                        groupStage1,
                        groupStage2,
                        sortFinalStage
                    };

            var aggregateOptions = new AggregateOptions
            {
                AllowDiskUse = true,
                MaxTime = TimeSpan.FromSeconds(120),
                BatchSize = 1000
            };

            var results = await _logs
                .Aggregate<BsonDocument>(pipeline, aggregateOptions)
                .ToListAsync(cancellationToken);

            var cihazLookup = cihazlar.ToDictionary(
                x => x.Id,
                x => x);

            var mapped = new List<EnerjiRaporGrafikModel>();

            foreach (var doc in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                decimal toplamImportKwh = 0;
                decimal toplamOrtalamaKw = 0;
                decimal toplamExportKwh = 0;

                var detaylar =
                    new List<EnerjiRaporGrafikCihazDetayModel>();

                var cihazDokumanlari = doc
                    .GetValue("Cihazlar", new BsonArray())
                    .AsBsonArray;

                foreach (var cihazBson in cihazDokumanlari)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var cihazDoc = cihazBson.AsBsonDocument;

                    var cihazIdValue = cihazDoc.GetValue(
                        "CihazId",
                        BsonNull.Value);

                    if (cihazIdValue.IsBsonNull)
                        continue;

                    var cihazId = cihazIdValue.ToInt32();

                    var firstImport = cihazDoc
                        .GetValue("FirstImport", 0)
                        .ToDecimalSafe();

                    var lastImport = cihazDoc
                        .GetValue("LastImport", 0)
                        .ToDecimalSafe();

                    var firstExport = cihazDoc
                        .GetValue("FirstExport", 0)
                        .ToDecimalSafe();

                    var lastExport = cihazDoc
                        .GetValue("LastExport", 0)
                        .ToDecimalSafe();

                    var firstTime = cihazDoc
                        .GetValue("FirstTime", BsonNull.Value)
                        .IsBsonNull
                            ? DateTime.MinValue
                            : cihazDoc["FirstTime"].ToUniversalTime();

                    var lastTime = cihazDoc
                        .GetValue("LastTime", BsonNull.Value)
                        .IsBsonNull
                            ? DateTime.MinValue
                            : cihazDoc["LastTime"].ToUniversalTime();

                    decimal importFarkWh = 0;
                    decimal importKwh = 0;
                    decimal importOrtalamaKw = 0;
                    decimal exportKwh = 0;

                    if (firstTime != DateTime.MinValue &&
                        lastTime != DateTime.MinValue)
                    {
                        var dakikaFarki =
                            (lastTime - firstTime).TotalMinutes;

                        var hamImportFarki =
                            lastImport - firstImport;

                        /*
                         * Sayaç resetlendiyse veya son değer ilk değerden
                         * küçükse astronomik/yanlış tüketim üretme.
                         */
                        if (dakikaFarki > 0 &&
                            hamImportFarki >= 0)
                        {
                            importFarkWh = hamImportFarki;
                            importKwh = importFarkWh / 1000m;

                            var sureSaat =
                                (decimal)dakikaFarki / 60m;

                            importOrtalamaKw =
                                importFarkWh / sureSaat / 1000m;

                            /*
                             * İsteğe bağlı aşırı değer kontrolü.
                             *
                             * Sayaçtan hesaplanan ortalama güç, ölçülen
                             * ortalama aktif gücün 5 katından fazlaysa
                             * veri şüpheli kabul edilir.
                             */
                            var olculenOrtalamaKw = cihazDoc
                                .GetValue("ToplamAktifGuc_W", 0)
                                .ToDecimalSafe() / 1000m;

                            if (olculenOrtalamaKw > 0 &&
                                importOrtalamaKw >
                                olculenOrtalamaKw * 5m)
                            {
                                importFarkWh = 0;
                                importKwh = 0;
                                importOrtalamaKw = 0;
                            }
                        }

                        var hamExportFarki =
                            lastExport - firstExport;

                        if (dakikaFarki > 0 &&
                            hamExportFarki >= 0)
                        {
                            exportKwh = hamExportFarki / 1000m;
                        }
                    }

                    toplamImportKwh += importKwh;
                    toplamOrtalamaKw += importOrtalamaKw;
                    toplamExportKwh += exportKwh;

                    cihazLookup.TryGetValue(
                        cihazId,
                        out var cihazMeta);

                    detaylar.Add(
                        new EnerjiRaporGrafikCihazDetayModel
                        {
                            CihazId = cihazId,
                            CihazName =
                                cihazMeta?.Name ?? string.Empty,
                            MakineName =
                                cihazMeta?.MakineName ?? string.Empty,
                            BolumName =
                                cihazMeta?.BolumName ?? string.Empty,

                            L1_A = cihazDoc
                                .GetValue("L1_A", 0)
                                .ToDoubleSafe(),

                            L2_A = cihazDoc
                                .GetValue("L2_A", 0)
                                .ToDoubleSafe(),

                            L3_A = cihazDoc
                                .GetValue("L3_A", 0)
                                .ToDoubleSafe(),

                            Toplam_Akim_A = cihazDoc
                                .GetValue("Toplam_Akim_A", 0)
                                .ToDoubleSafe(),

                            ToplamAktifGuc_W = cihazDoc
                                .GetValue("ToplamAktifGuc_W", 0)
                                .ToDoubleSafe(),

                            ImportAktifEnerji_kWh =
                                importKwh,

                            ImportOrtalamaEnerji_kWh =
                                importOrtalamaKw,

                            ExportAktifEnerji_kWh =
                                exportKwh,

                            MinTime = firstTime == DateTime.MinValue
                                ? null
                                : firstTime.AddHours(-3),

                            MaxTime = lastTime == DateTime.MinValue
                                ? null
                                : lastTime.AddHours(-3)
                        });
                }

                mapped.Add(
                    new EnerjiRaporGrafikModel
                    {
                        OkunmaZamani = doc
                            .GetValue(
                                "OkunmaZamani",
                                BsonNull.Value)
                            .IsBsonNull
                                ? default
                                : doc["OkunmaZamani"]
                                    .ToUniversalTime()
                                    .AddHours(-3),

                        L1_A = doc
                            .GetValue("L1_A", 0)
                            .ToDoubleSafe(),

                        L2_A = doc
                            .GetValue("L2_A", 0)
                            .ToDoubleSafe(),

                        L3_A = doc
                            .GetValue("L3_A", 0)
                            .ToDoubleSafe(),

                        Toplam_Akim_A = doc
                            .GetValue("Toplam_Akim_A", 0)
                            .ToDoubleSafe() / 1000,

                        ToplamAktifGuc_W = doc
                            .GetValue("ToplamAktifGuc_W", 0)
                            .ToDoubleSafe() / 1000,

                        ImportAktifEnerji_kWh =
                            toplamImportKwh,

                        ImportOrtalamaEnerji_kWh =
                            toplamOrtalamaKw,

                        ExportAktifEnerji_kWh =
                            toplamExportKwh,

                        CihazDetaylar = detaylar
                    });
            }

            return mapped;
        }
        public async Task<List<BsonDocument>> GetFirstLastByCihazIdsAsync(List<int> cihazIds, DateTime start, DateTime end, CancellationToken cancellationToken)
        {
            var s = start.AddHours(3);
            var e = end.AddHours(3);

            // ✅ FilterDefinition<Logs>
            var filter =
                Builders<Logs>.Filter.In("payload.okunan_analizor_id", cihazIds) &
                Builders<Logs>.Filter.Gte("payload.okunma_zamani", s) &
                Builders<Logs>.Filter.Lte("payload.okunma_zamani", e) &
                Builders<Logs>.Filter.Eq("payload.state", (int)EnerjiIzlemeDurum.Normal);

            var sort = Builders<Logs>.Sort
                .Ascending("payload.okunan_analizor_id")
                .Ascending("payload.okunma_zamani");

            var group = new BsonDocument
    {
        { "_id", "$payload.okunan_analizor_id" },
        { "first", new BsonDocument("$first", "$$ROOT") },
        { "last",  new BsonDocument("$last",  "$$ROOT") }
    };

            var project = new BsonDocument
    {
        { "cihazId", "$_id" },
        { "ilk_zaman", "$first.payload.okunma_zamani" },
        { "son_zaman", "$last.payload.okunma_zamani" },
        { "import_fark", new BsonDocument("$subtract", new BsonArray
            {
                new BsonDocument("$ifNull", new BsonArray{ "$last.payload.veri.Import_Aktif_Enerji_Wh", 0 }),
                new BsonDocument("$ifNull", new BsonArray{ "$first.payload.veri.Import_Aktif_Enerji_Wh", 0 })
            })
        },
        { "dakika_farki", new BsonDocument("$divide", new BsonArray
            {
                new BsonDocument("$subtract", new BsonArray{ "$last.payload.okunma_zamani", "$first.payload.okunma_zamani" }),
                60000
            })
        }
    };

            // ✅ Aggregate typed başlar, Group/Project BsonDocument’e döner
            var list = await _logs.Aggregate()
                .Match(filter)
                .Sort(sort)
                .Group<BsonDocument>(group)
                .Project(project)
                .ToListAsync(cancellationToken);

            // Kayıt olmayan cihazlar için “veri_bulunamadi”
            var found = list.Select(x => x.GetValue("cihazId").ToInt32()).ToHashSet();
            foreach (var id in cihazIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!found.Contains(id))
                {
                    list.Add(new BsonDocument
            {
                { "cihazId", id },
                { "veri_bulunamadi", true }
            });
                }
            }

            return list;
        }

        public async Task<List<HaftalikSayacTuketimModel>> GetSayacTuketimleri(List<vwCihaz> secilenCihazlar, List<vwCihaz> tumCihazlar, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            if (secilenCihazlar == null || secilenCihazlar.Count == 0)
                return new List<HaftalikSayacTuketimModel>();

            // Seçilen cihazların id seti
            var secilenIds = secilenCihazlar.Select(x => x.Id).ToHashSet();

            // Seçilen cihazlardan parent olanların child'larını da ekle
            var childCihazlar = tumCihazlar
                .Where(c => c.ParentCihazId != 0 && secilenIds.Contains(c.ParentCihazId))
                .ToList();

            // Sorguya girecek tüm cihazlar (parent + child), duplicate olmasın
            var sorgulananCihazlar = secilenCihazlar
                .Concat(childCihazlar)
                .DistinctBy(c => c.Id)
                .ToList();

            var cihazIds = sorgulananCihazlar.Select(x => x.Id).ToList();

            var matchStage = new BsonDocument("$match", new BsonDocument {
        { "payload.okunan_analizor_id", new BsonDocument("$in", new BsonArray(cihazIds)) },
        { "payload.okunma_zamani", new BsonDocument {
            { "$gte", baslangicZamani.AddHours(3) },
            { "$lte", bitisZamani.AddHours(3) }
        }},
        { "payload.state", new BsonInt32(1) }
    });

            var projectStage = new BsonDocument("$project", new BsonDocument {
        { "okunmaZamani", "$payload.okunma_zamani" },
        { "cihazId", "$payload.okunan_analizor_id" },
        { "Import_Aktif_Enerji_Wh", "$payload.veri.Import_Aktif_Enerji_Wh" }
    });

            var sortStage = new BsonDocument("$sort", new BsonDocument("okunmaZamani", 1));

            var groupStage = new BsonDocument("$group", new BsonDocument {
        { "_id", "$cihazId" },
        { "IlkZaman",  new BsonDocument("$first", "$okunmaZamani") },
        { "SonZaman",  new BsonDocument("$last",  "$okunmaZamani") },
        { "IlkImport", new BsonDocument("$first", "$Import_Aktif_Enerji_Wh") },
        { "SonImport", new BsonDocument("$last",  "$Import_Aktif_Enerji_Wh") }
    });

            var pipeline = new[] { matchStage, projectStage, sortStage, groupStage };

            var results = await _logs.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);

            var cihazLookup = sorgulananCihazlar.ToDictionary(c => c.Id, c => c);

            // Child → Parent eşlemesi
            var childToParent = sorgulananCihazlar
                .Where(c => c.ParentCihazId != 0)
                .ToDictionary(c => c.Id, c => c.ParentCihazId);

            // Parent bazında grupla: her parent kendi + child'larının tüketimini toplar
            // parentId → detay listesi
            var parentGruplari = new Dictionary<int, List<HaftalikCihazTuketimDetayModel>>();
            var parentToplamlar = new Dictionary<int, decimal>();

            // Önce tüm parent'ları init et (seçilen parent cihazlar)
            foreach (var c in secilenCihazlar.Where(c => c.ParentCihazId == 0 || !secilenIds.Contains(c.ParentCihazId)))
            {
                parentGruplari[c.Id] = new List<HaftalikCihazTuketimDetayModel>();
                parentToplamlar[c.Id] = 0;
            }

            decimal genelToplam = 0;

            foreach (var doc in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cihazId = doc["_id"].ToInt32();
                var ilkImport = doc.GetValue("IlkImport", BsonNull.Value).ToDecimalSafe();
                var sonImport = doc.GetValue("SonImport", BsonNull.Value).ToDecimalSafe();
                var tuketim_kWh = (sonImport - ilkImport) / 1000m;
                if (tuketim_kWh < 0) tuketim_kWh = 0;

                var ilkZaman = doc["IlkZaman"].ToUniversalTime().AddHours(3);
                var sonZaman = doc["SonZaman"].ToUniversalTime().AddHours(3);

                cihazLookup.TryGetValue(cihazId, out var meta);

                var detay = new HaftalikCihazTuketimDetayModel
                {
                    CihazId = cihazId,
                    CihazName = meta?.Name ?? string.Empty,
                    MakineName = meta?.MakineName ?? string.Empty,
                    BolumName = meta?.BolumName ?? string.Empty,
                    IlkOkuma_Wh = ilkImport,
                    SonOkuma_Wh = sonImport,
                    Tuketim_kWh = tuketim_kWh,
                    IlkOkumaZamani = ilkZaman,
                    SonOkumaZamani = sonZaman
                };

                // Bu cihaz bir child mı?
                if (childToParent.TryGetValue(cihazId, out var parentId) && parentGruplari.ContainsKey(parentId))
                {
                    parentGruplari[parentId].Add(detay);
                    parentToplamlar[parentId] += tuketim_kWh;
                }
                else if (parentGruplari.ContainsKey(cihazId))
                {
                    // Parent'ın kendi verisi
                    parentGruplari[cihazId].Insert(0, detay); // başa ekle
                    parentToplamlar[cihazId] += tuketim_kWh;
                }

                genelToplam += tuketim_kWh;
            }

            // Sonuç modelini oluştur: her parent için ayrı satır + altındaki child detaylar
            var cihazDetaylar = parentGruplari
                .OrderBy(kv => kv.Key)
                .Select(kv =>
                {
                    cihazLookup.TryGetValue(kv.Key, out var parentMeta);

                    // Parent'ın kendi ilk/son okuması (varsa listedeki ilk eleman)
                    var parentDetay = kv.Value.FirstOrDefault(d => d.CihazId == kv.Key);

                    return new HaftalikCihazTuketimDetayModel
                    {
                        CihazId = kv.Key,
                        CihazName = parentMeta?.Name ?? string.Empty,
                        MakineName = parentMeta?.MakineName ?? string.Empty,
                        BolumName = parentMeta?.BolumName ?? string.Empty,
                        IlkOkuma_Wh = parentDetay?.IlkOkuma_Wh ?? 0,
                        SonOkuma_Wh = parentDetay?.SonOkuma_Wh ?? 0,
                        Tuketim_kWh = parentToplamlar[kv.Key],   // parent + tüm child toplamı
                        IlkOkumaZamani = parentDetay?.IlkOkumaZamani ?? default,
                        SonOkumaZamani = parentDetay?.SonOkumaZamani ?? default,
                        AltCihazlar = kv.Value.Where(d => d.CihazId != kv.Key).ToList()
                    };
                }).ToList();

            return new List<HaftalikSayacTuketimModel>
    {
        new HaftalikSayacTuketimModel
        {
            HaftaBaslangic    = baslangicZamani,
            HaftaBitis        = bitisZamani,
            ToplamTuketim_kWh = genelToplam,
            CihazDetaylar     = cihazDetaylar
        }
    };
        }
        public async Task<(decimal? IlkImportAktifEnerji, decimal? IlkExportAktifEnerji, DateTime? IlkOkunmaZamani)> GetIlkOkunanImportAktifEnerjiAsync(int cihazId, DateTime tarih, CancellationToken cancellationToken)
        {
            /*
             * MongoDB'deki kayıtlar Türkiye saatini gösteriyor,
             * ancak +00:00 olarak işaretlenmiş.
             *
             * Bu nedenle 12:33 değerini 09:33'e çevirmeden,
             * saat değerini aynen koruyarak UTC türünde sorguluyoruz.
             */
            var sorguTarihi = DateTime.SpecifyKind(
                tarih,
                DateTimeKind.Utc);

            var result = await _logs
                .Find(x =>
                    x.payload.okunan_analizor_id == cihazId &&
                    x.payload.okunma_zamani >= sorguTarihi)
                .SortBy(x => x.payload.okunma_zamani)
                .Project(x => new
                {
                    x.payload.okunma_zamani,
                    ImportAktifEnerji = x.payload.veri.Import_Aktif_Enerji_Wh,
                    ExportAktifEnerji = x.payload.veri.Export_Aktif_Enerji_Wh
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (result == null)
                return (null, null, null);

            decimal? ilkImportAktifEnerji =
                result.ImportAktifEnerji.HasValue
                    ? Convert.ToDecimal(result.ImportAktifEnerji.Value)
                    : null;

            decimal? ilkExportAktifEnerji =
                result.ExportAktifEnerji.HasValue
                    ? Convert.ToDecimal(result.ExportAktifEnerji.Value)
                    : null;

            /*
             * MongoDB'den 12:33 UTC türünde gelen değeri,
             * saatini değiştirmeden Unspecified yapıyoruz.
             *
             * Böylece API 12:33 döner ve Angular tekrar +3 eklemez.
             */
            DateTime? ilkOkunmaZamani =
                result.okunma_zamani == default
                    ? null
                    : DateTime.SpecifyKind(
                        result.okunma_zamani,
                        DateTimeKind.Unspecified);

            return (ilkImportAktifEnerji, ilkExportAktifEnerji, ilkOkunmaZamani);
        }
        //public async Task<EnerjiHaftalikRaporModel> GetEnerjiRaporu(List<vwCihaz> tumCihazlar, List<int> secilenParentCihazIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        //{
        //    // Önceki dönem aralığı
        //    var aralikSuresi = bitisZamani - baslangicZamani;
        //    var oncekiBaslangic = baslangicZamani - aralikSuresi;
        //    var oncekiBitis = baslangicZamani;

        //    // Seçilen parent'lar ve onların child'larını bul
        //    var parentCihazlar = tumCihazlar
        //        .Where(c => secilenParentCihazIds.Contains(c.Id))
        //        .ToList();

        //    var childCihazlar = tumCihazlar
        //        .Where(c => c.ParentCihazId != 0 && secilenParentCihazIds.Contains(c.ParentCihazId))
        //        .ToList();

        //    var tumSorgulananCihazlar = parentCihazlar
        //        .Concat(childCihazlar)
        //        .DistinctBy(c => c.Id)
        //        .ToList();

        //    var tumIds = tumSorgulananCihazlar.Select(x => x.Id).ToList();

        //    // Bu dönem + önceki dönem verilerini paralel çek
        //    var buDonemTask = GetCihazIlkSonImport(tumIds, baslangicZamani, bitisZamani, cancellationToken);
        //    var oncekiDonemTask = GetCihazIlkSonImport(tumIds, oncekiBaslangic, oncekiBitis, cancellationToken);

        //    await Task.WhenAll(buDonemTask, oncekiDonemTask);

        //    var buDonem = await buDonemTask;        // cihazId → tuketim_kWh
        //    var oncekiDonem = await oncekiDonemTask;

        //    var cihazLookup = tumSorgulananCihazlar.ToDictionary(c => c.Id, c => c);

        //    // Child → Parent map
        //    var childToParent = childCihazlar.ToDictionary(c => c.Id, c => c.ParentCihazId);

        //    // Tüm analizörlerin (child) genel toplamı → pay hesabı için
        //    decimal genelAnalizorToplami = buDonem
        //        .Where(kv => childToParent.ContainsKey(kv.Key))
        //        .Sum(kv => kv.Value);

        //    // Trafo bazında grupla
        //    var trafoRaporlari = new List<TrafoRaporModel>();

        //    foreach (var parent in parentCihazlar)
        //    {
        //        cancellationToken.ThrowIfCancellationRequested();

        //        var parentTuketim = buDonem.TryGetValue(parent.Id, out var pt) ? pt : 0m;

        //        var altCihazlar = childCihazlar.Where(c => c.ParentCihazId == parent.Id).ToList();

        //        var analizorSatirlar = new List<AnalizorSatirModel>();

        //        foreach (var child in altCihazlar)
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();

        //            var buDonemTuketim = buDonem.TryGetValue(child.Id, out var bd) ? bd : 0m;
        //            var oncekiTuketim = oncekiDonem.TryGetValue(child.Id, out var od) ? od : 0m;

        //            analizorSatirlar.Add(new AnalizorSatirModel
        //            {
        //                CihazId = child.Id,
        //                CihazName = child.Name ?? string.Empty,
        //                MakineName = child.MakineName ?? string.Empty,
        //                BolumName = child.BolumName ?? string.Empty,
        //                Tuketim_kWh = buDonemTuketim,
        //                OncekiDonemTuketim_kWh = oncekiTuketim,
        //                ToplamTuketimdekiPay = genelAnalizorToplami > 0
        //                    ? Math.Round(buDonemTuketim / genelAnalizorToplami * 100, 2)
        //                    : 0,
        //                OncekiHaftayaGoreDegisim_kWh = buDonemTuketim - oncekiTuketim,
        //                BagliTrafoName = parent.Name ?? string.Empty
        //            });
        //        }

        //        var analizorToplami = analizorSatirlar.Sum(a => a.Tuketim_kWh);

        //        trafoRaporlari.Add(new TrafoRaporModel
        //        {
        //            TrafoId = parent.Id,
        //            TrafoName = parent.Name ?? string.Empty,
        //            SayacDegeri_kWh = parentTuketim,
        //            AnalizorlerToplami_kWh = analizorToplami,
        //            SayacAnalizorFarki_kWh = parentTuketim - analizorToplami,
        //            Analizorler = analizorSatirlar
        //        });
        //    }

        //    // Trafo paylarını hesapla (tüm trafoların sayaç toplamına göre)
        //    var genelSayacToplami = trafoRaporlari.Sum(t => t.SayacDegeri_kWh);
        //    foreach (var trafo in trafoRaporlari)
        //    {
        //        trafo.ToplamTuketimdekiPay = genelSayacToplami > 0
        //            ? Math.Round(trafo.SayacDegeri_kWh / genelSayacToplami * 100, 2)
        //            : 0;
        //    }

        //    return new EnerjiHaftalikRaporModel
        //    {
        //        BaslangicZamani = baslangicZamani,
        //        BitisZamani = bitisZamani,
        //        GenelToplamTuketim_kWh = genelSayacToplami,
        //        Trafolar = trafoRaporlari,
        //        TumAnalizorler = trafoRaporlari.SelectMany(t => t.Analizorler).ToList()
        //    };
        //}

        //        // Yardımcı metot: verilen cihaz listesi için tarih aralığında ilk/son import farkını döner
        //        private async Task<Dictionary<int, decimal>> GetCihazIlkSonImport(List<int> cihazIds, DateTime baslangic, DateTime bitis, CancellationToken cancellationToken)
        //        {
        //            var matchStage = new BsonDocument("$match", new BsonDocument {
        //    { "payload.okunan_analizor_id", new BsonDocument("$in", new BsonArray(cihazIds)) },
        //    { "payload.okunma_zamani", new BsonDocument {
        //        { "$gte", baslangic.AddHours(3) },
        //        { "$lte", bitis.AddHours(3) }
        //    }},
        //    { "payload.state", new BsonInt32(1) }
        //});

        //            var projectStage = new BsonDocument("$project", new BsonDocument {
        //    { "okunmaZamani", "$payload.okunma_zamani" },
        //    { "cihazId",      "$payload.okunan_analizor_id" },
        //    { "Import_Aktif_Enerji_Wh", "$payload.veri.Import_Aktif_Enerji_Wh" }
        //});

        //            var sortStage = new BsonDocument("$sort", new BsonDocument("okunmaZamani", 1));

        //            var groupStage = new BsonDocument("$group", new BsonDocument {
        //    { "_id",       "$cihazId" },
        //    { "IlkImport", new BsonDocument("$first", "$Import_Aktif_Enerji_Wh") },
        //    { "SonImport", new BsonDocument("$last",  "$Import_Aktif_Enerji_Wh") }
        //});

        //            var pipeline = new[] { matchStage, projectStage, sortStage, groupStage };

        //            var results = await _logs.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);

        //            return results.ToDictionary(
        //                doc => doc["_id"].ToInt32(),
        //                doc =>
        //                {
        //                    var ilk = doc.GetValue("IlkImport", BsonNull.Value).ToDecimalSafe();
        //                    var son = doc.GetValue("SonImport", BsonNull.Value).ToDecimalSafe();
        //                    var fark = (son - ilk) / 1000m;
        //                    return fark < 0 ? 0m : fark;
        //                }
        //            );
        //        }

        //public async Task<ToplamTuketimSonucu> GetToplamImportAktifEnerjiTuketimiAsync(List<int> cihazIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        //{
        //    /*
        //     * MongoDB'deki kayıtlar Türkiye saatini gösteriyor,
        //     * ancak +00:00 olarak işaretlenmiş.
        //     * Bu nedenle saat değerini aynen koruyarak UTC türünde sorguluyoruz.
        //     */
        //    var sorguBaslangic = DateTime.SpecifyKind(baslangicZamani, DateTimeKind.Utc);
        //    var sorguBitis = DateTime.SpecifyKind(bitisZamani, DateTimeKind.Utc);

        //    var sonucListesi = new List<CihazTuketimSonucu>();

        //    // Her cihaz için başlangıç ve bitiş kayıtlarını paralel çekiyoruz
        //    var tasks = cihazIds.Select(async cihazId =>
        //    {
        //        // Başlangıç zamanına eşit veya SONRAKİ en yakın kayıt (artan sıralama, ilk kayıt)
        //        var baslangicKaydiTask = _logs
        //            .Find(x =>
        //                x.payload.okunan_analizor_id == cihazId &&
        //                x.payload.okunma_zamani >= sorguBaslangic)
        //            .SortBy(x => x.payload.okunma_zamani)
        //            .Project(x => new
        //            {
        //                x.payload.okunma_zamani,
        //                ImportAktifEnerji = x.payload.veri.Import_Aktif_Enerji_Wh
        //            })
        //            .FirstOrDefaultAsync(cancellationToken);

        //        // Bitiş zamanına eşit veya ÖNCEKİ en yakın kayıt (azalan sıralama, ilk kayıt)
        //        var bitisKaydiTask = _logs
        //            .Find(x =>
        //                x.payload.okunan_analizor_id == cihazId &&
        //                x.payload.okunma_zamani <= sorguBitis)
        //            .SortByDescending(x => x.payload.okunma_zamani)
        //            .Project(x => new
        //            {
        //                x.payload.okunma_zamani,
        //                ImportAktifEnerji = x.payload.veri.Import_Aktif_Enerji_Wh
        //            })
        //            .FirstOrDefaultAsync(cancellationToken);

        //        await Task.WhenAll(baslangicKaydiTask, bitisKaydiTask);

        //        var baslangicKaydi = baslangicKaydiTask.Result;
        //        var bitisKaydi = bitisKaydiTask.Result;

        //        var sonuc = new CihazTuketimSonucu { CihazId = cihazId };

        //        if (baslangicKaydi != null)
        //        {
        //            sonuc.BaslangicDegeri = baslangicKaydi.ImportAktifEnerji.HasValue
        //                ? Convert.ToDecimal(baslangicKaydi.ImportAktifEnerji.Value)
        //                : null;
        //            sonuc.BaslangicZamani = baslangicKaydi.okunma_zamani == default
        //                ? null
        //                : DateTime.SpecifyKind(baslangicKaydi.okunma_zamani, DateTimeKind.Unspecified);
        //        }

        //        if (bitisKaydi != null)
        //        {
        //            sonuc.BitisDegeri = bitisKaydi.ImportAktifEnerji.HasValue
        //                ? Convert.ToDecimal(bitisKaydi.ImportAktifEnerji.Value)
        //                : null;
        //            sonuc.BitisZamani = bitisKaydi.okunma_zamani == default
        //                ? null
        //                : DateTime.SpecifyKind(bitisKaydi.okunma_zamani, DateTimeKind.Unspecified);
        //        }

        //        if (sonuc.BaslangicDegeri.HasValue && sonuc.BitisDegeri.HasValue)
        //        {
        //            sonuc.Tuketim = sonuc.BitisDegeri.Value - sonuc.BaslangicDegeri.Value;
        //        }
        //        else
        //        {
        //            sonuc.Not = baslangicKaydi == null
        //                ? "Başlangıç zamanı ve sonrasında kayıt bulunamadı"
        //                : "Bitiş zamanı ve öncesinde kayıt bulunamadı";
        //        }

        //        return sonuc;
        //    });

        //    var tumSonuclar = await Task.WhenAll(tasks);
        //    sonucListesi.AddRange(tumSonuclar);

        //    var toplamTuketim = sonucListesi
        //        .Where(x => x.Tuketim.HasValue)
        //        .Sum(x => x.Tuketim!.Value);

        //    return new ToplamTuketimSonucu
        //    {
        //        CihazSonuclari = sonucListesi,
        //        ToplamTuketim = toplamTuketim
        //    };
        //}
        public async Task<CihazTuketimSonucu> GetGesCihazUretimiAsync(int cihazId, string? cihazName, DateTime sorguBaslangic, DateTime sorguBitis, CancellationToken cancellationToken)
        {
            var exportKayitlari = await _logs
                .Find(x =>
                    x.payload.okunan_analizor_id == cihazId &&
                    x.payload.okunma_zamani >= sorguBaslangic &&
                    x.payload.okunma_zamani <= sorguBitis)
                .SortBy(x => x.payload.okunma_zamani)
                .Project(x => new
                {
                    x.payload.okunma_zamani,
                    ExportAktifEnerji =
                        x.payload.veri.Export_Aktif_Enerji_Wh
                })
                .ToListAsync(cancellationToken);

            var sonuc = new CihazTuketimSonucu
            {
                CihazId = cihazId,
                CihazName = cihazName,
                GesMi = true
            };

            if (exportKayitlari == null || !exportKayitlari.Any())
            {
                sonuc.Not =
                    "Seçilen tarih aralığında GES üretim kaydı bulunamadı.";

                return sonuc;
            }

            var exportDegerleri = exportKayitlari
                .Where(x => x.ExportAktifEnerji.HasValue)
                .Select(x =>
                    Convert.ToDecimal(x.ExportAktifEnerji!.Value))
                .ToList();

            if (!exportDegerleri.Any())
            {
                sonuc.Not =
                    "GES cihazında Export_Aktif_Enerji_Wh değeri bulunamadı.";

                return sonuc;
            }

            sonuc.GesExportToplami = exportDegerleri.Sum();

            sonuc.BaslangicZamani = DateTime.SpecifyKind(
                exportKayitlari.First().okunma_zamani,
                DateTimeKind.Unspecified);

            sonuc.BitisZamani = DateTime.SpecifyKind(
                exportKayitlari.Last().okunma_zamani,
                DateTimeKind.Unspecified);

            return sonuc;
        }
        public async Task<CihazTuketimSonucu> GetNormalCihazTuketimiAsync(int cihazId, string? cihazName, DateTime sorguBaslangic, DateTime sorguBitis, CancellationToken cancellationToken)
        {
            var baslangicKaydiTask = _logs
                .Find(x =>
                    x.payload.okunan_analizor_id == cihazId &&
                    x.payload.okunma_zamani >= sorguBaslangic)
                .SortBy(x => x.payload.okunma_zamani)
                .Project(x => new
                {
                    x.payload.okunma_zamani,

                    ImportAktifEnerji =
                        x.payload.veri.Import_Aktif_Enerji_Wh,

                    ExportAktifEnerji =
                        x.payload.veri.Export_Aktif_Enerji_Wh
                })
                .FirstOrDefaultAsync(cancellationToken);

            var bitisKaydiTask = _logs
                .Find(x =>
                    x.payload.okunan_analizor_id == cihazId &&
                    x.payload.okunma_zamani <= sorguBitis)
                .SortByDescending(x => x.payload.okunma_zamani)
                .Project(x => new
                {
                    x.payload.okunma_zamani,

                    ImportAktifEnerji =
                        x.payload.veri.Import_Aktif_Enerji_Wh,

                    ExportAktifEnerji =
                        x.payload.veri.Export_Aktif_Enerji_Wh
                })
                .FirstOrDefaultAsync(cancellationToken);

            await Task.WhenAll(
                baslangicKaydiTask,
                bitisKaydiTask);

            var baslangicKaydi = await baslangicKaydiTask;
            var bitisKaydi = await bitisKaydiTask;

            var sonuc = new CihazTuketimSonucu
            {
                CihazId = cihazId,
                CihazName = cihazName,
                GesMi = false
            };

            if (baslangicKaydi == null)
            {
                sonuc.Not =
                    "Başlangıç zamanı ve sonrasında kayıt bulunamadı.";

                return sonuc;
            }

            if (bitisKaydi == null)
            {
                sonuc.Not =
                    "Bitiş zamanı ve öncesinde kayıt bulunamadı.";

                return sonuc;
            }

            sonuc.BaslangicZamani =
                baslangicKaydi.okunma_zamani == default
                    ? null
                    : DateTime.SpecifyKind(
                        baslangicKaydi.okunma_zamani,
                        DateTimeKind.Unspecified);

            sonuc.BitisZamani =
                bitisKaydi.okunma_zamani == default
                    ? null
                    : DateTime.SpecifyKind(
                        bitisKaydi.okunma_zamani,
                        DateTimeKind.Unspecified);

            sonuc.BaslangicImportDegeri =
                baslangicKaydi.ImportAktifEnerji.HasValue
                    ? Convert.ToDecimal(
                        baslangicKaydi.ImportAktifEnerji.Value)
                    : null;

            sonuc.BitisImportDegeri =
                bitisKaydi.ImportAktifEnerji.HasValue
                    ? Convert.ToDecimal(
                        bitisKaydi.ImportAktifEnerji.Value)
                    : null;

            sonuc.BaslangicExportDegeri =
                baslangicKaydi.ExportAktifEnerji.HasValue
                    ? Convert.ToDecimal(
                        baslangicKaydi.ExportAktifEnerji.Value)
                    : null;

            sonuc.BitisExportDegeri =
                bitisKaydi.ExportAktifEnerji.HasValue
                    ? Convert.ToDecimal(
                        bitisKaydi.ExportAktifEnerji.Value)
                    : null;

            if (sonuc.BaslangicImportDegeri.HasValue &&
                sonuc.BitisImportDegeri.HasValue)
            {
                sonuc.ImportTuketim =
                    sonuc.BitisImportDegeri.Value -
                    sonuc.BaslangicImportDegeri.Value;
            }

            if (sonuc.BaslangicExportDegeri.HasValue &&
                sonuc.BitisExportDegeri.HasValue)
            {
                sonuc.ExportTuketim =
                    sonuc.BitisExportDegeri.Value -
                    sonuc.BaslangicExportDegeri.Value;
            }

            if (!sonuc.ImportTuketim.HasValue &&
                !sonuc.ExportTuketim.HasValue)
            {
                sonuc.Not =
                    "Import ve Export sayaç değerleri hesaplanamadı.";
            }
            else if (!sonuc.ImportTuketim.HasValue)
            {
                sonuc.Not =
                    "Import sayaç değeri hesaplanamadı.";
            }
            else if (!sonuc.ExportTuketim.HasValue)
            {
                sonuc.Not =
                    "Export sayaç değeri hesaplanamadı.";
            }

            return sonuc;
        }
        private static decimal? GuvenliEnerjiDegeri(double? deger)
        {
            if (!deger.HasValue)
                return null;

            var value = deger.Value;

            if (double.IsNaN(value) || double.IsInfinity(value))
                return null;

            // Enerji sayacı negatif olamaz.
            if (value < 0)
                return null;

            // Sisteminize göre gerçekçi bir üst sınır belirleyin.
            const double maksimumEnerji = 1_000_000_000_000; // örnek: 1 trilyon Wh

            if (value > maksimumEnerji)
                return null;

            return (decimal)value;
        }
        public async Task<EnerjiHaftalikRaporModel> GetEnerjiRaporuDonemli(List<vwCihaz> tumCihazlar, List<int> secilenParentCihazIds, DateTime baslangicZamani, DateTime bitisZamani, DateTime oncekiDonemBaslangicZamani, DateTime oncekiDonemBitisZamani, CancellationToken cancellationToken)
        {
            //// Önceki dönem aralığı
            //var aralikSuresi = bitisZamani - baslangicZamani;
            //var oncekiBaslangic = baslangicZamani - aralikSuresi;
            //var oncekiBitis = baslangicZamani;

            // Seçilen parent'lar ve onların child'larını bul
            var parentCihazlar = tumCihazlar
                .Where(c => secilenParentCihazIds.Contains(c.Id))
                .ToList();

            var childCihazlar = tumCihazlar
                .Where(c => c.ParentCihazId != 0 && secilenParentCihazIds.Contains(c.ParentCihazId))
                .ToList();

            var tumSorgulananCihazlar = parentCihazlar
                .Concat(childCihazlar)
                .DistinctBy(c => c.Id)
                .ToList();

            var tumIds = tumSorgulananCihazlar.Select(x => x.Id).ToList();

            // Bu dönem + önceki dönem verilerini paralel çek
            var buDonemTask = GetCihazIlkSonImport(tumIds, baslangicZamani, bitisZamani, cancellationToken);
            var oncekiDonemTask = GetCihazIlkSonImport(tumIds, oncekiDonemBaslangicZamani, oncekiDonemBitisZamani, cancellationToken);

            await Task.WhenAll(buDonemTask, oncekiDonemTask);

            var buDonem = await buDonemTask;        // cihazId → tuketim_kWh
            var oncekiDonem = await oncekiDonemTask;

            var cihazLookup = tumSorgulananCihazlar.ToDictionary(c => c.Id, c => c);

            // Child → Parent map
            var childToParent = childCihazlar.ToDictionary(c => c.Id, c => c.ParentCihazId);

            // Tüm analizörlerin (child) genel toplamı → pay hesabı için
            decimal genelAnalizorToplami = buDonem
                .Where(kv => childToParent.ContainsKey(kv.Key))
                .Sum(kv => kv.Value);

            // Trafo bazında grupla
            var trafoRaporlari = new List<TrafoRaporModel>();

            foreach (var parent in parentCihazlar)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var parentTuketim = buDonem.TryGetValue(parent.Id, out var pt) ? pt : 0m;

                var altCihazlar = childCihazlar.Where(c => c.ParentCihazId == parent.Id).ToList();

                var analizorSatirlar = new List<AnalizorSatirModel>();

                foreach (var child in altCihazlar)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var buDonemTuketim = buDonem.TryGetValue(child.Id, out var bd) ? bd : 0m;
                    var oncekiTuketim = oncekiDonem.TryGetValue(child.Id, out var od) ? od : 0m;

                    analizorSatirlar.Add(new AnalizorSatirModel
                    {
                        CihazId = child.Id,
                        CihazName = child.Name ?? string.Empty,
                        MakineName = child.MakineName ?? string.Empty,
                        BolumName = child.BolumName ?? string.Empty,
                        Tuketim_kWh = buDonemTuketim,
                        OncekiDonemTuketim_kWh = oncekiTuketim,
                        ToplamTuketimdekiPay = genelAnalizorToplami > 0
                            ? Math.Round(buDonemTuketim / genelAnalizorToplami * 100, 2)
                            : 0,
                        OncekiHaftayaGoreDegisim_kWh = buDonemTuketim - oncekiTuketim,
                        BagliTrafoName = parent.Name ?? string.Empty
                    });
                }

                var analizorToplami = analizorSatirlar.Sum(a => a.Tuketim_kWh);

                trafoRaporlari.Add(new TrafoRaporModel
                {
                    TrafoId = parent.Id,
                    TrafoName = parent.Name ?? string.Empty,
                    SayacDegeri_kWh = parentTuketim,
                    AnalizorlerToplami_kWh = analizorToplami,
                    SayacAnalizorFarki_kWh = parentTuketim - analizorToplami,
                    Analizorler = analizorSatirlar
                });
            }

            // Trafo paylarını hesapla (tüm trafoların sayaç toplamına göre)
            var genelSayacToplami = trafoRaporlari.Sum(t => t.SayacDegeri_kWh);
            foreach (var trafo in trafoRaporlari)
            {
                trafo.ToplamTuketimdekiPay = genelSayacToplami > 0
                    ? Math.Round(trafo.SayacDegeri_kWh / genelSayacToplami * 100, 2)
                    : 0;
            }

            return new EnerjiHaftalikRaporModel
            {
                BaslangicZamani = baslangicZamani,
                BitisZamani = bitisZamani,
                GenelToplamTuketim_kWh = genelSayacToplami,
                Trafolar = trafoRaporlari,
                TumAnalizorler = trafoRaporlari.SelectMany(t => t.Analizorler).ToList()
            };
        }

        public async Task<EnerjiHaftalikRaporModel> GetEnerjiRaporu(List<vwCihaz> tumCihazlar, List<int> secilenParentCihazIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            // Önceki dönem aralığı
            var aralikSuresi = bitisZamani - baslangicZamani;
            var oncekiBaslangic = baslangicZamani - aralikSuresi;
            var oncekiBitis = baslangicZamani;

            // Seçilen parent'lar ve onların child'larını bul
            var parentCihazlar = tumCihazlar
                .Where(c => secilenParentCihazIds.Contains(c.Id))
                .ToList();

            var childCihazlar = tumCihazlar
                .Where(c => c.ParentCihazId != 0 && secilenParentCihazIds.Contains(c.ParentCihazId))
                .ToList();

            var tumSorgulananCihazlar = parentCihazlar
                .Concat(childCihazlar)
                .DistinctBy(c => c.Id)
                .ToList();

            var tumIds = tumSorgulananCihazlar.Select(x => x.Id).ToList();

            // Bu dönem + önceki dönem verilerini paralel çek
            var buDonemTask = GetCihazIlkSonImport(tumIds, baslangicZamani, bitisZamani, cancellationToken);
            var oncekiDonemTask = GetCihazIlkSonImport(tumIds, oncekiBaslangic, oncekiBitis, cancellationToken);

            await Task.WhenAll(buDonemTask, oncekiDonemTask);

            var buDonem = await buDonemTask;        // cihazId → tuketim_kWh
            var oncekiDonem = await oncekiDonemTask;

            var cihazLookup = tumSorgulananCihazlar.ToDictionary(c => c.Id, c => c);

            // Child → Parent map
            var childToParent = childCihazlar.ToDictionary(c => c.Id, c => c.ParentCihazId);

            // Tüm analizörlerin (child) genel toplamı → pay hesabı için
            decimal genelAnalizorToplami = buDonem
                .Where(kv => childToParent.ContainsKey(kv.Key))
                .Sum(kv => kv.Value);

            // Trafo bazında grupla
            var trafoRaporlari = new List<TrafoRaporModel>();

            foreach (var parent in parentCihazlar)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var parentTuketim = buDonem.TryGetValue(parent.Id, out var pt) ? pt : 0m;

                var altCihazlar = childCihazlar.Where(c => c.ParentCihazId == parent.Id).ToList();

                var analizorSatirlar = new List<AnalizorSatirModel>();

                foreach (var child in altCihazlar)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var buDonemTuketim = buDonem.TryGetValue(child.Id, out var bd) ? bd : 0m;
                    var oncekiTuketim = oncekiDonem.TryGetValue(child.Id, out var od) ? od : 0m;

                    analizorSatirlar.Add(new AnalizorSatirModel
                    {
                        CihazId = child.Id,
                        CihazName = child.Name ?? string.Empty,
                        MakineName = child.MakineName ?? string.Empty,
                        BolumName = child.BolumName ?? string.Empty,
                        Tuketim_kWh = buDonemTuketim,
                        OncekiDonemTuketim_kWh = oncekiTuketim,
                        ToplamTuketimdekiPay = genelAnalizorToplami > 0
                            ? Math.Round(buDonemTuketim / genelAnalizorToplami * 100, 2)
                            : 0,
                        OncekiHaftayaGoreDegisim_kWh = buDonemTuketim - oncekiTuketim,
                        BagliTrafoName = parent.Name ?? string.Empty
                    });
                }

                var analizorToplami = analizorSatirlar.Sum(a => a.Tuketim_kWh);

                trafoRaporlari.Add(new TrafoRaporModel
                {
                    TrafoId = parent.Id,
                    TrafoName = parent.Name ?? string.Empty,
                    SayacDegeri_kWh = parentTuketim,
                    AnalizorlerToplami_kWh = analizorToplami,
                    SayacAnalizorFarki_kWh = parentTuketim - analizorToplami,
                    Analizorler = analizorSatirlar
                });
            }

            // Trafo paylarını hesapla (tüm trafoların sayaç toplamına göre)
            var genelSayacToplami = trafoRaporlari.Sum(t => t.SayacDegeri_kWh);
            foreach (var trafo in trafoRaporlari)
            {
                trafo.ToplamTuketimdekiPay = genelSayacToplami > 0
                    ? Math.Round(trafo.SayacDegeri_kWh / genelSayacToplami * 100, 2)
                    : 0;
            }

            return new EnerjiHaftalikRaporModel
            {
                BaslangicZamani = baslangicZamani,
                BitisZamani = bitisZamani,
                GenelToplamTuketim_kWh = genelSayacToplami,
                Trafolar = trafoRaporlari,
                TumAnalizorler = trafoRaporlari.SelectMany(t => t.Analizorler).ToList()
            };
        }

        // Yardımcı metot: verilen cihaz listesi için tarih aralığında ilk/son import farkını döner
        private async Task<Dictionary<int, decimal>> GetCihazIlkSonImport(List<int> cihazIds, DateTime baslangic, DateTime bitis, CancellationToken cancellationToken)
        {
            // 1. Her cihaz için min ve max zamanı bul ($sort yok, çok hızlı)
            var minMaxPipeline = new[]
                                    {
                                new BsonDocument("$match", new BsonDocument {
                                    { "payload.okunan_analizor_id", new BsonDocument("$in", new BsonArray(cihazIds)) },
                                    { "payload.okunma_zamani", new BsonDocument {
                                        { "$gte", baslangic.AddHours(3) },
                                        { "$lte", bitis.AddHours(3) }
                                    }},
                                    { "payload.state", new BsonInt32(1) }
                                }),
                                new BsonDocument("$project", new BsonDocument {
                                    { "cihazId",      "$payload.okunan_analizor_id" },
                                    { "okunmaZamani", "$payload.okunma_zamani" }
                                }),
                                new BsonDocument("$group", new BsonDocument {
                                    { "_id",      "$cihazId" },
                                    { "minZaman", new BsonDocument("$min", "$okunmaZamani") },
                                    { "maxZaman", new BsonDocument("$max", "$okunmaZamani") }
                                })
                            };

            var minMaxSonuclari = await _logs
                .Aggregate<BsonDocument>(minMaxPipeline)
                .ToListAsync(cancellationToken);

            if (!minMaxSonuclari.Any())
                return new Dictionary<int, decimal>();

            // 2. Sadece o min/max zamanlardaki kayıtları çek (çok az döküman)
            var filters = minMaxSonuclari.Select(r => new BsonDocument {
                                { "payload.okunan_analizor_id", r["_id"] },
                                { "payload.okunma_zamani", new BsonDocument("$in", new BsonArray(
                                    new[] { r["minZaman"], r["maxZaman"] }
                                ))},
                                { "payload.state", new BsonInt32(1) }
                            }).ToList();

            var fetchPipeline = new[]
                                    {
                                new BsonDocument("$match", new BsonDocument("$or", new BsonArray(filters))),
                                new BsonDocument("$project", new BsonDocument {
                                    { "cihazId",      "$payload.okunan_analizor_id" },
                                    { "okunmaZamani", "$payload.okunma_zamani" },
                                    { "import",       "$payload.veri.Import_Aktif_Enerji_Wh" }
                                }),
                                new BsonDocument("$sort", new BsonDocument("okunmaZamani", 1)),
                                new BsonDocument("$group", new BsonDocument {
                                    { "_id",       "$cihazId" },
                                    { "IlkImport", new BsonDocument("$first", "$import") },
                                    { "SonImport", new BsonDocument("$last",  "$import") }
                                })
                            };

            var sonuclar = await _logs
                .Aggregate<BsonDocument>(fetchPipeline)
                .ToListAsync(cancellationToken);

            return sonuclar.ToDictionary(
                doc => doc["_id"].ToInt32(),
                doc =>
                {
                    var ilk = doc.GetValue("IlkImport", BsonNull.Value).ToDecimalSafe();
                    var son = doc.GetValue("SonImport", BsonNull.Value).ToDecimalSafe();
                    var fark = (son - ilk) / 1000m;
                    return fark < 0 ? 0m : fark;
                }
            );
        }

        private static readonly Color NavyBlue = Color.FromArgb(0x1a, 0x23, 0x7e);
        private static readonly Color DarkSlate = Color.FromArgb(0x37, 0x47, 0x4f);
        private static readonly Color LightGreen = Color.FromArgb(0xe8, 0xf5, 0xe9);
        private static readonly Color LightBlue = Color.FromArgb(0xe8, 0xea, 0xf6);
        private static readonly Color Row1Bg = Color.FromArgb(0xf5, 0xf5, 0xf5);

        private static readonly string[] PastelColors =
        [
            "#FFB3BA", "#FFDFBA", "#FFFFBA", "#BAFFC9",
        "#BAE1FF", "#D4BAFF", "#FFB3F7", "#B3FFF6"
        ];

        public byte[] TamRaporOlustur(TamRaporRequest data)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();

            Sheet1_AnalizorTablosu(package, data);
            Sheet2_SayacAnalizorKarsilastirma(package, data);
            Sheet3_TrafoAnalizorDagilimi(package, data);
            Sheet4_PastaGrafik(package, data);

            //return package.GetAsByteArray();
            var bytes = package.GetAsByteArray();

            // ZIP içindeki chart XML'ini düzenle
            bytes = DuzeltChartEtiketleri(bytes, data.TumAnalizorler.Count);

            return bytes;
        }

        private byte[] DuzeltChartEtiketleri(byte[] xlsxBytes, int dilimSayisi)
        {
            using var memStream = new MemoryStream();
            memStream.Write(xlsxBytes, 0, xlsxBytes.Length);

            using var zip = new System.IO.Compression.ZipArchive(memStream,
                System.IO.Compression.ZipArchiveMode.Update, leaveOpen: true);

            // Chart dosyasını bul
            var chartEntry = zip.Entries.FirstOrDefault(e =>
                e.FullName.StartsWith("xl/charts/chart") && e.FullName.EndsWith(".xml"));

            if (chartEntry == null) return xlsxBytes;

            string xml;
            using (var reader = new StreamReader(chartEntry.Open()))
                xml = reader.ReadToEnd();

            // Her dilim için ayrı dLbl ekle — küçük dilimleri sağa/sola kaydır
            // Büyük dilimler OutEnd, küçükler bestFit kalır — Excel otomatik yerleştirir
            xml = xml.Replace(
                "<c:dLbls>",
                "<c:dLbls><c:numFmt formatCode=\"0.00%\" sourceLinked=\"0\"/>"
            );

            // dLblPos'u bestFit yap — Excel en iyi pozisyonu seçer
            xml = System.Text.RegularExpressions.Regex.Replace(
                xml,
                @"<c:dLblPos val=""[^""]*""/>",
                "<c:dLblPos val=\"bestFit\"/>"
            );

            // Güncellenmiş XML'i yaz
            var entry = zip.GetEntry(chartEntry.FullName)!;
            entry.Delete();
            var newEntry = zip.CreateEntry(chartEntry.FullName);
            using (var writer = new StreamWriter(newEntry.Open()))
                writer.Write(xml);

            zip.Dispose();
            return memStream.ToArray();
        }

        // ── SHEET 1 ───────────────────────────────────────────────────
        private void Sheet1_AnalizorTablosu(ExcelPackage package, TamRaporRequest data)
        {
            var ws = package.Workbook.Worksheets.Add("Analizör Tablosu");

            // Başlıklar
            var headers = new[] { "ANALİZÖR", "TÜKETİM (kWh)", "PAY (%)", "ÖNCEKİ DÖNEM (kWh)", "TRAFO", "DEĞİŞİM (kWh)" };
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cells[1, c + 1];
                cell.Value = headers[c];
                StyleHeader(cell, NavyBlue);
            }
            ws.Row(1).Height = 22;
            ws.Column(1).Width = 30;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 12;
            ws.Column(4).Width = 20;
            ws.Column(5).Width = 20;
            ws.Column(6).Width = 18;

            // Veri satırları
            for (int i = 0; i < data.TumAnalizorler.Count; i++)
            {
                var a = data.TumAnalizorler[i];
                var row = i + 2;
                var bg = i % 2 == 0 ? Row1Bg : Color.White;

                SetCell(ws, row, 1, a.CihazName, bg);
                SetCell(ws, row, 2, Math.Round(a.TuketimKwh, 2), bg, "#,##0.00");
                SetCell(ws, row, 3, Math.Round(a.ToplamTuketimdekiPay, 2), bg, "#,##0.00");
                SetCell(ws, row, 4, Math.Round(a.OncekiDonemTuketimKwh, 2), bg, "#,##0.00");
                SetCell(ws, row, 5, a.BagliTrafoName, bg);
                SetCell(ws, row, 6, Math.Round(a.OncekiHaftayaGoreDegisimKwh, 2), bg, "#,##0.00");

                // Değişim rengi
                var degisim = a.OncekiHaftayaGoreDegisimKwh;
                ws.Cells[row, 6].Style.Font.Bold = true;
                ws.Cells[row, 6].Style.Font.Color.SetColor(
                    degisim < 0 ? Color.FromArgb(0x2e, 0x7d, 0x32) :
                    degisim > 0 ? Color.FromArgb(0xc6, 0x28, 0x28) :
                                  Color.FromArgb(0x88, 0x88, 0x88));
            }

            // Toplam satırı
            var totalRow = data.TumAnalizorler.Count + 2;
            ws.Cells[totalRow, 1].Value = "TOPLAM";
            ws.Cells[totalRow, 2].Value = Math.Round(data.TumAnalizorler.Sum(a => a.TuketimKwh), 2);
            ws.Cells[totalRow, 4].Value = Math.Round(data.TumAnalizorler.Sum(a => a.OncekiDonemTuketimKwh), 2);
            var totalRange = ws.Cells[totalRow, 1, totalRow, 6];
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            totalRange.Style.Fill.BackgroundColor.SetColor(LightGreen);
            StyleBorder(totalRange);
            ws.Cells[totalRow, 2].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[totalRow, 4].Style.Numberformat.Format = "#,##0.00";
        }

        // ── SHEET 2 ───────────────────────────────────────────────────
        private void Sheet2_SayacAnalizorKarsilastirma(ExcelPackage package, TamRaporRequest data)
        {
            var ws = package.Workbook.Worksheets.Add("Sayaç-Analizör Karşılaştırma");

            // İlk kolon başlığı boş, sonraki kolonlar trafo isimleri
            ws.Cells[1, 1].Value = "";
            StyleHeader(ws.Cells[1, 1], NavyBlue);
            ws.Column(1).Width = 28;

            for (int t = 0; t < data.Trafolar.Count; t++)
            {
                var cell = ws.Cells[1, t + 2];
                cell.Value = data.Trafolar[t].TrafoName;
                StyleHeader(cell, NavyBlue);
                ws.Column(t + 2).Width = 22;
            }
            ws.Row(1).Height = 22;

            // Veri satırları
            for (int i = 0; i < data.TrafoKarsilastirmaData.Count; i++)
            {
                var row = data.TrafoKarsilastirmaData[i];
                var rowIdx = i + 2;
                var bg = i % 2 == 0 ? Row1Bg : Color.White;

                // baslik kolonu
                var baslikCell = ws.Cells[rowIdx, 1];
                baslikCell.Value = row.ContainsKey("baslik") ? row["baslik"].GetString() ?? "" : "";
                baslikCell.Style.Font.Bold = true;
                baslikCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                baslikCell.Style.Fill.BackgroundColor.SetColor(bg);
                StyleBorder(baslikCell);

                // Trafo kolonları
                for (int t = 0; t < data.Trafolar.Count; t++)
                {
                    var key = data.Trafolar[t].TrafoName;
                    var cell = ws.Cells[rowIdx, t + 2];
                    if (row.ContainsKey(key))
                    {
                        var element = row[key];
                        if (element.ValueKind == JsonValueKind.Number)
                        {
                            cell.Value = element.GetDouble();
                            cell.Style.Numberformat.Format = "#,##0.00";
                        }
                        else
                        {
                            cell.Value = element.GetString();
                        }
                    }
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(bg);
                    StyleBorder(cell);
                }
            }
        }

        // ── SHEET 3 ───────────────────────────────────────────────────
        private void Sheet3_TrafoAnalizorDagilimi(ExcelPackage package, TamRaporRequest data)
        {
            var ws = package.Workbook.Worksheets.Add("Trafo Analizör Dağılımı");
            int currentCol = 1;

            foreach (var trafo in data.Trafolar)
            {
                // Trafo başlığı (merge 3 kolon)
                var titleCell = ws.Cells[1, currentCol];
                titleCell.Value = $"{trafo.TrafoName} — Analizör Dağılımı";
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.Size = 12;
                titleCell.Style.Font.Color.SetColor(NavyBlue);
                titleCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                titleCell.Style.Fill.BackgroundColor.SetColor(LightBlue);
                StyleBorder(titleCell);
                ws.Cells[1, currentCol, 1, currentCol + 2].Merge = true;

                // Alt başlık
                var subCell = ws.Cells[2, currentCol];
                var sayac = trafo.SayacDegeriKwh?.ToString("0.00") ?? "0.00";
                var fark = trafo.SayacAnalizorFarkiKwh?.ToString("0.00") ?? "0.00";
                subCell.Value = $"Sayaç: {sayac} kWh  |  Fark: {fark} kWh";
                subCell.Style.Font.Italic = true;
                subCell.Style.Font.Size = 10;
                subCell.Style.Font.Color.SetColor(Color.FromArgb(0x88, 0x88, 0x88));
                ws.Cells[2, currentCol, 2, currentCol + 2].Merge = true;

                // Kolon başlıkları
                var colHeaders = new[] { "ANALİZÖR", "TÜKETİM (kWh)", "PAY (%)" };
                for (int h = 0; h < colHeaders.Length; h++)
                {
                    var cell = ws.Cells[3, currentCol + h];
                    cell.Value = colHeaders[h];
                    StyleHeader(cell, DarkSlate);
                    ws.Column(currentCol + h).Width = h == 0 ? 28 : 18;
                }

                // Analizör satırları
                for (int ai = 0; ai < trafo.Analizorler.Count; ai++)
                {
                    var analizor = trafo.Analizorler[ai];
                    var rowIdx = 4 + ai;
                    var pastel = ColorTranslator.FromHtml(PastelColors[ai % PastelColors.Length]);

                    var nameCell = ws.Cells[rowIdx, currentCol];
                    nameCell.Value = analizor.CihazName;
                    nameCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    nameCell.Style.Fill.BackgroundColor.SetColor(pastel);
                    nameCell.Style.Font.Size = 10;
                    StyleBorder(nameCell);

                    var kwhCell = ws.Cells[rowIdx, currentCol + 1];
                    kwhCell.Value = Math.Round(analizor.TuketimKwh, 2);
                    kwhCell.Style.Numberformat.Format = "#,##0.00";
                    kwhCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    kwhCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    kwhCell.Style.Fill.BackgroundColor.SetColor(Color.White);
                    StyleBorder(kwhCell);

                    var payCell = ws.Cells[rowIdx, currentCol + 2];
                    payCell.Value = Math.Round(analizor.ToplamTuketimdekiPay, 2);
                    payCell.Style.Numberformat.Format = "#,##0.00\"%\"";
                    payCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    payCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    payCell.Style.Fill.BackgroundColor.SetColor(Color.White);
                    StyleBorder(payCell);
                }

                currentCol += 4;
            }
        }

        // ── SHEET 4 ───────────────────────────────────────────────────
        //private void Sheet4_PastaGrafik(ExcelPackage package, TamRaporRequest data)
        //{
        //    var ws = package.Workbook.Worksheets.Add("Analizör Pasta Grafik");

        //    // Başlıklar
        //    ws.Cells[1, 1].Value = "ANALİZÖR";
        //    ws.Cells[1, 2].Value = "TÜKETİM (kWh)";
        //    StyleHeader(ws.Cells[1, 1], NavyBlue);
        //    StyleHeader(ws.Cells[1, 2], NavyBlue);
        //    ws.Column(1).Width = 30;
        //    ws.Column(2).Width = 20;

        //    // Veri
        //    for (int i = 0; i < data.TumAnalizorler.Count; i++)
        //    {
        //        var a = data.TumAnalizorler[i];
        //        ws.Cells[i + 2, 1].Value = a.CihazName;
        //        ws.Cells[i + 2, 2].Value = Math.Round(a.TuketimKwh, 2);
        //        ws.Cells[i + 2, 2].Style.Numberformat.Format = "#,##0.00";
        //    }

        //    // Pasta grafik
        //    var lastRow = data.TumAnalizorler.Count + 1;
        //    var chart = ws.Drawings.AddChart("PastaGrafik", eChartType.PieExploded) as ExcelPieChart;
        //    chart!.Title.Text = "Analizör Tüketim Dağılımı";

        //    var series = chart.Series.Add(
        //        ws.Cells[2, 2, lastRow, 2],
        //        ws.Cells[2, 1, lastRow, 1]
        //    );
        //    series.DataLabel.ShowPercent = true;
        //    series.DataLabel.ShowCategory = true;
        //    series.DataLabel.Position = eLabelPosition.OutEnd;

        //    chart.Legend.Position = eLegendPosition.Bottom;
        //    chart.SetPosition(1, 0, 3, 0);  // D2'den başla
        //    chart.SetSize(1500, 1500);
        //}

        private void Sheet4_PastaGrafik(ExcelPackage package, TamRaporRequest data)
        {
            var ws = package.Workbook.Worksheets.Add("Analizör Pasta Grafik");

            // Başlıklar
            ws.Cells[1, 1].Value = "ANALİZÖR";
            ws.Cells[1, 2].Value = "TÜKETİM (kWh)";
            StyleHeader(ws.Cells[1, 1], NavyBlue);
            StyleHeader(ws.Cells[1, 2], NavyBlue);
            ws.Column(1).Width = 30;
            ws.Column(2).Width = 20;

            // Sadece 0'dan büyük verileri yaz
            var filtrelenmisAnalizorler = data.TumAnalizorler
                .Where(a => a.TuketimKwh > 0)
                .ToList();

            for (int i = 0; i < filtrelenmisAnalizorler.Count; i++)
            {
                var a = filtrelenmisAnalizorler[i];
                ws.Cells[i + 2, 1].Value = a.CihazName;
                ws.Cells[i + 2, 2].Value = Math.Round(a.TuketimKwh, 2);
                ws.Cells[i + 2, 2].Style.Numberformat.Format = "#,##0.00";
            }

            // Pasta grafik — artık 0 değerli satır yok
            var lastRow = filtrelenmisAnalizorler.Count + 1;
            var chart = ws.Drawings.AddChart("PastaGrafik", eChartType.PieExploded) as ExcelPieChart;
            chart!.Title.Text = "Analizör Tüketim Dağılımı";
            var series = chart.Series.Add(
                ws.Cells[2, 2, lastRow, 2],
                ws.Cells[2, 1, lastRow, 1]
            );
            series.DataLabel.ShowPercent = true;
            series.DataLabel.ShowCategory = true;
            series.DataLabel.Position = eLabelPosition.OutEnd;
            chart.Legend.Position = eLegendPosition.Bottom;
            chart.SetPosition(1, 0, 3, 0);
            chart.SetSize(1500, 1500);
        }

        // ── Yardımcı metodlar ─────────────────────────────────────────
        private static void StyleHeader(ExcelRange cell, Color bgColor)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Font.Size = 11;
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(bgColor);
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            StyleBorder(cell);
        }

        private static void SetCell(ExcelWorksheet ws, int row, int col, object value, Color bg, string? format = null)
        {
            var cell = ws.Cells[row, col];
            cell.Value = value;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(bg);
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            if (format != null)
                cell.Style.Numberformat.Format = format;
            StyleBorder(cell);
        }

        private static void StyleBorder(ExcelRange cell)
        {
            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }
    }
}
