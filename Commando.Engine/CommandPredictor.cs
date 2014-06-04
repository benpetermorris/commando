using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using twomindseye.Commando.API1.EngineFacets;
using twomindseye.Commando.Engine.DB;
using twomindseye.Commando.Engine.Extension;

namespace twomindseye.Commando.Engine
{
    public static class CommandPredictor
    {
        public static List<CommandExecutor> ReorderCommands(IEnumerable<CommandExecutor> commands)
        {
            var commandsArray = commands.ToArray();

            var commandUsages = 
                CommandHistory.GetCommandUsages(commandsArray.Select(x => x.Command), true);

            return commands
                .OrderByDescending(ScoreArguments)
                .ThenByDescending(x => ScorePastUsages(x, commandUsages.Where(y => y.Command == x.Command))).ToList();
        }

        static double ScoreArguments(CommandExecutor info)
        {
            return info.Arguments
                       .Where(x => x.IsSpecified && x.Source == CommandArgumentSource.Parsed)
                       .Select(x => (x.FacetMoniker.FacetType == typeof (TextFacet) ? 0.25 : 1.0) * x.ParseRelevance)
                       .Sum()/info.Command.Parameters.Count;

            //double paramCount = info.CommandInfo.Parameters.Select(x => x.Type == typeof (ITextFacet) ? 0.5 : 1.0).Sum();
            //double nonTextArgCount = info.Arguments.Where(x => x.IsSpecified && x.FacetMoniker.FacetType != typeof(TextFacet)).Count();
            //double textArgCount = info.Arguments.Where(x => x.IsSpecified && x.FacetMoniker.FacetType == typeof(TextFacet)).Count();
            //return (nonTextArgCount + textArgCount / 2) / paramCount;
        }

        static double ScorePastUsages(CommandExecutor info, IEnumerable<CommandUsage> pastUsages)
        {
            var now = DateTime.Now;

            var query = from u in pastUsages
                        let ageInDays = now.Subtract(u.At).TotalDays
                        select 1/ageInDays;

            return query.Sum();
        }

        static double ScoreExecutionInfo(CommandExecutor info, IEnumerable<CommandUsage> commandUsages)
        {
            var now = DateTime.Now;

            var query = from u in commandUsages
                        let ageInDays = now.Subtract(u.At).TotalDays
                        let score = info.ScoreSimilarity(u.Executor)/ageInDays
                        select score;

            return query.Sum();
        }
    }
}
