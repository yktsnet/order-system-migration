## .NET 8 → .NET 10 (LTS) へのアップグレード
id: 02
skill: pr-workflow
branch-slug: dotnet10-upgrade
github_issue: 9
status: close
type: cleanup
対象: src/Api/CloudNativeApp.csproj, src/Api.Tests/CloudNativeApp.Tests.csproj, src/Api/Dockerfile
内容: .NET 8 は2026-11-10にEOL（サポート終了）を迎える。次期LTSである.NET 10へ移行し、TargetFramework・関連パッケージ・DockerのSDKバージョンを揃える。
確認: `dotnet build src/Api` / `dotnet test src/Api.Tests` がエラー・警告なく通ること（特にNullable/Analyzer関連の新規警告に注意）。全ファイルで `net8.0` 表記・`dotnet/sdk:8.0` / `dotnet/aspnet:8.0` 表記が残っていないことをgrepで確認。
---
## 背景

.NET 8はLTSとして2026-11-10にEOLを迎える（[.NET Blog](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/)）。次のLTSは.NET 10（〜2028-11サポート）。.NET 9は非LTS(STS)で.NET 8と同時期にEOLを迎えるため、9を経由せず10へ直接上げる。姉妹リポ `attendance-system-migration` は既に同様のアップグレードを完了済み（Issue 06 / PR #12）。

このリポには `.github/workflows/` によるCIが存在せず、`dotnet build`/`dotnet test`/`npm run build` はユーザーがローカル（Mac）で実行する運用（[CLAUDE.md](../CLAUDE.md)参照）。したがってCIワークフローの `dotnet-version` 更新は対象外。

依存関係は `AWSSDK.S3`, `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`, `Dapper`, `Npgsql`, xUnit系という標準構成で、.NET 8→10間で削除されたAPIは使用していない見込み。

## 変更方針

### `src/Api/CloudNativeApp.csproj`
- `<TargetFramework>net8.0</TargetFramework>` → `net10.0`
- 各パッケージを.NET 10対応バージョンへ更新（`Microsoft.AspNetCore.OpenApi` はASP.NET Core本体とメジャーバージョンを合わせる。`AWSSDK.S3`, `Swashbuckle.AspNetCore`, `Dapper`, `Npgsql` も.NET 10で動作する最新版に）

### `src/Api.Tests/CloudNativeApp.Tests.csproj`
- `<TargetFramework>net8.0</TargetFramework>` → `net10.0`
- `Microsoft.NET.Test.Sdk` / `xunit` / `xunit.runner.visualstudio` / `coverlet.collector` も必要なら最新へ

### `src/Api/Dockerfile`
- バックエンドビルドステージ（18行目）: `mcr.microsoft.com/dotnet/sdk:8.0` → `mcr.microsoft.com/dotnet/sdk:10.0`
- 実行ステージ（36行目）: `mcr.microsoft.com/dotnet/aspnet:8.0` → `mcr.microsoft.com/dotnet/aspnet:10.0`

## 実装順序
1. `CloudNativeApp.csproj` / `CloudNativeApp.Tests.csproj` のTargetFramework・パッケージ更新
2. ローカルで `dotnet build src/Api` / `dotnet test src/Api.Tests` が通ることを確認
3. `Dockerfile` のSDK/ASP.NETタグ更新

## 参考
- README.md / README.en.md / context/*.md 等に「.NET 8」という表記が残る場合があるが、これはドキュメント更新の話であり本Issueのスコープ外
