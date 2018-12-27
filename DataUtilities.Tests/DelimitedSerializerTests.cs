using System;
using System.Linq;
using Xunit;

namespace DataUtilities.Tests
{
    public class DelimitedSerializerTests
    {
        public class Constructor
        {
            [Fact]
            public void NullOption_ThrowsException()
            {
                Assert.Throws<ArgumentNullException>(() => new DelimitedSerializer(null));
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            public void NoDelimiter_ThrowException(string delimiter)
            {
                Assert.Throws<ArgumentException>(() => new DelimitedSerializer(new DelimitedSerializerOption{Delimiter = delimiter}));
            }
        }

        public class HeaderRow
        {
            [Fact]
            public void NoDelimitedColumnAttribute_ReturnAllPublicProperties()
            {
                var sut = new DelimitedSerializer();

                var headerRow = sut.HeaderRow<TestObject>();

                Assert.Equal("IntValue,StringValue,DateTimeValue,BooleanValue", headerRow);
            }

            [Fact]
            public void WithDelimitedColumnAttribute_ReturnPublicPropertiesWithDelimitedColumnAttribute()
            {
                var sut = new DelimitedSerializer();

                var headerRow = sut.HeaderRow<DelimitedColumnAttributeTestObject>();

                Assert.Equal("Int,StringValue", headerRow);
            }

            [Fact]
            public void UserDefinedColumnOrder()
            {
                var sut = new DelimitedSerializer(new DelimitedSerializerOption { Delimiter = "|" });

                var headerRow = sut.HeaderRow<TestObjectColumnOrder>();

                Assert.Equal("BooleanValue|DateTimeValue|StringValue|IntValue", headerRow);
            }
        }
        
        [Fact]
        public void GetDelimitedString_Object()
        {
            var sut = new DelimitedSerializer();
            var dateValue = new DateTime(1972, 12, 25);
            // ReSharper disable once SpecifyACultureInStringConversionExplicitly
            var dateValueString = dateValue.ToString();

            var obj = new TestObject
            {
                BooleanValue = true,
                DateTimeValue = new DateTime(1972, 12, 25),
                IntValue = -100,
                StringValue = "Hello World"
            };

            var result = sut.GetDelimitedString(obj);

            Assert.Equal($"-100,Hello World,{dateValueString},True", result);
        }

        // ReSharper disable once InconsistentNaming
        public class GetDelimitedString_ListOfObject
        {
            private readonly DelimitedSerializer _sut;

            public GetDelimitedString_ListOfObject()
            {
                var option = new DelimitedSerializerOption
                {
                    BooleanDateTimeFormatter = new BooleanDateTimeFormatter
                    {
                        DateTimeFormatter = d => d.ToString("yyyy-MM-dd")
                    }
                };

                _sut = new DelimitedSerializer(option);
            }

            [Fact]
            public void WithHeaderRow()
            {
                var obj = new[]
                {
                    new TestObject
                    {
                        BooleanValue = true,
                        DateTimeValue = new DateTime(1972, 12, 25),
                        IntValue = -100,
                        StringValue = "Hello World"
                    }
                };

                var result = _sut.GetDelimitedString(obj, includeHeaderRow:true).ToArray();

                Assert.Equal(2, result.Length);
                Assert.Equal("IntValue,StringValue,DateTimeValue,BooleanValue", result[0]);
                Assert.Equal("-100,Hello World,1972-12-25,True", result[1]);
            }

            [Fact]
            public void WithNoHeaderRow()
            {
                var obj = new[]
                {
                    new TestObject
                    {
                        BooleanValue = true,
                        DateTimeValue = new DateTime(1972, 12, 25),
                        IntValue = -100,
                        StringValue = "Hello World"
                    },
                    new TestObject
                    {
                        BooleanValue = false,
                        DateTimeValue = new DateTime(2018, 12, 25),
                        IntValue = 1,
                        StringValue = "Hello World 2"
                    }
                };

                var result = _sut.GetDelimitedString<TestObject>(obj).ToArray();

                Assert.Equal(2, result.Length);
                Assert.Equal("-100,Hello World,1972-12-25,True", result[0]);
                Assert.Equal("1,Hello World 2,2018-12-25,False", result[1]);
            }

            [Fact]
            public void WithTextQualifier()
            {
                var obj = new[]
                {
                    new TestObject
                    {
                        BooleanValue = true,
                        DateTimeValue = new DateTime(1972, 12, 25),
                        IntValue = -100,
                        StringValue = "It's a wonderful day"
                    }
                };

                var sut = new DelimitedSerializer(new DelimitedSerializerOption
                {
                    TextQualifier = "'",
                    BooleanDateTimeFormatter = new BooleanDateTimeFormatter
                    {
                        DateTimeFormatter = d => d.ToString("yyyy-MM-dd")
                    }
                });

                var result = sut.GetDelimitedString(obj, includeHeaderRow: true).ToArray();

                Assert.Equal(2, result.Length);
                Assert.Equal("'IntValue','StringValue','DateTimeValue','BooleanValue'", result[0]);
                Assert.Equal("'-100','It''s a wonderful day','1972-12-25','True'", result[1]);
            }

            [Fact]
            public void QualifyIfRequired()
            {
                var obj = new[]
                {
                    new TestObject
                    {
                        BooleanValue = true,
                        DateTimeValue = new DateTime(1972, 12, 25),
                        IntValue = -100,
                        StringValue = "It's a wonderful day, it's Christmas"
                    }
                };

                var sut = new DelimitedSerializer(new DelimitedSerializerOption
                {
                    TextQualifier = "'",
                    QualifyOnlyRequired = true,
                    BooleanDateTimeFormatter = new BooleanDateTimeFormatter
                    {
                        DateTimeFormatter = d => d.ToString("yyyy-MM-dd")
                    }
                });

                var result = sut.GetDelimitedString(obj, includeHeaderRow: true).ToArray();

                Assert.Equal(2, result.Length);
                Assert.Equal("IntValue,StringValue,DateTimeValue,BooleanValue", result[0]);
                Assert.Equal("-100,'It''s a wonderful day, it''s Christmas',1972-12-25,True", result[1]);
            }
        }
    }
}
