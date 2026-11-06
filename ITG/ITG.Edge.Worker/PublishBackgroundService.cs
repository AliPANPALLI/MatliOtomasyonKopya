
using ITG.Edge.Application;
using Microsoft.Extensions.Options;

namespace ITG.Edge.Worker;

public sealed class PublishBackgroundService(
    IOutbox outbox,
    IMessagePublisher publisher,
    IOptions<LocationOptions> location,
    ILogger<PublishBackgroundService> logger
) : BackgroundService
{
    private const int RetentionDays = 7;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Outbox, Program.cs icinde host baslamadan once initialize edilir.

        // İlk temizlik başlangıçta yapılır.
        var nextCleanup = DateTimeOffset.UtcNow;

        while (!ct.IsCancellationRequested)
        {
            PendingMessage? next = null;

            try
            {
                // Temizlik her 24 saatte bir kontrol edilir.
                if (DateTimeOffset.UtcNow >= nextCleanup)
                {
                    try
                    {
                        int deleted = await outbox.CleanupPublishedAsync(
                            RetentionDays, ct);

                        logger.LogInformation(
                            "ITG OUTBOX CLEANUP | RetentionDays={Days} | Deleted={Deleted}",
                            RetentionDays, deleted);

                        nextCleanup = DateTimeOffset.UtcNow.AddHours(24);
                    }
                    catch (OperationCanceledException)
                        when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception cleanupEx)
                    {
                        logger.LogWarning(
                            cleanupEx,
                            "ITG OUTBOX CLEANUP FAILED");

                        // Temizlik başarısız olursa 1 saat sonra tekrar dene.
                        nextCleanup = DateTimeOffset.UtcNow.AddHours(1);
                    }
                }

                next = await outbox.ClaimAsync(ct);

                if (next is null)
                {
                    await Task.Delay(1000, ct);
                    continue;
                }

                await publisher.PublishAsync(
                    next,
                    location.Value.LocationId,
                    ct);

                await outbox.MarkPublishedAsync(next.Id, ct);
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (InvalidOutboxMessageException ex) when (next is not null)
            {
                // Kalici hata: tekrar denemek sonuc degistirmez, mesaj Failed'a cekilir.
                logger.LogError(
                    ex,
                    "Outbox message permanently failed message={Id}",
                    next.Id);

                try
                {
                    await outbox.MarkFailedAsync(next.Id, ex.Message, ct);
                }
                catch (OperationCanceledException)
                    when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception databaseEx)
                {
                    logger.LogError(
                        databaseEx,
                        "Outbox mark failed failed");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Publish failed message={Id}",
                    next?.Id);

                if (next != null)
                {
                    try
                    {
                        await outbox.RescheduleAsync(
                            next.Id,
                            next.RetryCount + 1,
                            ex.Message,
                            ct);
                    }
                    catch (Exception databaseEx)
                    {
                        logger.LogError(
                            databaseEx,
                            "Outbox reschedule failed");
                    }
                }

                try
                {
                    await Task.Delay(2000, ct);
                }
                catch (OperationCanceledException)
                    when (ct.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
