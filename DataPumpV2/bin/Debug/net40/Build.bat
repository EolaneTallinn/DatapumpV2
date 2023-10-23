@echo off

setlocal enabledelayedexpansion

set "filename=T:\SHARED\FTP\Screenshots\Aleksander\DataPumpV2\DataPumpV2\bin\Debug\net40\DataPumpHashVersion.str"
set "chars=ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
set "length=10"

(for /f "delims=" %%a in ('type "%filename%"') do (
    set "line=%%a"
    set "randomString="
    for /L %%i in (1,1,%length%) do (
        set /a "index=!random! %% 62"
        for %%c in (!index!) do set "randomChar=!chars:~%%c,1!"
        set "randomString=!randomString!!randomChar!"
    )
    echo !randomString!
)) > "%filename%.tmp"

move /y "%filename%.tmp" "%filename%"

endlocal

timeout /t 9 /nobreak >nul

REM ----------------- BUILD OF THE APPLICATION [START] -----------------

REM Step 1: Set the path to MSBuild
set msbuildPath="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

REM Step 2: Set the path to the solution file
set solutionPath="T:\SHARED\FTP\Screenshots\Aleksander\DataPumpV2\DataPumpV2.sln"

REM Step 3: Build the solution using MSBuild
%msbuildPath% %solutionPath% /t:Build /p:Configuration=Debug

REM Step 4: Check the build output for success or failure
IF %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b %ERRORLEVEL%
)

REM Step 5: Build completed successfully!
echo Build completed successfully!

REM ----------------- BUILD OF THE APPLICATION [END] -----------------
pause