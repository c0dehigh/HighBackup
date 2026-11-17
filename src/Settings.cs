using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace HighBackup
{
    public class Settings
    {
        public string Host = "127.0.0.1";
        public int Port = 5432;
        public string User = "postgres";
        public string Password = "";
        public string Database = "";
        public string PgDump = "";
        public string Folder = "";
        public int Hours = 5;
        public int Minutes = 0;
        public int Keep = 20;
        public bool Enabled = false;
        public long NextRunTicks = 0;
        public long LastRunTicks = 0;
        public bool LastRunOk = false;
        public string LastRunSize = "";

        public TimeSpan Interval { get { return new TimeSpan(Hours, Minutes, 0); } }

        public Settings With(string database)
        {
            var copy = (Settings)MemberwiseClone();
            copy.Database = database;
            return copy;
        }

        static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HighBackup");
        static readonly string FilePath = Path.Combine(Dir, "settings.xml");
        public static readonly string LogPath = Path.Combine(Dir, "log.txt");

        public static Settings Load()
        {
            try
            {
                using (var stream = File.OpenRead(FilePath))
                    return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
            }
            catch
            {
                return new Settings();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Dir);
            using (var stream = File.Create(FilePath))
                new XmlSerializer(typeof(Settings)).Serialize(stream, this);
        }

        public static string Protect(string plain)
        {
            if (plain == "") return "";
            return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser));
        }

        public static string Unprotect(string stored)
        {
            try
            {
                return stored == "" ? "" : Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(stored), null, DataProtectionScope.CurrentUser));
            }
            catch
            {
                return "";
            }
        }
    }
}
