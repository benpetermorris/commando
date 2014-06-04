using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace twomindseye.Commando.API1.Parse
{
    [Serializable]
    [DebuggerDisplay("{Text}")]
    public sealed class ParseInput : IDeserializationCallback, IEnumerable<ParseInputTerm>
    {
        readonly List<ParseInputTerm> _terms;
        readonly int _insertionPointIndex;
        string _text;

        [NonSerialized] ReadOnlyCollection<ParseInputTerm> _termsReadOnly;
        [NonSerialized] string _compressedText;
        [NonSerialized] int[] _wst;
        [NonSerialized] int[] _wstr;
        [NonSerialized] string _textLower;

        private ParseInput()
        {
            _text = string.Empty;
            _terms = new List<ParseInputTerm>();
            _termsReadOnly = new ReadOnlyCollection<ParseInputTerm>(_terms);
        }

        public ParseInput(string text, int insertionPointIndex = -1)
            : this()
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            _insertionPointIndex = insertionPointIndex;

            Init(text);
        }

        public ParseInput(IEnumerable<ParseInputTerm> terms, int insertionPointIndex = -1)
            : this()
        {
            if (terms == null)
            {
                throw new ArgumentNullException("terms");
            }

            _insertionPointIndex = insertionPointIndex;

            foreach (var term in terms)
            {
                AddTerm(term);
            }

            InitAfterTerms();
        }

        public string Text { get { return _text; } }

        public string TextLower { get { return _textLower; } }

        public string CompressedText { get { return _compressedText; } }

        public string GetTextForRange(ParseRange range)
        {
            return Text.Substring(range.StartIndex, range.Length);
        }

        public ParseInputTerm GetTermAt(int originalIndex)
        {
            return _terms.Where(x => x.Range.Contains(originalIndex)).SingleOrDefault();
        }

        public ReadOnlyCollection<ParseInputTerm> Terms
        {
            get { return _termsReadOnly; }
        }

        public int InsertionPointIndex { get { return _insertionPointIndex; }}

        public int ToCompressed(int originalIndex)
        {
            if (originalIndex < 0 || originalIndex >= _wst.Length)
            {
                throw new ArgumentOutOfRangeException("originalIndex");
            }

            return originalIndex - _wst[originalIndex];
        }

        public int FromCompressed(int compressedIndex)
        {
            if (compressedIndex < 0 || compressedIndex >= _wst.Length)
            {
                throw new ArgumentOutOfRangeException("compressedIndex");
            }

            return compressedIndex + _wstr[compressedIndex];
        }

        public ParseRange ToCompressed(ParseRange originalRange)
        {
            return ParseRange.FromIndexes(
                ToCompressed(originalRange.StartIndex),
                ToCompressed(originalRange.EndIndex));
        }

        public ParseRange FromCompressed(ParseRange compressedRange)
        {
            return ParseRange.FromIndexes(
                FromCompressed(compressedRange.StartIndex),
                FromCompressed(compressedRange.EndIndex));
        }

        public ParseRange GetTermsRange(params ParseInputTerm[] terms)
        {
            if (terms.Any(x => x.Input != this))
            {
                throw new InvalidOperationException("all terms must be part of this ParseInput");
            }

            if (terms.Length == 0)
            {
                return null;
            }

            if (terms.Length == 1)
            {
                return terms[0].Range;
            }

            return terms.Skip(1).Aggregate(terms[0].Range, (accum, x) => accum.Union(x.Range));
        }

        void AddTerm(string text)
        {
            var mode = TermParseMode.Normal;

            Func<ParseInputTerm> createTerm = () => new ParseInputTerm(this, _terms.Count, text, _text.Length, mode);

            if (text.StartsWith("["))
            {
                mode = TermParseMode.History;
                text = text.Substring(1);
            }
            else
            {
                var factoryMatch = Regex.Match(text, @"^([a-z]+)/.+$", RegexOptions.IgnoreCase);

                if (factoryMatch.Success)
                {
                    var factoryAlias = factoryMatch.Groups[1].Value;
                    text = text.Substring(factoryAlias.Length + 1);
                    createTerm = () => new ParseInputTerm(this, _terms.Count, text, _text.Length, factoryAlias);
                }
            }

            if (_text.Length > 0)
            {
                _text += " ";
            }

            _terms.Add(createTerm());

            _text += text;
        }

        void AddTerm(ParseInputTerm copyFrom)
        {
            if (_text.Length > 0)
            {
                _text += " ";
            }

            _terms.Add(new ParseInputTerm(this, _terms.Count, _text.Length, copyFrom));

            _text += copyFrom.Text;
        }

        void Init(string text)
        {
            var startIdx = 0;
            var inTerm = false;
            var inTermQuoted = false;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];

                if (char.IsWhiteSpace(ch))
                {
                    if (inTerm && !inTermQuoted)
                    {
                        AddTerm(text.Substring(startIdx, i - startIdx));
                        inTerm = false;
                    }
                }
                else if (ch == '"' && inTermQuoted)
                {
                    AddTerm(text.Substring(startIdx, i - startIdx - 1));
                    inTerm = false;
                    inTermQuoted = false;
                }
                else if (!inTerm)
                {
                    startIdx = i;
                    inTerm = true;

                    if (ch == '"')
                    {
                        startIdx++;
                        inTermQuoted = true;
                    }
                }
            }

            if (inTerm && startIdx < text.Length)
            {
                AddTerm(text.Substring(startIdx, text.Length - startIdx));
            }

            InitAfterTerms();
        }

        void InitAfterTerms()
        {
            // generate compressed information
            var sb = new StringBuilder(_text.Length);
            _wst = new int[_text.Length];
            _wstr = new int[_text.Length];
            var wsum = 0;
            for (var i = 0; i < _text.Length; i++)
            {
                var ch = _text[i];
                _wst[i] = wsum;

                if (char.IsWhiteSpace(ch) || ch == '"')
                {
                    wsum++;
                }
                else
                {
                    _wstr[sb.Length] = wsum;
                    sb.Append(ch);
                }
            }
            _compressedText = sb.ToString();
        }

        public void OnDeserialization(object sender)
        {
            _textLower = _text.ToLower();
            _termsReadOnly = new ReadOnlyCollection<ParseInputTerm>(_terms);
            InitAfterTerms();
        }

        public IEnumerator<ParseInputTerm> GetEnumerator()
        {
            return _terms.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _terms.GetEnumerator();
        }
    }
}