param(
    [Parameter(Mandatory)][string]$Installer,
    [Parameter(Mandatory)][string]$ExpectedVersion,
    [Parameter(Mandatory)][string]$Report
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

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

function Assert-ApplicationStarts([string]$Path, [string]$WorkingDirectory) {
    $process = Start-Process -FilePath $Path -WorkingDirectory $WorkingDirectory -PassThru
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
            [void]$process.CloseMainWindow()
            if (-not $process.WaitForExit(10000)) {
                Stop-Process -Id $process.Id -Force
                [void]$process.WaitForExit(10000)
                throw 'The installed app did not close cleanly.'
            }
            if ($process.ExitCode -ne 0) { throw "The installed app closed with code $($process.ExitCode)." }
        }
        $process.Dispose()
    }
}

$installerPath = (Resolve-Path $Installer).Path
$reportPath = [IO.Path]::GetFullPath($Report)
$outputDirectory = Split-Path $reportPath
[void][IO.Directory]::CreateDirectory($outputDirectory)
$temporaryRoot = [IO.Path]::GetTempPath()
$testRoot = Join-Path $temporaryRoot ('The Shop package test ' + [Guid]::NewGuid().ToString('N'))
$target = Join-Path $testRoot 'Installed application'
$uninstaller = Join-Path $target 'unins000.exe'
$app = Join-Path $target 'TheShop.Desktop.exe'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\TheShop.RHabenicht_is1'
$shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'The Shop\The Shop.lnk'
if (Test-Path $uninstallKey) { throw 'An existing The Shop installation must not be changed by the package test.' }
if (Test-Path $shortcut) { throw 'An existing The Shop shortcut must not be changed by the package test.' }
[void][IO.Directory]::CreateDirectory($testRoot)
$externalDesign = Join-Path $testRoot 'Saved vehicle.json'
$internalDesign = Join-Path $target 'My designs\Saved vehicle.json'
$designText = '{"Name":"Installer preservation test","ChassisId":"sand-buggy","PowerPlantId":"gasoline","StreetIndex":1}'
Set-Content $externalDesign $designText -Encoding utf8NoBOM
$userFileHash = (Get-FileHash $externalDesign -Algorithm SHA256).Hash
$checks = [Collections.Generic.List[string]]::new()
$installedFiles = @()

try {
    $installArguments = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', "/DIR=`"$target`"")
    Invoke-CheckedProcess $installerPath ($installArguments + "/LOG=`"$(Join-Path $outputDirectory 'install.log')`"")
    foreach ($file in @('TheShop.Desktop.exe', 'TheShop.Desktop.dll', 'TheShop.Design.dll',
                        'hostfxr.dll', 'hostpolicy.dll', 'coreclr.dll', 'System.Windows.Forms.dll', 'INSTALL.txt', 'unins000.exe')) {
        if (-not (Test-Path (Join-Path $target $file))) { throw "Installed file missing: $file" }
    }
    if (Get-ChildItem $target -Filter '*.pdb' -Recurse) { throw 'Debug symbols were shipped in the installer.' }
    if (Test-Path (Join-Path $target 'TheShop.Design.exe')) { throw 'The command-line executable was shipped in the desktop installer.' }
    $installedVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $target 'TheShop.Desktop.dll')).ProductVersion.Split('+')[0]
    if ($installedVersion -ne $ExpectedVersion) { throw "Installed version was $installedVersion; expected $ExpectedVersion." }
    $registration = Get-ItemProperty $uninstallKey
    if ($registration.DisplayVersion -ne $ExpectedVersion) { throw 'Windows uninstall registration has the wrong version.' }
    if ($registration.InstallLocation.TrimEnd('\') -ne $target.TrimEnd('\')) { throw 'Windows uninstall registration has the wrong installation path.' }
    $configuration = Get-Content (Join-Path $target 'TheShop.Desktop.runtimeconfig.json') -Raw | ConvertFrom-Json
    $frameworkNames = @($configuration.runtimeOptions.includedFrameworks | ForEach-Object { $_.name })
    foreach ($framework in @('Microsoft.NETCore.App', 'Microsoft.WindowsDesktop.App')) {
        if ($framework -notin $frameworkNames) { throw "The installer is not self-contained: $framework is missing." }
    }
    $checks.Add('PASS: per-user installation to a path containing spaces; correct application and uninstall versions; bundled .NET and Windows Forms runtimes.')
    if (-not (Test-Path $shortcut)) { throw 'The Start menu shortcut was missing.' }
    $shell = New-Object -ComObject WScript.Shell
    if ($shell.CreateShortcut($shortcut).TargetPath -ne $app) { throw 'The Start menu shortcut points to the wrong executable.' }
    $checks.Add('PASS: Start menu shortcut points to the installed application.')
    Assert-ApplicationStarts $app $testRoot
    $checks.Add('PASS: installed application opened its main window from a separate working directory and closed cleanly.')

    $installedFiles = @(Get-ChildItem $target -File -Recurse | Select-Object -ExpandProperty FullName)
    [void][IO.Directory]::CreateDirectory((Split-Path $internalDesign))
    Copy-Item $externalDesign $internalDesign
    $designLibrary = Join-Path $target 'TheShop.Design.dll'
    $designLibraryHash = (Get-FileHash $designLibrary -Algorithm SHA256).Hash
    Remove-Item $designLibrary
    Invoke-CheckedProcess $installerPath ($installArguments + "/LOG=`"$(Join-Path $outputDirectory 'repair.log')`"")
    if ((Get-FileHash $designLibrary -Algorithm SHA256).Hash -ne $designLibraryHash) { throw 'Reinstall did not restore the missing application library.' }
    foreach ($file in @($externalDesign, $internalDesign)) {
        if ((Get-FileHash $file -Algorithm SHA256).Hash -ne $userFileHash) { throw "Reinstall changed a saved vehicle: $file" }
    }
    $registrations = @(Get-ChildItem (Split-Path $uninstallKey) | Where-Object { $_.PSChildName -eq 'TheShop.RHabenicht_is1' })
    if ($registrations.Count -ne 1) { throw 'Reinstall created duplicate uninstall registration.' }
    Assert-ApplicationStarts $app $testRoot
    $checks.Add('PASS: reinstall restored a deleted application library, retained saved vehicles, kept one uninstall entry, and reopened the application.')
} finally {
    if (Test-Path $uninstaller) {
        Invoke-CheckedProcess $uninstaller @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=`"$(Join-Path $outputDirectory 'uninstall.log')`"")
    }
}
$deadline = (Get-Date).AddSeconds(15)
do {
    $remainingFiles = @($installedFiles | Where-Object { Test-Path $_ })
    if ($remainingFiles.Count -eq 0) { break }
    Start-Sleep -Milliseconds 250
} while ((Get-Date) -lt $deadline)
if ($remainingFiles.Count -ne 0) { throw "Installer-owned files remained after uninstall: $($remainingFiles -join ', ')" }
if (Test-Path $uninstallKey) { throw 'Windows uninstall registration remained after uninstall.' }
if (Test-Path $shortcut) { throw 'The Start menu shortcut remained after uninstall.' }
foreach ($file in @($externalDesign, $internalDesign)) {
    if ((Get-FileHash $file -Algorithm SHA256).Hash -ne $userFileHash) { throw "Uninstall changed a saved vehicle: $file" }
}
$checks.Add('PASS: uninstall removed all installed program files, the Start menu shortcut and Windows registration, and preserved saved vehicle files.')
$reportLines = @(
    "The Shop $ExpectedVersion - Windows x64 installer test",
    "Completed (UTC): $([DateTime]::UtcNow.ToString('yyyy-MM-dd HH:mm:ss'))",
    "Test system: $([Runtime.InteropServices.RuntimeInformation]::OSDescription)",
    'Install lifecycle tests ran on a native GitHub-hosted Windows runner.',
    'The Windows 10/11 x64 target is specified by the installer; this run does not test every Windows release.',
    'Reinstallation repairs the current package. Upgrading from each historical package was not tested.',
    "Installer SHA-256: $((Get-FileHash $installerPath -Algorithm SHA256).Hash)",
    ''
) + $checks.ToArray()
Set-Content $reportPath $reportLines -Encoding utf8NoBOM
$checks | ForEach-Object { Write-Host $_ }
Remove-Item $testRoot -Recurse -Force
Write-Host 'Windows installer passed install, launch, repair, saved-file preservation, and uninstall.'
