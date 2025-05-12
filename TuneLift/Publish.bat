@echo off
setlocal

REM --- CONFIG ---
set BASENAME=TuneLift
set PROJECT=%BASENAME%.csproj
set RUNTIME=win-x64
set CONFIG=Release
set OUTPUT=Publish

REM --- GET VERSION ---
findstr /i "<Version>" "%PROJECT%" > _ver.tmp
set /p fullline=<_ver.tmp
set "str1=%fullline:*<Version>=%"
set "version=%str1:</Version>=%"
del _ver.tmp

:: Verify version extraction
if not defined version (
	echo.
    echo Error: Could not extract version from %PROJECT%.
	echo.
	pause
    exit /b 1
)

REM --- PUBLISH ---
dotnet publish %PROJECT% -c %CONFIG% -r %RUNTIME% --self-contained false ^
    /p:PublishSingleFile=true /p:PublishTrimmed=false /p:IncludeNativeLibrariesForSelfExtract=false ^
    -o %OUTPUT%
move "publish\%baseName%.exe" "publish\%baseName%-%version%.exe" 2>nul 

echo.
echo Publish complete. Output: %OUTPUT%\%BASENAME%-%VERSION%.exe
echo.
pause
