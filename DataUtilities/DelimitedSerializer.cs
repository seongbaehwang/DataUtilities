using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace DataUtilities
{
    public class DelimitedSerializer : IDelimitedSerializer
    {
        private static readonly ConcurrentDictionary<Type, IEnumerable<(PropertyInfo Property, string ColumnName)>>
            _propertyColumnNameMappingCache =
                new ConcurrentDictionary<Type, IEnumerable<(PropertyInfo Property, string ColumnName)>>();

        private readonly IDelimitedSerializerOption _option;

        protected IBooleanDateTimeFormatter BooleanDateTimeFormatter => _option.BooleanDateTimeFormatter;

        #region Constructors

        /// <summary>
        /// Initialize <see cref="DelimitedSerializer"/> with default option
        /// </summary>
        public DelimitedSerializer() : this(new DelimitedSerializerOption())
        {

        }

        /// <summary>
        /// Initialize <see cref="DelimitedSerializer"/> with the option <paramref name="option"/>
        /// </summary>
        /// <param name="option"></param>
        public DelimitedSerializer(IDelimitedSerializerOption option)
        {
            Validate(option);

            _option = option;
        }

        private void Validate(IDelimitedSerializerOption option)
        {
            if(option == null)
                throw new ArgumentNullException(nameof(option));

            if (string.IsNullOrEmpty(option.Delimiter))
            {
                throw new ArgumentException($"{nameof(IDelimitedSerializerOption.Delimiter)} is required");
            }
        }

        #endregion

        public string HeaderRow<T>() where T : class
        {
            var columnNames = string.IsNullOrEmpty(_option.TextQualifier)
                ? GetPropertyColumNameMapping<T>().Select(pc => pc.ColumnName)
                : GetPropertyColumNameMapping<T>().Select(pc => GetQualifiedString(pc.ColumnName));

            return string.Join(_option.Delimiter, columnNames);
        }

        /// <summary>
        /// Get concatenated string of string value of all public properties of <typeparam name="T"></typeparam>.
        /// or decorated with <see cref="ColumnAttribute"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string GetDelimitedString<T>(T obj) where T : class
        {
            return GetDelimitedString<T>(new[] { obj }).First();
        }

        public IEnumerable<string> GetDelimitedString<T>(IEnumerable<T> items, bool includeHeaderRow = false) where T : class
        {
            var props = GetPropertyColumNameMapping<T>().Select(m => m.Property).ToArray();

            var getStringValueMethod = GetStringValueMethod();

            if (includeHeaderRow)
            {
                yield return HeaderRow<T>();
            }

            foreach (var item in items)
            {
                yield return GetDelimitedString(item, props, getStringValueMethod);
            }
        }

        private string GetDelimitedString<T>(T obj, IReadOnlyList<PropertyInfo> properties, Func<object, Type, string> getStringValueMethod) where T : class
        {
            var values = new string[properties.Count];

            for (var i = 0; i < properties.Count; i++)
            {
                var pi = properties[i];

                values[i] = getStringValueMethod(obj == null ? null : pi.GetValue(obj), pi.PropertyType);
            }

            return string.Join(_option.Delimiter, values);
        }

        private Func<object, Type, string> GetStringValueMethod()
        {
            if (string.IsNullOrEmpty(_option.TextQualifier))
            {
                return GetStringValue;
            }

            if (_option.QualifyOnlyRequired)
            {
                return (obj, type) => QualifyStringIfRequired(GetStringValue(obj, type));
            }

            return (obj, type) => QualifyString(GetStringValue(obj, type));
        }

        private string GetQualifiedString(string value)
        {
            return _option.QualifyOnlyRequired ? QualifyStringIfRequired(value) : QualifyString(value);
        }

        private string QualifyStringIfRequired(string value)
        {
            return value.IndexOf(_option.Delimiter, StringComparison.Ordinal) < 0 ? value : QualifyString(value);
        }

        private string QualifyString(string value)
        {
            if (value.IndexOf(_option.TextQualifier, StringComparison.Ordinal) >= 0)
            {
                value = value.Replace(_option.TextQualifier, _option.TextQualifier + _option.TextQualifier);
            }

            return $"{_option.TextQualifier}{value}{_option.TextQualifier}";
        }

        protected virtual string GetStringValue(object value, Type valueType)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (BooleanDateTimeFormatter == null)
            {
                return value.ToString();
            }

            if (BooleanDateTimeFormatter.BooleanFormatter != null &&
                (valueType == typeof(bool) || Nullable.GetUnderlyingType(valueType) == typeof(bool)))
            {
                return BooleanDateTimeFormatter.BooleanFormatter((bool)value);
            }

            if (BooleanDateTimeFormatter.DateTimeFormatter != null &&
                (valueType == typeof(DateTime) || Nullable.GetUnderlyingType(valueType) == typeof(DateTime)))
            {
                return BooleanDateTimeFormatter.DateTimeFormatter((DateTime)value);
            }

            return value.ToString();
        }

        /// <summary>
        /// Get property and column name mapping in the order as they should be serialized
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// </returns>
        private IEnumerable<(PropertyInfo Property, string ColumnName)> GetPropertyColumNameMapping<T>()
        {
            var key = typeof(T);

            return _propertyColumnNameMappingCache.GetOrAdd(key, type
                =>
            {
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

                // sort by PropertyIndex as well to cover cases in which Order is not set or invalid, e.g., duplicate order
                return propertyColumnMapping
                    .OrderBy(pc => pc.Order)
                    .ThenBy(pc => pc.PropertyIndex)
                    .Select(pc => (pc.Property, pc.ColumnName))
                    .ToArray();
            });
        }
    }
}