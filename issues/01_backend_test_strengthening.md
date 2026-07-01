## C#バックエンドのテスト強化（TaxService単体・受注登録/取消のドメインルール）
id: 01
skill: pr-workflow
branch-slug: backend-test-strengthening
github_issue:
status: open
type: fix
対象: src/Api.Tests/TaxServiceTests.cs (新規), src/Api.Tests/OrderServiceRegisterDeleteTests.cs (新規), src/Api/Services/TaxService.cs, src/Api/Services/OrderService.cs
内容: C#側は Calculate の計算ロジックのみテストされており、TaxService単体の境界値テストと、移行時に重要な「注文の登録・取消（修正を許容しない）」というドメインルールに関するテストが不足している。Python Agent 側（test_agent.py）と同等の網羅性を目指し、DBアクセス部分はモック/インメモリで代替した単体テストを追加する。
確認: `dotnet test src/Api.Tests` で新規テストがすべて成功すること。RegisterOrderAsync/DeleteOrderAsync 双方について、在庫増減・トランザクションロールバック・「更新(PUT/PATCH)エンドポイントが存在せず登録と取消の2アクションのみで完結する」というドメインルールを検証できていることを目視確認する。

---
## 詳細

### (1) TaxService 単体テスト
- `TaxService.Calculate` は `Math.Floor(subTotal * 0.1m)` の単純計算だが単体テストが存在しない。
- 境界値（端数切り捨てが発生するケース、0円、小数点以下が0のケースなど）を含むテストケースを追加する。

### (2) 受注登録・取消のドメインルールテスト
- `OrderService.RegisterOrderAsync` / `DeleteOrderAsync` は「修正処理を許容しない」という業務ルール上、登録と取消の2アクションのみで運用される。この前提を検証するテストが存在しない。
- DBアクセス部分（Npgsql直結）をテスト可能にするため、モックまたはテスト用インメモリ実装を用意し、以下を検証する:
  - 登録時: 在庫が数量分減算されること、合計金額(TotalAmount)がTaxServiceの計算結果と一致すること、例外発生時にトランザクションがロールバックされ在庫・受注ともに変更されないこと
  - 取消時: 対象受注が存在する場合は在庫が数量分復元され受注が削除されること、存在しない場合は false を返し何も変更されないこと
- 「更新エンドポイントが存在しない」というアーキテクチャ上のルールについては、`Program.cs` のエンドポイント定義（MapPost/MapGet/MapDelete のみでMapPut/MapPatchが存在しないこと）を確認するテストまたは目視確認で担保する。既存の `Program.cs` 自体の変更は不要。

### 実装順序
1. TaxService の単体テスト追加
2. OrderService の DB アクセスをモック可能にする最小限の整理（大規模リファクタは避ける）
3. RegisterOrderAsync / DeleteOrderAsync のテスト追加
4. `dotnet test` で全体確認
