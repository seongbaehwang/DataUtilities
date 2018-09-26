using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DataUtilities
{
    public static class DataParser
    {
        /// <summary>
        /// Populate list of an instance of <see cref="T"/> from <see cref="reader"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static IEnumerable<T> ConvertToObjectList<T>(IDataReader reader) where T : class, new()
        {
            var colIndexes = new Dictionary<string, int>();
            for (var i = 0; i < reader.FieldCount; i++) colIndexes.Add(reader.GetName(i), i);

            var propColIndexMapping = GetColumnIndexPropertyMapping<T>(colIndexes);

            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);

                var obj = ConvertRowToObject<T>(values, propColIndexMapping);

                yield return obj;
            }
        }

        /// <summary>
        /// Convert a row into an object of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <param name="columnIndexPropertyMapping"></param>
        /// <returns></returns>
        public static T ConvertRowToObject<T>(IList<object> row, Dictionary<int, PropertyInfo> columnIndexPropertyMapping)
            where T : class, new()
        {
            var obj = new T();

            foreach (var cm in columnIndexPropertyMapping)
            {
                SetPropertyValue(obj, cm.Value, row[cm.Key]);
            }

            return obj;
        }

        /// <summary>
        /// Get column index and property of <see cref="T"/> mapping.
        /// By default, property name is to be used for column name. Or column name can be set with <see cref="ColumnNameAttribute"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnNameIndex"></param>
        /// <returns>
        /// A dictionary whose key is column index and value is property info
        /// </returns>
        public static Dictionary<int, PropertyInfo> GetColumnIndexPropertyMapping<T>(IEnumerable<KeyValuePair<string, int>> columnNameIndex)
        {
            var type = typeof(T);

            var properties = type.GetProperties();

            // if there is any property with ColumnNameAttribute, populate properties with ColumnNameAttribute
            var colNameProp = properties
                .Select(pi => (Attr: pi.GetCustomAttribute(typeof(ColumnNameAttribute)) as ColumnNameAttribute, Property: pi))
                .Where(ap => ap.Attr != null)
                .Select(ap => (ColumnName: ap.Attr.Name, Property: ap.Property))
                .ToArray();

            if (colNameProp.Length == 0)
            {
                colNameProp = properties.Select(e => (ColumnName: e.Name, Property: e)).ToArray();
            }

            var mapping = from cp in colNameProp
                          join ci in columnNameIndex on cp.ColumnName.ToUpper() equals ci.Key.ToUpper()
                          select (Property: cp.Property, ColumnIndex: ci.Value);

            return mapping.ToDictionary(m => m.ColumnIndex, m => m.Property);
        }

        /// <summary>
        /// Set an object's property to a value.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="pi"></param>
        /// <param name="value"></param>
        public static void SetPropertyValue(object instance, PropertyInfo pi, object value)
        {
            if (value == null || DBNull.Value.Equals(value))
            {
                pi.SetValue(instance, null);
                return;
            }

            var valueType = value.GetType();
            var propertyType = pi.PropertyType;

            if (propertyType == valueType)
            {
                pi.SetValue(instance, value);
                return;
            }

            if (propertyType.IsGenericType
                && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                && Nullable.GetUnderlyingType(propertyType) == valueType)
            {
                pi.SetValue(instance, value);
                return;
            }

            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                var dt = DateTime.Parse(value.ToString());
                DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                pi.SetValue(instance, dt);
                return;
            }

            if (propertyType == typeof(bool) && valueType != typeof(bool))
            {
                pi.SetValue(instance, value.ToString().ToBoolean());
                return;
            }

            var converter = TypeDescriptor.GetConverter(propertyType);
            var parsedValue = converter.ConvertFromInvariantString(value.ToString());

            pi.SetValue(instance, parsedValue);
        }

        /// <summary>
        /// Convert a string value into an instance of boolean. value is case-insensitive.
        /// true for 1, y, yes, true,
        /// false for others
        /// </summary>
        /// <param name="value"></param>
        /// <returns>
        /// true for 1, y, yes, true,
        /// false for others
        /// </returns>
        public static bool ToBoolean(this string value)
        {
            return !string.IsNullOrWhiteSpace(value) && _booleanMatcher.IsMatch(value);
        }

        private static readonly Regex _booleanMatcher;

        static DataParser()
        {
            _booleanMatcher = new Regex("^(true|y|yes|1)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }

    }

}
