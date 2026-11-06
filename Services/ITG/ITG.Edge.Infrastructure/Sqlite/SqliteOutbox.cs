using System.Text.Json;
using ITG.Edge.Application;
using ITG.Edge.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace ITG.Edge.Infrastructure.Sqlite;

public sealed class SqliteOutbox(IOptions<OutboxOptions> options) : IOutbox
{
    private string ConnectionString
    {
        get
        {
            var dbPath = Path.GetFullPath(options.Value.DatabasePath);
            return new SqliteConnectionStringBuilder { DataSource = dbPath, Pooling = false }.ToString();
        }
    }
    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var db = new SqliteConnection(ConnectionString);
        await db.OpenAsync(ct);
        await using var pragma = db.CreateCommand();
        pragma.CommandText = "PRAGMA busy_timeout=5000; PRAGMA synchronous=FULL;";
        await pragma.ExecuteNonQueryAsync(ct);
        return db;
    }
    public async Task InitializeAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.Value.DatabasePath))!);
        await using var db = await OpenAsync(ct);
        await using var cmd = db.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS outgoing_messages (
                id TEXT PRIMARY KEY, payload_json TEXT NOT NULL, measured_at TEXT NOT NULL,
                status TEXT NOT NULL, retry_count INTEGER NOT NULL DEFAULT 0,
                next_retry_at TEXT NOT NULL, locked_until TEXT,
                created_at TEXT NOT NULL, published_at TEXT, last_error TEXT
            );
            CREATE INDEX IF NOT EXISTS ix_outbox_available ON outgoing_messages(status,next_retry_at);
            """;
        await cmd.ExecuteNonQueryAsync(ct);
        await using var recovery = db.CreateCommand();
        // Startup recovery. A new process cannot own a previous process's InFlight lease.
        recovery.CommandText = "UPDATE outgoing_messages SET status='Pending',locked_until=NULL WHERE status='InFlight'";
        await recovery.ExecuteNonQueryAsync(ct);
    }
    public async Task SaveAsync(IReadOnlyCollection<TemperatureBatchMessage> messages, CancellationToken ct)
    {
        if (messages.Count == 0)
            return;
        await using var db = await OpenAsync(ct);
        await using var tx = (SqliteTransaction)await db.BeginTransactionAsync(ct);
        foreach (var message in messages)
        {
            await using var cmd = db.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT OR IGNORE INTO outgoing_messages
                (id,payload_json,measured_at,status,retry_count,next_retry_at,created_at)
                VALUES ($id,$payload,$time,'Pending',0,$now,$now)
                """;
            cmd.Parameters.AddWithValue("$id", message.MessageId.ToString());
            cmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(message, JsonOptions));
            cmd.Parameters.AddWithValue("$time", message.MeasuredAt.ToString("O"));
            cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
    public async Task<PendingMessage?> ClaimAsync(CancellationToken ct)
    {
        await using var db = await OpenAsync(ct);
        await using var tx = (SqliteTransaction)await db.BeginTransactionAsync(ct);
        var now = DateTimeOffset.UtcNow.ToString("O");
        await using var command = db.CreateCommand(); command.Transaction = tx;
        command.CommandText = """
            SELECT id,payload_json,retry_count FROM outgoing_messages
            WHERE (status='Pending' AND next_retry_at <= $now)
               OR (status='InFlight' AND locked_until <= $now)
            ORDER BY created_at LIMIT 1
            """;
        command.Parameters.AddWithValue("$now", now);
        string? id = null, payload = null; int retries = 0;
        await using (var result = await command.ExecuteReaderAsync(ct))
        {
            if (await result.ReadAsync(ct)) { id = result.GetString(0); payload = result.GetString(1); retries = result.GetInt32(2); }
        }
        if (id is null)
        {
            await tx.CommitAsync(ct); return null;
        }
        await using var mark = db.CreateCommand(); mark.Transaction = tx;
        mark.CommandText = "UPDATE outgoing_messages SET status='InFlight',locked_until=$until WHERE id=$id";
        mark.Parameters.AddWithValue("$id", id);
        mark.Parameters.AddWithValue("$until", DateTimeOffset.UtcNow.AddMinutes(2).ToString("O"));
        await mark.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        return new PendingMessage(id, payload!, retries);
    }
    public async Task MarkPublishedAsync(string id, CancellationToken ct)
    {
        await using var db = await OpenAsync(ct); await using var cmd = db.CreateCommand();
        cmd.CommandText = "UPDATE outgoing_messages SET status='Published',locked_until=NULL,published_at=$now,last_error=NULL WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id); cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task MarkFailedAsync(string id, string error, CancellationToken ct)
    {
        // Failed kayitlar ClaimAsync ve CleanupPublishedAsync tarafindan secilmez; inceleme icin saklanir.
        await using var db = await OpenAsync(ct); await using var cmd = db.CreateCommand();
        cmd.CommandText = "UPDATE outgoing_messages SET status='Failed',locked_until=NULL,last_error=$error WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$error", error.Length > 500 ? error[..500] : error);
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task RescheduleAsync(string id, int retryCount, string error, CancellationToken ct)
    {
        int delay = Math.Min(300, (int)Math.Pow(2, Math.Min(8, retryCount)));
        await using var db = await OpenAsync(ct); await using var cmd = db.CreateCommand();
        cmd.CommandText = """
            UPDATE outgoing_messages SET status='Pending',locked_until=NULL,retry_count=$retry,
            next_retry_at=$next,last_error=$error WHERE id=$id
            """;
        cmd.Parameters.AddWithValue("$id", id); cmd.Parameters.AddWithValue("$retry", retryCount);
        cmd.Parameters.AddWithValue("$next", DateTimeOffset.UtcNow.AddSeconds(delay).ToString("O"));
        cmd.Parameters.AddWithValue("$error", error.Length > 500 ? error[..500] : error);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> CleanupPublishedAsync(
        int retentionDays,
        CancellationToken ct)
    {
        if (retentionDays < 1)
            throw new ArgumentOutOfRangeException(nameof(retentionDays));

        var cutoff = DateTimeOffset.UtcNow
            .AddDays(-retentionDays)
            .ToString("O");

        await using var db = await OpenAsync(ct);
        await using var cmd = db.CreateCommand();

        cmd.CommandText = """
        DELETE FROM outgoing_messages
        WHERE status = 'Published'
          AND published_at IS NOT NULL
          AND published_at < $cutoff;
        """;

        cmd.Parameters.AddWithValue("$cutoff", cutoff);

        return await cmd.ExecuteNonQueryAsync(ct);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
