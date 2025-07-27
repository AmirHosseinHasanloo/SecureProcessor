using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Extentions
{
    public static class DateToShamsi
    {
        public static string ToShamsi(this DateTime value)
        {
            PersianCalendar pc = new PersianCalendar();

            return pc.GetYear(value) + "/" + pc.GetMonth(value).ToString("00") + "/" +
                   pc.GetDayOfMonth(value).ToString("00") + " Time" + pc.GetHour(value).ToString("00")
                   + ":" + pc.GetMinute(value).ToString("00") + ":" + pc.GetSecond(value).ToString("00");
        }
    }
}
