@setlocal
@pushd %~dp0
@set _C=Release

msbuild -p:Configuration=%_C% -warnaserror -Restore || exit /b
msbuild -p:Configuration=%_C% src\TestData -Restore || exit /b

dotnet test -c %_C% --no-build src\WixToolsetTest.BurnE2E || exit /b

@popd
@endlocal
