@setlocal
@pushd %~dp0
@set _C=Release

nuget restore || exit /b

msbuild -p:Configuration=%_C% -Restore || exit /b
msbuild -p:Configuration=%_C% src\test\examples\examples.proj || exit /b

dotnet test -c %_C% --no-build src\test\WixToolsetTest.Bal || exit /b
dotnet test -c %_C% --no-build src\test\WixToolsetTest.ManagedHost || exit /b

msbuild -p:Configuration=%_C% -p:NoBuild=true -t:Pack src\wixext\WixToolset.Bal.wixext.csproj || exit /b
msbuild -p:Configuration=%_C% -p:NoBuild=true -t:Pack src\WixToolset.Mba.Host\WixToolset.Mba.Host.csproj || exit /b

@popd
@endlocal
