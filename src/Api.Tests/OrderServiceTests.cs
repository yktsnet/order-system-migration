using Microsoft.Extensions.Configuration;
using CloudNativeApp.Services;
namespace CloudNativeApp.Tests;
public class OrderServiceTests
{
    private static OrderService MakeService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=test"
            })
            .Build();
        return new OrderService(config, new TaxService(), null!);
    }
    [Theory]
    [InlineData(1000,   1,   1000,  100,   1100, false)]
    [InlineData(85000, 12, 1020000, 102000, 1122000, true)]
    [InlineData(100000, 11, 1100000, 110000, 1210000, true)]
    [InlineData(11,     1,    11,    1,      12, false)]
    public void Calculate_ReturnsCorrectValues(
        decimal price, int qty,
        decimal expectedSub, decimal expectedTax, decimal expectedTotal,
        bool expectedIsHigh)
    {
        var svc = MakeService();
        var result = svc.Calculate(price, qty);
        Assert.Equal(expectedSub,    result.SubTotal);
        Assert.Equal(expectedTax,    result.TaxAmount);
        Assert.Equal(expectedTotal,  result.TotalAmount);
        Assert.Equal(expectedIsHigh, result.IsHighAmount);
    }
    [Fact]
    public void Calculate_IsHighAmount_False_WhenTotalIsExactlyOneMillion()
    {
        var svc = MakeService();
        var result = svc.Calculate(909091, 1);
        Assert.Equal(1_000_000m, result.TotalAmount);
        Assert.False(result.IsHighAmount);
    }
    [Fact]
    public void Calculate_Tax_IsFloored_NotRounded()
    {
        var svc = MakeService();
        var result = svc.Calculate(3, 1);
        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(3m, result.TotalAmount);
    }
    [Fact]
    public void Calculate_SubTotal_IsPrice_MultipliedBy_Qty()
    {
        var svc = MakeService();
        var result = svc.Calculate(500, 7);
        Assert.Equal(3500m, result.SubTotal);
    }
}
