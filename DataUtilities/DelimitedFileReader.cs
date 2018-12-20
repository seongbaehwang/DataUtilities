using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace DataUtilities
{
    public class DelimitedFileReader : IDataReader
    {
        private readonly TextReader _streamReader;

        private readonly bool _hasHeaderRow;

        private readonly char _delimiter;
        private readonly string _delimiterString;
        private readonly string[] _delimiterStringArray;

        private readonly char _qualifier;
        private readonly string _qualifierString;
        private readonly string _escapedQualifer;

        private Dictionary<int, string> _fieldIdNameDictionary;
        private Dictionary<string, int> _fieldNameIdDictionary;
        private string[] _currentData;
        private string _nextLine;

        private readonly Func<string, string[]> _splitMethod;
        private readonly StringParser _parser;

        #region constructors

        /// <summary>
        /// Initialize an instance of <see cref="DelimitedFileReader"/>
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="option">If not provided, default value of <see cref="DelimitedFileReaderOption"/>, i.e., CSV file with header row and without text qualifier</param>
        /// <param name="parser"></param>
        public DelimitedFileReader(string sourceFilePath, DelimitedFileReaderOption option = null, StringParser parser = null) : this(new StreamReader(sourceFilePath), option, parser)
        {
        }

        /// <summary>
        /// Initialize an instance of <see cref="DelimitedFileReader"/>
        /// </summary>
        /// <param name="textReader"></param>
        /// <param name="option">If not provided, default value of <see cref="DelimitedFileReaderOption"/>, i.e., CSV file with header row and without text qualifier</param>
        /// <param name="parser"></param>
        public DelimitedFileReader(TextReader textReader, DelimitedFileReaderOption option = null, StringParser parser = null)
        {
            _streamReader = textReader;
            _parser = parser ?? new StringParser();

            option = option ?? new DelimitedFileReaderOption();

            if (string.IsNullOrEmpty(option.Delimiter))
            {
                throw new ArgumentException("Delimiter is required.");
            }

            _hasHeaderRow = option.HasHeaderRow;

            // If both delimiter and qualifier are of type char, use SplitWithCharQualifer, twice faster
            if (option.Delimiter.Length == 1 && !string.IsNullOrEmpty(option.Qualifier) && option.Qualifier.Length == 1)
            {
                _delimiter = option.Delimiter[0];
                _qualifier = option.Qualifier[0];
                _qualifierString = new string(_qualifier, 1);
                _escapedQualifer = new string(_qualifier, 2);

                _splitMethod = SplitWithCharQualifer;
            }
            else
            {
                _delimiterString = option.Delimiter;
                _delimiterStringArray = new[] {_delimiterString};

                if (string.IsNullOrEmpty(option.Qualifier))
                {
                    _splitMethod = SplitWithNoQualifer;
                }
                else
                {
                    _splitMethod = SplitWithStringQualifer;
                    _qualifierString = option.Qualifier;
                    _escapedQualifer = _qualifierString + _qualifierString;
                }
            }

            Depth = 1;

            ParseHeaderRow(option.HasHeaderRow);
        }

        #endregion constructors

        private void ParseHeaderRow(bool hasHeaderRow)
        {
            var line = _streamReader.ReadLine();

            if (hasHeaderRow && string.IsNullOrWhiteSpace(line))
                throw new ArgumentException("The source file is empty");

            if(line == null) return;

            var columnNames = _splitMethod(line);

            if (hasHeaderRow)
            {
                _fieldIdNameDictionary = columnNames
                    .Select((val, idx) => new { Ordial = idx, Name = val.Trim() })
                    .ToDictionary(def => def.Ordial, def => def.Name);

                _nextLine = _streamReader.ReadLine();
            }
            else
            {
                _fieldIdNameDictionary = columnNames
                    .Select((val, idx) => new { Ordial = idx, Name = $"Column{idx}" })
                    .ToDictionary(def => def.Ordial, def => def.Name);

                _nextLine = line;
            }

            _fieldNameIdDictionary = _fieldIdNameDictionary
                .ToDictionary(kv => kv.Value, kv => kv.Key,
                    StringComparer.OrdinalIgnoreCase);

            FieldCount = _fieldIdNameDictionary.Count;
        }

        private string[] SplitWithNoQualifer(string line)
        {
            return line.Split(_delimiterStringArray, StringSplitOptions.None);
        }

        private string[] SplitWithCharQualifer(string line)
        {
            var isInsideQualifer = false;
            int startIndex = 1; // exclude qualifer

            var values = new List<string>();

            for (int charIndex = 0; charIndex < line.Length - 1; charIndex++)
            {
                if (line[charIndex].Equals(_qualifier))
                {
                    isInsideQualifer = !isInsideQualifer;
                }
                // current character is the actual delimiter, i.e., delimiter not inside qualifer 
                else if (!isInsideQualifer && line[charIndex].Equals(_delimiter))
                {
                    values.Add(line.Substring(startIndex, charIndex - startIndex - 1).Replace(_escapedQualifer, _qualifierString));
                    startIndex = charIndex + 2; // exclude qualifer
                }
            }

            if (startIndex < line.Length)
            {
                values.Add(line.Substring(startIndex, line.Length - startIndex - 1).Replace(_escapedQualifer, _qualifierString));
            }

            return values.ToArray();
        }

        private string[] SplitWithStringQualifer(string line)
        {
            var isInsideQualifer = false;
            int startIndex = _qualifierString.Length; // exclude qualifer

            var values = new List<string>();

            for (int charIndex = 0; charIndex < line.Length - 1; charIndex++)
            {
                if (line.Substring(charIndex, _qualifierString.Length).Equals(_qualifierString, StringComparison.Ordinal))
                {
                    isInsideQualifer = !isInsideQualifer;
                    charIndex += _qualifierString.Length - 1;
                }
                else if (!isInsideQualifer && line.Substring(charIndex, _delimiterString.Length).Equals(_delimiterString, StringComparison.Ordinal))
                {
                    values.Add(line.Substring(startIndex, charIndex - startIndex - _qualifierString.Length).Replace(_escapedQualifer, _qualifierString));

                    charIndex += _delimiterString.Length - 1;
                    startIndex = charIndex + _qualifierString.Length + 1;
                }
            }

            if (startIndex < line.Length)
            {
                values.Add(line.Substring(startIndex, line.Length - startIndex - _qualifierString.Length).Replace(_escapedQualifer, _qualifierString));
            }

            return values.ToArray();
        }

        //	private readonly Regex _regex;
        //	private void InitExpression(string delimiter, string qualifier, bool ignoreCase)
        //	{
        //		//	string _Statement = String.Format("{0}(?=(?:[^{1}]*{1}[^{1}]*{1})*(?![^{1}]*{1}))",
        //		//						Regex.Escape(delimiter), Regex.Escape(qualifier));
        //
        //		// After delimiter, there should be 
        //		var pattern = String.Format("{0}(?=(?:[^{1}]*{1}[^{1}]*{1})+$)",
        //							Regex.Escape(delimiter), Regex.Escape(qualifier));
        //
        //		var options = RegexOptions.Compiled;
        //		if (ignoreCase) options = options | RegexOptions.IgnoreCase;
        //
        //		_regex = new Regex(pattern, options);
        //	}

        public void Dispose()
        {
            _streamReader.Dispose();
            IsClosed = true;
        }

        /// <summary>
        /// Get column name of the column of index <param name="i"></param>.
        /// If there is no header row, column names are to be "Column" + index, e.g. Column0, Colum1 etc.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public virtual string GetName(int i)
        {
            return _fieldIdNameDictionary[i];
        }

        /// <summary>
        /// Always System.String, i.e., full name of <see cref="string"/>
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public virtual string GetDataTypeName(int i)
        {
            return typeof(string).FullName;
        }

        public virtual Type GetFieldType(int i)
        {
            return typeof(string);
        }

        object IDataRecord.GetValue(int i)
        {
            return _currentData[i];
        }

        public virtual string GetValue(int i)
        {
            return _currentData[i];
        }

        int IDataRecord.GetValues(object[] values)
        {
            var noOfInstance = values.Length <= _currentData.Length ? values.Length : _currentData.Length;

            Array.Copy(_currentData, values, noOfInstance);

            return noOfInstance;
        }

        public virtual string[] GetValues()
        {
            var values = new string[FieldCount];

            Array.Copy(_currentData, values, FieldCount);

            return values;
        }

        public int GetOrdinal(string name)
        {
            return _fieldNameIdDictionary[name];
        }

        public virtual bool GetBoolean(int i)
        {
            return _parser.BooleanParser(_currentData[i]);
        }

        public virtual byte GetByte(int i)
        {
            return byte.Parse(_currentData[i]);
        }

        public virtual long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public virtual char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public virtual long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public virtual Guid GetGuid(int i)
        {
            return Guid.Parse(_currentData[i]);
        }

        public virtual short GetInt16(int i)
        {
            return short.Parse(_currentData[i]);
        }

        public virtual int GetInt32(int i)
        {
            return int.Parse(_currentData[i]);
        }

        public virtual long GetInt64(int i)
        {
            return long.Parse(_currentData[i]);
        }

        public virtual float GetFloat(int i)
        {
            return float.Parse(_currentData[i]);
        }

        public virtual double GetDouble(int i)
        {
            return double.Parse(_currentData[i]);
        }

        public virtual string GetString(int i)
        {
            return _currentData[i];
        }

        public virtual decimal GetDecimal(int i)
        {
            return decimal.Parse(_currentData[i]);
        }

        public virtual DateTime GetDateTime(int i)
        {
            return _parser.DateTimeParser(_currentData[i]);
        }

        public virtual IDataReader GetData(int i)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Null if value is null or empty
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public virtual bool IsDBNull(int i)
        {
            return string.IsNullOrEmpty(_currentData[i]);
        }

        public virtual int FieldCount { get; private set; }

        object IDataRecord.this[int i] => GetValue(i);

        object IDataRecord.this[string name] => GetValue(GetOrdinal(name));

        public virtual void Close()
        {
            _streamReader.Close();
            IsClosed = true;
        }

        public virtual DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public virtual bool NextResult()
        {
            return false;
        }

        public virtual bool Read()
        {
            if (_nextLine == null)
            {
                _currentData = null;
                return false;
            }

            _currentData = _splitMethod(_nextLine);

            if (FieldCount != _currentData.Length)
            {
                var lineNumber = RecordsAffected + (_hasHeaderRow ? 2 : 1);
                throw new ApplicationException($"Malformed data found. (Line: {lineNumber}, Data: {_nextLine})");
            }

            RecordsAffected++;

            _nextLine = _streamReader.ReadLine();
            return true;
        }

        public virtual int Depth { get; }

        public virtual bool IsClosed { get; private set; }

        /// <summary>
        /// Number of lines read
        /// </summary>
        public virtual int RecordsAffected { get; private set; }

    }
}
