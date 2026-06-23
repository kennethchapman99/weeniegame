@echo off
REM Runs the backyard map-based greybox planner importer from the repo root.
cd /d "%~dp0.."
set "SOURCE=%~1"
set "AERIAL=%~2"
set "ANNOTATED=%~3"
set "UNITY=%~4"
if "%SOURCE%"=="" set "SOURCE=MAPS - template\01_exports_flat"
if "%AERIAL%"=="" set "AERIAL=MAPS - template\Backyard - Aerial.png"
if "%ANNOTATED%"=="" set "ANNOTATED=MAPS - template\01_exports_flat\export_map.HEIC"
if "%UNITY%"=="" set "UNITY=unity\CheddarAndCocoa"
echo Backyard Map-Based Greybox Planner - importer
echo Source:    %SOURCE%
echo Aerial:    %AERIAL%
echo Annotated: %ANNOTATED%
echo Unity:     %UNITY%
where python >nul 2>nul
if errorlevel 1 (echo ERROR: python not found.& pause& exit /b 1)
if not exist "%SOURCE%" (echo ERROR: Source folder not found: %SOURCE%& pause& exit /b 1)
if not exist "%UNITY%" (echo ERROR: Unity project not found: %UNITY%& pause& exit /b 1)
python backyard_reference_importer_tool\tools\backyard_importer\import_backyard_reference.py --source "%SOURCE%" --aerial "%AERIAL%" --annotated "%ANNOTATED%" --unity "%UNITY%"
echo (HEIC conversion needs macOS sips; on Windows export export_map.HEIC to PNG by hand - see docs.)
echo Done. Next: Unity menu Cheddar ^& Cocoa ^> Backyard ^> Build Map-Based Greybox Planner
pause
