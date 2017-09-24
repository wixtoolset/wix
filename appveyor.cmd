@setlocal
@pushd %~dp0
@set _P=%~dp0build\Release\publish

dotnet publish -c Release -o %_P% -r win-x86 src\wix
dotnet publish -c Release -o %_P% -r win-x86 src\WixToolset.BuildTasks

dotnet pack -c Release src\WixToolset.Core.InternalPackage

@popd
@endlocal