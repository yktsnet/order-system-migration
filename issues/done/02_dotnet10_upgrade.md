## PR記録: chore: .NET 8 → .NET 10 (LTS) へアップグレード
issue: 02 (02_dotnet10_upgrade.md)
PR: https://github.com/yktsnet/order-system-migration/pull/8
Merged: 184cacd60977af45b3a3db27fb98254c88d1e5f5

## 変更内容
.NET 8 は2026-11-10にEOL。次期LTSである.NET 10へTargetFramework・関連パッケージ・DockerのSDKバージョンを揃えた。

- `src/Api/CloudNativeApp.csproj`: `TargetFramework` を `net8.0` → `net10.0`。`AWSSDK.S3` 4.0.23.3→4.0.100.3、`Microsoft.AspNetCore.OpenApi` 8.0.26→10.0.9（ASP.NET Core本体のメジャーバージョンに合わせ10系）、`Swashbuckle.AspNetCore` 6.6.2→10.2.3、`Npgsql` 10.0.2→10.0.3 へ更新。`Dapper` は既に最新（2.1.79）のため変更なし。
- `src/Api.Tests/CloudNativeApp.Tests.csproj`: `TargetFramework` を `net8.0` → `net10.0`。`Microsoft.NET.Test.Sdk` 17.9.0→18.7.0、`xunit` 2.7.0→2.9.3、`xunit.runner.visualstudio` 2.5.7→2.8.2（xunit 2.x系と組む安定最新）、`coverlet.collector` 6.0.1→10.0.1 へ更新。
- `src/Api/Dockerfile`: バックエンドビルドステージを `mcr.microsoft.com/dotnet/sdk:8.0` → `sdk:10.0`、実行ステージを `mcr.microsoft.com/dotnet/aspnet:8.0` → `aspnet:10.0` へ更新。

## 静的確認結果
- `nix-shell -p dotnet-sdk_10 --run "dotnet build src/Api"`: Build succeeded、Warning 0 / Error 0
- `nix-shell -p dotnet-sdk_10 --run "dotnet test src/Api.Tests"`: Passed 22 / Failed 0 / Skipped 0
- `grep -rn "net8\.0\|dotnet/sdk:8\.0\|dotnet/aspnet:8\.0" src/Api/CloudNativeApp.csproj src/Api.Tests/CloudNativeApp.Tests.csproj src/Api/Dockerfile`: マッチなし（残存表記なし）
- 各パッケージのバージョンは nuget.org の stable 版一覧（プレリリース除外）から選定。.NET 8→10間で削除されたAPIの使用は build/test 通過により確認済み
- 変更対象は上記3ファイルのみで、issueの「対象」フィールドと一致（`git diff --name-only --cached`: src/Api.Tests/CloudNativeApp.Tests.csproj, src/Api/CloudNativeApp.csproj, src/Api/Dockerfile）

## 検証手順
- `docker compose build api` でDockerイメージが `dotnet/sdk:10.0` / `dotnet/aspnet:10.0` ベースで正常にビルドできることをMacローカルで確認
- `docker compose up` 後、通常のCRUD操作・受注取り消し/登録が従来通り動作することをブラウザで確認
