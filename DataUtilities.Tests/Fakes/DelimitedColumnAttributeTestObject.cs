using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataUtilities.Tests
{
    public class DelimitedColumnAttributeTestObject
    {
        [Column("Int")]
        public int IntValue { get; set; }

        /// <summary>
        /// Column name in a text file is the same as property name, i.e., StringValue
        /// </summary>
        [Column]
        public string StringValue { get; set; }

        public DateTime DateTimeValue { get; set; }
    }
}