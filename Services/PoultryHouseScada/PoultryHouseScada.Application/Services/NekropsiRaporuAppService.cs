using System.Text.Json;
using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.ViewModels;
using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using SharedKernel;

namespace PoultryHouseScada.Application.Services
{
    public class NekropsiRaporuAppService : INekropsiRaporuAppService
    {
        private static readonly HashSet<string> IzinVerilenFotografTipleri = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp",
            "image/heic",
            "image/heif",
        };

        private readonly INekropsiRaporuRepository _nekropsiRaporuRepository;

        public NekropsiRaporuAppService(INekropsiRaporuRepository nekropsiRaporuRepository)
        {
            _nekropsiRaporuRepository = nekropsiRaporuRepository;
        }

        public async Task<List<NekropsiRaporuOzetViewModel>> GetKumesNekropsiRaporlari(int kumesId)
        {
            var raporlar = await _nekropsiRaporuRepository.GetAllWithDetailsByKumesIdAsync(kumesId);

            return raporlar.Select(x => new NekropsiRaporuOzetViewModel
            {
                Id = x.Id,
                KumesId = x.KumesId,
                Tarih = x.Tarih,
                Hafta = x.Hafta,
                NekropsiSebebi = x.NekropsiSebebi,
                Pcr = x.Pcr,
                KayitZamani = x.KayitZamani,
                DoluBulguSayisi = x.Bulgular.Count,
                FotografSayisi = x.Fotograflar.Count,
            }).ToList();
        }

        public async Task<NekropsiRaporuViewModel?> GetNekropsiRaporuById(int id)
        {
            var rapor = await _nekropsiRaporuRepository.GetWithDetailsByIdAsync(id);

            if (rapor == null)
                return null;

            return new NekropsiRaporuViewModel
            {
                Id = rapor.Id,
                KumesId = rapor.KumesId,
                Tarih = rapor.Tarih,
                Hafta = rapor.Hafta,
                NekropsiSebebi = rapor.NekropsiSebebi,
                Pcr = rapor.Pcr,
                KayitZamani = rapor.KayitZamani,
                KayitYapanUserId = rapor.KayitYapanUserId,
                Bulgular = rapor.Bulgular.Select(b => new NekropsiBulguViewModel
                {
                    Key = b.Key,
                    Bulunan = b.Bulunan,
                    Toplam = b.Toplam,
                }).ToList(),
                Fotograflar = rapor.Fotograflar.Select(f => new NekropsiFotografViewModel
                {
                    Id = f.Id,
                    DosyaAdi = f.DosyaAdi,
                    ContentType = f.ContentType,
                    BoyutByte = f.BoyutByte,
                }).ToList(),
            };
        }

        public async Task<int> KaydetAsync(NekropsiRaporuKaydetRequest request, string? userId)
        {
            if (request.KumesId <= 0)
                throw new Exception("Kümes bilgisi zorunludur.");

            if (string.IsNullOrWhiteSpace(request.NekropsiSebebi))
                throw new Exception("Nekropsi sebebi zorunludur.");

            var bulgular = ParseBulgular(request.BulgularJson)
                .Where(b => b.Bulunan.HasValue || b.Toplam.HasValue)
                .ToList();

            var kuralDisi = bulgular.FirstOrDefault(b =>
                b.Bulunan.HasValue && b.Toplam.HasValue && b.Toplam.Value < b.Bulunan.Value);

            if (kuralDisi != null)
                throw new Exception($"\"{kuralDisi.Key}\" bulgusunda toplam, bulunan sayısından az olamaz.");

            var rapor = new NekropsiRaporu
            {
                KumesId = request.KumesId,
                Tarih = request.Tarih == default ? DateTime.Now : request.Tarih,
                Hafta = request.Hafta,
                NekropsiSebebi = request.NekropsiSebebi.Trim(),
                Pcr = string.IsNullOrWhiteSpace(request.Pcr) ? "BELIRTILMEDI" : request.Pcr,
                KayitZamani = DateTime.Now,
                KayitYapanUserId = userId,
                Silindi = false,
                Bulgular = bulgular.Select(b => new NekropsiBulgu
                {
                    Key = b.Key,
                    Bulunan = b.Bulunan,
                    Toplam = b.Toplam,
                }).ToList(),
            };

            await _nekropsiRaporuRepository.AddAsync(rapor);
            await _nekropsiRaporuRepository.SaveChangesAsync();

            if (request.Fotograflar is { Count: > 0 })
            {
                rapor.Fotograflar = await SaveFotograflarAsync(rapor.Id, request.Fotograflar);
                await _nekropsiRaporuRepository.UpdateAsync(rapor);
                await _nekropsiRaporuRepository.SaveChangesAsync();
            }

            return rapor.Id;
        }

        public async Task GuncelleAsync(int id, NekropsiRaporuKaydetRequest request, string? userId)
        {
            if (string.IsNullOrWhiteSpace(request.NekropsiSebebi))
                throw new Exception("Nekropsi sebebi zorunludur.");

            var bulgular = ParseBulgular(request.BulgularJson)
                .Where(b => b.Bulunan.HasValue || b.Toplam.HasValue)
                .ToList();

            var kuralDisi = bulgular.FirstOrDefault(b =>
                b.Bulunan.HasValue && b.Toplam.HasValue && b.Toplam.Value < b.Bulunan.Value);

            if (kuralDisi != null)
                throw new Exception($"\"{kuralDisi.Key}\" bulgusunda toplam, bulunan sayısından az olamaz.");

            var guncellenecekRapor = new NekropsiRaporu
            {
                Id = id,
                Tarih = request.Tarih == default ? DateTime.Now : request.Tarih,
                Hafta = request.Hafta,
                NekropsiSebebi = request.NekropsiSebebi.Trim(),
                Pcr = string.IsNullOrWhiteSpace(request.Pcr) ? "BELIRTILMEDI" : request.Pcr,
            };

            var yeniBulgular = bulgular.Select(b => new NekropsiBulgu
            {
                Key = b.Key,
                Bulunan = b.Bulunan,
                Toplam = b.Toplam,
            }).ToList();

            await _nekropsiRaporuRepository.GuncelleAsync(guncellenecekRapor, yeniBulgular);

            if (request.Fotograflar is { Count: > 0 })
            {
                var rapor = await _nekropsiRaporuRepository.GetByIdAsync(id);
                rapor.Fotograflar = await SaveFotograflarAsync(id, request.Fotograflar);
                await _nekropsiRaporuRepository.UpdateAsync(rapor);
                await _nekropsiRaporuRepository.SaveChangesAsync();
            }
        }

        public async Task SilFotografAsync(int fotografId)
        {
            var fotograf = await _nekropsiRaporuRepository.FotografSilAsync(fotografId);

            if (fotograf == null)
                return;

            var tamDosyaYolu = TamDosyaYolunuOlustur(fotograf.GoreliDosyaYolu);

            if (File.Exists(tamDosyaYolu))
                File.Delete(tamDosyaYolu);
        }

        public async Task<NekropsiFotografIndirModel?> GetFotografAsync(int fotografId)
        {
            var fotograf = await _nekropsiRaporuRepository.GetFotografByIdAsync(fotografId);

            if (fotograf == null)
                return null;

            var tamDosyaYolu = TamDosyaYolunuOlustur(fotograf.GoreliDosyaYolu);

            if (!File.Exists(tamDosyaYolu))
                return null;

            var veri = await File.ReadAllBytesAsync(tamDosyaYolu);

            return new NekropsiFotografIndirModel
            {
                Veri = veri,
                ContentType = string.IsNullOrWhiteSpace(fotograf.ContentType) ? "application/octet-stream" : fotograf.ContentType,
                DosyaAdi = fotograf.DosyaAdi,
            };
        }

        private static List<NekropsiBulguKaydetRequest> ParseBulgular(string? bulgularJson)
        {
            if (string.IsNullOrWhiteSpace(bulgularJson))
                return new List<NekropsiBulguKaydetRequest>();

            try
            {
                return JsonSerializer.Deserialize<List<NekropsiBulguKaydetRequest>>(
                    bulgularJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<NekropsiBulguKaydetRequest>();
            }
            catch (JsonException)
            {
                throw new Exception("Bulgular verisi geçersiz formatta.");
            }
        }

        private async Task<List<NekropsiFotograf>> SaveFotograflarAsync(int raporId, List<Microsoft.AspNetCore.Http.IFormFile> dosyalar)
        {
            var klasorYolu = Path.Combine(ApiUrls.NekropsiFotograflariPath, raporId.ToString());
            Directory.CreateDirectory(klasorYolu);

            var kaydedilenler = new List<NekropsiFotograf>();

            foreach (var dosya in dosyalar)
            {
                if (dosya.Length == 0)
                    continue;

                var contentType = string.IsNullOrWhiteSpace(dosya.ContentType) ? "image/jpeg" : dosya.ContentType.ToLowerInvariant();

                if (!IzinVerilenFotografTipleri.Contains(contentType))
                    continue;

                var uzanti = Path.GetExtension(dosya.FileName);
                if (string.IsNullOrWhiteSpace(uzanti) || uzanti.Length > 10)
                    uzanti = ".jpg";

                var dosyaAdiDiskte = $"{Guid.NewGuid():N}{uzanti}";
                var goreliDosyaYolu = Path.Combine(raporId.ToString(), dosyaAdiDiskte);
                var tamDosyaYolu = Path.Combine(ApiUrls.NekropsiFotograflariPath, goreliDosyaYolu);

                await using (var stream = new FileStream(tamDosyaYolu, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await dosya.CopyToAsync(stream);
                }

                kaydedilenler.Add(new NekropsiFotograf
                {
                    NekropsiRaporuId = raporId,
                    DosyaAdi = string.IsNullOrWhiteSpace(dosya.FileName) ? dosyaAdiDiskte : Path.GetFileName(dosya.FileName),
                    GoreliDosyaYolu = goreliDosyaYolu,
                    ContentType = contentType,
                    BoyutByte = dosya.Length,
                    KayitZamani = DateTime.Now,
                });
            }

            return kaydedilenler;
        }

        private static string TamDosyaYolunuOlustur(string goreliDosyaYolu)
        {
            var anaKlasorTamYolu = Path.GetFullPath(ApiUrls.NekropsiFotograflariPath);
            var tamDosyaYolu = Path.GetFullPath(Path.Combine(anaKlasorTamYolu, goreliDosyaYolu));

            var anaKlasorOnEki = anaKlasorTamYolu.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!tamDosyaYolu.StartsWith(anaKlasorOnEki, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Dosya yolu izin verilen klasörün dışında.");

            return tamDosyaYolu;
        }
    }
}
