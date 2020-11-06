pushd "%~dp0"

set xunitrunner=xunit.console.exe
where %xunitrunner%  2>&1 
if ERRORLEVEL 1 set xunitrunner=C:\Tools\xUnit20\xunit.console.exe
"%xunitrunner%" "test\unit\Microsoft.Azure.Relay.Bridge.Tests\bin\Debug\net48\Microsoft.Azure.Relay.Bridge.Tests.dll" -appveyor

rem cd test\nginx
rem msbuild /t:clean,build,vstest
rem popd




