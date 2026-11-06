namespace KantarTakip.Domain.Exceptions
{
    // API katmanında HTTP durum kodlarına çevrilir:
    // KayitBulunamadi -> 404, IsKurali -> 409, GecersizIstek -> 400.

    public class KayitBulunamadiException : Exception
    {
        public KayitBulunamadiException(string message) : base(message)
        {
        }
    }

    public class IsKuraliException : Exception
    {
        public IsKuraliException(string message) : base(message)
        {
        }
    }

    public class GecersizIstekException : Exception
    {
        public GecersizIstekException(string message) : base(message)
        {
        }
    }
}
