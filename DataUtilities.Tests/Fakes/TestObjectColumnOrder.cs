using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataUtilities.Tests
{
    public class TestObjectColumnOrder
    {
        [Column(Order = 4)]
        public int IntValue { get; set; }

        [Column(Order = 3)]
        public string StringValue { get; set; }

        [Column(Order = 2)]
        public DateTime DateTimeValue { get; set; }

        [Column(Order = 1)]
        public bool BooleanValue { get; set; }
    }
}