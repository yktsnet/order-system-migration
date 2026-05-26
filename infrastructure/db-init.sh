#!/usr/bin/env bash
set -euo pipefail
VPS="widget-vps"
echo "==> DB schema apply"
ssh "$VPS" "PGPASSWORD=demo1234 psql -h 127.0.0.1 -U postgres -d HANBAI" \
  < infrastructure/db/init/01_schema.sql
echo "==> done"
