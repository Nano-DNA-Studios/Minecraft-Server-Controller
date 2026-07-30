FROM ubuntu:24.04

ARG BUILD_CONFIG="Release"
ENV USERNAME=MSC

WORKDIR /Server-Controller

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        libicu74 \
    && rm -rf /var/lib/apt/lists/*

RUN useradd -ms /bin/bash ${USERNAME} 

COPY ./Minecraft-Server-Controller/rcon /usr/bin/rcon

COPY ./Minecraft-Server-Controller/bin/${BUILD_CONFIG}/net8.0/linux-x64/publish/ .

RUN chown -R ${USERNAME}:${USERNAME} /Server-Controller

# Drop privileges to non-root account
USER MSC

# Launch the app
CMD ["./Minecraft-Server-Controller"]
