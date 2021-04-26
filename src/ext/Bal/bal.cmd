@setlocal
@pushd %~dp0

@set _C=Debug
:parse_args
@if /i "%1"=="release" set _C=Release& shift
@if not "%1"=="" shift & goto parse_args

@echo Building ext\Util %_C% using %_N%

:: Restore
nuget restore dnchost\packages.config || exit /b
msbuild -t:Restore -p:Configuration=%_C% || exit /b

:: Build
msbuild -p:Configuration=%_C%;Platform=x86 dnchost\dnchost.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=x64 dnchost\dnchost.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64 dnchost\dnchost.vcxproj || exit /b

msbuild -p:Configuration=%_C%;Platform=x86 mbahost\mbahost.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=x64 mbahost\mbahost.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64 mbahost\mbahost.vcxproj || exit /b

msbuild -p:Configuration=%_C% || exit /b
msbuild -p:Configuration=%_C% test\examples\examples.proj || exit /b

:: Test
dotnet test -c %_C% --no-build test\WixToolsetTest.Bal || exit /b
dotnet test -c %_C% --no-build test\WixToolsetTest.ManagedHost || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true wixext\WixToolset.Bal.wixext.csproj || exit /b
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true WixToolset.Mba.Host\WixToolset.Mba.Host.csproj || exit /b

@popd
@endlocal
