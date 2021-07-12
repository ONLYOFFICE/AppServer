#!/usr/bin/bash

REPO_NAME="onlyoffice"
IMAGE_NAME="4testing-appserver-api"
IMAGE_TAG="latest"
DOCKER_COMPOSE_PATH=""

CHECK_HUB_TOOL=""
DOCKER_IMAGE=""
DOCKER_HUB_IMAGE=""
CHECK_DATE=""

while [ "$1" != "" ]; do
    case $1 in

        -rn | --reponame )
        	if [ "$2" != "" ]; then
    			REPO_NAME=$2
		    	shift
		    fi
		;;
        -in | --imagename )
            if [ "$2" != "" ]; then
                IMAGE_NAME=$2
                shift
            fi
        ;;
        -it | --imagetag )
            if [ "$2" != "" ]; then
                IMAGE_TAG=$2
                shift
            fi
        ;;
        -cp | --composepath )
            if [ "$2" != "" ]; then
                DOCKER_COMPOSE_PATH=$2
                shift
            fi
        ;;
        -? | -h | --help )
            echo " Usage: bash update_from_github.sh [PARAMETER] [[PARAMETER], ...]"
            echo "    Parameters:"
            echo "    -rn, --reponame              name of repository"
            echo "    -in, --imagename             name of image"
            echo "    -it, --imagetag              name of image tag"
            echo "    -cp, --composepath           path to docker-compose tool"
            echo "    -?, -h, --help               this help"
            echo "  Examples"
            echo "  bash update_from_github.sh -rn \"onlyoffice\" "
            exit 0
        ;;

		* )
			echo "Unknown parameter $1" 1>&2
			exit 1
		;;
    esac
  shift
done

# check available hub_tool
check_installed_hub_tool() {
    CHECK_HUB_TOOL=$(whereis hub-tool | awk '{print $2}')

    if [ -z ${CHECK_HUB_TOOL} ]; then
        echo "The Docker Hub Tool is not installed or is not installed in standard path ${PATH} directories" 
        echo "You can download the The Docker Hub Tool using the link https://github.com/docker/hub-tool/releases"
        echo "More information You can find on https://github.com/docker/hub-tool"
        exit 1;
    fi
}

# check available locale image
check_docker_image() {
    DOCKER_IMAGE=$(docker image ls ${REPO_NAME}/${IMAGE_NAME}:${IMAGE_TAG} | grep ${REPO_NAME} | awk '{print $1":"$2}')

    if [ -z ${DOCKER_IMAGE} ]; then
        echo "The Docker image ${REPO_NAME}/${IMAGE_NAME}:${IMAGE_TAG} is not created on a local machine" 
        exit 1;
    fi
}

# check available image on docker hub
check_docker_hub_image() {
    DOCKER_HUB_IMAGE=$(hub-tool tag ls ${REPO_NAME}/${IMAGE_NAME} | grep ${REPO_NAME}.*.${IMAGE_TAG} | awk '{print $1}')

    if [ -z ${DOCKER_HUB_IMAGE} ]; then
        echo "The Docker image ${REPO_NAME}/${IMAGE_NAME}:${IMAGE_TAG} is not uploaded on a docker hub" 
        exit 1;
    fi
}

# local date creation image inspect
get_local_image_date() {
    LOCAL_IMAGE=$1;    
 
    LOCAL_IMAGE_CREATED=$(docker inspect ${LOCAL_IMAGE} | jq .[].Created)
}

# remote date creation image inspect
get_remote_image_date() {
    REMOTE_IMAGE=$1;

    REMOTE_IMAGE_CREATED=$(hub-tool tag inspect ${REMOTE_IMAGE} --format "json" | jq .Config.created)
}

# compare images date creation
check_image_date_create() {
    LOCAL_DATE=$1
    REMOTE_DATE=$2

    if [ "${LOCAL_DATE}" == "${REMOTE_DATE}" ]; then
	   CHECK_DATE="false";
	else
       CHECK_DATE="true";
    fi
}

# update local images
docker_image_update() {

    if [ ${DOCKER_COMPOSE_PATH} ]; then
       cd ${DOCKER_COMPOSE_PATH}
    fi

    docker-compose -f appserver.yml -f notify.yml down --volumes
    docker-compose -f build.yml pull
    docker-compose -f appserver.yml -f notify.yml up -d
    docker image prune -a -f
}

check_installed_hub_tool
check_docker_image
check_docker_hub_image

get_local_image_date "${REPO_NAME}/${IMAGE_NAME}:${IMAGE_TAG}"
get_remote_image_date "${REPO_NAME}/${IMAGE_NAME}:${IMAGE_TAG}"

check_image_date_create ${LOCAL_IMAGE_CREATED} ${REMOTE_IMAGE_CREATED}

if [ ${CHECK_DATE} == "true" ]; then
    echo LOCAL_IMAGE_CREATED=${LOCAL_IMAGE_CREATED}
    echo REMOTE_IMAGE_CREATED=${REMOTE_IMAGE_CREATED}
    docker_image_update
fi
