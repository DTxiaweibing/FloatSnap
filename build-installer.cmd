@echo off
chcp 936 >nul
title FloatSnap 打包（ARM64 + x64）
cd /d "%~dp0"

set "VERSION=1.0.2"
set "ARM64_UPGRADE={B1F5E3A2-6C1D-4B7E-9A51-2F0D8C3E7A61}"
set "X64_UPGRADE={C2A6F4B3-7D2E-4C8F-8B62-3A1E9D4F8B72}"
set "ARM64_MAIN={D3B7A5C4-8E3F-4D90-9C73-4B2F0E5A9C83}"
set "X64_MAIN={E4C8B6D5-9F40-4EA1-AD84-5C3A1F6B0D94}"
set "ARM64_AUTO={F5D9C7E6-A051-4FB2-BE95-6D4B2A7C1E05}"
set "X64_AUTO={A6EAD8F7-B162-40C3-CF06-7E5C3B8D2F16}"

echo ============================================
echo   FloatSnap 打包：单文件 exe + 安装包
echo ============================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [错误] 找不到 dotnet，请先安装 .NET 9 SDK。
    pause
    exit /b 1
)

echo [1/4] 还原打包工具 WiX ...
dotnet tool restore
if errorlevel 1 (
    echo [错误] WiX 还原失败，请检查网络后重试。
    pause
    exit /b 1
)

echo.
echo [2/4] 关闭正在运行的 FloatSnap 并清理 dist ...
taskkill /f /im FloatSnap.exe >nul 2>&1
taskkill /f /im FloatSnap_arm64.exe >nul 2>&1
taskkill /f /im FloatSnap_x64.exe >nul 2>&1
if exist dist rmdir /s /q dist
mkdir dist

echo.
echo [3/4] 发布自包含单文件 exe（ARM64 / x64）...
call :publish arm64
if errorlevel 1 goto :fail
call :publish x64
if errorlevel 1 goto :fail

echo.
echo [4/4] 生成 MSI 安装包 ...
set "WIXCMD=dotnet tool run wix build installer\FloatSnap.wxs"

%WIXCMD% -arch arm64 -d "SourceExe=%~dp0dist\arm64\FloatSnap.exe" -d "IconPath=%~dp0Assets\FloatSnap.ico" -d "Version=%VERSION%" -d "UpgradeCode=%ARM64_UPGRADE%" -d "MainComponentGuid=%ARM64_MAIN%" -d "AutoStartComponentGuid=%ARM64_AUTO%" -o "dist\FloatSnap_Setup_arm64.msi"
if errorlevel 1 goto :fail

%WIXCMD% -arch x64 -d "SourceExe=%~dp0dist\x64\FloatSnap.exe" -d "IconPath=%~dp0Assets\FloatSnap.ico" -d "Version=%VERSION%" -d "UpgradeCode=%X64_UPGRADE%" -d "MainComponentGuid=%X64_MAIN%" -d "AutoStartComponentGuid=%X64_AUTO%" -o "dist\FloatSnap_Setup_x64.msi"
if errorlevel 1 goto :fail

echo.
echo ============================================
echo   打包成功
echo   ARM64 安装包: dist\FloatSnap_Setup_arm64.msi
echo   x64   安装包: dist\FloatSnap_Setup_x64.msi
echo   绿色单文件:   dist\FloatSnap_arm64.exe
echo                 dist\FloatSnap_x64.exe
echo ============================================
explorer "%CD%\dist"
pause
exit /b 0

:publish
echo   正在发布 win-%1 ...
dotnet publish -c Release -r win-%1 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o "dist\%1"
if errorlevel 1 exit /b 1
copy /y "dist\%1\FloatSnap.exe" "dist\FloatSnap_%1.exe" >nul
if errorlevel 1 (
    echo   [错误] 复制 FloatSnap_%1.exe 失败：文件可能正在运行，请先退出后再打包。
    exit /b 1
)
exit /b 0

:fail
echo.
echo [错误] 打包失败，请看上面的输出。
pause
exit /b 1
