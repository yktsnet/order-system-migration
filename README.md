# .NET WinForms Migration

[![CI](https://github.com/kyamakawa-widget/dotnet-modernization-lab/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/kyamakawa-widget/dotnet-modernization-lab/actions/workflows/ci.yml)

レガシーな Windows 業務アプリ（WinForms）を題材に、`.NET 8 Web API + React` への段階的移行を実践するためのサンプルプロジェクトです。

---

## 1. 概要とゴール

本プロジェクトの目的は、単なる画面の作り替えではなく、**「密結合なレガシーコードをいかに解体し、モダンなアーキテクチャへ再構成するか」** のプロセスを提示することにあります。

**Demo:** https://preceding-camel-remains-traveler.trycloudflare.com/  
**API ドキュメント (Swagger UI):** https://preceding-camel-remains-traveler.trycloudflare.com/api-docs

### 実践のポイント

- **解読**: 画面・SQL・業務ロジックが混在したコードの課題特定
- **分離**: UI、Service、Repository、データアクセス層への構造分離
- **刷新**: .NET 8 Web API と React による最新スタックへの移行
- **品質**: テスタビリティの確保と単体テストの導入

---

## 2. Before: レガシーな密結合の実態

`legacy/LegacyWinFormsApp/` では、古い業務アプリに典型的な「1つのクラスがすべてを知りすぎている」状態を再現しています。

### 構成イメージ

```
+-----------------------------------------------------------+
| [ 受注登録画面 ]                                     [×] |
+-----------------------------------------------------------+
| 受注番号: [ 20260522-001 ]  [ 検索(btnSearch) ] ← 2秒固まる |
| 得意先名: [ 株式会社大阪商事         ]                    |
| カテゴリ: [ 事務用品          ▼ ] ← 画面起動時にDBから取得 |
| 商品名称: [ 高性能オフィスチェア      ]                    |
| -------------------------------------------------------  |
| 単価: [ 85,000 ]  数量: [ 12 ]  在庫: [ 在庫：102 ]       |
|                                     ↑ TextChangedでDB通信 |
| -------------------------------------------------------  |
| 小計:   1,020,000 円                                      |
| 消費税:   102,000 円                                      |
| 合計:   1,122,000 円 ← [ 100万超えで文字が赤くなる ]      |
| -------------------------------------------------------  |
| [ 保存 ]  [ 削除 ]               [ 伝票印刷(LPT1) ]      |
| (保存ボタンの中に、SQL結合・在庫更新・トランザクションが全入り) |
+-----------------------------------------------------------+
```

### 主な課題点

- **UI イベント内の重い処理**: `TextChanged` 等での同期 DB 通信により UI がフリーズする。
- **SQL インジェクションのリスク**: 文字列結合による SQL 組み立て。
- **ドメインロジックの散逸**: 税計算や在庫更新が画面クラスに直接記述され、再利用やテストが不能。
- **ハードウェア依存**: LPT1 ポート指定など、特定の実行環境（Windows 端末）に強く依存。

---

## 3. After: モダンアーキテクチャへの転換

移行後は、責務に応じてコンポーネントを完全に分離し、Web 標準の技術スタックで再構築します。

### 構成

1. **Frontend (React/TypeScript)**: 状態管理と UI 表示に専念。
2. **Backend (ASP.NET Core)**: 業務ロジック（Service 層）とデータアクセスを隠蔽。
3. **Database**: 疎結合なアクセス（Dapper）。
4. **Object Storage**: AWS S3 互換の LocalStack をローカル環境で使用。

### 移行アプローチ

- **UI とロジックの完全分離**: 画面から DB へ直接アクセスせず、すべて API 経由で非同期に処理。
- **Service 層の導入**: 税計算や在庫確認を独立したクラスへ切り出し、単体テストを可能にする。
- **安全なデータアクセス**: パラメータ化クエリ（Dapper）を使用。
- **ポータビリティ**: Docker 化により、実行環境に依存しないデプロイを実現。

### 実装された主な API エンドポイント

| Method | Path | 説明 |
| ------ | ---- | ---- |
| GET | `/categories` | カテゴリマスタ取得 |
| GET | `/orders` | 受注履歴一覧取得（得意先名・商品名・カテゴリ・期間でフィルタ可） |
| GET | `/orders/export` | 受注履歴 CSV エクスポート（フィルタ条件を引き継ぎ） |
| POST | `/orders` | 受注登録（在庫更新をトランザクション内で実行） |
| DELETE | `/orders/{orderNo}` | 受注取消（在庫自動復元をトランザクション内で実行） |

### 実装された主な新機能

- **注文履歴の可視化**: 過去の受注データを一覧で確認できる「注文履歴」タブを新設。
- **履歴の検索・フィルタ**: 得意先名・商品名（部分一致）、カテゴリ、期間を組み合わせて絞り込み可能。
- **CSV エクスポート**: フィルタ条件を維持したまま受注履歴を CSV でダウンロード。Excel で直接開ける UTF-8 BOM 付き。
- **安全な取消処理**: 履歴一覧からの削除アクションに連動し、API のトランザクション内で在庫の自動復元を安全に実行。

---

## 4. 技術スタック

| Layer              | Technology                             |
| ------------------ | -------------------------------------- |
| **Frontend**       | React, TypeScript, Vite, Tailwind CSS  |
| **Backend**        | .NET 8 (Minimal API), xUnit            |
| **Database**       | PostgreSQL (Dapper)                    |
| **Object Storage** | LocalStack (AWS S3 互換)               |
| **Infrastructure** | Docker Compose, Terraform, Cloudflare Tunnel |

---

## 5. モダナイゼーションの方針

本プロジェクトでは、単なるコード書き換えではなく、以下の 6 つの柱を軸に「開発プロセス」を移行を実践します。

1. **ロジックの軽量抽出 (Minimal API)**: 巨大な `code-behind` を疎結合な Web API へ分解。
2. **環境の抽象化 (IaC)**: Terraform を用い、特定のサーバー環境への依存を排除。
3. **ポータビリティ (Docker)**: 「Windows でしか動かない」制約を破壊し、クラウドへの道を確保。
4. **セーフティネット (Unit Test)**: 既存機能を壊さずにリファクタリングするための武器を装備。
5. **検証容易性の確保**: Service 層と単体テストにより、変更時の影響を確認しやすくする。
6. **CI/CD のパイプライン化 (GitHub Actions)**: 自動でビルド・テストを実行し、品質を継続的に担保する仕組みを導入。

> **Focus & Scope**  
> 本プロジェクトは **「レガシー資産の解体と構造分離」** に特化しています。  
> 「認証・認可 (Auth0 等)」や「本番用 DB の冗長化構成」などは **対象外 (Out-of-Scope)** としています。

> **インフラ補足**: デプロイは infrastructure/deploy.sh（WSL から rsync + systemctl）で実行。  
> DB の初期化（初回のみ）は infrastructure/db-init.sh を別途実行。  
> 本番想定では Cloudflare Zero Trust による独自ドメイン＋アクセス制御、  
> または Terraform 定義を AWS (ECS/RDS/S3) へ拡張してデプロイ。

---

## 6. ディレクトリ構造

```
.
├── .github/
│   └── workflows/               # CI/CD パイプライン定義（GitHub Actions: 自動ビルド・テスト）
├── docs/
│   ├── architecture.md          # アーキテクチャ図（Mermaid）
│   ├── design.md                # UI デザイン方針（カラー・コンポーネント規則）
│   └── migration-plan.md        # 移行フェーズ定義
├── infrastructure/              # IaC・インフラストラクチャ定義
│   ├── db/
│   │   ├── init/
│   │   │   └── 01_schema.sql        # データベース初期化用 SQL
│   │   └── seed/
│   │       ├── generate_seed.py     # サンプルデータ生成スクリプト
│   │       └── 02_seed.sql          # 生成済みサンプルデータ（400件）
│   ├── deploy.sh                # WSL → VPS デプロイスクリプト（ビルド・転送・再起動）
│   ├── db-init.sh               # DB 初期化スクリプト（初回のみ実行）
│   ├── db-seed.sh               # サンプルデータ投入スクリプト
│   ├── main.tf                  # Terraform 定義（AWS ECS/RDS/S3 等の環境構築用）
│   ├── ci.sh                    # CI/デプロイ支援スクリプト
│   └── webhook_listener.py      # Webhook 受信・処理スクリプト
├── legacy/
│   └── LegacyWinFormsApp/       # Before: 密結合な WinForms 業務アプリのサンプルコード
├── src/
│   ├── Api/                     # After: .NET 8 Web API (Minimal API / Service 層)
│   ├── Api.Tests/               # xUnit による Service 層の単体テスト
│   └── Web/                     # After: React Frontend (Vite / TypeScript / Tailwind CSS)
├── docker-compose.yml           # ローカル開発用コンテナ構成（API / PostgreSQL / LocalStack）
```
