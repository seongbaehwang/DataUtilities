using System;

namespace SbhTech.DataUtilities
{
    public interface IBooleanDateTimeParser
    {
        Func<string, bool> BooleanParser { get; set; }

        Func<string, DateTime> DateTimeParser { get; set; }
    }
}