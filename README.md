# HighBackup

A tiny Windows app for **scheduled PostgreSQL backups** and **one-click restore**.

برنامه‌ای کوچک برای ویندوز که از دیتابیس **PostgreSQL** طبق زمان‌بندی **بکاپ خودکار** می‌گیرد و هر وقت لازم شد آن را **بازگردانی** می‌کند.

![HighBackup](docs/screenshot.png)

[⬇️ Download / دانلود](../../releases/latest) · [فارسی](#فارسی) · [English](#english)

---

<div dir="rtl">

## فارسی

### این برنامه چه کار می‌کند؟

اگر روی ویندوز (یا ویندوز سرور) دیتابیس PostgreSQL دارید و می‌خواهید هر چند ساعت یک‌بار خودکار از آن بکاپ گرفته شود، بدون اسکریپت‌نویسی و بدون نرم‌افزار سنگین و پولی، HighBackup برای همین ساخته شده است.

- یک فایل exe حدود ۱۴۰ کیلوبایتی است و نصب لازم ندارد.
- کار بکاپ را همان ابزار رسمی `pg_dump` انجام می‌دهد. برنامه فقط سر وقت اجرایش می‌کند و فایل‌ها را مرتب نگه می‌دارد.
- رابط کاربری فارسی و راست‌به‌چپ است.

### امکانات

- **زمان‌بندی دلخواه:** هر چند ساعت و چند دقیقه یک‌بار، مثلاً هر ۵ ساعت یا هر ۳۰ دقیقه.
- **پوشه‌ی دلخواه:** فایل‌ها با اسم تاریخ‌دار ذخیره می‌شوند، مثل `mydb_2026-11-17_14-00-01.dump`.
- **پاک‌کردن خودکار:** فقط N بکاپ آخر نگه داشته می‌شود تا دیسک پر نشود.
- **دکمه‌ی «الان بکاپ بگیر»** برای بکاپ فوری.
- **بازگردانی (ریستور):** فایل بکاپ و دیتابیس مقصد را انتخاب می‌کنید و بقیه‌اش با برنامه است:
  - اگر دیتابیس مقصد وجود نداشته باشد، ساخته می‌شود.
  - اگر وجود داشته باشد، **اول یک بکاپ ایمنی** از وضعیت فعلی‌اش گرفته می‌شود.
  - بازگردانی به‌صورت **همه یا هیچ** انجام می‌شود؛ اگر وسط کار خطایی پیش بیاید، هیچ جدولی تغییر نمی‌کند.

  ![بازگردانی](docs/restore.png)

- **تست اتصال:** نسخه‌ی دیتابیس و `pg_dump` را نشان می‌دهد.
- **کنار ساعت ویندوز:** با بستن پنجره، برنامه کنار ساعت ویندوز می‌ماند و بکاپ‌ها ادامه پیدا می‌کنند. اگر بکاپی ناموفق باشد، نوتیفیکیشن می‌دهد.
- **اجرای خودکار با روشن شدن ویندوز** (اختیاری).
- **گزارش رنگی** از همه‌ی بکاپ‌ها و بازگردانی‌ها، با تاریخ شمسی.

### پیش‌نیازها

- **ویندوز ۱۰ یا ۱۱، یا ویندوز سرور ۲۰۱۶ به بعد.** ‏.NET Framework 4.5 یا بالاتر لازم است که روی این نسخه‌ها از قبل نصب است.
- **ابزارهای PostgreSQL** (`pg_dump`، `pg_restore` و `psql`) روی همان سیستمی که برنامه اجرا می‌شود:
  - اگر PostgreSQL روی همان سیستم نصب است، این ابزارها هم نصب شده‌اند و برنامه خودش از مسیر `C:\Program Files\PostgreSQL\<نسخه>\bin` پیدایشان می‌کند.
  - اگر دیتابیس روی سرور دیگری است، نصب «Command Line Tools» از نصب‌کننده‌ی PostgreSQL کافی است.
  - ⚠️ نسخه‌ی این ابزارها باید **هم‌نسخه یا جدیدتر** از نسخه‌ی دیتابیس باشد.

### شروع کار

1. فایل `HighBackup.zip` را از بخش [Releases](../../releases/latest) دانلود و از حالت فشرده خارج کنید.
2. `HighBackup.exe` را اجرا کنید. اگر ویندوز پیغام SmartScreen نشان داد، روی **More info** و بعد **Run anyway** بزنید. این پیغام فقط به این خاطر است که فایل امضای دیجیتال ندارد.
3. اطلاعات اتصال را وارد کنید (سرور، پورت، نام کاربری، رمز و اسم دیتابیس) و **تست اتصال** را بزنید.
4. فاصله‌ی بکاپ‌ها و پوشه‌ی بکاپ را تعیین کنید و تیک **بکاپ خودکار فعال باشد** را بزنید.
5. **ذخیره‌ی تنظیمات** را بزنید. تمام!

### نکته‌های مهم

- **ویندوز سرور و ریموت دسکتاپ:** برنامه داخل نشست کاربر اجرا می‌شود. وقتی کارتان با سرور تمام شد، پنجره‌ی ریموت دسکتاپ را با ضربدر ببندید و **Sign out نکنید**؛ با Sign out برنامه هم بسته می‌شود.
- **جای تنظیمات:** تنظیمات در `%APPDATA%\HighBackup\settings.xml` ذخیره می‌شوند. رمز عبور با DPAPI ویندوز رمزنگاری می‌شود و فقط همان کاربر ویندوز می‌تواند آن را باز کند.
- **فرمت بکاپ‌ها:** فایل‌ها با فرمت custom ابزار `pg_dump` (`-F c`) و به‌صورت فشرده ذخیره می‌شوند. اگر بخواهید، دستی هم با `pg_restore` قابل بازگردانی‌اند.
- **قبل از بازگردانی** بهتر است برنامه‌ای که از دیتابیس استفاده می‌کند خاموش باشد. اگر جدول‌ها قفل باشند، بازگردانی بعد از ۳۰ ثانیه با خطا متوقف می‌شود و برنامه هنگ نمی‌کند.

### ساخت از روی سورس

به Visual Studio نیازی نیست. کامپایلر C# همراه خود ویندوز کافی است:

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

خروجی، فایل `HighBackup.exe` کنار `build.ps1` است.

</div>

---

## English

### What is it?

HighBackup is a small, free Windows app for anyone running **PostgreSQL on Windows** (desktop or Windows Server) who wants automatic backups every few hours, without writing scripts or paying for heavy backup software.

- A single ~140 KB exe. No installer.
- The actual backup is done by PostgreSQL's own `pg_dump`. HighBackup runs it on schedule and keeps the files tidy.
- The interface is in Persian (right-to-left).

### Features

- **Flexible schedule:** every *N hours and M minutes* (e.g. every 5 hours, or every 30 minutes).
- **Any backup folder:** files get timestamped names, e.g. `mydb_2026-11-17_14-00-01.dump`.
- **Retention:** keeps only the last *N* backups so the disk never fills up.
- **Backup now** button.
- **Restore:** pick a `.dump` file and a target database:
  - If the target database doesn't exist, it is created.
  - If it does exist, a **safety backup** of its current state is taken first.
  - The restore is **all-or-nothing** (single transaction): if anything fails, no tables are changed.

  ![Restore](docs/restore.png)

- **Connection test:** shows both the server version and the `pg_dump` version.
- **Tray icon:** closing the window keeps HighBackup running next to the clock, and you get a notification if a backup fails.
- **Start with Windows** (optional).
- **Colour-coded log** of every backup and restore.

### Requirements

- **Windows 10/11 or Windows Server 2016+.** It needs .NET Framework 4.5+, which is already installed on these versions.
- **PostgreSQL client tools** (`pg_dump`, `pg_restore`, `psql`) on the machine running HighBackup:
  - If PostgreSQL is installed on that machine, the tools are already there. HighBackup finds them automatically in `C:\Program Files\PostgreSQL\<version>\bin`.
  - For a remote server, installing just the "Command Line Tools" component of the PostgreSQL installer is enough.
  - ⚠️ The tools must be the **same version or newer** than the database server.

### Getting started

1. Download `HighBackup.zip` from [Releases](../../releases/latest) and extract it.
2. Run `HighBackup.exe`. If SmartScreen warns you, click **More info**, then **Run anyway**. This only appears because the exe is not code-signed.
3. Enter the connection details (host, port, user, password, database) and click **Test connection** (تست اتصال).
4. Set the interval and the backup folder, then tick **Enable automatic backup** (بکاپ خودکار فعال باشد).
5. Click **Save settings** (ذخیره‌ی تنظیمات). Done.

### Notes

- **Windows Server and RDP:** HighBackup runs inside your user session. When you're done, close the Remote Desktop window with the X instead of signing out; signing out closes the app too.
- **Settings location:** settings live in `%APPDATA%\HighBackup\settings.xml`. The password is encrypted with Windows DPAPI and can only be read by the same Windows user.
- **Backup format:** backups use `pg_dump`'s compressed custom format (`-F c`), so you can also restore them manually with `pg_restore`.
- **Before restoring,** it's best to stop whatever is using the database. If tables are locked, the restore stops with an error after 30 seconds instead of hanging.

### Building from source

You don't need Visual Studio. The C# compiler that ships with Windows is enough:

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

This produces `HighBackup.exe` next to `build.ps1`.

## License

[MIT](LICENSE)
