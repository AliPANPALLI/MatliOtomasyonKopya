using System.Net.Http;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using WeighbridgeTracking.Application.DTOs;
using WeighbridgeTracking.Application.Exceptions;
using WeighbridgeTracking.Application.Interfaces;
using WeighbridgeTracking.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace WeighbridgeTracking.Infrastructure.Integrations.SAP
{
    public class SapKantarService : ISapKantarService
    {
        private const string SoapAction =
            "urn:sap-com:document:sap:rfc:functions:ZWS_F0017_WS:ZWS_F0017Request";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        public SapKantarService(
            HttpClient httpClient,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<List<SapKantarDto>> GetByDateAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            if (endDate < startDate)
                throw new GecersizIstekException(
                    "Bitiş tarihi başlangıç tarihinden önce olamaz.");

            if (endDate.DayNumber - startDate.DayNumber > 6)
                throw new GecersizIstekException(
                    "Tek sorguda en fazla 7 günlük veri alınabilir.");

            var cacheKey = $"sap:kantar:date:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";

            var result = await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(30);

                    return await QuerySapAsync(
                        null,
                        startDate,
                        endDate,
                        cancellationToken);
                });

            return result ?? new List<SapKantarDto>();
        }

        public async Task<List<SapKantarDto>> GetByFisNoAsync(
            string fisNo,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fisNo))
                throw new GecersizIstekException("Fiş numarası zorunludur.");

            fisNo = fisNo.Trim();

            if (fisNo.Length > 20 || !fisNo.All(char.IsDigit))
                throw new GecersizIstekException(
                    "Fiş numarası yalnızca rakamlardan oluşmalıdır.");

            var cacheKey = $"sap:kantar:fis:{fisNo}";

            var result = await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(30);

                    return await QuerySapAsync(
                        fisNo,
                        null,
                        null,
                        cancellationToken);
                });

            return result ?? new List<SapKantarDto>();
        }

        private async Task<List<SapKantarDto>> QuerySapAsync(
            string? fisNo,
            DateOnly? startDate,
            DateOnly? endDate,
            CancellationToken cancellationToken)
        {
            var endpoint = _configuration["Sap:Endpoint"];
            var username = _configuration["Sap:Username"];
            var password = _configuration["Sap:Password"];

            if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new SapEntegrasyonException(
                    "SAP bağlantı ayarları eksik.");
            }

            var soapXml = BuildSoapRequest(
                fisNo, startDate, endDate);

            using var request = new HttpRequestMessage(
                HttpMethod.Post, endpoint);

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{username}:{password}"));

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            request.Headers.TryAddWithoutValidation(
                "SOAPAction", $"\"{SoapAction}\"");

            request.Content = new StringContent(
                soapXml, Encoding.UTF8, "text/xml");

            HttpResponseMessage response;
            string responseXml;

            try
            {
                response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                responseXml = await response.Content
                    .ReadAsStringAsync(cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new SapEntegrasyonException(
                    "SAP servisine ulaşılamadı.", ex);
            }
            catch (TaskCanceledException ex)
                when (!cancellationToken.IsCancellationRequested)
            {
                throw new SapEntegrasyonException(
                    "SAP servisi zaman aşımına uğradı.", ex);
            }

            using var responseScope = response;

            // SOAP Fault varsa HTTP 500 durumunda bile
            // XML içindeki hata bilgisi okunabilsin.
            XDocument document;

            try
            {
                document = XDocument.Parse(responseXml);
            }
            catch (System.Xml.XmlException)
            {
                throw new SapEntegrasyonException(
                    response.IsSuccessStatusCode
                        ? "SAP geçerli bir SOAP XML yanıtı döndürmedi."
                        : $"SAP HTTP {(int)response.StatusCode} döndürdü.");
            }

            var fault = document.Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "Fault");

            if (fault != null)
            {
                var message = GetValue(fault, "faultstring");

                throw new SapEntegrasyonException(
                    $"SAP SOAP hatası: {message ?? "Bilinmeyen hata"}");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new SapEntegrasyonException(
                    $"SAP HTTP {(int)response.StatusCode} döndürdü.");
            }

            var returnType = document.Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "E_RETURN_TYPE")
                ?.Value;

            var returnMessage = document.Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "E_RETURN_MESSAGE")
                ?.Value;

            if (returnType == "E" || returnType == "A")
            {
                throw new SapEntegrasyonException(
                    $"SAP işlem hatası: {returnMessage}");
            }

            var table = document.Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "T_KANTAR");

            if (table == null)
            {
                throw new SapEntegrasyonException(
                    "SAP yanıtında T_KANTAR alanı bulunamadı.");
            }

            return table.Elements()
                .Where(x => x.Name.LocalName == "item")
                .Select(MapItem)
                .ToList();
        }

        private static string BuildSoapRequest(
            string? fisNo,
            DateOnly? startDate,
            DateOnly? endDate)
        {
            XNamespace soap =
                "http://schemas.xmlsoap.org/soap/envelope/";

            XNamespace urn =
                "urn:sap-com:document:sap:rfc:functions";

            var operation = new XElement(urn + "ZWS_F0017");

            if (!string.IsNullOrWhiteSpace(fisNo))
            {
                operation.Add(new XElement(
                    "I_FIS_NO", fisNo));
            }

            if (startDate.HasValue)
            {
                operation.Add(new XElement(
                    "I_TARIH_BASLA",
                    startDate.Value.ToString(
                        "yyyy-MM-dd", CultureInfo.InvariantCulture)));
            }

            if (endDate.HasValue)
            {
                operation.Add(new XElement(
                    "I_TARIH_SON",
                    endDate.Value.ToString(
                        "yyyy-MM-dd", CultureInfo.InvariantCulture)));
            }

            // SAP TABLES parametresi: mutlaka gönderilmeli.
            operation.Add(new XElement("T_KANTAR"));

            var envelope = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(soap + "Envelope",
                    new XAttribute(
                        XNamespace.Xmlns + "soapenv", soap),
                    new XAttribute(
                        XNamespace.Xmlns + "urn", urn),
                    new XElement(soap + "Header"),
                    new XElement(soap + "Body", operation)));

            return envelope.ToString(
                SaveOptions.DisableFormatting);
        }

        private static SapKantarDto MapItem(XElement item)
        {
            return new SapKantarDto
            {
                FisNo = GetValue(item, "FIS_NO"),
                KantarId = ParseInt(GetValue(item, "KANTAR_ID")),
                Surec = GetValue(item, "SUREC"),
                Plaka = GetValue(item, "PLAKA"),
                Lgort = GetValue(item, "LGORT"),

                FirstAt = ParseDateTime(
                    GetValue(item, "FIRST_DATE"),
                    GetValue(item, "FIRST_TIME")),

                FirstValue = ParseDecimal(
                    GetValue(item, "FIRST_VALUE")),

                FirstUser = GetValue(item, "FIRST_USER"),
                FirstOk = GetValue(item, "FIRST_OK") == "X",

                SecondAt = ParseDateTime(
                    GetValue(item, "SECOND_DATE"),
                    GetValue(item, "SECOND_TIME")),

                SecondValue = ParseDecimal(
                    GetValue(item, "SECOND_VALUE")),

                SecondUser = GetValue(item, "SECOND_USER"),
                SecondOk = GetValue(item, "SECOND_OK") == "X",

                NetValue = ParseDecimal(
                    GetValue(item, "NET_VALUE")),

                Finished = GetValue(item, "FINISHED") == "X",

                DriverName = GetValue(item, "DRIVER_NAME"),
                DriverSurname = GetValue(item, "DRIVER_SURNAME")
            };
        }

        private static string? GetValue(
            XElement element, string name)
        {
            var value = element.Elements()
                .FirstOrDefault(x => x.Name.LocalName == name)
                ?.Value;

            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static int? ParseInt(string? value)
        {
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var result)
                ? result
                : null;
        }

        private static decimal? ParseDecimal(string? value)
        {
            return decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var result)
                ? result
                : null;
        }

        private static DateTime? ParseDateTime(
            string? date,
            string? time)
        {
            if (string.IsNullOrWhiteSpace(date) ||
                date == "0000-00-00")
            {
                return null;
            }

            var fullValue = $"{date} {time ?? "00:00:00"}";

            return DateTime.TryParseExact(
                fullValue,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result)
                ? DateTime.SpecifyKind(
                    result, DateTimeKind.Unspecified)
                : null;
        }
    }
}
