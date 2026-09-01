@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ============================================
echo  ApplianceX — clean + Release package
echo ============================================
echo.

REM ---- 1) Remove old bin / obj / Release ----
echo [1/4] Cleaning old build folders...
for %%D in (
  "ApplianceManagement\bin"
  "ApplianceManagement\obj"
  "Authenticator\bin"
  "Authenticator\obj"
  "Release"
) do (
  if exist %%~D (
    echo   Removing %%~D
    rmdir /s /q %%~D
  )
)
echo.

REM ---- 2) Locate MSBuild ----
set "MSBUILD="
where msbuild >nul 2>&1 && set "MSBUILD=msbuild"
if not defined MSBUILD if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD (
  echo ERROR: MSBuild not found. Install Visual Studio Build Tools or use Developer Command Prompt.
  exit /b 1
)
echo Using: %MSBUILD%
echo.

REM ---- 3) Build both projects ----
echo [2/4] Building ApplianceManagement (Release)...
"%MSBUILD%" "ApplianceManagement\ApplianceManagement.csproj" /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU /v:minimal /nologo
if errorlevel 1 (
  echo BUILD FAILED: ApplianceManagement
  exit /b 1
)

echo [3/4] Building Authenticator (Release)...
"%MSBUILD%" "Authenticator\Authenticator.csproj" /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU /v:minimal /nologo
if errorlevel 1 (
  echo BUILD FAILED: Authenticator
  exit /b 1
)
echo.

REM ---- 4) Package separate folders ----
echo [4/4] Packaging into Release\...
mkdir "Release\ApplianceManagement" 2>nul
mkdir "Release\Authenticator" 2>nul

xcopy /y /q "ApplianceManagement\bin\Release\*.exe" "Release\ApplianceManagement\" >nul
xcopy /y /q "ApplianceManagement\bin\Release\*.dll" "Release\ApplianceManagement\" >nul 2>nul
xcopy /y /q "ApplianceManagement\bin\Release\*.config" "Release\ApplianceManagement\" >nul 2>nul
xcopy /y /q "ApplianceManagement\bin\Release\*.pdb" "Release\ApplianceManagement\" >nul 2>nul

xcopy /y /q "Authenticator\bin\Release\*.exe" "Release\Authenticator\" >nul
xcopy /y /q "Authenticator\bin\Release\*.dll" "Release\Authenticator\" >nul 2>nul
xcopy /y /q "Authenticator\bin\Release\*.config" "Release\Authenticator\" >nul 2>nul
xcopy /y /q "Authenticator\bin\Release\*.pdb" "Release\Authenticator\" >nul 2>nul

if not exist "Release\ApplianceManagement\ApplianceManagement.exe" (
  echo ERROR: ApplianceManagement.exe missing in package.
  exit /b 1
)
if not exist "Release\Authenticator\Authenticator.exe" (
  echo ERROR: Authenticator.exe missing in package.
  exit /b 1
)

echo.
echo ============================================
echo  DONE — package folders:
echo ============================================
echo   %cd%\Release\ApplianceManagement\  ^(ApplianceManagement.exe^)
echo   %cd%\Release\Authenticator\          ^(Authenticator.exe^)
echo.
dir /b "Release\ApplianceManagement"
echo.
dir /b "Release\Authenticator"
echo.
pause
endlocal
