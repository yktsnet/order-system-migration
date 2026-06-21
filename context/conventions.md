# order-system-migration 開発規約

コードの書き方・編集の共通ルール（どう書くか）。ディレクトリ構成・データフローは `structure.md` を参照。

## 1. 技術スタック
- **バックエンド**: .NET 8 Minimal API。テストは xUnit。
- **フロントエンド**: React + TypeScript + Vite + Tailwind CSS。
- **AI Agent**: Python + FastAPI + LangGraph（Gemini API、Text-to-SQL）。
- **データベース**: PostgreSQL（.NET 側 Dapper / Agent 側 psycopg2）。
- **オブジェクトストレージ**: LocalStack（AWS S3 互換）。
- **インフラ**: Docker Compose、Terraform、GitHub Actions、Cloudflare Tunnel、NixOS（オンプレ）。

## 2. コードスタイル
- C# は `dotnet format` でフォーマットを統一する。Nullable 参照型を有効にした前提で書く。
- ドメイン計算（税計算・受注ロジック）は `Services/TaxService.cs`・`OrderService.cs` に集約し、エンドポイントから切り離してテスト可能に保つ。
- 受注操作は「取り消し」と「登録」の2アクションに明示分離する。誤操作による不整合を避けるため、既存受注の直接修正処理は実装しない。
- Python Agent は Text-to-SQL で **SELECT 文のみ許可**。生成・実行した SQL は AgentLog に監査記録する。LLM・DB はテストではモックする。
- TypeScript は型を明示し、API レスポンスの型は `src/Web/src/types.ts` に集約する。

## 3. ファイル編集戦略
- **広範囲の書き換え**: 変更箇所が多い場合（目安: 10箇所以上、またはファイルの20%超）、`str_replace` の繰り返しではなく `bash` でファイル全体を一括書き出す（`cat > path << 'EOF'` 等）。
- **局所的修正**: 数行以内の修正に限定してツールを使用。
- **静的チェック**: C# は `dotnet build src/Api`、計算ロジック変更時は `dotnet test src/Api.Tests`。Python Agent は `python -m py_compile *.py` と `python -m pytest`。
- **Nix 環境前提**: ホストは Nix 管理。`pip install` / `npm install -g` 等のグローバルインストールは禁止（環境を汚す）。標準で入っていないツールが要る場合のみ、使い捨てシェル `nix-shell -p {pkg} --run "..."` で実行する。
