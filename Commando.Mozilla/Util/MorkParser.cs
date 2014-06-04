using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace twomindseye.Commando.Mozilla.Util
{
    public enum MorkErrors
    {
        NoError = 0,
        FailedToOpen,
        UnsupportedVersion,
        DefectedFormat
    }

    public enum MorkTerm
    {
        NoneTerm = 0,
        DictTerm,
        GroupTerm,
        TableTerm,
        RowTerm,
        CellTerm,
        CommentTerm,
        LiteralTerm
    }

    public class MorkDict : Dictionary<int, string>
    {
    }

    public class MorkCells : Dictionary<int, int>
    {
    }

    public class MorkRowMap : Dictionary<int, MorkCells>
    {
    }

    public class RowScopeMap : Dictionary<int, MorkRowMap>
    {
    }

    public class MorkTableMap : Dictionary<int, RowScopeMap>
    {
    }

    public class TableScopeMap : Dictionary<int, MorkTableMap>
    {
    }

    public sealed class MorkParser
    {
        const string MorkMagicHeader = "// <!-- <mdb:mork:z v=\"1.4\"/> -->";
        const string MorkDictColumnMeta = "<(a=c)>";

        readonly int _defaultScope;
        MorkDict _columns;
        MorkCells _currentCells;
        MorkErrors _error;
        string _morkData;
        int _morkPos;
        TableScopeMap _mork;
        int _nextAddValueId;
        NowParsing _nowParsing;
        MorkDict _values;

        public MorkParser(int defaultScope = 0x80)
        {
            InitVars();
            _defaultScope = defaultScope;
        }

        public static void test()
        {
            var p = new MorkParser();
            p.Open(@"c:\temp\abook.mab");

            foreach (var table in p.GetTables(0x80))
            {
                foreach (MorkRowMap rowScope in table.Value.Select(x => x.Value))
                {
                    foreach (var row in p.GetRows(rowScope))
                    {
                        string text = string.Join(",",
                                                  row.Select(cell => string.Format("{0}={1}", cell.Key, cell.Value)));
                        Debug.WriteLine(text);
                    }
                }
            }
        }

        public bool Open(string path)
        {
            InitVars();
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                _morkData = reader.ReadToEnd();
            }
            return Parse();
        }

        public MorkErrors Error()
        {
            return _error;
        }

        void InitVars()
        {
            _error = MorkErrors.NoError;
            _morkPos = 0;
            _nowParsing = NowParsing.NPValues;
            _currentCells = null;
            _columns = new MorkDict();
            _values = new MorkDict();
            _mork = new TableScopeMap();
            _nextAddValueId = 0x7fffffff;
        }

        bool Parse()
        {
            var result = true;
            var cur = NextChar();

            // Run over mork chars and parse each term
            while (result && cur != 0)
            {
                if (!IsWhiteSpace(cur))
                {
                    // Figure out what a term
                    switch (cur)
                    {
                        case '<':
                            // Dict
                            result = ParseDict();
                            break;
                        case '/':
                            // Comment
                            result = ParseComment();
                            break;
                        case '{':
                            result = ParseTable();
                            // Table
                            break;
                        case '[':
                            result = ParseRow(0, 0);
                            // Row
                            break;
                        case '@':
                            result = ParseGroup();
                            // Group
                            break;
                        default:
                            _error = MorkErrors.DefectedFormat;
                            result = false;
                            break;
                    }
                }

                // Get next char
                cur = NextChar();
            }

            return result;
        }

        static bool IsWhiteSpace(char c)
        {
            switch (c)
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                case '\f':
                    return true;
                default:
                    return false;
            }
        }

        char NextChar()
        {
            var cur = (char) 0;

            if (_morkPos < _morkData.Length)
            {
                cur = _morkData[_morkPos];
                _morkPos++;
            }

            return cur;
        }

        bool ParseDict()
        {
            var cur = NextChar();
            var result = true;
            _nowParsing = NowParsing.NPValues;

            while (result && cur != '>' && cur != 0)
            {
                if (!IsWhiteSpace(cur))
                {
                    switch (cur)
                    {
                        case '<':
                        {
                            if (_morkData.Substring(_morkPos - 1, MorkDictColumnMeta.Length) == MorkDictColumnMeta)
                            {
                                _nowParsing = NowParsing.NPColumns;
                                _morkPos += MorkDictColumnMeta.Length - 1;
                            }


                            break;
                        }
                        case '(':
                            result = ParseCell();
                            break;
                        case '/':
                            result = ParseComment();
                            break;
                    }
                }

                cur = NextChar();
            }

            return result;
        }

        bool ParseComment()
        {
            var cur = NextChar();
            if ('/' != cur)
            {
                return false;
            }

            while (cur != '\r' && cur != '\n' && cur != 0)
            {
                cur = NextChar();
            }

            return true;
        }

        bool ParseCell()
        {
            var bColumnOid = false;
            var bValueOid = false;
            var bColumn = true;
            var corners = 0;
            var column = string.Empty;
            var text = string.Empty;
            var cur = NextChar();

            // Process cell start with column (bColumn == true)
            while (cur != ')' && cur != 0)
            {
                switch (cur)
                {
                    case '^':
                        // Oids
                        corners++;
                        switch (corners)
                        {
                            case 1:
                                bColumnOid = true;
                                break;
                            case 2:
                                bColumn = false;
                                bValueOid = true;
                                break;
                            default:
                                text += cur;
                                break;
                        }

                        break;
                    case '=':
                        // From column to value
                        if (bColumn)
                        {
                            bColumn = false;
                        }
                        else
                        {
                            text += cur;
                        }
                        break;
                    case '\\':
                    {
                        // Get next two chars
                        var nextChar = this.NextChar();
                        if ('\r' != nextChar && '\n' != nextChar)
                        {
                            text += nextChar;
                        }
                        else
                        {
                            NextChar();
                        }
                    }
                        break;
                    case '$':
                    {
                        // Get next two chars
                        var hexChar = string.Empty;
                        hexChar += NextChar();
                        hexChar += NextChar();

                        var x = int.Parse(hexChar, NumberStyles.HexNumber);
                        text += x.ToString();
                    }
                        break;
                    default:
                        // Just a char
                        if (bColumn)
                        {
                            column += cur;
                        }
                        else
                        {
                            text += cur;
                        }
                        break;
                }

                cur = NextChar();
            }

            // Apply column and text
            var columnId = int.Parse(column, NumberStyles.HexNumber);

            if (NowParsing.NPRows != _nowParsing)
            {
                // Dicts
                if ("" != text)
                {
                    if (_nowParsing == NowParsing.NPColumns)
                    {
                        _columns[columnId] = text;
                    }
                    else
                    {
                        _values[columnId] = text;
                    }
                }
            }
            else
            {
                if ("" != text)
                {
                    // Rows

                    if (bValueOid)
                    {
                        _currentCells[columnId] = int.Parse(text, NumberStyles.HexNumber);
                    }
                    else
                    {
                        _nextAddValueId--;
                        _values[_nextAddValueId] = text;
                        _currentCells[columnId] = _nextAddValueId;
                    }
                }
            }

            return true;
        }

        bool ParseTable()
        {
            var result = true;
            var textId = string.Empty;
            int id = 0, scope = 0;
            var cur = NextChar();

            // Get id
            while (cur != '{' && cur != '[' && cur != '}' && cur != 0)
            {
                if (!IsWhiteSpace(cur))
                {
                    textId += cur;
                }

                cur = NextChar();
            }

            ParseScopeId(textId, ref id, ref scope);

            // Parse the table
            while (result && cur != '}' && cur != 0)
            {
                if (!IsWhiteSpace(cur))
                {
                    switch (cur)
                    {
                        case '{':
                            result = ParseMeta('}');
                            break;
                        case '[':
                            result = ParseRow(id, scope);
                            break;
                        case '-':
                        case '+':
                            break;
                        default:
                        {
                            var justId = string.Empty;
                            while (!IsWhiteSpace(cur) && cur != 0)
                            {
                                justId += cur;
                                cur = NextChar();

                                if (cur == '}')
                                {
                                    return result;
                                }
                            }

                            var justIdNum = 0;
                            var justScopeNum = 0;
                            ParseScopeId(justId, ref justIdNum, ref justScopeNum);
                            SetCurrentRow(scope, id, justScopeNum, justIdNum);
                        }
                            break;
                    }
                }

                cur = NextChar();
            }

            return result;
        }

        static void ParseScopeId(string textId, ref int id, ref int scope)
        {
            var pos = 0;

            if ((pos = textId.IndexOf(':')) >= 0)
            {
                var tId = textId.Substring(0, pos);
                var tSc = textId.Substring(pos + 1);

                if (tSc.Length > 1 && '^' == tSc[0])
                {
                    // Delete '^'
                    tSc = tSc.Remove(0, 1);
                }

                var mul = 1;
                if (tId[0] == '-')
                {
                    tId = tId.Substring(1);
                    mul = -1;
                }

                id = mul*int.Parse(tId, NumberStyles.HexNumber);
                scope = int.Parse(tSc, NumberStyles.HexNumber);
            }
            else
            {
                id = int.Parse(textId, NumberStyles.HexNumber);
            }
        }

        static TValue GetOrAdd<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key)
            where TValue : new()
        {
            TValue val;
            if (!dict.TryGetValue(key, out val))
            {
                dict[key] = val = new TValue();
            }
            return val;
        }

        void SetCurrentRow(int tableScope, int tableId, int rowScope, int rowId)
        {
            if (rowScope == 0)
            {
                rowScope = _defaultScope;
            }

            if (tableScope == 0)
            {
                tableScope = _defaultScope;
            }

            MorkTableMap ts = GetOrAdd(_mork, Math.Abs(tableScope));
            RowScopeMap t = GetOrAdd(ts, Math.Abs(tableId));
            MorkRowMap rs = GetOrAdd(t, Math.Abs(rowScope));
            _currentCells = GetOrAdd(rs, Math.Abs(rowId));
        }

        //	=============================================================
        //	parseRow

        bool ParseRow(int tableId, int tableScope)
        {
            var result = true;
            var textId = string.Empty;
            var id = 0;
            var scope = 0;
            var cur = NextChar();

            _nowParsing = NowParsing.NPRows;

            // Get id
            while (cur != '(' && cur != ']' && cur != '[' && cur != 0)
            {
                if (!IsWhiteSpace(cur))
                {
                    textId += cur;
                }

                cur = NextChar();
            }

            ParseScopeId(textId, ref id, ref scope);
            SetCurrentRow(tableScope, tableId, scope, id);

            // Parse the row
            while (result && cur != ']' && cur != 0)
            {
                if (!IsWhiteSpace(cur))
                {
                    switch (cur)
                    {
                        case '(':
                            result = ParseCell();
                            break;
                        case '[':
                            result = ParseMeta(']');
                            break;
                        default:
                            result = false;
                            break;
                    }
                }

                cur = NextChar();
            }

            return result;
        }

        bool ParseGroup()
        {
            return ParseMeta('@');
        }

        bool ParseMeta(char c)
        {
            var cur = NextChar();

            while (cur != c && cur != 0)
            {
                cur = NextChar();
            }

            return true;
        }

        public MorkTableMap GetTables(int tableScope)
        {
            MorkTableMap map;
            return _mork.TryGetValue(tableScope, out map) ? map : null;
        }

        public MorkRowMap GetRows(int rowScope, RowScopeMap table)
        {
            MorkRowMap map;
            return table.TryGetValue(rowScope, out map) ? map : null;
        }

        public string GetValue(int oid)
        {
            string value;
            return _values.TryGetValue(oid, out value) ? value : null;
        }

        public string GetColumn(int oid)
        {
            string column;
            return _columns.TryGetValue(oid, out column) ? column : null;
        }

        public int? GetColumnOid(string name)
        {
            return _columns.Where(x => x.Value == name).Select(x => (int?) x.Key).FirstOrDefault();
        }

        public IEnumerable<IEnumerable<KeyValuePair<int, string>>> GetRows(MorkRowMap map)
        {
            return map.Select(pair => GetCells(pair.Value));
        }

        public IEnumerable<KeyValuePair<int, string>> GetCells(MorkCells cells)
        {
            return cells.Select(pair => new KeyValuePair<int, string>(pair.Key, GetValue(pair.Value)));
        }

        #region Nested type: NowParsing

        enum NowParsing
        {
            NPColumns,
            NPValues,
            NPRows
        } ;

        #endregion
    }
}