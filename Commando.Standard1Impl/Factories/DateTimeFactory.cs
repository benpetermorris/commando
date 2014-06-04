using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Standard1.FacetTypes;
using twomindseye.Commando.Standard1Impl.Facets;

namespace twomindseye.Commando.Standard1Impl.Factories
{
    /* 
     * Consider: where to put inference logic for datetime values? e.g. if I write "10am", and 
     * it's only 8am, I might infer "10am today". If it's 11am, I might infer "11am tomorrow". 
     * 
     * Also consider: the parser might look at several disconnected fragments to come up with a 
     * single facet. E.g. "tomorrow Jim 10am" contains a single date/time, interspersed with a 
     * contact.
     */

    public sealed class DateTimeFactory : FacetFactory
    {
        const string RegexFragmentAmPm = @"(?'ampm'a$|p$|am|pm)?";
        const string RegexFragmentTime = @"(?'hour'\d{1,2})(\:(?'minute'\d{2}))?";
        const string RegexFragmentTimeTogether = @"(?'hourminute'\d{4})";
        const string RegexFragmentRelativeDay = @"(?'relativeday'today|tomorrow|yesterday)";

        static readonly Regex[] s_regexes =
            new[]
            {
                new Regex(RegexFragmentTime + RegexFragmentAmPm),
                new Regex(RegexFragmentTimeTogether + RegexFragmentAmPm),
                new Regex(RegexFragmentRelativeDay),
            };

        protected override Type[] GetFacetTypesImpl()
        {
            return new[] {typeof (DateTimeFacet)};
        }

        protected override IEnumerable<ParseResult> ParseImpl(ParseInput input, ParseMode mode, IList<Type> facetTypes)
        {
            var builder = new DateTimeBuilder();

            ParseInputTerm termMin = null;
            ParseInputTerm termMax = null;
            ParseInputTerm termCurrent = null;

            Func<bool> updateTermIndexes =
                () =>
                {
                    if (termMin == null)
                    {
                        termMin = termCurrent;
                    }

                    if (termMax != null && termMax != termCurrent && termMax.Ordinal != termCurrent.Ordinal - 1)
                    {
                        return false;
                    }

                    termMax = termCurrent;

                    return true;
                };

            Func<Group, Action<string>, bool> parseGroup =
                (grp, parse) =>
                {
                    if (grp.Success)
                    {
                        if (!updateTermIndexes())
                        {
                            return false;
                        }

                        parse(grp.Value);
                    }

                    return true;
                };

            var gotAmPm = false;

            foreach (var t in input.Terms)
            {
                termCurrent = t;

                foreach (var match in 
                    from r in s_regexes
                    let m = r.Match(termCurrent.Text)
                    where m.Success
                    select m)
                {
                    if (!builder.Hour.HasValue && !parseGroup(match.Groups["hour"], val => builder.Hour = int.Parse(val)))
                    {
                        break;
                    }

                    if (!builder.Minute.HasValue && !parseGroup(match.Groups["minute"], val => builder.Minute = int.Parse(val)))
                    {
                        break;
                    }

                    if (!gotAmPm && !parseGroup(match.Groups["ampm"], 
                        val =>
                        {
                            gotAmPm = true;
                            builder.Hour += char.ToLower(val[0]) == 'p' ? 12 : 0;
                        }))
                    {
                        break;
                    }

                    //var hourminute = match.Groups["hourminute"];

                    //if (hourminute.Success)
                    //{}

                    if (!parseGroup(match.Groups["relativeday"],
                                    val =>
                                    {
                                        switch (val)
                                        {
                                            case "yesterday":
                                                builder.SetDayFrom(DateTime.Today.AddDays(-1));
                                                break;
                                            case "today":
                                                builder.SetDayFrom(DateTime.Today);
                                                break;
                                            case "tomorrow":
                                                builder.SetDayFrom(DateTime.Today.AddDays(1));
                                                break;
                                        }
                                    }))
                    {
                        break;
                    }
                }
            }

            if (builder.Type != 0)
            {
                var moniker = new FacetMoniker(GetType(), typeof(DateTimeFacet), builder.ToXml(),
                    builder.ToString(), extraData: FacetExtraData.BeginWith(typeof(IDateTimeFacet), "Type", builder.Type.ToString()), iconPath: null);
                var parseResult = new ParseResult(input, input.GetTermsRange(termMin, termMax), moniker, 1.0);
                
                return new[] {parseResult};
            }

            return null;
        }

        public override bool CanCreateFacet(FacetMoniker moniker)
        {
            try
            {
                DateTimeBuilder.FromXml(moniker.FactoryData);
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
                var builder = DateTimeBuilder.FromXml(moniker.FactoryData);
                return new DateTimeFacet(builder.DateTime, builder.Type);
            }
            catch (Exception)
            {
            }

            return null;
        }

        sealed class DateTimeBuilder
        {
            /// <summary>
            /// Four digits
            /// </summary>
            public int? Year { get; set; }
            /// <summary>
            /// 1-12
            /// </summary>
            public int? Month { get; set; }
            /// <summary>
            /// 1-31
            /// </summary>
            public int? Day { get; set; }
            /// <summary>
            /// 0-23
            /// </summary>
            public int? Hour { get; set; }
            /// <summary>
            /// 0-59
            /// </summary>
            public int? Minute { get; set; }

            public void SetDayFrom(DateTime dateTime)
            {
                Year = dateTime.Year;
                Month = dateTime.Month;
                Day = dateTime.Day;
            }

            public DateTime DateTime
            {
                get
                {
                    var now = DateTime.Now;
                    return new DateTime(Year ?? now.Year, Month ?? now.Month, Day ?? now.Day, Hour ?? 0, Minute ?? 0, 0);
                }
            }

            public DateTimeFacetType Type
            {
                get
                {
                    DateTimeFacetType rvl = 0;

                    if (Year.HasValue || Month.HasValue || Day.HasValue)
                    {
                        rvl |= DateTimeFacetType.Date;
                    }

                    if (Hour.HasValue || Minute.HasValue)
                    {
                        rvl |= DateTimeFacetType.Time;
                    }

                    return rvl;
                }
            }

            public override string ToString()
            {
                switch (Type)
                {
                    case DateTimeFacetType.Time:
                        return DateTime.ToString("t");
                    case DateTimeFacetType.Date:
                        return DateTime.ToString("d");
                    case DateTimeFacetType.Both:
                        return DateTime.ToString("g");
                }

                return "(empty)";
            }

            public string ToXml()
            {
                return new XElement("Root",
                                    new XAttribute("Year", Year ?? -1),
                                    new XAttribute("Month", Month ?? -1),
                                    new XAttribute("Day", Day ?? -1),
                                    new XAttribute("Hour", Hour ?? -1),
                                    new XAttribute("Minute", Minute ?? -1)).ToString();
            }

            public static DateTimeBuilder FromXml(string xml)
            {
                var el = XElement.Parse(xml);

                Func<XAttribute, int?> fromAttr =
                    attr => (attr == null || attr.Value == "-1") ? null : (int?)int.Parse(attr.Value);

                return new DateTimeBuilder
                       {
                           Year = fromAttr(el.Attribute("Year")),
                           Month = fromAttr(el.Attribute("Month")),
                           Day = fromAttr(el.Attribute("Day")),
                           Hour = fromAttr(el.Attribute("Hour")),
                           Minute = fromAttr(el.Attribute("Minute"))
                       };
            }
        }
    }
}