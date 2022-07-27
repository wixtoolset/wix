@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\..\build\logs
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@echo Building ext\Bal %_C% using %_N%

:: Restore
nuget restore dnchost\packages.config || exit /b
msbuild -t:Restore -p:Configuration=%_C% || exit /b

:: Build
msbuild -p:Configuration=%_C%;Platform=x86 test\examples\TestEngine\Example.TestEngine.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=x64 test\examples\TestEngine\Example.TestEngine.vcxproj || exit /b
msbuild -p:Configuration=%_C%;Platform=ARM64 test\examples\TestEngine\Example.TestEngine.vcxproj || exit /b

msbuild -p:Configuration=%_C% || exit /b

dotnet test test\WixToolsetTest.Dnc.HostGenerator -c %_C% --nologo --no-build -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.Dnc.HostGenerator.trx" || exit /b

msbuild -Restore -p:Configuration=%_C% test\examples\examples.proj -bl || exit /b

:: Test
dotnet test test\WixToolsetTest.Bal -c %_C% --no-build -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.Bal.trx" || exit /b
dotnet test test\WixToolsetTest.ManagedHost -c %_C% --no-build -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.ManagedHost.trx" || exit /b

:: Pack
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true wixext\WixToolset.Bal.wixext.csproj || exit /b
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true WixToolset.Dnc.HostGenerator\WixToolset.Dnc.HostGenerator.csproj || exit /b
msbuild -t:Pack -p:Configuration=%_C% -p:NoBuild=true WixToolset.Mba.Host\WixToolset.Mba.Host.csproj || exit /b

@popd
@endlocal
