using System;

namespace DataUtilities.Tests
{
    public class DelimitedColumnAttributeTestObject
    {
        [DelimitedColumn("Int")]
        public int IntValue { get; set; }

        /// <summary>
        /// Column name in a text file is the same as property name, i.e., StringValue
        /// </summary>
        [DelimitedColumn]
        public string StringValue { get; set; }

        public DateTime DateTimeValue { get; set; }
    }
}