#!/bin/bash

set -e

cat<<EOF

#######################################
#  INSTALL APP 
#######################################

EOF
apt-get -y update

if [ "$COMMUNITY_SERVER_INSTALLED" = "true" ]; then	
	ENVIRONMENT="$(cat /lib/systemd/system/${product}-api.service | grep -oP 'ENVIRONMENT=\K.*')"
	USER_CONNECTIONSTRING=$(json -f /etc/onlyoffice/${product}/appsettings.$ENVIRONMENT.json ConnectionStrings.default.connectionString)
	MYSQL_SERVER_HOST=$(echo $USER_CONNECTIONSTRING | grep -oP 'Server=\K.*' | grep -o '^[^;]*')
	MYSQL_SERVER_DB_NAME=$(echo $USER_CONNECTIONSTRING | grep -oP 'Database=\K.*' | grep -o '^[^;]*')
	MYSQL_SERVER_USER=$(echo $USER_CONNECTIONSTRING | grep -oP 'User ID=\K.*' | grep -o '^[^;]*')
	MYSQL_SERVER_PORT=$(echo $USER_CONNECTIONSTRING | grep -oP 'Port=\K.*' | grep -o '^[^;]*')
	MYSQL_ROOT_PASS=$(echo $USER_CONNECTIONSTRING | grep -oP 'Password=\K.*' | grep -o '^[^;]*')
fi

if [ "$DOCUMENT_SERVER_INSTALLED" = "false" ]; then
	DS_PORT=${DS_PORT:-8083};

	DS_DB_HOST=localhost;
	DS_DB_NAME=$DS_COMMON_NAME;
	DS_DB_USER=$DS_COMMON_NAME;
	DS_DB_PWD=$DS_COMMON_NAME;

	DS_JWT_ENABLED=${DS_JWT_ENABLED:-true};
	DS_JWT_SECRET="$(cat /dev/urandom | tr -dc A-Za-z0-9 | head -c 12)";
	DS_JWT_HEADER="AuthorizationJwt";
	
	if ! su - postgres -s /bin/bash -c "psql -lqt" | cut -d \| -f 1 | grep -q ${DS_DB_NAME}; then
		su - postgres -s /bin/bash -c "psql -c \"CREATE DATABASE ${DS_DB_NAME};\""
		su - postgres -s /bin/bash -c "psql -c \"CREATE USER ${DS_DB_USER} WITH password '${DS_DB_PWD}';\""
		su - postgres -s /bin/bash -c "psql -c \"GRANT ALL privileges ON DATABASE ${DS_DB_NAME} TO ${DS_DB_USER};\""
	fi

	echo ${package_sysname}-documentserver $DS_COMMON_NAME/ds-port select $DS_PORT | sudo debconf-set-selections
	echo ${package_sysname}-documentserver $DS_COMMON_NAME/db-pwd select $DS_DB_PWD | sudo debconf-set-selections
	echo ${package_sysname}-documentserver $DS_COMMON_NAME/db-user $DS_DB_USER | sudo debconf-set-selections
	echo ${package_sysname}-documentserver $DS_COMMON_NAME/db-name $DS_DB_NAME | sudo debconf-set-selections
	echo ${package_sysname}-documentserver-de $DS_COMMON_NAME/jwt-enabled select ${DS_JWT_ENABLED} | sudo debconf-set-selections
	echo ${package_sysname}-documentserver-de $DS_COMMON_NAME/jwt-secret select ${DS_JWT_SECRET} | sudo debconf-set-selections
	echo ${package_sysname}-documentserver-de $DS_COMMON_NAME/jwt-header select ${DS_JWT_HEADER} | sudo debconf-set-selections
	echo ${package_sysname}-documentserver-ee $DS_COMMON_NAME/jwt-enabled select ${DS_JWT_ENABLED} | sudo debconf-set-selections
	echo ${package_sysname}-documentserver-ee $DS_COMMON_NAME/jwt-secret select ${DS_JWT_SECRET} | sudo debconf-set-selections
	echo ${package_sysname}-documentserver-ee $DS_COMMON_NAME/jwt-header select ${DS_JWT_HEADER} | sudo debconf-set-selections
	
	apt-get install -yq ${package_sysname}-documentserver
fi
NGINX_ROOT_DIR="/etc/nginx"

NGINX_WORKER_PROCESSES=${NGINX_WORKER_PROCESSES:-$(grep processor /proc/cpuinfo | wc -l)};
NGINX_WORKER_CONNECTIONS=${NGINX_WORKER_CONNECTIONS:-$(ulimit -n)};

sed 's/^worker_processes.*/'"worker_processes ${NGINX_WORKER_PROCESSES};"'/' -i ${NGINX_ROOT_DIR}/nginx.conf
sed 's/worker_connections.*/'"worker_connections ${NGINX_WORKER_CONNECTIONS};"'/' -i ${NGINX_ROOT_DIR}/nginx.conf

if ! id "nginx" &>/dev/null; then
	systemctl stop nginx

	rm -dfr /var/log/nginx/*
	rm -dfr /var/cache/nginx/*
	useradd -s /bin/false nginx

	systemctl start nginx
else
	systemctl reload nginx
fi

if [ "$APPSERVER_INSTALLED" = "false" ]; then

	apt-get install -yq appserver

expect << EOF
	set timeout -1
	log_user 1

	spawn /usr/bin/appserver-configuration.sh
	expect -re "Database host:"
	send "\025$MYSQL_SERVER_HOST\r"

	expect -re "Database name:"
	send "\025$MYSQL_SERVER_DB_NAME\r"

	expect -re "Database user:"
	send "\025$MYSQL_SERVER_USER\r"

	expect -re "Database password:"
	send "\025$MYSQL_SERVER_PASS\r"

	expect eof	
EOF
fi

echo ""
echo "$RES_INSTALL_SUCCESS"
echo "$RES_QUESTIONS"
echo ""
