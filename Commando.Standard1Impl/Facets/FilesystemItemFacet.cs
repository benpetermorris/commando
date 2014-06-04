using System;
using System.IO;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Standard1.FacetTypes;

namespace twomindseye.Commando.Standard1Impl.Facets
{
    public sealed class FileSystemItemFacet : Facet, IFileSystemItemFacet
    {
        readonly string _path;
        readonly string _displayName;

        public FileSystemItemFacet(string path, string displayName = null)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            _path = path;
            _displayName = displayName ?? System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public override string DisplayName
        {
            get { return _displayName; }
        }

        public string Path
        {
            get { return _path; }
        }

        public bool Exists
        {
            get { return File.Exists(Path); }
        }
    }
}
