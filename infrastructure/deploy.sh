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
  "$PUBLISH_DIR/" \
  "$VPS:$VPS_APP_DIR/"
echo "==> [4/5] rsync frontend"
rsync -az --delete \
  -e "ssh -o ClearAllForwardings=yes" \
  src/Web/dist/ \
  "$VPS:$VPS_APP_DIR/wwwroot/"
echo "==> [5/5] rsync Agent"
rsync -az --delete \
  -e "ssh -o ClearAllForwardings=yes" \
  --exclude='.env' \
  --exclude='.venv/' \
  --exclude='__pycache__/' \
  --exclude='*.pyc' \
  src/Agent/ \
  "$VPS:$VPS_APP_DIR/agent/"
echo "==> done"
echo ""
echo "=========================================="
echo " サービス再起動は VPS 上で手動実行:"
echo "   sudo systemctl restart modernization-lab.service"
echo "   sudo systemctl restart modernization-agent.service"
echo "   systemctl is-active modernization-lab.service"
echo "   systemctl is-active modernization-agent.service"
echo "=========================================="
