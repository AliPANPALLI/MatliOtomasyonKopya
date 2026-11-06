namespace SapPersonelWorker.Models
{
    /// <summary>
    /// ZABAP_PA_MAT_FM002 ve ZHR_PERLIST03 servislerinin ortak döndürdüğü
    /// alanlar — SOAP cevabından ham (string) olarak okunur, DB'ye
    /// yazılmadan önce SapPersonelSyncWorker içinde tarih/tip dönüşümü
    /// yapılır. İki servis arasındaki farklar:
    ///   - PernrExt sadece ZABAP_PA_MAT_FM002'de var.
    ///   - TC_LAST4 alanının XML eleman adı serviste "TC_LAST4", diğerinde
    ///     "TC_LAST4_" (sondaki alt çizgi farklı) — okuma tarafında ikisi
    ///     de bu tek alana eşleniyor.
    /// </summary>
    public class SapPersonelRaw
    {
        public string Pernr { get; set; } = string.Empty;
        public string? PernrExt { get; set; }
        public string? Begda { get; set; }
        public string? Endda { get; set; }
        public string? Nachn { get; set; }
        public string? Vorna { get; set; }
        public string? Btrtl { get; set; }
        public string? Btext { get; set; }
        public string? Plans { get; set; }
        public string? Plstx { get; set; }
        public string? Orgeh { get; set; }
        public string? Orgtx { get; set; }
        public string? Persk { get; set; }
        public string? Ptext { get; set; }
        public string? Usrid { get; set; }
        public string? Pernr1 { get; set; }
        public string? Orgeh1 { get; set; }
        public string? Gbdat { get; set; }
        public string? Stat2 { get; set; }
        public string? Begda2 { get; set; }
        public string? Bukrs { get; set; }
        public string? Butxt { get; set; }
        public string? Werks { get; set; }
        public string? Name1 { get; set; }
        public string? GsmPrivate { get; set; }
        public string? GsmBusiness { get; set; }
        public string? TcLast4 { get; set; }
    }
}
