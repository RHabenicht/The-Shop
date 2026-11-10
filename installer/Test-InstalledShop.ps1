param(
    [Parameter(Mandatory)][string]$Installer
)
$ErrorActionPreference = 'Stop'

function Invoke-CheckedProcess([string]$Path, [string[]]$Arguments) {
    $process = Start-Process -FilePath $Path -ArgumentList $Arguments -PassThru
    try {
        if (-not $process.WaitForExit(60000)) { throw "$Path timed out." }
        if ($process.ExitCode -ne 0) { throw "$Path exited with code $($process.ExitCode)." }
    } finally {
        if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
        $process.Dispose()
    }
}

$target = Join-Path $env:RUNNER_TEMP ("the-shop-install-" + [Guid]::NewGuid().ToString('N'))
$uninstaller = Join-Path $target 'unins000.exe'
$app = Join-Path $target 'TheShop.Desktop.exe'

try {
    Invoke-CheckedProcess (Resolve-Path $Installer).Path @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', "/DIR=`"$target`"")
    if (-not (Test-Path $app)) { throw 'The installed executable was missing.' }
    $process = Start-Process $app -PassThru
    try {
        $deadline = (Get-Date).AddSeconds(30)
        do {
            $process.Refresh()
            if ($process.HasExited) { throw "The installed app exited with code $($process.ExitCode)." }
            if ($process.MainWindowTitle -eq 'The Shop | Rigger 3 Revised') { break }
            Start-Sleep -Milliseconds 250
        } while ((Get-Date) -lt $deadline)
        if ($process.MainWindowTitle -ne 'The Shop | Rigger 3 Revised') { throw 'The installed app did not open its main window.' }
    } finally {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            if (-not $process.WaitForExit(10000)) { throw 'The installed app did not stop.' }
        }
        $process.Dispose()
    }
} finally {
    if (Test-Path $uninstaller) {
        Invoke-CheckedProcess $uninstaller @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART')
    }
}
if (Test-Path $app) { throw 'The installed executable remained after uninstall.' }
Write-Host 'Windows installer passed install, launch, and uninstall.'
