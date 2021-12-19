@setlocal
cls
cd %~dp0
rd /s/q obj bin

pushd  D:\src\wix4\src\wix\WixToolset.Sdk
msbuild
rem msbuild -t:Publish -p:PublishDir=D:\src\wix4\build\wix\Debug\publish\WixToolset.Sdk\
rem msbuild -t:Pack
rem copy D:\src\wix4\build\artifacts\WixToolset.Sdk.*.nupkg D:\NugetLocal
rd /s/q C:\Users\Rob\.nuget\packages\wixtoolset.sdk
popd

pushd D:\src\wix4\src\test\wix\TestProjects\VcxprojStaticLib
rd /s/q Debug x64
popd

pushd D:\src\wix4\src\test\wix\TestProjects\VcxprojDll
rd /s/q Debug
popd

pushd D:\src\wix4\src\test\wix\TestProjects\CsprojClassLibraryNetCore
rd /s/q obj bin
popd

pushd D:\src\wix4\src\test\wix\TestProjects\CsprojConsoleNetCore
rd /s/q obj bin
popd

pushd D:\src\wix4\src\test\wix\TestProjects\WixprojLibraryVcxprojDll
rd /s/q obj bin
popd

pushd D:\src\wix4\src\test\wix\TestProjects\WixprojModuleCsprojWinFormsNetFx
rd /s/q obj bin
popd

pushd D:\src\wix4\src\test\wix\TestProjects\WixprojPackageVcxprojWindowsApp
rd /s/q obj bin
popd

rem msbuild.exe -Restore -bl -t:Rebuild
msbuild.exe -Restore -bl
@endlocal
