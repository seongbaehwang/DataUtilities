using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SbhTech.DataUtilities
{
    public interface IDelimitedSerializer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        string HeaderRow<T>() where T : class;

        /// <summary>
        /// Get concatenated string of string value of all public properties of <typeparam name="T"></typeparam>.
        /// or properties decorated with <see cref="ColumnAttribute"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        string GetDelimitedString<T>(T obj) where T : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="includeHeaderRow"></param>
        /// <returns></returns>
        IEnumerable<string> GetDelimitedString<T>(IEnumerable<T> items, bool includeHeaderRow = false) where T : class;
    }
}