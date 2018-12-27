namespace DataUtilities
{
    public class DelimitedSerializerOption : IDelimitedSerializerOption
    {
        /// <summary>
        /// The string to be used to separate columns.
        /// </summary>
        public virtual string Delimiter { get; set; } = ",";

        public virtual string TextQualifier { get; set; }

        /// <summary>
        /// Whether to qualify text only when text contains <see cref="Delimiter"/>
        /// </summary>
        public virtual bool QualifyOnlyRequired { get; set; }

        public virtual IBooleanDateTimeFormatter BooleanDateTimeFormatter { get; set; }
    }
}