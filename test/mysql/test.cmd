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

set PackageName=azbridge.%VersionPrefix%-%VersionSuffix%.%ImageName%.%ImageSuffix%
pushd "%~dp0"
set _MOUNTPATH=%cd%

if "%Operation%"=="build" (
 
    if NOT exist ..\..\artifacts\build\%TargetFramework%\%PackageName% (
        echo Cannot find ..\..\artifacts\build\%TargetFramework%\%PackageName% 
        exit 2
    )
    if not exist "tmp" mkdir tmp    
    copy /y ..\..\artifacts\build\%TargetFramework%\%PackageName% tmp > NUL
    docker build -f mysql.server.dockerfile . --tag azbridge-mysql-server --build-arg package_name=%PackageName%
    docker build -f mysql.client.dockerfile . --tag azbridge-mysql-client --build-arg package_name=%PackageName%

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
)

exit

:test
if %AZBRIDGE_TEST_CXNSTRING% == "" ( 
    echo AZBRIDGE_TEST_CXNSTRING environment variable must be set to valid relay connection string
    exit /b
)

rem start the web server
docker run -v %_MOUNTPATH%:/tests -d -v %_MOUNTPATH%/my.cnf:/etc/mysqld/conf.d/my.cnf --rm -d -e AZBRIDGE_TEST_CXNSTRING=%AZBRIDGE_TEST_CXNSTRING% -e MYSQL_ROOT_PASSWORD=PaSsWoRd112233 -e MYSQL_PASSWORD=PaSsWoRd112233 -e MYSQL_USER=mysql azbridge-mysql-server:latest > srvrun.log
for /f %%i in ( srvrun.log ) do set _SERVER_NAME=%%i
rem run the client
ping -n 10 127.0.0.1 > NUL 
docker run -v %_MOUNTPATH%:/tests -v %_MOUNTPATH%/my.cnf:/home/mysql/.my.cnf --rm -i -e AZBRIDGE_TEST_CXNSTRING=%AZBRIDGE_TEST_CXNSTRING% azbridge-mysql-client:latest bash /tests/run_client.sh
set RESULT=ERRORLEVEL
rem stop the web server
docker stop %_SERVER_NAME%

exit %RESULT%

:clean

set IMAGE_NAME=%ImageName%
for /f %%i in ('docker images %IMAGE_NAME% -q') do set _IMAGE_ID=%%i
if "%_IMAGE_ID%"=="" exit /b
docker rmi -f %IMAGE_NAME%:latest
exit


