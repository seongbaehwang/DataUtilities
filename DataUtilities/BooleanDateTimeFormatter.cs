using System;

namespace SbhTech.DataUtilities
{
    public class BooleanDateTimeFormatter : IBooleanDateTimeFormatter
    {
        public Func<bool, string> BooleanFormatter { get; set; }

        public Func<DateTime, string> DateTimeFormatter { get; set; }
    }
}