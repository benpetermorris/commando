using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Threading;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine.Load;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Engine.DB
{
    static class ExtensionStore
    {
        public static void InitExtension(LoaderExtension extension)
        {
            var queryParams = new object[]
                              {
                                  "@Path", extension.Path,
                                  "@IsScript", extension is LoaderScriptExtension
                              };

            var id = DatabaseUtil.ExecuteNullableScalar<int>(
                "SELECT Id FROM Extensions WHERE Path = @Path AND IsScript = @IsScript", 
                queryParams);

            if (id == null)
            {
                id = DatabaseUtil.ExecuteInsert(
                    "INSERT INTO Extensions (Path, IsScript) VALUES (@Path, @IsScript)", 
                    queryParams);
            }

            extension.DatabaseId = id.Value;
        }

        static bool InitExtensionItem(string tableName, LoaderExtensionItem item)
        {
            var wasAdded = false;

            var queryParams = new object[]
                              {
                                  "@Name", item.DatabaseName,
                                  "@ExtensionId", item.Extension.DatabaseId
                              };

            var id = DatabaseUtil.ExecuteNullableScalar<int>(
                String.Format("SELECT Id FROM {0} WHERE ExtensionId = @ExtensionId AND Name = @Name", tableName), 
                queryParams);

            if (id == null)
            {
                id = DatabaseUtil.ExecuteInsert(
                    String.Format("INSERT INTO {0} (ExtensionId, Name) VALUES (@ExtensionId, @Name) ", tableName),
                    queryParams);

                wasAdded = true;
            }

            item.DatabaseId = id.Value;

            return wasAdded;
        }

        public static void InitFacetType(LoaderFacetType info)
        {
            InitExtensionItem("FacetTypes", info);
        }

        public static void InitFacetFactory(LoaderFacetFactory info)
        {
            InitExtensionItem("FacetFactories", info);
        }

        public static void InitCommandContainer(LoaderCommandContainer info, IEnumerable<LoaderCommand> commands)
        {
            InitExtensionItem("CommandContainers", info);

            using (var connection = DatabaseUtil.GetConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var selectCommand = new SqlCeCommand(
                    "SELECT * FROM Commands WHERE CommandContainerId = @CommandContainerId AND CommandName = @CommandName",
                    connection, transaction);
                selectCommand.Parameters.AddWithValue("@CommandContainerId", info.DatabaseId);
                selectCommand.Parameters.Add("@CommandName", SqlDbType.NVarChar);

                var insertCommand = new SqlCeCommand(
                    "INSERT INTO Commands (CommandContainerId, CommandName, Aliases, SignatureHash) " +
                    "VALUES (@CommandContainerId, @CommandName, @Aliases, @SignatureHash)",
                    connection, transaction);
                insertCommand.Parameters.AddWithValue("@CommandContainerId", info.DatabaseId);
                insertCommand.Parameters.Add("@CommandName", SqlDbType.NVarChar);
                insertCommand.Parameters.Add("@Aliases", SqlDbType.NVarChar);
                insertCommand.Parameters.Add("@SignatureHash", SqlDbType.NChar, 32);

                foreach (var loadedCommand in commands)
                {
                    var found = false;

                    selectCommand.Parameters["@CommandName"].Value = loadedCommand.Command.Name;

                    foreach (var row in selectCommand.ExecuteReader().AsEnumerable())
                    {
                        var rowSignatureHash = (string)row["SignatureHash"];

                        if (rowSignatureHash == loadedCommand.Command.GetSignatureHash())
                        {
                            found = true;

                            // load other info
                            loadedCommand.SetAliases(((string) row["Aliases"]).Split(','));
                            loadedCommand.DatabaseId = (int) row["Id"];

                            break;
                        }
                    }

                    if (!found)
                    {
                        insertCommand.Parameters["@CommandName"].Value = loadedCommand.Command.Name;
                        insertCommand.Parameters["@Aliases"].Value = loadedCommand.Command.OriginalAliases.JoinStrings(",");
                        insertCommand.Parameters["@SignatureHash"].Value = loadedCommand.Command.GetSignatureHash();
                        insertCommand.ExecuteNonQuery();
                        loadedCommand.DatabaseId = connection.GetLastInsertedId(transaction);
                    }
                }

                transaction.Commit();
            }
        }
    }
}
