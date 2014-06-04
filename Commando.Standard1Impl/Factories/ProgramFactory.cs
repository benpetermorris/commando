using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Standard1.FacetTypes;
using twomindseye.Commando.Standard1Impl.Facets;

namespace twomindseye.Commando.Standard1Impl.Factories
{
    public sealed class ProgramFactory : FacetFactoryWithIndex
    {
        protected override Type[] GetFacetTypesImpl()
        {
            return new[] {typeof (FileSystemItemFacet)};
        }

        protected override IEnumerable<ParseResult> ParseImpl(ParseInput input, IEnumerable<FacetMoniker> indexEntries)
        {
            var increment = 1.0/input.Terms.Count;

            foreach (var entry in indexEntries)
            {
                var relevance = 0.0;
                var lower = entry.DisplayName.ToLower();
                var range = new ParseRange(0, 0);

                foreach (var term in input.Terms)
                {
                    if (lower.StartsWith(term.TextLower))
                    {
                        relevance += increment*1.5;
                    }
                    else if (lower.Contains(term.TextLower))
                    {
                        relevance += increment;
                    }
                    else
                    {
                        // require constant progress
                        break;
                    }

                    range = range.Union(term.Range);

                    if (relevance > 0.0)
                    {
                        yield return new ParseResult(input, range, entry, Math.Min(1.0, relevance));
                    }
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

        protected override IEnumerable<FacetMoniker> EnumerateIndexImpl()
        {
            var userItems = Directory.EnumerateFileSystemEntries(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "*.lnk", SearchOption.AllDirectories);
            var commonItems = Directory.EnumerateFileSystemEntries(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                "*.lnk", SearchOption.AllDirectories);

            foreach (var path in userItems.Concat(commonItems))
            {
                //Icon icon;
                //string displayName;

                var moniker = new FacetMoniker(GetType(), 
                    typeof(FileSystemItemFacet), path, Path.GetFileNameWithoutExtension(path),
                    sourceName: "Start Menu", extraData: FacetExtraData.BeginWith(typeof(IFileSystemItemFacet), "Type", "Program"), iconPath: null);

                yield return moniker;
            }
        }

        public override bool CanCreateFacet(FacetMoniker moniker)
        {
            return true;
        }

        public override IFacet CreateFacet(FacetMoniker moniker)
        {
            return new FileSystemItemFacet(moniker.FactoryData, moniker.DisplayName);
        }
    }
}
