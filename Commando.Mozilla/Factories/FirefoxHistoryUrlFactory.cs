using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Mozilla.Util;
using twomindseye.Commando.Standard1Impl.Facets;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Mozilla.Factories
{
    public sealed class FirefoxHistoryUrlFactory : FacetFactoryWithIndex
    {
        protected override Type[] GetFacetTypesImpl()
        {
            return new[] {typeof (UrlFacet)};
        }

        public override bool CanCreateFacet(FacetMoniker moniker)
        {
            return true;
        }

        public override IFacet CreateFacet(FacetMoniker moniker)
        {
            return new UrlFacet(moniker.FactoryData, moniker.DisplayName);
        }

        protected override IEnumerable<ParseResult> ParseImpl(ParseInput input, IEnumerable<FacetMoniker> indexEntries)
        {
            return from entry in indexEntries
                   let fdparts = new[] { entry.DisplayName, entry.FactoryData }
                   from term in input.Terms
                   where term.Text.Length > 2
                   let startsWith = fdparts.Any(x => x.StartsWith(term.Text, StringComparison.OrdinalIgnoreCase))
                   let contains = startsWith == false && fdparts.Any(x => x.IndexOf(term.Text, StringComparison.OrdinalIgnoreCase) != -1)
                   where startsWith || contains
                   select new ParseResult(term, entry, startsWith ? 0.75 : 0.5);
        }

        protected override IEnumerable<FacetMoniker> EnumerateIndexImpl()
        {
            var profilesIniDirectory =
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Mozilla\\Firefox";

            var activeProfileDirectory = ProfileManager.GetActiveProfileDirectory(profilesIniDirectory);
            if (activeProfileDirectory == null)
            {
                yield break;
            }

            var placesPath = Path.Combine(activeProfileDirectory, "places.sqlite");
            if (!File.Exists(placesPath))
            {
                yield break;
            }

            using (var copy = new DisposableFileCopy(placesPath))
            using (var conn = new SQLiteConnection("Data Source=" + copy.TempCopyPath))
            {
                const string cmdText =
                    @"SELECT mp.url, mb.title AS mbtitle, mp.title AS mptitle, mp.visit_count
                        FROM moz_bookmarks AS mb 
                        INNER JOIN moz_places AS mp on mp.id = mb.fk";

                conn.Open();

                using (var cmd = new SQLiteCommand(cmdText, conn))
                foreach (var row in cmd.ExecuteReader().AsEnumerable())
                {
                    var url = row.ValOrDefault<string>(0);

                    if (url == null)
                    {
                        continue;
                    }

                    var title = row.ValOrDefault(1, url);

                    yield return new FacetMoniker(GetType(), typeof(UrlFacet), url, title, sourceName: "Firefox Bookmarks", iconPath: null);
                }
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
    }
}
