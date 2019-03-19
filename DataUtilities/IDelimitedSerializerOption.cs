namespace SbhTech.DataUtilities
{
    public interface IDelimitedSerializerOption
    {
        /// <summary>
        /// The string to be used to separate columns.
        /// </summary>
        string Delimiter { get; set; }

        string TextQualifier { get; set; }

        /// <summary>
        /// Whether to qualify text only when text contains <see cref="Delimiter"/>
        /// </summary>
        bool QualifyOnlyRequired { get; set; }

        IBooleanDateTimeFormatter BooleanDateTimeFormatter { get; set; }
    }
}