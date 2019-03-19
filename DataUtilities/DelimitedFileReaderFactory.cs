using System.IO;

namespace SbhTech.DataUtilities
{
    public class DelimitedFileReaderFactory : IDelimitedFileReaderFactory
    {
        /// <summary>
        /// Initialize an instance of <see cref="DelimitedFileReader"/>
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="option">If not provided, default value of <see cref="DelimitedFileReaderOption"/>, i.e., CSV file with header row and without text qualifier</param>
        /// <param name="parser"></param>
        public DelimitedFileReader Create(string sourceFilePath, DelimitedFileReaderOption option = null,
            BooleanDateTimeParser parser = null)
        {
            return new DelimitedFileReader(sourceFilePath, option, parser);
        }

        /// <summary>
        /// Initialize an instance of <see cref="DelimitedFileReader"/>
        /// </summary>
        /// <param name="textReader"></param>
        /// <param name="option">If not provided, default value of <see cref="DelimitedFileReaderOption"/>, i.e., CSV file with header row and without text qualifier</param>
        /// <param name="parser"></param>
        public DelimitedFileReader Create(TextReader textReader, DelimitedFileReaderOption option = null,
            BooleanDateTimeParser parser = null)
        {
            return new DelimitedFileReader(textReader, option, parser);
        }
    }
}