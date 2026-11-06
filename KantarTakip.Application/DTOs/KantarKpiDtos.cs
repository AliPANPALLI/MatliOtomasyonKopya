using System;
using System.Collections.Generic;

namespace KantarTakip.Application.DTOs
{
    public sealed class KantarZiyaretKpiDto
    {
        public int GuvenlikId { get; set; }
        public string? ZiyaretciKayitId { get; set; }
        public string? Plaka { get; set; }

        public int? FirmaId { get; set; }
        public string? FirmaAdi { get; set; }

        public DateTime GirisZamani { get; set; }
        public DateTime? NumuneZamani { get; set; }
        public DateTime? IlkTartimZamani { get; set; }
        public DateTime? SonTartimZamani { get; set; }
        public string EslesmeDurumu { get; set; } = "ESLESMEDI";

        public double? SapTartimToplamDakika { get; set; }

        public double? SapFislerArasiBoslukDakika { get; set; }

        public bool IcSureDogrulandi { get; set; }

        public DateTime? ZiyaretEslesmeBitisSiniri { get; set; }
        public List<string> SapFisNolari { get; set; } = new();
        public int SapFisSayisi { get; set; }

        public string Durum { get; set; } = "";
        public string Aciklama { get; set; } = "";

        public double? GiristenNumuneyeDakika { get; set; }
        public double? NumunedenIlkTartimaDakika { get; set; }
        public double? GiristenIlkTartimaDakika { get; set; }
        public double? IlkTartimdanSonTartimaDakika { get; set; }
        public double? ToplamSureDakika { get; set; }

        public double? MevcutAsamadaBeklemeDakika { get; set; }

        public bool VeriTutarsizligi { get; set; }
        public List<string> Uyarilar { get; set; } = new();
        public List<KantarSapIslemDto> SapIslemleri { get; set; } = new();
    }

    public sealed class KantarFirmaKpiDto
    {
        public int? FirmaId { get; set; }
        public string? FirmaAdi { get; set; }

        public int ZiyaretSayisi { get; set; }
        public int NumuneBekleyenSayisi { get; set; }
        public int KantarBekleyenSayisi { get; set; }
        public int TartimdaSayisi { get; set; }
        public int TamamlananSayisi { get; set; }

        public double? OrtalamaNumuneTepkiDakika { get; set; }
        public double? OrtalamaKantaraGirisDakika { get; set; }
        public double? OrtalamaTartimlarArasiDakika { get; set; }
        public double? OrtalamaToplamSureDakika { get; set; }

        public double ToplamGerceklesenSureDakika { get; set; }
    }

    public sealed class KantarKpiRaporDto
    {
        public DateOnly Baslangic { get; set; }
        public DateOnly Bitis { get; set; }

        public DateTime HesaplamaZamani { get; set; }

        public int? KantarId { get; set; }

        public int ToplamZiyaret { get; set; }
        public int NumuneBekleyen { get; set; }
        public int KantarBekleyen { get; set; }
        public int Tartimda { get; set; }
        public int Tamamlanan { get; set; }

        public int EslesmeyenSapFisSayisi { get; set; }

        public List<KantarZiyaretKpiDto> Ziyaretler { get; set; } = new();
        public List<KantarFirmaKpiDto> Firmalar { get; set; } = new();

        public List<string> Uyarilar { get; set; } = new();
    }
}