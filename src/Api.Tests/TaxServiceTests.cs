using CloudNativeApp.Services;

namespace CloudNativeApp.Tests;

// TaxService.Calculate の単体テスト。端数切り捨てと境界値を網羅する。
public class TaxServiceTests
{
    [Fact]
    public void Calculate_ZeroSubTotal_ReturnsZero()
    {
        var svc = new TaxService();
        Assert.Equal(0m, svc.Calculate(0m));
    }

    [Theory]
    [InlineData(9, 0)]      // 9 * 0.1 = 0.9  -> 切り捨てで 0
    [InlineData(10, 1)]     // 10 * 0.1 = 1.0 -> ちょうど 1（切り捨て発生なし）
    [InlineData(11, 1)]     // 11 * 0.1 = 1.1 -> 切り捨てで 1
    [InlineData(19, 1)]     // 19 * 0.1 = 1.9 -> 切り捨てで 1
    [InlineData(999, 99)]   // 999 * 0.1 = 99.9 -> 切り捨てで 99
    [InlineData(1000, 100)] // 1000 * 0.1 = 100.0 -> ちょうど 100
    public void Calculate_FloorsFractionalYen_DoesNotRound(decimal subTotal, decimal expectedTax)
    {
        var svc = new TaxService();
        Assert.Equal(expectedTax, svc.Calculate(subTotal));
    }

    [Fact]
    public void Calculate_SmallSubTotal_LessThanOneYenTax_FlooredToZero()
    {
        var svc = new TaxService();
        // 5 * 0.1 = 0.5 -> 四捨五入なら 1 円になるが、業務ルールは切り捨てなので 0 円
        Assert.Equal(0m, svc.Calculate(5m));
    }

    [Fact]
    public void Calculate_UsesPublicTaxRate_ForExpectedValueCalculation()
    {
        var svc = new TaxService();
        var subTotal = 12345m;
        var expected = Math.Floor(subTotal * TaxService.TaxRate);
        Assert.Equal(expected, svc.Calculate(subTotal));
    }
}
