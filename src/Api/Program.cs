using Amazon.S3;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// --- 設定の読み込み ---
// 1. 環境変数や appsettings.json から値を取得します
// 第2引数は、設定がない場合のデフォルト値（ローカル実行用）です
var serviceUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
var bucketName = builder.Configuration["AWS:BucketName"] ?? "my-local-training-bucket";

// 2. AWS SDK (S3) の登録
builder.Services.AddSingleton<IAmazonS3>(sp => 
{
    var config = new AmazonS3Config
    {
        ServiceURL = serviceUrl, // 外部から注入されたURLを使用
        ForcePathStyle = true
    };
    // LocalStack用なので認証情報はダミーでOK
    return new AmazonS3Client("test", "test", config);
});

var app = builder.Build();

// --- エンドポイントの実装 ---

app.MapGet("/buckets", async (IAmazonS3 s3Client) =>
{
    var response = await s3Client.ListBucketsAsync();
    return Results.Ok(response.Buckets.Select(b => b.BucketName));
});

app.MapPost("/upload", async (IAmazonS3 s3Client, [FromQuery] string fileName) =>
{
    var request = new Amazon.S3.Model.PutObjectRequest
    {
        BucketName = bucketName, // 外部から注入されたバケット名を使用
        Key = fileName,
        ContentBody = $"Hello Cloud Native! Created at {DateTime.Now} (Source: {Environment.MachineName})"
    };

    await s3Client.PutObjectAsync(request);
    return Results.Ok($"'{fileName}' を '{bucketName}' にアップロード完了！");
});

app.Run();
