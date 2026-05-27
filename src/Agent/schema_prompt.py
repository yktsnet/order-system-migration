SCHEMA = """
テーブル定義（PostgreSQL）:

Orders(
  OrderNo VARCHAR PRIMARY KEY,
  OrderDate TIMESTAMP,
  CustomerName VARCHAR,
  CategoryId INT,
  ItemName VARCHAR,
  Price DECIMAL,
  Qty INT,
  TotalAmount DECIMAL
)

M_Category(
  CategoryId SERIAL PRIMARY KEY,
  CategoryName VARCHAR
)

M_Stock(
  ItemName VARCHAR PRIMARY KEY,
  CurrentStock INT
)

JOIN例:
  Orders JOIN M_Category ON Orders.CategoryId = M_Category.CategoryId

注意:
- 日付フィルタは OrderDate を使う
  例: OrderDate >= '2026-04-01' AND OrderDate < '2026-05-01'
- 金額集計は TotalAmount を使う
- SELECT 文のみ生成すること（INSERT/UPDATE/DELETE/DROP/TRUNCATE 厳禁）
- PostgreSQL 構文を使うこと
- テーブル名・カラム名は大文字小文字を正確に記述すること
- LIMIT は最大 200 にすること
"""
