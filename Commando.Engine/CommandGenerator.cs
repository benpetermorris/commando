using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using twomindseye.Commando.Util;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.EngineFacets;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Engine.DB;
using twomindseye.Commando.Engine.Load;
using twomindseye.Commando.Engine.Extension;

namespace twomindseye.Commando.Engine
{
    /// <summary>
    /// Generates all possible commands (including parameters) that can be invoked from
    /// the specified command text. Does not rank.
    /// </summary>
    public static class CommandGenerator
    {
        public static CommandGeneratorResult GenerateCommands(string commandText, int insertionPointIndex)
        {
            // TODO: plinq the whole thing

            if (commandText == null)
            {
                throw new ArgumentNullException("commandText");
            }

            if (insertionPointIndex < -1 || insertionPointIndex > commandText.Length)
            {
                throw new ArgumentOutOfRangeException("insertionPointIndex");
            }

            if (commandText.Length == 0)
            {
                return new CommandGeneratorResult();
            }

            Command[] potentialCommands;
            CommandExecutor[] potentialPartialCommands;

            var commandsFiltered = false;

            // TODO: delimiter for TextFacet -- possibly a colon. Solves for problem illustrated in example
            // here, which is that the second "Bob" may be parsed as a contact and excluded from the 
            // "remaining text" logic:  >email bob: Bob, Lunch?

            if (commandText[0] == '>')
            {
                var cmdName = commandText.Skip(1).TakeWhile(x => !Char.IsWhiteSpace(x)).CharsToString();

                if (cmdName.Length == 0)
                {
                    return new CommandGeneratorResult();
                }

                var trimmed = commandText.Substring(cmdName.Length + 1).TrimStart();
                insertionPointIndex -= commandText.Length - trimmed.Length;
                commandText = trimmed;

                var pca = 
                    (from cinfo in Loader.Commands
                     from a in cinfo.Aliases
                     where a.StartsWith(cmdName, StringComparison.CurrentCultureIgnoreCase)
                     select cinfo.Command).ToArray();

                var ppca = CommandHistory.LoadPartialCommands(cmdName).ToArray();

                if (pca.Length == 0 && ppca.Length == 0)
                {
                    return new CommandGeneratorResult();
                }

                potentialCommands = pca;
                potentialPartialCommands = ppca;

                commandsFiltered = true;
            }
            else
            {
                potentialCommands = Loader.Commands.Select(x => x.Command).ToArray();
                potentialPartialCommands = CommandHistory.LoadPartialCommands().ToArray();
            }

            var pptRegularCommands = potentialCommands
                .SelectMany(x => x.Parameters)
                .Select(x => x.TypeDescriptor);
            var pptPartialCommands = potentialPartialCommands
                .SelectMany(x => x.UnspecifiedParameters)
                .Select(x => x.TypeDescriptor);
            var potentialParameterTypes = pptRegularCommands
                .Union(pptPartialCommands)
                .ToArray();
            var potentialFacetTypes = Loader.GetFacetTypesForParameterTypes(potentialParameterTypes)
                .Select(x => x.TypeDescriptor)
                .Concat(TypeDescriptor.Get(typeof(TextFacet)))
                .Distinct()
                .ToArray();
            var potentialFactories = Loader
                .GetFacetFactoriesForFacetTypes(potentialFacetTypes)
                .ToArray();

            if (potentialFacetTypes.Length == 0)
            {
                return new CommandGeneratorResult();
            }

            var input = new ParseInput(commandText, insertionPointIndex);

            var allResults = new List<ParseResult>();

            var rvl = new CommandGeneratorResult();

            if (commandText.Length > 0)
            {
                var outConfigurationExceptions = new List<RequiresConfigurationException>();

                // TODO: parallelize the different forms of parsing (perfect multithreading opportunity)

                var potentialFacetMonikers = potentialFacetTypes.Select(x => x.Moniker).ToArray();

                var dynamicResults =
                    (from f in potentialFactories
                     where !(f.Factory is IFacetFactoryWithIndex)
                     let result = TryParse(rvl, f, input, ParseMode.All, potentialFacetMonikers)
                     where result != null
                     from r in result
                     select r).ToArray();

                var indexResults = FacetIndex.Parse(
                    potentialFactories.Select(x => x.Factory).OfType<IFacetFactoryWithIndex>(),
                    potentialFacetTypes,
                    input,
                    ParseMode.All,
                    outConfigurationExceptions).ToArray();

                var historyResults = FacetIndex.ParseFromHistory(potentialFacetTypes, input).ToArray();

                allResults =
                    (from r in dynamicResults.Union(indexResults).Union(historyResults)
                     orderby r.Range.StartIndex
                     select r).ToList();

                foreach (var ex in outConfigurationExceptions)
                {
                    rvl.AddRCException(ex);
                }

                // is this right commented out?
                //if (allResults.Count == 0)
                {
                    allResults.Add(CreateTextFacetResult(input, new ParseRange(0, input.Text.Length)));
                }
            }

            var groupedResults = allResults
                .GroupBy(x => x.Range)
                .ToDictionary(x => x.Key, x => x.ToArray());
            var rangeLists = CreateRangeLists(input, groupedResults.Keys);
            var commands = CreateCommandsFromResultLists(input, rangeLists, groupedResults, 
                potentialCommands, potentialPartialCommands, commandsFiltered);

            foreach (var cmd in commands.Distinct())
            {
                rvl.AddExecutor(cmd);
            }

            return rvl;
        }

        // must use ReferenceEquals to compare
        static readonly ParseRange RemainingTextRangeMarker = new ParseRange(0, 1);

        static List<List<ParseRange>> CreateRangeLists(ParseInput input, IEnumerable<ParseRange> groups)
        {
            var resultLists = groups
                .Where(x => x.StartIndex == 0)
                .Select(result => new List<ParseRange> {result})
                .ToList();

            var resultListsComplete = new List<List<ParseRange>>();

            while (resultLists.Count > 0)
            {
                var resultListsNew = new List<List<ParseRange>>();

                foreach (var list in resultLists)
                {
                    // find ParseResults that start immediately to the right of the rightmost ParseResult in 'list'.
                    // for each of these, make a copy of 'list', add the result to the copy, and add the copy to the 
                    // new set of lists (resultListsNew).
                    var rightIndex = input.ToCompressed(list[list.Count - 1].EndIndex);
                    var found = false;

                    foreach (var result in groups.Where(x => input.ToCompressed(x.StartIndex) == rightIndex + 1))
                    {
                        var newList = new List<ParseRange>(list);
                        newList.Add(result);
                        resultListsNew.Add(newList);
                        found = true;
                    }

                    if (found)
                    {
                        continue;
                    }

                    // no further ParseResults can be added to the list

                    if (rightIndex < input.CompressedText.Length - 1)
                    {
                        list.Add(RemainingTextRangeMarker);
                    }

                    // the argument list is complete
                    resultListsComplete.Add(list);
                }

                resultLists = resultListsNew;
            }

            return resultListsComplete;
        }

        static IEnumerable<CommandExecutor> CreateCommandsFromResultLists(ParseInput input, 
            List<List<ParseRange>> rangeLists, IDictionary<ParseRange, ParseResult[]> groupedResults, 
            IEnumerable<Command> potentialCommands, IEnumerable<CommandExecutor> potentialPartialCommands, 
            bool commandsFiltered)
        {
            if (!rangeLists.Any())
            {
                if (!commandsFiltered)
                {
                    return Enumerable.Empty<CommandExecutor>();
                }

                rangeLists = new List<List<ParseRange>> {new List<ParseRange>()};
            }

            var query1 = from c in potentialCommands
                         from rangeList in rangeLists
                         let cmd = CreateCommand(input, c, rangeList, groupedResults)
                         select cmd;

            var query2 = from c in potentialPartialCommands
                         from rangeList in rangeLists
                         let cmd = CreateCommand(input, c, rangeList, groupedResults)
                         select cmd;

            return query1.Union(query2).Where(x => x != null);
        }

        static CommandExecutor CreateCommand(ParseInput input, Command command, List<ParseRange> rangeList, 
            IDictionary<ParseRange, ParseResult[]> groupedResults)
        {
            if (command.Name == "Open Program")
            {
                int x = 10;
            }

            var argList = CreateCommand(input, rangeList, groupedResults, command.Parameters);

            if (argList == null)
            {
                return null;
            }

            // if any of the arguments is unspecified, offer some suggestions
            if (argList.Any(x => !x.IsSpecified))
            {
                argList = CommandHistory.PredictUnspecifiedArguments(command, argList);

                if (argList.Where((t, i) => !t.IsSpecified && !command.Parameters[i].Optional).Any())
                {
                    return null;
                }
            }

            return new CommandExecutor(input, command, argList);
        }

        static CommandExecutor CreateCommand(ParseInput input, CommandExecutor partialExecutor, 
            List<ParseRange> rangeList, IDictionary<ParseRange, ParseResult[]> groupedResults)
        {
            var argList = CreateCommand(input, rangeList, groupedResults, partialExecutor.UnspecifiedParameters);

            if (argList == null)
            {
                return null;
            }

            return partialExecutor.CreateSuggestedVersion(input,
                partialExecutor.UnspecifiedParameters.Zip(argList, Tuple.Create));
        }

        static List<CommandArgument> CreateCommand(ParseInput input, List<ParseRange> rangeList, 
            IDictionary<ParseRange, ParseResult[]> groupedResults, 
            IEnumerable<CommandParameter> parameters)
        {
            var argList = new List<CommandArgument>();
            var unusedRanges = new List<ParseRange>(rangeList);
            var textFacetParamIndex = -1;

            // The arguments don't have to appear in the same order as the parameters, so we loop through
            // parameters and pull out the first eligible argument for each. At the end of the loop, if all
            // arguments have been used, we can proceed to the next step.

            foreach (var param in parameters)
            {
                if (param.Type == typeof (ITextFacet))
                {
                    textFacetParamIndex = argList.Count;
                    argList.Add(CommandArgument.Unspecified);
                    continue;
                }

                var anyInRangeQuery =
                    from r in unusedRanges
                    where r != null && !ReferenceEquals(r, RemainingTextRangeMarker)
                    from pr in groupedResults[r]
                    where param.IsUsableAsArgument(pr.FacetMoniker)
                    select pr;

                var found = anyInRangeQuery.FirstOrDefault();

                if (found == null)
                {
                    // No match in the parse results
                    
                    // Try ambient facets
                    var ambient = Loader.AmbientFacets
                        .Where(x => param.IsUsableAsArgument(x.Moniker))
                        .Select(x => x.Moniker)
                        .FirstOrDefault();

                    if (ambient != null)
                    {
                        argList.Add(new CommandArgument(ambient));
                        continue;
                    }

                    argList.Add(CommandArgument.Unspecified);
                    continue;
                }

                unusedRanges[unusedRanges.IndexOf(found.Range)] = null;

                // TODO: optimization: leave off the suggestion filtering until we've determined 
                // the command is eligible

                // Do we have any eligible suggestions in the same ParseRange?
                IEnumerable<FacetMoniker> suggestions = null;

                if (groupedResults[found.Range].Length > 1)
                {
                    suggestions = groupedResults[found.Range]
                        .Where(x => !ReferenceEquals(found, x))
                        .Where(x => param.IsUsableAsArgument(x.FacetMoniker))
                        .Select(x => x.FacetMoniker);
                }

                argList.Add(new CommandArgument(found, suggestions));
            }

            var unusedCount = unusedRanges.Count(x => x != null);

            if (unusedCount != 0)
            {
                if (textFacetParamIndex == -1)
                {
                    return null;
                }

                // Can we use all of the unused up as a text facet, and are they all in line at the rightmost
                // part of the input?
                var unusedRightmostCount = Enumerable.Reverse(unusedRanges)
                    .TakeWhile(x => x != null)
                    .Count();

                if (unusedRightmostCount < unusedCount)
                {
                    // not all unused ranges are rightmost
                    return null;
                }

                if (ReferenceEquals(unusedRanges.Last(), RemainingTextRangeMarker))
                {
                    // rangeList is original - same length and index values as unusedRanges
                    var prev = rangeList[rangeList.Count - 2];
                    unusedRanges[unusedRanges.Count - 1] = ParseRange.FromIndexes(prev.EndIndex + 1, input.Text.Length - 1);
                }

                var unusedIndexStart = unusedRanges.Count - unusedRightmostCount;
                var unusedIndexEnd = unusedIndexStart + unusedRightmostCount - 1;
                var textRange = unusedRanges[unusedIndexStart].Union(unusedRanges[unusedIndexEnd]);

                argList[textFacetParamIndex] = new CommandArgument(CreateTextFacetResult(input, textRange));
            }

            return argList;
        }

        static ParseResult CreateTextFacetResult(ParseInput input, ParseRange range)
        {
            var textForRange = input.GetTextForRange(range).Trim();
            var moniker = new FacetMoniker(typeof(EngineFacetsFactory), typeof(TextFacet), textForRange, textForRange);
            return new ParseResult(input, range, moniker, ParseResult.ExactMatchRelevance);
        }

        static IEnumerable<ParseResult> TryParse(CommandGeneratorResult result, LoaderFacetFactory lff, ParseInput input, ParseMode mode, 
            IList<TypeMoniker> facetTypes)
        {
            try
            {
                return lff.Factory.Parse(input.RewriteInput(lff, false), mode, facetTypes);
            }
            catch (RequiresConfigurationException ex)
            {
                result.AddRCException(ex);
                return null;
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch
            {
                return null;
            }
        }

        internal static double ScoreSimilarity(FacetMoniker left, FacetMoniker right)
        {
            var score = 0.0;

            if (right.IsAmbient != left.IsAmbient)
            {
                return score;
            }

            if (right.FacetType == left.FacetType)
            {
                score += 0.5;
            }

            if (right.IsAmbient)
            {
                // both ambient
                if (right.AmbientToken == left.AmbientToken)
                {
                    score += 0.5;
                }

                return score;
            }

            if (right.FactoryType == left.FactoryType)
            {
                score += 0.25;
            }

            const double closenessMax = 0.25;

            if (left.FactoryData == right.FactoryData)
            {
                score += closenessMax;
            }
            else
            {
                var dist = (double)StringUtil.LevenshteinDistance(left.FactoryData, right.FactoryData);
                var closenessScore = closenessMax * (dist / Math.Max(right.FactoryData.Length, left.FactoryData.Length));
                score += closenessScore;
            }

            return score;
        }
    }
}
