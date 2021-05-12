# DockerizedAsp.netCoreApp
 Angular and Asp.net core application

## local run Command
docker build -t secure_docker_image .

docker run -d  -p 5000:80 --name secure_docker_container secure_docker_image


## docker hub push pull run Command
docker build -t ranjeethmd/open-image:secure-docker-image .

docker push ranjeethmd/open-image:secure-docker-image

docker image rm ranjeethmd/open-image:secure-docker-image

docker run -d  -p 5000:80 --name secure_docker_container ranjeethmd/open-image:secure-docker-image

## docker compose example
docker-compose up -d