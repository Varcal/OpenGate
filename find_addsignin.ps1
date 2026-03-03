$fwBase = "C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App"
$ver = Get-ChildItem $fwBase | Sort-Object Name -Descending | Select-Object -First 1
Write-Host "Framework: $($ver.FullName)"

$identityDlls = Get-ChildItem $ver.FullName -Filter "Microsoft.AspNetCore.Identity*.dll"
foreach ($dll in $identityDlls) {
    Write-Host "  $($dll.Name)"
    $bytes = [System.IO.File]::ReadAllBytes($dll.FullName)
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    $matches = [regex]::Matches($text, 'AddSignIn\w*')
    if ($matches.Count -gt 0) {
        Write-Host "    -> Found AddSignIn methods:"
        $matches | ForEach-Object { $_.Value } | Sort-Object -Unique | ForEach-Object { Write-Host "       $_" }
    }
}

# Also search all Identity-related DLLs in nuget cache
Write-Host "`nSearching NuGet cache..."
$nugetPath = "$env:USERPROFILE\.nuget\packages"
$idDlls = Get-ChildItem "$nugetPath\microsoft.aspnetcore.identity" -Recurse -Filter "Microsoft.AspNetCore.Identity.dll" -ErrorAction SilentlyContinue
foreach ($dll in $idDlls) {
    Write-Host "  NuGet: $($dll.FullName)"
    $bytes = [System.IO.File]::ReadAllBytes($dll.FullName)
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    $m = [regex]::Matches($text, 'AddSign\w+')
    if ($m.Count -gt 0) {
        $m | ForEach-Object { $_.Value } | Sort-Object -Unique | ForEach-Object { Write-Host "    $_" }
    }
}

