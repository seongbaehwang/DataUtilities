using System;

namespace DataUtilities.Tests
{
    public class ColumnNameAttributeTestObject
    {
        [ColumnName("Int")]
        public int IntValue { get; set; }

        [ColumnName("String")]
        public string StringValue { get; set; }

        public DateTime DateTimeValue { get; set; }
    }
}