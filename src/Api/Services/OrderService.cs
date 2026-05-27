using Dapper;
using Npgsql;
using System.Text;

namespace CloudNativeApp.Services;

// --- DTO定義 ---
public record CategoryDto(int Id, string Name);

public record CreateOrderRequest(
    string OrderNo,
    string CustomerName,
    int CategoryId,
    string ItemName,
    decimal Price,
    int Qty
);

public record OrderHistoryDto(
    string orderNo,
    DateTime orderDate,
    string customerName,
    string itemName,
    decimal price,
    int qty,
    decimal totalAmount,
    string categoryName
);

public record OrderSummary(
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    bool IsHighAmount
);

public record OrderFilterParams(
    string? CustomerName,
    int? CategoryId,
    string? ItemName,
    DateTime? From,
    DateTime? To
);

// --- Service実装 ---
public class OrderService
{
    private readonly string _connectionString;
    private readonly TaxService _taxService;

    public OrderService(IConfiguration config, TaxService taxService)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection が設定されていません。");
        _taxService = taxService;
    }

    public OrderSummary Calculate(decimal price, int qty)
    {
        var sub = price * qty;
        var tax = _taxService.Calculate(sub);
        var total = sub + tax;
        return new OrderSummary(sub, tax, total, total > 1_000_000);
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = "SELECT CategoryId as Id, CategoryName as Name FROM M_Category WHERE DeleteFlg = 0";
        return await conn.QueryAsync<CategoryDto>(sql);
    }

    public async Task<IEnumerable<OrderHistoryDto>> GetOrdersAsync(OrderFilterParams? filter = null)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        var conditions = new List<string> { "1=1" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter?.CustomerName))
        {
            conditions.Add("o.customername ILIKE @CustomerName");
            parameters.Add("CustomerName", $"%{filter.CustomerName}%");
        }
        if (!string.IsNullOrWhiteSpace(filter?.ItemName))
        {
            conditions.Add("o.itemname ILIKE @ItemName");
            parameters.Add("ItemName", $"%{filter.ItemName}%");
        }
        if (filter?.CategoryId.HasValue == true)
        {
            conditions.Add("o.categoryid = @CategoryId");
            parameters.Add("CategoryId", filter.CategoryId.Value);
        }
        if (filter?.From.HasValue == true)
        {
            conditions.Add("o.orderdate >= @From");
            parameters.Add("From", filter.From.Value);
        }
        if (filter?.To.HasValue == true)
        {
            conditions.Add("o.orderdate < @To");
            parameters.Add("To", filter.To.Value.Date.AddDays(1));
        }

        var where = string.Join(" AND ", conditions);
        var sql = $@"
            SELECT 
                o.orderno, 
                o.orderdate, 
                o.customername, 
                o.itemname, 
                o.price, 
                o.qty, 
                o.totalamount, 
                c.categoryname
            FROM Orders o
            JOIN M_Category c ON o.categoryid = c.categoryid
            WHERE {where}
            ORDER BY o.orderdate DESC";

        return await conn.QueryAsync<OrderHistoryDto>(sql, parameters);
    }

    public async Task<byte[]> GetOrdersCsvAsync(OrderFilterParams? filter = null)
    {
        var orders = await GetOrdersAsync(filter);
        var sb = new StringBuilder();
        sb.AppendLine("受注番号,受注日時,得意先名,商品名,カテゴリ,単価,数量,合計金額");
        foreach (var o in orders)
        {
            sb.AppendLine($"{o.orderNo},{o.orderDate:yyyy-MM-dd HH:mm:ss},{o.customerName},{o.itemName},{o.categoryName},{o.price},{o.qty},{o.totalAmount}");
        }
        // UTF-8 BOM付き（Excelで文字化けしない）
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<bool> RegisterOrderAsync(CreateOrderRequest req)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tran = await conn.BeginTransactionAsync();

        try
        {
            var summary = Calculate(req.Price, req.Qty);

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

            const string sqlStock = "UPDATE M_Stock SET CurrentStock = CurrentStock - @Qty WHERE ItemName = @ItemName";
            await conn.ExecuteAsync(sqlStock, new { req.Qty, req.ItemName }, tran);

            await tran.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            Console.WriteLine($"Register Error: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteOrderAsync(string orderNo)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tran = await conn.BeginTransactionAsync();

        try
        {
            const string sqlSelect = "SELECT itemname, qty FROM Orders WHERE orderno = @orderNo";
            var order = await conn.QuerySingleOrDefaultAsync<StockInfo>(sqlSelect, new { orderNo }, tran);

            if (order != null)
            {
                const string sqlUpdateStock = "UPDATE M_Stock SET CurrentStock = CurrentStock + @Qty WHERE ItemName = @ItemName";
                await conn.ExecuteAsync(sqlUpdateStock, new { Qty = order.Qty, ItemName = order.ItemName }, tran);

                const string sqlDelete = "DELETE FROM Orders WHERE orderno = @orderNo";
                await conn.ExecuteAsync(sqlDelete, new { orderNo }, tran);

                await tran.CommitAsync();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            Console.WriteLine($"Delete Error: {ex.Message}");
            throw;
        }
    }

    private class StockInfo
    {
        public string ItemName { get; set; } = string.Empty;
        public int Qty { get; set; }
    }
}
