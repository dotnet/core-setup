@if not defined _echo @echo off
setlocal

:SetupArgs
:: Initialize the args that will be passed to cmake
set __thisScriptFolder=%~dp0
set __nativeWindowsDir=%~dp0Windows
set __binDir=%~dp0..\..\Bin
set __rootDir=%~dp0..\..
set __CMakeBinBaseDir=""
set __IntermediatesBaseDir=""
set __BuildArch=x64
set __appContainer=""
set __VCBuildArch=x86_amd64
set CMAKE_BUILD_TYPE=Debug
set "__LinkArgs= "
set "__LinkLibraries= "
set __PortableBuild=0

:Arg_Loop
if [%1] == [] goto :ToolsVersion
if /i [%1] == [Release]     ( set CMAKE_BUILD_TYPE=Release&&shift&goto Arg_Loop)
if /i [%1] == [Debug]       ( set CMAKE_BUILD_TYPE=Debug&&shift&goto Arg_Loop)

if /i [%1] == [AnyCPU]      ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [x86]         ( set __BuildArch=x86&&set __VCBuildArch=x86&&shift&goto Arg_Loop)
if /i [%1] == [arm]         ( set __BuildArch=arm&&set __VCBuildArch=x86_arm&&set __SDKVersion="-DCMAKE_SYSTEM_VERSION=10.0"&&shift&goto Arg_Loop)
if /i [%1] == [x64]         ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)
if /i [%1] == [amd64]       ( set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&goto Arg_Loop)

if /i [%1] == [rid]         ( set __TargetRid=%2&&shift&&shift&goto Arg_Loop)
shift
goto :Arg_Loop

:ToolsVersion
:: Determine the tools version to pass to cmake/msbuild
if not defined VisualStudioVersion (
    if defined VS140COMNTOOLS (
        goto :VS2015
    ) 
    goto :MissingVersion
) 
if "%VisualStudioVersion%"=="14.0" (
    goto :VS2015
) 

:MissingVersion
:: Can't find VS 2013+
echo Error: Visual Studio 2015 required  
echo        Please see https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
exit /b 1

:VS2015
:: Setup vars for VS2015
set __VSVersion=vs2015
set __PlatformToolset=v140

:: Set the environment for the native build
call "%VS140COMNTOOLS%\..\..\VC\vcvarsall.bat" %__VCBuildArch%

:SetupDirs
:: Setup to cmake the native components
echo Commencing build of native UWP components
echo.

if %__CMakeBinBaseDir% == "" (
    set "__CMakeBinBaseDir=%__binDir%\%__TargetRid%.%CMAKE_BUILD_TYPE%\"
)
if %__IntermediatesBaseDir% == "" (
    set "__IntermediatesBaseDir=%__binDir%\obj\%__TargetRid%.%CMAKE_BUILD_TYPE%\"
)
set "__ResourcesDir=%__binDir%\obj\%__TargetRid%.%CMAKE_BUILD_TYPE%\uwphostResourceFiles"
set "__CMakeBinBaseDir=%__CMakeBinBaseDir:\=/%"
set "__IntermediatesBaseDir=%__IntermediatesBaseDir:\=/%"

set __SDKVersion="-DCMAKE_SYSTEM_VERSION=10.0"
set __CMAKE_SYSTEM=
set __CMAKE_CUSTOM_DEFINES=

call :GenerateAndCompile uwphost %__thisScriptFolder%
if ERRORLEVEL 1 goto :Failure
set __CMAKE_SYSTEM="-DCMAKE_SYSTEM_NAME:STRING=WindowsStore"
set __CMAKE_CUSTOM_DEFINES="-DUWPHOST_LIB_PATH:STRING=%__IntermediatesBaseDir%\uwphost\Host\UWPHost\%CMAKE_BUILD_TYPE%"
call :GenerateAndCompile uwpshim %__thisScriptFolder%host\UWPShim
if ERRORLEVEL 1 goto :Failure

echo Done building Native components
exit /b 0

:GenerateAndCompile
    set __IntermediatesDir=%__IntermediatesBaseDir%%1
    set __CMakeBinDir=%__CMakeBinBaseDir%%1

    echo Building project system for %__IntermediatesDir% Source: %2
    :: Check that the intermediate directory exists so we can place our cmake build tree there
    if exist "%__IntermediatesDir%" rd /s /q "%__IntermediatesDir%"
    if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"

    :: Regenerate the VS solution

    echo Calling "%__nativeWindowsDir%\gen-buildsys-win.bat" %2 %__BuildArch% %__ResourcesDir% %__CMakeBinDir%
    pushd "%__IntermediatesDir%"
    call "%__nativeWindowsDir%\gen-buildsys-win.bat" %2 %__BuildArch% %__ResourcesDir% %__CMakeBinDir%
    popd

    :CheckForProj
    :: Check that the project created by Cmake exists
    if exist "%__IntermediatesDir%\ALL_BUILD.vcxproj" goto BuildNativeProj
    exit /b 1

    :BuildNativeProj
    :: Build the project created by Cmake
    set __msbuildArgs=/p:Platform=%__BuildArch% /p:PlatformToolset="%__PlatformToolset%"

    cd %__rootDir%

    echo %__rootDir%\run.cmd build-native -- "%__IntermediatesDir%\ALL_BUILD.vcxproj" /t:rebuild /p:Configuration=%CMAKE_BUILD_TYPE% %__msbuildArgs% /v:d
    call %__rootDir%\run.cmd build-native -- "%__IntermediatesDir%\ALL_BUILD.vcxproj" /t:rebuild /p:Configuration=%CMAKE_BUILD_TYPE% %__msbuildArgs% /v:d
    IF ERRORLEVEL 1 (
        exit /b 1
    )
    
    goto :eof

:Failure
:: Build failed
echo Failed to generate native component build project!
exit /b 1