using Amazon.S3;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --- 共通設定 ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<OrderService>();

// --- 1. DB (PostgreSQL) の設定 ---
// ConnectionStrings:DefaultConnection は appsettings.json または環境変数から読み込まれます
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=HANBAI;Username=postgres;Password=p@ssw0rd;";

// --- 2. AWS SDK (S3/LocalStack) の設定 ---
var serviceUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
var bucketName = builder.Configuration["AWS:BucketName"] ?? "my-local-training-bucket";

builder.Services.AddSingleton<IAmazonS3>(sp => 
{
    var config = new AmazonS3Config { ServiceURL = serviceUrl, ForcePathStyle = true };
    return new AmazonS3Client("test", "test", config);
});

var app = builder.Build();

// 開発環境では Swagger を有効化
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- エンドポイント実装 ---

// [移植ロジック] カテゴリ一覧取得 (Phase 1 疎通確認用)
app.MapGet("/categories", async () =>
{
    try 
    {
        using var conn = new NpgsqlConnection(connectionString);
        const string sql = "SELECT CategoryId as Id, CategoryName as Name FROM M_Category WHERE DeleteFlg = 0";
        var categories = await conn.QueryAsync<CategoryDto>(sql);
        return Results.Ok(categories);
    }
    catch (Exception ex)
    {
        return Results.Problem($"DB接続エラー: {ex.Message}");
    }
});

// [既存ロジック保持] S3バケット一覧取得
app.MapGet("/buckets", async (IAmazonS3 s3Client) =>
{
    var response = await s3Client.ListBucketsAsync();
    return Results.Ok(response.Buckets.Select(b => b.BucketName));
});

// [既存ロジック保持] S3へダミーファイルをアップロード
app.MapPost("/upload", async (IAmazonS3 s3Client, [FromQuery] string fileName) =>
{
    var request = new Amazon.S3.Model.PutObjectRequest
    {
        BucketName = bucketName,
        Key = fileName,
        ContentBody = $"Modernized App: Created at {DateTime.Now} on {Environment.MachineName}"
    };
    await s3Client.PutObjectAsync(request);
    return Results.Ok($"'{fileName}' を '{bucketName}' にアップロード完了！");
});

// 受注登録 API (WinForms の保存ボタン相当)
app.MapPost("/orders", async (CreateOrderRequest req, OrderService service) =>
{
    try
    {
        var success = await service.RegisterOrderAsync(req);
        return success ? Results.Ok("登録完了") : Results.BadRequest("登録失敗");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// リアルタイム計算 API (WinForms の TextChanged 相当)
app.MapGet("/orders/calculate", (decimal price, int qty, OrderService service) =>
{
    return Results.Ok(service.Calculate(price, qty));
});

app.Run();

// DTO定義
public record CategoryDto(int Id, string Name);


// クライアントからの登録リクエスト
public record CreateOrderRequest(
    string OrderNo,
    string CustomerName,
    int CategoryId,
    string ItemName,
    decimal Price,
    int Qty
);

// 計算結果を含むレスポンス
public record OrderSummary(
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    bool IsHighAmount // 100万超え判定
);
