# 🚀 C# Modernization Sandbox: Cloud Native Foundation

本リポジトリは、WSL2環境下でLocalStackとTerraformを活用し、AWS実機を使わずにクラウドネイティブなC#開発を行うための共通基盤である。

## 🏗️ Architecture Overview

本基盤は以下の要素が連携するエコシステムとして構成されている。

1.  **Infrastructure (LocalStack & Terraform)**
    * Docker Compose上で起動する擬似AWS環境。
    * Terraform (HCL) により、S3バケットなどのリソースをコードで管理。
2.  **Containerization (Docker)**
    * .NET 8/9 アプリケーションをマルチステージビルドでコンテナ化。
    * 環境変数による接続先（LocalStack/本番AWS）の動的切り替え。
3.  **CI/CD Pipeline (Custom Webhook)**
    * VPS上のGitBucketへのPushをトリガーとした自作自動化フロー。
    * 自動テスト (xUnit) 成功時のみコンテナをデプロイする安全なパイプライン。

## 📂 Project Structure (Monorepo)

本リポジトリは複数のC#プロジェクトを管理するモノリポ構成を採用している。

.
├── docker-compose.yml      # 共通インフラ（LocalStack）および全アプリの定義
├── main.tf                 # Terraformによるクラウドリソース定義
├── ci.sh                   # 自動テスト・ビルド・デプロイを制御する指揮者
├── webhook_listener.py     # GitBucketからの通知を受け取る窓口
├── CloudNativeApp/         # C#プロジェクト 1（Project B 実装）
├── CloudNativeApp.Tests/   # プロジェクト 1 用のテスト
└── [FutureProjects]/       # 今後追加される Project A 等の各プロジェクト

## 🚀 Getting Started

### 1. インフラの起動
ルートディレクトリにて、LocalStackおよび各コンテナを起動する。

docker compose up -d

### 2. リソースの適用
Terraformを用いて、LocalStack内に必要なAWSリソースを作成する。

terraform init
terraform apply -auto-approve

## 🔗 CI/CD Workflow

本基盤における自動化フローは以下の通りである。

1.  **Code Push**: ローカルでの開発完了後、`gb-push` でGitBucketへ送信。
2.  **Webhook Notification**: GitBucketからSSHトンネルを通じてWSL2のリスナーへ通知。
3.  **Automated Testing**: `ci.sh` が起動し、`dotnet test` を実行。
    * プロジェクトが増えた場合、`ci.sh` にテスト対象を追加することで、全プロジェクトの品質を担保する。
4.  **Auto Deployment**: 全テスト合格時のみ、`docker compose up -d --build` によりコンテナが最新版へ更新される。

## 💡 Key Design Patterns

* **Infrastructure as Code (IaC)**: インフラの変更はすべて `main.tf` を通じて行い、手動設定を排除する。
* **Environment Abstraction**: アプリケーション側は接続先を意識せず、環境変数 `AWS__ServiceURL` を参照して動作する。
* **Fail-Safe Deployment**: テストが1つでも失敗した場合、古いコンテナを維持し、壊れたコードが反映されるのを防ぐ。
