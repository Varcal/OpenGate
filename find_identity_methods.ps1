$fwBase = "C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App"
$vers = Get-ChildItem $fwBase | ForEach-Object {
    $parts = $_.Name -split '\.'
    [PSCustomObject]@{ Dir = $_; Major = [int]$parts[0]; Minor = [int]$parts[1]; Patch = [int]($parts[2] -replace '[^0-9]', '') }
} | Sort-Object Major, Minor, Patch -Descending | Select-Object -First 1
Write-Host "Using framework: $($vers.Dir.FullName)"

$dll = Join-Path $vers.Dir.FullName "Microsoft.AspNetCore.Identity.dll"
Write-Host "DLL: $dll (exists: $(Test-Path $dll))"
$bytes = [System.IO.File]::ReadAllBytes($dll)
$text = [System.Text.Encoding]::UTF8.GetString($bytes)

Write-Host "`n=== AddSign* methods ==="
[regex]::Matches($text, 'AddSign\w+') | ForEach-Object { $_.Value } | Sort-Object -Unique

Write-Host "`n=== AddIdentityCookies ==="
[regex]::Matches($text, 'AddIdentity\w+') | ForEach-Object { $_.Value } | Sort-Object -Unique

Write-Host "`n=== IdentityBuilder extension methods ==="
[regex]::Matches($text, 'IdentityBuilder\w*') | ForEach-Object { $_.Value } | Sort-Object -Unique

