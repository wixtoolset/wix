@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INC=1
@if /i "%1"=="clean" set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\build\wix\%_C%

:: Clean

@if "%_INC%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo Building internal %_C%

:: internal
nuget restore || exit /b

:: dotnet pack -c %_C% WixBuildTools.MsgGen\WixBuildTools.MsgGen.csproj || exit /b
:: dotnet pack -c %_C% WixBuildTools.XsdGen\WixBuildTools.XsdGen.csproj || exit /b

msbuild internal_t.proj -p:Configuration=%_C% -tl -nologo -warnaserror -bl:%_L%\internal_build.binlog || exit /b

@goto :end

:clean
@rd /s/q "..\..\build\internal" 2> nul
@del "..\..\build\artifacts\WixInternal.TestSupport.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixInternal.TestSupport.Native.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixInternal.BaseBuildTasks.Sources.*.nupkg" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixinternal.testsupport" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixinternal.testsupport.native" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixinternal.basebuildtasks.sources" 2> nul
@exit /b

:end
@popd
@endlocal
