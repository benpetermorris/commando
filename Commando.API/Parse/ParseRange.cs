using System;
using System.Diagnostics;

namespace twomindseye.Commando.API1.Parse
{
    [Serializable]
    [DebuggerDisplay("({StartIndex}, {Length})")]
    public sealed class ParseRange : IEquatable<ParseRange>
    {
        public ParseRange(int startIndex, int length)
        {
            StartIndex = startIndex;
            Length = length;
        }

        public static ParseRange FromIndexes(int startIndex, int endIndex)
        {
            return new ParseRange(startIndex, endIndex - startIndex + 1);
        }

        public int StartIndex { get; private set; }

        public int Length { get; private set; }

        public int EndIndex
        {
            get { return StartIndex + Length - 1; }
        }

        public bool Contains(int index)
        {
            return index >= StartIndex && index <= EndIndex;
        }

        public bool Intersects(ParseRange other)
        {
            return (StartIndex >= other.StartIndex && StartIndex <= other.EndIndex) ||
                   (EndIndex >= other.StartIndex && EndIndex <= other.EndIndex) ||
                   (StartIndex < other.StartIndex && EndIndex > other.EndIndex);
        }

        public ParseRange Union(ParseRange other)
        {
            var min = Math.Min(StartIndex, other.StartIndex);
            var max = Math.Max(EndIndex, other.EndIndex);
            return new ParseRange(min, max - min + 1);
        }

        public bool Equals(ParseRange other)
        {
            return StartIndex == other.StartIndex &&
                   Length == other.Length;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ParseRange;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 0x42913d3b + StartIndex*Length;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", StartIndex, Length);
        }
    }
}