@echo off
REM ============================================================================
REM Classic Assistant - Gothic HUD - Auto Compilation Script
REM ============================================================================
REM Script di compilazione automatica per Windows
REM Basta eseguire questo file e la compilazione partirà automaticamente
REM ============================================================================

cls
echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║                                                                ║
echo ║      CLASSIC ASSISTANT - GOTHIC HUD AUTO COMPILATION          ║
echo ║                                                                ║
echo ║      Questo script compilerà automaticamente il progetto     ║
echo ║                                                                ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.

REM Verifica se MSBuild esiste
where /q msbuild
if errorlevel 1 (
    echo ❌ ERRORE: MSBuild non trovato!
    echo.
    echo Soluzione: Installa Visual Studio 2019 o 2022
    echo Scarica da: https://visualstudio.microsoft.com/vs/community/
    echo.
    pause
    exit /b 1
)

echo ✅ MSBuild trovato!
echo.
echo 📂 Cerco ClassicAssist.sln...

REM Controlla se il file .sln esiste
if not exist "ClassicAssist.sln" (
    echo ❌ ERRORE: ClassicAssist.sln non trovato nella cartella corrente!
    echo.
    echo Assicurati di eseguire questo script dalla cartella radice del progetto
    echo (dove si trova ClassicAssist.sln)
    echo.
    pause
    exit /b 1
)

echo ✅ ClassicAssist.sln trovato!
echo.
echo ⏳ Inizio compilazione... (potrebbe richiedere 2-5 minuti)
echo.

REM Clean soluzione
echo 🧹 Pulizia cartelle di build...
msbuild ClassicAssist.sln /t:Clean /p:Configuration=Release /p:Platform="Any CPU" /v:minimal

REM Rebuild soluzione
echo 🔨 Compilazione in corso...
msbuild ClassicAssist.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" /v:minimal

REM Verifica se la compilazione è riuscita
if errorlevel 1 (
    echo.
    echo ❌ COMPILAZIONE FALLITA!
    echo.
    echo Possibili cause:
    echo • Visual Studio non aggiornato
    echo • .NET Framework 4.8 non installato
    echo • File XAML con errori di sintassi
    echo.
    echo Soluzione:
    echo 1. Apri Visual Studio
    echo 2. Apri ClassicAssist.sln
    echo 3. Build → Rebuild Solution
    echo 4. Controlla la Error List per dettagli
    echo.
    pause
    exit /b 1
)

echo.
echo ✅ COMPILAZIONE COMPLETATA CON SUCCESSO!
echo.
echo 📁 Cartelle build:
echo    - bin/Release/
echo    - bin/BuildStaging/net48/
echo.
echo 🎮 Per avviare l'applicazione:
echo    1. Apri File Explorer
echo    2. Naviga a: bin\Release\
echo    3. Doppio click su: ClassicAssist.exe
echo.
echo 🖤 Goditi il tuo nuovo tema GOTICO DRAMMATICO! 🖤
echo.
pause
