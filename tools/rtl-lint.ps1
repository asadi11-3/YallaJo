# rtl-lint.ps1 - RTL/LTR regression guard for public (non-admin) views.
# Enforces RTL1 (logical utilities only), RTL3 (bdi/font-data on numerics),
# dir attributes on technical inputs, and localized SEO titles (CON1).
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File tools/rtl-lint.ps1
# Exit code 0 = clean, 1 = violations found.

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$webRoot  = Join-Path $repoRoot 'src\Hosts\YallaJo.Web'

# Non-admin view buckets (Admin area has its own audit cadence).
$viewDirs = @(
    (Join-Path $webRoot 'Views'),
    (Join-Path $webRoot 'Areas\Public\Views'),
    (Join-Path $webRoot 'Areas\Accounts\Views'),
    (Join-Path $webRoot 'Areas\Auth\Views'),
    (Join-Path $webRoot 'Areas\Business\Views'),
    (Join-Path $webRoot 'Areas\Guide\Views'),
    (Join-Path $webRoot 'Areas\Provider\Views'),
    (Join-Path $webRoot 'Areas\Creator\Views')
) | Where-Object { Test-Path $_ }

$violations = New-Object System.Collections.Generic.List[string]

function Add-Violation([string]$rule, [string]$file, [int]$line, [string]$text) {
    $rel = $file.Substring($repoRoot.Length + 1)
    $trim = $text.Trim()
    if ($trim.Length -gt 120) { $trim = $trim.Substring(0, 120) + '...' }
    $violations.Add(('[{0}] {1}:{2}  {3}' -f $rule, $rel, $line, $trim))
}

# R1 (RTL1): physical-direction Bootstrap utilities are banned - use ms-/me-/ps-/pe-/text-start/text-end.
$physicalUtil = '\b(ml|mr|pl|pr)-(sm-|md-|lg-|xl-|xxl-)?(0|1|2|3|4|5|auto|n\d)\b|\btext-left\b|\btext-right\b|\bfloat-left\b|\bfloat-right\b'

# R2 (RTL3): culture-formatted numbers must sit inside <bdi>/dir="ltr" wrappers.
# Bare "N" is the GUID hex format specifier (element ids) - only N<digits>/C are numeric.
$numericFmt = 'ToString\("(N\d+|C\d*)"'

# R3: inline styles must not carry physical direction values.
$styleDir = 'style="[^"]*(?<![a-z-])(left|right)\s*:|style="[^"]*(margin-left|margin-right|padding-left|padding-right)\s*:'

# R4: technical inputs need an explicit dir attribute (usually dir="ltr").
$techInput = '<input\b[^>]*type="(email|tel|url)"[^>]*>'

# R5 (CON1): SetSeo titles/descriptions must come from the localizer, not literals.
$seoLiteral = '^\s*(Title|Description)\s*=\s*"'

foreach ($dir in $viewDirs) {
    $files = Get-ChildItem -Path $dir -Recurse -Filter '*.cshtml'
    foreach ($f in $files) {
        $lines = [System.IO.File]::ReadAllLines($f.FullName)
        $inSeoBlock = $false
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            $n = $i + 1

            if ($line -match $physicalUtil) {
                Add-Violation 'RTL1-physical-utility' $f.FullName $n $line
            }

            if ($line -match $numericFmt -and $line -notmatch '<bdi' -and $line -notmatch 'dir="ltr"' `
                -and $line -notmatch '^\s*(string|var|private|public|internal)\b' `
                -and $line -notmatch 'value\s*=') {
                # Heuristic: skip helper definitions and form input values; flag render sites.
                if ($line -match '@' ) {
                    Add-Violation 'RTL3-unwrapped-numeric' $f.FullName $n $line
                }
            }

            if ($line -match $styleDir) {
                Add-Violation 'RTL1-inline-physical-style' $f.FullName $n $line
            }

            if ($line -match $techInput -and $line -notmatch 'dir="') {
                Add-Violation 'DIR-missing-on-technical-input' $f.FullName $n $line
            }

            if ($line -match 'SetSeo\s*\(') { $inSeoBlock = $true }
            if ($inSeoBlock) {
                if ($line -match $seoLiteral -and $line -notmatch 'Localizer\[') {
                    Add-Violation 'CON1-hardcoded-seo' $f.FullName $n $line
                }
                if ($line -match '\}\);') { $inSeoBlock = $false }
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host ('rtl-lint: {0} violation(s) found' -f $violations.Count) -ForegroundColor Red
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host 'rtl-lint: clean (no RTL/LTR violations in non-admin views)' -ForegroundColor Green
exit 0
