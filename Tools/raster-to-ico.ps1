$ErrorActionPreference = 'Stop'
# MSBuild invokes this from several projects in parallel; one mutex serializes writes to Logo.ico.
$mtx = New-Object System.Threading.Mutex( $false, 'ClassicAssistRasterToIco' )
if ( -not $mtx.WaitOne( 120000 ) ) { Write-Error 'Timeout waiting for raster-to-ico lock'; exit 1 }
try {
    Add-Type -AssemblyName System.Drawing
    $root = Split-Path -Parent $PSScriptRoot
    $candidates = @(
        (Join-Path $root 'logo.jpg'),
        (Join-Path $root 'Logo.jpg'),
        (Join-Path $root 'Logo.png'),
        (Join-Path $root 'logo.png')
    )
    $src = $null
    foreach ( $c in $candidates ) {
        if ( Test-Path $c ) { $src = $c; break }
    }
    if ( -not $src ) { Write-Error "No raster logo in repo root (logo.jpg or Logo.png)"; exit 1 }
    $ico = Join-Path $root 'Logo.ico'
    $bmp = [System.Drawing.Bitmap]::FromFile( $src )
    $h = $bmp.GetHicon()
    try {
        $ic = [System.Drawing.Icon]::FromHandle( $h )
        $fs = $null
        for ( $j = 0; $j -lt 40 -and $null -eq $fs; $j++ ) {
            try {
                $fs = [System.IO.File]::Create( $ico )
            }
            catch {
                Start-Sleep -Milliseconds 75
            }
        }
        if ( $null -eq $fs ) { throw "Cannot write $ico (file locked?)" }
        try { $ic.Save( $fs ) }
        finally { $fs.Close() }
        $ic.Dispose()
    }
    finally {
        $bmp.Dispose()
    }
    Write-Host "Wrote $ico from $src"
}
finally {
    $mtx.ReleaseMutex()
    $mtx.Dispose()
}
