@echo off
REM ============================================================================
REM Avvia lo script di compilazione automatica
REM ============================================================================

REM Ottieni il percorso dello script
setlocal enabledelayedexpansion
set SCRIPT_DIR=%~dp0

REM Prova a eseguire il .bat prima
cls
echo.
echo Avvio script di compilazione...
echo.

REM Esegui il batch script
call "%SCRIPT_DIR%COMPILA_AUTOMATICO.bat"

REM Se il batch fallisce, prova PowerShell
if errorlevel 1 (
    echo.
    echo Tentativo con PowerShell...
    echo.
    powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%COMPILA_AUTOMATICO.ps1"
)

endlocal
