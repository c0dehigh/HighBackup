using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HighBackup
{
    static class Dump
    {
        public static string Find()
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL");
            if (Directory.Exists(root))
            {
                var best = Directory.GetDirectories(root)
                    .Select(d => new { Path = Path.Combine(d, "bin", "pg_dump.exe"), Version = ParseVersion(Path.GetFileName(d)) })
                    .Where(x => File.Exists(x.Path))
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault();
                if (best != null) return best.Path;
            }
            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
            {
                try
                {
                    var path = Path.Combine(dir.Trim(), "pg_dump.exe");
                    if (File.Exists(path)) return path;
                }
                catch (ArgumentException) { }
            }
            return "";
        }

        public static string Backup(Settings s, string password)
        {
            Directory.CreateDirectory(s.Folder);
            var file = Path.Combine(s.Folder, s.Database + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + ".dump");
            var part = file + ".part";
            try
            {
                Run(s, password, "pg_dump", "-F c -f " + Quote(part) + " -d " + Quote(s.Database));
            }
            catch
            {
                File.Delete(part);
                throw;
            }
            File.Move(part, file);
            Prune(s);
            return file;
        }

        public static string Test(Settings s, string password)
        {
            var output = Run(s, password, "pg_dump", "-s -N \"*\" -d " + Quote(s.Database));
            var server = Regex.Match(output, @"Dumped from database version (\S+)").Groups[1].Value;
            var client = Regex.Match(output, @"Dumped by pg_dump version (\S+)").Groups[1].Value;
            return "نسخه‌ی دیتابیس: " + server + "\nنسخه‌ی pg_dump: " + client;
        }

        public static void Restore(Settings s, string password, string file, string target, Action<string> progress)
        {
            var exists = Run(s, password, "psql", "-X -tA -d postgres -c " + Quote("select 1 from pg_database where datname = '" + target.Replace("'", "''") + "'")).Trim() == "1";
            if (exists)
            {
                progress("گرفتن بکاپ ایمنی از وضعیت فعلی " + target + " …");
                progress("بکاپ ایمنی گرفته شد: " + Path.GetFileName(Backup(s.With(target), password)));
            }
            else
            {
                Run(s, password, "psql", "-X -d postgres -c " + Quote("CREATE DATABASE \"" + target.Replace("\"", "\"\"") + "\""));
                progress("دیتابیس " + target + " ساخته شد.");
            }
            progress("در حال بازگردانی " + Path.GetFileName(file) + " در " + target + " …");
            Run(s, password, "pg_restore", "--no-owner --no-privileges --single-transaction " + (exists ? "--clean --if-exists " : "") + "-d " + Quote(target) + " " + Quote(file), "-c lock_timeout=30000");
        }

        static string Run(Settings s, string password, string tool, string args, string options = null)
        {
            var exe = Path.Combine(Path.GetDirectoryName(s.PgDump), tool + ".exe");
            if (!File.Exists(exe)) throw new Exception(tool + ".exe کنار pg_dump پیدا نشد.");
            var psi = new ProcessStartInfo(exe, string.Format("-h {0} -p {1} -U {2} -w {3}", Quote(s.Host), s.Port, Quote(s.User), args))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            psi.EnvironmentVariables["PGPASSWORD"] = password;
            if (options != null) psi.EnvironmentVariables["PGOPTIONS"] = options;
            using (var process = Process.Start(psi))
            {
                var stderr = process.StandardError.ReadToEndAsync();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                var error = stderr.Result.Trim();
                if (process.ExitCode != 0) throw new Exception(error != "" ? error : tool + " exit code " + process.ExitCode);
                return output;
            }
        }

        static void Prune(Settings s)
        {
            if (s.Keep <= 0) return;
            var pattern = new Regex("^" + Regex.Escape(s.Database) + @"_\d{4}-\d\d-\d\d_\d\d-\d\d-\d\d\.dump$");
            var old = new DirectoryInfo(s.Folder).GetFiles("*.dump")
                .Where(f => pattern.IsMatch(f.Name))
                .OrderByDescending(f => f.Name, StringComparer.Ordinal)
                .Skip(s.Keep);
            foreach (var f in old) f.Delete();
        }

        static double ParseVersion(string name)
        {
            double v;
            return double.TryParse(name, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0;
        }

        static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
