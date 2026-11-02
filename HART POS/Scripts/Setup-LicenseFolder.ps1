param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AppPoolName,

    [string]$Identity,

    [string]$LicenseDirectory = "$env:ProgramData\HARTPOS"
)

if ([string]::IsNullOrWhiteSpace($Identity)) {
    $Identity = "IIS AppPool\$($AppPoolName.Trim())"
}

$Identity = $Identity.Trim()
$resolvedDirectory = [System.IO.Path]::GetFullPath($LicenseDirectory)
$programData = [System.IO.Path]::GetFullPath($env:ProgramData).TrimEnd('\') + '\'
if (-not $resolvedDirectory.StartsWith($programData, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The license directory must be under $env:ProgramData."
}

$currentIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object System.Security.Principal.WindowsPrincipal($currentIdentity)
if (-not $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an elevated PowerShell window.'
}

New-Item -ItemType Directory -Path $resolvedDirectory -Force -ErrorAction Stop | Out-Null
& "$env:SystemRoot\System32\icacls.exe" $resolvedDirectory /grant "${Identity}:(OI)(CI)M"
if ($LASTEXITCODE -ne 0) {
    throw "Could not grant Modify permission to $Identity on $resolvedDirectory."
}

Write-Output "Granted Modify permission to $Identity on $resolvedDirectory."
Write-Output 'Set Web.config LicenseDirectory to this same folder if it differs from the default.'
