
#! /usr/bin/env sh
set -x

sudo docker-compose pull \
    && sudo docker-compose down \
    && sudo docker-compose up -d