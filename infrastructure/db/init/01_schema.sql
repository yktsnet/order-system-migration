-- カテゴリマスタ
CREATE TABLE M_Category (
    CategoryId SERIAL PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL,
    DeleteFlg INT DEFAULT 0
);

-- 在庫マスタ
CREATE TABLE M_Stock (
    ItemName VARCHAR(100) PRIMARY KEY,
    CurrentStock INT NOT NULL DEFAULT 0
);

-- 受注データ
CREATE TABLE Orders (
    OrderNo VARCHAR(20) PRIMARY KEY,
    OrderDate TIMESTAMP NOT NULL,
    CustomerName VARCHAR(100),
    CategoryId INT REFERENCES M_Category(CategoryId),
    ItemName VARCHAR(100),
    Price DECIMAL(18, 2),
    Qty INT,
    TotalAmount DECIMAL(18, 2)
);

-- サンプルデータ投入
INSERT INTO M_Category (CategoryName) VALUES ('事務用品'), ('家具'), ('消耗品');
INSERT INTO M_Stock (ItemName, CurrentStock) VALUES ('高性能オフィスチェア', 102);
