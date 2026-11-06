using Microsoft.AspNetCore.Http;
using SharedApp.Domain.Models;
using SharedApp.Domain.Models.ReportModels;
using SharedKernel;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Threading;

namespace SharedApp.Infrastructure.Sender
{
    public class EmailSender
    {
        private const string fromAddress = "matliotomasyon@matli.com.tr";
        //private readonly SharedDbContext _context;
        private readonly SmtpClient _smtpClient;
        public class MailAttachment
        {
            public string FilePath { get; set; }
            public string? FileName { get; set; }
            public string? ContentType { get; set; }
            //public Stream Content { get; set; }
            //public string Name { get; set; }
            //public string MediaType { get; set; }
        }
        public EmailSender(/*SharedDbContext context*/)
        {
            _smtpClient = new SmtpClient
            {
                Port = 587,
                Host = "smtp.office365.com",
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("matliotomasyon@matli.com.tr", "TNx+/f(m#o'{j3LK")
            };
            //_context = context;
        }
        public async Task SendAlarmMessage(string alarmMesaj, List<string> emailAdresler)
        {
            try
            {
                var gonderildi = await SendEmailForAlarm(emailAdresler, alarmMesaj);
                //if (gonderildi.Item1)
                //{
                //    var gonderilenSms = new GonderilenSms();
                //    gonderilenSms.Mesaj = alarmMesaj;
                //    gonderilenSms.DonusMesaj = "";
                //    gonderilenSms.CepTelefonu = "";
                //    gonderilenSms.KayitZamani = DateTime.Now;
                //    gonderilenSms.NetIp = "";
                //    _context.GonderilenSms.Add(gonderilenSms);
                //    _context.SaveChanges();
                //}
            }
            catch (Exception ex)
            {
            }
        }

        public async void SendScheduledMaintenanceMessage(string emailKonu, string emailGovde, List<string> emailAdresler, IFormFile[] files)
        {
            try
            {
                var gonderildi = await SendSEmailForScheduledMaintenance(emailKonu, emailGovde, emailAdresler, files);
                //if (gonderildi.Item1)
                //{
                //    var gonderilenSms = new GonderilenSms();
                //    gonderilenSms.Mesaj = alarmMesaj;
                //    gonderilenSms.DonusMesaj = "";
                //    gonderilenSms.CepTelefonu = "";
                //    gonderilenSms.KayitZamani = DateTime.Now;
                //    gonderilenSms.NetIp = "";
                //    _context.GonderilenSms.Add(gonderilenSms);
                //    _context.SaveChanges();
                //}
            }
            catch (Exception ex)
            {
            }
        }

        public async void SendNewPassword(string newPassword, string email, string userFullName, string sicilNo)
        {
            try
            {
                var gonderildi = await SendEmailForResetPassword(email, newPassword, userFullName, sicilNo);
                //if (gonderildi.Item1)
                //{
                //    var gonderilenSms = new GonderilenSms();
                //    gonderilenSms.Mesaj = newPassword;
                //    gonderilenSms.DonusMesaj = "";
                //    gonderilenSms.CepTelefonu = "";
                //    gonderilenSms.KayitZamani = DateTime.Now;
                //    gonderilenSms.NetIp = "";
                //    _context.GonderilenSms.Add(gonderilenSms);
                //    _context.SaveChanges();
                //}
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task SendOtp(string otpCode, string email, bool isPasswordReset = false)
        {
            try
            {
                var gonderildi = await SendEmailForOtp(email, otpCode, isPasswordReset);
                //if (gonderildi.Item1)
                //{
                //    var gonderilenSms = new GonderilenSms();
                //    gonderilenSms.Mesaj = otpCode;
                //    gonderilenSms.DonusMesaj = "";
                //    gonderilenSms.CepTelefonu = "";
                //    gonderilenSms.KayitZamani = DateTime.Now;
                //    gonderilenSms.NetIp = "";
                //    _context.GonderilenSms.Add(gonderilenSms);
                //    _context.SaveChanges();
                //}
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async void SendNewUserEmail(string newPassword, string email, string userFullName, string sicilNo)
        {
            try
            {
                var gonderildi = await SendEmailForNewUser(email, newPassword, userFullName, sicilNo);
                //if (gonderildi.Item1)
                //{
                //    var gonderilenSms = new GonderilenSms();
                //    gonderilenSms.Mesaj = newPassword;
                //    gonderilenSms.DonusMesaj = "";
                //    gonderilenSms.CepTelefonu = "";
                //    gonderilenSms.KayitZamani = DateTime.Now;
                //    gonderilenSms.NetIp = "";
                //    _context.GonderilenSms.Add(gonderilenSms);
                //    _context.SaveChanges();
                //}
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //public async Task<bool> SendScheduledMaintenanceEmail(string newPassword, string email, string userFullName, string userName)
        //{
        //    try
        //    {
        //        var gonderildi = await SendEmailForScheduledMaintenance(email, newPassword, userFullName, userName);
        //        //if (gonderildi.Item1)
        //        //{
        //        //    var gonderilenSms = new GonderilenSms();
        //        //    gonderilenSms.Mesaj = newPassword;
        //        //    gonderilenSms.DonusMesaj = "";
        //        //    gonderilenSms.CepTelefonu = "";
        //        //    gonderilenSms.KayitZamani = DateTime.Now;
        //        //    gonderilenSms.NetIp = "";
        //        //    _context.GonderilenSms.Add(gonderilenSms);
        //        //    _context.SaveChanges();
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

        private async Task<Tuple<bool, string>> SendEmailForOtp(string email, string verifyCode, bool isPasswordReset = false)
        {
            try
            {


                if (string.IsNullOrEmpty(email))
                {
                    return new Tuple<bool, string>(false, "Email boş bırakılamaz!");
                    //throw new Exception("Email boş bırakılamaz!");
                }

                var label = isPasswordReset ? "Şifre sıfırlama kodunuz (10 dakika geçerlidir)" : "Siteye giriş doğrulama kodunuz";
                var message = $"<h2>{label}</h2><h3><b>{verifyCode}</b></h3>";
                using MailMessage mailMessage = new MailMessage()
                {
                    From = new MailAddress(fromAddress, "MATLI OTOMASYON"),
                    Subject = "Doğrulama Kodu",
                    Body = message,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(new MailAddress(email));
                await _smtpClient.SendMailAsync(mailMessage);
                return new Tuple<bool, string>(true, "Eposta gönderildi");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, ex.Message);
                //throw ex;
            }
        }

        private async Task<Tuple<bool, string>> SendEmailForResetPassword(string email, string newPassword, string userFullName, string sicilNo)
        {
            try
            {
                var _smtpClient = new SmtpClient
                {
                    Port = 587,
                    Host = "smtp.office365.com",
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("matliotomasyon@matli.com.tr", "TNx+/f(m#o'{j3LK")
                };

                if (string.IsNullOrEmpty(email))
                {
                    return new Tuple<bool, string>(false, "Email boş bırakılamaz!");
                    //throw new Exception("Email boş bırakılamaz!");
                }

                var message = $"<p>Sn, {userFullName},</p>" +
                    $"<p>Matlı Otomasyon üzerindeki {sicilNo} kullanıcınızın şifresi sıfırlanmıştır.</p>" +
                    $"<p>Şifre : <b>{newPassword}</b></p>" +
                    $"<p>Güvenliğiniz için şifrenizi değiştirmeyi unutmayınız ve şifrenizi kimseyle paylaşmayınız.</p>" +
                    $"<p>https://otomasyon.matli.com.tr</p>" +
                    $"<p>Saygılarımızla</p>";
                using MailMessage mailMessage = new MailMessage()
                {
                    From = new MailAddress(fromAddress, "MATLI OTOMASYON"),
                    Subject = "Şifre Sıfırlama",
                    Body = message,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(new MailAddress(email));
                await _smtpClient.SendMailAsync(mailMessage);
                return new Tuple<bool, string>(true, "Eposta gönderildi");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, ex.Message);
                //throw ex;
            }
        }

        private async Task<Tuple<bool, string>> SendEmailForNewUser(string email, string newPassword, string userFullName, string sicilNo)
        {
            try
            {
                var _smtpClient = new SmtpClient
                {
                    Port = 587,
                    Host = "smtp.office365.com",
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("matliotomasyon@matli.com.tr", "TNx+/f(m#o'{j3LK")
                };

                if (string.IsNullOrEmpty(email))
                {
                    return new Tuple<bool, string>(false, "Email boş bırakılamaz!");
                    //throw new Exception("Email boş bırakılamaz!");
                }

                var message = $"<p>Sn. {userFullName},</p>" +
                    $"<p>Matlı Otomasyon Portalına giriş yapabilmeniz için adınıza kullanıcı oluşturulmuş ve yetkileriniz verilmiştir.</p>" +
                    $"<p></p>" +
                    $"<p>Matlı Otomasyon Portalına, aşağıdaki kullanıcı bilgileri ile giriş yapabilirsiniz.</p>" +
                    $"<p>Kullanıcı Adı : <b>{sicilNo}</b></p>" +
                    $"<p>Şifre : <b>{newPassword}</b></p>" +
                    $"<p>Güvenliğiniz için şifrenizi değiştirmeyi unutmayınız ve şifrenizi kimseyle paylaşmayınız.</p>" +
                    $"<p>https://otomasyon.matli.com.tr</p>" +
                    $"<p>Saygılarımızla.</p>";
                using MailMessage mailMessage = new MailMessage()
                {
                    From = new MailAddress(fromAddress, "MATLI OTOMASYON"),
                    Subject = "Matlı Otomasyon Portalı - Kullanıcı Bilgileri",
                    Body = message,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(new MailAddress(email));
                //mailMessage.CC.Add(new MailAddress("beray.bayazit@matli.com.tr"));
                await _smtpClient.SendMailAsync(mailMessage);
                return new Tuple<bool, string>(true, "Eposta gönderildi");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, ex.Message);
                //throw ex;
            }
        }

        private async Task<Tuple<bool, string>> SendEmailForAlarm(List<string> emailAdresler, string alarmMessage)
        {
            try
            {
                var _smtpClient = new SmtpClient
                {
                    Port = 587,
                    Host = "smtp.office365.com",
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("matliotomasyon@matli.com.tr", "TNx+/f(m#o'{j3LK")
                };

                var message = $"<h2>Alarm: </h2><h3><b>{alarmMessage}</b></h3>";
                using MailMessage mailMessage = new MailMessage()
                {
                    From = new MailAddress(fromAddress, "MATLI OTOMASYON"),
                    Subject = "Alarm",
                    Body = message,
                    IsBodyHtml = true,
                };
                foreach (var emailAdres in emailAdresler)
                {
                    if (string.IsNullOrEmpty(emailAdres))
                    {
                        continue;
                    }
                    mailMessage.To.Add(new MailAddress(emailAdres));
                    await _smtpClient.SendMailAsync(mailMessage);
                }

                return new Tuple<bool, string>(true, "Eposta gönderildi");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, ex.Message);
                //throw ex;
            }
        }

        private async Task<Tuple<bool, string>> SendSEmailForScheduledMaintenance(string emailKonu, string mesaj, List<string> emailAdresler, IFormFile[] files)
        {
            try
            {
                var _smtpClient = new SmtpClient
                {
                    Port = 587,
                    Host = "smtp.office365.com",
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("matliotomasyon@matli.com.tr", "TNx+/f(m#o'{j3LK")
                };

                var message = $"{mesaj}";
                using MailMessage mailMessage = new MailMessage()
                {
                    From = new MailAddress(fromAddress, "MATLI OTOMASYON"),
                    Subject = emailKonu,
                    Body = message,
                    IsBodyHtml = false,
                };

                //const long MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB
                //foreach (var file in files)
                //{
                //    if (file.Length > 0)
                //    {
                //        // Dosya boyutunu kontrol et
                //        if (file.Length > MAX_FILE_SIZE)
                //        {
                //            throw new Exception("Dosya boyutu 10MB'yi aşamaz.");
                //        }
                //        // Dosyayı FileStream ile ekliyoruz
                //        var filePath = Path.Combine(Path.GetTempPath(), file.FileName); // Geçici dosya yolu

                //        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                //        {
                //            // IFormFile'ı disk üzerine yazıyoruz
                //            await file.CopyToAsync(fileStream);
                //        }

                //        // Dosyayı FileStream üzerinden ekliyoruz
                //        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //        {
                //            var attachment = new Attachment(fileStream, file.FileName);
                //            mailMessage.Attachments.Add(attachment);
                //        }
                //        System.IO.File.Delete(filePath);

                //        //var fileName = Path.GetFileName(file.FileName);

                //        //// Dosyayı bir MemoryStream'e kopyalayın
                //        //using (var stream = new MemoryStream())
                //        //{
                //        //    await file.CopyToAsync(stream);
                //        //    stream.Position = 0;

                //        //    // Dosyayı ekleyin
                //        //    var attachment = new Attachment(stream, fileName);
                //        //    mailMessage.Attachments.Add(attachment);
                //        //}
                //    }
                //}

                const long MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // Dosya boyutunu kontrol et
                        if (file.Length > MAX_FILE_SIZE)
                        {
                            throw new Exception("Dosya boyutu 10MB'yi aşamaz.");
                        }

                        // Dosyayı FileStream ile ekliyoruz
                        var filePath = Path.Combine(Path.GetTempPath(), file.FileName); // Geçici dosya yolu

                        // Geçici dosyaya yazma işlemi
                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        // Dosyayı FileStream üzerinden ekliyoruz
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            var attachment = new Attachment(fileStream, file.FileName);
                            mailMessage.Attachments.Add(attachment);
                        }

                        // Dosyayı silelim, çünkü artık ekledik
                        System.IO.File.Delete(filePath);
                    }
                }


                //if (files != null && files.Length > 0)
                //{
                //    foreach (var file in files)
                //    {
                //        if (file.Length > 0)
                //        {
                //            // Stream olarak dosyayı oku ve ekle
                //            var fileName = Path.GetFileName(file.FileName);
                //            using (var stream = new MemoryStream())
                //            {
                //                await file.CopyToAsync(stream);
                //                stream.Position = 0;
                //                var attachment = new Attachment(stream, fileName);
                //                mailMessage.Attachments.Add(attachment);
                //            }
                //        }
                //    }
                //}

                foreach (var emailAdres in emailAdresler)
                {
                    if (string.IsNullOrEmpty(emailAdres))
                    {
                        continue;
                    }
                    mailMessage.To.Add(new MailAddress(emailAdres));
                    await _smtpClient.SendMailAsync(mailMessage);
                }
                // Geçici dosyayı siliyoruz
                return new Tuple<bool, string>(true, "Eposta gönderildi");
            }
            catch (Exception ex)
            {
                return new Tuple<bool, string>(false, ex.Message);
                //throw ex;
            }
        }

        private string ConvertToHtmlFormat(string message)
        {
            // Replace newlines with <br> tags
            string htmlMessage = message.Replace(Environment.NewLine, "<br>");

            // Optionally replace multiple spaces with &nbsp;
            htmlMessage = htmlMessage.Replace("  ", "&nbsp;&nbsp;");

            return htmlMessage;
        }

        public async Task SendEmailAsync(MailGonderimTakip gonderilecekMail, CancellationToken cancellationToken = default /*string subject, string body, List<string> receivers, List<string> ccReceivers = null, List<MailAttachment> attachments = null*/)
        {
            try
            {
                if (string.IsNullOrEmpty(gonderilecekMail.MailIcerik))
                    throw new ArgumentNullException(nameof(gonderilecekMail.MailIcerik));

                using MailMessage message = new MailMessage()
                {
                    From = new MailAddress(fromAddress, "Matlı Otomasyon"),
                    Subject = gonderilecekMail.MailBasligi,
                    Body = gonderilecekMail.MailIcerik,
                    IsBodyHtml = gonderilecekMail.IsBodyHtml.GetValueOrDefault(false),
                };

                if (!string.IsNullOrEmpty(gonderilecekMail.GonderilecekTo))
                {
                    var receivers = gonderilecekMail.GonderilecekTo.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(email => email.Trim()).ToList();
                    if (receivers == null || receivers.Count == 0)
                        throw new ArgumentException("En az bir alıcı olmalıdır.", nameof(receivers));
                    foreach (var item in receivers.Distinct())
                    {
                        message.To.Add(new MailAddress(item));
                    }
                }
                else
                {
                    throw new ArgumentException("En az bir alıcı olmalıdır.", nameof(gonderilecekMail.GonderilecekTo));
                }

                if (!string.IsNullOrEmpty(gonderilecekMail.GonderilecekCc))
                {
                    var ccList = gonderilecekMail.GonderilecekCc.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(email => email.Trim()).ToList();
                    if (ccList.Count() > 0)
                    {
                        foreach (var item in ccList.Distinct())
                        {
                            message.CC.Add(new MailAddress(item));
                        }
                    }
                }
                var attachments = new List<MailAttachment>();
                try
                {
                    attachments = MailEkleriniHazirla(gonderilecekMail);
                    foreach (var attachmentModel in attachments)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!File.Exists(attachmentModel.FilePath))
                        {
                            /*
                             * Bir fotoğraf bulunamadı diye yangın alarm mailini
                             * tamamen engellemiyoruz.
                             */
                            //_logger.LogWarning(
                            //    "Mail eki dosyada bulunamadığı için atlandı. " +
                            //    "MailGonderimTakipId: {MailId}, DosyaYolu: {DosyaYolu}",
                            //    gonderilecekMail.Id,
                            //    attachmentModel.FilePath);

                            continue;
                        }

                        var contentType = string.IsNullOrWhiteSpace(attachmentModel.ContentType) ? "application/octet-stream" : attachmentModel.ContentType;

                        var attachment = new Attachment(attachmentModel.FilePath, contentType);

                        if (!string.IsNullOrWhiteSpace(attachmentModel.FileName))
                        {
                            attachment.Name = attachmentModel.FileName;
                        }

                        message.Attachments.Add(attachment);
                    }
                }
                catch (Exception)
                {
                }

                await _smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private static string GetContentType(string dosyaYolu)
        {
            return Path.GetExtension(dosyaYolu).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
        private List<MailAttachment> MailEkleriniHazirla(MailGonderimTakip gonderilecekMail)
        {
            var attachments = new List<MailAttachment>();

            /*
             * Sistemdeki eski EkYolu kullanımlarını bozmuyoruz.
             */
            EskiEkYolunuEkle(gonderilecekMail, attachments);

            /*
             * Yangın algılama fotoğrafları JSON içinden eklenir.
             */
            JsonEkleriniEkle(gonderilecekMail, attachments);

            /*
             * Aynı dosya iki farklı şekilde eklenmişse
             * yalnızca bir defa gönderilir.
             */
            return attachments
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.FilePath))
                .GroupBy(
                    x => Path.GetFullPath(x.FilePath),
                    StringComparer.OrdinalIgnoreCase)
                .Select(grup => grup.First())
                .ToList();
        }

        private static void EskiEkYolunuEkle(MailGonderimTakip gonderilecekMail, ICollection<MailAttachment> attachments)
        {
            if (string.IsNullOrWhiteSpace(gonderilecekMail.EkYolu))
            {
                return;
            }

            if (gonderilecekMail.EkYoluKlasorMu.GetValueOrDefault(false))
            {
                if (!Directory.Exists(gonderilecekMail.EkYolu))
                {
                    return;
                }

                foreach (var dosyaYolu in Directory.GetFiles(gonderilecekMail.EkYolu))
                {
                    attachments.Add(new MailAttachment
                    {
                        FilePath = dosyaYolu,
                        FileName = Path.GetFileName(dosyaYolu),
                        ContentType = GetContentType(dosyaYolu)
                    });
                }

                return;
            }

            if (File.Exists(gonderilecekMail.EkYolu))
            {
                attachments.Add(new MailAttachment
                {
                    FilePath = gonderilecekMail.EkYolu,
                    FileName = Path.GetFileName(gonderilecekMail.EkYolu),
                    ContentType = GetContentType(gonderilecekMail.EkYolu)
                });
            }
        }
        private void JsonEkleriniEkle(MailGonderimTakip gonderilecekMail, ICollection<MailAttachment> attachments)
        {
            if (string.IsNullOrWhiteSpace(gonderilecekMail.EkBilgileriJson))
            {
                return;
            }

            List<MailEkBilgisi>? ekBilgileri;

            try
            {
                ekBilgileri = JsonSerializer.Deserialize<List<MailEkBilgisi>>(gonderilecekMail.EkBilgileriJson);
            }
            catch (JsonException exception)
            {
                /*
                 * JSON bozuksa mail yine gönderilir,
                 * sadece fotoğraflar eklenmez.
                 */
                //_logger.LogError(
                //    exception,
                //    "Mail ek bilgileri JSON formatında okunamadı. " +
                //    "MailGonderimTakipId: {MailId}",
                //    gonderilecekMail.Id);

                return;
            }

            if (ekBilgileri == null ||
                ekBilgileri.Count == 0)
            {
                return;
            }

            foreach (var ekBilgisi in ekBilgileri)
            {
                if (string.IsNullOrWhiteSpace(ekBilgisi.GoreliDosyaYolu))
                {
                    continue;
                }

                try
                {
                    var tamDosyaYolu = YanginFotografTamYolunuOlustur(ekBilgisi.GoreliDosyaYolu);

                    attachments.Add(new MailAttachment
                    {
                        FilePath = tamDosyaYolu,
                        FileName = string.IsNullOrWhiteSpace(ekBilgisi.DosyaAdi) ? Path.GetFileName(tamDosyaYolu) : ekBilgisi.DosyaAdi,
                        ContentType = ekBilgisi.ContentType
                    });
                }
                catch (Exception exception)
                {
                    //_logger.LogError(
                    //    exception,
                    //    "Alarm fotoğrafı mail eki olarak hazırlanamadı. " +
                    //    "MailGonderimTakipId: {MailId}, " +
                    //    "AlarmLogId: {AlarmLogId}, " +
                    //    "Göreli dosya yolu: {DosyaYolu}",
                    //    gonderilecekMail.Id,
                    //    ekBilgisi.AlarmLogId,
                    //    ekBilgisi.GoreliDosyaYolu);
                }
            }
        }

        private static string YanginFotografTamYolunuOlustur(string goreliDosyaYolu)
        {
            if (string.IsNullOrWhiteSpace(goreliDosyaYolu))
            {
                throw new ArgumentException("Göreli dosya yolu boş olamaz.", nameof(goreliDosyaYolu));
            }

            var anaKlasorTamYolu = Path.GetFullPath(ApiUrls.ViyolYanginDosyalarPath);

            var tamDosyaYolu = Path.GetFullPath(Path.Combine(anaKlasorTamYolu, goreliDosyaYolu));

            var izinVerilenKlasorOnEki = anaKlasorTamYolu.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!tamDosyaYolu.StartsWith(izinVerilenKlasorOnEki, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Mail eki dosya yolu izin verilen klasörün dışında.");
            }

            return tamDosyaYolu;
        }
    }
}
//        const long MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

//foreach (var file in files)
//{
//    if (file.Length > 0)
//    {
//        if (file.Length > MAX_FILE_SIZE)
//        {
//            throw new Exception("Dosya boyutu 10MB'yi aşamaz.");
//        }

//        // Dosyayı stream olarak ekleyin
//        var fileName = Path.GetFileName(file.FileName);
//        using (var stream = new MemoryStream())
//        {
//            await file.CopyToAsync(stream);
//    stream.Position = 0;
//            var attachment = new Attachment(stream, fileName);
//    mailMessage.Attachments.Add(attachment);
//        }
//    }