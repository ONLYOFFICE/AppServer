#!/bin/bash

SRC_PATH="/AppServer"
BUILD_PATH=""

while [ "$1" != "" ]; do
    case $1 in
	    
        -sp | --srcpath )
        	if [ "$2" != "" ]; then
				SRC_PATH=$2
				shift
			fi
		;;

        -? | -h | --help )
            echo " Usage: bash publish-backend.sh [PARAMETER] [[PARAMETER], ...]"
            echo "    Parameters:"
            echo "      -sp, --srcpath             path to AppServer root directory"
            echo "      -?, -h, --help             this help"
            echo "  Examples"
            echo "  bash publish-backend.sh -sp /app/AppServer"
            exit 0
        ;;

		* )
			echo "Unknown parameter $1" 1>&2
			exit 1
		;;
    esac
  shift
done

echo "== Publish ASC.ApiSystem.csproj project =="
cd ${SRC_PATH}/common/services/ASC.ApiSystem
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/services/apisystem && echo -e "Done"

echo "== Publish ASC.Data.Backup.csproj project =="
cd ${SRC_PATH}/common/services/ASC.Data.Backup
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/services/backup && echo -e "Done"

echo "== Publish ASC.CRM.csproj project =="
cd ${SRC_PATH}/products/ASC.CRM/Server
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/products/ASC.CRM/server && echo -e "Done"

echo "== Publish ASC.Data.Storage.Encryption.csproj project =="
cd ${SRC_PATH}/common/services/ASC.Data.Storage.Encryption
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/services/storage.encryption && echo -e "Done"

echo "== Publish ASC.Files.csproj project =="
cd ${SRC_PATH}/products/ASC.Files/Server
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/products/ASC.Files/server
cp -avrf DocStore ${BUILD_PATH}/var/www/products/ASC.Files/server/ && echo -e "Done"

echo "== Publish ASC.Files.Service.csproj project =="
cd ${SRC_PATH}/products/ASC.Files/Service
dotnet add ASC.Files.Service.csproj reference ${SRC_PATH}/products/ASC.People/Server/ASC.People.csproj  ${SRC_PATH}/products/ASC.Files/Server/ASC.Files.csproj
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/products/ASC.Files/service && echo -e "Done"

echo "== Publish ASC.Data.Storage.Migration.csproj project =="
cd ${SRC_PATH}/common/services/ASC.Data.Storage.Migration
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/services/storage.migration && echo -e "Done"

echo "== Publish ASC.Notify.csproj project =="
cd ${SRC_PATH}/common/services/ASC.Notify
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/services/notify && echo -e "Done"

echo "== Publish ASC.People.csproj project =="
cd ${SRC_PATH}/products/ASC.People/Server
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/products/ASC.People/server && echo -e "Done"

echo "== Publish ASC.Projects.csproj project =="
cd ${SRC_PATH}/products/ASC.Projects/Server
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/products/ASC.Projects/server && echo -e "Done"

echo "== Publish ASC.Socket.csproj project =="
cd ${SRC_PATH}/common/services/ASC.Socket.IO.Svc
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/services/socket/service && echo -e "Done"

echo "== Publish ASC.Studio.Notify.csproj project =="
cd ${SRC_PATH}/common/services/ASC.Studio.Notify
dotnet add ASC.Studio.Notify.csproj reference ${SRC_PATH}/products/ASC.People/Server/ASC.People.csproj  ${SRC_PATH}/products/ASC.Files/Server/ASC.Files.csproj
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/services/studio.notify && echo -e "Done"

echo "== Publish ASC.TelegramService.csproj project =="
cd ${SRC_PATH}/common/services/ASC.TelegramService
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/services/telegram/service && echo -e "Done"

echo "== Publish ASC.Thumbnails.Svc.csproj project =="
cd ${SRC_PATH}/common/services/ASC.Thumbnails.Svc
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/services/thumb/service && echo -e "Done"

echo "== Publish ASC.UrlShortener.Svc.csproj project =="
cd ${SRC_PATH}/common/services/ASC.UrlShortener.Svc
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/services/urlshortener/service && echo -e "Done"

echo "== Publish ASC.Web.Api.csproj project =="
cd ${SRC_PATH}/web/ASC.Web.Api
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/studio/api && echo -e "Done"

echo "== Publish ASC.Web.Studio.csproj project =="
cd ${SRC_PATH}/web/ASC.Web.Studio
dotnet -d publish --no-build --self-contained -r linux-x64 -o ${BUILD_PATH}/var/www/studio/server && echo -e "Done"

echo "== Publish ASC.Thumbnails =="
cd ${SRC_PATH}/common/ASC.Thumbnails/
yarn build 
mkdir -p ${BUILD_PATH}/var/www/services/thumb/client && cp -avrf ./* ${BUILD_PATH}/var/www/services/thumb/client && echo -e "Done"

echo "== Publish Build ASC.UrlShortener =="
cd ${SRC_PATH}/common/ASC.UrlShortener
mkdir -p ${BUILD_PATH}/var/www/services/urlshortener/client && cp -avrf ./* ${BUILD_PATH}/var/www/services/urlshortener/client && echo -e "Done"

echo "== Publish ASC.Socket.IO =="
cd ${SRC_PATH}/common/ASC.Socket.IO
mkdir -p ${BUILD_PATH}/var/www/services/socket/client && cp -avrf ./* ${BUILD_PATH}/var/www/services/socket/client && echo -e "Done"
