@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INC=1
@if /i "%1"=="clean" set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\build\libs\%_C%

:: Clean

@if "%_INC%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo Building libs %_C%

msbuild -Restore libs_t.proj -p:Configuration=%_C% -tl -nologo -m -warnaserror -bl:%_L%\libs_build.binlog || exit /b

dotnet test ^
 %_B%\net6.0\WixToolsetTest.Versioning.dll ^
 %_B%\x86\DUtilUnitTest.dll ^
 %_B%\x64\DUtilUnitTest.dll ^
 --nologo -l "trx;LogFileName=%_L%\TestResults\libs.trx" || exit /b

@goto :end

:clean
@rd /s/q "..\..\build\libs" 2> nul
@del "..\..\build\artifacts\WixToolset.DUtil.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.Versioning.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.WcaUtil.*.nupkg" 2> nul
@del "%_L%\TestResults\libs.trx" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.dutil" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.versioning" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.wcautil" 2> nul
@exit /b

:end
@popd
@endlocal
