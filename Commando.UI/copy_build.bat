rem %1 = solution dir
rem %2 = configuration (debug | release)
rem %3 = target dir

del "%TEMP%\Commando.sdf"
if exist "%~3bin\Commando.sdf" copy /y "%~3bin\Commando.sdf" "%TEMP%\Commando.sdf"

xcopy /s /y /d "%~1Commando.UI\bin\%2\*" "%~3bin\"

xcopy /s /y /d "%~1Commando.Mozilla\bin\%2\*" "%~3Extensions\Commando.Mozilla\"
xcopy /s /y /d "%~1Commando.Google\bin\%2\*" "%~3Extensions\Commando.Google\"
del /s "%~3Extensions\Commando.Standard1.*"
del /s "%~3Extensions\Commando.API1.*"

xcopy /s /y /d "%~1packages\Microsoft.SqlServer.Compact.4.0.8876.1\NativeBinaries\x86\*.*" "%~3bin\x86\"
xcopy /s /y /d "%~1packages\Microsoft.SqlServer.Compact.4.0.8876.1\NativeBinaries\amd64\*.*" "%~3bin\amd64\"

xcopy /s /y /d "%~1Commando.Standard1Impl\bin\%2\*" "%~3Extensions\Commando.Standard1Impl\"

if exist "%TEMP%\Commando.sdf" copy /y "%TEMP%\Commando.sdf" "%~3bin\Commando.sdf"