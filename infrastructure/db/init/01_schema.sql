-- カテゴリマスタ
CREATE TABLE IF NOT EXISTS M_Category (
    CategoryId SERIAL PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL UNIQUE,
    DeleteFlg INT DEFAULT 0
);
-- 在庫マスタ
CREATE TABLE IF NOT EXISTS M_Stock (
    ItemName VARCHAR(100) PRIMARY KEY,
    CurrentStock INT NOT NULL DEFAULT 0
);
-- 受注データ
CREATE TABLE IF NOT EXISTS Orders (
    OrderNo VARCHAR(20) PRIMARY KEY,
    OrderDate TIMESTAMP NOT NULL,
    CustomerName VARCHAR(100),
    CategoryId INT REFERENCES M_Category(CategoryId),
    ItemName VARCHAR(100),
    Price DECIMAL(18, 2),
    Qty INT,
    TotalAmount DECIMAL(18, 2)
);
-- Agent ログ（Phase 2）
CREATE TABLE IF NOT EXISTS AgentLog (
    id SERIAL PRIMARY KEY,
    logged_at TIMESTAMP DEFAULT NOW(),
    question TEXT NOT NULL,
    generated_sql TEXT,
    success BOOLEAN NOT NULL,
    retry_count INT DEFAULT 0,
    error_message TEXT
);
-- サンプルデータ投入
INSERT INTO M_Category (CategoryName) VALUES ('事務用品'), ('家具'), ('消耗品')
    ON CONFLICT DO NOTHING;
INSERT INTO M_Stock (ItemName, CurrentStock) VALUES ('高性能オフィスチェア', 102)
    ON CONFLICT DO NOTHING;
