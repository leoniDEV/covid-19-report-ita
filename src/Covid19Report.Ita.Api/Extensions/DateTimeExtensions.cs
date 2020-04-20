using System;
using System.Runtime.CompilerServices;

namespace Covid19Report.Ita.Api.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToStringWithSeparatorAndZone(this DateTime dateTime) => ToStringWithSeparatorAndZone(dateTime, null);

        public static string ToStringWithSeparatorAndZone(this DateTime dateTime, IFormatProvider? formatProvider) =>
            dateTime.Kind == DateTimeKind.Utc ? dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", formatProvider) : dateTime.ToString("yyyy-MM-ddThh:mm:ss", formatProvider);

        public static string ToStringWithSeparatorAndZone(this DateTimeOffset dateTimeOffset) => ToStringWithSeparatorAndZone(dateTimeOffset, null);
        public static string ToStringWithSeparatorAndZone(this DateTimeOffset dateTimeOffset, IFormatProvider? formatProvider) =>
            dateTimeOffset.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", formatProvider);
    }
}
