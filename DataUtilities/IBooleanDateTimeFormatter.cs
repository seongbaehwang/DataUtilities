using System;

namespace DataUtilities
{
    public interface IBooleanDateTimeFormatter
    {
        Func<bool, string> BooleanFormatter { get; set; }

        Func<DateTime, string> DateTimeFormatter { get; set; }
    }
}