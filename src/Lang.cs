using System.Collections.Generic;
using System.Globalization;

namespace PgKeeper
{
    static class L
    {
        public static readonly string[] Codes = { "fa", "en", "de" };
        public static readonly string[] Names = { "فارسی", "English", "Deutsch" };
        public static string Code = "en";

        public static bool Rtl { get { return Code == "fa"; } }

        public static void Init(string saved)
        {
            var code = saved != "" ? saved : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            Code = System.Array.IndexOf(Codes, code) >= 0 ? code : "en";
        }

        public static string Get(string key, params object[] args)
        {
            var text = Strings[key][System.Array.IndexOf(Codes, Code)];
            return args.Length == 0 ? text : string.Format(text, args);
        }

        static readonly Dictionary<string, string[]> Strings = new Dictionary<string, string[]>
        {
            { "title", new[] { "PgKeeper — بکاپ خودکار PostgreSQL", "PgKeeper — automatic PostgreSQL backups", "PgKeeper — automatische PostgreSQL-Backups" } },
            { "subtitle", new[] { "بکاپ خودکار و بازگردانی دیتابیس PostgreSQL", "Scheduled PostgreSQL backups and restore", "Geplante PostgreSQL-Backups und Wiederherstellung" } },
            { "alreadyRunning", new[] { "PgKeeper از قبل در حال اجراست. آیکونش کنار ساعت ویندوز است.", "PgKeeper is already running. Its icon is next to the Windows clock.", "PgKeeper läuft bereits. Das Symbol befindet sich neben der Windows-Uhr." } },
            { "connection", new[] { "اتصال به دیتابیس", "Database connection", "Datenbankverbindung" } },
            { "host", new[] { "سرور", "Server", "Server" } },
            { "port", new[] { "پورت", "Port", "Port" } },
            { "user", new[] { "نام کاربری", "User", "Benutzer" } },
            { "password", new[] { "رمز عبور", "Password", "Passwort" } },
            { "database", new[] { "دیتابیس", "Database", "Datenbank" } },
            { "pgdumpPath", new[] { "مسیر pg_dump", "pg_dump path", "pg_dump-Pfad" } },
            { "browse", new[] { "انتخاب…", "Browse…", "Durchsuchen…" } },
            { "test", new[] { "تست اتصال", "Test connection", "Verbindung testen" } },
            { "schedule", new[] { "زمان‌بندی", "Schedule", "Zeitplan" } },
            { "enabled", new[] { "بکاپ خودکار فعال باشد", "Enable automatic backups", "Automatische Backups aktivieren" } },
            { "every", new[] { "هر", "Every", "Alle" } },
            { "hoursAnd", new[] { "ساعت و", "hours and", "Stunden und" } },
            { "minutesOnce", new[] { "دقیقه یک‌بار", "minutes", "Minuten" } },
            { "folder", new[] { "پوشه‌ی بکاپ", "Backup folder", "Backup-Ordner" } },
            { "keep", new[] { "نگه‌داشتن", "Keep", "Behalten" } },
            { "keepHint", new[] { "بکاپ آخر (صفر یعنی همه بمانند)", "latest backups (0 = keep all)", "neueste Backups (0 = alle behalten)" } },
            { "autostart", new[] { "با روشن شدن ویندوز اجرا شود", "Start with Windows", "Mit Windows starten" } },
            { "save", new[] { "ذخیره‌ی تنظیمات", "Save settings", "Einstellungen speichern" } },
            { "next", new[] { "بکاپ بعدی", "Next backup", "Nächstes Backup" } },
            { "last", new[] { "آخرین بکاپ", "Last backup", "Letztes Backup" } },
            { "backupNow", new[] { "الان بکاپ بگیر", "Back up now", "Jetzt sichern" } },
            { "restore", new[] { "بازگردانی بکاپ…", "Restore backup…", "Backup wiederherstellen…" } },
            { "openFolder", new[] { "باز کردن پوشه", "Open folder", "Ordner öffnen" } },
            { "log", new[] { "گزارش", "Log", "Protokoll" } },
            { "open", new[] { "باز کردن", "Open", "Öffnen" } },
            { "exit", new[] { "خروج", "Exit", "Beenden" } },
            { "noPgDump", new[] { "فایل pg_dump پیدا نشد. مسیرش را انتخاب کنید.", "pg_dump.exe was not found. Please choose its path.", "pg_dump.exe wurde nicht gefunden. Bitte den Pfad auswählen." } },
            { "noDatabase", new[] { "اسم دیتابیس را وارد کنید.", "Please enter the database name.", "Bitte den Datenbanknamen eingeben." } },
            { "noFolder", new[] { "پوشه‌ی بکاپ را انتخاب کنید.", "Please choose a backup folder.", "Bitte einen Backup-Ordner auswählen." } },
            { "badInterval", new[] { "فاصله‌ی بکاپ‌ها باید حداقل ۱ دقیقه باشد.", "The interval must be at least 1 minute.", "Das Intervall muss mindestens 1 Minute betragen." } },
            { "saved", new[] { "تنظیمات ذخیره شد.", "Settings saved.", "Einstellungen gespeichert." } },
            { "backingUp", new[] { "در حال گرفتن بکاپ از {0} …", "Backing up {0} …", "Sichere {0} …" } },
            { "backupOk", new[] { "بکاپ موفق: {0} ({1})", "Backup succeeded: {0} ({1})", "Backup erfolgreich: {0} ({1})" } },
            { "backupFailed", new[] { "بکاپ ناموفق بود: {0}", "Backup failed: {0}", "Backup fehlgeschlagen: {0}" } },
            { "connFailed", new[] { "اتصال برقرار نشد:\n{0}", "Could not connect:\n{0}", "Verbindung fehlgeschlagen:\n{0}" } },
            { "connOk", new[] { "اتصال برقرار است.\n{0}", "Connection works.\n{0}", "Verbindung erfolgreich.\n{0}" } },
            { "versions", new[] { "نسخه‌ی دیتابیس: {0}\nنسخه‌ی pg_dump: {1}", "Server version: {0}\npg_dump version: {1}", "Serverversion: {0}\npg_dump-Version: {1}" } },
            { "restoreConfirm", new[] {
                "محتوای دیتابیس «{0}» با این بکاپ جایگزین می‌شود.\nاگر این دیتابیس از قبل وجود داشته باشد، اول یک بکاپ ایمنی از وضعیت فعلی‌اش گرفته می‌شود.\nبهتر است برنامه‌ای که از دیتابیس استفاده می‌کند خاموش باشد.\n\nادامه می‌دهید؟",
                "The contents of database \"{0}\" will be replaced with this backup.\nIf the database already exists, a safety backup of its current state is taken first.\nIt is best to stop any program that uses the database.\n\nContinue?",
                "Der Inhalt der Datenbank \"{0}\" wird durch dieses Backup ersetzt.\nFalls die Datenbank bereits existiert, wird zuerst ein Sicherheits-Backup des aktuellen Stands erstellt.\nAm besten alle Programme beenden, die die Datenbank verwenden.\n\nFortfahren?" } },
            { "busy", new[] { "یک بکاپ در حال انجام است. چند لحظه بعد دوباره امتحان کنید.", "A backup is running. Please try again in a moment.", "Ein Backup läuft gerade. Bitte gleich noch einmal versuchen." } },
            { "restoreFailed", new[] { "بازگردانی ناموفق بود: {0}", "Restore failed: {0}", "Wiederherstellung fehlgeschlagen: {0}" } },
            { "restoreFailedAlert", new[] { "بازگردانی ناموفق بود و هیچ جدولی تغییر نکرد.\n\n{0}", "The restore failed and no tables were changed.\n\n{0}", "Die Wiederherstellung ist fehlgeschlagen, es wurden keine Tabellen geändert.\n\n{0}" } },
            { "restoreOk", new[] { "بازگردانی موفق: {0} ← {1}", "Restore succeeded: {0} → {1}", "Wiederherstellung erfolgreich: {0} → {1}" } },
            { "restoreOkAlert", new[] { "بازگردانی با موفقیت انجام شد.", "The restore completed successfully.", "Die Wiederherstellung war erfolgreich." } },
            { "restoreTitle", new[] { "بازگردانی بکاپ", "Restore backup", "Backup wiederherstellen" } },
            { "backupFile", new[] { "فایل بکاپ", "Backup file", "Backup-Datei" } },
            { "targetDb", new[] { "دیتابیس مقصد", "Target database", "Zieldatenbank" } },
            { "createdHint", new[] { "اگر این دیتابیس وجود نداشته باشد، ساخته می‌شود.", "If this database does not exist, it will be created.", "Falls diese Datenbank nicht existiert, wird sie angelegt." } },
            { "restoreButton", new[] { "بازگردانی", "Restore", "Wiederherstellen" } },
            { "cancel", new[] { "انصراف", "Cancel", "Abbrechen" } },
            { "dumpFilter", new[] { "بکاپ PostgreSQL (*.dump)|*.dump|همه‌ی فایل‌ها|*.*", "PostgreSQL backup (*.dump)|*.dump|All files|*.*", "PostgreSQL-Backup (*.dump)|*.dump|Alle Dateien|*.*" } },
            { "fileMissing", new[] { "فایل بکاپ پیدا نشد.", "The backup file was not found.", "Die Backup-Datei wurde nicht gefunden." } },
            { "targetMissing", new[] { "اسم دیتابیس مقصد را وارد کنید.", "Please enter the target database name.", "Bitte den Namen der Zieldatenbank eingeben." } },
            { "folderMissing", new[] { "این پوشه هنوز وجود ندارد.", "This folder does not exist yet.", "Dieser Ordner existiert noch nicht." } },
            { "running", new[] { "در حال انجام…", "In progress…", "Läuft…" } },
            { "off", new[] { "خاموش", "Off", "Aus" } },
            { "offSub", new[] { "بکاپ خودکار فعال نیست", "Automatic backups are off", "Automatische Backups sind aus" } },
            { "noneYet", new[] { "هنوز بکاپی گرفته نشده", "No backup yet", "Noch kein Backup" } },
            { "ok", new[] { "موفق ✓", "Succeeded ✓", "Erfolgreich ✓" } },
            { "failed", new[] { "ناموفق ✗", "Failed ✗", "Fehlgeschlagen ✗" } },
            { "tipNext", new[] { "بکاپ بعدی: {0}", "Next backup: {0}", "Nächstes Backup: {0}" } },
            { "tipOff", new[] { "بکاپ خودکار خاموش است", "Automatic backups are off", "Automatische Backups sind aus" } },
            { "lessMinute", new[] { "کمتر از یک دقیقه", "Less than a minute", "Unter einer Minute" } },
            { "inMinutes", new[] { "{0} دقیقه دیگر", "In {0} min", "In {0} Min." } },
            { "inHours", new[] { "{0} ساعت دیگر", "In {0} h", "In {0} Std." } },
            { "inHoursMinutes", new[] { "{0} ساعت و {1} دقیقه دیگر", "In {0} h {1} min", "In {0} Std. {1} Min." } },
            { "trayHint", new[] { "برنامه کنار ساعت ویندوز باز می‌ماند و بکاپ‌ها ادامه دارند.", "PgKeeper keeps running next to the clock and backups continue.", "PgKeeper läuft neben der Uhr weiter, die Backups gehen weiter." } },
            { "safetyBackup", new[] { "گرفتن بکاپ ایمنی از وضعیت فعلی {0} …", "Taking a safety backup of {0} …", "Erstelle Sicherheits-Backup von {0} …" } },
            { "safetyDone", new[] { "بکاپ ایمنی گرفته شد: {0}", "Safety backup saved: {0}", "Sicherheits-Backup gespeichert: {0}" } },
            { "dbCreated", new[] { "دیتابیس {0} ساخته شد.", "Database {0} created.", "Datenbank {0} angelegt." } },
            { "restoring", new[] { "در حال بازگردانی {0} در {1} …", "Restoring {0} into {1} …", "Stelle {0} in {1} wieder her …" } },
            { "toolMissing", new[] { "{0}.exe کنار pg_dump پیدا نشد.", "{0}.exe was not found next to pg_dump.", "{0}.exe wurde neben pg_dump nicht gefunden." } },
        };
    }
}
