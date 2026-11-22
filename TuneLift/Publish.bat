@echo off
rem Call the powershell script, passing along any command line options
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Publish.ps1" %*
rem Save the result now, because timeout would replace it
set EXITCODE=%errorlevel%
timeout 30
exit /b %EXITCODE%
