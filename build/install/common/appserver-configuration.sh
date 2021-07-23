#!/bin/bash
PRODUCT="appserver"
ENVIRONMENT="production"

APP_DIR="/etc/onlyoffice/${PRODUCT}"
USER_CONF="$APP_DIR/appsettings.$ENVIRONMENT.json"
NGINX_CONF="/etc/nginx/conf.d"
SYSTEMD_DIR="/lib/systemd/system"

MYSQL=""
DB_HOST=""
DB_PORT="3306"
DB_NAME=""
DB_USER=""
DB_PWD=""

APP_HOST="localhost"
APP_PORT="80"

DOCUMENT_SERVER_HOST="localhost";
DOCUMENT_SERVER_PORT="8083";

KAFKA_HOST="localhost"
KAFKA_PORT="9092"
ZOOKEEPER_HOST="localhost"
ZOOKEEPER_PORT="2181"

ELK_SHEME="http"
ELK_HOST="localhost"
ELK_PORT="9200"

JSON="json -I -f"
JSON_USERCONF="$JSON $USER_CONF -e"

[ $(id -u) -ne 0 ] && { echo "Root privileges required"; exit 1; }

while [ "$1" != "" ]; do
	case $1 in

		-ash | --appshost )
			if [ "$2" != "" ]; then
				APP_HOST=$2
				shift
			fi
		;;

		-asp | --appsport )
			if [ "$2" != "" ]; then
				APP_PORT=$2
				shift
			fi
		;;

		-dsh | --docshost )
			if [ "$2" != "" ]; then
				DOCUMENT_SERVER_HOST=$2
				shift
			fi
		;;

		-dsp | --docsport )
			if [ "$2" != "" ]; then
				DOCUMENT_SERVER_PORT=$2
				shift
			fi
		;;

		-kh | --kafkahost )
			if [ "$2" != "" ]; then
				KAFKA_HOST=$2
				shift
			fi
		;;

		-kp | --kafkaport )
			if [ "$2" != "" ]; then
				KAFKA_PORT=$2
				shift
			fi
		;;

		-zkh | --zookeeperhost )
			if [ "$2" != "" ]; then
				ZOOKEEPER_HOST=$2
				shift
			fi
		;;

		-zkp | --zookeeperport )
			if [ "$2" != "" ]; then
				ZOOKEEPER_HOST=$2
				shift
			fi
		;;

		-esh | --elastichost )
			if [ "$2" != "" ]; then
				ELK_HOST=$2
				shift
			fi
		;;

		-esp | --elasticport )
			if [ "$2" != "" ]; then
				ELK_HOST=$2
				shift
			fi
		;;

		-e | --environment )
			if [ "$2" != "" ]; then
				ENVIRONMENT=$2
				shift
			fi
		;;

		-? | -h | --help )
			echo "  Usage: bash ${PRODUCT}-configuration.sh [PARAMETER] [[PARAMETER], ...]"
			echo
			echo "    Parameters:"
			echo "      -ash, --appshost                    ${PRODUCT} ip"
			echo "      -asp, --appsport                    ${PRODUCT} port (default 80)"
			echo "      -dsh, --docshost                    document server ip"
			echo "      -dsp, --docsport                    document server port (default 8083)"
			echo "      -kh, --kafkahost                    kafka ip"
			echo "      -kp, --kafkaport                    kafka port (default 9092)"
			echo "      -zkh, --zookeeperhost               zookeeper ip"
			echo "      -zkp, --zookeeperport               zookeeper port (default 2181)"
			echo "      -esh, --elastichost                 elasticsearch ip"
			echo "      -esp, --elasticport                 elasticsearch port (default 9200)"
			echo "      -e, --environment                   environment (default 'production')"
			echo "      -?, -h, --help                      this help"
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

set_core_machinekey () {
	if [ -f $APP_DIR/.private/machinekey ]; then
		CORE_MACHINEKEY=$(cat $APP_DIR/.private/machinekey)
	else
		CORE_MACHINEKEY=$(cat /dev/urandom | tr -dc A-Za-z0-9 | head -c 12);
		echo $CORE_MACHINEKEY >> $APP_DIR/.private/machinekey
	fi
}

install_json() {

	if [ ! -e /usr/bin/json ]; then
		echo -n "Install json package... "
		npm i json -g >/dev/null 2>&1
		echo "OK"
	fi

	#Creating a user-defined .json
	if [ ! -e $USER_CONF ]; then
		echo "{}" >> $USER_CONF
		chown onlyoffice:onlyoffice $USER_CONF
	
		set_core_machinekey
		$JSON_USERCONF "this.core={'base-domain': \"$APP_HOST\", 'machinekey': \"$CORE_MACHINEKEY\" }" \
		-e "this.urlshortener={ 'path': '../ASC.UrlShortener/index.js' }" -e "this.thumb={ 'path': '../ASC.Thumbnails/' }" \
		-e "this.socket={ 'path': '../ASC.Socket.IO/' }" >/dev/null 2>&1
		$JSON $APP_DIR/appsettings.json -e "this.core.products.subfolder='server'" >/dev/null 2>&1
		$JSON $APP_DIR/appsettings.services.json -e "this.core={ 'products': { 'folder': '../../products', 'subfolder': 'server'} }" >/dev/null 2>&1
		
	fi
}

restart_services() {
	echo -n "Restarting services... "

	sed -i "s/ENVIRONMENT=.*/ENVIRONMENT=$ENVIRONMENT/" $SYSTEMD_DIR/${PRODUCT}*.service >/dev/null 2>&1
	systemctl daemon-reload

	for SVC in nginx mysql* ${PRODUCT}-api ${PRODUCT}-api-system ${PRODUCT}-urlshortener ${PRODUCT}-thumbnails \
	${PRODUCT}-socket ${PRODUCT}-studio-notify ${PRODUCT}-notify ${PRODUCT}-people-server ${PRODUCT}-files \
	${PRODUCT}-files-services ${PRODUCT}-studio ${PRODUCT}-backup ${PRODUCT}-storage-encryption \
	${PRODUCT}-storage-migration ${PRODUCT}-projects-server ${PRODUCT}-telegram-service ${PRODUCT}-crm \
	${PRODUCT}-calendar ${PRODUCT}-mail elasticsearch kafka zookeeper
	do
		if systemctl is-active $SVC | grep -q "active"; then
			systemctl restart $SVC.service >/dev/null 2>&1
		else
			systemctl enable $SVC.service  >/dev/null 2>&1
			systemctl start $SVC.service  >/dev/null 2>&1
		fi
	done
	echo "OK"
}

input_db_params(){
    local user_connectionString=$(json -f $USER_CONF ConnectionStrings.default.connectionString)
    local def_DB_HOST=$(echo $user_connectionString | grep -oP 'Server=\K.*' | grep -o '^[^;]*')
    local def_DB_NAME=$(echo $user_connectionString | grep -oP 'Database=\K.*' | grep -o '^[^;]*')
    local def_DB_USER=$(echo $user_connectionString | grep -oP 'User ID=\K.*' | grep -o '^[^;]*')

	read -e -p "Database host: " -i "$DB_HOST" DB_HOST
	read -e -p "Database name: " -i "$DB_NAME" DB_NAME
	read -e -p "Database user: " -i "$DB_USER" DB_USER
	read -e -p "Database password: " -s DB_PWD
    
    if [ -z $DB_HOST ]; then
		DB_HOST="${def_DB_HOST}";
	fi

	if [ -z $DB_NAME ]; then
		DB_NAME="${def_DB_NAME}";
	fi

	if [ -z $DB_USER ]; then
		DB_USER="${def_DB_USER}";
	fi

    echo
}

establish_mysql_conn(){
	echo -n "Trying to establish MySQL connection... "

	command -v mysql >/dev/null 2>&1 || { echo "MySQL client not found"; exit 1; }

	MYSQL="mysql -h$DB_HOST -u$DB_USER"
	if [ -n "$DB_PWD" ]; then
		MYSQL="$MYSQL -p$DB_PWD"
	fi

	$MYSQL -e ";" >/dev/null 2>&1
	ERRCODE=$?
	if [ $ERRCODE -ne 0 ]; then
		systemctl mysql* start >/dev/null 2>&1
		$MYSQL -e ";" >/dev/null 2>&1 || { echo "FAILURE"; exit 1; }
	fi

    #Save db settings in .json
	$JSON_USERCONF "this.ConnectionStrings={'default': {'connectionString': \
	\"Server=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;User ID=$DB_USER;Password=$DB_PWD;Pooling=true;Character Set=utf8;AutoEnlist=false;SSL Mode=none\"}}" >/dev/null 2>&1

	echo "OK"
}

mysql_check_connection() {
	while ! $MYSQL -e ";" >/dev/null 2>&1; do
    		sleep 1
	done
}

change_mysql_config(){
	if [ "$DIST" = "RedHat" ]; then
	
		local CNF_PATH="/etc/my.cnf";
		local CNF_SERVICE_PATH="/usr/lib/systemd/system/mysqld.service";

		if ! grep -q "\[mysqld\]" ${CNF_PATH}; then
			CNF_PATH="/etc/my.cnf.d/server.cnf";

			if ! grep -q "\[mysqld\]" ${CNF_PATH}; then
				exit 1;
			fi
		fi 

		if ! grep -q "\[Unit\]" ${CNF_SERVICE_PATH}; then
			CNF_SERVICE_PATH="/lib/systemd/system/mysqld.service";

			if ! grep -q "\[Unit\]" ${CNF_SERVICE_PATH}; then
				CNF_SERVICE_PATH="/lib/systemd/system/mariadb.service";
					
				if ! grep -q "\[Unit\]" ${CNF_SERVICE_PATH}; then 
					exit 1;
				fi
			fi
		fi 

	elif [ "$DIST" = "Debian" ]; then

		sed "s/#max_connections.*/max_connections = 1000/" -i /etc/mysql/my.cnf || true # ignore errors

		CNF_PATH="/etc/mysql/mysql.conf.d/mysqld.cnf";
		CNF_SERVICE_PATH="/lib/systemd/system/mysql.service";

		if mysql -V | grep -q "MariaDB"; then
			CNF_PATH="/etc/mysql/mariadb.conf.d/50-server.cnf";
			CNF_SERVICE_PATH="/lib/systemd/system/mariadb.service";
		fi

	fi

    sed '/skip-networking/d' -i ${CNF_PATH} || true # ignore errors

    if ! grep -q "^sql_mode" ${CNF_PATH}; then
		sed "/\[mysqld\]/a sql_mode = 'NO_ENGINE_SUBSTITUTION'" -i ${CNF_PATH} # disable new STRICT mode in mysql 5.7
	else
		sed "s/sql_mode.*/sql_mode = 'NO_ENGINE_SUBSTITUTION'/" -i ${CNF_PATH} || true # ignore errors
	fi

	if ! grep -q "^max_connections"  ${CNF_PATH}; then
		sed '/\[mysqld\]/a max_connections = 1000' -i ${CNF_PATH}
	else
		sed "s/max_connections.*/max_connections = 1000/" -i ${CNF_PATH} || true # ignore errors
	fi

	if ! grep -q "^group_concat_max_len"  ${CNF_PATH}; then
		sed '/\[mysqld\]/a group_concat_max_len = 2048' -i ${CNF_PATH}
	else
		sed "s/group_concat_max_len.*/group_concat_max_len = 2048/" -i ${CNF_PATH} || true # ignore errors
	fi

	if ! grep -q "^max_allowed_packet"  ${CNF_PATH}; then
		sed '/\[mysqld\]/a max_allowed_packet = 1048576000' -i ${CNF_PATH}
	else
		sed "s/max_allowed_packet.*/max_allowed_packet = 1048576000/" -i ${CNF_PATH} || true # ignore errors
	fi

	if ! grep -q "^character_set_server"  ${CNF_PATH}; then
		sed '/\[mysqld\]/a character_set_server = utf8' -i ${CNF_PATH}
	else
		sed "s/character_set_server.*/character_set_server = utf8/" -i ${CNF_PATH} || true # ignore errors
	fi
	
	if ! grep -q "^collation_server"  ${CNF_PATH}; then
		sed '/\[mysqld\]/a collation_server = utf8_general_ci' -i ${CNF_PATH}
	else
		sed "s/collation_server.*/collation_server = utf8_general_ci/" -i ${CNF_PATH} || true # ignore errors
	fi
	
	if ! grep -q "^default-authentication-plugin"  ${CNF_PATH}; then
		sed '/\[mysqld\]/a default-authentication-plugin = mysql_native_password' -i ${CNF_PATH}
	else
		sed "s/default-authentication-plugin.*/default-authentication-plugin = mysql_native_password/" -i ${CNF_PATH} || true # ignore errors
	fi

	if [ -e ${CNF_SERVICE_PATH} ]; then
		
		if ! grep -q "^LimitNOFILE"  ${CNF_SERVICE_PATH}; then
			sed '/\[Service\]/a LimitNOFILE = infinity' -i ${CNF_SERVICE_PATH}
		else
			sed "s/LimitNOFILE.*/LimitNOFILE = infinity/" -i ${CNF_SERVICE_PATH} || true # ignore errors
		fi

		if ! grep -q "^LimitMEMLOCK"  ${CNF_SERVICE_PATH}; then
			sed '/\[Service\]/a LimitMEMLOCK = infinity' -i ${CNF_SERVICE_PATH}
		else
			sed "s/LimitMEMLOCK.*/LimitMEMLOCK = infinity/" -i ${CNF_SERVICE_PATH} || true # ignore errors
		fi
	
	fi

    systemctl daemon-reload >/dev/null 2>&1
	systemctl restart mysql* >/dev/null 2>&1
}

execute_mysql_script(){

	change_mysql_config

    mysql_check_connection
    
    if [ "$DB_USER" = "root" ] && [ ! "$(mysql -V | grep ' 5.5.')" ]; then
	   # allow connect via mysql_native_password with root and empty password
	   $MYSQL -D "mysql" -e "update user set plugin='mysql_native_password' where user='root';ALTER USER '${DB_USER}'@'localhost' IDENTIFIED WITH mysql_native_password BY '${DB_PWD}';" >/dev/null 2>&1
	fi

	#Checking the quantity of the tables created in the db
    DB_TABLES_COUNT=$($MYSQL --silent --skip-column-names -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='${DB_NAME}'"); 
    
	local SQL_DIR="/var/www/${PRODUCT}/sql"
    if [ "${DB_TABLES_COUNT}" -eq "0" ]; then

		echo -n "Installing MYSQL database... "

		#Adding data to the db
		sed -i -e '1 s/^/SET SQL_MODE='ALLOW_INVALID_DATES';\n/;' $SQL_DIR/onlyoffice.sql
		$MYSQL -e "CREATE DATABASE IF NOT EXISTS $DB_NAME CHARACTER SET utf8 COLLATE 'utf8_general_ci';" >/dev/null 2>&1
		echo 'CREATE TABLE IF NOT EXISTS `Tenants` ( `id` varchar(200) NOT NULL, `Status` varchar(200) NOT NULL) ENGINE=InnoDB DEFAULT CHARSET=utf8;' >> $SQL_DIR/onlyoffice.sql #Fix non-existent tables Tenants
		$MYSQL "$DB_NAME" < "$SQL_DIR/createdb.sql" >/dev/null 2>&1
		$MYSQL "$DB_NAME" < "$SQL_DIR/onlyoffice.sql" >/dev/null 2>&1
		$MYSQL "$DB_NAME" < "$SQL_DIR/onlyoffice.data.sql" >/dev/null 2>&1
		$MYSQL "$DB_NAME" < "$SQL_DIR/onlyoffice.resources.sql" >/dev/null 2>&1
		for i in $(ls $SQL_DIR/*upgrade*.sql); do
			$MYSQL "$DB_NAME" < ${i} >/dev/null 2>&1
		done
	else
		echo -n "Upgrading MySQL database... "
		for i in $(ls $SQL_DIR/*upgrade*.sql); do
			$MYSQL "$DB_NAME" < ${i} >/dev/null 2>&1
		done
    fi
    echo "OK"
}

setup_nginx(){
	echo -n "Configuring nginx... "

	mv -f $NGINX_CONF/default.conf $NGINX_CONF/default.conf.old >/dev/null 2>&1

    sed -i "s/listen.*;/listen $APP_PORT;/" $NGINX_CONF/onlyoffice.conf

    shopt -s nocasematch
    PORTS=()
	if $(getenforce) >/dev/null 2>&1; then
		case $(getenforce) in
			enforcing|permissive)
				PORTS+=('8081') #Storybook
				PORTS+=("$DOCUMENT_SERVER_PORT")
				PORTS+=('5001') #ASC.Web.Studio
				PORTS+=('5002') #ASC.People
				PORTS+=('5008') #ASC.Files/client
				PORTS+=('5013') #ASC.Files/editor
				PORTS+=('5014') #ASC.CRM
				setsebool -P httpd_can_network_connect on
			;;
			disabled)
				:
			;;
		esac

		for PORT in ${PORTS[@]}; do
			semanage port -a -t http_port_t -p tcp $PORT >/dev/null 2>&1 || \
			semanage port -m -t http_port_t -p tcp $PORT >/dev/null 2>&1 || \
			true
		done
	fi
    chown nginx:nginx /etc/nginx/* -R
    sudo sed -e 's/#//' -i $NGINX_CONF/onlyoffice.conf
	echo "OK"
}

setup_docs() {
	echo -n "Configuring Docs... "
	local DS_CONF="/etc/onlyoffice/documentserver/local.json"
	local JSON_DSCONF="$JSON $DS_CONF -e"

	#Changing the Docs port in nginx conf
	sed -i "s/0.0.0.0:.*;/0.0.0.0:$DOCUMENT_SERVER_PORT;/" $NGINX_CONF/ds.conf
	sed -i "s/]:.*;/]:$DOCUMENT_SERVER_PORT default_server;/g" $NGINX_CONF/ds.conf 
	sed "0,/proxy_pass .*;/{s/proxy_pass .*;/proxy_pass http:\/\/${DOCUMENT_SERVER_HOST}:${DOCUMENT_SERVER_PORT};/}" -i $NGINX_CONF/onlyoffice.conf 	

	#Enable JWT validation for Docs
	$JSON_DSCONF "this.services.CoAuthoring.token.enable.browser='true'" >/dev/null 2>&1 
	$JSON_DSCONF "this.services.CoAuthoring.token.enable.request.inbox='true'" >/dev/null 2>&1
	$JSON_DSCONF "this.services.CoAuthoring.token.enable.request.outbox='true'" >/dev/null 2>&1
	
	local DOCUMENT_SERVER_JWT_SECRET=$(cat ${DS_CONF} | json services.CoAuthoring.secret.inbox.string)
	local DOCUMENT_SERVER_JWT_HEADER=$(cat ${DS_CONF} | json services.CoAuthoring.token.inbox.header)

	#Save Docs address and JWT in .json
	$JSON_USERCONF "this.files={'docservice': {\
	'secret': {'value': \"$DOCUMENT_SERVER_JWT_SECRET\",'header': \"$DOCUMENT_SERVER_JWT_HEADER\"}, \
	'url': {'public': '/ds-vpath/','internal': \"http://${DOCUMENT_SERVER_HOST}:${DOCUMENT_SERVER_PORT}\",'portal': \"http://$APP_HOST:$APP_PORT\"}}}" >/dev/null 2>&1
	
	#Enable ds-example autostart
	sed 's,autostart=false,autostart=true,' -i /etc/supervisord.d/ds-example.ini >/dev/null 2>&1 || sed 's,autostart=false,autostart=true,' -i /etc/supervisor/conf.d/ds-example.conf >/dev/null 2>&1
	supervisorctl start ds:example >/dev/null 2>&1
	
	echo "OK"
}

change_elasticsearch_config(){

	systemctl stop elasticsearch

	local ELASTIC_SEARCH_CONF_PATH="/etc/elasticsearch/elasticsearch.yml"
	local ELASTIC_SEARCH_JAVA_CONF_PATH="/etc/elasticsearch/jvm.options";

	if /usr/share/elasticsearch/bin/elasticsearch-plugin list | grep -q "ingest-attachment"; then
		/usr/share/elasticsearch/bin/elasticsearch-plugin remove -s ingest-attachment
	fi
		/usr/share/elasticsearch/bin/elasticsearch-plugin install -s -b ingest-attachment	

	if [ -f ${ELASTIC_SEARCH_CONF_PATH}.rpmnew ]; then
		cp -rf ${ELASTIC_SEARCH_CONF_PATH}.rpmnew ${ELASTIC_SEARCH_CONF_PATH};   
	fi

	if [ -f ${ELASTIC_SEARCH_JAVA_CONF_PATH}.rpmnew ]; then
		cp -rf ${ELASTIC_SEARCH_JAVA_CONF_PATH}.rpmnew ${ELASTIC_SEARCH_JAVA_CONF_PATH};   
	fi

	if ! grep -q "indices.fielddata.cache.size" ${ELASTIC_SEARCH_CONF_PATH}; then
		echo "indices.fielddata.cache.size: 30%" >> ${ELASTIC_SEARCH_CONF_PATH}
	else
		sed -i "s/indices.fielddata.cache.size.*/indices.fielddata.cache.size: 30%/" ${ELASTIC_SEARCH_CONF_PATH} 
	fi

	if ! grep -q "indices.memory.index_buffer_size" ${ELASTIC_SEARCH_CONF_PATH}; then
		echo "indices.memory.index_buffer_size: 30%" >> ${ELASTIC_SEARCH_CONF_PATH}
	else
		sed -i "s/indices.memory.index_buffer_size.*/indices.memory.index_buffer_size: 30%/" ${ELASTIC_SEARCH_CONF_PATH} 
	fi

	if grep -q "HeapDumpOnOutOfMemoryError" ${ELASTIC_SEARCH_JAVA_CONF_PATH}; then
		sed "/-XX:+HeapDumpOnOutOfMemoryError/d" -i ${ELASTIC_SEARCH_JAVA_CONF_PATH}
	fi

	local TOTAL_MEMORY=$(free -m | grep -oP '\d+' | head -n 1);
	local MEMORY_REQUIREMENTS=12228; #RAM ~4*3Gb

	if [ ${TOTAL_MEMORY} -gt ${MEMORY_REQUIREMENTS} ]; then
		if ! grep -q "[-]Xms1g" ${ELASTIC_SEARCH_JAVA_CONF_PATH}; then
			echo "-Xms4g" >> ${ELASTIC_SEARCH_JAVA_CONF_PATH}
		else
			sed -i "s/-Xms1g/-Xms4g/" ${ELASTIC_SEARCH_JAVA_CONF_PATH} 
		fi

		if ! grep -q "[-]Xmx1g" ${ELASTIC_SEARCH_JAVA_CONF_PATH}; then
			echo "-Xmx4g" >> ${ELASTIC_SEARCH_JAVA_CONF_PATH}
		else
			sed -i "s/-Xmx1g/-Xmx4g/" ${ELASTIC_SEARCH_JAVA_CONF_PATH} 
		fi
	fi

	if [ -d /etc/elasticsearch/ ]; then 
		chmod g+ws /etc/elasticsearch/
	fi

	systemctl start elasticsearch
}

setup_elasticsearch() {
	echo -n "Configuring elasticsearch... "

	#Save elasticsearch parameters in .json
	$JSON $APP_DIR/elastic.json -e "this.elastic={'Scheme': \"${ELK_SHEME}\",'Host': \"${ELK_HOST}\",'Port': \"${ELK_PORT}\",'Threads': \"1\" }" >/dev/null 2>&1

	change_elasticsearch_config
	
	echo "OK"
}

setup_kafka() {

	KAFKA_SERVICE=$(systemctl --type=service | grep 'kafka' | tr -d '●' | awk '{print $1;}')

	if [ -n ${KAFKA_SERVICE} ]; then

		echo -n "Configuring kafka... "
		
		local KAFKA_DIR="$(cat $SYSTEMD_DIR/$KAFKA_SERVICE | grep ExecStop= | cut -c 10- | rev | cut -c 26- | rev)"
		local KAFKA_CONF="${KAFKA_DIR}/config"

		#Change kafka config
		sed -i "s/clientPort=.*/clientPort=${ZOOKEEPER_PORT}/g" $KAFKA_CONF/zookeeper.properties
		sed -i "s/zookeeper.connect=.*/zookeeper.connect=${ZOOKEEPER_HOST}:${ZOOKEEPER_PORT}/g" $KAFKA_CONF/server.properties
		sed -i "s/bootstrap.servers=.*/bootstrap.servers=${KAFKA_HOST}:${KAFKA_PORT}/g" $KAFKA_CONF/consumer.properties
		sed -i "s/bootstrap.servers=.*/bootstrap.servers=${KAFKA_HOST}:${KAFKA_PORT}/g" $KAFKA_CONF/connect-standalone.properties
		sed -i "s/logger.kafka.controller=.*,/logger.kafka.controller=INFO,/g" $KAFKA_CONF/log4j.properties
		sed -i "s/logger.state.change.logger=.*,/logger.state.change.logger=INFO,/g" $KAFKA_CONF/log4j.properties
		echo "log4j.logger.kafka.producer.async.DefaultEventHandler=INFO, kafkaAppender" >> $KAFKA_CONF/log4j.properties
		
		#Save kafka parameters in .json
		$JSON_USERCONF "this.kafka={'BootstrapServers': \"${KAFKA_HOST}:${KAFKA_PORT}\"}" >/dev/null 2>&1

		#Add topics for kafka
		KAFKA_TOPICS=( ascchannelQuotaCacheItemAny
			ascchannelTariffCacheItemRemove
			ascchannelTenantCacheItemInsertOrUpdate
			ascchannelTenantSettingRemove )

		for i in "${KAFKA_TOPICS[@]}" 
		do
			${KAFKA_DIR}/bin/kafka-topics.sh --create --zookeeper ${ZOOKEEPER_HOST}:${ZOOKEEPER_PORT} --topic $i --replication-factor 1 --partitions 3 >/dev/null 2>&1
		done
		
		echo "OK"
	fi

}

if command -v yum >/dev/null 2>&1; then
	DIST="RedHat"
	PACKAGE_MANAGER="rpm -q"
elif command -v apt >/dev/null 2>&1; then
	DIST="Debian"
	PACKAGE_MANAGER="dpkg -l"
	mkdir -p /var/log/onlyoffice/appserver/
	chown -R onlyoffice:onlyoffice /var/www/appserver/
	chown -R onlyoffice:onlyoffice /var/log/onlyoffice/appserver/
	chown -R onlyoffice:onlyoffice /etc/onlyoffice/appserver/
fi

install_json

if $PACKAGE_MANAGER mysql-client >/dev/null 2>&1 || $PACKAGE_MANAGER mysql-community-client >/dev/null 2>&1; then
    input_db_params
    establish_mysql_conn || exit $?
    execute_mysql_script || exit $?
fi 

if $PACKAGE_MANAGER nginx >/dev/null 2>&1; then
    setup_nginx
fi

if $PACKAGE_MANAGER onlyoffice-documentserver >/dev/null 2>&1 || $PACKAGE_MANAGER onlyoffice-documentserver-de >/dev/null 2>&1 || $PACKAGE_MANAGER onlyoffice-documentserver-ee >/dev/null 2>&1; then
    setup_docs
fi

if $PACKAGE_MANAGER elasticsearch >/dev/null 2>&1; then
    setup_elasticsearch
fi

setup_kafka

restart_services
