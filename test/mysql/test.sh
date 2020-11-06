#!/bin/bash

if [ ! -z $1 ]; then Operation=$1; fi
if [ ! -z $2 ]; then ImageName=$2; fi
if [ ! -z $3 ]; then ImageSuffix=$3; fi
if [ ! -z $4 ]; then VersionPrefix=$4; fi
if [ ! -z $5 ]; then VersionSuffix=$5; fi
if [ ! -z $6 ]; then TargetFramework=$6; fi

if [ -z ${Operation+x} ]; then Operation='build'; fi
if [ -z ${ImageName+x} ]; then ImageName='ubuntu.18.04-x64'; fi
if [ -z ${ImageSuffix+x} ]; then VersionSuffix='deb'; fi
if [ -z ${VersionSuffix+x} ]; then VersionSuffix='preview'; fi
if [ -z ${VersionPrefix+x} ]; then VersionPrefix='1.0.0'; fi
if [ -z ${TargetFramework+x} ]; then TargetFramework='netcoreapp3.1'; fi

echo $@

PackageName=azbridge.$VersionPrefix-$VersionSuffix.$ImageName.$ImageSuffix

if [ "${Operation}" == "build" ]; then
    pushd "${0%/*}" > /dev/null 
    if [ ! -d "tmp" ]; then mkdir tmp; fi
  
    cp ../../artifacts/build/$TargetFramework/$PackageName tmp/ > /dev/null
    docker build -f mysql.server.dockerfile . --tag azbridge-mysql-server --build-arg package_name=$PackageName
    _RESULT=$?
    if [ $_RESULT -ne 0 ]; then
       rm -rf tmp
       popd
       exit $_RESULT
    fi
    docker build -f mysql.client.dockerfile . --tag azbridge-mysql-client --build-arg package_name=$PackageName
    _RESULT=$?
    if [ $_RESULT -ne 0 ]; then
       rm -rf tmp
       popd
       exit $_RESULT
    fi
    rm -rf tmp
    popd
else 
    if [ "${Operation}" == "test" ]; then
        pushd "${0%/*}" > /dev/null 
        IMAGE_NAME=$PackageName
        _CXNSTRING=$AZBRIDGE_TEST_CXNSTRING
        if [ -z $_CXNSTRING ]; then 
            echo AZBRIDGE_TEST_CXNSTRING environment variable must be set to valid relay connection string
            exit 
        fi
        
        # start the web server
        server_name=$(docker run -v $(pwd):/tests -d -v $(pwd)/my.cnf:/etc/mysqld/conf.d/my.cnf --rm -d -e AZBRIDGE_TEST_CXNSTRING="$_CXNSTRING" -e MYSQL_ROOT_PASSWORD=PaSsWoRd112233 -e MYSQL_PASSWORD=PaSsWoRd112233 -e MYSQL_USER=mysql azbridge-mysql-server:latest)
        # wait for server to start
        echo waiting 20 seconds
        sleep 20
        # run the client
        docker run -v $(pwd):/tests -v $(pwd)/my.cnf:/home/mysql/.my.cnf --rm -i -e AZBRIDGE_TEST_CXNSTRING="$_CXNSTRING" azbridge-mysql-client:latest bash /tests/run_client.sh
        _RESULT=$?
        # stop the web server
        docker stop $server_name
      
        #diff --strip-trailing-cr -B -i -w downloaded.txt index.html
                
        if [ $_RESULT -eq 0 ]; then 
            echo OK
            exit 0
        else
            echo not ok
            exit 1
        fi
    else 
        if [ "${Operation}" == "clean" ]; then
            if [ ! -z `docker images "azbridge-mysql-server" -q` ]; then
                docker rmi -f azbridge-mysql-server:latest
            fi
            if [ ! -z `docker images "azbridge-mysql-client" -q` ]; then
                docker rmi -f azbridge-mysql-client:latest
            fi
        else
            echo "Unknown command"
        fi
    fi
fi 