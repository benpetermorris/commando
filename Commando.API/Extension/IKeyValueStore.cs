namespace twomindseye.Commando.API1.Extension
{
    public interface IKeyValueStore
    {
        object this[string key] { get; set; }
        
        int? GetInt(string key);
        double? GetDouble(string key);
        bool? GetBool(string key);
        string GetString(string key);

        void SetValue(string key, int value);
        void SetValue(string key, double value);
        void SetValue(string key, bool value);
        void SetValue(string key, string value);

        void SetProtectedString(string key, string value);
        string GetProtectedString(string key);

        void RemoveValue(string key);
    }
}
