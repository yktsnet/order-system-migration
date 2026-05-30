#!/usr/bin/env bash
set -euo pipefail

if [[ -f .env ]]; then
  set -a; source .env; set +a
fi

REMOTE="${DEPLOY_HOST:-sv6}"
REMOTE_USER="${DEPLOY_USER:-sv6}"
APP_PATH="/home/${REMOTE_USER}/github-public/order-system-migration"

echo "==> [1/3] ディレクトリ確保"
ssh "$REMOTE" "mkdir -p $APP_PATH"

echo "==> [2/3] ファイル転送"
rsync -az --delete \
  --exclude='.git' \
  --exclude='node_modules' \
  --exclude='volume' \
  --exclude='.env' \
  . "$REMOTE:$APP_PATH/"

echo "==> [3/3] .env 転送 + docker compose up --build"
rsync -az .env "$REMOTE:$APP_PATH/.env"
ssh "$REMOTE" "cd $APP_PATH && docker compose up -d --build"

echo "==> done"
