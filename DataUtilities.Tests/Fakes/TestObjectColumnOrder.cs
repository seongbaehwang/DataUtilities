using System;

namespace DataUtilities.Tests
{
    public class TestObjectColumnOrder
    {
        [DelimitedColumn(Order = 4)]
        public int IntValue { get; set; }

        [DelimitedColumn(Order = 3)]
        public string StringValue { get; set; }

        [DelimitedColumn(Order = 2)]
        public DateTime DateTimeValue { get; set; }

        [DelimitedColumn(Order = 1)]
        public bool BooleanValue { get; set; }
    }
}