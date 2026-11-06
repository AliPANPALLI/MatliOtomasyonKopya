namespace ItgTemperatureIngestionWorker;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "itg";
    public string UserName { get; set; } = "itg";
    public string Password { get; set; } = "itg";

    /// <summary>
    /// Kuyruk, DLQ'su ile birlikte RabbitMQ tarafında (itg vhost'u, quorum tip,
    /// x-dead-letter-exchange = itg-ingestion-dlq) zaten tanımlı. Worker bu kuyruğu
    /// yeniden declare etmeye çalışmaz; sadece var olan kuyruğa abone olur.
    /// </summary>
    public string QueueName { get; set; } = "itg.ingestion.q";
}
