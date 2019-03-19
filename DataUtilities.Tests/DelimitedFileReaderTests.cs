using System;
using System.IO;
using Xunit;

namespace SbhTech.DataUtilities.Tests
{
    public class DelimitedFileReaderTests
    {
        [Fact]
        public void NoDelimiter_ArgumentException()
        {
            var sr = new StringReader("");

            Assert.Throws<ArgumentException>(() => new DelimitedFileReader(sr,
                new DelimitedFileReaderOption {Delimiter = "", HasHeaderRow = false}));
        }

        [Fact]
        public void EmptyFileWithHasHeaderRowOptionOn_ArgumentException()
        {
            var sr = new StringReader("");

            Assert.Throws<ArgumentException>(() => new DelimitedFileReader(sr,
                new DelimitedFileReaderOption { Delimiter = ",", HasHeaderRow = true }));
        }

        [Fact]
        public void WithNoHeaderRow_ColumnNames_ColumnFollowedByColumnIndex()
        {
            var data = new[]
            {
                "1|Jackie Chan|2011-12-25"
            };

            var sr = new StringReader(string.Join(Environment.NewLine, data));
            var sut = new DelimitedFileReader(sr, new DelimitedFileReaderOption{Delimiter = "|", HasHeaderRow = false});

            sut.Read();

            Assert.Equal("Column0", sut.GetName(0));
            Assert.Equal("Column1", sut.GetName(1));
            Assert.Equal("Column2", sut.GetName(2));
        }

        [Fact]
        public void WithHeaderRow_ColumnNamesSourcedFromFirstRow()
        {
            var data = new[]
            {
                "Id|Name|DateOfBirth",
                "1|Jackie Chan|2011-12-25"
            };

            var sr = new StringReader(string.Join(Environment.NewLine, data));
            var sut = new DelimitedFileReader(sr, new DelimitedFileReaderOption { Delimiter = "|", HasHeaderRow = true });

            sut.Read();

            Assert.Equal("Id", sut.GetName(0));
            Assert.Equal("Name", sut.GetName(1));
            Assert.Equal("DateOfBirth", sut.GetName(2));
        }

        [Fact]
        public void DefaultOption_CommaSeparatedWithHeaderRow()
        {
            var data = new[]
            {
                "Id,Name,DateOfBirth",
                "1,Jackie Chan,2011-12-25"
            };

            var sr = new StringReader(string.Join(Environment.NewLine, data));
            var sut = new DelimitedFileReader(sr);

            sut.Read();

            Assert.Equal("Id", sut.GetName(0));
            Assert.Equal("Name", sut.GetName(1));

            Assert.Equal(1, sut.GetInt32(0));
            Assert.Equal("Jackie Chan", sut.GetValue(1));
            Assert.Equal(new DateTime(2011,12,25), sut.GetDateTime(sut.GetOrdinal("DateOfBirth")));
        }

        [Fact]
        public void CustomParser()
        {
            var data = new[]
            {
                "BooleanColumn,DateTimeColumn",
                "Y,12252018"
            };

            var sr = new StringReader(string.Join(Environment.NewLine, data));
            var parser = new BooleanDateTimeParser
            {
                BooleanParser = s=> s?.Trim().ToLower() == "y",
                DateTimeParser = s => DateTime.ParseExact(s, "MMddyyyy", null)
            };

            var sut = new DelimitedFileReader(sr, parser: parser);

            sut.Read();

            Assert.True(sut.GetBoolean(0));
            Assert.Equal(new DateTime(2018, 12, 25), sut.GetDateTime(1));
        }
    }
}
