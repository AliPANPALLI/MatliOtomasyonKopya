using Microsoft.Data.SqlClient;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models.ReportModels;
using System.Data;

namespace SharedApp.Infrastructure.Repositories
{
    public class ProductionReportRepository : IProductionReportRepository
    {
        private static readonly Dictionary<string, string> LokasyonEnvVarMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["turgutlu"] = "ProductionDb_Turgutlu",
            ["samsun"] = "ProductionDb_Samsun",
            ["burdur"] = "ProductionDb_Burdur",
        };

        private const string Sql = @"SELECT
    LotNo,
    MaterialDocumentID,
    ProductionID,
    BatchOrderID,
    FormulaName,
    OrderNo,
    BatchNo,
    MaterialCode,
    MaterialName,
    MaterialGroup,
    ContainerCode,
    BeginDate,
    EndDate,
    Duration,
    PlanAmount,
    RealAmount,
    Diff,
    Perc,
    HammBeginDate,
    HammStartDate,
    HammCompleteDate,
    HammEndDate,
    HammDuration,
    MixBeginDate,
    MixStartDate,
    MixCompleteDate,
    MixEndDate,
    MixDuration,
    MixGateDuration,
    MixDurationNoGate,
    MolassesDuration,
    MolassesDurationSet,
    OutOfTolerance,
    OutOfTolerance20,
    MlsDuration,
    MelasFault,
    MelasFaultRatio,
    VinasFault,
    VinasFaultRatio,
    MixAltiRuntime,
    MixRuntime,
    SieveActive,
    SievePassive,
    SieveStatePercentage,
    UniteID,
    PelletOrderID
FROM VwProductionReport
WHERE
    BeginDate >= @BaslangicZamani
    AND EndDate <= @BitisZamani
ORDER BY BeginDate;";

        // Samsun'daki VwProductionReport, Turgutlu/Burdur'dan farklı bir kolon setine sahip:
        // MlsDuration, MelasFault(Ratio), VinasFault(Ratio), MixAltiRuntime, PelletOrderID yok.
        private const string SamsunSql = @"SELECT
    LotNo,
    MaterialDocumentID,
    ProductionID,
    BatchOrderID,
    UniteID,
    FormulaName,
    OrderNo,
    BatchNo,
    MaterialCode,
    MaterialName,
    MaterialGroup,
    ContainerCode,
    BeginDate,
    EndDate,
    Duration,
    PlanAmount,
    RealAmount,
    Diff,
    Perc,
    HammBeginDate,
    HammStartDate,
    HammCompleteDate,
    HammEndDate,
    HammDuration,
    MixBeginDate,
    MixStartDate,
    MixCompleteDate,
    MixEndDate,
    MixDuration,
    MixActionDuration,
    MixGateDuration,
    MixDurationNoGate,
    MolassesDuration,
    MolassesDurationSet,
    OutOfTolerance,
    OutOfTolerance20,
    SieveActive,
    SievePassive,
    SieveStatePercentage,
    MixRuntime
FROM VwProductionReport
WHERE
    BeginDate >= @BaslangicZamani
    AND EndDate <= @BitisZamani
ORDER BY BeginDate;";

        private static readonly Dictionary<string, string> LokasyonSqlMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["turgutlu"] = Sql,
            ["samsun"] = SamsunSql,
            ["burdur"] = Sql,
        };

        public async Task<List<ProductionReport>> GetProductionReportAsync(string lokasyon, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(lokasyon) || !LokasyonEnvVarMap.TryGetValue(lokasyon.Trim(), out var envVarName))
            {
                throw new ArgumentException($"Geçersiz lokasyon: '{lokasyon}'. Beklenen değerler: {string.Join(", ", LokasyonEnvVarMap.Keys)}");
            }

            var sql = LokasyonSqlMap[lokasyon.Trim()];

            var connectionString = Environment.GetEnvironmentVariable(envVarName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"'{envVarName}' sistem değişkeni tanımlı değil.");
            }

            var result = new List<ProductionReport>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@BaslangicZamani", SqlDbType.DateTime2).Value = baslangicZamani;
            command.Parameters.Add("@BitisZamani", SqlDbType.DateTime2).Value = bitisZamani;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new ProductionReport
                {
                    LotNo = ReadString(reader, "LotNo"),
                    MaterialDocumentID = ReadInt(reader, "MaterialDocumentID"),
                    ProductionID = ReadInt(reader, "ProductionID"),
                    BatchOrderID = ReadInt(reader, "BatchOrderID"),
                    FormulaName = ReadString(reader, "FormulaName"),
                    OrderNo = ReadString(reader, "OrderNo"),
                    BatchNo = ReadInt(reader, "BatchNo"),
                    MaterialCode = ReadString(reader, "MaterialCode"),
                    MaterialName = ReadString(reader, "MaterialName"),
                    MaterialGroup = ReadString(reader, "MaterialGroup"),
                    ContainerCode = ReadString(reader, "ContainerCode"),
                    BeginDate = ReadDateTime(reader, "BeginDate"),
                    EndDate = ReadDateTime(reader, "EndDate"),
                    Duration = ReadInt(reader, "Duration"),
                    PlanAmount = ReadDecimal(reader, "PlanAmount"),
                    RealAmount = ReadDecimal(reader, "RealAmount"),
                    Diff = ReadDecimal(reader, "Diff"),
                    Perc = ReadDecimal(reader, "Perc"),
                    HammBeginDate = ReadDateTime(reader, "HammBeginDate"),
                    HammStartDate = ReadDateTime(reader, "HammStartDate"),
                    HammCompleteDate = ReadDateTime(reader, "HammCompleteDate"),
                    HammEndDate = ReadDateTime(reader, "HammEndDate"),
                    HammDuration = ReadInt(reader, "HammDuration"),
                    MixBeginDate = ReadDateTime(reader, "MixBeginDate"),
                    MixStartDate = ReadDateTime(reader, "MixStartDate"),
                    MixCompleteDate = ReadDateTime(reader, "MixCompleteDate"),
                    MixEndDate = ReadDateTime(reader, "MixEndDate"),
                    MixDuration = ReadInt(reader, "MixDuration"),
                    MixActionDuration = ReadInt(reader, "MixActionDuration"),
                    MixGateDuration = ReadInt(reader, "MixGateDuration"),
                    MixDurationNoGate = ReadInt(reader, "MixDurationNoGate"),
                    MolassesDuration = ReadDecimal(reader, "MolassesDuration"),
                    MolassesDurationSet = ReadDecimal(reader, "MolassesDurationSet"),
                    OutOfTolerance = ReadInt(reader, "OutOfTolerance"),
                    OutOfTolerance20 = ReadInt(reader, "OutOfTolerance20"),
                    MlsDuration = ReadInt(reader, "MlsDuration"),
                    MelasFault = ReadDecimal(reader, "MelasFault"),
                    MelasFaultRatio = ReadDecimal(reader, "MelasFaultRatio"),
                    VinasFault = ReadDecimal(reader, "VinasFault"),
                    VinasFaultRatio = ReadDecimal(reader, "VinasFaultRatio"),
                    MixAltiRuntime = ReadDecimal(reader, "MixAltiRuntime"),
                    MixRuntime = ReadDecimal(reader, "MixRuntime"),
                    SieveActive = ReadInt(reader, "SieveActive"),
                    SievePassive = ReadInt(reader, "SievePassive"),
                    SieveStatePercentage = ReadDecimal(reader, "SieveStatePercentage"),
                    UniteID = ReadInt(reader, "UniteID"),
                    PelletOrderID = ReadInt(reader, "PelletOrderID"),
                });
            }

            return result;
        }

        private static bool TryGetOrdinal(SqlDataReader reader, string column, out int ordinal)
        {
            try
            {
                ordinal = reader.GetOrdinal(column);
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                // Lokasyona göre VwProductionReport'ta bulunmayan kolonlar olabiliyor
                // (örn. Samsun'da MlsDuration, MelasFault, PelletOrderID vb. yok).
                ordinal = -1;
                return false;
            }
        }

        private static string? ReadString(SqlDataReader reader, string column)
        {
            if (!TryGetOrdinal(reader, column, out var i)) return null;
            return reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i));
        }

        private static int? ReadInt(SqlDataReader reader, string column)
        {
            if (!TryGetOrdinal(reader, column, out var i)) return null;
            return reader.IsDBNull(i) ? null : Convert.ToInt32(reader.GetValue(i));
        }

        private static decimal? ReadDecimal(SqlDataReader reader, string column)
        {
            if (!TryGetOrdinal(reader, column, out var i)) return null;
            return reader.IsDBNull(i) ? null : Convert.ToDecimal(reader.GetValue(i));
        }

        private static DateTime? ReadDateTime(SqlDataReader reader, string column)
        {
            if (!TryGetOrdinal(reader, column, out var i)) return null;
            return reader.IsDBNull(i) ? null : Convert.ToDateTime(reader.GetValue(i));
        }
    }
}
