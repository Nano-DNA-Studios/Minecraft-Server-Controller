#!/bin/bash

# 1. Configuration
PROJECT_FILE="Minecraft-Server-Controller/Minecraft-Server-Controller.csproj"

echo "--- STEP 1: Deep Cleaning Project ---"
rm -rf Minecraft-Server-Controller/bin
rm -rf Minecraft-Server-Controller/obj

echo "--- STEP 2: Publishing for Linux-x64 ---"
dotnet publish "$PROJECT_FILE" -c Release -r linux-x64 --self-contained true

echo "--- STEP 3: Rebuilding Docker Containers ---"
docker compose build