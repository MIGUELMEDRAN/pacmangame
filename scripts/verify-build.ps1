param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

Write-Host "==> dotnet --info"
dotnet --info

Write-Host "==> dotnet restore"
dotnet restore

Write-Host "==> dotnet build -c $Configuration"
$buildOutput = dotnet build -c $Configuration 2>&1
$buildOutput | ForEach-Object { $_ }

$blockedWarnings = @(
    'CS8600',
    'CS8604',
    'AVLN3001'
)

$found = @()
foreach ($warning in $blockedWarnings) {
    if ($buildOutput -match $warning) {
        $found += $warning
    }
}

if ($found.Count -gt 0) {
    $list = ($found | Sort-Object -Unique) -join ', '
    throw "Build completed but blocked warnings were found: $list"
}

Write-Host "Build verification passed without blocked warnings." -ForegroundColor Green
