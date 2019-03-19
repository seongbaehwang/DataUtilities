using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace SbhTech.DataUtilities
{
    public class DelimitedSerializer2
    {
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
        /// or decorated with <see cref="ColumnAttribute"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public string GetDelimitedString<T>(T obj, IDelimitedSerializerOption option = null) where T : class
        {
            return GetDelimitedString<T>(new[] {obj}, option).First();
        }

        public IEnumerable<string> GetDelimitedString<T>(IEnumerable<T> items, IDelimitedSerializerOption option = null, bool includeHeaderRow = false) where T : class
        {
            option = option ?? new DelimitedSerializerOption();

            var props = GetPropertyColumNameMapping<T>().Select(m => m.Property).ToArray();

            var getStringValueMethod = GetStringValueMethod(option);

            if (includeHeaderRow)
            {
                yield return HeaderRow<T>(option.Delimiter, option.TextQualifier, option.QualifyOnlyRequired);
            }

            foreach (var item in items)
            {
                yield return GetDelimitedString(item, props, option.Delimiter, getStringValueMethod);
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

        private Func<object, Type, string> GetStringValueMethod(IDelimitedSerializerOption option)
        {
            if (string.IsNullOrEmpty(option.TextQualifier))
            {
                return (obj, type) => GetStringValue(obj, type, option.BooleanDateTimeFormatter);
            }

            if (option.QualifyOnlyRequired)
            {
                return (obj, type) =>
                {
                    var value = GetStringValue(obj, type, option.BooleanDateTimeFormatter);

                    return QualifyStringIfRequired(value, option.TextQualifier, option.Delimiter);
                };
            }

            return (obj, type) => GetQualifiedString(GetStringValue(obj, type, option.BooleanDateTimeFormatter),
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

        protected virtual string GetStringValue(object value, Type valueType, IBooleanDateTimeFormatter booleanDateTimeFormatter)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (booleanDateTimeFormatter == null)
            {
                return value.ToString();
            }

            if (booleanDateTimeFormatter.BooleanFormatter != null && 
                (valueType == typeof(bool) || Nullable.GetUnderlyingType(valueType) == typeof(bool)))
            {
                return booleanDateTimeFormatter.BooleanFormatter((bool)value);
            }

            if (booleanDateTimeFormatter.DateTimeFormatter != null && 
                (valueType == typeof(DateTime) || Nullable.GetUnderlyingType(valueType) == typeof(DateTime)))
            {
                return booleanDateTimeFormatter.DateTimeFormatter((DateTime)value);
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
                            pi.GetCustomAttribute(typeof(ColumnAttribute)) as ColumnAttribute,
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