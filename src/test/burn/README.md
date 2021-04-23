# integration

This repo is for building installers, and then executing xunit tests that run them and verify that they worked.

## Running tests

The main focus of these tests is to validate behavior in a real environment.
Depending on who you talk to, these are integration or system-level or end-to-end (E2E) tests.
They modify machine state so it's strongly recommended *not* to run these tests on your dev box.
They should be run on a VM instead, where you can easily roll back.

1. Run appveyor.cmd to build everything (the tests will refuse to run).
1. Copy the build\Release\netcoreapp3.1 folder to your VM.
1. Open a command prompt and navigate to the netcoreapp3.1 folder.
1. Run the runtests.cmd file to run the tests.

You can modify the runtests.cmd to run specific tests.
For example, the following line runs only the specified test:

> dotnet test --filter WixToolsetTest.BurnE2E.BasicFunctionalityTests.CanInstallAndUninstallSimpleBundle WixToolsetTest.BurnE2E.dll

The VM must have:
1. x64 .NET Core SDK of 5.0 or later (for the test runner)
1. Any version of .NET Framework (for the .NET Framework TestBA)
1. x86 .NET Core Desktop Runtime of 5.0 or later (for the .NET Core TestBA)

## Updating dependencies

Use the `updatepackage.ps1` script from https://github.com/wixtoolset/Home.
For example:

* updatepackage.ps1 -TargetFolder path\to\repo -PackageName WixToolset.Bal.wixext -NewVersion 4.0.91
* updatepackage.ps1 -TargetFolder path\to\repo -PackageName WixToolset.Data -NewVersion 4.0.199
* updatepackage.ps1 -TargetFolder path\to\repo -PackageName WixToolset.Dependency.wixext -NewVersion 4.0.25
* updatepackage.ps1 -TargetFolder path\to\repo -PackageName WixToolset.Mba.Core -NewVersion 4.0.52
* updatepackage.ps1 -TargetFolder path\to\repo -PackageName WixToolset.NetFx.wixext -NewVersion 4.0.67
* updatepackage.ps1 -TargetFolder path\to\repo -PackageName WixToolset.Util.wixext -NewVersion 4.0.82
* updatepackage.ps1 -TargetFolder path\to\repo -PackageName WixToolset.Sdk -NewVersion 4.0.0-build-0204

## Building with local changes

The micro repo model makes this very difficult and painful.
The basic idea is to make your changes in each individual repo on the master branch (to get a stable version), commit, and then use appveyor.cmd to build the nuget package.
Put your custom nuget packages into a folder, and modify each repo's nuget.config with an entry to that folder.

Alternatively, go into the NuGet package cache (%USERPROFILE%\.nuget\packages) and replace the official binaries with your locally built binaries.

Both of those approaches will poison your NuGet package cache, so you probably will want to run the following command to clear it when you're done:

> nuget locals all -clear