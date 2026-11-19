using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PgKeeper
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
        ComboBox picker;
        Timer timer;
        TableLayoutPanel root, settingsColumn;
        Header header;
        bool running, allowShow, exiting, hintShown;

        public MainForm(Settings settings, bool startHidden, Settings draft = null)
        {
            allowShow = !startHidden;
            s = settings;
            if (s.PgDump == "" || !File.Exists(s.PgDump)) s.PgDump = Dump.Find();
            if (s.Enabled && s.NextRunTicks == 0) s.NextRunTicks = (DateTime.Now + s.Interval).Ticks;

            BuildUi();
            Fill(draft ?? s);
            LoadLog();
            UpdateStatus();

            timer = new Timer { Interval = 1000 };
            timer.Tick += (o, e) => Tick();
            timer.Start();
        }

        void BuildUi()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = L.Get("title");
            Font = new Font("Segoe UI", 9.75f);
            ForeColor = Theme.Text;
            BackColor = Theme.Back;
            RightToLeft = L.Rtl ? RightToLeft.Yes : RightToLeft.No;
            RightToLeftLayout = L.Rtl;
            ClientSize = new Size(1120, 660);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Program.AppIcon(32);

            var conn = new Card(L.Get("connection"));
            var connGrid = Grid();
            host = Field(connGrid, L.Get("host"), Input());
            port = Field(connGrid, L.Get("port"), Num(1, 65535));
            user = Field(connGrid, L.Get("user"), Input());
            pass = Field(connGrid, L.Get("password"), Input());
            pass.UseSystemPasswordChar = true;
            db = Field(connGrid, L.Get("database"), Input());
            pgdump = Field(connGrid, L.Get("pgdumpPath"), Input(), Theme.Button(L.Get("browse"), PickPgDump));
            testBtn = Theme.Button(L.Get("test"), TestConnection);
            Field(connGrid, "", testBtn);
            conn.Add(connGrid);

            var sched = new Card(L.Get("schedule"));
            var schedGrid = Grid();
            enabled = Field(schedGrid, "", new CheckBox { Text = L.Get("enabled"), AutoSize = true });
            hours = Num(0, 999);
            minutes = Num(0, 59);
            Field(schedGrid, L.Get("every"), Row(hours, Caption(L.Get("hoursAnd")), minutes, Caption(L.Get("minutesOnce"))));
            folder = Field(schedGrid, L.Get("folder"), Input(), Theme.Button(L.Get("browse"), PickFolder));
            keep = Num(0, 9999);
            Field(schedGrid, L.Get("keep"), Row(keep, Caption(L.Get("keepHint"))));
            autostart = Field(schedGrid, "", new CheckBox { Text = L.Get("autostart"), AutoSize = true });
            Field(schedGrid, "", Theme.Button(L.Get("save"), SaveClicked, true));
            sched.Add(schedGrid);

            var status = new Card(null);
            var statusGrid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, BackColor = Theme.Surface };
            statusGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statusGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            dot = new Label { Text = "●", AutoSize = true, Margin = new Padding(0, 0, 0, 0) };
            statusGrid.Controls.Add(Row(dot, Caption(L.Get("next"), Theme.Muted)), 0, 0);
            statusGrid.Controls.Add(Caption(L.Get("last"), Theme.Muted), 1, 0);
            nextValue = BigValue();
            lastValue = BigValue();
            statusGrid.Controls.Add(nextValue, 0, 1);
            statusGrid.Controls.Add(lastValue, 1, 1);
            nextSub = Caption("", Theme.Muted);
            lastSub = Caption("", Theme.Muted);
            statusGrid.Controls.Add(nextSub, 0, 2);
            statusGrid.Controls.Add(lastSub, 1, 2);
            status.Add(statusGrid);

            nowBtn = Theme.Button(L.Get("backupNow"), BackupNow, true);
            restoreBtn = Theme.Button(L.Get("restore"), RestoreClicked);
            var actions = Row(nowBtn, restoreBtn, Theme.Button(L.Get("openFolder"), OpenFolder));
            actions.Margin = new Padding(2, 0, 2, 2);

            var logCard = new Card(L.Get("log"), true);
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
            picker = LanguagePicker();
            header = new Header(picker);
            Controls.Add(header);

            var menu = new ContextMenuStrip { RightToLeft = RightToLeft };
            menu.Items.Add(L.Get("open"), null, (o, e) => ShowMain());
            menu.Items.Add(L.Get("backupNow"), null, (o, e) => RunBackup());
            menu.Items.Add(L.Get("exit"), null, (o, e) => ExitApp());
            tray = new NotifyIcon { Icon = Program.AppIcon(SystemInformation.SmallIconSize.Width), ContextMenuStrip = menu, Visible = true, Text = "PgKeeper" };
            tray.DoubleClick += (o, e) => ShowMain();

            ResumeLayout(false);
            PerformLayout();
        }

        ComboBox LanguagePicker()
        {
            var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110, Margin = new Padding(0, 8, 0, 0) };
            box.Items.AddRange(L.Names);
            box.SelectedIndex = Array.IndexOf(L.Codes, L.Code);
            box.SelectedIndexChanged += (o, e) => ChangeLanguage(L.Codes[box.SelectedIndex]);
            return box;
        }

        void ChangeLanguage(string code)
        {
            if (code == L.Code) return;
            var draft = Read();
            L.Code = code;
            s.Language = draft.Language = code;
            s.Save();
            BeginInvoke((Action)(() =>
            {
                var next = new MainForm(s, false, draft) { StartPosition = FormStartPosition.Manual, Location = Location };
                Program.Context.MainForm = next;
                next.Show();
                ExitApp();
            }));
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
            else if (!(control is TextBox)) t.SetColumnSpan(control, 2);
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

        void Fill(Settings src)
        {
            host.Text = src.Host;
            port.Value = Math.Max(1, Math.Min(65535, src.Port));
            user.Text = src.User;
            pass.Text = Settings.Unprotect(src.Password);
            db.Text = src.Database;
            pgdump.Text = src.PgDump;
            enabled.Checked = src.Enabled;
            hours.Value = Math.Min(999, Math.Max(0, src.Hours));
            minutes.Value = Math.Min(59, Math.Max(0, src.Minutes));
            folder.Text = src.Folder;
            keep.Value = Math.Min(9999, Math.Max(0, src.Keep));
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (key != null && key.GetValue("HighBackup") != null)
                {
                    key.DeleteValue("HighBackup");
                    key.SetValue("PgKeeper", "\"" + Application.ExecutablePath + "\" /tray");
                }
                autostart.Checked = key != null && key.GetValue("PgKeeper") != null;
            }
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
                Language = s.Language,
            };
        }

        static string Validate(Settings t, bool needFolder)
        {
            if (!File.Exists(t.PgDump)) return L.Get("noPgDump");
            if (t.Database == "") return L.Get("noDatabase");
            if (needFolder && t.Folder == "") return L.Get("noFolder");
            if (t.Enabled && t.Interval <= TimeSpan.Zero) return L.Get("badInterval");
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
                if (autostart.Checked) key.SetValue("PgKeeper", "\"" + Application.ExecutablePath + "\" /tray");
                else key.DeleteValue("PgKeeper", false);
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
            if (SaveFromForm(false)) Log(L.Get("saved"));
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
                Log(L.Get("backupFailed", error), 2);
                Notify(error, ToolTipIcon.Error);
                return;
            }
            SetBusy(true);
            Log(L.Get("backingUp", s.Database));
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
                    Log(L.Get("backupFailed", message), 2);
                    Notify(L.Get("backupFailed", message), ToolTipIcon.Error);
                }
                else
                {
                    s.LastRunSize = FormatSize(new FileInfo(t.Result).Length);
                    Log(L.Get("backupOk", Path.GetFileName(t.Result), s.LastRunSize), 1);
                }
                s.Save();
                UpdateStatus();
            })));
        }

        void SetBusy(bool busy)
        {
            running = busy;
            nowBtn.Enabled = restoreBtn.Enabled = picker.Enabled = !busy;
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
                if (r.IsFaulted) Alert(L.Get("connFailed", r.Exception.GetBaseException().Message), MessageBoxIcon.Error);
                else Alert(L.Get("connOk", r.Result), MessageBoxIcon.Information);
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
            if (MessageBox.Show(this, L.Get("restoreConfirm", target), "PgKeeper", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2, Program.BoxOptions()) != DialogResult.Yes) return;
            if (running)
            {
                Alert(L.Get("busy"), MessageBoxIcon.Information);
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
                    Log(L.Get("restoreFailed", message), 2);
                    Alert(L.Get("restoreFailedAlert", message), MessageBoxIcon.Error);
                }
                else
                {
                    Log(L.Get("restoreOk", Path.GetFileName(file), target), 1);
                    Alert(L.Get("restoreOkAlert"), MessageBoxIcon.Information);
                }
            })));
        }

        bool AskRestore(Settings t, out string file, out string target)
        {
            file = target = null;
            using (var dialog = new Form
            {
                Text = L.Get("restoreTitle"),
                Font = Font,
                ForeColor = Theme.Text,
                BackColor = Theme.Surface,
                RightToLeft = RightToLeft,
                RightToLeftLayout = L.Rtl,
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
                Field(grid, L.Get("backupFile"), fileBox, Theme.Button(L.Get("browse"), () =>
                {
                    using (var pick = new OpenFileDialog { Filter = L.Get("dumpFilter"), InitialDirectory = Directory.Exists(t.Folder) ? t.Folder : "" })
                        if (pick.ShowDialog(dialog) == DialogResult.OK) fileBox.Text = pick.FileName;
                }));
                var dbBox = Field(grid, L.Get("targetDb"), Input());
                dbBox.Text = t.Database;
                Field(grid, "", Caption(L.Get("createdHint"), Theme.Muted));

                var ok = Theme.Button(L.Get("restoreButton"), () => { }, true);
                ok.DialogResult = DialogResult.OK;
                var cancel = Theme.Button(L.Get("cancel"), () => { });
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
                Alert(L.Get("fileMissing"), MessageBoxIcon.Warning);
                return false;
            }
            if (target == "")
            {
                Alert(L.Get("targetMissing"), MessageBoxIcon.Warning);
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
            else Alert(L.Get("folderMissing"), MessageBoxIcon.Warning);
        }

        void UpdateStatus()
        {
            var on = s.Enabled && s.NextRunTicks > 0;
            dot.ForeColor = running ? Theme.Primary : on ? Theme.Green : Theme.Muted;
            if (running)
            {
                nextValue.Text = L.Get("running");
                nextValue.ForeColor = Theme.Primary;
            }
            else
            {
                nextValue.Text = on ? Countdown(new DateTime(s.NextRunTicks) - DateTime.Now) : L.Get("off");
                nextValue.ForeColor = on ? Theme.Text : Theme.Muted;
            }
            nextSub.Text = on ? FormatDate(new DateTime(s.NextRunTicks)) : L.Get("offSub");

            if (s.LastRunTicks == 0)
            {
                lastValue.Text = "—";
                lastValue.ForeColor = Theme.Muted;
                lastSub.Text = L.Get("noneYet");
            }
            else
            {
                lastValue.Text = L.Get(s.LastRunOk ? "ok" : "failed");
                lastValue.ForeColor = s.LastRunOk ? Theme.Green : Theme.Red;
                lastSub.Text = FormatDate(new DateTime(s.LastRunTicks)) + (s.LastRunSize != "" ? " · ‎" + s.LastRunSize : "");
            }
            var tip = "PgKeeper — " + (on ? L.Get("tipNext", nextSub.Text) : L.Get("tipOff"));
            if (tray.Text != tip) tray.Text = tip;
        }

        static string Countdown(TimeSpan left)
        {
            if (left.TotalMinutes < 1) return L.Get("lessMinute");
            var h = (int)left.TotalHours;
            var m = left.Minutes;
            if (h == 0) return L.Get("inMinutes", m);
            return m == 0 ? L.Get("inHours", h) : L.Get("inHoursMinutes", h, m);
        }

        void Log(string message, int kind = 0)
        {
            var line = "[" + FormatDate(DateTime.Now) + "] " + (kind == 1 ? "✓ " : kind == 2 ? "✗ " : "") + message;
            Append(line);
            try { File.AppendAllText(Settings.LogPath, line + Environment.NewLine); }
            catch (IOException) { }
        }

        void Append(string line)
        {
            log.SelectionStart = log.TextLength;
            log.SelectionLength = 0;
            log.SelectionColor = line.Contains("✗") ? Theme.Red : line.Contains("✓") ? Theme.Green : Color.FromArgb(51, 65, 85);
            log.SelectedText = (L.Rtl ? line.Replace("✓ ", "").Replace("✗ ", "") : line) + "\n";
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
            tray.ShowBalloonTip(5000, "PgKeeper", message, icon);
        }

        void Alert(string message, MessageBoxIcon icon)
        {
            MessageBox.Show(this, message, "PgKeeper", MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1, Program.BoxOptions());
        }

        static string FormatDate(DateTime d)
        {
            if (!L.Rtl) return d.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
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
            Close();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var need = settingsColumn.GetPreferredSize(new Size(settingsColumn.Width, 0)).Height + root.Padding.Vertical;
            ClientSize = new Size(ClientSize.Width, header.Height + need);
            MinimumSize = Size;
            if (StartPosition != FormStartPosition.Manual) CenterToScreen();
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
                    Notify(L.Get("trayHint"), ToolTipIcon.Info);
                }
                return;
            }
            timer.Dispose();
            tray.Dispose();
            base.OnFormClosing(e);
        }
    }
}
