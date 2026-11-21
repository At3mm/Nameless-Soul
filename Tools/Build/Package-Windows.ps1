param(
    [string]$Version="0.1.0",
    [string]$PlayerDirectory="Builds/Windows",
    [string]$OutputDirectory="Builds/Installer",
    [string]$Compiler=""
)
$ErrorActionPreference="Stop"
$projectRoot=[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$player=[System.IO.Path]::GetFullPath((Join-Path $projectRoot $PlayerDirectory))
$output=[System.IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))
if(!(Test-Path -LiteralPath (Join-Path $player "NamelessSoul.exe"))){throw "Build the Windows player before packaging."}
if(!$Compiler){
 $candidates=@(
  (Join-Path $env:LOCALAPPDATA "Programs/Inno Setup 6/ISCC.exe"),
  "C:/Program Files (x86)/Inno Setup 6/ISCC.exe",
  "C:/Program Files/Inno Setup 6/ISCC.exe"
 )
 $Compiler=$candidates|Where-Object {Test-Path -LiteralPath $_}|Select-Object -First 1
}
if(!$Compiler){throw "Install Inno Setup 6 or provide -Compiler."}
New-Item -ItemType Directory -Path $output -Force|Out-Null
# Ship source/license notices without shipping the original asset packs.
$notices=Join-Path $player "ThirdPartyNotices"
foreach($sourceName in @("Assets","References")){
 $sourceRoot=Join-Path $projectRoot $sourceName
 foreach($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File){
  if($file.Extension -eq ".meta" -or $file.Name -notmatch '(?i)(licen[cs]e|credits|attribution|^SOURCE\.md$)'){continue}
  $relative=[System.IO.Path]::GetRelativePath($projectRoot,$file.FullName)
  $destination=Join-Path $notices $relative
  New-Item -ItemType Directory -Path (Split-Path $destination) -Force|Out-Null
  Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
 }
}
Set-Content -LiteralPath (Join-Path $player "README.txt") -Value "Nameless Soul - Windows x64. Run NamelessSoul.exe. Keep the Data folder, MonoBleedingEdge and DLL files beside the executable. Controls can be changed in Settings. Third-party source and license notices are included in ThirdPartyNotices." -Encoding utf8
& $Compiler "/DGameSource=$player" "/DInstallerOutput=$output" "/DAppVersion=$Version" (Join-Path $PSScriptRoot "WindowsInstaller.iss")
if($LASTEXITCODE -ne 0){throw "Installer compilation failed."}
$installer=Join-Path $output "Nameless-Soul-$Version-Windows-Setup.exe"
if(!(Test-Path -LiteralPath $installer)){throw "Installer file missing."}
$hash=Get-FileHash -LiteralPath $installer -Algorithm SHA256
Set-Content -LiteralPath "$installer.sha256" -Value ($hash.Hash.ToLowerInvariant()+"  "+[System.IO.Path]::GetFileName($installer)) -Encoding ascii
Write-Output $installer
