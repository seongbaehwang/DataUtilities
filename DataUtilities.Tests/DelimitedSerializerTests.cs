using System;
using System.Linq;
using Xunit;

namespace DataUtilities.Tests
{
    public class DelimitedSerializerTests
    {
        [Fact]
        public void HeaderRow_NoDelimitedColumnAttribute()
        {
            var sut = new DelimitedSerializer();

            var headerRow = sut.HeaderRow<TestObject>();

            Assert.Equal("IntValue,StringValue,DateTimeValue,BooleanValue", headerRow);
        }

        [Fact]
        public void HeaderRow_DelimitedColumnAttribute()
        {
            var sut = new DelimitedSerializer();

            var headerRow = sut.HeaderRow<DelimitedColumnAttributeTestObject>();

            Assert.Equal("Int,StringValue", headerRow);
        }

        [Fact]
        public void HeaderRow_UserDefinedColumnOrder()
        {
            var sut = new DelimitedSerializer();

            var headerRow = sut.HeaderRow<TestObjectColumnOrder>("|");

            Assert.Equal("BooleanValue|DateTimeValue|StringValue|IntValue", headerRow);
        }

        [Fact]
        public void DelimitedString_Object()
        {
            var sut = new DelimitedSerializer();
            var obj = new TestObject
            {
                BooleanValue = true,
                DateTimeValue = new DateTime(1972, 12, 25),
                IntValue = -100,
                StringValue = "Hello World"
            };

            var option = new DelimitedStringOption
            {
                DateTimeFormatter = d=> d.ToString("yyyy-MM-dd")
            };

            var s = sut.GetDelimitedString(obj, option);

            Assert.Equal("-100,Hello World,1972-12-25,True", s);
        }
    }
}
