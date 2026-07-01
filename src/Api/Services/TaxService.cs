namespace CloudNativeApp.Services;

public class TaxService
{
    // 税率。テストから期待値の算出に利用できるよう公開する。
    public const decimal TaxRate = 0.1m;

    /// <summary>
    /// 消費税額を算出する。端数は切り捨て（四捨五入・切り上げは行わない）。
    /// </summary>
    public decimal Calculate(decimal subTotal)
        => Math.Floor(subTotal * TaxRate);
}
