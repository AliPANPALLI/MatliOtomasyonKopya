namespace WeighbridgeTracking.Domain.Helpers
{
    public static class PlakaHelper
    {
        // "34 abc-12" -> "34ABC12"
        // Plaka araması ve SAP eşleştirmesi bu formu kullanır.
        public static string Normalize(string? plaka)
        {
            return new string(
                (plaka ?? "")
                    .Where(char.IsLetterOrDigit)
                    .ToArray())
                .ToUpperInvariant();
        }
    }
}
