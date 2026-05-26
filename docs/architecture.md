# アーキテクチャ概要

## Before → After

```mermaid
graph TD
    subgraph Before["❌ Before: WinForms 密結合"]
        UI["OrderForm.cs\n(画面)"]
        UI -->|SQL直書き| DB1[("SQL Server")]
        UI -->|ビジネスロジック| UI
        UI -->|LPT1印刷| HW["ハードウェア依存"]
    end

    subgraph After["✅ After: レイヤー分離"]
        React["React / TypeScript\n(UI層)"]
        API["ASP.NET Core\nMinimal API\n(API層)"]
        SVC["OrderService\n(Service層)"]
        DAP["Dapper\n(Repository層)"]
        DB2[("PostgreSQL")]

        React -->|HTTP / JSON| API
        API --> SVC
        SVC --> DAP
        DAP --> DB2
    end
```

## コンポーネント責務

| コンポーネント | 責務 |
|---|---|
| React (src/Web) | 状態管理・表示のみ。ビジネスロジック不所持 |
| Minimal API (Program.cs) | ルーティング・リクエスト受付 |
| OrderService | 税計算・トランザクション管理・在庫更新 |
| Dapper | パラメータ化クエリによる安全なDBアクセス |
| Docker Compose | 環境依存の排除。ローカル〜本番同一構成 |

## インフラ構成

```mermaid
graph LR
    User["ブラウザ"] -->|HTTPS| CF["Cloudflare Tunnel"]
    CF --> Container["Docker Compose\napi + web + postgres"]
    Container -->|Terraform管理| Infra["infrastructure/\n(HCL)"]
```
