SET RuntimeTestsEnabled=true
SET RuntimeDomainTestsEnabled=true
dotnet test WixToolsetTest.MsiE2E.dll -v normal --logger trx
