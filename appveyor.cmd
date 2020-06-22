@setlocal
@pushd %~dp0
@set _C=Release

nuget restore || exit /b

msbuild -p:Configuration=%_C% -t:Restore || exit /b

msbuild -p:Configuration=%_C% || exit /b

msbuild -p:Configuration=%_C% -t:Pack src\wixext\WixToolset.Bal.wixext.csproj || exit /b
msbuild -p:Configuration=%_C% -t:Pack src\WixToolset.Mba.Host\WixToolset.Mba.Host.csproj || exit /b

msbuild -p:Configuration=%_C% src\test\WixToolsetTest.Bal\WixToolsetTest.Bal.csproj || exit /b
dotnet test -c %_C% --no-build src\test\WixToolsetTest.Bal || exit /b

@popd
@endlocal