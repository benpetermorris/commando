using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.Engine.DB;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Engine.Extension
{
    sealed class KeyValueStore : MarshalByRefObject, IKeyValueStore
    {
        readonly int _extensionId;

        public KeyValueStore(int extensionId)
        {
            _extensionId = extensionId;
        }

        class KeyValue
        {
            public KeyValue(int value)
            {
                Value = value;
                IntValue = value;
            }

            public KeyValue(double value)
            {
                Value = value;
                DoubleValue = value;
            }

            public KeyValue(bool value)
            {
                Value = value;
                BoolValue = value;
            }

            public KeyValue(string value)
            {
                Value = value;
                StringValue = value;
            }

            public object Value { get; private set; }
            public int? IntValue { get; private set; }
            public double? DoubleValue { get; private set; }
            public string StringValue { get; private set; }
            public bool? BoolValue { get; private set; }
        }

        object[] CreateQueryParams(string key, params object[] otherParams)
        {
            return new object[]
                   {
                       "@ExtensionId", _extensionId,
                       "@Key", key
                   }.Union(otherParams).ToArray();
        }

        KeyValue ReadValue(string key)
        {
            var row = DatabaseUtil.SelectSingleRow(
                "SELECT * FROM KeyValueStore WHERE ExtensionId = @ExtensionId AND [Key] = @Key",
                CreateQueryParams(key));

            if (row == null)
            {
                return null;
            }

            var value = row.GetOrDefault("IntValue");

            if (value != null)
            {
                return new KeyValue((int)value);
            }

            value = row.GetOrDefault("DoubleValue");

            if (value != null)
            {
                return new KeyValue((double)value);
            }

            value = row.GetOrDefault("BoolValue");

            if (value != null)
            {
                return new KeyValue((bool)value);
            }

            value = row.GetOrDefault("StringValue");

            if (value != null)
            {
                return new KeyValue((string)value);
            }

            Debug.Fail("No value in row");

            return null;
        }

        void WriteValue(string key, KeyValue value)
        {
            string colName = null;

            if (value.IntValue.HasValue)
            {
                colName = "IntValue";
            }
            else if (value.DoubleValue.HasValue)
            {
                colName = "DoubleValue";
            }
            else if (value.BoolValue != null)
            {
                colName = "BoolValue";
            }
            else if (value.StringValue != null)
            {
                colName = "StringValue";
            }
            else
            {
                Debug.Fail("No value in KeyValue");
            }

            var queryParams = CreateQueryParams(key, "@Value", value.Value);

            var cmdText = string.Format(
                "UPDATE KeyValueStore SET {0} = @Value WHERE ExtensionId = @ExtensionId AND [Key] = @Key",
                colName);

            if (DatabaseUtil.ExecuteNonQuery(cmdText, queryParams) == 1)
            {
                return;
            }

            cmdText = string.Format(
                "INSERT INTO KeyValueStore (ExtensionId, [Key], {0}) VALUES (@ExtensionId, @Key, @Value)",
                colName);

            DatabaseUtil.ExecuteNonQuery(cmdText, queryParams);
        }

        public object this[string key]
        {
            get
            {
                var value = ReadValue(key);
                return value == null ? null : value.Value;
            }
            set
            {
                if (value == null)
                {
                    RemoveValue(key);
                    return;
                }

                if (value is int)
                {
                    SetValue(key, (int)value);
                }
                else if (value is double)
                {
                    SetValue(key, (double)value);
                }
                else if (value is bool)
                {
                    SetValue(key, (bool)value);
                }
                else if (value is string)
                {
                    SetValue(key, (string)value);
                }
                else
                {
                    throw new ArgumentException("not an int, double, bool, or string", "value");
                }
            }
        }

        public int? GetInt(string key)
        {
            var value = ReadValue(key);
            return value == null || !value.IntValue.HasValue ? null : value.IntValue;
        }

        public double? GetDouble(string key)
        {
            var value = ReadValue(key);
            return value == null || !value.DoubleValue.HasValue ? null : value.DoubleValue;
        }

        public bool? GetBool(string key)
        {
            var value = ReadValue(key);
            return value == null || !value.BoolValue.HasValue ? null : value.BoolValue;
        }

        public string GetString(string key)
        {
            var value = ReadValue(key);
            return value == null ? null : value.StringValue;
        }

        public static bool IsValidValueType(Type type)
        {
            return type == typeof (int) || type == typeof (string) ||
                   type == typeof (double) || type == typeof (bool);
        }

        public string GetProtectedString(string key)
        {
            var value = GetString(key);
            
            if (value == null)
            {
                return null;
            }

            try
            {
                var bytes = StringUtil.HexToBytes(value);
                bytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                return Encoding.Unicode.GetString(bytes);
            }
            catch
            {
                return null;
            }
        }

        public void SetValue(string key, int value)
        {
            WriteValue(key, new KeyValue(value));
        }

        public void SetValue(string key, double value)
        {
            WriteValue(key, new KeyValue(value));
        }

        public void SetValue(string key, bool value)
        {
            WriteValue(key, new KeyValue(value));
        }
        
        public void SetValue(string key, string value)
        {
            if (value == null)
            {
                RemoveValue(key);
            }
            else
            {
                WriteValue(key, new KeyValue(value));
            }
        }

        public void SetProtectedString(string key, string value)
        {
            var bytes = Encoding.Unicode.GetBytes(value);
            bytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            WriteValue(key, new KeyValue(bytes.ToHexString()));
        }

        public void RemoveValue(string key)
        {
            DatabaseUtil.ExecuteNonQuery(
                "DELETE FROM KeyValueStore WHERE ExtensionId = @ExtensionId AND [Key] = @Key",
                CreateQueryParams(key));
        }
    }
}
