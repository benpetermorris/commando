using System;
using System.IO;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Mozilla.Util
{
    static class ProfileManager
    {
        public static string GetActiveProfileDirectory(string profilesIniDirectory)
        {
            IniFile iniFile = null;

            try
            {
                iniFile = IniFile.LoadFrom(Path.Combine(profilesIniDirectory, "profiles.ini"));
            }
            catch (Exception)
            {
                return null;
            }

            string defaultProfilePath = null;

            foreach (var section in iniFile.Sections)
            {
                if (section == "General")
                {
                    continue;
                }

                var path = iniFile[section, "Path", true];
                var name = iniFile[section, "Name", true];
                var isRelative = iniFile[section, "IsRelative", true];
                var isDefault = iniFile[section, "Default", true] ?? "0";

                if (path == null || name == null || isRelative == null)
                {
                    continue;
                }

                var fullPath = isRelative == "1"
                                ? Path.Combine(profilesIniDirectory, path)
                                : path;

                if (isDefault == "1")
                {
                    return fullPath;
                }

                if (name == "default")
                {
                    defaultProfilePath = fullPath;
                }
            }

            return defaultProfilePath;
        }
    }
}
