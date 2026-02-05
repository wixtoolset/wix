SET RuntimeTestsEnabled=true
dotnet test WixToolsetTest.BurnE2E.dll -v normal --logger trx;LogFileName=%TMP%\SandboxTests.trx

ROBOCOPY /NFL /NDL /S /PURGE %TMP% C:\Build\SandboxLogs *.log *.trx
