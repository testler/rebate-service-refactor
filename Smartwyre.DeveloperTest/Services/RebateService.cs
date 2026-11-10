using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Services;

public class RebateService : IRebateService
{
    private readonly IRebateDataStore _rebateDataStore;
    private readonly IProductDataStore _productDataStore;

    public RebateService(IRebateDataStore rebateDataStore, IProductDataStore productDataStore)
    {
        _rebateDataStore = rebateDataStore;
        _productDataStore = productDataStore;
    }

    public CalculateRebateResult Calculate(CalculateRebateRequest request)
    {
        var rebate = _rebateDataStore.GetRebate(request.RebateIdentifier);
        var product = _productDataStore.GetProduct(request.ProductIdentifier);

        var canCalculateRebate = false;
        var rebateAmount = 0m;

        // Rebate and product must both exist.
        if (rebate != null && product != null)
        {
            // Product must support the incentive, and the values its formula uses must not be zero.
            switch (rebate.Incentive)
            {
                case IncentiveType.FixedCashAmount:
                    canCalculateRebate = product.SupportedIncentives.HasFlag(SupportedIncentiveType.FixedCashAmount) && rebate.Amount != 0;
                    rebateAmount = rebate.Amount;
                    break;

                case IncentiveType.FixedRateRebate:
                    canCalculateRebate = product.SupportedIncentives.HasFlag(SupportedIncentiveType.FixedRateRebate) && rebate.Percentage != 0 && product.Price != 0 && request.Volume != 0;
                    rebateAmount = product.Price * rebate.Percentage * request.Volume;
                    break;

                case IncentiveType.AmountPerUom:
                    canCalculateRebate = product.SupportedIncentives.HasFlag(SupportedIncentiveType.AmountPerUom) && rebate.Amount != 0 && request.Volume != 0;
                    rebateAmount = rebate.Amount * request.Volume;
                    break;
            }
        }

        var result = new CalculateRebateResult { Success = canCalculateRebate };

        if (result.Success)
        {
            _rebateDataStore.StoreCalculationResult(rebate, rebateAmount);
        }

        return result;
    }
}
