using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DataUtilities
{
    public class DelimitedSerializerOption
    {
        /// <summary>
        /// The string to be used to separate columns.
        /// </summary>
        public string ColumnDelimiter { get; set; } = ",";

        /// <summary>
        /// The string to be used to separate rows.
        /// </summary>
        public string RowDelimiter { get; set; } = Environment.NewLine;
        
        public string TextQualifier { get; set; }

        public Func<bool, string> BooleanFormatter { get; set; } = b => b.ToString();

        public Func<DateTime, string> DateTimeFormatter { get; set; } = d => d.ToString(CultureInfo.InvariantCulture);
    }

    /// Represents a serializer that will serialize arbitrary objects to files with specific row and column separators.
    public class DelimitedSerializer
    {
        /// <summary>
        /// Serializes an object to a delimited file. Throws an exception if any of the property names, column names, or values contain either the <see cref="ColumnDelimiter"/> or the <see cref="RowDelimiter"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="items">A list of the items to serialize.</param>
        /// <returns>The serialized string.</returns>
        public string Serialize<T>(List<T> items)
        {
            return "";

            //if (string.IsNullOrEmpty(ColumnDelimiter))
            //{
            //    throw new ArgumentException($"The property '{nameof(ColumnDelimiter)}' cannot be null or an empty string.");
            //}

            //if (string.IsNullOrEmpty(RowDelimiter))
            //{
            //    throw new ArgumentException($"The property '{nameof(RowDelimiter)}' cannot be null or an empty string.");
            //}

            //var result = new ExtendedStringBuilder();

            //var properties = typeof(T).GetProperties()
            //    .Where(x => Attribute.IsDefined(x, typeof(DelimitedColumnAttribute)))
            //    .OrderBy(x => ((DelimitedColumnAttribute)x.GetCustomAttributes(typeof(DelimitedColumnAttribute), true)[0]).Order)
            //    .ThenBy(x => ((DelimitedColumnAttribute)x.GetCustomAttributes(typeof(DelimitedColumnAttribute), true)[0]).Name)
            //    .ThenBy(x => x.Name);

            //foreach (var property in properties)
            //{
            //    var attribute = (DelimitedColumnAttribute)property.GetCustomAttributes(typeof(DelimitedColumnAttribute), true)[0];

            //    var name = attribute.Name ?? property.Name;

            //    if (name.Contains(ColumnDelimiter))
            //    {
            //        throw new ArgumentException($"The column name string '{name}' contains an invalid character: '{ColumnDelimiter}'.");
            //    }
            //    if (name.Contains(RowDelimiter))
            //    {
            //        throw new ArgumentException($"The column name string '{name}' contains an invalid character: '{RowDelimiter}'.");
            //    }

            //    if (result.Length > 0)
            //    {
            //        result += ColumnDelimiter;
            //    }

            //    result += name;
            //}

            //foreach (var item in items)
            //{
            //    var row = new ExtendedStringBuilder();

            //    foreach (var property in properties)
            //    {
            //        var value = property.GetValue(item).ToString();

            //        if (value.Contains(ColumnDelimiter))
            //        {
            //            throw new ArgumentException($"The property value string '{value}' contains an invalid character: '{ColumnDelimiter}'.");
            //        }
            //        if (value.Contains(RowDelimiter))
            //        {
            //            throw new ArgumentException($"The property value string '{value}' contains an invalid character: '{RowDelimiter}'.");
            //        }

            //        if (row.Length > 0)
            //        {
            //            row += ColumnDelimiter;
            //        }

            //        row += value;
            //    }

            //    result += RowDelimiter;
            //    result += row;
            //}

            //return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public string HeaderRow<T>(string delimiter = ",") where T: class 
        {
            return string.Join(delimiter, GetPropertyColumNameMapping<T>().Select(pc => pc.ColumnName));
        }

        public IEnumerable<string> GetDelimitedString<T>(IEnumerable<T> items, DelimitedSerializerOption option = null) where T: class
        {
            option = option ?? new DelimitedSerializerOption();

            var props = GetPropertyColumNameMapping<T>().Select(m => m.Property).ToArray();

            foreach (var item in items)
            {
                yield return GetDelimitedString(item, props, option); ;
            }
        }

        public string GetDelimitedString<T>(T obj, DelimitedSerializerOption option) where T: class
        {
            var mapping = GetPropertyColumNameMapping<T>();

            return GetDelimitedString(obj, mapping.Select(m => m.Property).ToArray(), option);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="properties"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        private string GetDelimitedString<T>(T obj, PropertyInfo[] properties, DelimitedSerializerOption option) where T: class
        {
            var values = new object[properties.Length];

            for (var i = 0; i < properties.Length; i++)
            {
                var pi = properties[i];
                var propertyType = pi.PropertyType;
                var value = pi.GetValue(obj);

                if (value == null || DBNull.Value.Equals(value))
                {
                    values[i] = value;
                    continue;
                }

                if (propertyType == typeof(bool) ||
                    propertyType.IsGenericType
                    && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && Nullable.GetUnderlyingType(propertyType) == typeof(bool))
                {
                    values[i] = option.BooleanFormatter((bool) value);
                    continue;
                }

                if (propertyType == typeof(DateTime) ||
                    propertyType.IsGenericType
                     && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                     && Nullable.GetUnderlyingType(propertyType) == typeof(DateTime))
                {
                    values[i] = option.DateTimeFormatter((DateTime)value);
                    continue;
                }

                values[i] = value;
            }

            return string.Join(option.ColumnDelimiter, values);
        }

        private string GetStringValue(object obj, PropertyInfo pi, Func<bool, string> booleanFormatter, Func<DateTime, string> dateTimeFormatter)
        {
            var value = pi.GetValue(obj);

            if (value == null || DBNull.Value.Equals(value))
            {
                return string.Empty;
            }

            if (pi.PropertyType == typeof(bool) ||
                pi.PropertyType.IsGenericType
                && pi.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                && Nullable.GetUnderlyingType(pi.PropertyType) == typeof(bool))
            {
                return booleanFormatter((bool)value);
            }

            if (pi.PropertyType == typeof(DateTime) ||
                pi.PropertyType.IsGenericType
                && pi.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                && Nullable.GetUnderlyingType(pi.PropertyType) == typeof(DateTime))
            {
                return dateTimeFormatter((DateTime)value);
            }

            return value.ToString();
        }

        private (PropertyInfo Property, string ColumnName)[] GetPropertyColumNameMapping<T>()
        {
            var type = typeof(T);

            var properties = type.GetProperties();

            // if there is any property with DelimitedColumnAttribute, serialize properties with DelimitedColumnAttribute
            (PropertyInfo Property, int PropertyIndex, string ColumnName, int Order)[] propertyColumnMapping =
                properties
                    .Select(
                        (pi, idx) => (Attr:
                            pi.GetCustomAttribute(typeof(DelimitedColumnAttribute)) as DelimitedColumnAttribute,
                            Property:
                            pi, PropertyIndex: idx))
                    .Where(ap => ap.Attr != null)
                    .Select(ap => (ap.Property, ap.PropertyIndex, ap.Attr.Name ?? ap.Property.Name, ap.Attr.Order))
                    .ToArray();

            if (propertyColumnMapping.Length == 0)
            {
                return properties
                    .Select(pi => (pi, pi.Name))
                    .ToArray();
            }

            if (propertyColumnMapping.Any(cp => cp.Order <= 0))
            {
                return propertyColumnMapping
                    .OrderBy(pc => pc.PropertyIndex)
                    .Select(pc => (pc.Property, pc.ColumnName))
                    .ToArray();
            }

            return propertyColumnMapping
                .OrderBy(pc => pc.Order)
                .Select(pc => (pc.Property, pc.ColumnName))
                .ToArray();
        }
    }
}