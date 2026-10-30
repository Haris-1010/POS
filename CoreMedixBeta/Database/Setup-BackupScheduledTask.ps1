<#
.SYNOPSIS
    Registers a Windows Scheduled Task that pings the POS backup scheduler
    endpoint on a fixed cadence, as a resilience fallback to the in-process
    scheduler (Service/BackupSchedulerService.cs).

.DESCRIPTION
    CoreMedixBeta already runs an in-process timer that checks every minute
    whether a backup is due and runs it in the background. That timer only
    runs while the IIS application pool is alive. If the app pool is
    recycled, idle-timed-out, or the server is rebooted, scheduled backups
    pause until the next request wakes the app pool back up.

    This script closes that gap: it registers a Windows Scheduled Task that
    calls the same due-check on its own clock (default every 5 minutes),
    independent of IIS traffic. The endpoint
    (BackupSettingsController.ScheduledBackup) only runs a backup when the
    schedule saved in the database says one is actually due, and both the
    in-process timer and this task coordinate through a SQL Server
    application lock, so they can never create duplicate or overlapping
    backups.

.PARAMETER SiteUrl
    Full URL to the ScheduledBackup endpoint, e.g.
    "http://localhost/BackupSettings/ScheduledBackup". Use the site's real
    host header if it's bound to one (not just "localhost").

.PARAMETER Key
    The shared secret. Must match the "BackupSchedulerKey" appSetting in
    Web.config exactly. If omitted, the script tries to read it from the
    Web.config next to this script's parent folder's CoreMedixBeta project.

.PARAMETER IntervalMinutes
    How often the task fires (default 5). This does NOT control how often
    backups run - that's the "Backup Frequency" set in Settings -> Database
    Backup & Restore. Keep this well below the shortest interval you plan to
    offer (5 minutes) so a due backup is never missed for long.

.PARAMETER TaskName
    Scheduled task name (default "CoreMedixBeta-BackupScheduler").

.EXAMPLE
    .\Setup-BackupScheduledTask.ps1 -SiteUrl "http://localhost/BackupSettings/ScheduledBackup"

.EXAMPLE
    .\Setup-BackupScheduledTask.ps1 -SiteUrl "http://pos.local/BackupSettings/ScheduledBackup" -Key "fe12fb74...3faa3f36510" -IntervalMinutes 5

.NOTES
    Run this script from an elevated (Administrator) PowerShell prompt.
    Safe to re-run: it unregisters any existing task with the same name first.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SiteUrl,

    [Parameter(Mandatory = $false)]
    [string]$Key,

    [Parameter(Mandatory = $false)]
    [int]$IntervalMinutes = 5,

    [Parameter(Mandatory = $false)]
    [string]$TaskName = "CoreMedixBeta-BackupScheduler"
)

$ErrorActionPreference = "Stop"

if (-not ([Security.Principal.WindowsIdentity]::GetCurrent().Groups -contains "S-1-5-32-544")) {
    Write-Warning "This script usually needs to run as Administrator to register a Scheduled Task. If registration fails below, re-open PowerShell as Administrator and try again."
}

if (-not $Key) {
    $webConfigPath = Join-Path $PSScriptRoot "..\Web.config"
    if (Test-Path $webConfigPath) {
        try {
            [xml]$webConfig = Get-Content $webConfigPath
            $Key = ($webConfig.configuration.appSettings.add | Where-Object { $_.key -eq "BackupSchedulerKey" }).value
        } catch {
            Write-Warning "Could not parse $webConfigPath automatically: $($_.Exception.Message)"
        }
    }
}

if (-not $Key) {
    throw "Could not determine the BackupSchedulerKey. Pass it explicitly with -Key '<value from Web.config appSettings>'."
}

$fullUrl = "$SiteUrl`?key=$Key"

Write-Host "Task name:        $TaskName"
Write-Host "Target URL:       $SiteUrl"
Write-Host "Interval:         every $IntervalMinutes minute(s)"
Write-Host ""

# Remove any existing task with the same name so this script is safe to re-run.
$existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Existing task '$TaskName' found - removing it before re-registering."
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}

# The task runs PowerShell, which calls the endpoint with Invoke-WebRequest.
# -UseBasicParsing avoids a dependency on Internet Explorer's engine.
$command = "Invoke-WebRequest -Uri '$fullUrl' -UseBasicParsing -TimeoutSec 120 | Out-Null"
$action = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument "-NoProfile -NonInteractive -WindowStyle Hidden -Command `"$command`""

# Starts shortly after boot/registration, then repeats indefinitely.
$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) `
    -RepetitionInterval (New-TimeSpan -Minutes $IntervalMinutes) `
    -RepetitionDuration ([TimeSpan]::MaxValue)

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
    -StartWhenAvailable -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2)

# Runs as SYSTEM so it works even when no user is logged in.
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger `
    -Settings $settings -Principal $principal `
    -Description "Pings the CoreMedixBeta POS backup scheduler endpoint so automatic database backups keep running even if the IIS application pool is recycled or idle." | Out-Null

Write-Host ""
Write-Host "Scheduled Task '$TaskName' registered successfully." -ForegroundColor Green
Write-Host "It will call: $SiteUrl every $IntervalMinutes minute(s)."
Write-Host ""
Write-Host "To test it immediately, run:"
Write-Host "    Start-ScheduledTask -TaskName '$TaskName'"
Write-Host "To remove it later, run:"
Write-Host "    Unregister-ScheduledTask -TaskName '$TaskName' -Confirm:`$false"
