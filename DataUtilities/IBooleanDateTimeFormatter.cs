using System;

namespace SbhTech.DataUtilities
{
    public interface IBooleanDateTimeFormatter
    {
        Func<bool, string> BooleanFormatter { get; set; }

        Func<DateTime, string> DateTimeFormatter { get; set; }
    }
}