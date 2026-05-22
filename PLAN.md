# 🚀 C# Modernization & Cloud Infrastructure Training Plan

本計画は、**「実機（AWS）への課金ゼロ」**かつ**「Windows/WSL2環境」**で完結しつつ、モダンなC#開発者として最高レベルの市場価値（レガシー移行・クラウドネイティブ化）を習得するための独立した2つのプロジェクト構成である。

---

## 🏗️ Project A: Legacy Code Modernization
**【目的】** 過去の C# 資産を解読し、.NET 8/9 基準のクリーンなアーキテクチャへ再構築する。

### 1. ターゲット選定（考古学フェーズ）
* **GitHub 探索:** `targetFramework="v4.5"` 等を含む、5〜10年前のメンテナンス停止リポジトリを Fork。
* **AI 静的解析:** AI エージェントにコードを読み込ませ、以下の項目を抽出・レビューする。
    * **密結合:** `new` 演算子による依存関係のハードコード箇所。
    * **同期ボトルネック:** `Thread.Sleep` や非同期化されていない I/O 処理。
    * **レガシー設定:** `web.config` や `Global.asax` に埋もれた設定・初期化ロジック。

### 2. モダン・リプレイス（建築フェーズ）
* **Minimal API 移行:** .NET 8/9 のトップレベルステートメントを用いた、ボイラープレートレスな Web API への刷新。
* **DI (Dependency Injection) 実装:** サービスコレクションを用いた疎結合な設計への再定義。
* **データアクセス近代化:** レガシーな ADO.NET や Dapper 構成を、Entity Framework Core 8 (PostgreSQL) へ移植。
* **AI レビュー:** AI に「モダンなデザインパターン（Repository, Unit of Work等）」を適用させ、その意図を読み解く。

---

## ☁️ Project B: Cloud Native Transformation
**【目的】** LocalStack を活用し、AWS 実機を使わずにプロフェッショナルなクラウドインフラ構築と CI/CD フローを習得する。

### 1. ローカルクラウド基盤の構築
* **LocalStack 起動:** Docker Compose を用い、WSL2 上に擬似 AWS 環境（S3, Lambda, RDS, SQS 等）を起動。
* **Terraform による IaC:** * 手動ポチポチを排し、HCL (HashiCorp Configuration Language) でインフラを定義。
    * LocalStack に対して `terraform apply` を実行し、環境構築をコードで完結させる。

### 2. クラウド最適化実装と自動化
* **AWS SDK 連携:** C# アプリに `AWSSDK` を導入。接続先を LocalStack (localhost:4566) に向け、S3 へのファイル保存や Lambda 連携を実装。
* **コンテナ・ストラテジー:** * `dotnet publish` を用いたマルチステージビルドによる軽量 Docker イメージの作成。
    * 開発・本番の環境差分を環境変数で吸収する設計の徹底。
* **GitHub Actions パイプライン:** * プッシュ時に自動で `dotnet test` を実行。
    * Docker イメージのビルド後、LocalStack 環境に対するデプロイ・シミュレーションの自動化。

---

## 🛠️ Technology Stack Summary
| 項目 | 採用技術 |
| :--- | :--- |
| **Language** | C# 12 / 13 |
| **Platform** | .NET 8 / 9 (Linux Runtime on WSL2) |
| **Infrastructure** | Docker, LocalStack (AWS Simulator), Terraform |
| **Database** | PostgreSQL (with pgvector for AI context) |
