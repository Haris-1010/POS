# Database Backup & Restore — Setup & Operations Guide

This covers the Automatic Database Backup & Restore feature: `Settings → Database Backup & Restore`
in the POS app (`BackupSettingsController`, `Service/BackupService.cs`,
`Service/BackupSchedulerService.cs`).

## 1. How the pieces fit together

```
Settings UI (interval, folder, retention)
        │  saved to
        ▼
Settings.BackupSettings (SQL table)
        │
        ├─ In-process timer (BackupSchedulerService) — ticks every 1 minute
        │  while the IIS app pool is alive, runs a backup when due.
        │
        └─ Optional Windows Scheduled Task (Database/Setup-BackupScheduledTask.ps1)
           — pings /BackupSettings/ScheduledBackup on its own clock, so
             backups keep happening even if the app pool is recycled/idle.

Both paths call the same engine (Service/BackupService.cs), which runs
SQL Server's own `BACKUP DATABASE ... WITH COMPRESSION` (falls back to no
compression on editions that don't support it), verifies the file with
`RESTORE VERIFYONLY`, records the result in Settings.BackupHistory, and
applies the retention policy.
```

## 2. One-time setup

1. **Create the tables and permissions.** Run `Database/Backup_Tables.sql` in SQL Server
   Management Studio against the POS database (same server/database as the
   `NewConnectionString` connection string in `Web.config`). It's idempotent — safe to re-run.
   It creates:
   - `Settings.BackupSettings` (one row — the current configuration/status)
   - `Settings.BackupHistory` (one row per backup/restore attempt)
   - `Settings.BackupAuditLog` (audit trail of every settings change, test, backup, restore,
     delete, download, and cleanup)
   - Permission keys `BackupSettings_View`, `BackupSettings_Manage`, `BackupSettings_Restore`
     in `Auth.Permissions` (Admin bypasses permission checks automatically; use these to grant
     non-admin roles access if needed).

2. **Open the settings page.** Log in as Admin → `Settings` → **Database Backup & Restore**
   card (also reachable directly at `/BackupSettings`).

3. **Configure:**
   - **Enable Automatic Backup** — toggle on.
   - **Backup Frequency** — 15 min / 30 min / 1 hour / 6 hours / 12 hours / 24 hours, or a
     custom interval in minutes (5–10,080).
   - **Backup Folder Path** — an absolute Windows path, e.g. `D:\POS_Backups`. Click
     **Test Folder** first — this creates the folder if missing and writes/deletes a small test
     file to prove the app can write there.
   - **Backup Retention (days)** — backups older than this are deleted automatically after each
     successful run. `0` = keep forever. The most recent successful backup is never deleted by
     cleanup, even if it's older than the retention window.
   - Click **Save Settings**.

## 3. Windows / file-system permissions

Two different Windows identities need write access to the backup folder, because SQL Server
itself writes the `.bak` file — the web app only tells it where to write:

- **SQL Server service account** (the account the `SQL Server (MSSQLSERVER)` or named-instance
  service runs as — check `services.msc`) needs **read/write** on the backup folder. This is the
  account that actually performs `BACKUP DATABASE ... TO DISK = '...'`.
- **IIS application pool identity** (the pool running the POS site — check IIS Manager →
  Application Pools → Identity) needs **read/write/delete** on the same folder. The web app uses
  this to validate the folder (Test Folder), delete backups manually, apply retention cleanup,
  and serve downloads.

If either identity lacks access, you'll see a clear error in the UI (folder test failure, or a
"Failed" row in Backup History with the underlying error message) rather than a silent failure.

Grant access via folder Properties → Security → Edit → add the service account / app pool
identity (app pool identities use the form `IIS AppPool\<PoolName>`) with Modify permission.

## 4. How automatic scheduling works

- **In-process timer** (`Service/BackupSchedulerService.cs`): starts automatically when the app
  starts (`Global.asax.cs` → `Application_Start`), ticks every minute, and runs a backup
  whenever `Settings.BackupSettings.NextBackupTime` has passed. This alone is enough for most
  deployments — it keeps working as long as the site is reachable and the app pool hasn't been
  recycled.

- **Optional Windows Scheduled Task fallback** (recommended for production): registers a task
  that calls the same due-check independently of IIS traffic, so scheduling survives an app pool
  recycle, an IIS restart, or a period where nobody is using the site. Install it once, from an
  elevated PowerShell prompt:

  ```powershell
  cd "D:\Haris Dev\POS\CoreMedixBeta\Database"
  .\Setup-BackupScheduledTask.ps1 -SiteUrl "http://localhost/BackupSettings/ScheduledBackup"
  ```

  The script reads the shared secret automatically from `Web.config`'s `BackupSchedulerKey`
  appSetting (or pass `-Key` explicitly). Use your site's real host header instead of
  `localhost` if it's bound to one. The endpoint is already allow-listed for anonymous access in
  `Web.config` (`<location path="BackupSettings/ScheduledBackup">`) — it's still protected,
  because the request is rejected unless the `key` query parameter matches `BackupSchedulerKey`
  exactly.

  Both mechanisms coordinate through a SQL Server application lock
  (`sp_getapplock` on resource `CoreMedixBeta.DatabaseBackup`), so they can never run two backups
  (or a backup and a restore) at the same time, even across processes.

## 5. Verifying a backup

- Click **Backup Now** to run one immediately, or wait for the scheduled one.
- Check **Backup History** — a new row appears with status `Running` → `Success` (or `Failed`
  with the reason shown in the row and in the **Details** modal).
- Every backup is automatically verified by SQL Server (`RESTORE VERIFYONLY FROM DISK = ...`)
  immediately after it's written — a `Success` status means SQL Server has already confirmed the
  file is readable and intact, not just that a file exists on disk.
- The **Details** button on any history row shows the exact backup location, file size, start/end
  time, who/what triggered it (Manual / Scheduled / ScheduledTask / PreRestore), and any error.
- **Download** lets you copy the `.bak` file off the server (e.g. to keep an off-site copy).

## 6. Restoring the database

1. In **Backup History**, click **Restore** on a `Success` row.
2. Read the warning — restoring **overwrites all current data** and disconnects all active POS
   users.
3. Type the exact database name to confirm (shown in the dialog).
4. The app automatically:
   - Validates the backup file again (`RESTORE HEADERONLY` + `RESTORE VERIFYONLY`) and confirms
     it belongs to the current database.
   - Takes a **safety backup of the current database first** (type `PreRestore` in history), so
     the pre-restore state is never lost.
   - Puts the database in `SINGLE_USER` mode, runs `RESTORE DATABASE ... WITH REPLACE, RECOVERY`,
     then returns it to `MULTI_USER`.
5. If the safety backup itself fails (e.g. the current database is too corrupt to back up), the
   UI offers a second, explicit `FORCE RESTORE` confirmation that skips the safety backup —
   only use this as a last resort.

Only users with the `BackupSettings_Restore` permission (or the Admin role) can restore.

## 7. SQL Server requirements

- The SQL login used by the app's connection string (`NewConnectionString` in `Web.config`) needs:
  - `BACKUP DATABASE` permission on the POS database.
  - `ALTER DATABASE` permission (needed to switch the database to/from `SINGLE_USER` during a
    restore) and the ability to connect to `master` (restore runs against `master`, since you
    can't restore a database you're currently connected to).
  - A login that's already a `sysadmin` or `db_owner` satisfies all of the above; for a more
    restricted login, grant `ALTER ANY DATABASE` or the specific database-level roles.
- **Backup compression** (`WITH COMPRESSION`) requires SQL Server Standard/Enterprise/Developer.
  On editions that don't support it (e.g. some Express configurations), the engine automatically
  retries without compression — no configuration needed.
- Make sure the drive hosting the backup folder has enough free space; the Backup Folder card on
  the settings page shows live free space.

## 8. IIS requirements

- No special IIS configuration is needed beyond what's already in `Web.config`:
  `BackupSettings/ScheduledBackup` is allow-listed for anonymous access (so the Scheduled Task
  can call it without a logged-in session), but every request still requires the correct
  `BackupSchedulerKey`.
- If the site runs behind HTTPS only, use the HTTPS URL in
  `Setup-BackupScheduledTask.ps1 -SiteUrl`.
- Confirm the application pool's **Idle Time-out** and **recycling** settings if you're relying
  solely on the in-process timer — a recycled/idle pool pauses that timer until the next request.
  This is exactly what the Scheduled Task fallback in section 4 is for.
