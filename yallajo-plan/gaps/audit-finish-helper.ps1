$files = @(
    "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\PackagesController.cs",
    "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\ProviderBookingsController.cs",
    "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\TourApplicationsController.cs",
    "src\Hosts\YallaJo.Web\Areas\Provider\Controllers\TourImagesController.cs"
)

foreach ($f in $files) {
    $lines = Get-Content -LiteralPath $f
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match '\s+IActionResult\s+(Finish|RedirectToImages|RedirectToStatus)\s*\(') {
            Write-Output ""
            Write-Output ">>> $f (line $($i + 1))"
            $depth = 0
            $opened = $false
            for ($j = $i; $j -lt $lines.Length; $j++) {
                $bl = $lines[$j]
                Write-Output ("    " + $bl)
                foreach ($ch in $bl.ToCharArray()) {
                    if ($ch -eq '{') { $depth++; $opened = $true }
                    elseif ($ch -eq '}') { $depth-- }
                }
                if ($opened -and $depth -le 0) { break }
            }
        }
    }
}
