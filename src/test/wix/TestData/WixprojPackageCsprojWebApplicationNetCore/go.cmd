@setlocal
cls
cd %~dp0
rd /s/q obj bin

pushd ..\CsprojWebApplicationNetCore
rd /s/q obj bin
popd

pushd  D:\src\wix4\src\wix\WixToolset.BuildTasks
msbuild
popd

pushd  D:\src\wix4\src\wix\WixToolset.Sdk
msbuild -t:Publish -p:PublishDir=D:\src\wix4\build\wix\Debug\publish\WixToolset.Sdk\
msbuild -t:Pack

copy D:\src\wix4\build\artifacts\WixToolset.Sdk.*.nupkg D:\NugetLocal
rd /s/q C:\Users\Rob\.nuget\packages\wixtoolset.sdk
popd

msbuild.exe -Restore -bl
@endlocal
