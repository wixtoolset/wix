@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INC=1
@if /i "%1"=="clean" set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\build\api\%_C%

:: Clean

@if "%_INC%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo Building api %_C%

:: Restore
:: Build
:: Pack

msbuild api_t.proj -p:Configuration=%_C% -tl -nologo -warnaserror -bl:%_L%\api_build.binlog || exit /b

:: Test
dotnet test ^
 %_B%\net6.0\WixToolsetTest.Data.dll ^
 %_B%\net6.0\win-x86\WixToolsetTest.Mba.Core.dll ^
 %_B%\x86\BalUtilUnitTest.dll ^
 %_B%\x86\BextUtilUnitTest.dll ^
 --nologo -l "trx;LogFileName=%_L%\TestResults\api.trx" || exit /b

@goto :end

:clean
@rd /s/q "..\..\build\api" 2> nul
@del "..\..\build\artifacts\WixToolset.BalUtil.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.BextUtil.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.BootstrapperCore.Native.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.Data.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.Extensibility.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.Mba.Core.*.nupkg" 2> nul
@del "%_L%\TestResults\api.trx" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.balutil" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.bextutil" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.bootstrappercore.native" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.data" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.extensibility" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.mba.core" 2> nul
@exit /b

:end
@popd
@endlocal
