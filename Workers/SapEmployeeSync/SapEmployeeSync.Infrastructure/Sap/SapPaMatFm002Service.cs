using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using SapEmployeeSync.Application.Models;

namespace SapEmployeeSync.Infrastructure.Sap
{
    /// <summary>
    /// SAP tarafında SRT (SOAP Runtime) üzerinden yayınlanan
    /// ZABAP_PA_MAT_FM002 RFC servisini çağırır — I_PERNR boş
    /// gönderildiğinde tüm personel listesini (GT_OUT) döndürür.
    /// </summary>
    public class SapPaMatFm002Service
    {
        private static readonly XNamespace SoapNs = "http://www.w3.org/2003/05/soap-envelope";
        private static readonly XNamespace UrnNs = "urn:sap-com:document:sap:rfc:functions";

        private readonly HttpClient _httpClient;
        private readonly SapSettings _settings;
        private readonly ILogger<SapPaMatFm002Service> _logger;

        public SapPaMatFm002Service(HttpClient httpClient, IOptions<SapSettings> options, ILogger<SapPaMatFm002Service> logger)
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

        /// <param name="pernr">Boş bırakılırsa tüm personel listesi döner.</param>
        public async Task<List<SapPersonelRaw>> GetPersonelListesiAsync(string? pernr, CancellationToken cancellationToken)
        {
            var sonuc = new List<SapPersonelRaw>();

            if (string.IsNullOrWhiteSpace(_settings.PaMatFm002.BaseUrl))
            {
                _logger.LogWarning("Sap:PaMatFm002:BaseUrl appsettings icinde tanimli degil, ZABAP_PA_MAT_FM002 cagrilmadi.");
                return sonuc;
            }

            var requestXml = BuildRequest(pernr);
            _logger.LogInformation("ZABAP_PA_MAT_FM002 istegi gonderiliyor (I_PERNR={Pernr}): {RequestXml}", pernr, requestXml);

            using var content = new StringContent(requestXml, Encoding.UTF8, "application/soap+xml");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, _settings.PaMatFm002.BaseUrl) { Content = content };
            requestMessage.Headers.TryAddWithoutValidation("SOAPAction", $"\"{_settings.PaMatFm002.SoapAction ?? string.Empty}\"");

            using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("ZABAP_PA_MAT_FM002 cevabi ({StatusCode})", (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ZABAP_PA_MAT_FM002 servisi HTTP {StatusCode} dondu: {Body}", (int)response.StatusCode, responseBody);
                return sonuc;
            }

            return ParseResponse(responseBody);
        }

        private static string BuildRequest(string? pernr)
        {
            var envelope = new XElement(SoapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", SoapNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "urn", UrnNs.NamespaceName),
                new XElement(SoapNs + "Header"),
                new XElement(SoapNs + "Body",
                    new XElement(UrnNs + "ZABAP_PA_MAT_FM002",
                        new XElement("I_PERNR", pernr ?? string.Empty)
                    )
                )
            );

            return new XDocument(envelope).ToString(SaveOptions.DisableFormatting);
        }

        private List<SapPersonelRaw> ParseResponse(string responseBody)
        {
            var sonuc = new List<SapPersonelRaw>();

            XDocument doc;
            try
            {
                doc = XDocument.Parse(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZABAP_PA_MAT_FM002 cevabi parse edilemedi: {Body}", responseBody);
                return sonuc;
            }

            var body = doc.Root?.Elements().FirstOrDefault(x => x.Name.LocalName == "Body");
            var fault = body?.Elements().FirstOrDefault(x => x.Name.LocalName == "Fault");
            if (fault != null)
            {
                _logger.LogError("ZABAP_PA_MAT_FM002 servisi SOAP Fault dondu: {Fault}", fault.ToString());
                return sonuc;
            }

            var responseElement = body?.Elements().FirstOrDefault(x => x.Name.LocalName == "ZABAP_PA_MAT_FM002Response");
            if (responseElement == null)
            {
                _logger.LogError("ZABAP_PA_MAT_FM002 cevabinda ZABAP_PA_MAT_FM002Response bulunamadi: {Body}", responseBody);
                return sonuc;
            }

            var items = responseElement.Element("GT_OUT")?.Elements("item") ?? Enumerable.Empty<XElement>();
            foreach (var item in items)
            {
                var pernr = item.Element("PERNR")?.Value;
                if (string.IsNullOrWhiteSpace(pernr))
                    continue;

                sonuc.Add(new SapPersonelRaw
                {
                    Pernr = pernr.Trim(),
                    PernrExt = item.Element("PERNR_EXT")?.Value,
                    Begda = item.Element("BEGDA")?.Value,
                    Endda = item.Element("ENDDA")?.Value,
                    Nachn = item.Element("NACHN")?.Value,
                    Vorna = item.Element("VORNA")?.Value,
                    Btrtl = item.Element("BTRTL")?.Value,
                    Btext = item.Element("BTEXT")?.Value,
                    Plans = item.Element("PLANS")?.Value,
                    Plstx = item.Element("PLSTX")?.Value,
                    Orgeh = item.Element("ORGEH")?.Value,
                    Orgtx = item.Element("ORGTX")?.Value,
                    Persk = item.Element("PERSK")?.Value,
                    Ptext = item.Element("PTEXT")?.Value,
                    Usrid = item.Element("USRID")?.Value,
                    Pernr1 = item.Element("PERNR1")?.Value,
                    Orgeh1 = item.Element("ORGEH1")?.Value,
                    Gbdat = item.Element("GBDAT")?.Value,
                    Stat2 = item.Element("STAT2")?.Value,
                    Begda2 = item.Element("BEGDA2")?.Value,
                    Bukrs = item.Element("BUKRS")?.Value,
                    Butxt = item.Element("BUTXT")?.Value,
                    Werks = item.Element("WERKS")?.Value,
                    Name1 = item.Element("NAME1")?.Value,
                    GsmPrivate = item.Element("GSM_PRIVATE_")?.Value,
                    GsmBusiness = item.Element("GSM_BUSINESS_")?.Value,
                    TcLast4 = item.Element("TC_LAST4")?.Value,
                });
            }

            return sonuc;
        }
    }
}
