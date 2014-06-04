using System;
using System.Diagnostics;

namespace twomindseye.Commando.API1.Parse
{
    public enum TermParseMode
    {
        Normal,
        SpecificFactory,
        History
    }

    [Serializable]
    [DebuggerDisplay("'{Text}', Mode = {Mode}")]
    public sealed class ParseInputTerm
    {
        readonly ParseRange _range;

        internal ParseInputTerm(ParseInput input, int ordinal, string text, int startIndex, TermParseMode mode)
        {
            Input = input;
            Ordinal = ordinal;
            Text = text;
            TextLower = Text.ToLower();
            Mode = mode;
            _range = new ParseRange(startIndex, text.Length);
        }

        internal ParseInputTerm(ParseInput input, int ordinal, string text, int startIndex, string factoryAlias)
            : this(input, ordinal, text, startIndex, TermParseMode.SpecificFactory)
        {
            FactoryAlias = factoryAlias;
        }

        internal ParseInputTerm(ParseInput input, int ordinal, int startIndex, ParseInputTerm copyFrom)
            : this(input, ordinal, copyFrom.Text, startIndex, copyFrom.Mode)
        {
            FactoryAlias = copyFrom.FactoryAlias;
        }
        
        public ParseRange Range
        {
            get { return _range; }
        }

        public ParseInput Input { get; private set; }
        public int Ordinal { get; private set; }
        public string Text { get; private set; }
        public string TextLower { get; private set; }
        public TermParseMode Mode { get; private set; }
        public string FactoryAlias { get; private set; }
    }
}