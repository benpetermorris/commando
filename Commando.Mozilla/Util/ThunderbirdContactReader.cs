using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Mozilla.Util
{
    sealed class ThunderbirdContactReader
    {
        ReadOnlyCollection<ReadOnlyDictionary<int, string>> _data;

        public static int? FirstNameOid { get; private set; }
        public static int? LastNameOid { get; private set; }
        public static int? DisplayNameOid { get; private set; }
        public static int? EmailOid { get; private set; }

        public bool Load()
        {
            var profilesIniDirectory = 
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Thunderbird";

            var activeProfileDirectory = ProfileManager.GetActiveProfileDirectory(profilesIniDirectory);
            if (activeProfileDirectory == null)
            {
                return false;
            }

            var addressBookPath = Path.Combine(activeProfileDirectory, "abook.mab");
            if (!File.Exists(addressBookPath))
            {
                return false;
            }

            using (var copy = new DisposableFileCopy(addressBookPath))
            {
                var p = new MorkParser();
                p.Open(copy.TempCopyPath);

                FirstNameOid = p.GetColumnOid("FirstName");
                LastNameOid = p.GetColumnOid("LastName");
                DisplayNameOid = p.GetColumnOid("DisplayName");
                EmailOid = p.GetColumnOid("PrimaryEmail");

                _data = new ReadOnlyCollection<ReadOnlyDictionary<int, string>>(
                    (from table in p.GetTables(0x80)
                     from rowScope in table.Value.Select(x => x.Value)
                     from row in p.GetRows(rowScope)
                     select new ReadOnlyDictionary<int, string>(row.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))).ToList());
            }

            return true;
        }

        public ReadOnlyCollection<ReadOnlyDictionary<int, string>> Contacts
        {
            get { return _data; }
        }

        public string GetContactValue(IDictionary<int, string> contact, int? oid)
        {
            if (!oid.HasValue)
            {
                return null;
            }

            string value;
            return contact.TryGetValue(oid.Value, out value) ? value : null;
        }
    }
}