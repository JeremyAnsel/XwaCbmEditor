@echo off
setlocal

cd "%~dp0"

For %%a in (
"XwaCbmEditor\bin\Release\net48\*.dll"
"XwaCbmEditor\bin\Release\net48\*.exe"
"XwaCbmEditor\bin\Release\net48\*.config"
) do (
xcopy /s /d "%%~a" dist\
)

For %%a in (
"XwaCbmExplorer\bin\Release\net48\*.dll"
"XwaCbmExplorer\bin\Release\net48\*.exe"
"XwaCbmExplorer\bin\Release\net48\*.config"
) do (
xcopy /s /d "%%~a" dist\
)
