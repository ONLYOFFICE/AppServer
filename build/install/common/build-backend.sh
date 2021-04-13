#!/bin/bash

SRC_PATH="/AppServer"
RID_ID="linux-x64"
ARGS=""

while [ "$1" != "" ]; do
    case $1 in
	    
        -sp | --srcpath )
        	if [ "$2" != "" ]; then
				SRC_PATH=$2
				shift
			fi
		;;

        -ri | --runtime )
          if [ "$2" != "" ]; then
            RID_ID=$2
            shift
          fi
    ;;

        -ar | --arguments )
          if [ "$2" != "" ]; then
            ARGS=$2
            shift
          fi
    ;;

        -? | -h | --help )
            echo " Usage: bash build-backend.sh [PARAMETER] [[PARAMETER], ...]"
            echo "    Parameters:"
            echo "      -sp, --srcpath             path to AppServer root directory"
            echo "      -ri, --runtime             RID ids for .NET runtime publish (by default=linux-x64)"
            echo "      -ar, --arguments           additional arguments publish the .NET runtime with your application"
            echo "      -?, -h, --help             this help"
            echo "  Examples"
            echo "  bash build-backend.sh -sp /app/AppServer"
            exit 0
        ;;

		* )
			echo "Unknown parameter $1" 1>&2
			exit 1
		;;
    esac
  shift
done
	
echo "== BACK-END-BUILD =="

cd ${SRC_PATH}
dotnet restore ASC.Web.sln --configfile .nuget/NuGet.Config ${ARGS}
dotnet build ASC.Web.sln -r ${RID_ID} ${ARGS} 

echo "== Build ASC.Thumbnails =="
yarn install --cwd common/ASC.Thumbnails --frozen-lockfile

echo "== Build ASC.UrlShortener =="
yarn install --cwd common/ASC.UrlShortener --frozen-lockfile

echo "== Build ASC.Socket.IO =="
yarn install --cwd common/ASC.Socket.IO --frozen-lockfile
