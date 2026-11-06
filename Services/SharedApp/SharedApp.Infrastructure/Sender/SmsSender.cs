using Microsoft.Extensions.Configuration;
using SharedApp.Domain.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace SharedApp.Infrastructure.Sender
{
    /// <summary>
    /// innomobil SMS servisi üzerinden OTP, şifre, yeni kullanıcı ve alarm SMS'lerini gönderir.
    /// Servis hesabı koda gömülmez; <c>Sms__UserName</c> ve <c>Sms__Password</c> ortam değişkenlerinden okunur.
    /// </summary>
    public class SmsSender
    {
        private readonly string _userName;
        private readonly string _password;

        /// <summary>
        /// innomobil hesabını konfigürasyondan okur.
        /// </summary>
        public SmsSender(IConfiguration configuration)
        {
            _userName = configuration["Sms:UserName"] ?? string.Empty;
            _password = configuration["Sms:Password"] ?? string.Empty;
        }

        /// <summary>
        /// innomobil isteğinin XML gövdesine eklenen, XML'e uygun kaçışlanmış kullanıcı adı ve parola alanları.
        /// </summary>
        private string CredentialsXml =>
            "<UserName>" + System.Security.SecurityElement.Escape(_userName) + "</UserName>" +
            "<PassWord>" + System.Security.SecurityElement.Escape(_password) + "</PassWord>";
        //public void SendNodeRedStatusSms(List<string> phoneNumbers, string serverIp, string serverName, bool isOnline)
        //{
        //    string durum = isOnline ? "TEKRAR AKTIF" : "DURDU";
        //    string message = $"Node-RED servisi {durum}. " + $"Sunucu IP: {serverIp}, " + $"Sunucu: {serverName}, " + $"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
        //    SendSms(phoneNumbers, message);
        //}
        /// <summary>
        /// Verilen numaralara aynı SMS metnini gönderir. Numaralar 90 ile başlayan uluslararası biçime çevrilir.
        /// </summary>
        public void SendSms(List<string> phoneNumberList, string message)
        {
            HttpWebResponse objHttpWebResponse = null;
            Stream objRequestStream = null;

            var objHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://s.innomobil.com/api/v1/sendsms");


            var numbers = string.Join(",", phoneNumberList.Select(x =>
            {
                var phone = x.Trim();

                if (phone.StartsWith("0"))
                    phone = phone.Substring(1);

                if (!phone.StartsWith("90"))
                    phone = "90" + phone;

                return phone;
            }));

            var postData =
                "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
                "<MainmsgBody>" +
                "<Command>0</Command>" +
                "<Type>2</Type>" +
                "<Concat>1</Concat>" +
                CredentialsXml +
                "<Mesgbody>" +
                    System.Security.SecurityElement.Escape(message) +
                "</Mesgbody>" +
                "<Numbers>" + numbers + "</Numbers>" +
                "<Originator>MATLI AS</Originator>" +
                "</MainmsgBody>";

            var bytes = Encoding.UTF8.GetBytes(postData);

            objHttpWebRequest.Method = "POST";
            objHttpWebRequest.ContentLength = bytes.Length;
            objHttpWebRequest.ContentType = "text/xml; encoding='utf-8'";

            objRequestStream = objHttpWebRequest.GetRequestStream();
            objRequestStream.Write(bytes, 0, bytes.Length);
            objRequestStream.Close();

            objHttpWebResponse =
                (HttpWebResponse)objHttpWebRequest.GetResponse();

            var responseFromServer = "";
        }
        /// <summary>
        /// Giriş veya şifre sıfırlama doğrulama kodunu SMS ile gönderir.
        /// </summary>
        public void SendOtpSms(string phoneNumber, string verifyCode, string fullName, bool isPasswordReset = false)
        {
            HttpWebResponse objHttpWebResponse = null;
            Stream objRequestStream = null;
            var objHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://s.innomobil.com/api/v1/sendsms");
            var postData = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
                           "<MainmsgBody>" +
                           "<Command>0</Command>" +
                           "<Type>2</Type>" +
                           "<Concat>1</Concat>" +
                           CredentialsXml +
                           "<Mesgbody>" + (isPasswordReset ? "Matlı Otomasyon şifre sıfırlama kodunuz: " + verifyCode + ". Kod 10 dakika geçerlidir. Kimseyle paylaşmayın." : GetOtpSmsMesaj(verifyCode, fullName)) + "</Mesgbody>" +
                           "<Numbers>90" + phoneNumber + "</Numbers>" +
                           "<Originator>MATLI AS</Originator>" +
                           "</MainmsgBody>";

            var bytes = Encoding.UTF8.GetBytes(postData);
            objHttpWebRequest.Method = "POST";
            objHttpWebRequest.ContentLength = bytes.Length;
            objHttpWebRequest.ContentType = "text/xml; encoding='utf-8'";
            objRequestStream = objHttpWebRequest.GetRequestStream();
            objRequestStream.Write(bytes, 0, bytes.Length);
            objRequestStream.Close();
            objHttpWebResponse = (HttpWebResponse)objHttpWebRequest.GetResponse();
            var responseFromServer = "";
        }

        /// <summary>
        /// Kullanıcıya portal giriş şifresini SMS ile gönderir.
        /// </summary>
        public void SendPasswordSms(string phoneNumber, string password, string fullName)
        {
            HttpWebResponse objHttpWebResponse = null;
            Stream objRequestStream = null;
            var objHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://s.innomobil.com/api/v1/sendsms");
            var postData = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
                           "<MainmsgBody>" +
                           "<Command>0</Command>" +
                           "<Type>2</Type>" +
                           "<Concat>1</Concat>" +
                           CredentialsXml +
                           "<Mesgbody>" + GetPasswordSmsMesaj(password, fullName) + "</Mesgbody>" +
                           "<Numbers>90" + phoneNumber + "</Numbers>" +
                           "<Originator>MATLI AS</Originator>" +
                           "</MainmsgBody>";

            var bytes = Encoding.UTF8.GetBytes(postData);
            objHttpWebRequest.Method = "POST";
            objHttpWebRequest.ContentLength = bytes.Length;
            objHttpWebRequest.ContentType = "text/xml; encoding='utf-8'";
            objRequestStream = objHttpWebRequest.GetRequestStream();
            objRequestStream.Write(bytes, 0, bytes.Length);
            objRequestStream.Close();
            objHttpWebResponse = (HttpWebResponse)objHttpWebRequest.GetResponse();
            var responseFromServer = "";
        }
        /// <summary>
        /// Yeni oluşturulan kullanıcıya kullanıcı adı ve şifresini SMS ile gönderir.
        /// </summary>
        public async void SendNewUserSms(string newPassword, string phoneNumber, string userFullName, string sicilNo)
        {
            try
            {
                var postData = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
                              "<MainmsgBody>" +
                              "<Command>0</Command>" +
                              "<Type>2</Type>" +
                              "<Concat>1</Concat>" +
                              CredentialsXml +
                              "<Mesgbody>" + GetNewUserSmsMesaj(sicilNo, newPassword, userFullName) + "</Mesgbody>" +
                              "<Numbers>90" + phoneNumber + "</Numbers>" +
                              "<Originator>MATLI AS</Originator>" +
                              "</MainmsgBody>";
                var gonderildi = await SendSmsForNewUser(phoneNumber, postData, userFullName, sicilNo);
                //if (gonderildi.Item1)
                //{
                //    var gonderilenSms = new GonderilenSms();
                //    gonderilenSms.Mesaj = postData;
                //    gonderilenSms.DonusMesaj = gonderildi.Item2;
                //    gonderilenSms.CepTelefonu = "";
                //    gonderilenSms.KayitZamani = DateTime.Now;
                //    gonderilenSms.NetIp = "";
                //    gonderilenSms.BasariliMi = gonderildi.Item1;
                //    _context.GonderilenSms.Add(gonderilenSms);
                //    _context.SaveChanges();
                //}
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// Hazırlanmış yeni kullanıcı SMS gövdesini innomobil'e gönderir; sonucu başarı bilgisi ve mesajla döner.
        /// </summary>
        private async Task<Tuple<bool, string>> SendSmsForNewUser(string phoneNumber, string postData, string userFullName, string sicilNo)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return new Tuple<bool, string>(false, "Telefon numarası boş bırakılamaz!");
                }

                HttpWebResponse objHttpWebResponse = null;
                Stream objRequestStream = null;
                var objHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://s.innomobil.com/api/v1/sendsms");

                var bytes = Encoding.UTF8.GetBytes(postData);
                objHttpWebRequest.Method = "POST";
                objHttpWebRequest.ContentLength = bytes.Length;
                objHttpWebRequest.ContentType = "text/xml; encoding='utf-8'";
                objRequestStream = objHttpWebRequest.GetRequestStream();
                objRequestStream.Write(bytes, 0, bytes.Length);
                objRequestStream.Close();
                objHttpWebResponse = (HttpWebResponse)objHttpWebRequest.GetResponse();
                var responseFromServer = "";

                return new Tuple<bool, string>(true, "Sms gönderildi");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, ex.Message);
                //throw ex;
            }
        }

        /// <summary>
        /// Alarm mesajını SMS ile gönderir; sonucu başarı bilgisi ve mesajla döner.
        /// </summary>
        public async Task<Tuple<bool, string>> SendSmsForAlarm(string phoneNumber, string alarmMesaj, string userFullName)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return new Tuple<bool, string>(false, "Telefon numarası boş bırakılamaz!");
                }

                HttpWebResponse objHttpWebResponse = null;
                Stream objRequestStream = null;
                var objHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://s.innomobil.com/api/v1/sendsms");

                var postData = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
                               "<MainmsgBody>" +
                               "<Command>0</Command>" +
                               "<Type>2</Type>" +
                               "<Concat>1</Concat>" +
                               CredentialsXml +
                               "<Mesgbody>" + GetNewAlarmSmsMesaj(alarmMesaj, userFullName) + "</Mesgbody>" +
                               "<Numbers>90" + phoneNumber + "</Numbers>" +
                               "<Originator>MATLI AS</Originator>" +
                               "</MainmsgBody>";

                var bytes = Encoding.UTF8.GetBytes(postData);
                objHttpWebRequest.Method = "POST";
                objHttpWebRequest.ContentLength = bytes.Length;
                objHttpWebRequest.ContentType = "text/xml; encoding='utf-8'";
                objRequestStream = objHttpWebRequest.GetRequestStream();
                objRequestStream.Write(bytes, 0, bytes.Length);
                objRequestStream.Close();
                objHttpWebResponse = (HttpWebResponse)objHttpWebRequest.GetResponse();
                var responseFromServer = "";

                return new Tuple<bool, string>(true, "Sms gönderildi");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, ex.Message);
                //throw ex;
            }
        }

        /// <summary>
        /// Doğrulama kodu SMS metnini oluşturur.
        /// </summary>
        public static string GetOtpSmsMesaj(string verifyCode, string fullName)
        {
            var message = $"Sayın {fullName}; Akıllı sms şifreniz: {verifyCode}. Akıllı sms şifrenizi Matlı personeli dahil kimseyle paylaşmayınız. MATLI OTOMASYON";
            return ReplaceSmsMesaj(message);
        }

        /// <summary>
        /// Portal şifresi SMS metnini oluşturur.
        /// </summary>
        public static string GetPasswordSmsMesaj(string password, string fullName)
        {
            var message = $"Sayın {fullName}; Matlı Otomasyon Portalına giriş şifreniz: {password}. Şifrenizi Matlı personeli dahil kimseyle paylaşmayınız. MATLI OTOMASYON";
            return ReplaceSmsMesaj(message);
        }

        /// <summary>
        /// Yeni kullanıcı bilgilendirme SMS metnini oluşturur.
        /// </summary>
        public static string GetNewUserSmsMesaj(string sicilNo, string newPassword, string fullName)
        {
            var message = $"Sayın {fullName}; Matlı Otomasyon Portalına giriş için adınıza kullanıcı oluşturulmuş ve yetkileriniz verilmiştir. Kullanıcı Adı: {sicilNo} ve Şifre: {newPassword} ile giriş yapabilirsiniz. Şifrenizi Matlı personeli dahil kimseyle paylaşmayınız. Şifrenizi değiştirmeyi unutmayınız. MATLI OTOMASYON";
            return ReplaceSmsMesaj(message);
        }

        /// <summary>
        /// Alarm SMS metnini oluşturur.
        /// </summary>
        public static string GetNewAlarmSmsMesaj(string alarmMesaj, string fullName)
        {
            //var message = $"Sayın {fullName}; {alarmMesaj} MATLI OTOMASYON";
            var message = $"{alarmMesaj} MATLI OTOMASYON";
            return ReplaceSmsMesaj(message);
        }

        /// <summary>
        /// SMS metnindeki XML özel karakterlerini kaçışlar.
        /// </summary>
        public static string ReplaceSmsMesaj(string text)
        {
            var r = text.Replace("<", "&lt;");
            r = r.Replace(">", "&gt;");
            r = r.Replace("&", "&amp;");
            r = r.Replace("\"", "&quot;");
            r = r.Replace("'", "&apos;");
            return r;
        }
    }
}