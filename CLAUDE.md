# CLAUDE.md

@context/conventions.md
@context/structure.md

Claude Code は本ファイルを最優先の指示として実行すること。

## 動作フロー
- 起動時に `issues/` 内の対象 Issue（`status: open`）を確認する。
- 実装開始前に `context/conventions.md` と `context/structure.md` を読み、規約と構造を把握する。
- ローカル環境にて `claude/{id}-{branch-slug}` ブランチ上で作業していることを認識する。
- 実装・検証・PR 作成はグローバルの `pr-workflow` スキル（`~/.claude/skills/pr-workflow/SKILL.md`）の手順に従う。

## コマンド
- API ビルド（構文・型チェック）: `dotnet build src/Api`
- API テスト: `dotnet test src/Api.Tests`（xUnit）
- フロントエンド型チェック/ビルド: `cd src/Web && npm ci && npm run build`
- Agent 構文チェック: `cd src/Agent && python -m py_compile *.py`
- Agent テスト: `cd src/Agent && python -m pytest`（LLM/DB はモック）

## アーキテクチャの要点
- 3 サービス構成: .NET 10 Minimal API（`src/Api`）/ React フロント（`src/Web`）/ Python AI Agent（`src/Agent`）。
- 受注操作は「取り消し」と「登録」の2アクションに明示分離（修正処理は許容しない）。ドメイン計算は `Services/TaxService.cs`・`OrderService.cs`。
- AI 自然言語インターフェースは LangGraph + Text-to-SQL（`src/Agent/agent.py`）。発行する SQL は SELECT のみ許可、実行は AgentLog に監査記録する。
- オブジェクトストレージは LocalStack（AWS S3 互換）。`AWS__ServiceURL` の差し替えのみで本番 S3 へ移行可能。

## 検証手段
- PR 前の Agent 側確認は `dotnet build`/`dotnet test`、フロントの `npm run build`、Agent の `py_compile`/`pytest` まで。
- 動作確認（Docker 起動・ブラウザ確認・LocalStack）は user が Mac ローカルで実施。手順は PR の `## 検証手順` に記載する。

> 禁止・強制（docker / ssh / rsync / git push 等の遮断）は `.claude/settings.json` の deny で管理する。本ファイルには書かない。
