@setlocal
cls
cd %~dp0
rd /s/q obj bin

pushd  D:\src\wix4\src\wix\WixToolset.Sdk
msbuild
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

msbuild -Restore -bl
@endlocal
