# order-system-migration ディレクトリ構造

どこに何があるか。コードの書き方（規約）は `conventions.md` を参照。

## トップレベル

```
order-system-migration/
├── src/              # アプリ本体（Agent / Api / Web）
├── infrastructure/   # DB 初期化・シード・LocalStack・Terraform・デプロイ
├── legacy/           # 移行元 WinForms（Before の参照用・変更しない）
├── docs/design.md    # UI デザイン方針
├── docker-compose.yml # ローカル/本番コンテナ構成
├── context/          # Agent 向け共通コンテキスト（本ファイル群）
└── issues/           # ローカル Issue 管理（done/ に完了分と PR 控え）
```

## src/

```
src/
├── Api/                    # .NET 10 Minimal API
│   ├── Program.cs          # エントリポイント・DI・エンドポイント登録
│   ├── Endpoints/          # ルート定義
│   ├── Services/
│   │   ├── OrderService.cs # 受注の取り消し/登録
│   │   └── TaxService.cs   # 税計算（境界値テスト対象）
│   └── Dockerfile
├── Api.Tests/              # xUnit（TaxService 境界値テスト等、DB 不要）
├── Agent/                  # Python FastAPI + LangGraph AI Agent
│   ├── main.py             # FastAPI エントリ
│   ├── agent.py            # LangGraph グラフ・Text-to-SQL ロジック
│   ├── schema_prompt.py    # スキーマをプロンプトへ展開
│   ├── db.py               # DB アクセス（psycopg2・SELECT のみ）
│   └── tests/              # pytest（LLM/DB はモック）
└── Web/                    # React + TypeScript + Vite
    └── src/
        ├── App.tsx
        ├── ChatPanel.tsx   # 自然言語インターフェースの UI
        └── types.ts
```

## データフロー

```
通常操作:  Web(React) → API(.NET Endpoints) → OrderService/TaxService → PostgreSQL
AI 検索:   Web(ChatPanel) → Agent(FastAPI) → LangGraph(Text-to-SQL, SELECT のみ)
                                                      ↓
                                          PostgreSQL（読み取り）＋ AgentLog（監査記録）
ストレージ: API → LocalStack（S3 互換、AWS__ServiceURL 差し替えで本番 S3 へ）
```

## レイヤー構成

- **表示層**: `src/Web`（React/Vite）。通常操作と AI チャットの2系統。
- **API 層**: `src/Api/Endpoints`（Minimal API）。ドメイン計算は `Services` に分離。
- **AI 層**: `src/Agent`（FastAPI + LangGraph）。Text-to-SQL は SELECT のみ、AgentLog に監査。
- **永続層**: PostgreSQL。初期化/シードは `infrastructure/db/`。
- **ストレージ層**: LocalStack（S3 互換）。
- **IaC**: `infrastructure/main.tf`（Terraform）。

## issues/

- `{NN}_{slug}.md`: 実装対象 Issue。`status: open` のものを Agent が処理。
- `00_template.md`: Issue ひな形。
- `done/`: 完了 Issue と PR 控え（`{id}_{slug}_pr.md`）。
