#!/usr/bin/env bash
set -euo pipefail

REMOTE="${DEPLOY_HOST:-sv6}"
REMOTE_USER="${DEPLOY_USER:-sv6}"
APP_PATH="/home/${REMOTE_USER}/github-public/order-system-migration"

echo "==> DB seed data apply"
ssh "$REMOTE" "docker exec -i \$(docker compose -f $APP_PATH/docker-compose.yml ps -q db) \
  psql -U postgres -d HANBAI" \
  < infrastructure/db/seed/02_seed.sql

echo "==> done"
