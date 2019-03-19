using System;

namespace SbhTech.DataUtilities
{
    public class BooleanDateTimeParser: IBooleanDateTimeParser
    {
        /// <summary>
        /// Default is <see cref="bool.Parse"/>
        /// </summary>
        public virtual Func<string, bool> BooleanParser { get; set; } = bool.Parse;

        /// <summary>
        /// Default is <see cref="DateTime.Parse"/>
        /// </summary>
        public virtual Func<string, DateTime> DateTimeParser { get; set; } = DateTime.Parse;
    }
}