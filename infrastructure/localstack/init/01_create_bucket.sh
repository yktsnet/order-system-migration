#!/bin/bash
# LocalStack 起動完了後に自動実行される
# awslocal は LocalStack 同梱の AWS CLI ラッパー（エンドポイント指定不要）
awslocal s3 mb s3://order-exports
echo "[localstack-init] bucket order-exports created"
