@setlocal
@pushd %~dp0

@set _C=Debug
@set _L=%~dp0..\..\build\logs
:parse_args
@if /i "%1"=="release" set _C=Release
@if not "%1"=="" shift & goto parse_args

@set _P_OBJ=%~dp0..\..\build\wix\obj\publish_t\%_C%\
@set _P=%~dp0..\..\build\wix\%_C%\publish\
@set _RCO=/S /R:1 /W:1 /NP /XO  /NS /NC /NFL /NDL /NJH /NJS

@echo Building wix %_C%

:: Restore
msbuild -t:Restore wix.sln -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\wix_restore.binlog || exit /b


:: Build
msbuild wixnative\wixnative_t.proj -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\wixnative_build.binlog || exit /b

msbuild wix.sln -p:Configuration=%_C% -nologo -m -warnaserror -bl:%_L%\wix_build.binlog || exit /b


:: Pre-Publish Test
dotnet test -c %_C% --no-build --nologo test\WixToolsetTest.Converters -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.Converters.trx" || exit /b
dotnet test -c %_C% --no-build --nologo test\WixToolsetTest.Converters.Symbolizer -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.Converters.Symbolizer.trx" || exit /b
dotnet test -c %_C% --no-build --nologo test\WixToolsetTest.Core.Burn -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.Core.Burn.trx" || exit /b
dotnet test -c %_C% --no-build --nologo test\WixToolsetTest.Core.Native -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.Core.Native.trx" || exit /b
dotnet test -c %_C% --no-build --nologo test\WixToolsetTest.CoreIntegration -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.CoreIntegration.trx" || exit /b
dotnet test -c %_C% --no-build --nologo test\WixToolsetTest.Heat -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.Heat.trx" || exit /b


:: Publish
msbuild publish_t.proj -p:Configuration=%_C% -nologo -warnaserror -bl:%_L%\wix_publish.binlog || exit /b

robocopy %_P_OBJ%\WixToolset.Sdk\separate\net472\x86\buildtasks %_P%\WixToolset.Sdk\tools\net472\x86 %_RCO% /XF Microsoft.Build.*.dll
robocopy %_P_OBJ%\WixToolset.Sdk\separate\net472\x86\heat %_P%\WixToolset.Sdk\tools\net472\x86 %_RCO%
robocopy %_P_OBJ%\WixToolset.Sdk\separate\net472\x86\wix %_P%\WixToolset.Sdk\tools\net472\x86 %_RCO%

robocopy %_P_OBJ%\WixToolset.Sdk\separate\net472\x64\buildtasks %_P%\WixToolset.Sdk\tools\net472\x64 %_RCO% /XF Microsoft.Build.*.dll
robocopy %_P_OBJ%\WixToolset.Sdk\separate\net472\x64\heat %_P%\WixToolset.Sdk\tools\net472\x64 %_RCO%
robocopy %_P_OBJ%\WixToolset.Sdk\separate\net472\x64\wix %_P%\WixToolset.Sdk\tools\net472\x64 %_RCO%

robocopy %_P_OBJ%\WixToolset.Sdk\separate\netcoreapp3.1\buildtasks %_P%\WixToolset.Sdk\tools\netcoreapp3.1 %_RCO% /XF Microsoft.Build.*.dll
robocopy %_P_OBJ%\WixToolset.Sdk\separate\netcoreapp3.1\heat %_P%\WixToolset.Sdk\tools\netcoreapp3.1 %_RCO%
robocopy %_P_OBJ%\WixToolset.Sdk\separate\netcoreapp3.1\wix %_P%\WixToolset.Sdk\tools\netcoreapp3.1 %_RCO%

msbuild -t:Publish -p:Configuration=%_C% -nologo -warnaserror -p:PublishDir=%_P%WixToolset.Sdk\ WixToolset.Sdk\WixToolset.Sdk.csproj -bl:%_L%\wix_sdk_publish.binlog || exit /b

:: TODO - used by MsbuildFixture.ReportsInnerExceptionForUnexpectedExceptions test
:: msbuild -t:Publish -Restore -p:Configuration=%_C% -p:TargetFramework=net472 -p:RuntimeIdentifier=linux-x86 -p:PublishDir=%_P%WixToolset.Sdk\broken\net472\ wix\wix.csproj || exit /b


:: Test
dotnet test -c %_C% --no-build --nologo test\WixToolsetTest.BuildTasks -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.BuildTasks.trx" || exit /b
dotnet test -c %_C% --no-build --nologo test\WixToolsetTest.Sdk -l "trx;LogFileName=%_L%\TestResults\WixToolsetTest.Sdk.trx" || exit /b

:: Pack
msbuild pack_t.proj -p:Configuration=%_C% -nologo -m -warnaserror -bl:..\..\build\logs\wix_pack.binlog || exit /b

@popd
@endlocal
