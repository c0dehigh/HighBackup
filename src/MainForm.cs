using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HighBackup
{
    public class MainForm : Form
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        Settings s;
        TextBox host, user, pass, db, pgdump, folder;
        RichTextBox log;
        NumericUpDown port, hours, minutes, keep;
        CheckBox enabled, autostart;
        Button testBtn, nowBtn, restoreBtn;
        Label nextValue, nextSub, lastValue, lastSub, dot;
        NotifyIcon tray;
        TableLayoutPanel root, settingsColumn;
        Header header;
        bool running, allowShow, exiting, hintShown;

        public MainForm(bool startHidden)
        {
            allowShow = !startHidden;
            s = Settings.Load();
            if (s.PgDump == "" || !File.Exists(s.PgDump)) s.PgDump = Dump.Find();
            if (s.Enabled && s.NextRunTicks == 0) s.NextRunTicks = (DateTime.Now + s.Interval).Ticks;

            BuildUi();
            Fill();
            LoadLog();
            UpdateStatus();

            var timer = new Timer { Interval = 1000 };
            timer.Tick += (o, e) => Tick();
            timer.Start();
        }

        void BuildUi()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "HighBackup — بکاپ خودکار PostgreSQL";
            Font = new Font("Segoe UI", 9.75f);
            ForeColor = Theme.Text;
            BackColor = Theme.Back;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            ClientSize = new Size(1120, 660);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Program.AppIcon(32);

            var conn = new Card("اتصال به دیتابیس");
            var connGrid = Grid();
            host = Field(connGrid, "سرور", Input());
            port = Field(connGrid, "پورت", Num(1, 65535));
            user = Field(connGrid, "نام کاربری", Input());
            pass = Field(connGrid, "رمز عبور", Input());
            pass.UseSystemPasswordChar = true;
            db = Field(connGrid, "دیتابیس", Input());
            pgdump = Field(connGrid, "مسیر pg_dump", Input(), Theme.Button("انتخاب…", PickPgDump));
            testBtn = Theme.Button("تست اتصال", TestConnection);
            Field(connGrid, "", testBtn);
            conn.Add(connGrid);

            var sched = new Card("زمان‌بندی");
            var schedGrid = Grid();
            enabled = Field(schedGrid, "", new CheckBox { Text = "بکاپ خودکار فعال باشد", AutoSize = true });
            hours = Num(0, 999);
            minutes = Num(0, 59);
            Field(schedGrid, "هر", Row(hours, Caption("ساعت و"), minutes, Caption("دقیقه یک‌بار")));
            folder = Field(schedGrid, "پوشه‌ی بکاپ", Input(), Theme.Button("انتخاب…", PickFolder));
            keep = Num(0, 9999);
            Field(schedGrid, "نگه‌داشتن", Row(keep, Caption("بکاپ آخر (صفر یعنی همه بمانند)")));
            autostart = Field(schedGrid, "", new CheckBox { Text = "با روشن شدن ویندوز اجرا شود", AutoSize = true });
            Field(schedGrid, "", Theme.Button("ذخیره‌ی تنظیمات", SaveClicked, true));
            sched.Add(schedGrid);

            var status = new Card(null);
            var statusGrid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, BackColor = Theme.Surface };
            statusGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statusGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            dot = new Label { Text = "●", AutoSize = true, Margin = new Padding(0, 0, 0, 0) };
            statusGrid.Controls.Add(Row(dot, Caption("بکاپ بعدی", Theme.Muted)), 0, 0);
            statusGrid.Controls.Add(Caption("آخرین بکاپ", Theme.Muted), 1, 0);
            nextValue = BigValue();
            lastValue = BigValue();
            statusGrid.Controls.Add(nextValue, 0, 1);
            statusGrid.Controls.Add(lastValue, 1, 1);
            nextSub = Caption("", Theme.Muted);
            lastSub = Caption("", Theme.Muted);
            statusGrid.Controls.Add(nextSub, 0, 2);
            statusGrid.Controls.Add(lastSub, 1, 2);
            status.Add(statusGrid);

            nowBtn = Theme.Button("الان بکاپ بگیر", BackupNow, true);
            restoreBtn = Theme.Button("بازگردانی بکاپ…", RestoreClicked);
            var actions = Row(nowBtn, restoreBtn, Theme.Button("باز کردن پوشه", OpenFolder));
            actions.Margin = new Padding(2, 0, 2, 2);

            var logCard = new Card("گزارش", true);
            log = new RichTextBox { ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ScrollBars = RichTextBoxScrollBars.Vertical, DetectUrls = false };
            logCard.Add(log, true);

            settingsColumn = Column(conn, sched);
            var left = Column(status, actions, logCard);
            left.RowStyles[2] = new RowStyle(SizeType.Percent, 100);

            root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(settingsColumn, 0, 0);
            root.Controls.Add(left, 1, 0);
            Controls.Add(root);
            header = new Header();
            Controls.Add(header);

            var menu = new ContextMenuStrip { RightToLeft = RightToLeft.Yes };
            menu.Items.Add("باز کردن", null, (o, e) => ShowMain());
            menu.Items.Add("الان بکاپ بگیر", null, (o, e) => RunBackup());
            menu.Items.Add("خروج", null, (o, e) => ExitApp());
            tray = new NotifyIcon { Icon = Program.AppIcon(SystemInformation.SmallIconSize.Width), ContextMenuStrip = menu, Visible = true, Text = "HighBackup" };
            tray.DoubleClick += (o, e) => ShowMain();

            ResumeLayout(false);
            PerformLayout();
        }

        static TableLayoutPanel Column(params Control[] items)
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Margin = new Padding(0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (var item in items)
            {
                t.Controls.Add(item);
                t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            return t;
        }

        static TableLayoutPanel Grid()
        {
            var t = new TableLayoutPanel { ColumnCount = 3, AutoSize = true, BackColor = Theme.Surface };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            return t;
        }

        static T Field<T>(TableLayoutPanel t, string label, T control, Control extra = null) where T : Control
        {
            var row = t.RowCount++;
            t.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Theme.Muted, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 12, 6) }, 0, row);
            control.Anchor = control is TextBox ? AnchorStyles.Left | AnchorStyles.Right : AnchorStyles.Left;
            t.Controls.Add(control, 1, row);
            if (extra != null) t.Controls.Add(extra, 2, row);
            return control;
        }

        static FlowLayoutPanel Row(params Control[] items)
        {
            var f = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = Color.Transparent };
            f.Controls.AddRange(items);
            return f;
        }

        static TextBox Input()
        {
            return new TextBox { BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(3, 5, 3, 5) };
        }

        static NumericUpDown Num(int min, int max)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Width = 80, TextAlign = HorizontalAlignment.Center, BorderStyle = BorderStyle.FixedSingle };
        }

        static Label Caption(string text, Color? color = null)
        {
            return new Label { Text = text, AutoSize = true, ForeColor = color ?? Theme.Text, Margin = new Padding(4, 6, 8, 0) };
        }

        static Label BigValue()
        {
            return new Label { AutoSize = true, Font = new Font("Segoe UI", 14f, FontStyle.Bold), Margin = new Padding(2, 4, 2, 2) };
        }

        void Fill()
        {
            host.Text = s.Host;
            port.Value = Math.Max(1, Math.Min(65535, s.Port));
            user.Text = s.User;
            pass.Text = Settings.Unprotect(s.Password);
            db.Text = s.Database;
            pgdump.Text = s.PgDump;
            enabled.Checked = s.Enabled;
            hours.Value = Math.Min(999, Math.Max(0, s.Hours));
            minutes.Value = Math.Min(59, Math.Max(0, s.Minutes));
            folder.Text = s.Folder;
            keep.Value = Math.Min(9999, Math.Max(0, s.Keep));
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                autostart.Checked = key != null && key.GetValue("HighBackup") != null;
        }

        Settings Read()
        {
            return new Settings
            {
                Host = host.Text.Trim(),
                Port = (int)port.Value,
                User = user.Text.Trim(),
                Password = Settings.Protect(pass.Text),
                Database = db.Text.Trim(),
                PgDump = pgdump.Text.Trim(),
                Folder = folder.Text.Trim(),
                Hours = (int)hours.Value,
                Minutes = (int)minutes.Value,
                Keep = (int)keep.Value,
                Enabled = enabled.Checked,
                NextRunTicks = s.NextRunTicks,
                LastRunTicks = s.LastRunTicks,
                LastRunOk = s.LastRunOk,
                LastRunSize = s.LastRunSize,
            };
        }

        static string Validate(Settings t, bool needFolder)
        {
            if (!File.Exists(t.PgDump)) return "فایل pg_dump پیدا نشد. مسیرش را انتخاب کنید.";
            if (t.Database == "") return "اسم دیتابیس را وارد کنید.";
            if (needFolder && t.Folder == "") return "پوشه‌ی بکاپ را انتخاب کنید.";
            if (t.Enabled && t.Interval <= TimeSpan.Zero) return "فاصله‌ی بکاپ‌ها باید حداقل ۱ دقیقه باشد.";
            return null;
        }

        void Apply(Settings t)
        {
            if (!t.Enabled) t.NextRunTicks = 0;
            else if (!s.Enabled || t.Interval != s.Interval || t.NextRunTicks == 0) t.NextRunTicks = (DateTime.Now + t.Interval).Ticks;
            s = t;
            s.Save();
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (autostart.Checked) key.SetValue("HighBackup", "\"" + Application.ExecutablePath + "\" /tray");
                else key.DeleteValue("HighBackup", false);
            }
            UpdateStatus();
        }

        bool SaveFromForm(bool needFolder)
        {
            var t = Read();
            var error = Validate(t, needFolder || t.Enabled);
            if (error != null)
            {
                Alert(error, MessageBoxIcon.Warning);
                return false;
            }
            Apply(t);
            return true;
        }

        void SaveClicked()
        {
            if (SaveFromForm(false)) Log("تنظیمات ذخیره شد.");
        }

        void BackupNow()
        {
            if (SaveFromForm(true)) RunBackup();
        }

        void Tick()
        {
            UpdateStatus();
            if (!s.Enabled || running || s.NextRunTicks == 0 || s.Interval <= TimeSpan.Zero) return;
            var now = DateTime.Now;
            var next = new DateTime(s.NextRunTicks);
            if (now < next) return;
            while (next <= now) next += s.Interval;
            s.NextRunTicks = next.Ticks;
            s.Save();
            RunBackup();
        }

        void RunBackup()
        {
            if (running) return;
            var error = Validate(s, true);
            if (error != null)
            {
                Log("بکاپ ناموفق بود: " + error);
                Notify(error, ToolTipIcon.Error);
                return;
            }
            SetBusy(true);
            Log("در حال گرفتن بکاپ از " + s.Database + " …");
            var cfg = s;
            var password = Settings.Unprotect(s.Password);
            Task.Run(() => Dump.Backup(cfg, password)).ContinueWith(t => BeginInvoke((Action)(() =>
            {
                SetBusy(false);
                s.LastRunTicks = DateTime.Now.Ticks;
                s.LastRunOk = !t.IsFaulted;
                if (t.IsFaulted)
                {
                    var message = t.Exception.GetBaseException().Message;
                    s.LastRunSize = "";
                    Log("بکاپ ناموفق بود: " + message);
                    Notify("بکاپ ناموفق بود: " + message, ToolTipIcon.Error);
                }
                else
                {
                    s.LastRunSize = FormatSize(new FileInfo(t.Result).Length);
                    Log("بکاپ موفق: " + Path.GetFileName(t.Result) + " (" + s.LastRunSize + ")");
                }
                s.Save();
                UpdateStatus();
            })));
        }

        void SetBusy(bool busy)
        {
            running = busy;
            nowBtn.Enabled = restoreBtn.Enabled = !busy;
            UpdateStatus();
        }

        void TestConnection()
        {
            var t = Read();
            var error = Validate(t, false);
            if (error != null)
            {
                Alert(error, MessageBoxIcon.Warning);
                return;
            }
            testBtn.Enabled = false;
            var password = pass.Text;
            Task.Run(() => Dump.Test(t, password)).ContinueWith(r => BeginInvoke((Action)(() =>
            {
                testBtn.Enabled = true;
                if (r.IsFaulted) Alert("اتصال برقرار نشد:\n" + r.Exception.GetBaseException().Message, MessageBoxIcon.Error);
                else Alert("اتصال برقرار است.\n" + r.Result, MessageBoxIcon.Information);
            })));
        }

        void RestoreClicked()
        {
            var t = Read();
            var error = Validate(t, true);
            if (error != null)
            {
                Alert(error, MessageBoxIcon.Warning);
                return;
            }
            string file, target;
            if (!AskRestore(t, out file, out target)) return;
            var confirm = "محتوای دیتابیس «" + target + "» با این بکاپ جایگزین می‌شود.\n" +
                "اگر این دیتابیس از قبل وجود داشته باشد، اول یک بکاپ ایمنی از وضعیت فعلی‌اش گرفته می‌شود.\n" +
                "بهتر است سرور بازی خاموش باشد.\n\nادامه می‌دهید؟";
            if (MessageBox.Show(this, confirm, "HighBackup", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) != DialogResult.Yes) return;
            if (running)
            {
                Alert("یک بکاپ در حال انجام است. چند لحظه بعد دوباره امتحان کنید.", MessageBoxIcon.Information);
                return;
            }
            SetBusy(true);
            var password = pass.Text;
            Action<string> progress = m => BeginInvoke((Action)(() => Log(m)));
            Task.Run(() => Dump.Restore(t, password, file, target, progress)).ContinueWith(r => BeginInvoke((Action)(() =>
            {
                SetBusy(false);
                if (r.IsFaulted)
                {
                    var message = r.Exception.GetBaseException().Message;
                    Log("بازگردانی ناموفق بود: " + message);
                    Alert("بازگردانی ناموفق بود و هیچ جدولی تغییر نکرد.\n\n" + message, MessageBoxIcon.Error);
                }
                else
                {
                    Log("بازگردانی موفق: " + Path.GetFileName(file) + " ← " + target);
                    Alert("بازگردانی با موفقیت انجام شد.", MessageBoxIcon.Information);
                }
            })));
        }

        bool AskRestore(Settings t, out string file, out string target)
        {
            file = target = null;
            using (var dialog = new Form
            {
                Text = "بازگردانی بکاپ",
                Font = Font,
                ForeColor = Theme.Text,
                BackColor = Theme.Surface,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterParent,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(14),
            })
            {
                var grid = Grid();
                var fileBox = Input();
                fileBox.Width = 380;
                Field(grid, "فایل بکاپ", fileBox, Theme.Button("انتخاب…", () =>
                {
                    using (var pick = new OpenFileDialog { Filter = "بکاپ PostgreSQL (*.dump)|*.dump|همه‌ی فایل‌ها|*.*", InitialDirectory = Directory.Exists(t.Folder) ? t.Folder : "" })
                        if (pick.ShowDialog(dialog) == DialogResult.OK) fileBox.Text = pick.FileName;
                }));
                var dbBox = Field(grid, "دیتابیس مقصد", Input());
                dbBox.Text = t.Database;
                Field(grid, "", Caption("اگر این دیتابیس وجود نداشته باشد، ساخته می‌شود.", Theme.Muted));

                var ok = Theme.Button("بازگردانی", () => { }, true);
                ok.DialogResult = DialogResult.OK;
                var cancel = Theme.Button("انصراف", () => { });
                cancel.DialogResult = DialogResult.Cancel;
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                var buttons = Row(ok, cancel);
                buttons.Margin = new Padding(0, 10, 0, 0);

                var root = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
                root.Controls.Add(grid);
                root.Controls.Add(buttons);
                dialog.Controls.Add(root);

                if (dialog.ShowDialog(this) != DialogResult.OK) return false;
                file = fileBox.Text.Trim();
                target = dbBox.Text.Trim();
            }
            if (!File.Exists(file))
            {
                Alert("فایل بکاپ پیدا نشد.", MessageBoxIcon.Warning);
                return false;
            }
            if (target == "")
            {
                Alert("اسم دیتابیس مقصد را وارد کنید.", MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        void PickPgDump()
        {
            using (var dialog = new OpenFileDialog { Filter = "pg_dump.exe|pg_dump.exe", FileName = pgdump.Text })
                if (dialog.ShowDialog(this) == DialogResult.OK) pgdump.Text = dialog.FileName;
        }

        void PickFolder()
        {
            using (var dialog = new FolderBrowserDialog { SelectedPath = folder.Text, ShowNewFolderButton = true })
                if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = dialog.SelectedPath;
        }

        void OpenFolder()
        {
            var path = folder.Text.Trim();
            if (Directory.Exists(path)) Process.Start("explorer.exe", "\"" + path + "\"");
            else Alert("این پوشه هنوز وجود ندارد.", MessageBoxIcon.Warning);
        }

        void UpdateStatus()
        {
            var on = s.Enabled && s.NextRunTicks > 0;
            dot.ForeColor = running ? Theme.Primary : on ? Theme.Green : Theme.Muted;
            if (running)
            {
                nextValue.Text = "در حال انجام…";
                nextValue.ForeColor = Theme.Primary;
            }
            else
            {
                nextValue.Text = on ? Countdown(new DateTime(s.NextRunTicks) - DateTime.Now) : "خاموش";
                nextValue.ForeColor = on ? Theme.Text : Theme.Muted;
            }
            nextSub.Text = on ? FormatDate(new DateTime(s.NextRunTicks)) : "بکاپ خودکار فعال نیست";

            if (s.LastRunTicks == 0)
            {
                lastValue.Text = "—";
                lastValue.ForeColor = Theme.Muted;
                lastSub.Text = "هنوز بکاپی گرفته نشده";
            }
            else
            {
                lastValue.Text = s.LastRunOk ? "موفق ✓" : "ناموفق ✗";
                lastValue.ForeColor = s.LastRunOk ? Theme.Green : Theme.Red;
                lastSub.Text = FormatDate(new DateTime(s.LastRunTicks)) + (s.LastRunSize != "" ? " · ‎" + s.LastRunSize : "");
            }
            var tip = "HighBackup — " + (on ? "بکاپ بعدی: " + nextSub.Text : "بکاپ خودکار خاموش است");
            if (tray.Text != tip) tray.Text = tip;
        }

        static string Countdown(TimeSpan left)
        {
            if (left.TotalMinutes < 1) return "کمتر از یک دقیقه";
            var h = (int)left.TotalHours;
            var m = left.Minutes;
            if (h == 0) return m + " دقیقه دیگر";
            return m == 0 ? h + " ساعت دیگر" : h + " ساعت و " + m + " دقیقه دیگر";
        }

        void Log(string message)
        {
            var line = "[" + FormatDate(DateTime.Now) + "] " + message;
            Append(line);
            try { File.AppendAllText(Settings.LogPath, line + Environment.NewLine); }
            catch (IOException) { }
        }

        void Append(string line)
        {
            log.SelectionStart = log.TextLength;
            log.SelectionLength = 0;
            log.SelectionColor = line.Contains("ناموفق") ? Theme.Red : line.Contains("موفق") ? Theme.Green : Color.FromArgb(51, 65, 85);
            log.SelectedText = line + "\n";
            log.ScrollToCaret();
        }

        void LoadLog()
        {
            if (!File.Exists(Settings.LogPath)) return;
            var lines = File.ReadAllLines(Settings.LogPath);
            if (lines.Length > 1000)
            {
                lines = lines.Skip(lines.Length - 1000).ToArray();
                File.WriteAllLines(Settings.LogPath, lines);
            }
            foreach (var line in lines.Skip(Math.Max(0, lines.Length - 200))) Append(line);
        }

        void Notify(string message, ToolTipIcon icon)
        {
            tray.ShowBalloonTip(5000, "HighBackup", message, icon);
        }

        void Alert(string message, MessageBoxIcon icon)
        {
            MessageBox.Show(this, message, "HighBackup", MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
        }

        static string FormatDate(DateTime d)
        {
            var pc = new PersianCalendar();
            return string.Format("{0:0000}/{1:00}/{2:00} {3:00}:{4:00}", pc.GetYear(d), pc.GetMonth(d), pc.GetDayOfMonth(d), d.Hour, d.Minute);
        }

        static string FormatSize(long bytes)
        {
            return bytes >= 1048576 ? (bytes / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) + " MB" : (bytes / 1024.0).ToString("0", CultureInfo.InvariantCulture) + " KB";
        }

        void ShowMain()
        {
            allowShow = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        void ExitApp()
        {
            exiting = true;
            tray.Visible = false;
            Application.Exit();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var need = settingsColumn.GetPreferredSize(new Size(settingsColumn.Width, 0)).Height + root.Padding.Vertical;
            ClientSize = new Size(ClientSize.Width, header.Height + need);
            MinimumSize = Size;
            CenterToScreen();
            ActiveControl = nowBtn;
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!allowShow)
            {
                value = false;
                if (!IsHandleCreated) CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                if (!hintShown)
                {
                    hintShown = true;
                    Notify("برنامه کنار ساعت ویندوز باز می‌ماند و بکاپ‌ها ادامه دارند.", ToolTipIcon.Info);
                }
                return;
            }
            tray.Visible = false;
            base.OnFormClosing(e);
        }
    }
}
