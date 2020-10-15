
#! /usr/bin/env sh
set -x

docker-compose pull \
    && docker-compose down \
    && docker-compose up -d