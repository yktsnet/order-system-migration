using Amazon.S3;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using CloudNativeApp.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 共通設定 ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS設定：React(5173ポート)からの通信を許可
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        var origins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:5173"];
        policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader();
    });
});

// --- Service / DB 設定 ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=HANBAI;Username=postgres;Password=p@ssw0rd;";

builder.Services.AddScoped<OrderService>();

builder.Services.AddSingleton<IAmazonS3>(sp => {
    var serviceUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
    var config = new AmazonS3Config { ServiceURL = serviceUrl, ForcePathStyle = true };
    return new AmazonS3Client("test", "test", config);
});

var app = builder.Build();

app.UseCors();

app.UseDefaultFiles(); // index.htmlをデフォルトとして扱う
app.UseStaticFiles();  // wwwrootフォルダの中身を公開する

app.UseSwagger();
app.UseSwaggerUI();

// --- エンドポイント定義 ---

// 1. カテゴリマスタ取得
app.MapGet("/categories", async () => {
    using var conn = new NpgsqlConnection(connectionString);
    const string sql = "SELECT CategoryId as Id, CategoryName as Name FROM M_Category WHERE DeleteFlg = 0";
    var categories = await conn.QueryAsync<CategoryDto>(sql);
    return Results.Ok(categories);
});

// 2. 受注履歴の取得 (追加)
app.MapGet("/orders", async (OrderService service) => {
    var orders = await service.GetOrdersAsync();
    return Results.Ok(orders);
});

// 3. 受注登録
app.MapPost("/orders", async (CreateOrderRequest req, OrderService service) => {
    try {
        var success = await service.RegisterOrderAsync(req);
        return success ? Results.Ok("登録完了") : Results.BadRequest("登録失敗");
    } catch (Exception ex) {
        return Results.Problem(ex.Message);
    }
});

// 4. 受注取消 (追加)
app.MapDelete("/orders/{orderNo}", async (string orderNo, OrderService service) => {
    try {
        var success = await service.DeleteOrderAsync(orderNo);
        return success ? Results.Ok() : Results.NotFound();
    } catch (Exception ex) {
        return Results.Problem(ex.Message);
    }
});

app.MapFallbackToFile("index.html");
app.Run();

// DTO定義
public record CategoryDto(int Id, string Name);
