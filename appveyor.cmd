@setlocal
@pushd %~dp0
@set _C=Release

nuget restore || exit /b

msbuild -p:Configuration=%_C% src\test\WixToolsetTest.Core.Native\WixToolsetTest.Core.Native.csproj || exit /b
dotnet test -c %_C% --no-build src\test\WixToolsetTest.Core.Native\WixToolsetTest.Core.Native.csproj || exit /b

msbuild -t:Pack -p:Configuration=%_C% src\WixToolset.Core.Native\WixToolset.Core.Native.csproj || exit /b

@popd
@endlocal