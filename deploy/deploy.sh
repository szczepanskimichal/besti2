#!/usr/bin/env bash
# Runs on the server (called by the deploy job): pull the images of one commit, restart, check that the site answers
set -euo pipefail
cd /opt/besti
echo "TAG=$1" > tag.env
compose() { docker compose -f docker-compose.prod.yml --env-file .env --env-file tag.env "$@"; }

compose pull
compose up -d --remove-orphans

for _ in $(seq 1 36); do
  if compose exec -T web wget -q -O /dev/null http://api:8080/health \
     && compose exec -T web wget -q -O /dev/null http://127.0.0.1:3000/; then
    echo "Deployed $1"
    docker image prune -f > /dev/null
    exit 0
  fi
  sleep 5
done

echo "The site did not answer after 3 minutes" >&2
compose logs --tail=80 api web >&2
exit 1
