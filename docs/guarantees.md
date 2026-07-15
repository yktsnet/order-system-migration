# Guarantee Ledger

## Guarantees

### 1. `src/Api.Tests/TaxServiceTests.cs` — src/Api/Services/TaxService.cs (Calculate)

- `subTotal` が 0 の場合は 0 を返す
- 消費税額は `subTotal * TaxRate` の小数点以下を切り捨てて算出する（四捨五入・切り上げは行わない）。境界値（切り捨てが発生しないちょうどの倍数を含む）を網羅する
- 1円未満の端数（例: 5円→0.5円）も切り捨てて0円になる
- `TaxRate` は `public const decimal` として公開されており、呼び出し側は `TaxService.TaxRate` を参照して期待値を独自に算出できる

| 保証(要約) | 対応テスト |
|---|---|
| ゼロ入力は0 | `Calculate_ZeroSubTotal_ReturnsZero` |
| 端数切り捨て（境界値網羅） | `Calculate_FloorsFractionalYen_DoesNotRound` |
| 1円未満は0円に切り捨て | `Calculate_SmallSubTotal_LessThanOneYenTax_FlooredToZero` |
| `TaxRate` が公開定数 | `Calculate_UsesPublicTaxRate_ForExpectedValueCalculation` |

### 2. `src/Api.Tests/OrderServiceRegisterDeleteTests.cs` — src/Api/Services/OrderService.cs (RegisterOrderAsync, DeleteOrderAsync), src/Api/Program.cs

- `RegisterOrderAsync` は登録した数量ぶん在庫を減算する
- `RegisterOrderAsync` が記録する合計金額は `TaxService.Calculate` による税額計算と一致する
- `RegisterOrderAsync` の登録処理中に例外が発生した場合、在庫・受注のいずれも変更されず、例外がそのまま呼び出し元に伝播する（登録と在庫減算は単一トランザクションとして扱われる）
- `DeleteOrderAsync` は対象受注が存在する場合、在庫を数量ぶん復元したうえで受注を削除し `true` を返す
- `DeleteOrderAsync` は対象受注が存在しない場合、在庫・受注のいずれも変更せず `false` を返す
- 受注操作は「登録(POST `/orders`)」と「取消(DELETE `/orders/{orderNo}`)」の2アクションのみで構成され、更新用の `PUT`/`PATCH` エンドポイントは存在しない

| 保証(要約) | 対応テスト |
|---|---|
| 登録時の在庫減算 | `RegisterOrderAsync_DecrementsStock_ByOrderedQty` |
| 登録金額は税計算と一致 | `RegisterOrderAsync_TotalAmount_MatchesTaxServiceCalculation` |
| 登録失敗時のロールバック | `RegisterOrderAsync_WhenExceptionOccurs_RollsBack_StockAndOrderUnchanged` |
| 取消時の在庫復元・削除 | `DeleteOrderAsync_WhenOrderExists_RestoresStock_AndDeletesOrder` |
| 存在しない受注の取消は無変更でfalse | `DeleteOrderAsync_WhenOrderDoesNotExist_ReturnsFalse_AndDoesNotChangeStock` |
| 更新エンドポイント不在・登録/取消のみ | `ProgramCs_HasNoUpdateEndpoints_OnlyRegisterAndDeleteActionsExist` |

### 3. `src/Api.Tests/OrderServiceTests.cs` — src/Api/Services/OrderService.cs (Calculate, コンストラクタ)

- `Calculate(price, qty)` は `SubTotal = price * qty`、`TaxAmount` はその小数点以下切り捨て、`TotalAmount = SubTotal + TaxAmount` を返す
- `TotalAmount` が100万円を超える場合に `IsHighAmount` が `true` になる。ちょうど100万円の場合は `false`（境界は「超過」であり「以上」ではない）
- 税額は四捨五入ではなく切り捨てで算出される
- `ConnectionStrings:DefaultConnection` が設定に存在しない場合、コンストラクタは `InvalidOperationException` を送出する

| 保証(要約) | 対応テスト |
|---|---|
| Sub/Tax/Total/IsHighAmountの算出（表駆動） | `Calculate_ReturnsCorrectValues` |
| 100万円ちょうどはIsHighAmount=false | `Calculate_IsHighAmount_False_WhenTotalIsExactlyOneMillion` |
| 税額は切り捨て | `Calculate_Tax_IsFloored_NotRounded` |
| SubTotalはprice×qty | `Calculate_SubTotal_IsPrice_MultipliedBy_Qty` |
| 接続文字列欠如で例外 | `Constructor_MissingDefaultConnectionString_ThrowsInvalidOperationException` |

### 4. `src/Agent/tests/test_agent.py` — src/Agent/agent.py (validate_sql, route_classify, route_validate, route_execute, classify_intent, generate_sql, execute_sql, format_response, handle_error)

- `validate_sql` は大文字・小文字を問わず `SELECT` 文（サブクエリを含む）をエラー無しで通す
- `validate_sql` は `INSERT`/`UPDATE`/`DELETE`/`DROP`/`TRUNCATE` を含むSQL、および `WITH` 句始まりで `DELETE` を含むSQLをエラーとし、エラーメッセージに該当キーワードを含める
- `route_classify` は `error` が `"out_of_scope"` の場合 `"end"` を返す
- `route_classify` は `error` が `"out_of_scope"` 以外（空文字・他の文字列いずれも）の場合 `"generate_sql"` を返す
- `route_validate` は `error` が非空なら `"handle_error"` を返す
- `route_validate` は `error` が空なら `"execute_sql"` を返す
- `route_execute` は `error` が空なら `"format_response"` を返す
- `route_execute` は `error` があり `retry_count < 2` なら `"generate_sql"` を返す
- `route_execute` は `error` があり `retry_count >= 2` なら `"handle_error"` を返す
- `classify_intent` はLLM応答に `"YES"` が含まれる場合、`error` を `"out_of_scope"` にしない
- `classify_intent` はLLM応答に `"NO"` が含まれる場合、`error` を `"out_of_scope"` に設定し、`answer` に受注データへの問い合わせを促す非空の案内文（「受注」を含む）を設定する
- `generate_sql` はLLM応答からSQLを生成して `error` を空文字にリセットする
- `generate_sql` は応答中のコードブロック記法(```)を除去する
- `generate_sql` は `retry_count > 0` かつ前回 `error` がある場合はその内容をプロンプトに含め、初回（`retry_count == 0`）はリトライヒントを含めない
- `execute_sql` は成功時に `result` をそのまま返し `error` を空文字にする
- `execute_sql` はDBアクセスで例外が発生した場合、エラーメッセージ文言を `error` に含め、`retry_count` を1増やす（複数回失敗時は累積する）
- `format_response` は `result` が空でない場合はLLM応答をそのまま `answer` に設定し、`result` が空の場合はLLMを呼び出さずに「見つかりませんでした」を含む定型文を返す
- `format_response` は成功・空結果いずれの場合も `db.log_agent` による監査記録を1回行う
- `handle_error` は `answer` に `error` の内容を含むメッセージを設定し、`success=False` として `db.log_agent` に監査記録する

| 保証(要約) | 対応テスト |
|---|---|
| SELECT文（サブクエリ含む）は許可 | `test_valid_select`, `test_valid_select_lowercase`, `test_select_with_subquery_allowed` |
| 禁止操作はエラーで拒否 | `test_insert_rejected`, `test_update_rejected`, `test_delete_rejected`, `test_drop_rejected`, `test_truncate_rejected`, `test_non_select_start_rejected` |
| out_of_scopeは"end"へ | `test_out_of_scope_goes_to_end` |
| out_of_scope以外は"generate_sql"へ | `test_empty_error_goes_to_generate_sql`, `test_other_error_goes_to_generate_sql` |
| errorありは"handle_error"へ | `test_has_error_goes_to_handle_error` |
| errorなしは"execute_sql"へ | `test_no_error_goes_to_execute_sql` |
| 成功時は"format_response"へ | `test_success_goes_to_format_response`, `test_no_error_empty_result_goes_to_format_response` |
| リトライ上限未満は"generate_sql"へ | `test_first_retry_goes_to_generate_sql`, `test_second_retry_goes_to_generate_sql` |
| リトライ上限到達で"handle_error"へ | `test_retry_limit_goes_to_handle_error` |
| YES応答はスコープ内のまま | `test_yes_response_passes_through` |
| NO応答はout_of_scope・案内文設定 | `test_no_response_sets_out_of_scope`, `test_out_of_scope_answer_contains_example` |
| SQL生成とerrorリセット | `test_sql_is_generated`, `test_error_cleared_on_new_attempt` |
| コードブロック除去 | `test_code_block_backticks_stripped` |
| リトライヒントの有無 | `test_retry_hint_included_in_prompt`, `test_no_retry_hint_on_first_attempt` |
| 実行成功時の結果反映 | `test_success_returns_result` |
| DBエラー時のerror設定とretry_count加算 | `test_db_error_sets_error_message`, `test_db_error_increments_retry_count`, `test_db_error_accumulates_retry_count` |
| 結果整形とLLM呼び出し有無 | `test_formats_non_empty_result`, `test_empty_result_returns_not_found`, `test_empty_result_does_not_call_llm` |
| 監査ログ記録 | `test_log_agent_called_on_success`, `test_log_agent_called_on_empty_result` |
| エラー時のメッセージ・監査記録 | `test_answer_contains_error_message`, `test_log_agent_called_with_success_false` |

## Gaps

以下は保証すべきと思われるが、対応するテストが無い。

- `OrderService.GetCategoriesAsync`/`GetOrdersAsync`/`GetOrdersCsvAsync` は `NpgsqlConnection` に直結しており、リポジトリ抽象を介さないため実DB無しでは単体テストできない
- `NpgsqlOrderRepository`（本番用の `IOrderRepository` 実装）はNpgsql直結のため実DB無しでは単体テストできない
- `TaxService.Calculate`/`OrderService.Calculate` に負の `subTotal`/`price`/`qty` を渡した場合の挙動は未定義（業務上想定されない入力のため、現状のテストからは切り捨て方向の契約を断定できない）
- 在庫数量を超える数量で `RegisterOrderAsync` を呼んだ場合に在庫チェックが働くかどうかは未テスト（現行実装にチェック処理が無く、意図した仕様か未確定な変更前提のため保証化を見送り）
- `src/Agent/main.py` の FastAPI エンドポイント（`POST /chat`, `GET /health`）に対応するテストが無い
- `src/Agent/db.py`（`execute_query`, `log_agent` の実DBアクセス実装）は実DB無しでは単体テストできない

## About

対象は各テストファイルが実際に検証している公開関数・メソッド・クラス・エンドポイントの外部から観測可能な振る舞いであり、対象外は実DB・実LLM接続を要する内部実装（Npgsql直結処理、Gemini API呼び出し本体など）。**ここに載っていない振る舞いは約束ではなく、予告なく変わりうる。** 本ドキュメントは design-decisions.md 相当のドキュメントと同格として扱う。
