@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building internal %_C%

:: internal
nuget restore || exit /b

:: dotnet pack -c %_C% WixBuildTools.MsgGen\WixBuildTools.MsgGen.csproj || exit /b
:: dotnet pack -c %_C% WixBuildTools.XsdGen\WixBuildTools.XsdGen.csproj || exit /b

msbuild -t:Pack WixBuildTools.TestSupport\WixBuildTools.TestSupport.csproj -p:Configuration=%_C% -nologo -m -warnaserror -bl:..\..\build\logs\internal_build.binlog || exit /b

msbuild -t:Build WixBuildTools.TestSupport.Native\WixBuildTools.TestSupport.Native.vcxproj -p:Configuration=%_C%;Platform=x86 -nologo || exit /b
msbuild -t:Build WixBuildTools.TestSupport.Native\WixBuildTools.TestSupport.Native.vcxproj -p:Configuration=%_C%;Platform=x64 -nologo || exit /b

@popd
@endlocal
