using MongoDB.Bson;

namespace EnerjiIzleme.Infrastructure.Repositories.Core
{
    public static class BsonSafeExtensions
    {
        public static double ToDoubleSafe(this BsonValue v, double fallback = 0d)
        {
            if (v == null || v.IsBsonNull || v.IsBsonUndefined) return fallback;

            return v.BsonType switch
            {
                BsonType.Double => v.AsDouble,
                BsonType.Int32 => v.AsInt32,
                BsonType.Int64 => v.AsInt64,
                BsonType.Decimal128 => (double)Decimal128.ToDecimal(v.AsDecimal128),
                BsonType.String => double.TryParse(v.AsString, out var d) ? d : fallback,
                _ => fallback
            };
        }

        public static decimal ToDecimalSafe(this BsonValue v, decimal fallback = 0m)
        {
            if (v == null || v.IsBsonNull || v.IsBsonUndefined) return fallback;

            try
            {
                return v.BsonType switch
                {
                    BsonType.Decimal128 => Decimal128.ToDecimal(v.AsDecimal128),
                    // NaN/Infinity veya decimal sınırlarını (~7.9x10^28) aşan double değerler
                    // doğrudan cast edilince OverflowException fırlatır; bu durumda fallback dönülür.
                    BsonType.Double => double.IsNaN(v.AsDouble) || double.IsInfinity(v.AsDouble)
                        ? fallback
                        : (decimal)v.AsDouble,
                    BsonType.Int32 => v.AsInt32,
                    BsonType.Int64 => v.AsInt64,
                    BsonType.String => decimal.TryParse(v.AsString, out var d) ? d : fallback,
                    _ => fallback
                };
            }
            catch (OverflowException)
            {
                return fallback;
            }
        }

        public static DateTime? ToDateTimeSafe(this BsonValue v)
        {
            if (v == null || v.IsBsonNull || v.IsBsonUndefined) return null;

            if (v.BsonType == BsonType.DateTime) return v.ToUniversalTime();
            if (v.BsonType == BsonType.String && DateTime.TryParse(v.AsString, out var dt)) return dt.ToUniversalTime();
            return null;
        }
    }
}
