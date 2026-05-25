using Dapper;
using Npgsql;

public class OrderService
{
    private readonly string _connectionString;
    private const decimal TaxRate = 0.1m;

    public OrderService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    // 計算ロジック（単体テストが可能な状態にする）
    public OrderSummary Calculate(decimal price, int qty)
    {
        var sub = price * qty;
        var tax = sub * TaxRate;
        var total = sub + tax;
        return new OrderSummary(sub, tax, total, total > 1000000);
    }

    // 保存ロジック（トランザクション処理）
    public async Task<bool> RegisterOrderAsync(CreateOrderRequest req)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tran = await conn.BeginTransactionAsync();

        try
        {
            var summary = Calculate(req.Price, req.Qty);

            // 1. 注文登録
            const string sqlOrder = @"
                INSERT INTO Orders (OrderNo, OrderDate, CustomerName, CategoryId, ItemName, Price, Qty, TotalAmount)
                VALUES (@OrderNo, @OrderDate, @CustomerName, @CategoryId, @ItemName, @Price, @Qty, @TotalAmount)";
            
            await conn.ExecuteAsync(sqlOrder, new {
                req.OrderNo,
                OrderDate = DateTime.Now,
                req.CustomerName,
                req.CategoryId,
                req.ItemName,
                req.Price,
                req.Qty,
                summary.TotalAmount
            }, tran);

            // 2. 在庫更新
            const string sqlStock = "UPDATE M_Stock SET CurrentStock = CurrentStock - @Qty WHERE ItemName = @ItemName";
            var affected = await conn.ExecuteAsync(sqlStock, new { req.Qty, req.ItemName }, tran);

            if (affected == 0) throw new Exception("在庫が見つかりません。");

            await tran.CommitAsync();
            return true;
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }
}
