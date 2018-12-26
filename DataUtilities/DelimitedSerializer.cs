using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataUtilities
{
    public class DelimitedSerializer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public string Serialize<T>(IEnumerable<T> items, DelimitedSerializerOption option) where T : class
        {
            var rows = new List<string>();

            if (option.IncludeHeaderRow)
            {
                rows.Add(HeaderRow<T>(option.ColumnDelimiter, option.TextQualifier, option.QualifyOnlyRequired));
            }

            return string.Join(option.RowDelimiter, rows.Concat(GetDelimitedString(items, option)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delimiter"></param>
        /// <param name="textQualifier"></param>
        /// <param name="qualifyOnlyRequired"></param>
        /// <returns></returns>
        public string HeaderRow<T>(string delimiter = ",", string textQualifier = null, bool qualifyOnlyRequired = false) where T : class
        {
            IEnumerable<string> columnNames;

            if (string.IsNullOrEmpty(textQualifier))
            {
                columnNames = GetPropertyColumNameMapping<T>().Select(pc => pc.ColumnName);
            }
            else if(qualifyOnlyRequired)
            {
                columnNames = GetPropertyColumNameMapping<T>().Select(pc => QualifyStringIfRequired(pc.ColumnName, textQualifier, delimiter));
            }
            else
            {
                columnNames = GetPropertyColumNameMapping<T>().Select(pc => GetQualifiedString(pc.ColumnName, textQualifier));
            }

            return string.Join(delimiter, columnNames);
        }

        /// <summary>
        /// Get concatenated string of string value of all public properties of <typeparam name="T"></typeparam>.
        /// or decorated with <see cref="DelimitedColumnAttribute"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public string GetDelimitedString<T>(T obj, DelimitedStringOption option = null) where T : class
        {
            return GetDelimitedString<T>(new[] {obj}, option).First();
        }

        public IEnumerable<string> GetDelimitedString<T>(IEnumerable<T> items, DelimitedStringOption option = null) where T : class
        {
            option = option ?? new DelimitedStringOption();

            var props = GetPropertyColumNameMapping<T>().Select(m => m.Property).ToArray();

            var getStringValueMethod = GetStringValueMethod(option);

            foreach (var item in items)
            {
                yield return GetDelimitedString(item, props, option.ColumnDelimiter, getStringValueMethod);
            }
        }

        private string GetDelimitedString<T>(T obj, IReadOnlyList<PropertyInfo> properties, string columnDelimiter, Func<object, Type, string> getStringValueMethod) where T : class
        {
            var values = new string[properties.Count];

            for (var i = 0; i < properties.Count; i++)
            {
                var pi = properties[i];

                values[i] = getStringValueMethod(obj == null ? null : pi.GetValue(obj), pi.PropertyType);
            }

            return string.Join(columnDelimiter, values);
        }

        private Func<object, Type, string> GetStringValueMethod(DelimitedStringOption option)
        {
            if (string.IsNullOrEmpty(option.TextQualifier))
            {
                return (obj, type) => GetStringValue(obj, type, option.BooleanFormatter, option.DateTimeFormatter);
            }

            if (option.QualifyOnlyRequired)
            {
                return (obj, type) =>
                {
                    var value = GetStringValue(obj, type, option.BooleanFormatter, option.DateTimeFormatter);

                    return QualifyStringIfRequired(value, option.TextQualifier, option.ColumnDelimiter);
                };
            }

            return (obj, type) => GetQualifiedString(GetStringValue(obj, type, option.BooleanFormatter, option.DateTimeFormatter),
                option.TextQualifier);
        }

        private string QualifyStringIfRequired(string value, string textQualifier, string delimiter)
        {
            return value.IndexOf(delimiter, StringComparison.Ordinal) < 0 ? value : GetQualifiedString(value, textQualifier);
        }

        private string GetQualifiedString(string value, string textQualifier)
        {
            if (value.IndexOf(textQualifier, StringComparison.Ordinal) >= 0)
            {
                value = value.Replace(textQualifier, textQualifier + textQualifier);
            }

            return $"{textQualifier}{value}{textQualifier}";
        }

        private string GetStringValue(object value, Type valueType, Func<bool, string> booleanFormatter, Func<DateTime, string> dateTimeFormatter)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (valueType == typeof(bool) ||
                valueType.IsGenericType
                && valueType.GetGenericTypeDefinition() == typeof(Nullable<>)
                && Nullable.GetUnderlyingType(valueType) == typeof(bool))
            {
                return booleanFormatter((bool)value);
            }

            if (valueType == typeof(DateTime) ||
                valueType.IsGenericType
                && valueType.GetGenericTypeDefinition() == typeof(Nullable<>)
                && Nullable.GetUnderlyingType(valueType) == typeof(DateTime))
            {
                return dateTimeFormatter((DateTime)value);
            }

            return value.ToString();
        }

        /// <summary>
        /// Get property and column name mapping in the order as they should be serialized
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// </returns>
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

            // no property decorated with DelimitedColumnAttribute.
            // same order as properties declared on their class
            if (propertyColumnMapping.Length == 0)
            {
                return properties
                    .Select(pi => (pi, pi.Name))
                    .ToArray();
            }

            // TODO: Duplicate column name, duplicate order, Validate???

            // not all decorated properties has Order set, use property index
            if (propertyColumnMapping.Any(cp => cp.Order <= 0))
            {
                return propertyColumnMapping
                    .OrderBy(pc => pc.PropertyIndex)
                    .Select(pc => (pc.Property, pc.ColumnName))
                    .ToArray();
            }

            // all decorated properties has Order set
            return propertyColumnMapping
                .OrderBy(pc => pc.Order)
                .Select(pc => (pc.Property, pc.ColumnName))
                .ToArray();
        }
    }
 }