using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace DataUtilities.Tests
{
    public class BooleanDateTimeParserFixture
    {
        public IBooleanDateTimeParser BooleanDateTimeParser { get; set; } = new BooleanDateTimeParser();
    }

    public class DataReaderConvertTests : IClassFixture<BooleanDateTimeParserFixture>
    {
        private readonly BooleanDateTimeParserFixture _booleanDateTimeParserFixture;

        public DataReaderConvertTests(BooleanDateTimeParserFixture booleanDateTimeParserFixture)
        {
            _booleanDateTimeParserFixture = booleanDateTimeParserFixture;
        }

        [Fact]
        public void ConvertToObjectList()
        {
            var sut = new DataReaderConverter(_booleanDateTimeParserFixture.BooleanDateTimeParser);

            var data = new[]
            {
                "IntValue,StringValue,DateTimeValue",
                "1,Jackie Chan,1954-04-07",
                "2,Angelina Jolie,1975-07-04"
            };

            var reader = new DelimitedFileReader(new StringReader(string.Join(Environment.NewLine, data)));

            var result = sut.ConvertToObjectList<TestObject>(reader).ToArray();

            Assert.Equal(2, result.Length);
            Assert.Equal("Jackie Chan", result[0].StringValue);
            Assert.Equal(new DateTime(1975, 7, 4), result[1].DateTimeValue);
        }

        [Fact]
        public void ConvertToObject()
        {
            var sut = new DataReaderConverter(_booleanDateTimeParserFixture.BooleanDateTimeParser);
            var type = typeof(TestObject);

            var columnIndexPropertyMapping = new Dictionary<int, PropertyInfo>
            {
                {0, type.GetProperty(nameof(TestObject.StringValue))},
                {1, type.GetProperty(nameof(TestObject.IntValue))},
                {2, type.GetProperty(nameof(TestObject.DateTimeValue))},
                {3, type.GetProperty(nameof(TestObject.BooleanValue))}
            };

            var obj = sut.ConvertToObject<TestObject>(new object[] { "Jackie Chan", 1, new DateTime(1970, 12, 25), true }, columnIndexPropertyMapping);

            Assert.Equal(1, obj.IntValue);
            Assert.Equal("Jackie Chan", obj.StringValue);
            Assert.Equal(new DateTime(1970, 12, 25), obj.DateTimeValue);
            Assert.True(obj.BooleanValue);
        }

        [Fact]
        public void GetColumnIndexPropertyMapping_UsingColumnNameAttribute()
        {
            var sut = new DataReaderConverter(_booleanDateTimeParserFixture.BooleanDateTimeParser);
            var columnNameIndex = new Dictionary<string, int>
            {
                {"String", 0},
                {"DateTime", 1},
                {"Int", 2}
            };

            var mapping = sut.GetColumnIndexPropertyMapping<ColumnNameAttributeTestObject>(columnNameIndex);

            // Only properties decorated with ColumnNameAttribute are mapped.
            Assert.Equal(2, mapping.Count);

            Assert.Equal("StringValue", mapping[0].Name);
            Assert.Equal("IntValue", mapping[2].Name);
        }
    }
}
