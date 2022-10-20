using System;

namespace IotCommon
{
    public static class DateUtils
    {
        public static string CovertToString(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Returns Pacific time if VancouverTimeZone or PacificTimeZone is defined in the system
        /// Otherwise return UTC time.
        /// </summary>
        /// <param name="utcDate"></param>
        /// <returns></returns>
        public static DateTime ConvertUtcToPacificTime(DateTime utcDate)
        {
            var date = ConvertTimeFromUtc(utcDate, Constants.VancouverTimeZone);

            if (date != null)
                return (DateTime)date;

            date = ConvertTimeFromUtc(utcDate, Constants.PacificTimeZone);

            if (date != null)
                return (DateTime)date;

            return utcDate;
        }

        private static DateTime? ConvertTimeFromUtc(DateTime date, string timeZoneId)
        {
            try
            {
                var timezone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(date, timezone);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
        }

        public static DateTime ConvertPacificToUtcTime(DateTime pstDate)
        {
            var date = ConvertTimeToUtc(pstDate, Constants.VancouverTimeZone);

            if (date != null)
                return (DateTime)date;

            date = ConvertTimeToUtc(pstDate, Constants.PacificTimeZone);

            if (date != null)
                return (DateTime)date;

            return pstDate;
        }

        private static DateTime? ConvertTimeToUtc(DateTime date, string timeZoneId)
        {
            try
            {
                var timezone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeToUtc(date, timezone);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
        }

        public static (DateTime utcDateFrom, DateTime utcDateTo) GetUtcDateRange(DateTime pstDateFrom, DateTime pstDateTo)
        {
            pstDateFrom = pstDateFrom.Date;
            pstDateTo = pstDateTo.Date.AddDays(1).AddSeconds(-1);

            var utcDateFrom = ConvertPacificToUtcTime(pstDateFrom);
            var utcDateTo = ConvertPacificToUtcTime(pstDateTo);

            return (utcDateFrom, utcDateTo);
        }

        public static long ConvertPacificToUtcTotalMilliseconds(DateTime pstDate)
        {
            var date = ConvertPacificToUtcTime(pstDate);

            return (long)date.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        public static DateTime ConvertUtcTotalMillisecondsToPst(long utcMilliseconds)
        {
            var utcDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(utcMilliseconds);
            return ConvertUtcToPacificTime(utcDate);
        }

    }

}
