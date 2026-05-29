# .NET WinForms Migration（発注システム）

[![CI](https://github.com/kyamakawa-widget/dotnet-winforms-migration/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/kyamakawa-widget/dotnet-winforms-migration/actions/workflows/ci.yml)

レガシーな Windows 業務アプリ（WinForms）を題材に、`.NET 8 Web API + React` への段階的移行、さらに **Python Agent による自然言語インターフェース** の追加まで、一連のモダナイゼーション・プロセスを実践するためのサンプルプロジェクト。

[dotnet-webforms-migration](https://github.com/kyamakawa-widget/dotnet-webforms-migration)（WebForms 移行）の姉妹リポ。WinForms 固有の問題（UI フリーズ・LPT1 依存・画面クラスへのロジック集中）の解体と再構成に加え、**責務分離が完了した構造への AI 機能の追加統合**まで扱う。

---

## 1. 概要とゴール

本プロジェクトの目的は、単なる画面の作り替えではなく、**「密結合なレガシーコードをいかに解体し、モダンなアーキテクチャへ再構成するか」** のプロセスを提示することにある。

**Demo:** https://preceding-camel-remains-traveler.trycloudflare.com/  
※ WinForms After / WebForms After で URL を共用。どちらか一方が稼働中。  
**API ドキュメント (Swagger UI):** `/api-docs`

### 実践のポイント

- **解読**: 画面・SQL・業務ロジックが混在したコードの課題特定
- **分離**: UI、Service、Repository 層への責務分離
- **刷新**: .NET 8 Web API と React による再構築
- **品質**: テスタビリティの確保と単体テストの導入
- **拡張**: 責務分離が完了した構造への AI 機能の追加統合

---

## 2. Before: レガシーな密結合の実態

`legacy/LegacyWinFormsApp/` では、古い業務アプリに典型的な「1つのクラスがすべてを知りすぎている」状態を再現。

### 業務の背景

- 発注業務は Windows 端末上の専用アプリ（WinForms）で完結していた
- 履歴確認・検索は別端末・別システムで行っていた
- 月次集計・得意先ランキング等の分析は、CSV エクスポート後に Excel で手動加工していた
- 「先月カテゴリ別の売上は？」の問いに答えるには、担当者が Excel を開いて加工する必要があった

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

```mermaid
graph TD
    Browser["Windows端末"]
    subgraph OrderForm["❌ OrderForm.cs（1クラスがすべてを担当）"]
        EVT["UIイベント処理\nTextChanged / btnSave_Click"]
        BL["ビジネスロジック\n税計算・在庫更新"]
        SQL["SQL文字列結合\nインジェクションリスク"]
    end
    Browser --> OrderForm
    SQL -->|"同期通信 → UIフリーズ"| DB[("SQL Server")]
    EVT -->|"LPT1ポート直指定"| HW["ハードウェア依存\n（Windows専用印刷）"]
```

> **Before コードについて**  
> `legacy/OrderForm.cs` には実際の WinForms コード（コメント付き）を収録。実行環境は不要で、コードレベルの問題を読み取るためのリファレンスとして機能する。

---

## 3. After Phase 1 — モダンアーキテクチャへの転換

移行後は責務に応じてコンポーネントを完全に分離し、Windows 環境依存・UI フリーズ・SQL インジェクションリスクを排除する。

### 移行アプローチ

- **UI とロジックの完全分離**: 画面から DB へ直接アクセスせず、すべて API 経由で非同期処理。
- **Service 層の導入**: 税計算・在庫確認・トランザクション管理を `OrderService` へ切り出し、単体テストを可能にする。
- **安全なデータアクセス**: Dapper のパラメータ化クエリで SQL インジェクションを根絶。
- **合計のリアルタイム計算**: `TextChanged` での同期 DB 通信を廃止。フロント側で即時計算。
- **ポータビリティ**: Docker 化により、Windows 専用制約（LPT1 等）を排除。

```mermaid
graph LR
    React["React / TypeScript\n(UI層)"]
    API["ASP.NET Core\nMinimal API\n(API層)"]
    SVC["OrderService\n(Service層)"]
    DAP["Dapper\n(Repository層)"]
    DB[("PostgreSQL")]
    React -->|"HTTP / JSON\n非同期・UIフリーズなし"| API
    API --> SVC
    SVC --> DAP
    DAP --> DB
```

### 計算ロジックの分離（テスタビリティ）

`TaxService` を `OrderService` から独立させ、DB 接続なしで計算ロジック単体をテスト可能にしている。

```
OrderService（DBアクセス・トランザクション管理）
    └── TaxService（純粋計算）← xUnit が直接テスト（境界値 7 ケース）
```

### 実装エンドポイント

| Method | Path | 説明 |
|---|---|---|
| GET | `/categories` | カテゴリマスタ取得 |
| GET | `/orders` | 受注履歴一覧（得意先名・商品名・カテゴリ・期間でフィルタ可） |
| GET | `/orders/export` | 受注履歴 CSV エクスポート（フィルタ条件を引き継ぎ・UTF-8 BOM） |
| POST | `/orders` | 受注登録（在庫更新をトランザクション内で実行） |
| DELETE | `/orders/{orderNo}` | 受注取消（在庫自動復元をトランザクション内で実行） |

---

## 4. After Phase 2 — AI 自然言語インターフェース

密結合のままでは AI を独立したコンポーネントとして追加できない。Phase 1 の分離が完了した構造を前提に、「CSV → Excel 手動集計」という運用を自然言語インターフェースで置き換える。

### Before / After

| Before (WinForms + Excel) | After (.NET 8 + React + Agent) |
|---|---|
| フィルタ操作 → CSV エクスポート → Excel 手動集計 | 自然言語で問うと即答が返る |
| 得意先ランキングは担当者が加工して初めて判明 | 「ランキングは？」の一言で回答 |
| 事前に画面を作っていない集計軸には対応不可 | スキーマが同じなら任意の集計が可能 |

### 全体構成

```
【Phase 1】
React → .NET 8 API → PostgreSQL

【Phase 2 追加】
React → .NET 8 API → PostgreSQL
      ↘
        Python FastAPI (Agent) → LangGraph → PostgreSQL
```

React は .NET API と Python Agent それぞれに直接通信する。AI 推論を .NET 経由にせず、業務ロジックと推論の責務を明確に分離したまま維持。

### LangGraph フロー

```mermaid
flowchart TD
    START([START]) --> classify_intent[classify_intent\nDB問い合わせか判定]
    classify_intent -->|対象外| END_NG([END])
    classify_intent -->|対応可能| generate_sql[generate_sql\nLLMがSELECT文生成]
    generate_sql --> validate_sql[validate_sql\nSELECT以外を検証]
    validate_sql -->|NG| END_ERR([END])
    validate_sql -->|OK| execute_sql[execute_sql\nPostgreSQLへ実行]
    execute_sql -->|成功| format_response[format_response\n結果を自然言語で整形]
    execute_sql -->|失敗| handle_error[handle_error\n最大2回リトライ]
    handle_error -->|リトライ| generate_sql
    handle_error -->|上限到達| END_ERR
    format_response --> END_OK([END])
```

### Agent の内部構成

```
src/Agent/
├── main.py           # FastAPI エントリポイント
├── agent.py          # LangGraph グラフ定義
├── schema_prompt.py  # DB スキーマをプロンプト文字列で定義
├── db.py             # PostgreSQL 接続（psycopg2）
├── requirements.txt
└── Dockerfile
```

### 主な設計判断

- **LLM は Gemini API**: 低コストでデモ環境を継続稼働できる。
- **Text-to-SQL（RAG ではない）**: 対象が構造化された PostgreSQL スキーマのため、ベクトル検索より SQL 生成が適切。
- **SELECT 文のみ許可**: `validate_sql` ノードで DDL/DML を弾き、DB への副作用を排除。
- **リトライ最大 2 回**: SQL 実行失敗時に `generate_sql` へ戻り、エラー内容を LLM へフィードバック。
- **エージェントログ**: フロー終了時に `AgentLog` テーブルへ記録。生成 SQL の失敗パターン把握・異常 SQL の監査に使用。
- **責務分離の維持**: 業務ロジックに手を入れることなく自然言語インターフェースを統合。

### 対応する質問例

- 「先月の受注件数は？」
- 「カテゴリ別の売上合計を教えて」
- 「得意先ランキングトップ3は？」
- 「今月一番高い単価の商品は？」
- 「在庫が 50 以下の商品は？」

---

## 5. 技術スタック

| Layer | Technology |
|---|---|
| **Frontend** | React, TypeScript, Vite, Tailwind CSS |
| **Backend** | .NET 8 (Minimal API), xUnit |
| **AI Agent** | Python, FastAPI, LangGraph, Gemini API |
| **Database** | PostgreSQL (Dapper / psycopg2) |
| **Object Storage** | LocalStack (AWS S3 互換) |
| **Infrastructure** | Docker Compose, Terraform, Cloudflare Tunnel, GitHub Actions |

---

## 6. モダナイゼーションの方針

1. **ロジックの軽量抽出 (Minimal API)**: 巨大な `OrderForm.cs` を疎結合な Web API へ分解。
2. **環境の抽象化 (IaC)**: Terraform を用い、特定のサーバー環境への依存を排除。
3. **ポータビリティ (Docker)**: 「Windows でしか動かない」制約を破壊し、クラウドへの道を確保。
4. **セーフティネット (Unit Test)**: 既存機能を壊さずにリファクタリングするための武器を装備。
5. **CI/CD のパイプライン化 (GitHub Actions)**: push ごとにビルド・テストを自動実行し、品質を継続的に担保。
6. **AI 統合の容易化**: 責務分離が完了した構造では、AI サービスを独立したコンポーネントとして追加できる。業務ロジックに手を入れることなく自然言語インターフェースを統合したことがその実証。

> **Focus & Scope**  
> 本プロジェクトは **「レガシー資産の解体と構造分離」** に特化。  
> 認証・認可の本格実装・本番用 DB の冗長化構成・会話履歴管理は **対象外 (Out-of-Scope)**。
>
> **Agent の認証について**  
> Python Agent（`/chat`）は意図的に認証なしで実装。推論サービスは推論のみに専念させる責務分離の方針により、認証は .NET API 側で一元管理する設計を想定。本番化する際の構成は `React → .NET /chat [Authorize] → localhost:8001/chat`。Agent は VPS 内部からのみ受け付ける前提。

---

## 7. デモ運用

**Demo:** https://preceding-camel-remains-traveler.trycloudflare.com/  
※ WinForms After / WebForms After で URL を共用。どちらか一方が稼働中。

[dotnet-webforms-migration](https://github.com/kyamakawa-widget/dotnet-webforms-migration)（WebForms After）と本リポ（WinForms After）は、**同一の Cloudflare Tunnel・同一 URL** を共用している。Tunnel は常時稼働のまま、背後の systemd サービスを排他的に切り替えることで、URL を変えずに 2 つの After デモを提示できる。

```mermaid
graph LR
    User["ブラウザ"]
    Tunnel["Cloudflare Tunnel\n同一 URL / localhost:5153"]
    subgraph VPS["VPS"]
        SVC1["winforms-migration.service\nWinForms After"]
        SVC2["webforms-migration.service\nWebForms After"]
        DB[("PostgreSQL")]
    end
    User -->|"HTTPS"| Tunnel
    Tunnel -->|"active 時のみ"| SVC1
    Tunnel -->|"active 時のみ"| SVC2
    SVC1 --> DB
    SVC2 --> DB
```

常にどちらか一方のみ active（`switch-demo.sh` で排他切替）。Tunnel の設定変更・URL 再発行なしに両デモを切り替えられる。

---

## 8. dotnet-webforms-migration との対比

| | dotnet-winforms-migration（本リポ） | [dotnet-webforms-migration](https://github.com/kyamakawa-widget/dotnet-webforms-migration) |
|---|---|---|
| **Before** | WinForms（デスクトップ） | WebForms（レガシー Web） |
| **問題の性質** | 実行時に表面化する問題 | 稼働しながら蓄積する構造的負債 |
| **レガシー固有の問題** | UI フリーズ・LPT1 依存 | AutoPostBack・ViewState |
| **業務ドメイン** | 受注管理 | 勤怠管理 |
| **Phase 2 の拡張** | AI 自然言語インターフェース | SignalR リアルタイム機能 |
| **共通の問題** | コードビハインド密結合・SQL インジェクション・テスト不能 ||

---

## 9. ディレクトリ構造

```
.
├── .github/
│   └── workflows/
│       └── ci.yml                        # CI（.NET テスト + React ビルド）
├── docs/
│   └── design.md                         # UI デザイン方針（カラー・コンポーネント規則）
├── infrastructure/
│   ├── db/
│   │   ├── init/
│   │   │   └── 01_schema.sql             # DB 初期化（テーブル定義・AgentLog 含む）
│   │   └── seed/
│   │       ├── generate_seed.py          # サンプルデータ生成スクリプト
│   │       └── 02_seed.sql               # 生成済みサンプルデータ（400件・6ヶ月分）
│   ├── agent-setup.sh                    # VPS 初回セットアップ（venv 構築・systemd 登録）
│   ├── deploy.sh                         # WSL → VPS デプロイ（ビルド・転送・再起動）
│   ├── db-init.sh                        # DB 初期化（初回のみ）
│   ├── db-seed.sh                        # サンプルデータ投入
│   ├── main.tf                           # Terraform 定義（AWS ECS/RDS/S3 環境構築用）
│   ├── ci.sh                             # CI/デプロイ支援スクリプト
│   └── webhook_listener.py               # Webhook 受信・処理スクリプト
├── legacy/
│   └── LegacyWinFormsApp/
│       └── OrderForm.cs                  # Before（変更なし・コードレベルの問題リファレンス）
├── src/
│   ├── Agent/                            # Phase 2: Python FastAPI + LangGraph AI Agent
│   │   ├── main.py
│   │   ├── agent.py
│   │   ├── schema_prompt.py
│   │   ├── db.py
│   │   ├── requirements.txt
│   │   └── Dockerfile
│   ├── Api/                              # After: .NET 8 Minimal API
│   │   ├── Endpoints/
│   │   ├── Services/
│   │   │   ├── OrderService.cs
│   │   │   └── TaxService.cs
│   │   ├── Program.cs
│   │   └── Dockerfile
│   ├── Api.Tests/                        # xUnit テスト
│   │   └── OrderServiceTests.cs          # TaxService 境界値テスト（DB 不要・7 ケース）
│   └── Web/                              # After: React Frontend
│       └── src/
│           ├── App.tsx
│           ├── ChatPanel.tsx
│           └── types.ts
├── docker-compose.yml
└── README.md
```
