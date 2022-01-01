7z a "build\logs\test_burn_%GITHUB_RUN_ID%.zip" "%TEMP%\*.log" "%TEMP%\..\*.log" || exit /b
7z a "build\testresults.zip" @src\testresultfilelist.txt || exit /b