#!/bin/bash

# Config
IMAGE_NAME="ghcr.io/nano-dna-studios/minecraft-server-controller:latest"
TAG="latest"
TARGET_COMPOSE_DIR="~/Services/Minecraft-Server"

echo "--- STEP 1: Deep Cleaning Project ---"
rm -rf Automatic-Bluray-Ripping/bin
rm -rf Automatic-Bluray-Ripping/obj

echo "--- STEP 2: Publishing for Linux-x64 ---"
dotnet publish "$PROJECT_FILE" -c Release -r linux-x64 --self-contained true

echo "--- STEP 3: Building Local Docker Image ---"
docker build --build-arg BUILD_CONFIG="Release" -t "${IMAGE_NAME}:${TAG}" .

echo "--- STEP 4: Transferring and Loading Image Directly ---"
docker save "${IMAGE_NAME}:${TAG}" | ssh rnaserver "docker load"

echo "--- STEP 5: Restarting Docker Compose on Target ---"
ssh rnaserver << EOF
  echo "--> Navigating to compose directory..."
  cd ${TARGET_COMPOSE_DIR}
  
  echo "--> Stopping old containers..."
  docker compose down
  
  echo "--> Starting new containers..."
  docker compose up -d --force-recreate
  
  echo "--> Done!"
EOF

echo "--- LOOP COMPLETE ---"


