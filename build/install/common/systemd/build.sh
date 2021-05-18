#!/bin/bash

BASEDIR="$(cd $(dirname $0) && pwd)"
BUILD_PATH="$BASEDIR/modules"

while [ "$1" != "" ]; do
    case $1 in
	    
        -bp | --buildpath )
        	if [ "$2" != "" ]; then
				    BUILD_PATH=$2
				    shift
			    fi
		;;

        -? | -h | --help )
            echo " Usage: bash build.sh [PARAMETER] [[PARAMETER], ...]"
            echo "    Parameters:"
            echo "      -bp, --buildpath           output path"
            echo "      -?, -h, --help             this help"
            echo "  Examples"
            echo "  bash build.sh -bp /etc/systemd/system/"
            exit 0
    ;;

		* )
			echo "Unknown parameter $1" 1>&2
			exit 1
		;;
    esac
  shift
done

SERVICE_SYSNAME="appserver"
PRODUCT="onlyoffice/${SERVICE_SYSNAME}"
BASE_DIR="/etc/${PRODUCT}"
PATH_TO_CONF="${BASE_DIR}"
STORAGE_ROOT="${BASE_DIR}/data"
LOG_DIR="/var/log/${PRODUCT}"
DOTNET_RUN="/usr/bin/dotnet"
APP_URLS="http://0.0.0.0"
ENVIRONMENT=" --ENVIRONMENT=production"

SERVICE_NAME=(
	api
	api-system
	urlshortener
	thumbnails
	socket
	studio-notify
	notify 
	people-server
	files
	files-services
	studio
	backup
	storage-encryption
	storage-migration
	projects-server
	telegram-service
	crm
	calendar
	mail
	)

reassign_values (){
  case $1 in
	api )
		SERVICE_PORT="5000"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/studio/api/"
		EXEC_FILE="ASC.Web.Api.dll"
	;;
	api-system )
		SERVICE_PORT="5010"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/services/ASC.ApiSystem/"
		EXEC_FILE="ASC.ApiSystem.dll"
	;;
	urlshortener )
		SERVICE_PORT="9999"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/services/ASC.UrlShortener/"
		EXEC_FILE="ASC.UrlShortener.Svc.dll"
	;;
	thumbnails )
		SERVICE_PORT="9800"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/services/ASC.Thumbnails/"
		EXEC_FILE="ASC.Thumbnails.Svc.dll"
	;;
	socket )
		SERVICE_PORT="9899"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/services/ASC.Socket.IO/"
		EXEC_FILE="ASC.Socket.IO.Svc.dll"
	;;
	studio-notify )
		SERVICE_PORT="5006"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/services/ASC.Studio.Notify/"
		EXEC_FILE="ASC.Studio.Notify.dll"
		CORE=" --core:products:folder=/var/www/${SERVICE_SYSNAME}/products --core:products:subfolder=server "
	;;
	notify )
		SERVICE_PORT="5005"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/services/ASC.Notify/"
		EXEC_FILE="ASC.Notify.dll"
		CORE=" --core:products:folder=/var/www/${SERVICE_SYSNAME}/products --core:products:subfolder=server "
	;;
	people-server )
		SERVICE_PORT="5004"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/products/ASC.People/server/"
		EXEC_FILE="ASC.People.dll"
	;;
	files )
		SERVICE_PORT="5007"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/products/ASC.Files/server/"
		EXEC_FILE="ASC.Files.dll"
	;;
	files-services )
		SERVICE_PORT="5009"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/products/ASC.Files/service/"
		EXEC_FILE="ASC.Files.Service.dll"
		CORE=" --core:products:folder=/var/www/${SERVICE_SYSNAME}/products --core:products:subfolder=server"
	;;
	studio )
		SERVICE_PORT="5003"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/studio/server/"
		EXEC_FILE="ASC.Web.Studio.dll"
	;;
	backup )
		SERVICE_PORT="5012"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/services/ASC.Data.Backup/"
		EXEC_FILE="ASC.Data.Backup.dll"
		CORE=" --core:products:folder=/var/www/${SERVICE_SYSNAME}/products --core:products:subfolder=server"
	;;
	storage-migration )
		SERVICE_PORT="5018"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/services/ASC.Data.Storage.Migration/"
		EXEC_FILE="ASC.Data.Storage.Migration.dll"
	;;
	storage-encryption )
		SERVICE_PORT="5019"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/services/ASC.Data.Storage.Encryption/"
		EXEC_FILE="ASC.Data.Storage.Encryption.dll"
	;;
	projects-server )
		SERVICE_PORT="5020"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/products/ASC.Projects/server/"
		EXEC_FILE="ASC.Projects.dll"
	;;
	telegram-service )
		SERVICE_PORT="51702"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/services/ASC.TelegramService/"
		EXEC_FILE="ASC.TelegramService.dll"
	;;
	crm )
		SERVICE_PORT="5021"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/products/ASC.CRM/server/"
		EXEC_FILE="ASC.CRM.dll"
	;;
	calendar )
		SERVICE_PORT="5023"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/products/ASC.Calendar/server/"
		EXEC_FILE="ASC.Calendar.dll"
	;;
	mail )
		SERVICE_PORT="5022"
		WORK_DIR="/var/www/${SERVICE_SYSNAME}/products/ASC.Mail/server/"
		EXEC_FILE="ASC.Mail.dll"
	;;
  esac
  SERVICE_NAME="$1"
  EXEC_START="${DOTNET_RUN} ${WORK_DIR}${EXEC_FILE} --urls=${APP_URLS}:${SERVICE_PORT} --pathToConf=${PATH_TO_CONF} \
  --'\$STORAGE_ROOT'=${STORAGE_ROOT} --log:dir=${LOG_DIR} --log:name=${SERVICE_NAME}${CORE}${ENVIRONMENT}"
  CORE=""
}

write_to_file () {
  sed -i -e 's#${SERVICE_NAME}#'$SERVICE_NAME'#g' -e 's#${WORK_DIR}#'$WORK_DIR'#g' -e \
  "s#\${EXEC_START}#$EXEC_START#g" $BUILD_PATH/${SERVICE_SYSNAME}-${SERVICE_NAME[$i]}.service
}

mkdir -p $BUILD_PATH

for i in ${!SERVICE_NAME[@]}; do
  cp $BASEDIR/service $BUILD_PATH/${SERVICE_SYSNAME}-${SERVICE_NAME[$i]}.service
  reassign_values "${SERVICE_NAME[$i]}"
  write_to_file $i
done
