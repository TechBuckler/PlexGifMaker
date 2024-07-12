#!/bin/bash
mkdir -p "${PLEXGIFMAKER_DATA_PATH:-./plexgifmaker_keys}"
chmod 755 "${PLEXGIFMAKER_DATA_PATH:-./plexgifmaker_keys}"
docker-compose down
docker-compose build
docker-compose up -d