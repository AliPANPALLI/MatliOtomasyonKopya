namespace WeighbridgeTracking.Application.Exceptions
{
    // SAP servisine ulaşılamadığında veya SAP hata döndürdüğünde.
    // API katmanında 502 Bad Gateway olarak döner.
    public class SapEntegrasyonException : Exception
    {
        public SapEntegrasyonException(string message) : base(message)
        {
        }

        public SapEntegrasyonException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
