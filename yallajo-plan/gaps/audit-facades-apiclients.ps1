$ErrorActionPreference = "Continue"

$roots = @(
    @{ Name="Provider.Facades";    Path="src\Hosts\YallaJo.Web\Areas\Provider\Facades";    Kind="Facade" }
    @{ Name="Provider.ApiClients"; Path="src\Hosts\YallaJo.Web\Areas\Provider\ApiClients"; Kind="ApiClient" }
    @{ Name="Business.Facades";    Path="src\Hosts\YallaJo.Web\Areas\Business\Facades";    Kind="Facade" }
    @{ Name="Business.ApiClients"; Path="src\Hosts\YallaJo.Web\Areas\Business\ApiClients"; Kind="ApiClient" }
)

$rows = @()

foreach ($r in $roots) {
    Get-ChildItem -Path $r.Path -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object {
        $name = $_.BaseName
        $content = Get-Content -LiteralPath $_.FullName -Raw
        $isSealed = $content -match 'public\s+sealed\s+class\s+\w+'
        $hasSuffix = ($r.Kind -eq "Facade" -and $name.EndsWith("Facade")) -or ($r.Kind -eq "ApiClient" -and $name.EndsWith("ApiClient"))
        $usesHttpContext = $content -match 'HttpContext|TempData|IHttpContextAccessor'
        $usesHttpClient = $content -match 'HttpClient\b' -and -not ($content -match 'IApiClient')
        $injectsIApi = $content -match 'IApiClient\s+\w+' -or $content -match 'IApiClient '
        $evictCount = ([regex]::Matches($content, 'EvictByTagAsync')).Count
        $hasIOutputCache = $content -match 'IOutputCacheStore'
        # Count methods (rough): public async Task<...> Name(
        $methodCount = ([regex]::Matches($content, 'public\s+async\s+Task<[^>]+>\s+\w+\s*\(')).Count

        # Count IApiClient calls in ApiClient files
        $apiCallCount = ([regex]::Matches($content, '_api\.(GetAsync|PostAsync|PutAsync|PatchAsync|DeleteAsync|PostFileAsync)')).Count

        $rows += [pscustomobject]@{
            Group         = $r.Name
            Kind          = $r.Kind
            File          = $_.Name
            Sealed        = $isSealed
            SuffixOK      = $hasSuffix
            UsesHttpCtx   = $usesHttpContext
            DirectHttpCl  = $usesHttpClient
            InjectsIApi   = $injectsIApi
            HasOutputCache= $hasIOutputCache
            EvictCount    = $evictCount
            Methods       = $methodCount
            ApiCalls      = $apiCallCount
        }
    }
}

Write-Output "=== FACADE/APICLIENT AUDIT ==="
$rows | Sort-Object Group, File | Format-Table -AutoSize | Out-String -Width 220

Write-Output ""
Write-Output "=== VIOLATIONS ==="
Write-Output "-- Non-sealed --"
$v1 = $rows | Where-Object { -not $_.Sealed }
if ($v1) { $v1 | Format-Table File, Group -AutoSize | Out-String -Width 200 } else { "NONE" }

Write-Output ""
Write-Output "-- Wrong suffix --"
$v2 = $rows | Where-Object { -not $_.SuffixOK }
if ($v2) { $v2 | Format-Table File, Group -AutoSize | Out-String -Width 200 } else { "NONE" }

Write-Output ""
Write-Output "-- Facade uses HttpContext/TempData (forbidden) --"
$v3 = $rows | Where-Object { $_.Kind -eq "Facade" -and $_.UsesHttpCtx }
if ($v3) { $v3 | Format-Table File, Group -AutoSize | Out-String -Width 200 } else { "NONE" }

Write-Output ""
Write-Output "-- Facade uses HttpClient directly --"
$v4 = $rows | Where-Object { $_.Kind -eq "Facade" -and $_.DirectHttpCl }
if ($v4) { $v4 | Format-Table File, Group -AutoSize | Out-String -Width 200 } else { "NONE" }

Write-Output ""
Write-Output "-- ApiClient missing IApiClient injection --"
$v5 = $rows | Where-Object { $_.Kind -eq "ApiClient" -and -not $_.InjectsIApi }
if ($v5) { $v5 | Format-Table File, Group -AutoSize | Out-String -Width 200 } else { "NONE" }

Write-Output ""
Write-Output "-- ApiClient method-to-API-call ratio < 0.8 (possible multi-line implementations) --"
$v6 = $rows | Where-Object { $_.Kind -eq "ApiClient" -and $_.Methods -gt 0 -and ($_.ApiCalls -lt $_.Methods) }
if ($v6) { $v6 | Format-Table File, Methods, ApiCalls -AutoSize | Out-String -Width 200 } else { "NONE" }

Write-Output ""
Write-Output "-- Facade with NO EvictByTagAsync call (even though file likely has writes) --"
$v7 = $rows | Where-Object { $_.Kind -eq "Facade" -and $_.EvictCount -eq 0 }
if ($v7) { $v7 | Format-Table File, Group, Methods -AutoSize | Out-String -Width 200 } else { "NONE" }

Write-Output ""
Write-Output "=== FACADE/APICLIENT NAME PAIRING ==="
$facades = ($rows | Where-Object { $_.Kind -eq "Facade" } | ForEach-Object { $_.File -replace '\.cs$','' -replace 'Facade$','' })
$apiclients = ($rows | Where-Object { $_.Kind -eq "ApiClient" } | ForEach-Object { $_.File -replace '\.cs$','' -replace 'ApiClient$','' })
$facadeOnly = $facades | Where-Object { $apiclients -notcontains $_ }
$apiClientOnly = $apiclients | Where-Object { $facades -notcontains $_ }
Write-Output "Facades without matching ApiClient:"
if ($facadeOnly) { $facadeOnly | ForEach-Object { "  $_" } } else { "  NONE" }
Write-Output "ApiClients without matching Facade:"
if ($apiClientOnly) { $apiClientOnly | ForEach-Object { "  $_" } } else { "  NONE" }

Write-Output ""
Write-Output "=== EvictByTagAsync TAG INVENTORY (all facades) ==="
foreach ($r in $roots | Where-Object { $_.Kind -eq "Facade" }) {
    Get-ChildItem -Path $r.Path -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw
        $tags = [regex]::Matches($content, 'EvictByTagAsync\s*\(\s*([^,)]+)') | ForEach-Object { $_.Groups[1].Value.Trim() }
        if ($tags) {
            foreach ($t in $tags) {
                "  {0,-30}  {1}" -f $_.Name, $t
            }
        }
    }
}
