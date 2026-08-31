# Minecraft-Server-Controller
A simple, one page dashboard for controlling and managing a PaperMC Minecraft Server

NOTE : This website has been intentionally designed to for use with the Dynmap plugin for now. 

This controller can display and control the following things for a Minecraft Server :

## Displays
- Players Online
- Server Name
- Server Version
- Latency
- CPU Usage
- Memory Usage
- Dynmap view
- Backups
- Logs

## Controls
- Sending Messages to Server
- Running Commands on Server
- Load / Delete Backup
- Start
- Stop
- Restart
- Save
- Force Save
- Backup

## Running the Server
To run the service, it is suggested to use the following template in tandem with the Minecraft Server container :
```yml
services:
  app:
    user: "0:0"
    image: ghcr.io/nano-dna-studios/minecraft-server-controller:latest
    container_name: minecraft-server-controller
    env_file:
      - ./.env
    ports:
      - ${ControllerPort}:80
    volumes:
      - type: volume
        source: mc-data
        target: /data
        volume:
          nocopy: true
      - ./backup:/backup
      - /var/run/docker.sock:/var/run/docker.sock
  server:
    image: ghcr.io/nano-dna-studios/minecraft-papermc-server:26.2
    container_name: minecraft-papermc-server
    env_file:
      - ./.env
    environment:
      - MEMORY_MIN=2G
      - MEMORY_MAX=10G
    ports:
      - ${SERVER_PORT}:25565
      - ${MapPort}:8123
    volumes:
      - mc-data:/data
volumes:
  mc-data:
```

And it is suggested to used the following template for environment variables :
```
RCONHost=server
Delay=5000
NumOfBackups=3
ControllerPort=8081
ServerContainerName=<container-name>

# Dynmap Settings
MapHealthUrl=http://server:8123/
MapBrowserUrl=http://<local-ip>:8123/
MapPort=8123

# Server Properties
MAX_PLAYERS=20
MOTD=<NAME>
RCON_PASSWORD=<PASSWORD>
RCON_PORT=25575
SERVER_PORT=25565
VIEW_DISTANCE=<16-32>
SIM_DISTANCE=<16-32>
```
