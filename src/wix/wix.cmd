@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs

:parse_args
@if /i "%1"=="release" set _C=Release
@if /i "%1"=="inc" set _INC=1
@if /i "%1"=="clean" set _CLEAN=1
@if not "%1"=="" shift & goto parse_args

@set _B=%~dp0..\..\build\wix\%_C%

:: Clean

@if "%_INC%"=="" call :clean
@if NOT "%_CLEAN%"=="" goto :end

@echo Building wix %_C%

:: Restore
msbuild -t:Restore wix.sln -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\wix_restore.binlog || exit /b


:: Build
msbuild wixnative\wixnative_t.proj -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\wixnative_build.binlog || exit /b

msbuild wix.sln -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\wix_build.binlog || exit /b

msbuild publish_t.proj -p:Configuration=%_C% -nologo -warnaserror -bl:%_L%\wix_publish.binlog || exit /b

msbuild -t:Publish -p:Configuration=%_C% -nologo -warnaserror WixToolset.Sdk\WixToolset.Sdk.csproj -bl:%_L%\wix_sdk_publish.binlog || exit /b

:: TODO - used by MsbuildFixture.ReportsInnerExceptionForUnexpectedExceptions test
:: msbuild -t:Publish -Restore -p:Configuration=%_C% -p:TargetFramework=net472 -p:RuntimeIdentifier=linux-x86 -p:PublishDir=%_P%WixToolset.Sdk\broken\net472\ wix\wix.csproj || exit /b


:: Test
dotnet test ^
 %_B%\test\WixToolsetTest.Converters\net6.0\WixToolsetTest.Converters.dll ^
 %_B%\test\WixToolsetTest.Converters.Symbolizer\net472\WixToolsetTest.Converters.Symbolizer.dll ^
 %_B%\test\WixToolsetTest.Core\net6.0\WixToolsetTest.Core.dll ^
 %_B%\test\WixToolsetTest.Core.Native\net6.0\win-x64\WixToolsetTest.Core.Native.dll ^
 %_B%\test\WixToolsetTest.CoreIntegration\net6.0\WixToolsetTest.CoreIntegration.dll ^
 %_B%\test\WixToolsetTest.BuildTasks\net472\WixToolsetTest.BuildTasks.dll ^
 %_B%\test\WixToolsetTest.Sdk\net472\WixToolsetTest.Sdk.dll ^
 --nologo -l "trx;LogFileName=%_L%\TestResults\wix.trx" || exit /b


:: Pack
msbuild pack_t.proj -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\wix_pack.binlog || exit /b

@goto :end

:clean
@rd /s/q "..\..\build\wix" 2> nul
@del "..\..\build\artifacts\wix.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.BuildTasks.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.Converters.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.Core.*.nupkg" 2> nul
@del "..\..\build\artifacts\WixToolset.Sdk.*.nupkg" 2> nul
@del "%_L%\TestResults\wix.trx" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wix" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.buildtasks" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.converters" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.converters.symbolizer" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.core" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.core.burn" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.core.native" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.core.windowsinstaller" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixinternal.core.testpackage" 2> nul
@rd /s/q "%USERPROFILE%\.nuget\packages\wixtoolset.sdk" 2> nul
@exit /b

:end
@popd
@endlocal
