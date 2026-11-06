
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using ITG.Edge.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ITG.Edge.Infrastructure.RabbitMq;

public sealed class RabbitMqPublisher(
    IOptions<RabbitMqOptions> settings,
    ILogger<RabbitMqPublisher> logger) : IMessagePublisher
{
    public async Task PublishAsync(
        PendingMessage message,
        string locationId,
        CancellationToken ct)
    {
        var opt = settings.Value;

        // 1. Outbox JSON'unun lokasyonu dogru mu?
        string? payloadLocation;
        try
        {
            using var json = JsonDocument.Parse(message.Payload);

            payloadLocation =
                json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("locationId", out JsonElement location) &&
                location.ValueKind == JsonValueKind.String
                    ? location.GetString()
                    : null;
        }
        catch (JsonException ex)
        {
            throw new InvalidOutboxMessageException(
                "Outbox payload gecerli bir JSON degil.", ex);
        }

        if (!string.Equals(
            payloadLocation,
            locationId,
            StringComparison.Ordinal))
        {
            throw new InvalidOutboxMessageException(
                $"Outbox lokasyon uyusmazligi. " +
                $"Payload={payloadLocation}, Config={locationId}");
        }

        // 2. RabbitMQ baglanti ayarlari
        if (string.IsNullOrWhiteSpace(opt.Host) ||
            string.IsNullOrWhiteSpace(opt.UserName) ||
            string.IsNullOrWhiteSpace(opt.Password))
        {
            throw new InvalidOperationException(
                "RabbitMQ baglanti ayarlari eksik.");
        }

        if (!File.Exists(opt.CaCertificatePath))
        {
            throw new FileNotFoundException(
                "RabbitMQ CA sertifikasi bulunamadi.",
                opt.CaCertificatePath);
        }

        using var trustedCa =
            X509CertificateLoader.LoadCertificateFromFile(
                opt.CaCertificatePath);

        string serverName = string.IsNullOrWhiteSpace(opt.ServerName)
            ? opt.Host
            : opt.ServerName;

        // 3. TLS baglantisi
        var factory = new ConnectionFactory
        {
            HostName = opt.Host,
            Port = opt.Port,
            VirtualHost = opt.VirtualHost,
            UserName = opt.UserName,
            Password = opt.Password,

            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(30),

            Ssl = new SslOption
            {
                Enabled = true,
                ServerName = serverName,
                Version = SslProtocols.Tls12 | SslProtocols.Tls13,

                CertificateValidationCallback =
                    (_, certificate, presentedChain, errors) =>
                    {
                        if (certificate is null)
                            return false;

                        if ((errors &
                            SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                            return false;

                        if ((errors &
                            SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
                            return false;

                        using var serverCert =
                            X509CertificateLoader.LoadCertificate(
                                certificate.GetRawCertData());

                        using var chain = new X509Chain();

                        chain.ChainPolicy.TrustMode =
                            X509ChainTrustMode.CustomRootTrust;

                        chain.ChainPolicy.CustomTrustStore.Add(trustedCa);

                        // Sunucunun gonderdigi ara sertifikalar zincir kurulumunda kullanilir;
                        // guven yalnizca CustomTrustStore'daki CA'dan gelir.
                        if (presentedChain is not null)
                        {
                            foreach (X509ChainElement element in presentedChain.ChainElements)
                                chain.ChainPolicy.ExtraStore.Add(element.Certificate);
                        }

                        chain.ChainPolicy.RevocationMode =
                            X509RevocationMode.NoCheck;

                        chain.ChainPolicy.VerificationFlags =
                            X509VerificationFlags.NoFlag;

                        return chain.Build(serverCert);
                    }
            }
        };

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(ct);

        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        await using var connection =
            await factory.CreateConnectionAsync(timeout.Token);

        await using var channel =
            await connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true),
                timeout.Token);

        // 4. OSB-YEM routing key
        string routingKey =
            $"{opt.RoutingKeyPrefix}.{locationId}";

        int returned = 0;

        channel.BasicReturnAsync += (_, args) =>
        {
            Interlocked.Exchange(ref returned, 1);

            logger.LogError(
                "RabbitMQ mesaji yonlendirilemedi. RoutingKey={RoutingKey}",
                args.RoutingKey);

            return Task.CompletedTask;
        };

        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = message.Id,
            ContentType = "application/json",
            Type = "itg.temperature.telemetry"
        };

        byte[] body = Encoding.UTF8.GetBytes(message.Payload);

        // 5. Mesaji gonder ve broker onayini bekle
        await channel.BasicPublishAsync(
            exchange: opt.Exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: timeout.Token);

        if (Volatile.Read(ref returned) != 0)
        {
            throw new InvalidOperationException(
                $"RabbitMQ mesaji kuyruga yonlendirilemedi: {routingKey}");
        }

        logger.LogInformation(
            "RabbitMQ confirmed. MessageId={MessageId}, RoutingKey={RoutingKey}",
            message.Id,
            routingKey);
    }
}
