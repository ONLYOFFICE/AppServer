#!/bin/bash

set -e

cat<<EOF

#######################################
#  INSTALL PREREQUISITES
#######################################

EOF

# clean yum cache
${package_manager} clean all

${package_manager} -y install yum-utils

DIST=$(rpm -q --whatprovides redhat-release || rpm -q --whatprovides centos-release);
DIST=$(echo $DIST | sed -n '/-.*/s///p');
REV=$(cat /etc/redhat-release | sed s/.*release\ // | sed s/\ .*//);
REV_PARTS=(${REV//\./ });
REV=${REV_PARTS[0]};

if ! [[ "$REV" =~ ^[0-9]+$ ]]; then
	REV=7;
fi

read_unsupported_installation () {
	read -p "$RES_CHOICE_INSTALLATION " CHOICE_INSTALLATION
	case "$CHOICE_INSTALLATION" in
		y|Y ) yum -y install $DIST*-release
		;;

		n|N ) exit 0;
		;;

		* ) echo $RES_CHOICE;
			read_unsupported_installation
		;;
	esac
}

{ yum check-update $DIST*-release; exitCode=$?; } || true #Checking for distribution update

UPDATE_AVAILABLE_CODE=100
if [[ $exitCode -eq $UPDATE_AVAILABLE_CODE ]]; then
	res_unsupported_version
	echo $RES_UNSPPORTED_VERSION
	echo $RES_SELECT_INSTALLATION
	echo $RES_ERROR_REMINDER
	echo $RES_QUESTIONS
	read_unsupported_installation
fi

# add epel repo
rpm -ivh https://dl.fedoraproject.org/pub/epel/epel-release-latest-$REV.noarch.rpm || true
rpm -ivh https://rpms.remirepo.net/enterprise/remi-release-$REV.rpm || true

#add nodejs repo
curl -sL https://rpm.nodesource.com/setup_12.x | bash - || true

#add yarn
curl -sL https://dl.yarnpkg.com/rpm/yarn.repo | tee /etc/yum.repos.d/yarn.repo || true

#add dotnet repo
rpm -Uvh https://packages.microsoft.com/config/centos/$REV/packages-microsoft-prod.rpm || true

#add mysql repo
case $REV in
	8) 	dnf remove -y @mysql
		dnf module -y reset mysql && dnf module -y disable mysql
		${package_manager} localinstall -y https://dev.mysql.com/get/mysql80-community-release-el8-1.noarch.rpm || true ;;
	7) 	${package_manager} localinstall -y https://dev.mysql.com/get/mysql80-community-release-el7-3.noarch.rpm || true ;;
	6) 	${package_manager} localinstall -y https://dev.mysql.com/get/mysql80-community-release-el6-3.noarch.rpm || true ;;
esac

if ! rpm -q mysql-community-server; then
	MYSQL_FIRST_TIME_INSTALL="true";
fi

#add elasticsearch repo
rpm --import https://artifacts.elastic.co/GPG-KEY-elasticsearch
cat > /etc/yum.repos.d/elasticsearch.repo <<END
[elasticsearch]
name=Elasticsearch repository for 7.x packages
baseurl=https://artifacts.elastic.co/packages/7.x/yum
gpgcheck=1
gpgkey=https://artifacts.elastic.co/GPG-KEY-elasticsearch
enabled=0
autorefresh=1
type=rpm-md
END

#install kafka
PRODUCT_DIR="/var/www/${product}"
if [ "$(ls -A "$PRODUCT_DIR/services/kafka" 2> /dev/null)" == "" ]; then
	mkdir -p ${PRODUCT_DIR}/services/
	getent passwd kafka >/dev/null || useradd -m -d ${PRODUCT_DIR}/services/kafka -s /sbin/nologin -p kafka kafka
	cd ${PRODUCT_DIR}/services/kafka
	wget https://downloads.apache.org/kafka/2.7.0/kafka_2.13-2.7.0.tgz
	tar xzf kafka_*.tgz --strip 1 && rm -rf kafka_*.tgz
	chown -R kafka ${PRODUCT_DIR}/services/kafka
	cd -
fi

if [ ! -e /lib/systemd/system/zookeeper.service ]; then
cat > /lib/systemd/system/zookeeper.service <<END
[Unit]
Requires=network.target remote-fs.target
After=network.target remote-fs.target
[Service]
Type=simple
User=kafka
ExecStart=/bin/sh -c '${PRODUCT_DIR}/services/kafka/bin/zookeeper-server-start.sh ${PRODUCT_DIR}/services/kafka/config/zookeeper.properties > ${PRODUCT_DIR}/services/kafka/zookeeper.log 2>&1'
ExecStop=${PRODUCT_DIR}/services/kafka/bin/zookeeper-server-stop.sh
Restart=on-abnormal
[Install]
WantedBy=multi-user.target
END
fi

if [ ! -e /lib/systemd/system/kafka.service ]; then
cat > /lib/systemd/system/kafka.service <<END
[Unit]
Requires=zookeeper.service
After=zookeeper.service
[Service]
Type=simple
User=kafka
ExecStart=/bin/sh -c '${PRODUCT_DIR}/services/kafka/bin/kafka-server-start.sh ${PRODUCT_DIR}/services/kafka/config/server.properties > ${PRODUCT_DIR}/services/kafka/kafka.log 2>&1'
ExecStop=${PRODUCT_DIR}/services/kafka/bin/kafka-server-stop.sh
Restart=on-abnormal
[Install]
WantedBy=multi-user.target
END
fi

# add nginx repo
cat > /etc/yum.repos.d/nginx.repo <<END
[nginx-stable]
name=nginx stable repo
baseurl=https://nginx.org/packages/centos/$REV/\$basearch/
gpgcheck=1
enabled=1
gpgkey=https://nginx.org/keys/nginx_signing.key
module_hotfixes=true
END

if [ "$REV" = "8" ]; then
	rabbitmq_version="-3.8.12"

	cat > /etc/yum.repos.d/rabbitmq-server.repo <<END
[rabbitmq-server]
name=rabbitmq-server
baseurl=https://packagecloud.io/rabbitmq/rabbitmq-server/el/7/\$basearch
repo_gpgcheck=1
gpgcheck=0
enabled=1
gpgkey=https://packagecloud.io/rabbitmq/rabbitmq-server/gpgkey
sslverify=0
sslcacert=/etc/pki/tls/certs/ca-bundle.crt
metadata_expire=300
END

fi

${package_manager} -y install yum-plugin-versionlock
${package_manager} versionlock clear

${package_manager} -y install epel-release \
			expect \
			nano \
			nodejs \
			gcc-c++ \
			make \
			yarn \
			dotnet-sdk-5.0 \
			elasticsearch-7.13.1 --enablerepo=elasticsearch \
			mysql-server \
			nginx \
			supervisor \
			postgresql \
			postgresql-server \
			rabbitmq-server$rabbitmq_version \
			redis --enablerepo=remi \
			java
	
postgresql-setup initdb	|| true

semanage permissive -a httpd_t

if [ ! -e /usr/bin/json ]; then
	npm i json -g >/dev/null 2>&1
fi

systemctl daemon-reload
package_services="rabbitmq-server postgresql redis supervisord nginx kafka mysqld"
