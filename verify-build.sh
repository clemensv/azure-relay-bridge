#!/bin/bash

if [ ! -z $APPVEYOR_BUILD_VERSION ]; then _VersionProp="/p:VersionPrefix=$APPVEYOR_BUILD_VERSION"; fi

dotnet test --verbosity=normal /p:SelfContained=false
pushd test/mysql
dotnet clean --verbosity=normal /p:Configuration=Debug /p:TargetFramework=netcoreapp3.1 $_BuildProp $_VersionProp $@
dotnet msbuild /t:build --verbosity=normal /p:Configuration=Debug /p:TargetFramework=netcoreapp3.1 $_BuildProp $_VersionProp $@
dotnet test --verbosity=normal /p:Configuration=Debug /p:TargetFramework=netcoreapp3.1 $_BuildProp $_VersionProp $@
popd
pushd test/nginx
dotnet clean --verbosity=normal /p:Configuration=Debug /p:TargetFramework=netcoreapp3.1 $_BuildProp $_VersionProp $@
dotnet msbuild /t:build --verbosity=normal /p:Configuration=Debug /p:TargetFramework=netcoreapp3.1 $_BuildProp $_VersionProp $@
dotnet test --verbosity=normal /p:Configuration=Debug /p:TargetFramework=netcoreapp3.1 $_BuildProp $_VersionProp $@
popd
