using System;
using System.Diagnostics;
using twomindseye.Commando.API1.Facets;

namespace twomindseye.Commando.API1.Parse
{
    [Serializable]
    [DebuggerDisplay("MatchedText = {MatchedText}, Range = ({Range})")]
    public sealed class ParseResult : IEquatable<ParseResult>
    {
        public const double ExactMatchRelevance = 1.0;

        public ParseResult(ParseInput input, ParseRange range, FacetMoniker facetMoniker, double relevance, 
            bool isSuggestion = false)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (range == null)
            {
                throw new ArgumentNullException("range");
            }

            if (facetMoniker == null)
            {
                throw new ArgumentNullException("facetMoniker");
            }

            if (relevance < 0 || relevance > 1)
            {
                throw new ArgumentOutOfRangeException("relevance", "must be between 0 and 1, inclusive");
            }

            Input = input;
            Range = range;
            CompressedRange = input.ToCompressed(Range);
            FacetMoniker = facetMoniker;
            Relevance = relevance;
            IsSuggestion = isSuggestion;
        }

        public ParseResult(ParseInputTerm term, FacetMoniker facetMoniker, double relevance, bool isSuggestion = false)
            : this(term.Input, term.Range, facetMoniker, relevance, isSuggestion)
        {
        }

        public ParseInput Input { get; private set; }
        public ParseRange Range { get; private set; }
        public ParseRange CompressedRange { get; private set; }
        public FacetMoniker FacetMoniker { get; private set; }
        public double Relevance { get; private set; }
        public bool IsSuggestion { get; private set; }

        public string MatchedText
        {
            get { return Input.Text.Substring(Range.StartIndex, Range.Length); }
        }

        public override string ToString()
        {
            return string.Format("'{0}' ({1})", MatchedText, FacetMoniker);
        }

        public bool Equals(ParseResult other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Equals(other.Range, Range) && Equals(other.FacetMoniker, FacetMoniker);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != typeof (ParseResult))
            {
                return false;
            }
            return Equals((ParseResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Range.GetHashCode()*397) ^ FacetMoniker.GetHashCode();
            }
        }

        public static bool operator ==(ParseResult left, ParseResult right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ParseResult left, ParseResult right)
        {
            return !Equals(left, right);
        }
    }
}
