using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Standard1.FacetTypes;
using twomindseye.Commando.Standard1Impl.Facets;

namespace twomindseye.Commando.Mozilla.Factories
{
    public sealed class FirefoxLocationsFactory : FacetFactory
    {
        protected override Type[] GetFacetTypesImpl()
        {
            return new[] {typeof (FileSystemItemFacet)};
        }

        protected override IEnumerable<ParseResult> ParseImpl(ParseInput input, ParseMode mode, IList<Type> facetTypes)
        {
            var index = input.TextLower.IndexOf("firefox downloads");

            if (index != -1)
            {
                var result = new ParseResult(input,
                    new ParseRange(index, 17), 
                    CreateMonikerOf<FileSystemItemFacet>("Firefox Downloads Folder", "downloads"), 
//                        FacetExtraData.BeginWith<IFileSystemItemFacet>("Type", "Folder")), 
                    1.0);

                return new[] {result};
            }

            // TODO: should suggest based on "firefox"

            return null;
        }

        public override bool CanCreateFacet(FacetMoniker moniker)
        {
            return true;
        }

        public override IFacet CreateFacet(FacetMoniker moniker)
        {
            switch (moniker.FactoryData)
            {
                case "downloads":
                    return new FileSystemItemFacet(@"c:\users\ben\downloads", "Firefox Downloads Folder");
            }

            return null;
        }
    }
}
