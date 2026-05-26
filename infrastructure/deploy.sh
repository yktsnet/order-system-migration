#!/usr/bin/env bash
set -euo pipefail
VPS="widget-vps"
VPS_APP_DIR="/home/k_yamakawa/ops/modernization-lab"
PUBLISH_DIR="./publish/api"
echo "==> [1/4] .NET publish"
dotnet publish src/Api/CloudNativeApp.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "$PUBLISH_DIR"
echo "==> [2/4] React build"
(cd src/Web && npm ci && npm run build)
echo "==> [3/4] rsync API"
rsync -az --delete \
  --exclude='appsettings*.json' \
  "$PUBLISH_DIR/" \
  "$VPS:$VPS_APP_DIR/"
echo "==> [4/4] rsync frontend"
rsync -az --delete \
  src/Web/dist/ \
  "$VPS:$VPS_APP_DIR/wwwroot/"
echo "==> [5/5] service restart"  
ssh "$VPS" "sudo systemctl restart modernization-lab.service && systemctl is-active modernization-lab.service"
echo "==> done"
