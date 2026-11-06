using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SapPersonelWorker.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace SapPersonelWorker.Core
{
    /// <summary>
    /// SAP tarafında SRT (SOAP Runtime) üzerinden yayınlanan ZHR_PERLIST03
    /// RFC servisini çağırır. T_RESULT girdi/çıktı paylaşımlı bir tablo
    /// parametresidir; SAP'ın sonucu doldurması için istekte boş alanlı
    /// tek bir item ile "echo" edilmesi gerekir — hiç gönderilmezse SAP
    /// tabloyu doldurmuyor.
    /// </summary>
    public class SapPerlist03Service
    {
        private static readonly XNamespace SoapNs = "http://www.w3.org/2003/05/soap-envelope";
        private static readonly XNamespace UrnNs = "urn:sap-com:document:sap:rfc:functions";

        private readonly HttpClient _httpClient;
        private readonly SapSettings _settings;
        private readonly ILogger<SapPerlist03Service> _logger;

        public SapPerlist03Service(HttpClient httpClient, IOptions<SapSettings> options, ILogger<SapPerlist03Service> logger)
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

        public async Task<List<SapPersonelRaw>> GetPersonelListesiAsync(CancellationToken cancellationToken)
        {
            var sonuc = new List<SapPersonelRaw>();

            if (string.IsNullOrWhiteSpace(_settings.Perlist03.BaseUrl))
            {
                _logger.LogWarning("Sap:Perlist03:BaseUrl appsettings icinde tanimli degil, ZHR_PERLIST03 cagrilmadi.");
                return sonuc;
            }

            var requestXml = BuildRequest();
            _logger.LogInformation("ZHR_PERLIST03 istegi gonderiliyor: {RequestXml}", requestXml);

            using var content = new StringContent(requestXml, Encoding.UTF8, "application/soap+xml");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, _settings.Perlist03.BaseUrl) { Content = content };
            requestMessage.Headers.TryAddWithoutValidation("SOAPAction", $"\"{_settings.Perlist03.SoapAction ?? string.Empty}\"");

            using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogInformation("ZHR_PERLIST03 cevabi ({StatusCode})", (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ZHR_PERLIST03 servisi HTTP {StatusCode} dondu: {Body}", (int)response.StatusCode, responseBody);
                return sonuc;
            }

            return ParseResponse(responseBody);
        }

        private static readonly string[] ItemAlanlari =
        {
            "PERNR", "BEGDA", "ENDDA", "NACHN", "VORNA", "BTRTL", "BTEXT",
            "PLANS", "PLSTX", "ORGEH", "ORGTX", "PERSK", "PTEXT", "USRID",
            "PERNR1", "ORGEH1", "GBDAT", "STAT2", "BEGDA2", "BUKRS", "BUTXT",
            "WERKS", "NAME1", "GSM_PRIVATE_", "GSM_BUSINESS_", "TC_LAST4_"
        };

        private static string BuildRequest()
        {
            // SAP, T_RESULT tablo parametresini istekte görmezse (girdi/çıktı
            // paylaşımlı olduğu için) sonucu doldurmuyor — bu yüzden boş
            // alanlı tek bir item ile parametreyi "echo" etmemiz gerekiyor.
            var item = new XElement("item", ItemAlanlari.Select(ad => new XElement(ad)));

            var envelope = new XElement(SoapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", SoapNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "urn", UrnNs.NamespaceName),
                new XElement(SoapNs + "Header"),
                new XElement(SoapNs + "Body",
                    new XElement(UrnNs + "ZHR_PERLIST03",
                        new XElement("T_RESULT", item)
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
                _logger.LogError(ex, "ZHR_PERLIST03 cevabi parse edilemedi: {Body}", responseBody);
                return sonuc;
            }

            var body = doc.Root?.Elements().FirstOrDefault(x => x.Name.LocalName == "Body");
            var fault = body?.Elements().FirstOrDefault(x => x.Name.LocalName == "Fault");
            if (fault != null)
            {
                _logger.LogError("ZHR_PERLIST03 servisi SOAP Fault dondu: {Fault}", fault.ToString());
                return sonuc;
            }

            var responseElement = body?.Elements().FirstOrDefault(x => x.Name.LocalName == "ZHR_PERLIST03Response");
            if (responseElement == null)
            {
                _logger.LogError("ZHR_PERLIST03 cevabinda ZHR_PERLIST03Response bulunamadi: {Body}", responseBody);
                return sonuc;
            }

            var items = responseElement.Element("T_RESULT")?.Elements("item") ?? Enumerable.Empty<XElement>();
            foreach (var item in items)
            {
                var pernr = item.Element("PERNR")?.Value;
                if (string.IsNullOrWhiteSpace(pernr))
                    continue;

                sonuc.Add(new SapPersonelRaw
                {
                    Pernr = pernr.Trim(),
                    // PERNR_EXT bu serviste yok.
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
                    // Bu serviste eleman adi sonunda alt cizgi var: TC_LAST4_.
                    TcLast4 = item.Element("TC_LAST4_")?.Value,
                });
            }

            return sonuc;
        }
    }
}
