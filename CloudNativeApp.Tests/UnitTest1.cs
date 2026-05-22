using Amazon.S3;
using Xunit;

namespace CloudNativeApp.Tests;

public class S3IntegrationTest
{
    private readonly IAmazonS3 _s3Client;

    public S3IntegrationTest()
    {
        // テスト時も LocalStack を見に行くように設定
        var config = new AmazonS3Config
        {
            ServiceURL = "http://localhost:4566",
            ForcePathStyle = true
        };
        _s3Client = new AmazonS3Client("test", "test", config);
    }

    [Fact]
    public async Task LocalStack_ShouldHave_OurTrainingBucket()
    {
        // Arrange (準備): Terraformで作ったはずのバケット名
        var expectedBucket = "my-local-training-bucket";

        // Act (実行): バケット一覧を取得
        var response = await _s3Client.ListBucketsAsync();
        var bucketNames = response.Buckets.Select(b => b.BucketName);

        // Assert (検証): 期待したバケットが存在するか
        Assert.Contains(expectedBucket, bucketNames);
    }
}
