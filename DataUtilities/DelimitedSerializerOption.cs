using System;

namespace DataUtilities
{
    public class DelimitedSerializerOption : DelimitedStringOption
    {
        /// <summary>
        /// Whether to include header row or not. Default is true
        /// </summary>
        public virtual bool IncludeHeaderRow { get; set; } = true;

        /// <summary>
        /// The string to be used to separate rows.
        /// </summary>
        public virtual string RowDelimiter { get; set; } = Environment.NewLine;
    }
}