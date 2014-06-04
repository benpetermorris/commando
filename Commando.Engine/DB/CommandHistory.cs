using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Engine.Extension;
using twomindseye.Commando.Engine.Load;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Engine.DB
{
    static class CommandHistory
    {
        public static FacetMoniker[] GetMostUsedFacets(TypeMoniker commandParameterType, int maxResults)
        {
            return GetMostUsedFacets(commandParameterType, null, maxResults);
        }

        public static FacetMoniker[] GetMostUsedFacets(CommandParameter parameter, int maxResults)
        {
            return GetMostUsedFacets(null, parameter, maxResults);
        }

        static FacetMoniker[] GetMostUsedFacets(TypeMoniker commandParameterType, CommandParameter parameter, int maxResults)
        {
            var rvl = new List<FacetMoniker>();

            commandParameterType = parameter == null ? commandParameterType : parameter.Type;

            var facetTypeIds = Loader.GetFacetTypesImplementing(commandParameterType)
                .Select(x => x.DatabaseId.ToString())
                .JoinStrings(",");

            if (facetTypeIds == "")
            {
                // no facet types support this command parameter type
                return rvl.ToArray();
            }

            using (var connection = DatabaseUtil.GetConnection())
            {
                const string innerSelect =
                    @"SELECT cua.FacetMonikerId, COUNT(*) AS _count
                      FROM CommandUsageArguments AS cua 
                      INNER JOIN FacetMonikers AS fm ON fm.Id = cua.FacetMonikerId
                      {0}
                      WHERE fm.FacetTypeId IN ({1})
                      GROUP BY cua.FacetMonikerId";

                const string forCommandFragment =
                    @"INNER JOIN CommandUsages AS cu ON cu.Id = cua.CommandUsageId AND cu.CommandId = @CommandId";

                var innerX = String.Format("INNER JOIN ({0}) AS x ON x.FacetMonikerId = fm.Id", String.Format(innerSelect, "", facetTypeIds));

                var innerY = parameter == null
                    ? null
                    : String.Format("LEFT OUTER JOIN ({0}) AS y ON y.FacetMonikerId = fm.Id", String.Format(innerSelect, forCommandFragment, facetTypeIds));

                var selectYCount = parameter == null ? "" : ", y._count";

                var order = parameter == null ? "x._count DESC" : "COALESCE(y._count, 0) DESC, x._count DESC";

                const string cmdText =
                    @"SELECT fm.FactoryTypeId, fm.FacetTypeId, fm.DisplayName, fm.DateTime, fm.FactoryData, fm.Source, fm.ExtraData, x._count {0}
                      FROM FacetMonikers AS fm
                      {1}
                      {2}
                      WHERE fm.Id = x.FacetMonikerId
                      ORDER BY {3}";

                var finalCmdText = String.Format(cmdText, selectYCount, innerX, innerY, order);

                using (var cmd = new SqlCeCommand(finalCmdText, connection))
                {
                    if (parameter != null)
                    {
                        cmd.Parameters.AddWithValue("@CommandId", Loader.GetCommandInfo(parameter.Command).DatabaseId);
                    }

                    foreach (var row in cmd.ExecuteReader().AsEnumerable())
                    {
                        var moniker = FacetIndex.MaterializeMoniker(row);

                        if (moniker == null || (parameter != null && !parameter.IsUsableAsArgument(moniker)))
                        {
                            continue;
                        }

                        rvl.Add(moniker);

                        if (rvl.Count == maxResults)
                        {
                            break;
                        }
                    }
                }
            }

            return rvl.ToArray();
        }

        public static FacetUsage[] GetFacetUsages(FacetMoniker moniker)
        {
            return GetFacetUsages(new[] { moniker });
        }

        public static FacetUsage[] GetFacetUsages(IEnumerable<FacetMoniker> monikers)
        {
            var rvl = new List<FacetUsage>();

            using (var connection = DatabaseUtil.GetConnection())
            {
                const string cmdText =
                    @"SELECT cu.CommandId, cua.MatchedText, cua.Ordinal, cu.At
                      FROM CommandUsageArguments AS cua
                      INNER JOIN CommandUsages AS cu ON cu.Id = cua.CommandUsageId
                      INNER JOIN FacetMonikers AS fm ON fm.Id = cua.FacetMonikerId
                      WHERE fm.FacetTypeId = @FacetTypeId AND fm.FactoryTypeId = @FactoryTypeID AND fm.FactoryDataHash = @FactoryDataHash";

                using (var cmd = new SqlCeCommand(cmdText, connection))
                {
                    cmd.Parameters.AddWithValue("@FacetTypeId", 0);
                    cmd.Parameters.AddWithValue("@FactoryTypeId", 0);
                    cmd.Parameters.AddWithValue("@FactoryDataHash", "");

                    foreach (var moniker in monikers)
                    {
                        if (rvl.Any(x => x.Moniker == moniker))
                        {
                            continue;
                        }

                        cmd.Parameters["@FacetTypeId"].Value = Loader.GetFacetInfo(moniker.FacetType).DatabaseId;
                        cmd.Parameters["@FactoryTypeId"].Value = Loader.GetFactoryInfo(moniker.FactoryType).DatabaseId;
                        cmd.Parameters["@FactoryDataHash"].Value = moniker.HashString;

                        var query =
                            from row in cmd.ExecuteReader().AsEnumerable()
                            let command = Loader.GetCommandInfo((int)row["CommandId"]).Command
                            where command != null
                            select new FacetUsage(
                                moniker,
                                command,
                                (int)row["Ordinal"],
                                (DateTime)row["At"],
                                (string)row["MatchedText"]);

                        rvl.AddRange(query);
                    }
                }
            }

            return rvl.ToArray();
        }

        public static CommandUsage[] GetCommandUsages(Command command, bool commandOnly = false)
        {
            return GetCommandUsages(new[] { command }, commandOnly);
        }

        public static CommandUsage[] GetCommandUsages(IEnumerable<Command> commands, bool commandsOnly = false)
        {
            var rvl = new List<CommandUsage>();

            using (var selectCommandConnection = DatabaseUtil.GetConnection())
            using (var selectArgConnection = DatabaseUtil.GetConnection())
            {
                const string selectCommandText =
                    @"SELECT cu.Id, cu.At, cu.Text AS ParseInput
                      FROM CommandUsages AS cu 
                      WHERE cu.CommandId = @CommandId";

                const string selectArgText =
                    @"SELECT fm.FactoryTypeId, fm.FacetTypeId, fm.DisplayName, fm.DateTime, fm.FactoryData, fm.Source, fm.ExtraData, 
                             cua.Ordinal, cua.RangeStartIndex, cua.RangeLength, cua.Relevance, cua.MatchedText
                      FROM CommandUsageArguments AS cua
                      INNER JOIN FacetMonikers AS fm ON fm.Id = cua.FacetMonikerId
                      WHERE cua.CommandUsageId = @CommandUsageId
                      ORDER BY cua.Ordinal";

                using (var selectCommandUsageCmd = new SqlCeCommand(selectCommandText, selectCommandConnection))
                using (var selectUsageArgumentsCmd = new SqlCeCommand(selectArgText, selectArgConnection))
                {
                    selectCommandUsageCmd.Parameters.AddWithValue("@CommandId", SqlDbType.Int);
                    selectUsageArgumentsCmd.Parameters.AddWithValue("@CommandUsageId", SqlDbType.Int);

                    foreach (var command in commands.Distinct())
                    {
                        selectCommandUsageCmd.Parameters["@CommandId"].Value = Loader.GetCommandInfo(command).DatabaseId;

                        foreach (var cmdRow in selectCommandUsageCmd.ExecuteReader().AsEnumerable())
                        {
                            if (commandsOnly)
                            {
                                rvl.Add(new CommandUsage(command, (DateTime)cmdRow["At"]));
                                continue;
                            }

                            var parseInput = new ParseInput((string)cmdRow["ParseInput"]);
                            var arguments = new List<CommandArgument>();
                            var lastOrdinal = -1;

                            selectUsageArgumentsCmd.Parameters["@CommandUsageId"].Value = (int)cmdRow["Id"];

                            foreach (var argRow in selectUsageArgumentsCmd.ExecuteReader().AsEnumerable())
                            {
                                var ordinal = (int)argRow["Ordinal"];

                                while (++lastOrdinal < ordinal)
                                {
                                    arguments.Add(CommandArgument.Unspecified);
                                }

                                // reconstruct facet moniker
                                var moniker = FacetIndex.MaterializeMoniker(argRow);

                                if (moniker == null)
                                {
                                    arguments = null;
                                    break;
                                }

                                if ((int)argRow["RangeStartIndex"] != -1)
                                {
                                    // reconstruct parse range
                                    var range = new ParseRange(
                                        (int)argRow["RangeStartIndex"],
                                        (int)argRow["RangeLength"]);

                                    // reconstruct parseresult
                                    var parseResult = new ParseResult(
                                        parseInput,
                                        range,
                                        moniker,
                                        (double)argRow["Relevance"]);

                                    arguments.Add(new CommandArgument(parseResult));
                                }
                                else
                                {
                                    // moniker only
                                    arguments.Add(new CommandArgument(moniker));
                                }
                            }

                            if (arguments != null)
                            {
                                while (++lastOrdinal < command.Parameters.Count)
                                {
                                    arguments.Add(CommandArgument.Unspecified);
                                }

                                var cei = new CommandExecutor(parseInput, command, arguments);
                                rvl.Add(new CommandUsage(cei, (DateTime)cmdRow["At"]));
                            }
                        }
                    }
                }
            }

            return rvl.ToArray();
        }

        public static List<CommandArgument> PredictUnspecifiedArguments(Command command, List<CommandArgument> arguments)
        {
            var usages = GetCommandUsages(command);
            var scoredUsages = new List<Tuple<CommandUsage, double>>();

            foreach (var usage in usages)
            {
                var nScored = 0;
                var totalSimilarity = 0.0;
                var maxArgIndex = Math.Min(arguments.Count, usage.Executor.Arguments.Count);
                var useful = false;

                for (var index = 0; index < maxArgIndex; index++)
                {
                    var usageArgument = usage.Executor.Arguments[index];

                    if (!usageArgument.IsSpecified)
                    {
                        continue;
                    }

                    var curArgument = arguments[index];

                    if (!curArgument.IsSpecified)
                    {
                        useful = useful || usageArgument.IsSpecified;
                        continue;
                    }

                    totalSimilarity += CommandGenerator.ScoreSimilarity(curArgument.FacetMoniker, usageArgument.FacetMoniker);

                    nScored++;
                }

                if (useful && nScored > 0)
                {
                    scoredUsages.Add(Tuple.Create(usage, totalSimilarity / nScored));
                }
            }

            scoredUsages = scoredUsages.OrderByDescending(x => x.Item2).ToList();

            var rvl = new List<CommandArgument>();

            for (var i = 0; i < arguments.Count; i++)
            {
                if (arguments[i].IsSpecified)
                {
                    rvl.Add(arguments[i]);
                    continue;
                }

                var parameter = command.Parameters[i];
                var argument = CommandArgument.Unspecified;
                var usageMonikers = scoredUsages
                    .Select(x => x.Item1.Executor.Arguments[i])
                    .Where(x => x.IsSpecified && parameter.IsUsableAsArgument(x.FacetMoniker))
                    .Select(x => x.FacetMoniker)
                    .ToArray();

                if (usageMonikers.Length > 0)
                {
                    argument = new CommandArgument(usageMonikers, !parameter.Optional);
                }
                else
                {
                    var mostUsed = GetMostUsedFacets(parameter, 5);

                    if (mostUsed.Length > 0)
                    {
                        argument = new CommandArgument(mostUsed, !parameter.Optional);
                    }
                }

                rvl.Add(argument);
            }

            return rvl;
        }

        public static void SaveExecutedCommand(CommandExecutor executor)
        {
            using (var connection = DatabaseUtil.GetConnection())
            {
                using (var trans = connection.BeginTransaction())
                {
                    const string insertCmdText =
                        @"INSERT INTO CommandUsages (Text, At, CommandId)
                          VALUES (@Text, @At, @CommandId)";
                    const string insertArgText =
                        @"INSERT INTO CommandUsageArguments 
                          (CommandUsageId, FacetMonikerId, Ordinal, RangeStartIndex, RangeLength, Relevance, MatchedText)
                          VALUES 
                          (@CommandUsageId, @FacetMonikerId, @Ordinal, @RangeStartIndex, @RangeLength, 0.0, @MatchedText)";

                    int commandUsageId;

                    using (var cmd = new SqlCeCommand(insertCmdText, connection, trans))
                    {
                        cmd.Parameters.AddWithValue("@CommandId", Loader.GetCommandInfo(executor.Command).DatabaseId);
                        cmd.Parameters.AddWithValue("@Text", executor.Input == null ? "" : executor.Input.Text);
                        cmd.Parameters.AddWithValue("@At", DateTime.Now);
                        cmd.ExecuteNonQuery();

                        commandUsageId = connection.GetLastInsertedId(trans);
                    }

                    using (var cmd = new SqlCeCommand(insertArgText, connection, trans))
                    {
                        cmd.Parameters.AddWithValue("@CommandUsageId", commandUsageId);
                        cmd.Parameters.AddWithValue("@FacetMonikerId", 0);
                        cmd.Parameters.AddWithValue("@Ordinal", 0);
                        cmd.Parameters.AddWithValue("@RangeStartIndex", 0);
                        cmd.Parameters.AddWithValue("@RangeLength", 0);
                        cmd.Parameters.AddWithValue("@MatchedText", "");

                        for (var ordinal = 0; ordinal < executor.Arguments.Count; ordinal++)
                        {
                            var arg = executor.Arguments[ordinal];

                            if (!arg.IsSpecified)
                            {
                                continue;
                            }

                            cmd.Parameters["@Ordinal"].Value = ordinal;
                            cmd.Parameters["@FacetMonikerId"].Value =
                                FacetIndex.SaveFacetMoniker(arg.FacetMoniker, connection: connection, trans: trans);

                            if (arg.ParseRange != null)
                            {
                                cmd.Parameters["@RangeStartIndex"].Value = arg.ParseRange.StartIndex;
                                cmd.Parameters["@RangeLength"].Value = arg.ParseRange.Length;
                                cmd.Parameters["@MatchedText"].Value = executor.Input == null ? "" : executor.Input.GetTextForRange(arg.ParseRange);
                            }
                            else
                            {
                                cmd.Parameters["@RangeStartIndex"].Value = -1;
                                cmd.Parameters["@RangeLength"].Value = -1;
                                cmd.Parameters["@MatchedText"].Value = "";
                            }

                            cmd.ExecuteNonQuery();
                        }
                    }

                    trans.Commit();
                }
            }
        }

        public static IEnumerable<CommandExecutor> LoadPartialCommands(string alias = null)
        {
            using (var connection = DatabaseUtil.GetConnection())
            {
                const string selectCmdText =
                    @"SELECT Id, CommandId, Alias FROM PartialCommands";
                const string selectArgText =
                    @"SELECT fm.FactoryTypeId, fm.FacetTypeId, fm.DisplayName, fm.DateTime, fm.FactoryData, fm.Source, fm.ExtraData, 
                             pca.Ordinal
                      FROM PartialCommandArguments AS pca
                      INNER JOIN FacetMonikers AS fm ON fm.Id = pca.FacetMonikerId
                      WHERE pca.PartialCommandId = @PartialCommandId
                      ORDER BY pca.Ordinal";

                var selectCmdTextFinal = selectCmdText + (alias == null ? "" : " WHERE Alias = @Alias");

                using (var selectCmdCommand = new SqlCeCommand(selectCmdTextFinal, connection))
                using (var selectArgCommand = new SqlCeCommand(selectArgText, connection))
                {
                    if (alias != null)
                    {
                        selectCmdCommand.Parameters.AddWithValue("@Alias", alias);
                    }

                    var partialCommandIdParam = selectArgCommand.Parameters.Add("@PartialCommandId", SqlDbType.Int);

                    foreach (var cmd in selectCmdCommand.ExecuteReader().AsEnumerable())
                    {
                        partialCommandIdParam.Value = (int) cmd["Id"];

                        var command = Loader.GetCommandInfo((int)cmd["CommandId"]);

                        if (command == null)
                        {
                            continue;
                        }

                        var args = (from arg in selectArgCommand.ExecuteReader().AsEnumerable()
                                    let moniker = FacetIndex.MaterializeMoniker(arg)
                                    select new
                                    {
                                        Ordinal = (int) arg["Ordinal"],
                                        Argument = new CommandArgument(moniker)
                                    }).ToArray();

                        var allArgs = new List<CommandArgument>();

                        for (var ordinal = 0; ordinal < command.Command.Parameters.Count; ordinal++)
                        {
                            var loadedArg =
                                args.Where(x => x.Ordinal == ordinal).Select(x => x.Argument).FirstOrDefault();

                            allArgs.Add(loadedArg ?? CommandArgument.Unspecified);
                        }

                        var cmdAlias = (string) cmd["Alias"];

                        yield return new CommandExecutor(null, command.Command, allArgs, cmdAlias, new[] { cmdAlias });
                    }
                }
            }
        }

        public static void SavePartialCommand(CommandExecutor executor, string alias)
        {
            using (var connection = DatabaseUtil.GetConnection())
            using (var trans = connection.BeginTransaction())
            {
                const string deleteCmdText =
                    @"DELETE FROM PartialCommands WHERE Alias = @Alias";
                const string insertCmdText =
                    @"INSERT INTO PartialCommands (CommandId, Alias)
                      VALUES (@CommandId, @Alias)";
                const string insertArgText =
                    @"INSERT INTO PartialCommandArguments
                      (PartialCommandId, FacetMonikerId, Ordinal)
                      VALUES 
                      (@PartialCommandId, @FacetMonikerId, @Ordinal)";

                int partialCommandId;

                using (var cmd = new SqlCeCommand(deleteCmdText, connection, trans))
                {
                    cmd.Parameters.AddWithValue("@Alias", alias);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCeCommand(insertCmdText, connection, trans))
                {
                    cmd.Parameters.AddWithValue("@CommandId", Loader.GetCommandInfo(executor.Command).DatabaseId);
                    cmd.Parameters.AddWithValue("@Alias", alias);
                    cmd.ExecuteNonQuery();

                    partialCommandId = connection.GetLastInsertedId(trans);
                }

                using (var cmd = new SqlCeCommand(insertArgText, connection, trans))
                {
                    cmd.Parameters.AddWithValue("@PartialCommandId", partialCommandId);
                    cmd.Parameters.AddWithValue("@FacetMonikerId", 0);
                    cmd.Parameters.AddWithValue("@Ordinal", 0);

                    for (var ordinal = 0; ordinal < executor.Arguments.Count; ordinal++)
                    {
                        var arg = executor.Arguments[ordinal];

                        if (!arg.IsSpecified)
                        {
                            continue;
                        }

                        cmd.Parameters["@Ordinal"].Value = ordinal;
                        cmd.Parameters["@FacetMonikerId"].Value =
                            FacetIndex.SaveFacetMoniker(arg.FacetMoniker, connection: connection, trans: trans);

                        cmd.ExecuteNonQuery();
                    }
                }

                trans.Commit();
            }
        }
    }
}