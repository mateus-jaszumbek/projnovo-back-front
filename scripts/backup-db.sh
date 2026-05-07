#!/bin/bash
set -euo pipefail

BACKUP_DIR="/home/ec2-user/backups"
CONTAINER="servicosapp-postgres"
DB_USER="servicosapp"
DB_NAME="servicosapp"
KEEP_DAYS=7
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
FILE="$BACKUP_DIR/backup_$TIMESTAMP.sql"

mkdir -p "$BACKUP_DIR"

echo "[$TIMESTAMP] Iniciando backup..."
docker exec "$CONTAINER" pg_dump -U "$DB_USER" "$DB_NAME" > "$FILE"

SIZE=$(du -sh "$FILE" | cut -f1)
echo "[$TIMESTAMP] Backup salvo: $FILE ($SIZE)"

# Remove backups mais antigos que 7 dias
find "$BACKUP_DIR" -name "backup_*.sql" -mtime +$KEEP_DAYS -delete
echo "[$TIMESTAMP] Backups antigos removidos (>$KEEP_DAYS dias)"
