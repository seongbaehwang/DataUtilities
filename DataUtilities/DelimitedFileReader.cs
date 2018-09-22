﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace DataUtilities
{
    public class DelimitedFileReader : IDataReader
    {
        private readonly TextReader _streamReader;

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

        #region constructors

        public DelimitedFileReader(string sourceFilePath, DelimitedFileReaderOption option) : this(new StreamReader(sourceFilePath), option)
        {
        }

        public DelimitedFileReader(TextReader textReader, DelimitedFileReaderOption option)
        {
            _streamReader = textReader;

            if (string.IsNullOrEmpty(option.Delimiter))
            {
                throw new ArgumentException("Delimiter is required.");
            }

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
            RecordsAffected = -1;

            ParseHeaderRow(option.HasHeaderRow);
        }

        #endregion constructors

        private void ParseHeaderRow(bool hasHeaderRow)
        {
            var line = _streamReader.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
                throw new ArgumentException("The source file is empty");

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

        public string GetName(int i)
        {
            return _fieldIdNameDictionary[i];
        }

        /// <summary>
        /// Always System.String
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public string GetDataTypeName(int i)
        {
            return typeof(string).FullName;
        }

        public Type GetFieldType(int i)
        {
            return typeof(string);
        }

        object IDataRecord.GetValue(int i)
        {
            return _currentData[i];
        }

        public string GetValue(int i)
        {
            return _currentData[i];
        }

        int IDataRecord.GetValues(object[] values)
        {
            var noOfInstance = values.Length <= _currentData.Length ? values.Length : _currentData.Length;

            Array.Copy(_currentData, values, noOfInstance);

            return noOfInstance;
        }

        public string[] GetValues()
        {
            var values = new string[FieldCount];

            Array.Copy(_currentData, values, FieldCount);

            return values;
        }

        public int GetOrdinal(string name)
        {
            return _fieldNameIdDictionary[name];
        }

        public bool GetBoolean(int i)
        {
            return bool.Parse(_currentData[i]);
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            return Guid.Parse(_currentData[i]);
        }

        public short GetInt16(int i)
        {
            return short.Parse(_currentData[i]);
        }

        public int GetInt32(int i)
        {
            return int.Parse(_currentData[i]);
        }

        public long GetInt64(int i)
        {
            return long.Parse(_currentData[i]);
        }

        public float GetFloat(int i)
        {
            return float.Parse(_currentData[i]);
        }

        public double GetDouble(int i)
        {
            return double.Parse(_currentData[i]);
        }

        public string GetString(int i)
        {
            return _currentData[i];
        }

        public decimal GetDecimal(int i)
        {
            return decimal.Parse(_currentData[i]);
        }

        public DateTime GetDateTime(int i)
        {
            return DateTime.Parse(_currentData[i]);
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Null if value is null or empty
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public bool IsDBNull(int i)
        {
            return string.IsNullOrEmpty(_currentData[i]);
        }

        public int FieldCount => _fieldIdNameDictionary.Count;

        object IDataRecord.this[int i] => GetValue(i);

        object IDataRecord.this[string name] => GetValue(GetOrdinal(name));

        public void Close()
        {
            _streamReader.Close();
            IsClosed = true;
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }

        public bool Read()
        {
            if (_nextLine == null)
            {
                _currentData = null;
                return false;
            }

            _currentData = _splitMethod(_nextLine);

            _nextLine = _streamReader.ReadLine();
            return true;
        }

        public int Depth { get; }

        public bool IsClosed { get; private set; }

        public int RecordsAffected { get; }

    }
}