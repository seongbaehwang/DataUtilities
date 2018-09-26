using System.ComponentModel;

namespace DataUtilities
{
    public class DelimitedFileReaderOption
    {
        public DelimitedFileReaderOption()
        {
            Delimiter = ",";
            HasHeaderRow = true;
        }

        /// <summary>
        /// Column delimiter. Default is comma
        /// </summary>
        [DefaultValue(",")]
        public string Delimiter { get; set; }

        /// <summary>
        /// Text qualifier, Default is null, i.e., no text qualifier
        /// </summary>
        [DefaultValue(null)]
        public string Qualifier { get; set; }

        /// <summary>
        /// Whether the first line is header row, i.e., contains column names, or not. Default is true, i.e. has header row
        /// </summary>
        [DefaultValue(true)]
        public bool HasHeaderRow { get; set; }
    }
}