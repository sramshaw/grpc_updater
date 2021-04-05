# grpc_updater
a mechanism to upgrade a service on a PC by interacting with a container conveying the new version

It it composed of 2 parts:
- one microservice that runs in docker
- one updater, which is a client requesting updates from the docker container

Assuming the service to update is a single file application, the updater only needs to download one file.


