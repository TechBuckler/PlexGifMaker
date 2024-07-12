@echo off
setlocal enabledelayedexpansion

if not defined PLEXGIFMAKER_DATA_PATH (
    set "PLEXGIFMAKER_DATA_PATH=.\plexgifmaker_keys"
    echo PLEXGIFMAKER_DATA_PATH is not set. Using default: !PLEXGIFMAKER_DATA_PATH!
)

echo Creating directory: !PLEXGIFMAKER_DATA_PATH!
if not exist "!PLEXGIFMAKER_DATA_PATH!" mkdir "!PLEXGIFMAKER_DATA_PATH!"

if errorlevel 1 (
    echo Failed to create directory. Using current directory.
    set "PLEXGIFMAKER_DATA_PATH=."
)

echo Using PLEXGIFMAKER_DATA_PATH: !PLEXGIFMAKER_DATA_PATH!

docker-compose down
docker-compose build
docker-compose up -d

endlocal