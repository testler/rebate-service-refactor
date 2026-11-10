using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Services;
using Smartwyre.DeveloperTest.Types;
using Xunit;

namespace Smartwyre.DeveloperTest.Tests;

public class RebateServiceTests
{
    private readonly FakeRebateDataStore _rebateDataStore = new();
    private readonly FakeProductDataStore _productDataStore = new();

    public RebateServiceTests()
    {
        _rebateDataStore.Rebate = new Rebate { Amount = 2m, Percentage = 0.1m };
        _productDataStore.Product = new Product
        {
            Price = 100m,
            SupportedIncentives = SupportedIncentiveType.FixedCashAmount | SupportedIncentiveType.FixedRateRebate | SupportedIncentiveType.AmountPerUom,
        };
    }

    [Theory]
    [InlineData(IncentiveType.FixedCashAmount, 2)]
    [InlineData(IncentiveType.FixedRateRebate, 50)]
    [InlineData(IncentiveType.AmountPerUom, 10)]
    public void Calculate_SupportedIncentive_SucceedsAndStoresRebateAmount(IncentiveType incentive, int expectedRebateAmount)
    {
        _rebateDataStore.Rebate.Incentive = incentive;

        var result = CalculateWithVolume(5m);

        Assert.True(result.Success);
        Assert.Equal(expectedRebateAmount, _rebateDataStore.StoredRebateAmount);
    }

    [Fact]
    public void Calculate_RebateNotFound_FailsAndStoresNothing()
    {
        _rebateDataStore.Rebate = null;

        AssertFailedAndNothingStored(CalculateWithVolume(5m));
    }

    [Fact]
    public void Calculate_ProductNotFound_FailsAndStoresNothing()
    {
        _productDataStore.Product = null;

        AssertFailedAndNothingStored(CalculateWithVolume(5m));
    }

    [Fact]
    public void Calculate_ProductDoesNotSupportIncentive_FailsAndStoresNothing()
    {
        _rebateDataStore.Rebate.Incentive = IncentiveType.FixedCashAmount;
        _productDataStore.Product.SupportedIncentives = SupportedIncentiveType.AmountPerUom;

        AssertFailedAndNothingStored(CalculateWithVolume(5m));
    }

    [Fact]
    public void Calculate_ZeroVolume_FailsAndStoresNothing()
    {
        _rebateDataStore.Rebate.Incentive = IncentiveType.AmountPerUom;

        AssertFailedAndNothingStored(CalculateWithVolume(0m));
    }

    [Fact]
    public void Calculate_ZeroRebateAmount_FailsAndStoresNothing()
    {
        _rebateDataStore.Rebate = new Rebate { Incentive = IncentiveType.FixedCashAmount, Amount = 0m };

        AssertFailedAndNothingStored(CalculateWithVolume(5m));
    }

    [Fact]
    public void Calculate_ZeroPercentage_FailsAndStoresNothing()
    {
        _rebateDataStore.Rebate.Incentive = IncentiveType.FixedRateRebate;
        _rebateDataStore.Rebate.Percentage = 0m;

        AssertFailedAndNothingStored(CalculateWithVolume(5m));
    }

    [Fact]
    public void Calculate_ZeroPrice_FailsAndStoresNothing()
    {
        _rebateDataStore.Rebate.Incentive = IncentiveType.FixedRateRebate;
        _productDataStore.Product.Price = 0m;

        AssertFailedAndNothingStored(CalculateWithVolume(5m));
    }

    private CalculateRebateResult CalculateWithVolume(decimal volume)
    {
        var rebateService = new RebateService(_rebateDataStore, _productDataStore);
        return rebateService.Calculate(new CalculateRebateRequest { Volume = volume });
    }

    private void AssertFailedAndNothingStored(CalculateRebateResult result)
    {
        Assert.False(result.Success);
        Assert.Null(_rebateDataStore.StoredRebateAmount);
    }

    private class FakeRebateDataStore : IRebateDataStore
    {
        public Rebate Rebate { get; set; }
        public decimal? StoredRebateAmount { get; private set; }

        public Rebate GetRebate(string rebateIdentifier) => Rebate;
        public void StoreCalculationResult(Rebate rebate, decimal rebateAmount) => StoredRebateAmount = rebateAmount;
    }

    private class FakeProductDataStore : IProductDataStore
    {
        public Product Product { get; set; }

        public Product GetProduct(string productIdentifier) => Product;
    }
}
