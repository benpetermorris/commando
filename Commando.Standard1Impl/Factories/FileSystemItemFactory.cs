using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Standard1.FacetTypes;
using twomindseye.Commando.Standard1Impl.Facets;

namespace twomindseye.Commando.Standard1Impl.Factories
{
    public sealed class FileSystemItemFactory : FacetFactory
    {
        protected override Type[] GetFacetTypesImpl()
        {
            return new[] {typeof (FileSystemItemFacet)};
        }

        static string RecaseFilePath(string path)
        {
            try
            {
                if (!Regex.IsMatch(path, @"^[a-z]\:\\$", RegexOptions.IgnoreCase))
                {
                    path = path.TrimEnd('\\', '/');
                }

                var dir = Path.GetDirectoryName(path);
                var fn = Path.GetFileName(path);
                var results = Directory.GetFileSystemEntries(dir, fn);
                return results.Length == 1 ? results[0] : path;
            }
            catch
            {
            }

            return path;
        }

        protected override IEnumerable<ParseResult> ParseImpl(ParseInput input, ParseMode mode, IList<Type> facetTypes)
        {
            var startRegex = new Regex(@"\:\\");

            foreach (Match startMatch in startRegex.Matches(input.Text))
            {
                if (startMatch.Index == 0)
                {
                    continue;
                }

                var startIndex = startMatch.Index - 1;
                string best = null;
                var bestIsFile = false;

                Func<char, bool> isBreak = ch => char.IsWhiteSpace(ch) || ch == '\\';

                for (var idx = startIndex; idx <= input.Text.Length; idx++)
                {
                    if (idx == input.Text.Length || isBreak(input.Text[idx]))
                    {
                        var len = idx - startIndex;
                        var text = input.Text.Substring(startIndex, len);

                        if (File.Exists(text))
                        {
                            bestIsFile = true;
                            best = text;
                        }
                        else if (Directory.Exists(text))
                        {
                            bestIsFile = false;
                            best = text;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (best == null)
                {
                    continue;
                }

                var bestRange = new ParseRange(startIndex, best.Length);

                if (bestIsFile)
                {
                    yield return CreateParseResult(input, bestRange, false, RecaseFilePath(best), ParseResult.ExactMatchRelevance, false);
                }

                yield return CreateParseResult(input, bestRange, false, RecaseFilePath(best), ParseResult.ExactMatchRelevance, true);

                if (bestRange.EndIndex == input.Text.Length - 1 || (mode & ParseMode.Suggest) == 0 || input.Text[bestRange.EndIndex + 1] != '\\')
                {
                    continue;
                }

                var remain = new string(Enumerable.Skip<char>(input.Text, bestRange.EndIndex + 2).TakeWhile(ch => !char.IsWhiteSpace(ch)).ToArray());

                if (remain.Length > 0)
                {
                    var bestAndRemainRange = new ParseRange(startIndex, bestRange.Length + 1 + remain.Length);
                    string directory;

                    try
                    {
                        directory = RecaseFilePath(best);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (directory != null)
                    {
                        var query = Directory
                            .EnumerateFileSystemEntries(directory, remain + "*.*")
                            .Take(10)
                            .Select(x => Path.Combine(directory, Path.GetFileName(x)))
                            .Select(x => CreateParseResult(input, bestAndRemainRange, true, x, 0.5, Directory.Exists(x)));

                        foreach (var result in query)
                        {
                            yield return result;
                        }
                    }
                }
            }
        }

        ParseResult CreateParseResult(ParseInput input, ParseRange range, bool isSuggestion, string path, double relevance, bool isDirectory)
        {
            var type = "Folder";

            if (!isDirectory)
            {
                switch (Path.GetExtension(path).ToLower())
                {
                    case ".lnk":
                    case ".exe":
                        type = "Program";
                        break;
                    default:
                        type = "File";
                        break;
                }
            }

            var moniker = new FacetMoniker(GetType(), typeof(FileSystemItemFacet), path,
                path, extraData: FacetExtraData.BeginWith(typeof(IFileSystemItemFacet), "Type", type), iconPath: null);

            return new ParseResult(input, range, moniker, relevance, isSuggestion);
        }

        public override bool CanCreateFacet(FacetMoniker moniker)
        {
            return true;
        }

        public override IFacet CreateFacet(FacetMoniker moniker)
        {
            return new FileSystemItemFacet(moniker.FactoryData);
        }
    }
}
