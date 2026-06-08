$ErrorActionPreference = "Continue"

# 1) Inspect the 8 PRG-flagged methods to determine if they're really missing redirect
$flagged = @(
    @{ File = "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\PackagesController.cs"; Method = "Submit" }
    @{ File = "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\ProviderBookingsController.cs"; Method = "Confirm" }
    @{ File = "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\ProviderBookingsController.cs"; Method = "Complete" }
    @{ File = "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\TourApplicationsController.cs"; Method = "Approve" }
    @{ File = "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\TourApplicationsController.cs"; Method = "Open" }
    @{ File = "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\TourApplicationsController.cs"; Method = "Close" }
    @{ File = "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\TourImagesController.cs"; Method = "Upload" }
    @{ File = "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\TourImagesController.cs"; Method = "Delete" }
)

Write-Output "=== METHOD BODIES FOR PRG-FLAGGED ACTIONS ==="
foreach ($f in $flagged) {
    $path = $f.File
    $method = $f.Method
    $lines = Get-Content -LiteralPath $path
    $startLine = -1
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match "^\s*public\s+(async\s+)?Task<IActionResult>\s+$method\s*\(") {
            $startLine = $i
            break
        }
    }
    if ($startLine -lt 0) {
        Write-Output ">>> $path : $method NOT FOUND"
        continue
    }
    Write-Output ""
    Write-Output ">>> $path :: $method (line $($startLine + 1))"
    # Print until matching brace closes
    $depth = 0
    $opened = $false
    for ($j = $startLine; $j -lt $lines.Length; $j++) {
        $bl = $lines[$j]
        Write-Output ("    " + $bl)
        foreach ($ch in $bl.ToCharArray()) {
            if ($ch -eq '{') { $depth++; $opened = $true }
            elseif ($ch -eq '}') { $depth-- }
        }
        if ($opened -and $depth -le 0) { break }
    }
}

Write-Output ""
Write-Output "=== INLINE ICurrentUser.HasPermission CHECK (Tour editor + others without class-level perm) ==="
$tourEditors = @(
    "ToursController.cs", "TourPricingController.cs", "TourSchedulesController.cs",
    "TourWaypointsController.cs", "TourGuidesController.cs", "TourImagesController.cs",
    "TourApplicationsController.cs", "TourAvailabilityController.cs",
    "BookingsController.cs", "ProviderBookingsController.cs",
    "ReviewsController.cs", "SettingsController.cs"
)
foreach ($name in $tourEditors) {
    $path = "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\$name"
    $content = Get-Content -LiteralPath $path -Raw
    $hasInline = ($content -match 'ICurrentUser' -or $content -match 'HasPermission')
    $hasInjection = ($content -match 'private\s+readonly\s+ICurrentUser')
    Write-Output ("{0,-32}  ICurrentUser-injected={1}  HasPermission-mentioned={2}" -f $name, $hasInjection, $hasInline)
}
