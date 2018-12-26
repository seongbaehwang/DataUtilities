using System;

namespace DataUtilities
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DelimitedColumnAttribute : Attribute
    {
        public DelimitedColumnAttribute()
        {
            
        }

        public DelimitedColumnAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of the column. If this is null or empty string, property name is to be used.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The order the column should appear in.
        /// Value should be a positive value. 0 and negative value is considered as not-assigned.
        /// This property is used only in serialization. 
        /// </summary>
        public int Order { get; set; }
    }
}