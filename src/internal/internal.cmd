@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Building internal %_C%

:: internal

nuget restore || exit /b

:: dotnet pack -c %_C% WixBuildTools.MsgGen\WixBuildTools.MsgGen.csproj || exit /b
:: dotnet pack -c %_C% WixBuildTools.XsdGen\WixBuildTools.XsdGen.csproj || exit /b

msbuild -t:Pack -p:Configuration=%_C% WixBuildTools.TestSupport\WixBuildTools.TestSupport.csproj || exit /b

msbuild -t:Build -p:Configuration=%_C% WixBuildTools.TestSupport.Native\WixBuildTools.TestSupport.Native.vcxproj || exit /b

@popd
@endlocal
