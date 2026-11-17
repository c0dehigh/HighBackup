using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace HighBackup
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            bool created;
            using (new Mutex(true, "HighBackup.SingleInstance", out created))
            {
                if (!created)
                {
                    MessageBox.Show("HighBackup از قبل در حال اجراست. آیکونش کنار ساعت ویندوز است.", "HighBackup",
                        MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(args.Contains("/tray")));
            }
        }

        public static Icon AppIcon(int size)
        {
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream("HighBackup.app.ico"))
                return new Icon(stream, size, size);
        }
    }
}
