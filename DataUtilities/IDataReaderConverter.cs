using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace SbhTech.DataUtilities
{
    public interface IDataReaderConverter
    {
        /// <summary>
        /// Populate list of an instance of <see cref="TOutput"/> from <see cref="reader"/>
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        IEnumerable<TOutput> ConvertToObjectList<TOutput>(IDataReader reader) where TOutput : class, new();

        /// <summary>
        /// Convert instance of <see cref="IList{T}"/> into an object of type <see cref="TOutput"/>
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="data"></param>
        /// <param name="columnIndexPropertyMapping"></param>
        /// <returns></returns>
        TOutput ConvertToObject<TOutput>(IList<object> data, Dictionary<int, PropertyInfo> columnIndexPropertyMapping)
            where TOutput : class, new();
    }
}