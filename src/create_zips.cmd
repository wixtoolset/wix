7z a "build\logs\test_burn_%GITHUB_RUN_ID%.zip" "%TEMP%\*.log" "%TEMP%\..\*.log"
7z a "build\testresults.zip" @src\testresultfilelist.txt
