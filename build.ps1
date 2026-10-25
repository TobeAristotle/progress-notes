[CmdletBinding()]
param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Requires the Windows x64 .NET Framework C# compiler.' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$outputPath = Join-Path (Resolve-Path -LiteralPath $OutputDirectory).Path '进步签.exe'
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName }
$arguments = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+',
    ('/win32icon:' + (Join-Path $PSScriptRoot 'assets\app.ico')),
    ('/resource:' + (Join-Path $PSScriptRoot 'assets\app.ico') + ',DesktopNote.AppIcon'),
    ('/win32manifest:' + (Join-Path $PSScriptRoot 'src\app.manifest')),
    '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll',
    '/reference:System.Runtime.Serialization.dll', ('/out:' + $outputPath)) + $sources
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output $outputPath
