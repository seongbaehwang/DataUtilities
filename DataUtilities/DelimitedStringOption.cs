using System;
using System.Globalization;

namespace DataUtilities
{
    public class DelimitedStringOption
    {
        /// <summary>
        /// The string to be used to separate columns.
        /// </summary>
        public virtual string ColumnDelimiter { get; set; } = ",";

        public virtual string TextQualifier { get; set; }

        /// <summary>
        /// Whether to qualify text only when text contains <see cref="DelimitedStringOption.ColumnDelimiter"/>
        /// </summary>
        public bool QualifyOnlyRequired { get; set; }

        public virtual Func<bool, string> BooleanFormatter { get; set; } = b => b.ToString();

        public virtual Func<DateTime, string> DateTimeFormatter { get; set; } = d => d.ToString(CultureInfo.InvariantCulture);
    }
}