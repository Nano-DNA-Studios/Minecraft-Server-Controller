FROM ubuntu:24.04

ARG BUILD_CONFIG="Release"
ENV USERNAME=MSC
ENV DEBIAN_FRONTEND=noninteractive
ENV TZ=America/Toronto

WORKDIR /Server-Controller

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        libicu74 \
        lsb-release \
        p7zip \
        gnupg \
        tzdata \
    && install -m 0755 -d /etc/apt/keyrings \
    && curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc \
    && chmod a+r /etc/apt/keyrings/docker.asc \
    && echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu noble stable" > /etc/apt/sources.list.d/docker.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends docker-ce-cli \
    && rm -rf /var/lib/apt/lists/*

RUN useradd -ms /bin/bash ${USERNAME} 

COPY ./Minecraft-Server-Controller/rcon /usr/bin/rcon
COPY ./Minecraft-Server-Controller/bin/${BUILD_CONFIG}/net8.0/linux-x64/publish/ .

RUN mkdir /data /backup && chown -R ${USERNAME}:${USERNAME} /Server-Controller /data /backup

# Drop privileges to non-root account
USER MSC

# Launch the app
CMD ["./Minecraft-Server-Controller"]
