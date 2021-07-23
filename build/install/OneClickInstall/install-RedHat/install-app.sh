#!/bin/bash

set -e

cat<<EOF

#######################################
#  INSTALL APP 
#######################################

EOF

if [ -e /etc/redis.conf ]; then
 sed -i "s/bind .*/bind 127.0.0.1/g" /etc/redis.conf
 sed -r "/^save\s[0-9]+/d" -i /etc/redis.conf
 
 systemctl restart redis
fi

sed "/host\s*all\s*all\s*127\.0\.0\.1\/32\s*ident$/s|ident$|trust|" -i /var/lib/pgsql/data/pg_hba.conf
sed "/host\s*all\s*all\s*::1\/128\s*ident$/s|ident$|trust|" -i /var/lib/pgsql/data/pg_hba.conf

for SVC in $package_services; do
		systemctl start $SVC	
		systemctl enable $SVC
done

MYSQL_SERVER_HOST=${MYSQL_SERVER_HOST:-"localhost"}
MYSQL_SERVER_DB_NAME=${MYSQL_SERVER_DB_NAME:-"${package_sysname}"}
MYSQL_SERVER_USER=${MYSQL_SERVER_USER:-"root"}
MYSQL_SERVER_PORT=${MYSQL_SERVER_PORT:-3306}

if [ "${MYSQL_FIRST_TIME_INSTALL}" = "true" ]; then
	MYSQL_TEMPORARY_ROOT_PASS="";

	if [ -f "/var/log/mysqld.log" ]; then
		MYSQL_TEMPORARY_ROOT_PASS=$(cat /var/log/mysqld.log | grep "temporary password" | rev | cut -d " " -f 1 | rev | tail -1);
	fi

	while ! mysqladmin ping -u root --silent; do
		sleep 1
	done

	if ! mysql "-u$MYSQL_SERVER_USER" "-p$MYSQL_TEMPORARY_ROOT_PASS" -e ";" >/dev/null 2>&1; then
		if [ -z $MYSQL_TEMPORARY_ROOT_PASS ]; then
		   MYSQL="mysql --connect-expired-password -u$MYSQL_SERVER_USER -D mysql";
		else
		   MYSQL="mysql --connect-expired-password -u$MYSQL_SERVER_USER -p${MYSQL_TEMPORARY_ROOT_PASS} -D mysql";
		   MYSQL_ROOT_PASS=$(echo $MYSQL_TEMPORARY_ROOT_PASS | sed -e 's/;/%/g' -e 's/=/%/g');
		fi

		$MYSQL -e "ALTER USER '${MYSQL_SERVER_USER}'@'localhost' IDENTIFIED WITH mysql_native_password BY '${MYSQL_ROOT_PASS}'" >/dev/null 2>&1 \
		|| $MYSQL -e "UPDATE user SET plugin='mysql_native_password', authentication_string=PASSWORD('${MYSQL_ROOT_PASS}') WHERE user='${MYSQL_SERVER_USER}' and host='localhost';"		

		systemctl restart mysqld
	fi
fi

if [ "${UPDATE}" = "true" ] && [ "${$APPSERVER_INSTALLED}" = "true" ]; then
	ENVIRONMENT="$(cat /lib/systemd/system/${product}-api.service | grep -oP 'ENVIRONMENT=\K.*')"
	USER_CONNECTIONSTRING=$(json -f /etc/onlyoffice/${product}/appsettings.$ENVIRONMENT.json ConnectionStrings.default.connectionString)
	MYSQL_SERVER_HOST=$(echo $USER_CONNECTIONSTRING | grep -oP 'Server=\K.*' | grep -o '^[^;]*')
	MYSQL_SERVER_DB_NAME=$(echo $USER_CONNECTIONSTRING | grep -oP 'Database=\K.*' | grep -o '^[^;]*')
	MYSQL_SERVER_USER=$(echo $USER_CONNECTIONSTRING | grep -oP 'User ID=\K.*' | grep -o '^[^;]*')
	MYSQL_SERVER_PORT=$(echo $USER_CONNECTIONSTRING | grep -oP 'Port=\K.*' | grep -o '^[^;]*')
	MYSQL_ROOT_PASS=$(echo $USER_CONNECTIONSTRING | grep -oP 'Password=\K.*' | grep -o '^[^;]*')
fi

if [ "$DOCUMENT_SERVER_INSTALLED" = "false" ]; then
	declare -x DS_PORT=8083

	DS_RABBITMQ_HOST=localhost;
	DS_RABBITMQ_USER=guest;
	DS_RABBITMQ_PWD=guest;
	
	DS_REDIS_HOST=localhost;
	
	DS_COMMON_NAME=${DS_COMMON_NAME:-"ds"};

	DS_DB_HOST=localhost;
	DS_DB_NAME=$DS_COMMON_NAME;
	DS_DB_USER=$DS_COMMON_NAME;
	DS_DB_PWD=$DS_COMMON_NAME;
	
	declare -x JWT_ENABLED=true;
	declare -x JWT_SECRET="$(cat /dev/urandom | tr -dc A-Za-z0-9 | head -c 12)";
	declare -x JWT_HEADER="AuthorizationJwt";
		
	if ! su - postgres -s /bin/bash -c "psql -lqt" | cut -d \| -f 1 | grep -q ${DS_DB_NAME}; then
		su - postgres -s /bin/bash -c "psql -c \"CREATE DATABASE ${DS_DB_NAME};\""
		su - postgres -s /bin/bash -c "psql -c \"CREATE USER ${DS_DB_USER} WITH password '${DS_DB_PWD}';\""
		su - postgres -s /bin/bash -c "psql -c \"GRANT ALL privileges ON DATABASE ${DS_DB_NAME} TO ${DS_DB_USER};\""
	fi
	
	${package_manager} -y install ${package_sysname}-documentserver

	systemctl restart supervisord
	
expect << EOF
	
	set timeout -1
	log_user 1
	
	spawn documentserver-configure.sh
	
	expect "Configuring database access..."
	
	expect -re "Host"
	send "\025$DS_DB_HOST\r"
	
	expect -re "Database name"
	send "\025$DS_DB_NAME\r"
	
	expect -re "User"
	send "\025$DS_DB_USER\r"
	
	expect -re "Password"
	send "\025$DS_DB_PWD\r"
	
	if { "${INSTALLATION_TYPE}" == "ENTERPRISE" || "${INSTALLATION_TYPE}" == "DEVELOPER" } {
		expect "Configuring redis access..."
		send "\025$DS_REDIS_HOST\r"
	}
	
	expect "Configuring AMQP access... "
	expect -re "Host"
	send "\025$DS_RABBITMQ_HOST\r"
	
	expect -re "User"
	send "\025$DS_RABBITMQ_USER\r"
	
	expect -re "Password"
	send "\025$DS_RABBITMQ_PWD\r"
	
	expect eof
	
EOF
	DOCUMENT_SERVER_INSTALLED="true";
elif [ "$UPDATE" = "true" ] && [ "$DOCUMENT_SERVER_INSTALLED" = "true" ]; then
	${package_manager} -y update ${package_sysname}-documentserver
fi

NGINX_ROOT_DIR="/etc/nginx"

NGINX_WORKER_PROCESSES=${NGINX_WORKER_PROCESSES:-$(grep processor /proc/cpuinfo | wc -l)};
NGINX_WORKER_CONNECTIONS=${NGINX_WORKER_CONNECTIONS:-$(ulimit -n)};

sed 's/^worker_processes.*/'"worker_processes ${NGINX_WORKER_PROCESSES};"'/' -i ${NGINX_ROOT_DIR}/nginx.conf
sed 's/worker_connections.*/'"worker_connections ${NGINX_WORKER_CONNECTIONS};"'/' -i ${NGINX_ROOT_DIR}/nginx.conf

if rpm -q "firewalld"; then
	firewall-cmd --permanent --zone=public --add-service=http
	firewall-cmd --permanent --zone=public --add-service=https
	systemctl restart firewalld.service
fi

if [ "$APPSERVER_INSTALLED" = "false" ] || [ "$UPDATE" = "true" ]; then
	if [ "$APPSERVER_INSTALLED" = "false" ]; then
		${package_manager} install -y ${package_sysname}-${product} 
	else
		${package_manager} -y update ${package_sysname}-${product}
	fi

	if [ "${MYSQL_FIRST_TIME_INSTALL}" = "true" ] || [ "$UPDATE" = "true" ]; then
expect << EOF
		set timeout -1
		log_user 1

		if { "${UPDATE}" == "true" } {
			spawn ${product}-configuration.sh -e ${ENVIRONMENT}
		} else {
			spawn ${product}-configuration.sh
		}

		expect -re "Database host:"
		send "\025$MYSQL_SERVER_HOST\r"

		expect -re "Database name:"
		send "\025$MYSQL_SERVER_DB_NAME\r"

		expect -re "Database user:"
		send "\025$MYSQL_SERVER_USER\r"

		expect -re "Database password:"
		send "\025$MYSQL_ROOT_PASS\r"

		expect eof	
EOF
		APPSERVER_INSTALLED="true";
	else 
		bash ${product}-configuration.sh
		APPSERVER_INSTALLED="true";
	fi
fi

echo ""
echo "$RES_INSTALL_SUCCESS"
echo "$RES_QUESTIONS"
echo ""
