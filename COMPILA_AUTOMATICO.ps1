# ============================================================================
# Classic Assistant - Gothic HUD - Auto Compilation Script (PowerShell)
# ============================================================================
# Script di compilazione automatico per Windows (PowerShell version)
# Esegui: powershell -ExecutionPolicy Bypass -File COMPILA_AUTOMATICO.ps1
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║                                                                ║" -ForegroundColor Magenta
Write-Host "║      CLASSIC ASSISTANT - GOTHIC HUD AUTO COMPILATION          ║" -ForegroundColor Magenta
Write-Host "║                                                                ║" -ForegroundColor Magenta
Write-Host "║      Script di compilazione automatica per Windows            ║" -ForegroundColor Magenta
Write-Host "║                                                                ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""

# Verifica se MSBuild esiste
Write-Host "🔍 Cercando MSBuild..." -ForegroundColor Yellow
$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue

if (-not $msbuild) {
    Write-Host "❌ ERRORE: MSBuild non trovato!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Soluzione: Installa Visual Studio 2019 o 2022" -ForegroundColor Yellow
    Write-Host "Scarica da: https://visualstudio.microsoft.com/vs/community/" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Premi INVIO per chiudere"
    exit 1
}

Write-Host "✅ MSBuild trovato!" -ForegroundColor Green
Write-Host ""

# Verifica se il file .sln esiste
Write-Host "📂 Cercando ClassicAssist.sln..." -ForegroundColor Yellow

if (-not (Test-Path "ClassicAssist.sln")) {
    Write-Host "❌ ERRORE: ClassicAssist.sln non trovato!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Assicurati di eseguire questo script dalla cartella radice" -ForegroundColor Yellow
    Write-Host "(dove si trova ClassicAssist.sln)" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Premi INVIO per chiudere"
    exit 1
}

Write-Host "✅ ClassicAssist.sln trovato!" -ForegroundColor Green
Write-Host ""

Write-Host "⏳ Compilazione in corso..." -ForegroundColor Yellow
Write-Host "   (potrebbe richiedere 2-5 minuti)" -ForegroundColor Gray
Write-Host ""

# Clean
Write-Host "🧹 Pulizia cartelle di build..." -ForegroundColor Yellow
& msbuild ClassicAssist.sln /t:Clean /p:Configuration=Release /p:Platform="Any CPU" /v:minimal | Out-Null

# Rebuild
Write-Host "🔨 Compilazione in corso..." -ForegroundColor Yellow
$buildResult = & msbuild ClassicAssist.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" /v:minimal

# Verifica risultato
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ COMPILAZIONE FALLITA!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possibili cause:" -ForegroundColor Yellow
    Write-Host "  • Visual Studio non aggiornato" -ForegroundColor Gray
    Write-Host "  • .NET Framework 4.8 non installato" -ForegroundColor Gray
    Write-Host "  • File XAML con errori di sintassi" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Soluzione:" -ForegroundColor Yellow
    Write-Host "  1. Apri Visual Studio" -ForegroundColor Gray
    Write-Host "  2. Apri ClassicAssist.sln" -ForegroundColor Gray
    Write-Host "  3. Build → Rebuild Solution" -ForegroundColor Gray
    Write-Host "  4. Controlla la Error List" -ForegroundColor Gray
    Write-Host ""
    Read-Host "Premi INVIO per chiudere"
    exit 1
}

Write-Host ""
Write-Host "✅ COMPILAZIONE COMPLETATA CON SUCCESSO!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Cartelle build:" -ForegroundColor Cyan
Write-Host "   - bin\Release\" -ForegroundColor Gray
Write-Host "   - bin\BuildStaging\net48\" -ForegroundColor Gray
Write-Host ""
Write-Host "🎮 Per avviare l'applicazione:" -ForegroundColor Cyan
Write-Host "   1. Apri File Explorer" -ForegroundColor Gray
Write-Host "   2. Naviga a: bin\Release\" -ForegroundColor Gray
Write-Host "   3. Doppio click su: ClassicAssist.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "🖤 Goditi il tuo nuovo tema GOTICO DRAMMATICO! 🖤" -ForegroundColor Magenta
Write-Host ""

# Chiedi se lanciare l'applicazione
$launch = Read-Host "Vuoi avviare l'applicazione ora? (S/N)"
if ($launch -eq "S" -or $launch -eq "s") {
    $exePath = ".\bin\Release\ClassicAssist.exe"
    if (Test-Path $exePath) {
        Write-Host ""
        Write-Host "🚀 Avvio in corso..." -ForegroundColor Green
        & $exePath
    } else {
        Write-Host ""
        Write-Host "❌ Eseguibile non trovato al percorso: $exePath" -ForegroundColor Red
        Read-Host "Premi INVIO per chiudere"
    }
} else {
    Write-Host ""
    Write-Host "✅ Script completato. Puoi avviare manualmente ClassicAssist.exe quando vuoi!" -ForegroundColor Green
    Read-Host "Premi INVIO per chiudere"
}
