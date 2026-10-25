[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$app = Join-Path $PSScriptRoot 'bin\进步签.exe'
$root = Join-Path $PSScriptRoot ('test-results\run-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $root | Out-Null
$cases = @(
    @('--self-test', 'basic', 'self-test-result.txt'),
    @('--shared-data-test', 'storage', 'shared-data-test-result.txt'),
    @('--autostart-test', 'startup', 'autostart-test-result.txt'))
foreach ($case in $cases) {
    $directory = Join-Path $root $case[1]
    $process = Start-Process -FilePath $app -ArgumentList @($case[0], ('"' + $directory + '"')) -WindowStyle Hidden -Wait -PassThru
    $report = Join-Path $directory $case[2]
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $report)) { throw ('Test failed: ' + $case[0]) }
    $text = Get-Content -LiteralPath $report -Raw -Encoding UTF8
    if (-not $text.StartsWith('PASS:')) { throw ('Missing PASS: ' + $case[0]) }
    Write-Output $text
}
