@if "%_echo%" neq "on" echo off
setlocal
setlocal enableextensions
setlocal enabledelayedexpansion

if defined VisualStudioVersion goto :Run

set _VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist %_VSWHERE% (
  for /f "usebackq tokens=*" %%i in (`%_VSWHERE% -latest -prerelease -property installationPath`) do set _VSCOMNTOOLS=%%i\Common7\Tools
)
if not exist "%_VSCOMNTOOLS%" set _VSCOMNTOOLS=%VS140COMNTOOLS%
if not exist "%_VSCOMNTOOLS%" (
    echo Error: Visual Studio 2015 or 2017 required.
    echo        Please see https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
    exit /b 1
)

set VSCMD_START_DIR="%~dp0"
call "%_VSCOMNTOOLS%\VsDevCmd.bat"

:Run

:: this makes test explorer work in Visual Studio
:: Found the env vars by running build -MsBuildLogging=/bl, then look at the Exec task in RunTest target

set CMD_START_DIR=%~dp0
set NUGET_PACKAGES=%CMD_START_DIR%packages\
set DOTNET_SDK_PATH=%CMD_START_DIR%Tools\dotnetcli\

set TEST_TARGETRID=win-x64
set BUILDRID=win-x64
set BUILD_ARCHITECTURE=x64
set BUILD_CONFIGURATION=Debug

set TEST_ARTIFACTS=%CMD_START_DIR%Bin\tests\%TEST_TARGETRID%.%BUILD_CONFIGURATION%\
set MNA_TFM=netcoreapp3.0
set MNA_SEARCH=%CMD_START_DIR%Bin\obj\%TEST_TARGETRID%.%BUILD_CONFIGURATION%\hostFxr\host\fxr\*

:: We expect one hostfxr version directory in the MNA_SEARCH path
for /d  %%i in (%MNA_SEARCH%) do (
  set MNA_PATH=%%i
  )

set MNA_VERSION=%MNA_PATH:*host\fxr\=%

devenv %CMD_START_DIR%\Microsoft.DotNet.CoreSetup.sln
