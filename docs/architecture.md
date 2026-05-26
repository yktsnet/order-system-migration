# アーキテクチャ概要

## Before: WinForms 密結合

```mermaid
graph TD
    subgraph OrderForm["❌ OrderForm.cs（1クラスがすべてを担当）"]
        EVT["UIイベント処理\nTextChanged / btnSave_Click"]
        BL["ビジネスロジック\n税計算・在庫更新"]
        SQL["SQL文字列結合\nインジェクションリスク"]
    end
    SQL -->|同期通信 → UIフリーズ| DB[("SQL Server")]
    EVT -->|LPT1ポート直指定| HW["ハードウェア依存\n（Windows専用印刷）"]
```

## After: レイヤー分離

```mermaid
graph LR
    React["React / TypeScript\n(UI層)"]
    API["ASP.NET Core\nMinimal API\n(API層)"]
    SVC["OrderService\n(Service層)"]
    DAP["Dapper\n(Repository層)"]
    DB[("PostgreSQL")]

    React -->|HTTP / JSON| API
    API --> SVC
    SVC --> DAP
    DAP --> DB
```

---

## コンポーネント責務

| コンポーネント | 責務 |
|---|---|
| React (src/Web) | 状態管理・表示のみ。ビジネスロジック不所持 |
| Minimal API (Program.cs) | ルーティング・リクエスト受付 |
| OrderService | 税計算・トランザクション管理・在庫更新 |
| Dapper | パラメータ化クエリによる安全なDBアクセス |
| Docker Compose | 環境依存の排除。ローカル〜本番同一構成 |

---

## インフラ構成

```mermaid
graph LR
    User["ブラウザ"] -->|HTTPS| CF["Cloudflare Tunnel"]
    CF --> Container["Docker Compose\napi + postgres + localstack"]
    Container -->|Terraform管理| Infra["infrastructure/\n(HCL)"]
```
