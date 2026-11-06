
using System.Globalization;

namespace WaterLineScada.Application.Helpers
{

    public static class SuHatRaporTarihHelper2
    {
        public static DateTime GetWeekStart2(DateTime date)
        {
            // Pazartesi = haftanın ilk günü.
            var gunFarki = ((int)date.DayOfWeek + 6) % 7;

            return date.Date.AddDays(-gunFarki);
        }

        public static DateTime GetPeriodStart2(
            DateTime date,
            string period)
        {
            return period switch
            {
                "Saatlik" => new DateTime(
                    date.Year,
                    date.Month,
                    date.Day,
                    date.Hour,
                    0,
                    0,
                    date.Kind),

                "Günlük" => date.Date,

                "Haftalık" => GetWeekStart2(date),

                "Aylık" => new DateTime(
                    date.Year,
                    date.Month,
                    1),

                "Yıllık" => new DateTime(
                    date.Year,
                    1,
                    1),

                _ => throw new ArgumentException(
                    "Geçersiz periyot",
                    nameof(period))
            };
        }

        public static DateTime GetNextPeriod2(
            DateTime date,
            string period)
        {
            return period switch
            {
                "Saatlik" => date.AddHours(1),
                "Günlük" => date.AddDays(1),
                "Haftalık" => date.AddDays(7),
                "Aylık" => date.AddMonths(1),
                "Yıllık" => date.AddYears(1),

                _ => throw new ArgumentException(
                    "Geçersiz periyot",
                    nameof(period))
            };
        }

        public static (
            DateTime Start,
            DateTime EndExclusive
        ) GetReportRange2(
            DateTime startDate,
            DateTime endDate,
            string period)
        {
            return period switch
            {
                "Saatlik" => (
                    GetPeriodStart2(startDate, period),
                    endDate),

                "Günlük" => (
                    startDate.Date,
                    endDate.Date.AddDays(1)),

                "Haftalık" => (
                    GetWeekStart2(startDate),
                    GetWeekStart2(endDate).AddDays(7)),

                "Aylık" => (
                    new DateTime(
                        startDate.Year,
                        startDate.Month,
                        1),

                    new DateTime(
                            endDate.Year,
                            endDate.Month,
                            1)
                        .AddMonths(1)),

                "Yıllık" => (
                    new DateTime(
                        startDate.Year,
                        1,
                        1),

                    new DateTime(
                        endDate.Year + 1,
                        1,
                        1)),

                _ => throw new ArgumentException(
                    "Geçersiz periyot",
                    nameof(period))
            };
        }

        public static string GetPeriodName2(
            DateTime date,
            string period)
        {
            var trCulture = CultureInfo.GetCultureInfo("tr-TR");

            return period switch
            {
                "Saatlik" =>
                    $"{date:dd MM yyyy} / " +
                    $"{date:HH}:00-" +
                    $"{date.AddHours(1):HH}:00",

                "Günlük" =>
                    date.ToString(
                        "dd MM yyyy dddd",
                        trCulture),

                "Haftalık" =>
                    $"{ISOWeek.GetYear(date)} / " +
                    $"Hafta {ISOWeek.GetWeekOfYear(date)}",

                "Aylık" =>
                    date.ToString(
                        "MMMM yyyy",
                        trCulture),

                "Yıllık" =>
                    date.ToString(
                        "yyyy",
                        trCulture),

                _ => throw new ArgumentException(
                    "Geçersiz periyot",
                    nameof(period))
            };
        }
    }
}
