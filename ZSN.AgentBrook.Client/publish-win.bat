@echo off
chcp 65001 >nul

REM ---- Find and load MSVC before setlocal ----
set "VCVARSALL="
set "CLANG_DIR="
call :find_msvc
if defined VCVARSALL call "%VCVARSALL%" x64 >nul 2>&1
if defined CLANG_DIR set "PATH=%CLANG_DIR%;%PATH%"

setlocal enabledelayedexpansion

echo ============================================
echo   ZSN.AgentBrook.Client Windows Publish
echo ============================================
echo.

REM ---- Configuration ----
set API_URL=http://localhost:5003
set PUBLISH_DIR=..\Publish\ClientApp
set CARGO_TARGET_DIR=C:\ZSN-AgentBrook-Build
REM Code Signing (leave empty to skip signing)
REM   Get thumbprint: certutil -store My
REM   set SIGN_CERT_THUMBPRINT=your_certificate_thumbprint_here
set SIGN_CERT_THUMBPRINT=
set SIGN_TIMESTAMP_URL=http://timestamp.digicert.com
REM ---- End Configuration ----

set "PROJECT_ROOT=%~dp0"
set "CLIENT_APP=%PROJECT_ROOT%client-app"

echo [1/6] Checking environment...
echo.

where node >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Node.js not found. Please install Node.js 20+.
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('node -v') do echo   Node.js: %%v

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK not found. Please install .NET 10.0 SDK.
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version') do echo   .NET SDK: %%v

where rustc >nul 2>&1
if %errorlevel% neq 0 (
    echo   Rust:    NOT INSTALLED - Tauri desktop build will be skipped
    set HAS_RUST=0
) else (
    for /f "tokens=*" %%v in ('rustc --version') do echo   Rust: %%v
    set HAS_RUST=1
)

where link.exe >nul 2>&1
if !errorlevel! equ 0 (
    echo   MSVC:   link.exe found
) else (
    echo   MSVC:   link.exe NOT found
)
echo.

echo [2/6] Writing production API URL...
echo   VITE_API_BASE_URL=%API_URL%/api
> "%CLIENT_APP%\.env.production" (
    echo VITE_API_BASE_URL=%API_URL%/api
    echo VITE_APP_TITLE=ZSN AgentBrook
    echo VITE_APP_ID=
    echo VITE_APP_SECRET=
)
echo   Done.
echo.

echo [3/6] Installing frontend dependencies...
cd /d "%CLIENT_APP%"
echo   Stopping node-related processes...
powershell -NoProfile -Command "$names=@('node','npm','npx','pnpm','yarn'); Get-Process -Name $names -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue" 2>nul
for /f %%t in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set "CLEAN_TS=%%t"
if "!CLEAN_TS!"=="" set "CLEAN_TS=%RANDOM%%RANDOM%"
set "OLD_NODE_MODULES_DIR=_node_modules_old_!CLEAN_TS!"
echo   Removing stale _node_modules_old*...
powershell -NoProfile -Command "Get-ChildItem -LiteralPath '.' -Directory -Force -ErrorAction SilentlyContinue | Where-Object { $_.Name -like '_node_modules_old*' } | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }" 2>nul
if exist "node_modules" (
    echo   Removing old node_modules via PowerShell...
    powershell -NoProfile -Command "$p='node_modules'; if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Recurse -Force -ErrorAction SilentlyContinue }" 2>nul
    if exist "node_modules" (
        echo   Fallback: renaming locked node_modules to !OLD_NODE_MODULES_DIR!...
        ren "node_modules" "!OLD_NODE_MODULES_DIR!" 2>nul
    )
    if exist "node_modules" (
        echo [ERROR] Failed to clean node_modules. Please close processes locking files and retry.
        pause
        exit /b 1
    )
)
if exist "!OLD_NODE_MODULES_DIR!" (
    echo   Removing !OLD_NODE_MODULES_DIR!...
    powershell -NoProfile -Command "if (Test-Path -LiteralPath '!OLD_NODE_MODULES_DIR!') { Remove-Item -LiteralPath '!OLD_NODE_MODULES_DIR!' -Recurse -Force -ErrorAction SilentlyContinue }" 2>nul
)
echo   Running npm ci...
call npm ci
if !errorlevel! neq 0 (
    echo [ERROR] npm ci failed.
    pause
    exit /b 1
)
echo.

echo [4/6] Building frontend...
call npm run build:web
if !errorlevel! neq 0 (
    echo [ERROR] Frontend build failed.
    pause
    exit /b 1
)
echo   Output: wwwroot/
echo.

echo [5/6] Publishing .NET project (Web)...
cd /d "%PROJECT_ROOT%"
dotnet publish ZSN.AgentBrook.Client.csproj -c Release -o "%PUBLISH_DIR%" --self-contained false
if %errorlevel% neq 0 (
    echo [ERROR] .NET publish failed.
    pause
    exit /b 1
)
echo   Done: %PUBLISH_DIR%
echo.

if not "%HAS_RUST%"=="1" (
    echo [6/6] Skipping Tauri desktop build (Rust not installed^).
    echo   Install Rust from https://rustup.rs/ to enable desktop build.
    goto :win_summary
)

where link.exe >nul 2>&1
if !errorlevel! neq 0 (
    echo [6/6] Skipping Tauri desktop build (MSVC linker not found^).
    goto :win_summary
)

echo [6/6] Building Tauri desktop app (Windows x64)...
echo   Cargo target dir: %CARGO_TARGET_DIR%
echo   This may take several minutes on first build...
cd /d "%CLIENT_APP%"
mkdir "%CARGO_TARGET_DIR%" 2>nul
set CARGO_TARGET_DIR=%CARGO_TARGET_DIR%

REM Ensure x64 target is installed (needed when building on ARM64 Windows)
rustup target add x86_64-pc-windows-msvc >nul 2>&1

call npm run tauri:build -- --target x86_64-pc-windows-msvc
if !errorlevel! neq 0 (
    echo [ERROR] Tauri build failed.
    pause
    exit /b 1
)

set "DESKTOP_DIR=%PROJECT_ROOT%..\Publish\ClientApp-Desktop"
if not exist "%DESKTOP_DIR%" mkdir "%DESKTOP_DIR%"

echo   Copying build artifacts...

REM Copy raw executable
if exist "%CARGO_TARGET_DIR%\x86_64-pc-windows-msvc\release\zsn-agentbrook.exe" (
    copy /y "%CARGO_TARGET_DIR%\x86_64-pc-windows-msvc\release\zsn-agentbrook.exe" "%DESKTOP_DIR%\" >nul
    echo   Copied: zsn-agentbrook.exe
)

REM Copy NSIS installer (recommended for distribution)
set "NSIS_FOUND=0"
for /r "%CARGO_TARGET_DIR%\x86_64-pc-windows-msvc\release\bundle\nsis" %%f in (*setup*.exe) do (
    copy /y "%%f" "%DESKTOP_DIR%\" >nul
    echo   Copied: %%~nxf
    set "NSIS_FOUND=1"
)
for /r "%CARGO_TARGET_DIR%\release\bundle\nsis" %%f in (*setup*.exe) do (
    copy /y "%%f" "%DESKTOP_DIR%\" >nul
    echo   Copied: %%~nxf
    set "NSIS_FOUND=1"
)
if "!NSIS_FOUND!"=="0" echo   [WARNING] NSIS installer not found!

REM Copy MSI installer (alternative)
for /r "%CARGO_TARGET_DIR%\x86_64-pc-windows-msvc\release\bundle\msi" %%f in (*.msi) do (
    copy /y "%%f" "%DESKTOP_DIR%\" >nul
    echo   Copied: %%~nxf
)

REM Code signing (optional)
if defined SIGN_CERT_THUMBPRINT (
    if "!NSIS_FOUND!"=="1" (
        echo   Signing installer...
        for %%f in ("%DESKTOP_DIR%\*setup*.exe") do (
            signtool sign /sha1 !SIGN_CERT_THUMBPRINT! /tr !SIGN_TIMESTAMP_URL! /td sha256 /fd sha256 "%%f" >nul 2>&1
            if !errorlevel! equ 0 (
                echo   Signed: %%~nxf
            ) else (
                echo   [WARNING] Failed to sign: %%~nxf
            )
        )
    )
) else (
    if "!NSIS_FOUND!"=="1" (
        echo.
        echo   [NOTE] Installer is NOT code-signed.
        echo   Windows SmartScreen may warn users on first run.
        echo   Users click "More info" then "Run anyway" to proceed.
        echo   To enable signing, set SIGN_CERT_THUMBPRINT in this script.
    )
)

echo.
echo   Cleaning up temp files...
cd /d "%CLIENT_APP%"
powershell -NoProfile -Command "Get-ChildItem -LiteralPath '.' -Directory -Force -ErrorAction SilentlyContinue | Where-Object { $_.Name -like '_node_modules_old*' } | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }" 2>nul
echo   Cleaning up CARGO_TARGET_DIR...
if exist "%CARGO_TARGET_DIR%" (
    powershell -NoProfile -Command "if (Test-Path -LiteralPath $env:CARGO_TARGET_DIR) { Remove-Item -LiteralPath $env:CARGO_TARGET_DIR -Recurse -Force -ErrorAction SilentlyContinue }" 2>nul
    if exist "%CARGO_TARGET_DIR%" echo   [WARNING] Failed to remove %CARGO_TARGET_DIR%
)

:win_summary
cd /d "%PROJECT_ROOT%"
echo ============================================
echo   Windows publish complete!
echo.
echo   Web:     %PROJECT_ROOT%%PUBLISH_DIR%
echo   Desktop: %PROJECT_ROOT%..\Publish\ClientApp-Desktop\
echo   API:     %API_URL%
echo.
echo   Start Web:
echo     cd %PUBLISH_DIR%
echo     dotnet ZSN.AgentBrook.Client.dll
echo     Visit: http://localhost:5006
echo ============================================

pause
exit /b

REM ---- Subroutine: find MSVC and Clang ----
:find_msvc
for /d %%y in ("%ProgramFiles%\Microsoft Visual Studio\*") do (
    for %%e in (Community Professional Enterprise BuildTools Preview) do (
        if exist "%%y\%%e\VC\Auxiliary\Build\vcvarsall.bat" set "VCVARSALL=%%y\%%e\VC\Auxiliary\Build\vcvarsall.bat"
        if exist "%%y\%%e\VC\Tools\Llvm\x64\bin\clang.exe" set "CLANG_DIR=%%y\%%e\VC\Tools\Llvm\x64\bin"
    )
)
if not defined CLANG_DIR (
    if exist "%ProgramFiles%\LLVM\bin\clang.exe" set "CLANG_DIR=%ProgramFiles%\LLVM\bin"
)
exit /b
