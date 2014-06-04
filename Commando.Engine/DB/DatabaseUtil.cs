using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using twomindseye.Commando.Engine.Load;

namespace twomindseye.Commando.Engine.DB
{
    static class DatabaseUtil
    {
        public static SqlCeConnection GetConnection()
        {
            var builder = new SqlCeConnectionStringBuilder();
            builder.DataSource = Path.Combine(Loader.EngineDirectory, "Commando.sdf");
            var conn = new SqlCeConnection(builder.ConnectionString);
            conn.Open();
            return conn;
        }

        // the Init methods are called by Loader in a single-threaded process, so no need to be threadsafe

        public static T? ExecuteNullableScalar<T>(string commandText, params object[] paramNamesAndValues)
            where T : struct
        {
            var rvl = ExecuteScalar<object>(commandText, paramNamesAndValues);
            return rvl == null || rvl == DBNull.Value ? (T?) null : (T) rvl;
        }

        public static T ExecuteScalar<T>(string commandText, params object[] paramNamesAndValues)
            where T : class
        {
            var rvl = default(T);
            ExecuteCommand(commandText, paramNamesAndValues, cmd => rvl = cmd.ExecuteScalar() as T);
            return rvl;
        }

        public static IDictionary<string, object> SelectSingleRow(string commandText, params object[] paramNamesAndValues)
        {
            Dictionary<string, object> rvl = null;

            Action<SqlCeCommand> getRow =
                cmd =>
                {
                    using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (!reader.Read())
                        {
                            return;
                        }

                        rvl = new Dictionary<string, object>();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var val = reader.GetValue(i);
                            rvl[reader.GetName(i)] = val == DBNull.Value ? null : val;
                        }
                    }
                };

            ExecuteCommand(commandText, paramNamesAndValues, getRow);

            return rvl;
        }

        public static int? ExecuteInsert(string commandText, params object[] paramNamesAndValues)
        {
            int? rvl = null;
            ExecuteCommand(commandText, paramNamesAndValues, 
                cmd =>
                {
                    if (cmd.ExecuteNonQuery() == 1)
                    {
                        rvl = GetLastInsertedId(cmd.Connection);
                    }
                });
            return rvl;
        }

        public static int ExecuteNonQuery(string commandText, params object[] paramNamesAndValues)
        {
            var rvl = 0;
            ExecuteCommand(commandText, paramNamesAndValues, cmd => rvl = cmd.ExecuteNonQuery());
            return rvl;
        }

        static void ExecuteCommand(string commandText, IList<object> paramNamesAndValues, Action<SqlCeCommand> executeAction)
        {
            ExecuteCommand(null, commandText, paramNamesAndValues, executeAction);
        }

        static void ExecuteCommand(SqlCeConnection connection, string commandText, IList<object> paramNamesAndValues, Action<SqlCeCommand> executeAction)
        {
            var localConnection = connection;

            if (localConnection == null)
            {
                localConnection = GetConnection();
            }

            try
            {
                var command = new SqlCeCommand(commandText, localConnection);

                for (var i = 0; i < paramNamesAndValues.Count; i += 2)
                {
                    var paramName = paramNamesAndValues[i] as string;

                    if (paramName == null)
                    {
                        throw new ArgumentException("Expected string for parameter name.");
                    }

                    if (paramName.Length == 0 || paramName[0] != '@')
                    {
                        throw new ArgumentException("Parameter name is empty, or did not begin with '@'.");
                    }

                    command.Parameters.AddWithValue(paramName, paramNamesAndValues[i + 1]);
                }

                executeAction(command);
            }
            finally
            {
                if (connection == null)
                {
                    localConnection.Dispose();
                }
            }
        }

        public static int GetLastInsertedId(this SqlCeConnection connection, SqlCeTransaction transaction = null)
        {
            using (var cmd = new SqlCeCommand("SELECT @@IDENTITY", connection, transaction))
            {
                var tmp = cmd.ExecuteScalar();
                return (int) (decimal) tmp;
            }
        }
    }
}
