using System;
using System.Collections.Generic;
using System.Xml.Linq;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Mozilla.Util;
using twomindseye.Commando.Standard1Impl.Facets;

namespace twomindseye.Commando.Mozilla.Factories
{
    public sealed class ThunderbirdContactFactory : FacetFactoryWithIndex
    {
        const double FullMatch = 1.0 / 3;
        const double StartMatch = 1.0 / 6;
        const double ContainsMatch = 1.0 / 9;

        protected override Type[] GetFacetTypesImpl()
        {
            return new[] {typeof (ContactFacet)};
        }

        static double ApplyRelevance(ref double relevance, string contactPart, string word)
        {
            var apply = 0.0;

            if (contactPart != null)
            {
                if (contactPart == word)
                {
                    apply = FullMatch;
                }
                else if (contactPart.StartsWith(word, StringComparison.CurrentCultureIgnoreCase))
                {
                    apply = StartMatch;
                }
                else if (word.Length > 2 && contactPart.IndexOf(word, StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    apply = ContainsMatch;
                }
            }

            relevance += apply;

            return apply;
        }

        protected override IEnumerable<ParseResult> ParseImpl(ParseInput input, IEnumerable<FacetMoniker> indexEntries)
        {
            foreach (var entry in indexEntries)
            {
                var ci = ContactInfo.FromXml(entry.FactoryData);

                var relevance = 0.0;

                ParseRange firstNameRange = null;
                ParseRange lastNameRange = null;
                ParseRange emailRange = null;

                foreach (var term in input.Terms)
                {
                    if (firstNameRange == null)
                    {
                        if (ApplyRelevance(ref relevance, ci.FirstName, term.TextLower) > 0)
                        {
                            firstNameRange = term.Range;
                            continue;
                        }
                    }

                    if (lastNameRange == null)
                    {
                        if (ApplyRelevance(ref relevance, ci.LastName, term.TextLower) > 0)
                        {
                            lastNameRange = term.Range;
                            continue;
                        }
                    }

                    if (emailRange == null)
                    {
                        if (ApplyRelevance(ref relevance, ci.Email, term.TextLower) > 0)
                        {
                            emailRange = term.Range;
                            continue;
                        }
                    }
                }

                if (relevance == 0.0 ||
                    ((firstNameRange != null && lastNameRange != null) &&
                     lastNameRange.StartIndex <= firstNameRange.StartIndex))
                {
                    continue;
                }

                // determine the input range that caused the match
                ParseRange range;

                if (firstNameRange != null)
                {
                    if (lastNameRange != null)
                    {
                        range = firstNameRange.Union(lastNameRange);
                    }
                    else
                    {
                        range = firstNameRange;
                    }
                }
                else if (lastNameRange != null)
                {
                    range = lastNameRange;
                }
                else
                {
                    range = emailRange;
                }

                yield return new ParseResult(input, range, entry, relevance);
            }
        }

        protected override IEnumerable<FacetMoniker> EnumerateIndexImpl()
        {
            var reader = new ThunderbirdContactReader();

            if (!reader.Load())
            {
                yield break;
            }

            foreach (var contact in reader.Contacts)
            {
                var ci = new ContactInfo();
                ci.FirstName = reader.GetContactValue(contact, ThunderbirdContactReader.FirstNameOid);
                ci.LastName = reader.GetContactValue(contact, ThunderbirdContactReader.LastNameOid);
                ci.DisplayName = reader.GetContactValue(contact, ThunderbirdContactReader.DisplayNameOid);
                ci.Email = reader.GetContactValue(contact, ThunderbirdContactReader.EmailOid);

                if (ci.FirstName == null && ci.LastName == null && ci.DisplayName == null && ci.Email == null)
                {
                    continue;
                }

                if (ci.FirstName == null && ci.LastName == null && ci.DisplayName != null)
                {
                    var split = ci.DisplayName.Split(' ');

                    if (split.Length > 0)
                    {
                        ci.FirstName = split[0];
                    }

                    if (split.Length > 1)
                    {
                        ci.LastName = split[1];
                    }
                }

                if (ci.DisplayName == null)
                {
                    if (ci.FirstName != null)
                    {
                        if (ci.LastName != null)
                        {
                            ci.DisplayName = ci.FirstName + " " + ci.LastName;
                        }
                        else
                        {
                            ci.DisplayName = ci.FirstName;
                        }
                    }
                    else if (ci.LastName != null)
                    {
                        ci.DisplayName = ci.LastName;
                    }
                    else
                    {
                        ci.DisplayName = ci.Email;
                    }
                }

                if (ci.Email != null && ci.DisplayName != ci.Email)
                {
                    ci.DisplayName = ci.DisplayName + " (" + ci.Email + ")";
                }

                yield return new FacetMoniker(GetType(), typeof(ContactFacet), ci.ToXml(), ci.DisplayName, sourceName: "Thunderbird Contacts");
            }
        }

        public override FactoryIndexMode IndexMode
        {
            get { return FactoryIndexMode.Replace; }
        }

        public override bool ShouldUpdateIndex(FacetIndexReason indexReason, DateTime? lastUpdatedAt)
        {
            return indexReason == FacetIndexReason.Startup;
        }

        public override bool CanCreateFacet(FacetMoniker moniker)
        {
            try
            {
                // TODO: this is not fast
                ContactInfo.FromXml(moniker.FactoryData);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        public override IFacet CreateFacet(FacetMoniker moniker)
        {
            try
            {
                var ci = ContactInfo.FromXml(moniker.FactoryData);
                return new ContactFacet(ci.DisplayName, ci.Email);
            }
            catch (Exception)
            {
                return null;
            }
        }

        class ContactInfo
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string DisplayName { get; set; }
            public string Email { get; set; }

            static XAttribute AttrFor(string name, string value)
            {
                return new XAttribute(name, value ?? "");
            }

            static string ValueFor(XAttribute attribute)
            {
                return attribute.Value == "" ? null : attribute.Value;
            }

            public string ToXml()
            {
                return
                    new XElement("ContactInfo",
                        new XAttribute("Version", "1"),
                        AttrFor("FirstName", FirstName),
                        AttrFor("LastName", LastName),
                        AttrFor("DisplayName", DisplayName),
                        AttrFor("Email", Email))
                    .ToString();
            }

            public static ContactInfo FromXml(string xml)
            {
                var el = XElement.Parse(xml);

                return new ContactInfo
                {
                    FirstName = ValueFor(el.Attribute("FirstName")),
                    LastName = ValueFor(el.Attribute("LastName")),
                    DisplayName = ValueFor(el.Attribute("DisplayName")),
                    Email = ValueFor(el.Attribute("Email"))
                };
            }
        }
    }
}
