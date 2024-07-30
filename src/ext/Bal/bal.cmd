@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INC=1
@if /i "%1"=="clean" set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\..\build\Bal.wixext\%_C%

:: Clean

@if "%_INC%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo Building ext\Bal %_C% using %_N%

:: Restore
:: Build
:: Pack
:: Note: This test project must be restored and built directly to get all its support files laid out correctly.
::       Everything else is built by the traversal project.
msbuild -Restore -p:Configuration=%_C% -tl -nologo -m -warnaserror test\WixToolsetTest.BootstrapperApplications\WixToolsetTest.BootstrapperApplications.csproj -bl:%_L%\ext_bal_build.binlog || exit /b

msbuild bal_t.proj -p:Configuration=%_C% -tl -nologo -warnaserror -bl:%_L%\bal_build.binlog || exit /b

:: Test
dotnet test ^
  %_B%\x86\WixStdFnUnitTest.dll ^
  %_B%\net8.0\WixToolsetTest.BootstrapperApplications.dll ^
  --nologo -l "trx;LogFileName=%_L%\TestResults\bal.wixext.trx" || exit /b

@goto :end

:clean
@rd /s/q "..\..\..\build\Bal.wixext" 2> nul
@del "..\..\..\build\artifacts\WixToolset.Bal.wixext.*.nupkg" 2> nul
@del "..\..\..\build\artifacts\WixToolset.BootstrapperApplications.wixext.*.nupkg" 2> nul
@del "..\..\..\build\artifacts\WixToolset.WixStandardBootstrapperApplicationFunctionApi.*.nupkg" 2> nul
@del "%_L%\ext_bal_build.binlog" 2> nul
@del "%_L%\bal_fnsapi_build.binlog" 2> nul
@del "%_L%\bal_examples_build.binlog" 2> nul
@del "%_L%\TestResults\bal.wixext.trx" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.bal.wixext" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.bootstrapperapplications.wixext" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.wixstandardbootstrapperapplicationfunctionapi" 2> nul
@exit /b

:end
@popd
@endlocal
