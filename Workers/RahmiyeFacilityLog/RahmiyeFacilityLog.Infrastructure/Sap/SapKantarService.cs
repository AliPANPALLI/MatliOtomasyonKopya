using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using RahmiyeFacilityLog.Application.Models;

namespace RahmiyeFacilityLog.Infrastructure.Sap
{
    /// <summary>
    /// SAP tarafında SRT (SOAP Runtime) üzerinden yayınlanan ZWS_F0017 (kantar/tartım) RFC servisini çağırır.
    /// </summary>
    public class SapKantarService
    {
        private static readonly XNamespace SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace UrnNs = "urn:sap-com:document:sap:rfc:functions";

        private readonly HttpClient _httpClient;
        private readonly SapSettings _settings;
        private readonly ILogger<SapKantarService> _logger;

        public SapKantarService(HttpClient httpClient, IOptions<SapSettings> options, ILogger<SapKantarService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.Username}:{_settings.Password}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
        }
              
        public async Task<List<SapKantarModel>> GetKantarKayitlariAsync(DateTime tarihBasla, DateTime? tarihSon, CancellationToken cancellationToken)
        {
            var sonuc = new List<SapKantarModel>();

            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            {
                _logger.LogWarning("Sap:BaseUrl appsettings icinde tanimli degil, SAP kantar servisi cagrilmadi.");
                return sonuc;
            }

            var requestXml = BuildRequest(tarihBasla, tarihSon);
            _logger.LogInformation("SAP kantar istegi gonderiliyor (I_TARIH_BASLA={Basla}, I_TARIH_SON={Son}): {RequestXml}", tarihBasla.ToString("yyyy-MM-dd"), tarihSon?.ToString("yyyy-MM-dd"), requestXml);

            using var content = new StringContent(requestXml, Encoding.UTF8, "text/xml");
            content.Headers.ContentType!.CharSet = "utf-8";

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl) { Content = content };
            requestMessage.Headers.TryAddWithoutValidation("SOAPAction", $"\"{_settings.SoapAction ?? string.Empty}\"");

            using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("SAP kantar cevabi ({StatusCode}): {ResponseXml}", (int)response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SAP kantar servisi HTTP {StatusCode} dondu: {Body}", (int)response.StatusCode, responseBody);
                return sonuc;
            }

            return ParseResponse(responseBody);
        }

        private static string BuildRequest(DateTime tarihBasla, DateTime? tarihSon)
        {
            var envelope = new XElement(SoapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", SoapNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "urn", UrnNs.NamespaceName),
                new XElement(SoapNs + "Header"),
                new XElement(SoapNs + "Body",
                    new XElement(UrnNs + "ZWS_F0017",
                        new XElement("I_FIS_NO", string.Empty),
                        new XElement("I_TARIH_BASLA", tarihBasla.ToString("yyyy-MM-dd")),
                        new XElement("I_TARIH_SON", tarihSon?.ToString("yyyy-MM-dd") ?? string.Empty),
                        new XElement("T_KANTAR",
                            new XElement("item",
                                new XElement("FIS_NO", string.Empty),
                                new XElement("KANTAR_ID", string.Empty),
                                new XElement("SUREC", string.Empty),
                                new XElement("PLAKA", string.Empty),
                                new XElement("FIRST_DATE", string.Empty),
                                new XElement("FIRST_TIME", string.Empty),
                                new XElement("FIRST_VALUE", string.Empty),
                                new XElement("FIRST_USER", string.Empty),
                                new XElement("FIRST_OK", string.Empty),
                                new XElement("SECOND_DATE", string.Empty),
                                new XElement("SECOND_TIME", string.Empty),
                                new XElement("SECOND_VALUE", string.Empty),
                                new XElement("SECOND_USER", string.Empty),
                                new XElement("SECOND_OK", string.Empty),
                                new XElement("LAST_VALUE", string.Empty),
                                new XElement("THIRD_DATE", string.Empty),
                                new XElement("THIRD_TIME", string.Empty),
                                new XElement("THIRD_USER", string.Empty),
                                new XElement("THIRD_OK", string.Empty),
                                new XElement("DRIVER_ID", string.Empty),
                                new XElement("DRIVER_NAME", string.Empty),
                                new XElement("DRIVER_SURNAME", string.Empty),
                                new XElement("TEL_NUMBER", string.Empty)
                            )
                        )
                    )
                )
            );

            return new XDocument(envelope).ToString(SaveOptions.DisableFormatting);
        }

        private List<SapKantarModel> ParseResponse(string responseBody)
        {
            var sonuc = new List<SapKantarModel>();

            XDocument doc;
            try
            {
                doc = XDocument.Parse(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAP kantar servisi cevabi parse edilemedi: {Body}", responseBody);
                return sonuc;
            }

            var body = doc.Root?.Element(SoapNs + "Body");
            var fault = body?.Element(SoapNs + "Fault") ?? body?.Elements().FirstOrDefault(x => x.Name.LocalName == "Fault");
            if (fault != null)
            {
                _logger.LogError("SAP kantar servisi SOAP Fault dondu: {Fault}", fault.ToString());
                return sonuc;
            }

            var responseElement = body?.Elements().FirstOrDefault(x => x.Name.LocalName == "ZWS_F0017Response");
            if (responseElement == null)
            {
                _logger.LogError("SAP kantar servisi cevabinda ZWS_F0017Response bulunamadi: {Body}", responseBody);
                return sonuc;
            }

            var returnType = responseElement.Element("E_RETURN_TYPE")?.Value;
            var returnMessage = responseElement.Element("E_RETURN_MESSAGE")?.Value;
            if (string.Equals(returnType, "E", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("SAP kantar servisi hata dondu: {Mesaj}", returnMessage);
                return sonuc;
            }

            var items = responseElement.Element("T_KANTAR")?.Elements("item") ?? Enumerable.Empty<XElement>();
            foreach (var item in items)
            {
                sonuc.Add(new SapKantarModel
                {
                    FisNo = item.Element("FIS_NO")?.Value,
                    KantarId = item.Element("KANTAR_ID")?.Value,
                    Surec = item.Element("SUREC")?.Value,
                    Plaka = item.Element("PLAKA")?.Value,
                    Lgort = item.Element("LGORT")?.Value,

                    FirstDate = item.Element("FIRST_DATE")?.Value,
                    FirstTime = item.Element("FIRST_TIME")?.Value,
                    FirstValue = ParseDecimal(item.Element("FIRST_VALUE")?.Value),
                    FirstUser = item.Element("FIRST_USER")?.Value,
                    FirstOk = item.Element("FIRST_OK")?.Value,

                    SecondDate = item.Element("SECOND_DATE")?.Value,
                    SecondTime = item.Element("SECOND_TIME")?.Value,
                    SecondValue = ParseDecimal(item.Element("SECOND_VALUE")?.Value),
                    SecondUser = item.Element("SECOND_USER")?.Value,
                    SecondOk = item.Element("SECOND_OK")?.Value,

                    LastValue = ParseDecimal(item.Element("LAST_VALUE")?.Value),

                    ThirdDate = item.Element("THIRD_DATE")?.Value,
                    ThirdTime = item.Element("THIRD_TIME")?.Value,
                    ThirdUser = item.Element("THIRD_USER")?.Value,
                    ThirdOk = item.Element("THIRD_OK")?.Value,

                    NetValue = ParseDecimal(item.Element("NET_VALUE")?.Value),

                    DriverId = item.Element("DRIVER_ID")?.Value,
                    DriverName = item.Element("DRIVER_NAME")?.Value,
                    DriverSurname = item.Element("DRIVER_SURNAME")?.Value,
                    TelNumber = item.Element("TEL_NUMBER")?.Value,

                    Finished = item.Element("FINISHED")?.Value,
                });
            }

            return sonuc;
        }

        private static decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
        }
    }
}
