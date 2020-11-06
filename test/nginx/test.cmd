rem @echo off

echo %~1 %~2 %~3 %~4 %~5 %~6

if not "%~1" == "" set Operation=%~1
if not "%~2" == "" set ImageName=%~2
if not "%~3" == "" set ImageSuffix=%~3
if not "%~4" == "" set VersionPrefix=%~4
if not "%~5" == "" set VersionSuffix=%~5
if not "%~6" == "" set TargetFramework=%~6

if "%Operation%"=="" set Operation=build
if "%ImageName%"=="" set ImageName=ubuntu.18.04-x64
if "%ImageSuffix%"=="" set ImageSuffix=deb
if "%VersionSuffix%"=="" set VersionSuffix=preview
if "%VersionPrefix%"=="" set VersionPrefix=1.0.0
if "%TargetFramework%"=="" set TargetFramework=netcoreapp3.1

pushd "%~dp0"

set PackageName=azbridge.%VersionPrefix%-%VersionSuffix%.%ImageName%.%ImageSuffix%
set _MOUNTPATH=%cd%

if "%Operation%"=="build" (
    
    if NOT exist ..\..\artifacts\build\%TargetFramework%\%PackageName% (
        echo Cannot find ..\..\artifacts\build\%TargetFramework%\%PackageName% 
        exit 2
    )
    if not exist "tmp" mkdir tmp    
    copy /y ..\..\artifacts\build\%TargetFramework%\%PackageName% tmp > NUL
    docker build -f %ImageName%.server.dockerfile . --tag azbridge-nginx-server-%ImageName% --build-arg package_name=%PackageName%
    docker build -f %ImageName%.client.dockerfile . --tag azbridge-nginx-client-%ImageName% --build-arg package_name=%PackageName%
    rd /s /q tmp
    popd
    exit /b
)

if "%Operation%"=="test" (
    goto test
)

if "%Operation%"=="clean" (
   goto clean
)
else (
    echo "Unknown operation"
    popd
)
exit

:test 

if '%AZBRIDGE_TEST_CXNSTRING%' == "" ( 
    echo AZBRIDGE_TEST_CXNSTRING environment variable must be set to valid relay connection string
    exit /b
)

rem start the web server
docker run -v %_MOUNTPATH%:/tests --rm -d -e AZBRIDGE_TEST_CXNSTRING=%AZBRIDGE_TEST_CXNSTRING% azbridge-nginx-server-%ImageName%:latest bash /tests/run_nginx.sh > srvrun.log
for /f %%i in ( srvrun.log ) do set _SERVER_NAME=%%i
rem run the client
docker run -v %_MOUNTPATH%:/tests --rm -e AZBRIDGE_TEST_CXNSTRING=%AZBRIDGE_TEST_CXNSTRING% azbridge-nginx-client-%ImageName%:latest bash /tests/run_client.sh
rem stop the web server
docker stop %_SERVER_NAME%

fc /L downloaded.txt index.html > NUL
if ERRORLEVEL 1 (
    set _RESULT=%ERRORLEVEL%
    echo Error %_RESULT%
)
if ERRORLEVEL 0 (
    set _RESULT=0
    echo OK
)
popd
exit _RESULT

:clean

set IMAGE_NAME=%ImageName%

for /f %%i in ('docker images azbridge-nginx-server-%IMAGE_NAME% -q') do set _IMAGE_ID=%%i
if "%_IMAGE_ID%"=="" exit /b
docker rmi -f azbridge-nginx-server-%ImageName%:latest

for /f %%i in ('docker images azbridge-nginx-client-%IMAGE_NAME% -q') do set _IMAGE_ID=%%i
if "%_IMAGE_ID%"=="" exit /b
docker rmi -f azbridge-nginx-client-%ImageName%:latest
popd
