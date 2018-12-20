using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;

namespace DataUtilities
{
    public class DataReaderConverter : IDataReaderConverter
    {
        public DataReaderConverter(IBooleanDateTimeParser parser)
        {
            _parser = parser;
        }

        /// <summary>
        /// Populate list of an instance of <see cref="TOutput"/> from <see cref="reader"/>
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public virtual IEnumerable<TOutput> ConvertToObjectList<TOutput>(IDataReader reader) where TOutput : class, new()
        {
            var colIndexes = new Dictionary<string, int>();
            for (var i = 0; i < reader.FieldCount; i++) colIndexes.Add(reader.GetName(i), i);

            var columnIndexPropertyMapping = GetColumnIndexPropertyMapping<TOutput>(colIndexes);

            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);

                var obj = ConvertToObject<TOutput>(values, columnIndexPropertyMapping);

                yield return obj;
            }
        }

        /// <summary>
        /// Convert instance of <see cref="IList{T}"/> into an object of type <see cref="TOutput"/>
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="data"></param>
        /// <param name="columnIndexPropertyMapping"></param>
        /// <returns></returns>
        public virtual TOutput ConvertToObject<TOutput>(IList<object> data, Dictionary<int, PropertyInfo> columnIndexPropertyMapping)
            where TOutput : class, new()
        {
            var obj = new TOutput();

            foreach (var cm in columnIndexPropertyMapping)
            {
                SetPropertyValue(obj, cm.Value, data[cm.Key]);
            }

            return obj;
        }

        /// <summary>
        /// Get column index and property of <see cref="TOutput"/> mapping.
        /// By default, property name is to be used for column name. Or column name can be set with <see cref="ColumnNameAttribute"/>
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="columnNameIndex"></param>
        /// <returns>
        /// A dictionary whose key is column index and value is property info
        /// </returns>
        public virtual Dictionary<int, PropertyInfo> GetColumnIndexPropertyMapping<TOutput>(IEnumerable<KeyValuePair<string, int>> columnNameIndex)
        {
            var type = typeof(TOutput);

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
        public virtual void SetPropertyValue(object instance, PropertyInfo pi, object value)
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
                && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (Nullable.GetUnderlyingType(propertyType) == valueType)
                {
                    pi.SetValue(instance, value);
                    return;
                }

                // when input is empty string
                if (valueType == typeof(string) && value.ToString().Trim() == "")
                {
                    pi.SetValue(instance, null);
                    return;
                }
            }

            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                pi.SetValue(instance, _parser.DateTimeParser(value.ToString()));
                return;
            }

            if (propertyType == typeof(bool) && valueType != typeof(bool))
            {
                pi.SetValue(instance, _parser.BooleanParser(value.ToString()));
                return;
            }

            var converter = TypeDescriptor.GetConverter(propertyType);
            var parsedValue = converter.ConvertFromInvariantString(value.ToString());

            pi.SetValue(instance, parsedValue);
        }

        private readonly IBooleanDateTimeParser _parser;
    }

}
