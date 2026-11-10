using System;
using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Services;
using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Runner;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Sample rebates: fixed-cash, fixed-rate, per-uom. Sample product: product-1.");
            Console.WriteLine("Enter rebate identifier, product identifier and volume, one per line:");
            args = new[] { Console.ReadLine(), Console.ReadLine(), Console.ReadLine() };
        }

        if (!decimal.TryParse(args[2], out var volume))
        {
            Console.WriteLine("Volume must be a number.");
            return;
        }

        var rebateDataStore = new SampleRebateDataStore();
        var rebateService = new RebateService(rebateDataStore, new SampleProductDataStore());

        var result = rebateService.Calculate(new CalculateRebateRequest
        {
            RebateIdentifier = args[0],
            ProductIdentifier = args[1],
            Volume = volume,
        });

        Console.WriteLine($"Success: {result.Success}");

        if (result.Success)
        {
            Console.WriteLine($"Rebate amount: {rebateDataStore.StoredRebateAmount}");
        }
    }

    // The real data stores have their database code removed, so the runner uses sample data.
    private class SampleRebateDataStore : IRebateDataStore
    {
        public decimal StoredRebateAmount { get; private set; }

        public Rebate GetRebate(string rebateIdentifier)
        {
            switch (rebateIdentifier)
            {
                case "fixed-cash":
                    return new Rebate { Incentive = IncentiveType.FixedCashAmount, Amount = 50m };

                case "fixed-rate":
                    return new Rebate { Incentive = IncentiveType.FixedRateRebate, Percentage = 0.1m };

                case "per-uom":
                    return new Rebate { Incentive = IncentiveType.AmountPerUom, Amount = 2m };

                default:
                    return null;
            }
        }

        public void StoreCalculationResult(Rebate rebate, decimal rebateAmount) => StoredRebateAmount = rebateAmount;
    }

    private class SampleProductDataStore : IProductDataStore
    {
        public Product GetProduct(string productIdentifier)
        {
            if (productIdentifier != "product-1")
            {
                return null;
            }

            return new Product { Price = 100m, SupportedIncentives = SupportedIncentiveType.FixedCashAmount | SupportedIncentiveType.FixedRateRebate | SupportedIncentiveType.AmountPerUom };
        }
    }
}
