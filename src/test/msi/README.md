# integration

This layer is for building installers, and then executing xunit tests that run them and verify that they worked.

## Running tests

The main focus of these tests is to validate behavior in a real environment.
Depending on who you talk to, these are integration or system-level or end-to-end (E2E) tests.
They modify machine state so it's strongly recommended *not* to run these tests on your dev box.
They should be run on a VM instead, where you can easily roll back.

1. Run build.cmd to build everything (the tests will not automatically run).
1. Copy the build\IntegrationMsi\Debug\net8.0-windows folder to your VM.
1. Open an elevated command prompt and navigate to the net8.0-windows folder.
1. Run the runtests.cmd file to run the tests.

You can modify the runtests.cmd to run specific tests.
For example, the following line runs only the specified test:

> dotnet test --filter WixToolsetTest.BurnE2E.BasicFunctionalityTests.CanInstallAndUninstallSimpleBundle_x86_wixstdba WixToolsetTest.BurnE2E.dll

The VM must have:
1. x64 .NET Core SDK of 3.1 or later (for the test runner)

## Building with local changes

The current build process will poison your NuGet package cache, so you may have to run the following command to clear it:

> nuget locals all -clear