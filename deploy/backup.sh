#!/usr/bin/env bash
# Nightly backup (cron 03:30, as root): database, photos and DataProtection keys
# to the Hetzner Storage Box. Keeps 14 days here and on the box.
set -euo pipefail
cd /opt/besti

BOX="u700408@u700408.your-storagebox.de"
DAY=$(date +%F)
SA_PASSWORD=$(grep '^MSSQL_SA_PASSWORD=' .env | cut -d= -f2-)
compose() { docker compose -f docker-compose.prod.yml --env-file .env --env-file tag.env "$@"; }

# Healthchecks.io: /start now, the exit code at the end (0 = OK, anything else = mail)
HC_URL=$(grep '^BACKUP_PING_URL=' .env | cut -d= -f2- || true)
hc() { [ -z "$HC_URL" ] || curl -fsS -m 10 --retry 3 -o /dev/null "$HC_URL$1" || true; }
trap 'hc "/$?"' EXIT
hc /start

# 1. Database: SQL Server (user 10001 in the container) writes the .bak into ./backups
mkdir -p backups
chown 10001 backups
compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SA_PASSWORD" -b \
  -Q "BACKUP DATABASE [besti2] TO DISK = N'/backups/besti2.bak' WITH INIT, CHECKSUM"
gzip -c backups/besti2.bak > "backups/besti2-$DAY.bak.gz"
rm backups/besti2.bak

# 2. Photos and DataProtection keys (without the keys everyone is logged out after a restore)
tar czf "backups/files-$DAY.tar.gz" -C /var/lib/docker/volumes besti_uploads/_data besti_keys/_data

# 3. Keep 14 days, then copy to the box (--delete: the box keeps the same 14 days)
find backups -type f -mtime +13 -delete
rsync -a --delete -e "ssh -p 23 -i /root/.ssh/storagebox" backups/ "$BOX:besti/"

# 4. Database size: SQL Server Express stops at 10 GB of data, so warn at 7 GB.
#    Its own healthchecks.io check; a failure here never fails the backup above.
SIZE_URL=$(grep '^DB_SIZE_PING_URL=' .env | cut -d= -f2- || true)
if [ -n "$SIZE_URL" ]; then
  SIZE_MB=$(compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SA_PASSWORD" -d besti2 -h -1 -W \
    -Q "SET NOCOUNT ON; SELECT CAST(SUM(CAST(size AS bigint)) * 8 / 1024 AS int) FROM sys.database_files WHERE type_desc = 'ROWS'" \
    | tr -dc '0-9' || true)
  if [ -z "$SIZE_MB" ]; then
    curl -fsS -m 10 --retry 3 -o /dev/null --data-raw "Could not read the database size" "$SIZE_URL/fail" || true
  elif [ "$SIZE_MB" -ge 7168 ]; then
    curl -fsS -m 10 --retry 3 -o /dev/null --data-raw "Database is $SIZE_MB MB (limit 10240 MB)" "$SIZE_URL/fail" || true
  else
    curl -fsS -m 10 --retry 3 -o /dev/null --data-raw "Database is $SIZE_MB MB" "$SIZE_URL" || true
  fi
fi

echo "$(date '+%F %T') backup OK"