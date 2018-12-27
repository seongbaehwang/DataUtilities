using System.Collections.Generic;

namespace DataUtilities
{
    public interface IDelimitedSerializer
    {
        string HeaderRow<T>() where T : class;

        /// <summary>
        /// Get concatenated string of string value of all public properties of <typeparam name="T"></typeparam>.
        /// or decorated with <see cref="DelimitedColumnAttribute"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        string GetDelimitedString<T>(T obj) where T : class;

        IEnumerable<string> GetDelimitedString<T>(IEnumerable<T> items, bool includeHeaderRow = false) where T : class;
    }
}