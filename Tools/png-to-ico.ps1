# Calls raster-to-ico.ps1 (logo.jpg or Logo.png -> Logo.ico)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'raster-to-ico.ps1')
