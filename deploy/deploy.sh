#!/usr/bin/env bash
# Runs on the server (called by the deploy job): copy of the database, pull the images of one commit, restart,
# check that the site answers. When it does not, go back to the previous version (the job still fails).
set -euo pipefail
cd /opt/besti
NEW_TAG=$1
OLD_TAG=$(grep '^TAG=' tag.env 2>/dev/null | cut -d= -f2- || true)
SA_PASSWORD=$(grep '^MSSQL_SA_PASSWORD=' .env | cut -d= -f2-)
compose() { docker compose -f docker-compose.prod.yml --env-file .env --env-file tag.env "$@"; }

# Both the API and the web answer within 3 minutes
healthy() {
  for _ in $(seq 1 36); do
    if compose exec -T web wget -q -O /dev/null http://api:8080/health \
       && compose exec -T web wget -q -O /dev/null http://127.0.0.1:3000/; then
      return 0
    fi
    sleep 5
  done
  return 1
}

# 1. Copy of the database before the new API runs its migrations (overwritten on every deploy)
compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SA_PASSWORD" -b \
  -Q "BACKUP DATABASE [besti2] TO DISK = N'/backups/pre-deploy.bak' WITH INIT, CHECKSUM"

# 2. New version (when the pull fails, tag.env goes back so the next deploy knows what is running)
echo "TAG=$NEW_TAG" > tag.env
if ! compose pull; then
  echo "TAG=$OLD_TAG" > tag.env
  exit 1
fi
compose up -d --remove-orphans

if healthy; then
  echo "Deployed $NEW_TAG"
  # Keep only this version and the previous one (for a rollback): every deploy adds two new images,
  # and prune -f alone removes only images without a tag. An image still in use is never removed
  docker images --format '{{.Repository}}:{{.Tag}}' \
    | grep -E '^ghcr\.io/szczepanskimichal/besti2-(api|web):' \
    | grep -v -E ":(${NEW_TAG}|${OLD_TAG:-none})$" \
    | xargs -r docker rmi > /dev/null 2>&1 || true
  docker image prune -f > /dev/null
  exit 0
fi

echo "The site did not answer after 3 minutes" >&2
compose logs --tail=80 api web >&2

# 3. Back to the previous version. Migrations are not undone: so far they only add columns and tables,
#    and the old version works with them. pre-deploy.bak is there if a restore is ever needed.
if [ -n "$OLD_TAG" ] && [ "$OLD_TAG" != "$NEW_TAG" ]; then
  echo "Rolling back to $OLD_TAG" >&2
  echo "TAG=$OLD_TAG" > tag.env
  compose up -d --remove-orphans
  if healthy; then
    echo "Rolled back to $OLD_TAG, the site works again" >&2
  else
    echo "The previous version does not answer either" >&2
  fi
fi
exit 1
