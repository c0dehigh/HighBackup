using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PgKeeper
{
    static class Program
    {
        public static ApplicationContext Context;

        [STAThread]
        static void Main(string[] args)
        {
            var settings = Settings.Load();
            L.Init(settings.Language);
            bool created;
            using (new Mutex(true, "PgKeeper.SingleInstance", out created))
            {
                if (!created)
                {
                    MessageBox.Show(L.Get("alreadyRunning"), "PgKeeper", MessageBoxButtons.OK, MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1, BoxOptions());
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Context = new ApplicationContext(new MainForm(settings, args.Contains("/tray")));
                Application.Run(Context);
            }
        }

        public static MessageBoxOptions BoxOptions()
        {
            return L.Rtl ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : 0;
        }

        public static Icon AppIcon(int size)
        {
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream("PgKeeper.app.ico"))
                return new Icon(stream, size, size);
        }
    }
}
