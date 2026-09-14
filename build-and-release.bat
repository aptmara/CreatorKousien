@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

cd /d "%~dp0"

if "%DISCORD_WEBHOOK_URL%"=="" (
    set "DISCORD_WEBHOOK_URL=https://discordapp.com/api/webhooks/1548943062900805642/k5XFW5GAH_DVX4wQJfs2zMW3bKrvb_qCbjV_cWIVl2okfyohPrjYVoiLu_ab2bDNU58h"
)

set TARGET_BRANCH=master

echo ============================================================
echo [INIT] Preparing Build and Release Process
echo ============================================================

for /f "usebackq tokens=*" %%i in (`powershell -NoProfile -Command "(Get-Date).ToString('yyyyMMdd-HHmmss')"` ) do set TIMESTAMP=%%i

if "%TIMESTAMP%"=="" (
    call :on_error "Failed to obtain timestamp."
    exit /b 1
)

set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Unity.exe
if not exist "%UNITY_PATH%" (
    call :on_error "Unity Editor not found at: %UNITY_PATH%"
    exit /b 1
)

echo Target Branch: %TARGET_BRANCH%
echo Release Tag:   v%TIMESTAMP%
echo.

set "LOG_FILE=%~dp0unity-build.log"

echo ============================================================
echo [STEP 1/4] Starting Unity Build (StandaloneWindows64)
echo ============================================================
echo Streaming build logs from Unity...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$log = $env:LOG_FILE;" ^
  "if (Test-Path $log) { Remove-Item $log -Force };" ^
  "$proc = Start-Process -FilePath $env:UNITY_PATH -ArgumentList '-batchmode', '-quit', '-projectPath', '%~dp0.', '-buildTarget', 'StandaloneWindows64', '-executeMethod', 'JenkinsBuild.BuildWindows', '-logFile', $log -PassThru;" ^
  "while (-not (Test-Path $log) -and -not $proc.HasExited) { Start-Sleep -Milliseconds 100 };" ^
  "if (Test-Path $log) {" ^
  "  $fs = [System.IO.File]::Open($log, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite);" ^
  "  $sr = New-Object System.IO.StreamReader($fs);" ^
  "  while (-not $proc.HasExited -or -not $sr.EndOfStream) {" ^
  "    while (-not $sr.EndOfStream) { Write-Host $sr.ReadLine() };" ^
  "    Start-Sleep -Milliseconds 200;" ^
  "  };" ^
  "  $sr.Close(); $fs.Close();" ^
  "};" ^
  "$proc.WaitForExit();" ^
  "exit $proc.ExitCode"

if errorlevel 1 (
    call :on_error "Unity build failed. Check unity-build.log for details."
    exit /b 1
)

echo.
echo ============================================================
echo [STEP 2/4] Verifying Build Output
echo ============================================================
if not exist "%~dp0Build\Windows\CreatorKousien.exe" (
    call :on_error "Build output not found at: Build\Windows\CreatorKousien.exe"
    exit /b 1
)
echo Build output verified successfully.
echo.

echo ============================================================
echo [STEP 3/4] Compressing Artifacts to ZIP
echo ============================================================
if not exist "%~dp0Build" mkdir "%~dp0Build"
set ZIP_NAME=CreatorKousien_%TIMESTAMP%.zip
set ZIP_PATH=%~dp0Build\%ZIP_NAME%
if exist "%ZIP_PATH%" del "%ZIP_PATH%"

tar -a -c -v -f "%ZIP_PATH%" -C "%~dp0Build\Windows" .
if errorlevel 1 (
    call :on_error "Failed to compress build artifacts."
    exit /b 1
)
echo.
echo Successfully created: %ZIP_PATH%
echo.

echo ============================================================
echo [STEP 4/4] Creating GitHub Release & Uploading Asset
echo ============================================================
set TAG_NAME=v%TIMESTAMP%

gh release create "%TAG_NAME%" "%ZIP_PATH%" --target "%TARGET_BRANCH%" --title "%TAG_NAME%" --notes "Automated release build created on %TIMESTAMP%."
if errorlevel 1 (
    call :on_error "GitHub Release creation failed. Tag: %TAG_NAME%"
    exit /b 1
)

set "RELEASE_URL=https://github.com/aptmara/CreatorKousien/releases/tag/%TAG_NAME%"
set "DOWNLOAD_URL=https://github.com/aptmara/CreatorKousien/releases/download/%TAG_NAME%/%ZIP_NAME%"

echo.
echo ============================================================
echo [SUCCESS] Release %TAG_NAME% created and uploaded successfully!
echo ============================================================
echo Target Branch:       %TARGET_BRANCH%
echo Release URL:         %RELEASE_URL%
echo Direct Download URL: %DOWNLOAD_URL%
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\notify-discord.ps1" -Status "SUCCESS" -Tag "%TAG_NAME%" -Branch "%TARGET_BRANCH%" -ZipPath "%ZIP_PATH%"

echo.
echo Press any key to exit...
pause >nul
exit /b 0

:on_error
echo.
echo ============================================================
echo [ERROR] %~1
echo ============================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\notify-discord.ps1" -Status "FAILED" -Branch "%TARGET_BRANCH%" -ErrorReason "%~1" -LogFile "%LOG_FILE%"
echo.
echo Press any key to exit...
pause >nul
goto :eof
