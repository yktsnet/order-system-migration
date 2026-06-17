#!/usr/bin/env bash
set -euo pipefail
if [[ -z "${DEPLOY_HOST:-}" ]]; then
  echo "❌ Error: DEPLOY_HOST environment variable is not set." >&2
  exit 1
fi

if [[ -z "${DEPLOY_USER:-}" ]]; then
  echo "❌ Error: DEPLOY_USER environment variable is not set." >&2
  exit 1
fi

REMOTE="${DEPLOY_HOST}"
REMOTE_USER="${DEPLOY_USER}"
APP_PATH="${DEPLOY_PATH:-/home/${REMOTE_USER}/github-public/order-system-migration}"

echo "==> DB seed data apply"
ssh "$REMOTE" "docker exec -i \$(docker compose -f $APP_PATH/docker-compose.yml ps -q db) \
  psql -U postgres -d HANBAI" \
  < infrastructure/db/seed/02_seed.sql

echo "==> done"
