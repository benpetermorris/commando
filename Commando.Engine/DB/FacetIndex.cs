using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.API1.Parse;
using twomindseye.Commando.Engine.Load;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Engine.DB
{
    static class FacetIndex
    {
        public static bool TryUpdateFacetFactoryIndex(LoaderFacetFactory info)
        {
            var ffwi = info.Factory as IFacetFactoryWithIndex;

            if (ffwi == null)
            {
                throw new ArgumentException("factory is not indexed");
            }

            var lastUpdatedAt = DatabaseUtil.ExecuteNullableScalar<DateTime>(
                "SELECT LastIndexUpdate FROM FacetFactories WHERE Id = @Id",
                "@Id", info.DatabaseId);

            if (!ffwi.ShouldUpdateIndex(FacetIndexReason.Startup, lastUpdatedAt))
            {
                return false;
            }

            ThreadPool.QueueUserWorkItem( notused =>
                                         {
                                             // what is this for?
                                             // Thread.Sleep(2000);
                                             UpdateFromFactory(info);
                                         });

            return true;
        }

        static void UpdateFromFactory(LoaderFacetFactory factory)
        {
            var ffwi = factory.Factory as IFacetFactoryWithIndex;
            List<FacetMoniker> index;

            try
            {
                index = ffwi.EnumerateIndex().ToList();
            }
            catch (RequiresConfigurationException rce)
            {
                factory.AddIndexingException(rce);
                return;
            }
            catch (Exception ex)
            {
                // TODO: failure log?
                Debug.WriteLine("Could not enumerate indexed factory {0}: {1}", factory.Name, ex.Message);
                return;
            }

            using (var connection = DatabaseUtil.GetConnection())
            {
                const string cmdText =
                    @"SELECT FacetTypeId, FactoryDataHash, DisplayName
                      FROM FacetMonikers
                      WHERE FactoryTypeId = @FactoryTypeId";

                // load the existing index
                using (var cmd = new SqlCeCommand(cmdText, connection))
                {
                    cmd.Parameters.AddWithValue("@FactoryTypeId", factory.DatabaseId);

                    using (var reader = cmd.ExecuteResultSet(ResultSetOptions.Updatable))
                    {
                        while (reader.Read())
                        {
                            var facetType = Loader.GetFacetInfo((int) reader["FacetTypeId"]);

                            if (facetType == null)
                            {
                                // this would be unusual, since the factory is loaded
                                continue;
                            }

                            var factoryDataHash = (string) reader["FactoryDataHash"];
                            var displayName = (string) reader["DisplayName"];
                            var indexIndex = index.FindIndex(x => x.FacetType == facetType.Type && 
                                x.HashString == factoryDataHash && 
                                x.DisplayName == displayName);
                            
                            if (indexIndex == -1)
                            {
                                reader.Delete();
                            }
                            else
                            {
                                index.RemoveAt(indexIndex);
                            }
                        }
                    }
                }

                foreach (var moniker in index)
                {
                    SaveFacetMoniker(moniker, connection: connection);
                }
            }
        }

        public static FacetMoniker MaterializeMoniker(IDataRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            if (record.IsDBNull("FactoryTypeId"))
            {
                // ambient facet
                var ambient = Loader.GetAmbientFacet(Guid.Parse((string) record["FactoryData"]));
                return ambient == null ? null : ambient.Moniker;
            }

            var facetType = Loader.GetFacetInfo((int) record["FacetTypeId"]);
            var factory = Loader.GetFactoryInfo((int) record["FactoryTypeId"]);

            if (facetType == null || factory == null)
            {
                return null;
            }

            var edCol = record["ExtraData"];
            var extraData =
                edCol == DBNull.Value
                    ? null
                    : DeserializeExtraData((string)edCol);

            var moniker = new FacetMoniker(factory.Type,
                facetType.Type,
                (string)record["FactoryData"],
                (string)record["DisplayName"],
                record.ValOrDefault<DateTime?>("DateTime"),
                record.ValOrDefault<string>("Source"), null, extraData);

            return factory.Factory.CanCreateFacet(moniker) ? moniker : null;
        }

        static string SerializeExtraData(FacetMoniker moniker)
        {
            var join = from ed in moniker.ExtraData
                       let ekey = Uri.EscapeDataString(ed.Key)
                       let evalue = Uri.EscapeDataString(ed.Value)
                       let facetType = Loader.GetFacetInfo(ed.FacetType)
                       where facetType != null
                       select String.Format("{0}={1}={2}", facetType.DatabaseId, ekey, evalue);

            return @join.JoinStrings("&");
        }

        static IEnumerable<FacetExtraData> DeserializeExtraData(string extraData)
        {
            return from set in extraData.Split('&')
                   let parts = set.Split('=').Select(Uri.UnescapeDataString).ToArray()
                   where parts.Length == 3
                   let facetType = Loader.GetFacetInfo(Int32.Parse(parts[0]))
                   where facetType != null
                   select new FacetExtraData(facetType.Type, parts[1], parts[2]);
        }

        public const string DontSetAlias = "dontsetalias";

        public static int? SaveFacetMoniker(FacetMoniker moniker, string alias = DontSetAlias, 
            SqlCeConnection connection = null, SqlCeTransaction trans = null)
        {
            var localConnection = connection ?? DatabaseUtil.GetConnection();

            try
            {
                var facetTypeId = Loader.GetFacetInfo(moniker.FacetType).DatabaseId;
                int? factoryTypeId = null;
                string factoryData;
                
                if (!moniker.IsAmbient)
                {
                    factoryTypeId = Loader.GetFactoryInfo(moniker.FactoryType).DatabaseId;
                    factoryData = moniker.FactoryData;
                }
                else
                {
                    // we misuse the factorydata column for ambient facets because it simplifies 
                    // storage/retrieval
                    factoryData = moniker.AmbientToken.ToString();
                }

                using (var cmd = new SqlCeCommand("FacetMonikers", localConnection, trans))
                {
                    cmd.IndexName = "UQ_FacetMonikers";
                    cmd.CommandType = CommandType.TableDirect;

                    using (var rs = cmd.ExecuteResultSet(ResultSetOptions.Updatable | ResultSetOptions.Scrollable))
                    {
                        if (rs.Seek(DbSeekOptions.FirstEqual, factoryTypeId, facetTypeId, moniker.HashString) && rs.Read())
                        {
                            if (alias != DontSetAlias)
                            {
                                var aliasOrdinal = rs.GetOrdinal("Alias");

                                if (rs.IsDBNull(aliasOrdinal) != (alias == null) || rs[aliasOrdinal] as string != alias)
                                {
                                    rs.SetString(aliasOrdinal, alias);
                                    rs.Update();
                                }
                            }

                            return (int)rs["Id"];
                        }

                        var record = rs.CreateRecord();

                        record["FacetTypeId"] = facetTypeId;
                        record["FactoryTypeId"] = factoryTypeId;
                        record["DisplayName"] = moniker.DisplayName;
                        record["FactoryDataHash"] = moniker.HashString;
                        record["FactoryData"] = factoryData;
                        record["Source"] = moniker.Source;
                        record["Alias"] = alias;

                        if (moniker.DateTime != null)
                        {
                            record["DateTime"] = moniker.DateTime.Value;
                        }
                        
                        if (moniker.ExtraData != null)
                        {
                            record["ExtraData"] = SerializeExtraData(moniker);
                        }
                        
                        rs.Insert(record, DbInsertOptions.PositionOnInsertedRow);

                        return (int) rs["Id"];
                    }
                }
            }
            finally
            {
                if (connection == null)
                {
                    localConnection.Dispose();
                }
            }
        }

        public static IEnumerable<ParseResult> ParseFromHistory(IEnumerable<TypeDescriptor> facetTypes, ParseInput input)
        {
            var facetTypeIds = String.Join(",",
                from ft in facetTypes select Loader.GetFacetInfo(ft).DatabaseId.ToString());

            if (facetTypeIds.Length == 0)
            {
                yield break;
            }

            using (var connection = DatabaseUtil.GetConnection())
            {
                const string cmdText =
                    @"SELECT FacetMonikers.Id, DisplayName, DateTime, FactoryData, FacetTypeId, FactoryTypeId, Source, ExtraData, Alias,
                             cua.MatchedText, cua.Relevance
                      FROM FacetMonikers
                      LEFT OUTER JOIN CommandUsageArguments AS cua ON cua.FacetMonikerId = FacetMonikers.Id
                      WHERE FacetTypeID IN ({0})
                      AND ({1})
                      ORDER BY FactoryTypeId";

                using (var cmd = new SqlCeCommand())
                {
                    var clausesBuilder = new StringBuilder();

                    for (var index = 0; index < input.Terms.Count; index++)
                    {
                        if (clausesBuilder.Length > 0)
                        {
                            clausesBuilder.Append(" OR ");
                        }

                        var term = input.Terms[index];
                        var paramName = "@t" + index;
                        clausesBuilder.AppendFormat("cua.MatchedText = {0} OR Alias = {0}", paramName);
                        cmd.Parameters.AddWithValue(paramName, term.Text);
                    }

                    cmd.Connection = connection;
                    cmd.CommandText = String.Format(cmdText, facetTypeIds, clausesBuilder);

                    Func<string, string, bool> compareTerm = (term, text) =>
                        string.Compare(term, text, StringComparison.CurrentCultureIgnoreCase) == 0;

                    var returned = new List<ParseResult>();

                    var query = 
                        from row in cmd.ExecuteReader().AsEnumerable()
                        let moniker = MaterializeMoniker(row)
                        where moniker != null
                        let rowMatchedText = row["MatchedText"] as string
                        let rowAlias = row["Alias"] as string
                        let match = 
                            // determine the (first) input term that selected this row:
                            (from t in input.Terms
                             let mtMatch = compareTerm(t.Text, rowMatchedText)
                             let aliasMatch = compareTerm(t.Text, rowAlias)
                             where mtMatch || aliasMatch
                             select new { Term = t, mtMatch, aliasMatch }).First()
                        where match.Term.IsFactorySourceValid(moniker.GetFactory())
                        select new ParseResult(match.Term, moniker, match.aliasMatch ? 1.0 : row.ValOrDefault("Relevance", 1.0));
                    // above: if "Relevance" is null, we matched via alias, which is relevance 1.0.

                    foreach (var result in query)
                    {
                        if (!returned.Contains(result))
                        {
                            returned.Add(result);

                            yield return result;
                        }
                    }
                }
            }
        }

        public static IEnumerable<ParseResult> Parse(IEnumerable<IFacetFactoryWithIndex> factories, IEnumerable<TypeDescriptor> facetTypes, 
            ParseInput input, ParseMode all, List<RequiresConfigurationException> outConfigurationExceptions)
        {
            var factoryTypesAndIds = factories.ToDictionary(f => Loader.GetFactoryInfo(f).DatabaseId);

            if (factoryTypesAndIds.Count == 0)
            {
                yield break;
            }

            var facetTypeIds = String.Join(",", from ft in facetTypes select Loader.GetFacetInfo(ft).DatabaseId.ToString());

            using (var connection = DatabaseUtil.GetConnection())
            {
                const string cmdText =
                    @"SELECT Id, DisplayName, DateTime, FactoryData, FacetTypeId, FactoryTypeId, Source, ExtraData
                      FROM FacetMonikers 
                      WHERE FactoryTypeId = @FactoryTypeId AND FacetTypeID IN ({0})
                      ORDER BY FactoryTypeId";

                var cmd = new SqlCeCommand(String.Format(cmdText, facetTypeIds), connection);
                var param = cmd.Parameters.Add("@FactoryTypeId", SqlDbType.Int);

                foreach (var factory in factoryTypesAndIds)
                {
                    param.Value = factory.Key;

                    if (factory.Key == 18)
                    {
                        int x = 10;
                    }

                    var query = from row in cmd.ExecuteReader().AsEnumerable() select MaterializeMoniker(row);

                    IEnumerable<ParseResult> results;

                    try
                    {
                        // TODO: remove ToArray when you figure out what causes "the row is deleted" bullll sheeet
                        results = factory.Value.Parse(input.RewriteInput(Loader.GetFactoryInfo(factory.Value), false), query.ToArray());
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch (RequiresConfigurationException ex)
                    {
                        outConfigurationExceptions.Add(ex);
                        continue;
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var result in results)
                    {
                        yield return result;
                    }
                }
            }
        }
    }
}