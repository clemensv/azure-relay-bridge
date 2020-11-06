#!/bin/bash
pushd "${0%/*}" > /dev/null 
if [ ! -z $APPVEYOR_BUILD_VERSION ]; then _VersionProp="/p:VersionPrefix=$APPVEYOR_BUILD_VERSION"; fi
if [ ! -z $BUILDVERSION ]; then _VersionProp="/p:VersionPrefix=$BUILDVERSION"; fi
dotnet restore
dotnet msbuild /t:Package /p:Configuration=Release /p:TargetFramework=netcoreapp3.1 $_BuildProp $_VersionProp $@
popd
