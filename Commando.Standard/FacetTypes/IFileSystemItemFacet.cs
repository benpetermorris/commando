using twomindseye.Commando.API1.Facets;

namespace twomindseye.Commando.Standard1.FacetTypes
{
    [ExtraDataDefinition("Type", "Program", "File", "Folder")]
    public interface IFileSystemItemFacet : IFacet
    {
        string Path { get; }
        bool Exists { get; }
    }
}
