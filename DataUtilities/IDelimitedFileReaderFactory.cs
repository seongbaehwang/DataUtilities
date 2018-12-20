using System.IO;

namespace DataUtilities
{
    public interface IDelimitedFileReaderFactory
    {
        /// <summary>
        /// Initialize an instance of <see cref="DelimitedFileReader"/>
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="option">If not provided, default value of <see cref="DelimitedFileReaderOption"/>, i.e., CSV file with header row and without text qualifier</param>
        /// <param name="parser"></param>
        DelimitedFileReader Create(string sourceFilePath, DelimitedFileReaderOption option = null,
            BooleanDateTimeParser parser = null);

        DelimitedFileReader Create(TextReader textReader, DelimitedFileReaderOption option = null,
            BooleanDateTimeParser parser = null);
    }
}