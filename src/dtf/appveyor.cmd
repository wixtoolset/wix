@setlocal
@pushd %~dp0
@set _C=Release
@set _P=%~dp0build\%_C%\publish

nuget restore || exit /b

msbuild -p:Configuration=%_C% || exit /b

msbuild src\Tools\SfxCA\SfxCA.vcxproj -p:Configuration=%_C% -p:Platform="x64" -p:OutputPath="%_P%\WixToolset.Dtf.MSBuild\tools\\" || exit /b
msbuild src\Tools\SfxCA\SfxCA.vcxproj -p:Configuration=%_C% -p:Platform="x86" -p:OutputPath="%_P%\WixToolset.Dtf.MSBuild\tools\\" || exit /b

msbuild src\Tools\MakeSfxCA\MakeSfxCA.csproj -p:Configuration=%_C% -p:TargetFramework="net461" -p:OutputPath="%_P%\WixToolset.Dtf.MSBuild\tools\net461" || exit /b
msbuild src\Tools\MakeSfxCA\MakeSfxCA.csproj -p:Configuration=%_C% -p:TargetFramework="netcoreapp3.1" -p:OutputPath="%_P%\WixToolset.Dtf.MSBuild\tools\netcoreapp3.1" || exit /b

msbuild src\WixToolset.Dtf.MSBuild\WixToolset.Dtf.MSBuild.csproj -p:Configuration=%_C% -p:OutputPath="%_P%\WixToolset.Dtf.MSBuild" || exit /b
msbuild src\WixToolset.Dtf.MSBuild\WixToolset.Dtf.MSBuild.csproj -target:Pack -p:Configuration=%_C% || exit /b

msbuild -t:Pack -p:Configuration=%_C% src\WixToolset.Dtf.Compression || exit /b
msbuild -t:Pack -p:Configuration=%_C% src\WixToolset.Dtf.Compression.Cab || exit /b
msbuild -t:Pack -p:Configuration=%_C% src\WixToolset.Dtf.Compression.Zip || exit /b
msbuild -t:Pack -p:Configuration=%_C% src\WixToolset.Dtf.Resources || exit /b
msbuild -t:Pack -p:Configuration=%_C% src\WixToolset.Dtf.WindowsInstaller || exit /b
msbuild -t:Pack -p:Configuration=%_C% src\WixToolset.Dtf.WindowsInstaller.Linq || exit /b
msbuild -t:Pack -p:Configuration=%_C% src\WixToolset.Dtf.WindowsInstaller.Package || exit /b

@popd
@endlocal