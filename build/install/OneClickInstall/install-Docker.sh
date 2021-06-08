#!/bin/bash

# (c) Copyright Ascensio System Limited 2010-2016
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and limitations under the License.
# You can contact Ascensio System SIA by email at sales@onlyoffice.com

PRODUCT="onlyoffice"
BASE_DIR="/app/$PRODUCT";
ENV=""

NETWORK=${PRODUCT}

DISK_REQUIREMENTS=40960;
MEMORY_REQUIREMENTS=5500;
CORE_REQUIREMENTS=2;

DIST="";
REV="";
KERNEL="";

INSTALL_MYSQL_SERVER="true";
INSTALL_DOCUMENT_SERVER="true";
INSTALL_APPSERVER="true";
UPDATE="false";

HUB="";
USERNAME="";
PASSWORD="";

MYSQL_DATABASE=""
MYSQL_USER=""
MYSQL_PASSWORD=""
MYSQL_ROOT_PASSWORD=""
MYSQL_HOST=""

ZOO_PORT=""
ZOO_HOST=""
KAFKA_HOST=""

ELK_HOST=""

DOCUMENT_SERVER_JWT_SECRET=""
DOCUMENT_SERVER_HOST=""

APP_CORE_BASE_DOMAIN=""
APP_CORE_MACHINEKEY=""

HELP_TARGET="install-Docker.sh";

SKIP_HARDWARE_CHECK="false";

EXTERNAL_PORT="8092"
SERVICE_PORT="5050"

while [ "$1" != "" ]; do
	case $1 in

		-u | --update )
			if [ "$2" != "" ]; then
				UPDATE=$2
				shift
			fi
		;;

		-hub | --hub )
			if [ "$2" != "" ]; then
				HUB=$2
				shift
			fi
		;;

		-un | --username )
			if [ "$2" != "" ]; then
				USERNAME=$2
				shift
			fi
		;;

		-p | --password )
			if [ "$2" != "" ]; then
				PASSWORD=$2
				shift
			fi
		;;

		-ias | --installappserver )
			if [ "$2" != "" ]; then
				INSTALL_APPSERVER=$2
				shift
			fi
		;;

		-ids | --installdocumentserver )
			if [ "$2" != "" ]; then
				INSTALL_DOCUMENT_SERVER=$2
				shift
			fi
		;;

		-imysql | --installmysql )
			if [ "$2" != "" ]; then
				INSTALL_MYSQL_SERVER=$2
				shift
			fi
		;;

		-ht | --helptarget )
			if [ "$2" != "" ]; then
				HELP_TARGET=$2
				shift
			fi
		;;

		-mysqld | --mysqldatabase )
			if [ "$2" != "" ]; then
				MYSQL_DATABASE=$2
				shift
			fi
		;;

		-mysqlrp | --mysqlrootpassword )
			if [ "$2" != "" ]; then
				MYSQL_ROOT_PASSWORD=$2
				shift
			fi
		;;

		-mysqlu | --mysqluser )
			if [ "$2" != "" ]; then
				MYSQL_USER=$2
				shift
			fi
		;;

		-mysqlh | --mysqlhost )
			if [ "$2" != "" ]; then
				MYSQL_HOST=$2
				shift
			fi
		;;

		-mysqlp | --mysqlpassword )
			if [ "$2" != "" ]; then
				MYSQL_PASSWORD=$2
				shift
			fi
		;;

		-zp | --zookeeperport )
			if [ "$2" != "" ]; then
				ZOO_PORT=$2
				shift
			fi
		;;

		-zh | --zookeeperhost )
			if [ "$2" != "" ]; then
				ZOO_HOST=$2
				shift
			fi
		;;

		-kh | --kafkahost )
			if [ "$2" != "" ]; then
				KAFKA_HOST=$2
				shift
			fi
		;;

		-esh | --elasticsearchhost )
			if [ "$2" != "" ]; then
				ELK_HOST=$2
				shift
			fi
		;;

		-skiphc | --skiphardwarecheck )
			if [ "$2" != "" ]; then
				SKIP_HARDWARE_CHECK=$2
				shift
			fi
		;;

		-ip | --internalport )
			if [ "$2" != "" ]; then
				SERVICE_PORT=$2
				shift
			fi
		;;

		-ep | --externalport )
			if [ "$2" != "" ]; then
				EXTERNAL_PORT=$2
				shift
			fi
		;;

		-ash | --appserverhost )
			if [ "$2" != "" ]; then
				APP_CORE_BASE_DOMAIN=$2
				shift
			fi
		;;
		
		-mk | --machinekey )
			if [ "$2" != "" ]; then
				APP_CORE_MACHINEKEY=$2
				shift
			fi
		;;
		
		-env )
			if [ "$2" != "" ]; then
				ENV=$2
				shift
			fi
		;;

		-? | -h | --help )
			echo "  Usage: bash $HELP_TARGET [PARAMETER] [[PARAMETER], ...]"
			echo
			echo "    Parameters:"
			echo "      -hub, --hub                       dockerhub name"
			echo "      -un, --username                   dockerhub username"
			echo "      -p, --password                    dockerhub password"
			echo "      -ias, --installappserver          install or update appserver (true|false)"
			echo "      -ids, --installdocumentserver     install or update document server (true|false)"
			echo "      -imysql, --installmysql           install or update mysql (true|false)"
			echo "      -mysqlrp, --mysqlrootpassword     mysql server root password"
			echo "      -mysqld, --mysqldatabase          appserver database name"
			echo "      -mysqlu, --mysqluser              appserver database user"
			echo "      -mysqlp, --mysqlpassword          appserver database password"
			echo "      -mysqlh, --mysqlhost              mysql server host"
			echo "      -ash, --appserverhost             appserver host"
			echo "      -zp, --zookeeperport              zookeeper port (default value 2181)"
			echo "      -zh, --zookeeperhost              zookeeper host"
			echo "      -kh, --kafkahost                  kafka host"
			echo "      -esh, --elasticsearchhost         elasticsearch host"
			echo "      -skiphc, --skiphardwarecheck      skip hardware check (true|false)"
			echo "      -ip, --internalport               internal appserver port (default value 5050)"
			echo "      -ep, --externalport               external appserver port (default value 8092)"
			echo "      -mk, --machinekey                 setting for core.machinekey"
			echo "      -?, -h, --help                    this help"
			echo
			echo "    Install all the components without document server:"
			echo "      bash $HELP_TARGET -ids false"
			echo
			echo "    Install Document Server only. Skip the installation of MYSQL and Appserver:"
			echo "      bash $HELP_TARGET -ias false -ids true -imysql false -ims false"
			echo "    Update all installed components. Stop the containers that need to be updated, remove them and run the latest versions of the corresponding components. The portal data should be picked up automatically:"
			echo "      bash $HELP_TARGET -u true"
			echo
			echo "    Update Document Server only to version 4.4.2.20 and skip the update for all other components:"
			echo "      bash $HELP_TARGET -u true -dv 4.4.2.20 -ias false"
			echo
			echo "    Update Appserver only to version 0.1.10 and skip the update for all other components:"
			echo "      bash $HELP_TARGET -u true -av 9.1.0.393 -ids false"
			echo
			exit 0
		;;

		* )
			echo "Unknown parameter $1" 1>&2
			exit 1
		;;
	esac
	shift
done

root_checking () {
	if [ ! $( id -u ) -eq 0 ]; then
		echo "To perform this action you must be logged in with root rights"
		exit 1;
	fi
}

command_exists () {
    type "$1" &> /dev/null;
}

file_exists () {
	if [ -z "$1" ]; then
		echo "file path is empty"
		exit 1;
	fi

	if [ -f "$1" ]; then
		return 0; #true
	else
		return 1; #false
	fi
}

to_lowercase () {
	echo "$1" | awk '{print tolower($0)}'
}

trim () {
	echo -e "$1" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//'
}

get_random_str () {
	LENGTH=$1;

	if [[ -z ${LENGTH} ]]; then
		LENGTH=12;
	fi

	VALUE=$(cat /dev/urandom | tr -dc A-Za-z0-9 | head -c ${LENGTH});
	echo "$VALUE"
}

get_os_info () {
	OS=`to_lowercase \`uname\``

	if [ "${OS}" == "windowsnt" ]; then
		echo "Not supported OS";
		exit 1;
	elif [ "${OS}" == "darwin" ]; then
		echo "Not supported OS";
		exit 1;
	else
		OS=`uname`

		if [ "${OS}" == "SunOS" ] ; then
			echo "Not supported OS";
			exit 1;
		elif [ "${OS}" == "AIX" ] ; then
			echo "Not supported OS";
			exit 1;
		elif [ "${OS}" == "Linux" ] ; then
			MACH=`uname -m`

			if [ "${MACH}" != "x86_64" ]; then
				echo "Currently only supports 64bit OS's";
				exit 1;
			fi

			KERNEL=`uname -r`

			if [ -f /etc/redhat-release ] ; then
				CONTAINS=$(cat /etc/redhat-release | { grep -sw release || true; });
				if [[ -n ${CONTAINS} ]]; then
					DIST=`cat /etc/redhat-release |sed s/\ release.*//`
					REV=`cat /etc/redhat-release | sed s/.*release\ // | sed s/\ .*//`
				else
					DIST=`cat /etc/os-release | grep -sw 'ID' | awk -F=  '{ print $2 }' | sed -e 's/^"//' -e 's/"$//'`
					REV=`cat /etc/os-release | grep -sw 'VERSION_ID' | awk -F=  '{ print $2 }' | sed -e 's/^"//' -e 's/"$//'`
				fi
			elif [ -f /etc/SuSE-release ] ; then
				REV=`cat /etc/os-release  | grep '^VERSION_ID' | awk -F=  '{ print $2 }' |  sed -e 's/^"//'  -e 's/"$//'`
				DIST='SuSe'
			elif [ -f /etc/debian_version ] ; then
				REV=`cat /etc/debian_version`
				DIST='Debian'
				if [ -f /etc/lsb-release ] ; then
					DIST=`cat /etc/lsb-release | grep '^DISTRIB_ID' | awk -F=  '{ print $2 }'`
					REV=`cat /etc/lsb-release | grep '^DISTRIB_RELEASE' | awk -F=  '{ print $2 }'`
				elif [ -f /etc/lsb_release ] || [ -f /usr/bin/lsb_release ] ; then
					DIST=`lsb_release -a 2>&1 | grep 'Distributor ID:' | awk -F ":" '{print $2 }'`
					REV=`lsb_release -a 2>&1 | grep 'Release:' | awk -F ":" '{print $2 }'`
				fi
			elif [ -f /etc/os-release ] ; then
				DIST=`cat /etc/os-release | grep -sw 'ID' | awk -F=  '{ print $2 }' | sed -e 's/^"//' -e 's/"$//'`
				REV=`cat /etc/os-release | grep -sw 'VERSION_ID' | awk -F=  '{ print $2 }' | sed -e 's/^"//' -e 's/"$//'`
			fi
		fi

		DIST=$(trim $DIST);
		REV=$(trim $REV);
	fi
}

check_os_info () {
	if [[ -z ${KERNEL} || -z ${DIST} || -z ${REV} ]]; then
		echo "$KERNEL, $DIST, $REV";
		echo "Not supported OS";
		exit 1;
	fi
}

check_kernel () {
	MIN_NUM_ARR=(3 10 0);
	CUR_NUM_ARR=();

	CUR_STR_ARR=$(echo $KERNEL | grep -Po "[0-9]+\.[0-9]+\.[0-9]+" | tr "." " ");
	for CUR_STR_ITEM in $CUR_STR_ARR
	do
		CUR_NUM_ARR=(${CUR_NUM_ARR[@]} $CUR_STR_ITEM)
	done

	INDEX=0;

	while [[ $INDEX -lt 3 ]]; do
		if [ ${CUR_NUM_ARR[INDEX]} -lt ${MIN_NUM_ARR[INDEX]} ]; then
			echo "Not supported OS Kernel"
			exit 1;
		elif [ ${CUR_NUM_ARR[INDEX]} -gt ${MIN_NUM_ARR[INDEX]} ]; then
			INDEX=3
		fi
		(( INDEX++ ))
	done
}

check_hardware () {
	AVAILABLE_DISK_SPACE=$(df -m /  | tail -1 | awk '{ print $4 }');

	if [ ${AVAILABLE_DISK_SPACE} -lt ${DISK_REQUIREMENTS} ]; then
		echo "Minimal requirements are not met: need at least $DISK_REQUIREMENTS MB of free HDD space"
		exit 1;
	fi

	TOTAL_MEMORY=$(free -m | grep -oP '\d+' | head -n 1);

	if [ ${TOTAL_MEMORY} -lt ${MEMORY_REQUIREMENTS} ]; then
		echo "Minimal requirements are not met: need at least $MEMORY_REQUIREMENTS MB of RAM"
		exit 1;
	fi

	CPU_CORES_NUMBER=$(cat /proc/cpuinfo | grep processor | wc -l);

	if [ ${CPU_CORES_NUMBER} -lt ${CORE_REQUIREMENTS} ]; then
		echo "The system does not meet the minimal hardware requirements. CPU with at least $CORE_REQUIREMENTS cores is required"
		exit 1;
	fi
}

install_service () {
	if command_exists apt-get; then
		apt-get -y update
		apt-get -y -q install $1
	elif command_exists yum; then
		yum -y install $1
	fi

	if ! command_exists $1; then
		echo "command $1 not found"
		exit 1;
	fi
}

install_docker_compose () {
	if ! command_exists python3; then
		if command_exists apt-get; then
			apt-get -y -q install python3
		elif command_exists yum; then
			yum -y install python3
		fi
	fi

	if command_exists pip3; then
		curl -O https://bootstrap.pypa.io/get-pip.py
		python3 get-pip.py || true
		rm get-pip.py
	fi

	python3 -m pip install docker-compose
	sudo ln -s /usr/local/bin/docker-compose /usr/bin/docker-compose

	if ! command_exists docker-compose; then
		echo "command docker-compose not found"
		exit 1;
	fi
}

check_ports () {
	RESERVED_PORTS=(443 2181 2888 3306 3888 8081 8099 9092 9200 9300 9800 9899 9999 33060);
	ARRAY_PORTS=();
	USED_PORTS="";

	if ! command_exists netstat; then
		install_service net-tools
	fi

	if [ "${EXTERNAL_PORT//[0-9]}" = "" ]; then
		for RESERVED_PORT in "${RESERVED_PORTS[@]}"
		do
			if [ "$RESERVED_PORT" -eq "$EXTERNAL_PORT" ] ; then
				echo "External port $EXTERNAL_PORT is reserved. Select another port"
				exit 1;
			fi

			if [ "$RESERVED_PORT" -eq "$SERVICE_PORT" ] ; then
				echo "Internal port $SERVICE_PORT is reserved. Select another port"
				exit 1;
			fi
		done
	else
		echo "Invalid external port $EXTERNAL_PORT"
		exit 1;
	fi

	if [ "$INSTALL_APPSERVER" == "true" ]; then
		ARRAY_PORTS=(${ARRAY_PORTS[@]} "$EXTERNAL_PORT");
	fi

	for PORT in "${ARRAY_PORTS[@]}"
	do
		REGEXP=":$PORT$"
		CHECK_RESULT=$(netstat -lnt | awk '{print $4}' | { grep $REGEXP || true; })

		if [[ $CHECK_RESULT != "" ]]; then
			if [[ $USED_PORTS != "" ]]; then
				USED_PORTS="$USED_PORTS, $PORT"
			else
				USED_PORTS="$PORT"
			fi
		fi
	done

	if [[ $USED_PORTS != "" ]]; then
		echo "The following TCP Ports must be available: $USED_PORTS"
		exit 1;
	fi
}

check_docker_version () {
	CUR_FULL_VERSION=$(docker -v | cut -d ' ' -f3 | cut -d ',' -f1);
	CUR_VERSION=$(echo $CUR_FULL_VERSION | cut -d '-' -f1);
	CUR_EDITION=$(echo $CUR_FULL_VERSION | cut -d '-' -f2);

	if [ "${CUR_EDITION}" == "ce" ] || [ "${CUR_EDITION}" == "ee" ]; then
		return 0;
	fi

	if [ "${CUR_VERSION}" != "${CUR_EDITION}" ]; then
		echo "Unspecific docker version"
		exit 1;
	fi

	MIN_NUM_ARR=(1 10 0);
	CUR_NUM_ARR=();

	CUR_STR_ARR=$(echo $CUR_VERSION | grep -Po "[0-9]+\.[0-9]+\.[0-9]+" | tr "." " ");

	for CUR_STR_ITEM in $CUR_STR_ARR
	do
		CUR_NUM_ARR=(${CUR_NUM_ARR[@]} $CUR_STR_ITEM)
	done

	INDEX=0;

	while [[ $INDEX -lt 3 ]]; do
		if [ ${CUR_NUM_ARR[INDEX]} -lt ${MIN_NUM_ARR[INDEX]} ]; then
			echo "The outdated Docker version has been found. Please update to the latest version."
			exit 1;
		elif [ ${CUR_NUM_ARR[INDEX]} -gt ${MIN_NUM_ARR[INDEX]} ]; then
			return 0;
		fi
		(( INDEX++ ))
	done
}

install_docker_using_script () {
	if ! command_exists curl ; then
		install_service curl
	fi

	curl -fsSL https://get.docker.com -o get-docker.sh
	sh get-docker.sh
	rm get-docker.sh
}

install_docker () {

	if [ "${DIST}" == "Ubuntu" ] || [ "${DIST}" == "Debian" ] || [[ "${DIST}" == CentOS* ]] || [ "${DIST}" == "Fedora" ]; then

		install_docker_using_script
		systemctl start docker
		systemctl enable docker

	elif [ "${DIST}" == "Red Hat Enterprise Linux Server" ]; then

		echo ""
		echo "Your operating system does not allow Docker CE installation."
		echo "You can install Docker EE using the manual here - https://docs.docker.com/engine/installation/linux/rhel/"
		echo ""
		exit 1;

	elif [ "${DIST}" == "SuSe" ]; then

		echo ""
		echo "Your operating system does not allow Docker CE installation."
		echo "You can install Docker EE using the manual here - https://docs.docker.com/engine/installation/linux/suse/"
		echo ""
		exit 1;

	elif [ "${DIST}" == "altlinux" ]; then

		apt-get -y install docker-io
		chkconfig docker on
		service docker start
		systemctl enable docker

	else

		echo ""
		echo "Docker could not be installed automatically."
		echo "Please use this official instruction https://docs.docker.com/engine/installation/linux/other/ for its manual installation."
		echo ""
		exit 1;

	fi

	if ! command_exists docker ; then
		echo "error while installing docker"
		exit 1;
	fi
}

docker_login () {
	if [[ -n ${USERNAME} && -n ${PASSWORD}  ]]; then
		docker login ${HUB} --username ${USERNAME} --password ${PASSWORD}
	fi
}

create_network () {
	EXIST=$(docker network ls | awk '{print $2;}' | { grep -x ${NETWORK} || true; });

	if [[ -z ${EXIST} ]]; then
		docker network create --driver bridge ${NETWORK}
	fi
}

get_container_env_parameter () {
	CONTAINER_NAME=$1;
	PARAMETER_NAME=$2;
	VALUE="";

	if [[ -z ${CONTAINER_NAME} ]]; then
		echo "Empty container name"
		exit 1;
	fi

	if [[ -z ${PARAMETER_NAME} ]]; then
		echo "Empty parameter name"
		exit 1;
	fi

	if command_exists docker ; then
		CONTAINER_EXIST=$(docker ps -aqf "name=$CONTAINER_NAME");

		if [[ -n ${CONTAINER_EXIST} ]]; then
			VALUE=$(docker inspect --format='{{range .Config.Env}}{{println .}}{{end}}' ${CONTAINER_NAME} | grep "${PARAMETER_NAME}=" | sed 's/^.*=//');
		fi
	fi

	echo "$VALUE"
}

set_jwt_secret () {
	CURRENT_JWT_SECRET="";

	if [[ -z ${JWT_SECRET} ]]; then
		CURRENT_JWT_SECRET=$(get_container_env_parameter "${PRODUCT}-document-server" "JWT_SECRET");

		if [[ -n ${CURRENT_JWT_SECRET} ]]; then
			DOCUMENT_SERVER_JWT_SECRET="$CURRENT_JWT_SECRET";
		fi
	fi

	if [[ -z ${JWT_SECRET} ]]; then
		CURRENT_JWT_SECRET=$(get_container_env_parameter "${PRODUCT}-api" "DOCUMENT_SERVER_JWT_SECRET");

		if [[ -n ${CURRENT_JWT_SECRET} ]]; then
			DOCUMENT_SERVER_JWT_SECRET="$CURRENT_JWT_SECRET";
		fi
	fi

	if [[ -z ${JWT_SECRET} ]] && [[ "$UPDATE" != "true" ]]; then
		DOCUMENT_SERVER_JWT_SECRET=$(get_random_str 12);
	fi
}

set_core_machinekey () {
	CURRENT_CORE_MACHINEKEY="";

	if [[ -z ${CORE_MACHINEKEY} ]]; then
		if file_exists ${BASE_DIR}/.private/machinekey; then
			CURRENT_CORE_MACHINEKEY=$(cat ${BASE_DIR}/.private/machinekey);

			if [[ -n ${CURRENT_CORE_MACHINEKEY} ]]; then
				APP_CORE_MACHINEKEY="$CURRENT_CORE_MACHINEKEY";
			fi
		fi
	fi

	if [[ -z ${CORE_MACHINEKEY} ]]; then
		CURRENT_CORE_MACHINEKEY=$(get_container_env_parameter "${PRODUCT}-api" "$APP_CORE_MACHINEKEY");

		if [[ -n ${CURRENT_CORE_MACHINEKEY} ]]; then
			APP_CORE_MACHINEKEY="$CURRENT_CORE_MACHINEKEY";
		fi
	fi

	if [[ -z ${CORE_MACHINEKEY} ]] && [[ "$UPDATE" != "true" ]]; then
		APP_CORE_MACHINEKEY=$(get_random_str 12);
		echo $APP_CORE_MACHINEKEY > ${BASE_DIR}/.private/machinekey
	fi
}

download_files () {
	mkdir -p ${BASE_DIR}
	mkdir -p ${BASE_DIR}/.private/
	mkdir -p ${BASE_DIR}/config/mysql/conf.d/

	if ! command_exists wget; then
		install_service wget
	fi
	
	DOWNLOAD_URL_PREFIX="https://raw.githubusercontent.com/ONLYOFFICE/AppServer/develop/build/install/docker"
	wget -q -O $BASE_DIR/.env "${DOWNLOAD_URL_PREFIX}/.env"
	wget -q -O $BASE_DIR/appserver.yml "${DOWNLOAD_URL_PREFIX}/appserver.yml"
	wget -q -O $BASE_DIR/db.yml "${DOWNLOAD_URL_PREFIX}/db.yml"
	wget -q -O $BASE_DIR/ds.yml "${DOWNLOAD_URL_PREFIX}/ds.yml"
	wget -q -O $BASE_DIR/config/createdb.sql "${DOWNLOAD_URL_PREFIX}/config/createdb.sql"
	wget -q -O $BASE_DIR/config/onlyoffice.sql "${DOWNLOAD_URL_PREFIX}/config/onlyoffice.sql"
	wget -q -O $BASE_DIR/config/onlyoffice.data.sql "${DOWNLOAD_URL_PREFIX}/config/onlyoffice.data.sql"
	wget -q -O $BASE_DIR/config/mysql/conf.d/mysql.cnf "${DOWNLOAD_URL_PREFIX}/config/mysql/conf.d/mysql.cnf"
	wget -q -O $BASE_DIR/config/onlyoffice.resources.sql "${DOWNLOAD_URL_PREFIX}/config/onlyoffice.resources.sql"
	wget -q -O $BASE_DIR/config/onlyoffice.upgradev110.sql "${DOWNLOAD_URL_PREFIX}/config/onlyoffice.upgradev110.sql"
	wget -q -O $BASE_DIR/config/onlyoffice.upgradev111.sql "${DOWNLOAD_URL_PREFIX}/config/onlyoffice.upgradev111.sql"
	wget -q -O $BASE_DIR/config/onlyoffice.upgradev115.sql "${DOWNLOAD_URL_PREFIX}/config/onlyoffice.upgradev115.sql"

	if [[ -n ${ENV} ]]; then
		sed -i "s/STATUS=.*/STATUS=\"${ENV}\"/g" $BASE_DIR/.env
	fi
}

reconfigure () {
	VARIABLE_NAME=$1
	VARIABLE_VALUE=$2

	if [[ -n ${VARIABLE_VALUE} ]]; then
		sed -i "s/${VARIABLE_NAME}=.*/${VARIABLE_NAME}=${VARIABLE_VALUE}/g" $BASE_DIR/.env
	fi
}

install_mysql_server () {
	if ! command_exists docker-compose; then
		install_docker_compose
	fi

	reconfigure MYSQL_DATABASE ${MYSQL_DATABASE}
	reconfigure MYSQL_USER ${MYSQL_USER}
	reconfigure MYSQL_PASSWORD ${MYSQL_PASSWORD}
	reconfigure MYSQL_ROOT_PASSWORD ${MYSQL_ROOT_PASSWORD}
	reconfigure MYSQL_HOST ${MYSQL_HOST}

	docker-compose -f $BASE_DIR/db.yml up -d
}

install_document_server () {
	if ! command_exists docker-compose; then
		install_docker_compose
	fi

	reconfigure DOCUMENT_SERVER_JWT_SECRET ${DOCUMENT_SERVER_JWT_SECRET}
	reconfigure DOCUMENT_SERVER_HOST ${DOCUMENT_SERVER_HOST}

	docker-compose -f $BASE_DIR/ds.yml up -d
}

install_appserver () {
	if ! command_exists docker-compose; then
		install_docker_compose
	fi

	reconfigure ZOO_PORT ${ZOO_PORT}
	reconfigure ZOO_HOST ${ZOO_HOST}
	reconfigure KAFKA_HOST ${KAFKA_HOST}
	reconfigure ELK_HOST ${ELK_HOST}
	reconfigure SERVICE_PORT ${SERVICE_PORT}
	reconfigure APP_CORE_MACHINEKEY ${APP_CORE_MACHINEKEY}
	reconfigure APP_CORE_BASE_DOMAIN ${APP_CORE_BASE_DOMAIN}

	if [[ -n $EXTERNAL_PORT ]]; then
		sed -i "s/8092:8092/${EXTERNAL_PORT}:8092/g" $BASE_DIR/appserver.yml
	fi

	docker-compose -f $BASE_DIR/appserver.yml up -d
}

start_installation () {
	root_checking

	get_os_info

	check_os_info
	
	if [ "$UPDATE" != "true" ]; then
		check_ports
	fi

	if [ "$SKIP_HARDWARE_CHECK" != "true" ]; then
		check_hardware
	fi

	if command_exists docker ; then
		check_docker_version
		service docker start
	else
		install_docker
	fi

	docker_login

	download_files

	set_jwt_secret

	set_core_machinekey

	create_network

	if [ "$INSTALL_MYSQL_SERVER" == "true" ]; then
		install_mysql_server
	fi
	
	if [ "$INSTALL_DOCUMENT_SERVER" == "true" ]; then
		install_document_server
	fi

	if [ "$INSTALL_APPSERVER" == "true" ]; then
		install_appserver
	fi

	echo ""
	echo "Thank you for installing ONLYOFFICE Appserver."
	echo "In case you have any questions contact us via http://support.onlyoffice.com or visit our forum at http://dev.onlyoffice.org"
	echo ""

	exit 0;
}

start_installation