using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;

namespace twomindseye.Commando.Engine
{
    public sealed class CommandArgument : IEquatable<CommandArgument>
    {
        public static readonly CommandArgument Unspecified;
        static readonly ReadOnlyCollection<FacetMoniker> s_emptySuggestions;
        ReadOnlyCollection<FacetMoniker> _suggestions;

        static CommandArgument()
        {
            s_emptySuggestions = new ReadOnlyCollection<FacetMoniker>(new FacetMoniker[] { });
            Unspecified = new CommandArgument();
        }

        // for Unspecified
        private CommandArgument()
        {
            _suggestions = s_emptySuggestions;
        }

        internal CommandArgument(ParseResult parseResult, IEnumerable<FacetMoniker> orderedSuggestions = null)
        {
            if (parseResult == null)
            {
                throw new ArgumentNullException("parseResult");
            }

            Source = CommandArgumentSource.Parsed;
            FacetMoniker = parseResult.FacetMoniker;
            ParseRange = parseResult.Range;
            ParseRelevance = parseResult.Relevance;

            InitSuggestions(orderedSuggestions);
        }

        internal CommandArgument(FacetMoniker facetMoniker, IEnumerable<FacetMoniker> orderedSuggestions = null)
        {
            Source = CommandArgumentSource.Suggested;
            FacetMoniker = facetMoniker;
            InitSuggestions(orderedSuggestions);
        }

        internal CommandArgument(IEnumerable<FacetMoniker> orderedSuggestions, bool assignMoniker)
        {
            if (orderedSuggestions == null)
            {
                throw new ArgumentNullException("orderedSuggestions");
            }

            var masArray = orderedSuggestions.ToArray();

            if (masArray.Length == 0)
            {
                throw new ArgumentException("the sequence is empty", "orderedSuggestions");
            }

            Source = CommandArgumentSource.Suggested;

            if (assignMoniker)
            {
                FacetMoniker = masArray[0];
                masArray = masArray.Skip(1).ToArray();
            }

            InitSuggestions(masArray);
        }

        void InitSuggestions(IEnumerable<FacetMoniker> suggestions)
        {
            if (suggestions == null)
            {
                _suggestions = s_emptySuggestions;
                return;
            }

            var suggestionsArray = suggestions.ToArray();

            if (suggestionsArray.Any(x => x == null))
            {
                throw new ArgumentNullException("suggestions", "some elements are null");
            }

            _suggestions = new ReadOnlyCollection<FacetMoniker>(suggestionsArray);
        }

        public bool IsSpecified
        {
            get { return !ReferenceEquals(FacetMoniker, null); }
        }

        public CommandArgumentSource Source { get; private set; }

        public FacetMoniker FacetMoniker { get; private set; }

        public ReadOnlyCollection<FacetMoniker> Suggestions { get { return _suggestions; } }

        public ParseRange ParseRange { get; private set; }

        public double ParseRelevance { get; private set; }

        internal IFacet CreateFacet()
        {
            return IsSpecified ? FacetMoniker.CreateFacet() : null;
        }

        public override string ToString()
        {
            if (!IsSpecified)
            {
                return "Unspecified";
            }

            return FacetMoniker.ToString();
        }

        public bool Equals(CommandArgument other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(other.FacetMoniker, FacetMoniker);
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

            if (obj.GetType() != typeof(CommandArgument))
            {
                return false;
            }

            return Equals((CommandArgument)obj);
        }

        public override int GetHashCode()
        {
            if (!IsSpecified)
            {
                return 0;
            }

            return FacetMoniker.GetHashCode();
        }

        public static bool operator ==(CommandArgument left, CommandArgument right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CommandArgument left, CommandArgument right)
        {
            return !Equals(left, right);
        }
    }
}