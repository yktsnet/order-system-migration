#!/usr/bin/env bash
set -euo pipefail
VPS="widget-vps"
VPS_APP_DIR="/home/k_yamakawa/ops/modernization-lab"
PUBLISH_DIR="./publish/api"

echo "==> [1/5] .NET publish"
dotnet publish src/Api/CloudNativeApp.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "$PUBLISH_DIR"

echo "==> [2/5] React build"
(cd src/Web && npm ci && npm run build)

echo "==> [3/5] rsync API"
rsync -az --delete \
  -e "ssh -o ClearAllForwardings=yes" \
  --exclude='appsettings*.json' \
  --exclude='wwwroot/' \
  --exclude='agent/' \
  --exclude='agent-venv/' \
  --exclude='agent.env' \
  "$PUBLISH_DIR/" \
  "$VPS:$VPS_APP_DIR/"

echo "==> [4/5] rsync frontend"
rsync -az --delete \
  -e "ssh -o ClearAllForwardings=yes" \
  src/Web/dist/ \
  "$VPS:$VPS_APP_DIR/wwwroot/"

echo "==> [5/5] rsync Agent"
rsync -az \
  -e "ssh -o ClearAllForwardings=yes" \
  --exclude='.env' \
  --exclude='.venv' \
  --exclude='__pycache__' \
  --exclude='*.pyc' \
  src/Agent/ \
  "$VPS:$VPS_APP_DIR/agent/"

echo ""
echo "==> restart"
ssh -o ClearAllForwardings=yes "$VPS" "
  if systemctl is-active --quiet winforms-migration.service; then
    sudo systemctl restart winforms-migration.service
    echo '  winforms-migration.service: restarted'
  else
    echo '  winforms-migration.service: inactive - skipped'
  fi

  if systemctl is-active --quiet modernization-agent.service; then
    sudo systemctl restart modernization-agent.service
    echo '  modernization-agent.service: restarted'
  else
    echo '  modernization-agent.service: inactive - skipped'
  fi
"

echo "==> done"
