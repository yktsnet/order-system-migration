# 移行計画 — レガシー WinForms → .NET 8 Web API + React

## 移行の基本方針

「一括書き換え」ではなく、**動作を維持しながら段階的に責務を切り出す** アプローチを採用。
各フェーズは独立してデプロイ・検証可能な単位とする。

---

## フェーズ定義

### Phase 0 — 現状把握・課題整理

**目的**: レガシーコードの問題点を特定し、移行スコープを確定する。

| 作業 | 詳細 |
|---|---|
| コード読解 | `OrderForm.cs` の責務を列挙（UI / SQL / ビジネスロジック / ハードウェア） |
| 課題分類 | SQLインジェクション・UI フリーズ・テスト不能・環境依存を文書化 |
| スコープ確定 | 認証・DB冗長化は Out-of-Scope と明示 |

**成果物**: 本 `migration-plan.md`、`legacy/OrderForm.cs`（ビフォー状態の保存）

---

### Phase 1 — ロジック抽出（Service 層の確立）

**目的**: 画面クラスに混在したビジネスロジックを独立したクラスへ切り出す。

| 作業 | 詳細 |
|---|---|
| 税計算の分離 | `OrderForm.cs` 内の税計算ロジック → `OrderService.Calculate()` へ移管 |
| 在庫更新の分離 | 保存ボタン内のトランザクション処理 → `OrderService.RegisterOrderAsync()` へ移管 |
| 取消処理の分離 | 削除 + 在庫復元 → `OrderService.DeleteOrderAsync()` へ移管 |
| 単体テスト追加 | `Calculate()` を対象に境界値テスト 7 ケース実装（DB 不要） |

**検証**: `dotnet test` がすべてパス

---

### Phase 2 — API 化（HTTP インターフェースの確立）

**目的**: Service 層を外部から HTTP で呼び出せる Minimal API としてラップする。

| 作業 | 詳細 |
|---|---|
| エンドポイント定義 | `GET /categories`, `GET /orders`, `POST /orders`, `DELETE /orders/{orderNo}` |
| パラメータ化クエリ | 文字列結合 SQL → Dapper のパラメータバインドに置換 |
| Swagger 有効化 | `AddSwaggerGen()` + `UseSwaggerUI()` で API 仕様を常時公開 |
| CORS 設定 | `AllowedOrigins` を設定ファイル駆動に（`AllowAnyOrigin` を廃止） |

**検証**: Swagger UI（`/swagger`）で全エンドポイントの動作確認

---

### Phase 3 — フロントエンド化（React への置き換え）

**目的**: WinForms の画面を React/TypeScript で再実装し、Web ブラウザから操作可能にする。

| 作業 | 詳細 |
|---|---|
| 受注登録フォーム | カテゴリ選択・単価・数量入力 → `POST /orders` を非同期呼び出し |
| 合計リアルタイム計算 | TextChanged での DB 通信を廃止。フロント側で即時計算 |
| 注文履歴タブ | `GET /orders` の結果を一覧表示。削除アクションで `DELETE /orders/{orderNo}` |
| 高額警告 | 合計 100 万超で視覚的フィードバック（旧: 文字を赤くする → 新: バッジ表示） |

**検証**: ブラウザから一連の受注登録・履歴確認・取消が完結

---

### Phase 4 — インフラ整備（環境依存の排除）

**目的**: 「Windows でしか動かない」制約を破壊し、どこでも同じ手順で起動できる状態にする。

| 作業 | 詳細 |
|---|---|
| Docker 化 | API / Web / PostgreSQL を `docker-compose.yml` で一元管理 |
| IaC (Terraform) | `infrastructure/` に Terraform 定義。LocalStack でローカル検証 |
| Cloudflare Tunnel | 固定 IP 不要でデモ URL を外部公開 |
| CI/CD | GitHub Actions で push ごとに build + test を自動実行 |

**検証**: `docker compose up` 1 コマンドで全環境が起動。CI が green

---

## 移行前後の対比

| 観点 | Before (WinForms) | After (.NET 8 + React) |
|---|---|---|
| ビジネスロジックの場所 | 画面クラス (`OrderForm.cs`) | `OrderService` に集約 |
| データアクセス | 文字列結合 SQL（インジェクションリスク） | Dapper パラメータ化クエリ |
| UI のデータ取得タイミング | 同期・フリーズあり | 非同期 HTTP（fetch） |
| テスト可能性 | 不可（UI と密結合） | `Calculate()` 等は DB 不要でテスト可 |
| 実行環境 | Windows 端末のみ（LPT1 依存） | Docker があればどこでも動作 |
| デプロイ | 手動インストール | `docker compose up` |
