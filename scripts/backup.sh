#!/bin/bash
# StickyNotes DB Backup Script
# Runs via cron every 6 hours, keeps 7 days of backups

BACKUP_DIR="/home/sdk/backups/stickynotes"
DB_PATH="/var/lib/stickynotes/notes.db"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

mkdir -p "$BACKUP_DIR"

# Use SQLite online backup (safe even while app is running)
sqlite3 "$DB_PATH" ".backup '$BACKUP_DIR/notes_$TIMESTAMP.db'"

# Delete backups older than 7 days
find "$BACKUP_DIR" -name "notes_*.db" -mtime +7 -delete

echo "Backup completed: notes_$TIMESTAMP.db"
