namespace CodingPortal2.Shared
{
    public static class TimeHelper
    {
        public static TimeSpan CalculateTimeSpanHoursMinutes(int totalHours, int totalMinutes)
        {
            int days = totalHours / 24;
            int hours = totalHours % 24;

            if (totalMinutes > 60)
            {
                hours += totalMinutes / 60;
            }

            int minutes = totalMinutes % 60;
            
            if (days > 365)
            {
                days = 365;
            }
            
            return new TimeSpan(days, hours, minutes, 0);
        }
        
        public static TimeSpan CalculateTimeSpanDaysHoursMinutes(int totalDays, int totalHours, int totalMinutes)
        {
            int days = totalDays;

            if (totalHours > 24)
            {
                days += totalHours / 24;
                totalHours %= 24;
            }

            if (totalMinutes > 60)
            {
                totalHours += totalMinutes / 60;
            }

            int hours = totalHours % 24;
            int minutes = totalMinutes % 60;

            if (days > 365)
            {
                days = 365;
            }

            return new TimeSpan(days, hours, minutes, 0);
        }
    }
}