using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SbhTech.DataUtilities.Tests
{
    public class TestObjectWithColumnAttribute
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