## 変更内容

C#側は `Calculate` の計算ロジックのみテストされており、TaxService単体の境界値テストと、移行時に重要な「注文の登録・取消（修正を許容しない）」というドメインルールに関するテストが不足していた。Python Agent側（test_agent.py）と同等の網羅性を目指し、DBアクセス部分はモック/インメモリで代替した単体テストを追加した。

### (1) TaxService 単体テスト（`src/Api.Tests/TaxServiceTests.cs` 新規）
- `TaxService.Calculate`（`Math.Floor(subTotal * 0.1m)`）の境界値テストを追加。
  - 0円、端数切り捨てが発生するケース（9円→0円、11円→1円、999円→99円など）、切り捨てが発生しないちょうどの金額（10円→1円、1000円→100円）を網羅。
- `TaxService.TaxRate` を `private const` から `public const` に変更し、テストから期待値算出に利用できるようにした（値・挙動は変更なし）。

### (2) 受注登録・取消のドメインルールテスト（`src/Api.Tests/OrderServiceRegisterDeleteTests.cs` 新規）
- `OrderService.RegisterOrderAsync` / `DeleteOrderAsync` のDBアクセス（Npgsql直結）をテスト可能にするため、`src/Api/Services/OrderService.cs` に `IOrderRepository` インターフェースを追加し、実装を以下の2つに分離した。
  - `NpgsqlOrderRepository`: 既存のNpgsql直結・トランザクション処理をそのまま移設（本番用、動作は従来と同一）。
  - テスト用のインメモリ実装（`FakeOrderRepository`、テストファイル内に定義）: 在庫・受注をDictionaryで管理し、トランザクションのロールバックを模擬。
- `OrderService` に `IOrderRepository?` を受け取るコンストラクタを追加（省略時は従来どおり `NpgsqlOrderRepository` を使用するため、`Program.cs` の変更は不要）。
- 追加したテストケース:
  - 登録時に在庫が数量分減算されること
  - 登録時のTotalAmountが`TaxService`の計算結果と一致すること
  - 登録処理中に例外が発生した場合、ロールバックされ在庫・受注ともに変更されないこと
  - 取消時、対象受注が存在する場合は在庫が数量分復元され受注が削除されること
  - 取消時、対象受注が存在しない場合は`false`を返し何も変更されないこと
  - `Program.cs`のエンドポイント定義を読み取り、`MapPut`/`MapPatch`が存在せず`MapPost`/`MapDelete`のみで受注操作が完結していることを検証（`Program.cs`自体は変更していない）

## 静的確認結果

- `dotnet build src/Api` : ビルド成功（0 Warning, 0 Error）。
- `dotnet test src/Api.Tests` : 既存7件 + 新規15件 = 全22件成功（Failed: 0, Passed: 22, Skipped: 0）。
- コードを読んで確認した内容:
  - `RegisterOrderAsync`/`DeleteOrderAsync`の呼び出し元は`Program.cs`の`MapPost("/orders")`/`MapDelete("/orders/{orderNo}")`のみで、シグネチャ（引数・戻り値）は変更していないため既存の呼び出し元に影響なし。
  - `NpgsqlOrderRepository`に移設したSQL・トランザクション制御ロジックは元の`OrderService`実装から一字一句変更しておらず、本番動作は従来と同一。
  - `Program.cs`は対象ファイルに含めておらず、実際に変更していない（`git diff --name-only HEAD~1` で確認済み）。
  - `GetCategoriesAsync`/`GetOrdersAsync`/`GetOrdersCsvAsync`は今回のリポジトリ抽象化の対象外とし、既存のNpgsql直結のままとした（大規模リファクタを避けるため、Issueのスコープである登録・取消のみを対象にした）。

`git diff --name-only HEAD~1` の出力:
```
src/Api.Tests/OrderServiceRegisterDeleteTests.cs
src/Api.Tests/TaxServiceTests.cs
src/Api/Services/OrderService.cs
src/Api/Services/TaxService.cs
```

## 検証手順

本Issueはユニットテスト追加のみで、Docker起動やブラウザでの動作確認は不要。以下をuserのMacローカルで実施:

```
dotnet test src/Api.Tests
```
- 全テスト（22件）が成功することを確認する。
