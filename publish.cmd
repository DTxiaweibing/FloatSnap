@echo off
chcp 936 >nul
title FloatSnap 构建脚本
cd /d "%~dp0"

rem 自动识别架构：ARM64 机器（骁龙平板等）必须发 win-arm64，
rem 直接从 %PROCESSOR_ARCHITECTURE% 判断会踩“x64 模拟环境报 AMD64”的坑，
rem 所以再用 PROCESSOR_IDENTIFIER（含 ARMv8）兜底。
set "RID=win-x64"
if /i "%PROCESSOR_ARCHITECTURE%"=="ARM64" set "RID=win-arm64"
if /i "%PROCESSOR_ARCHITEW6432%"=="ARM64" set "RID=win-arm64"
if not "%PROCESSOR_IDENTIFIER:ARM=%"=="%PROCESSOR_IDENTIFIER%" set "RID=win-arm64"
if /i "%PROCESSOR_ARCHITECTURE%"=="x86" if /i "%PROCESSOR_ARCHITEW6432%"=="" set "RID=win-x86"

echo ============================================
echo   FloatSnap 一键构建
echo ============================================
echo.
echo 当前目录: %CD%
echo 目标架构: %RID%
echo.

echo [0/4] 检查 .NET SDK ...
where dotnet
if errorlevel 1 (
    echo.
    echo [错误] 系统找不到 dotnet 命令。
    echo 可能原因：安装的是 ".NET Runtime" 而不是 ".NET SDK"，
    echo 或者安装后没有重启电脑/终端。
    echo 请到 https://dotnet.microsoft.com/download/dotnet/9.0 下载 64 位 SDK。
    echo.
    pause
    exit /b 1
)
dotnet --version

echo.
echo [1/4] 关闭正在运行的旧版本并清理 publish ...
taskkill /f /im FloatSnap.exe >nul 2>&1
if exist "publish" rmdir /s /q "publish"
if exist "publish" (
    echo [警告] publish 目录暂时删不掉，可能仍被占用，继续尝试覆盖。
)

echo.
echo [2/4] 还原并编译...
dotnet build -c Release
if errorlevel 1 (
    echo.
    echo [错误] 编译失败，请把上面的报错内容发给开发者。
    pause
    exit /b 1
)

echo.
echo [3/4] 发布 exe ^(%RID%^)...
dotnet publish -c Release -r %RID% --self-contained false -o publish
if errorlevel 1 (
    echo.
    echo [错误] 发布失败，请把上面的报错内容发给开发者。
    pause
    exit /b 1
)

echo.
echo [4/4] 检查产物...
if not exist "publish\FloatSnap.exe" (
    echo [错误] publish\FloatSnap.exe 未生成，请反馈此窗口内容。
    pause
    exit /b 1
)

echo.
echo ============================================
echo   构建成功！
echo   可执行文件: %CD%\publish\FloatSnap.exe
echo ============================================
explorer "%CD%\publish"
pause
