using SharedApp.Domain.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace SharedApp.Infrastructure.Sender
{
    public class SmsSender
    {
        //private readonly SharedDbContext _context;
        public SmsSender(/*SharedDbContext context*/)
        {
            //_context = context;
        }
        //public void SendNodeRedStatusSms(List<string> phoneNumbers, string serverIp, string serverName, bool isOnline)
        //{
        //    string durum = isOnline ? "TEKRAR AKTIF" : "DURDU";
        //    string message = $"Node-RED servisi {durum}. " + $"Sunucu IP: {serverIp}, " + $"Sunucu: {serverName}, " + $"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
        //    SendSms(phoneNumbers, message);
        //}
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
                "<UserName>matliapi</UserName>" +
                "<PassWord>j(gQWIyKKnh5{2</PassWord>" +
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
                           "<UserName>matliapi</UserName>" +
                           "<PassWord>j(gQWIyKKnh5{2</PassWord>" +
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
                           "<UserName>matliapi</UserName>" +
                           "<PassWord>j(gQWIyKKnh5{2</PassWord>" +
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
        public async void SendNewUserSms(string newPassword, string phoneNumber, string userFullName, string sicilNo)
        {
            try
            {
                var postData = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
                              "<MainmsgBody>" +
                              "<Command>0</Command>" +
                              "<Type>2</Type>" +
                              "<Concat>1</Concat>" +
                              "<UserName>matliapi</UserName>" +
                              "<PassWord>j(gQWIyKKnh5{2</PassWord>" +
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
                               "<UserName>matliapi</UserName>" +
                               "<PassWord>j(gQWIyKKnh5{2</PassWord>" +
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

        public static string GetOtpSmsMesaj(string verifyCode, string fullName)
        {
            var message = $"Sayın {fullName}; Akıllı sms şifreniz: {verifyCode}. Akıllı sms şifrenizi Matlı personeli dahil kimseyle paylaşmayınız. MATLI OTOMASYON";
            return ReplaceSmsMesaj(message);
        }

        public static string GetPasswordSmsMesaj(string password, string fullName)
        {
            var message = $"Sayın {fullName}; Matlı Otomasyon Portalına giriş şifreniz: {password}. Şifrenizi Matlı personeli dahil kimseyle paylaşmayınız. MATLI OTOMASYON";
            return ReplaceSmsMesaj(message);
        }

        public static string GetNewUserSmsMesaj(string sicilNo, string newPassword, string fullName)
        {
            var message = $"Sayın {fullName}; Matlı Otomasyon Portalına giriş için adınıza kullanıcı oluşturulmuş ve yetkileriniz verilmiştir. Kullanıcı Adı: {sicilNo} ve Şifre: {newPassword} ile giriş yapabilirsiniz. Şifrenizi Matlı personeli dahil kimseyle paylaşmayınız. Şifrenizi değiştirmeyi unutmayınız. MATLI OTOMASYON";
            return ReplaceSmsMesaj(message);
        }

        public static string GetNewAlarmSmsMesaj(string alarmMesaj, string fullName)
        {
            //var message = $"Sayın {fullName}; {alarmMesaj} MATLI OTOMASYON";
            var message = $"{alarmMesaj} MATLI OTOMASYON";
            return ReplaceSmsMesaj(message);
        }

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