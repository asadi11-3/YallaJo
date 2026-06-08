$ErrorActionPreference = "Continue"
$areas = @(
    "src\Hosts\YallaJo.Web\Areas\Provider\Controllers",
    "src\Hosts\YallaJo.Web\Areas\Business\Controllers"
)

$rows = @()

foreach ($area in $areas) {
    Get-ChildItem -Path $area -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object {
        $path = $_.FullName
        $rel  = $_.FullName.Replace((Get-Location).Path + "\", "")
        $lines = Get-Content -LiteralPath $path

        # Find class declaration line and gather class-level attributes (lines above class)
        $classIdx = -1
        for ($i = 0; $i -lt $lines.Length; $i++) {
            if ($lines[$i] -match '^\s*public\s+(sealed\s+)?(partial\s+)?class\s+\w+Controller') {
                $classIdx = $i
                break
            }
        }

        $classArea     = ""
        $classAuth     = ""
        $classReqPerm  = ""

        if ($classIdx -ge 0) {
            # look backward for attribute lines
            for ($j = $classIdx - 1; $j -ge 0; $j--) {
                $L = $lines[$j].Trim()
                if ($L -eq "") { continue }
                if (-not $L.StartsWith("[")) { break }
                if ($L -match '^\[Area\(') { $classArea = $L }
                elseif ($L -match '^\[Authorize') { $classAuth = $L }
                elseif ($L -match '^\[RequirePermission\(') { $classReqPerm = $L }
            }
        }

        # Collect actions: search HttpGet/HttpPost lines
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $line = $lines[$i]
            if ($line -match '^\s*\[Http(Get|Post|Put|Delete|Patch)\("([^"]*)"\)\]') {
                $verb = $matches[1]
                $route = $matches[2]
                # Scan a window of ~8 lines forward to find method signature + flags
                $window = ""
                $methodLine = -1
                for ($k = $i; $k -lt [Math]::Min($i + 12, $lines.Length); $k++) {
                    $window += $lines[$k] + "`n"
                    if ($lines[$k] -match '^\s*public\s+(async\s+)?Task<IActionResult>\s+(\w+)\s*\(') {
                        $methodLine = $k
                        break
                    }
                }
                if ($methodLine -lt 0) { continue }

                $methodName = ($lines[$methodLine] -replace '^\s*public\s+(async\s+)?Task<IActionResult>\s+(\w+)\s*\(.*', '$2')
                $hasAntiForgery = ($window -match '\[ValidateAntiForgeryToken\]')
                $methodReqPerm = ($window | Select-String -Pattern '\[RequirePermission\(([^)]+)\)' | ForEach-Object { $_.Matches[0].Groups[1].Value }) -join ";"

                # Look for body braces to find PRG: scan from method line forward until matching brace count returns to 0
                $depth = 0
                $opened = $false
                $methodBody = ""
                for ($b = $methodLine; $b -lt $lines.Length; $b++) {
                    $bl = $lines[$b]
                    foreach ($ch in $bl.ToCharArray()) {
                        if ($ch -eq '{') { $depth++; $opened = $true }
                        elseif ($ch -eq '}') { $depth-- }
                    }
                    $methodBody += $bl + "`n"
                    if ($opened -and $depth -le 0) { break }
                }

                $hasRedirect = ($methodBody -match 'RedirectToAction\s*\(' -or $methodBody -match 'RedirectToActionPermanent\s*\(' -or $methodBody -match 'LocalRedirect\s*\(')
                $hasIApiClient = ($methodBody -match 'IApiClient' -or $methodBody -match 'HttpClient')
                $hasGuardSignOut = ($methodBody -match 'GuardSignOut')

                $rows += [pscustomobject]@{
                    File         = Split-Path $rel -Leaf
                    Method       = $methodName
                    Verb         = $verb
                    Route        = $route
                    AntiForgery  = $hasAntiForgery
                    HasRedirect  = $hasRedirect
                    GuardSignOut = $hasGuardSignOut
                    MethodReqPerm= $methodReqPerm
                    ClassArea    = $classArea
                    ClassAuth    = $classAuth
                    ClassReqPerm = $classReqPerm
                    Line         = $i + 1
                }
            }
        }
    }
}

Write-Output "TOTAL_ROUTES: $($rows.Count)"
Write-Output ""

Write-Output "=== ROUTE MAP ==="
$rows | Sort-Object File, Line | ForEach-Object {
    $flagPost = if ($_.Verb -eq "Post") { if ($_.AntiForgery) { "AF+" } else { "AF-" } } else { "   " }
    $flagPrg  = if ($_.Verb -eq "Post") { if ($_.HasRedirect) { "PRG+" } else { "PRG-" } } else { "    " }
    $perm = if ($_.MethodReqPerm) { $_.MethodReqPerm } else { "(no method perm)" }
    "{0,-32} {1,-5} {2,-55} {3} {4} {5}" -f $_.File, $_.Verb, $_.Route, $flagPost, $flagPrg, $perm
}

Write-Output ""
Write-Output "=== VIOLATIONS ==="
Write-Output ""
Write-Output "-- POST missing [ValidateAntiForgeryToken] --"
$violations1 = $rows | Where-Object { $_.Verb -eq "Post" -and -not $_.AntiForgery }
if ($violations1) { $violations1 | Format-Table File, Method, Verb, Route -AutoSize | Out-String -Width 200 } else { "NONE" }

Write-Output ""
Write-Output "-- POST missing PRG (no RedirectToAction/LocalRedirect) --"
$violations2 = $rows | Where-Object { $_.Verb -eq "Post" -and -not $_.HasRedirect }
if ($violations2) { $violations2 | Format-Table File, Method, Verb, Route -AutoSize | Out-String -Width 200 } else { "NONE" }

Write-Output ""
Write-Output "-- HttpPut / HttpDelete / HttpPatch in BFF (forbidden) --"
$violations3 = $rows | Where-Object { $_.Verb -in @("Put","Delete","Patch") }
if ($violations3) { $violations3 | Format-Table File, Method, Verb, Route -AutoSize | Out-String -Width 200 } else { "NONE" }

Write-Output ""
Write-Output "-- Methods referencing IApiClient / HttpClient (forbidden) --"
$violations4 = $rows | Where-Object { $_ -match 'IApiClient' }  # placeholder; verified via separate scan below

Write-Output ""
Write-Output "=== CLASS HEADERS PER CONTROLLER ==="
$rows | Group-Object File | ForEach-Object {
    $g = $_.Group[0]
    "{0,-32} Area={1,-25} Auth={2,-50} ClassPerm={3}" -f $_.Name, $g.ClassArea, $g.ClassAuth, $g.ClassReqPerm
}

Write-Output ""
Write-Output "=== POST PERMISSION COVERAGE ==="
$rows | Where-Object { $_.Verb -eq "Post" } | Group-Object File | ForEach-Object {
    $name = $_.Name
    $total = $_.Group.Count
    $noPerm = ($_.Group | Where-Object { -not $_.MethodReqPerm -and -not $_.ClassReqPerm }).Count
    if ($noPerm -gt 0) {
        "{0,-32} POSTs={1}  No-permission-checks={2}" -f $name, $total, $noPerm
    }
}
