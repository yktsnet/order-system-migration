using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using CloudNativeApp.Services;

namespace CloudNativeApp.Tests;

// OrderService.RegisterOrderAsync / DeleteOrderAsync のドメインルールテスト。
// 「登録」「取消」の2アクションのみで完結し、修正(update)処理は許容しないことを前提に、
// DBアクセス(Npgsql直結)は IOrderRepository のインメモリ実装に差し替えて検証する。
public class OrderServiceRegisterDeleteTests
{
    // 実DBのトランザクション（登録+在庫減算 / 取消+在庫復元 を1操作として扱う）を模した
    // テスト用インメモリリポジトリ。ThrowOnRegister を立てると、実際のトランザクション同様
    // 途中で失敗した場合に加えた変更を取り消してから例外を投げる。
    private class FakeOrderRepository : IOrderRepository
    {
        public Dictionary<string, int> Stock { get; } = new();
        public Dictionary<string, (string ItemName, int Qty)> Orders { get; } = new();
        public decimal? LastRegisteredTotalAmount { get; private set; }
        public bool ThrowOnRegister { get; set; }

        public Task<bool> RegisterOrderAsync(CreateOrderRequest req, DateTime orderDate, decimal totalAmount)
        {
            var stockBefore = Stock.TryGetValue(req.ItemName, out var current) ? current : 0;

            // トランザクション内の操作: 受注登録 + 在庫減算
            Orders[req.OrderNo] = (req.ItemName, req.Qty);
            Stock[req.ItemName] = stockBefore - req.Qty;

            if (ThrowOnRegister)
            {
                // ロールバック: 加えた変更をすべて取り消してから例外を伝播する
                Orders.Remove(req.OrderNo);
                Stock[req.ItemName] = stockBefore;
                throw new InvalidOperationException("simulated register failure");
            }

            LastRegisteredTotalAmount = totalAmount;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteOrderAsync(string orderNo)
        {
            if (!Orders.TryGetValue(orderNo, out var order))
            {
                return Task.FromResult(false);
            }

            var stockBefore = Stock.TryGetValue(order.ItemName, out var current) ? current : 0;
            Stock[order.ItemName] = stockBefore + order.Qty;
            Orders.Remove(orderNo);
            return Task.FromResult(true);
        }
    }

    private static (OrderService Service, FakeOrderRepository Repository) MakeService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=test"
            })
            .Build();
        var repository = new FakeOrderRepository();
        var service = new OrderService(config, new TaxService(), null!, repository);
        return (service, repository);
    }

    [Fact]
    public async Task RegisterOrderAsync_DecrementsStock_ByOrderedQty()
    {
        var (service, repo) = MakeService();
        repo.Stock["ボールペン"] = 100;
        var req = new CreateOrderRequest("O-001", "得意先A", 1, "ボールペン", 100m, 5);

        var result = await service.RegisterOrderAsync(req);

        Assert.True(result);
        Assert.Equal(95, repo.Stock["ボールペン"]);
    }

    [Fact]
    public async Task RegisterOrderAsync_TotalAmount_MatchesTaxServiceCalculation()
    {
        var (service, repo) = MakeService();
        repo.Stock["ノート"] = 50;
        var req = new CreateOrderRequest("O-002", "得意先B", 1, "ノート", 1000m, 3);

        await service.RegisterOrderAsync(req);

        var expectedSubTotal = 1000m * 3;
        var expectedTotal = expectedSubTotal + new TaxService().Calculate(expectedSubTotal);
        Assert.Equal(expectedTotal, repo.LastRegisteredTotalAmount);
    }

    [Fact]
    public async Task RegisterOrderAsync_WhenExceptionOccurs_RollsBack_StockAndOrderUnchanged()
    {
        var (service, repo) = MakeService();
        repo.Stock["消しゴム"] = 20;
        repo.ThrowOnRegister = true;
        var req = new CreateOrderRequest("O-003", "得意先C", 1, "消しゴム", 50m, 2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterOrderAsync(req));

        Assert.Equal(20, repo.Stock["消しゴム"]);
        Assert.False(repo.Orders.ContainsKey("O-003"));
    }

    [Fact]
    public async Task DeleteOrderAsync_WhenOrderExists_RestoresStock_AndDeletesOrder()
    {
        var (service, repo) = MakeService();
        repo.Stock["定規"] = 30;
        await service.RegisterOrderAsync(new CreateOrderRequest("O-004", "得意先D", 1, "定規", 100m, 4));
        Assert.Equal(26, repo.Stock["定規"]); // 登録直後は在庫が減算されている

        var result = await service.DeleteOrderAsync("O-004");

        Assert.True(result);
        Assert.Equal(30, repo.Stock["定規"]);
        Assert.False(repo.Orders.ContainsKey("O-004"));
    }

    [Fact]
    public async Task DeleteOrderAsync_WhenOrderDoesNotExist_ReturnsFalse_AndDoesNotChangeStock()
    {
        var (service, repo) = MakeService();
        repo.Stock["ホチキス"] = 10;

        var result = await service.DeleteOrderAsync("NOT-EXIST");

        Assert.False(result);
        Assert.Equal(10, repo.Stock["ホチキス"]);
    }

    // アーキテクチャ上のルール: 「更新(PUT/PATCH)エンドポイントは存在せず、
    // 登録(POST)と取消(DELETE)の2アクションのみで完結する」ことを Program.cs のエンドポイント
    // 定義から確認する。既存の Program.cs 自体は変更しない。
    [Fact]
    public void ProgramCs_HasNoUpdateEndpoints_OnlyRegisterAndDeleteActionsExist()
    {
        var programPath = Path.GetFullPath(
            Path.Combine(Path.GetDirectoryName(GetThisFilePath())!, "..", "Api", "Program.cs"));
        var source = File.ReadAllText(programPath);

        Assert.DoesNotContain("MapPut(", source);
        Assert.DoesNotContain("MapPatch(", source);
        Assert.Contains("MapPost(\"/orders\"", source);
        Assert.Contains("MapDelete(\"/orders/{orderNo}\"", source);
    }

    private static string GetThisFilePath([CallerFilePath] string path = "") => path;
}
