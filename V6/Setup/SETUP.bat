@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ============================================
echo   ApplianceX  -  Database Setup
echo ============================================
echo.

set DBNAME=APPLIANCE_DB
set SERVER=.
set CONNFILE=%~dp0connectionstring.txt
set SQLFILE=%~dp0database.sql

if not exist "%SQLFILE%" (
  echo ERROR: database.sql not found in this folder.
  pause
  exit /b 1
)

echo Detecting SQL Server (sqlcmd)...
where sqlcmd >nul 2>&1
if errorlevel 1 (
  echo.
  echo sqlcmd not found.
  echo SQL Server Express / full must already be installed.
  echo This script cannot download SQL 2008 automatically.
  echo.
  echo 1. Install SQL Server Express (any 2012+ is fine)
  echo 2. Re-run this SETUP.bat as Administrator
  echo.
  pause
  exit /b 1
)

echo.
set /p SERVER=SQL Server name [default . ]:
if "%SERVER%"=="" set SERVER=.

echo.
echo Creating / updating database %DBNAME% on %SERVER% ...
sqlcmd -S "%SERVER%" -E -i "%SQLFILE%"
if errorlevel 1 (
  echo.
  echo sqlcmd failed. Trying named instance SQLEXPRESS...
  sqlcmd -S ".\SQLEXPRESS" -E -i "%SQLFILE%"
  if errorlevel 1 (
    echo Failed to create database. Check SQL is running and you have rights.
    pause
    exit /b 1
  )
  set SERVER=.\SQLEXPRESS
)

set CS=Data Source=%SERVER%;Initial Catalog=%DBNAME%;Integrated Security=True;Connect Timeout=30

(
  echo %CS%
) > "%CONNFILE%"

echo.
echo ============================================
echo   DONE
echo ============================================
echo Connection string saved to:
echo   %CONNFILE%
echo.
echo Content:
type "%CONNFILE%"
echo.
echo Next steps:
echo  1. Copy connection string into Authenticator -^> SQL Connection
echo  2. Generate license.dat and place next to ApplianceManagement.exe
echo  3. Or put same string in App.config connectionStrings
echo.
pause
