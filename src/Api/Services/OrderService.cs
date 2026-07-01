using Amazon.S3;
using Amazon.S3.Model;
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

// --- 受注登録・取消のDBアクセスを抽象化するリポジトリ ---
// RegisterOrderAsync/DeleteOrderAsync は「登録」「取消」の2アクションのみで完結し、
// 修正(update)処理は許容しない、というドメインルールを守ったまま単体テスト可能にするための境界。
public interface IOrderRepository
{
    /// <summary>
    /// 受注を登録し、在庫を数量分減算する。登録・在庫更新は単一トランザクションとして扱い、
    /// 途中で例外が発生した場合はロールバックし、在庫・受注のいずれも変更されないこと。
    /// </summary>
    Task<bool> RegisterOrderAsync(CreateOrderRequest req, DateTime orderDate, decimal totalAmount);

    /// <summary>
    /// 受注を取り消す。対象受注が存在する場合は在庫を数量分復元して受注を削除し true を返す。
    /// 存在しない場合は false を返し、何も変更しない。
    /// </summary>
    Task<bool> DeleteOrderAsync(string orderNo);
}

// Npgsqlに直結する本番用実装。トランザクション制御はここに集約する。
internal sealed class NpgsqlOrderRepository : IOrderRepository
{
    private readonly string _connectionString;

    public NpgsqlOrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> RegisterOrderAsync(CreateOrderRequest req, DateTime orderDate, decimal totalAmount)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tran = await conn.BeginTransactionAsync();

        try
        {
            const string sqlOrder = @"
                INSERT INTO Orders (OrderNo, OrderDate, CustomerName, CategoryId, ItemName, Price, Qty, TotalAmount)
                VALUES (@OrderNo, @OrderDate, @CustomerName, @CategoryId, @ItemName, @Price, @Qty, @TotalAmount)";

            await conn.ExecuteAsync(sqlOrder, new {
                req.OrderNo,
                OrderDate = orderDate,
                req.CustomerName,
                req.CategoryId,
                req.ItemName,
                req.Price,
                req.Qty,
                TotalAmount = totalAmount
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

// --- Service実装 ---
public class OrderService
{
    private readonly string _connectionString;
    private readonly TaxService _taxService;
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly IOrderRepository _repository;

    public OrderService(IConfiguration config, TaxService taxService, IAmazonS3 s3)
        : this(config, taxService, s3, repository: null)
    {
    }

    // テスト等でDBアクセス(Npgsql直結)をモック/インメモリ実装に差し替えるためのコンストラクタ。
    // repository を省略した場合は既存どおり Npgsql に直結する。
    public OrderService(IConfiguration config, TaxService taxService, IAmazonS3 s3, IOrderRepository? repository)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection が設定されていません。");
        _taxService = taxService;
        _s3 = s3;
        _bucketName = config["AWS:BucketName"] ?? "order-exports";
        _repository = repository ?? new NpgsqlOrderRepository(_connectionString);
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
        var csvBytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        // S3にアーカイブ保存（失敗してもダウンロードは継続）
        // 環境変数 AWS__ServiceURL を切り替えるだけで LocalStack ↔ 本番 S3 を切り替え可能
        try
        {
            var key = $"exports/{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.csv";
            using var stream = new MemoryStream(csvBytes);
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = "text/csv"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"S3 archive failed: {ex.Message}");
        }

        return csvBytes;
    }

    // 「登録」アクション。修正(update)は許容せず、常に新規登録として扱う。
    public async Task<bool> RegisterOrderAsync(CreateOrderRequest req)
    {
        var summary = Calculate(req.Price, req.Qty);
        return await _repository.RegisterOrderAsync(req, DateTime.Now, summary.TotalAmount);
    }

    // 「取消」アクション。修正(update)は許容せず、削除のみで完結する。
    public async Task<bool> DeleteOrderAsync(string orderNo)
        => await _repository.DeleteOrderAsync(orderNo);
}
