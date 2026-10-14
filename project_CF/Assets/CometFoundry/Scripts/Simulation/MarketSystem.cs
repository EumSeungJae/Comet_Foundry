using System;
using System.Collections.Generic;
using CometFoundry.Core;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    public sealed class MarketSystem
    {
        private static readonly double[] purchaseMultipliersByTrustLevel = { 1.75d, 1.5d, 1.25d, 1d };
        private static readonly double[] saleMultipliersByTrustLevel = { 0.5d, 0.6d, 0.7d, 0.8d };

        public const double MinPriceMultiplier = 0.7d;
        public const double MaxPriceMultiplier = 1.3d;
        public const double DefaultPriceRefreshIntervalDays = 1d;
        public const double DefaultMaxPriceShiftPerRefresh = 0.12d;

        private readonly Dictionary<ResourceDefinition, double> priceMultipliers = new Dictionary<ResourceDefinition, double>();
        private readonly Dictionary<ResourceMarketTag, double> eventDemandMultipliers = new Dictionary<ResourceMarketTag, double>();
        private readonly Random random;
        private double nextPriceRefreshDay;

        public MarketSystem(int seed = 0)
        {
            random = seed == 0 ? new Random() : new Random(seed);
        }

        public double PriceRefreshIntervalDays { get; set; } = DefaultPriceRefreshIntervalDays;
        public double MaxPriceShiftPerRefresh { get; set; } = DefaultMaxPriceShiftPerRefresh;
        public double NextPriceRefreshDay => nextPriceRefreshDay;

        public void SetPriceMultiplier(ResourceDefinition resource, double multiplier)
        {
            if (resource != null)
            {
                priceMultipliers[resource] = ClampPriceMultiplier(multiplier <= 0d ? 1d : multiplier);
            }
        }

        public void UpdatePrices(SimulationClock clock, IReadOnlyList<ResourceDefinition> resources)
        {
            if (clock == null)
            {
                return;
            }

            if (nextPriceRefreshDay <= 0d || clock.Day >= nextPriceRefreshDay)
            {
                RefreshPrices(resources, clock.Day);
            }
        }

        public void RefreshPrices(IReadOnlyList<ResourceDefinition> resources, double currentDay)
        {
            if (resources != null)
            {
                for (int i = 0; i < resources.Count; i++)
                {
                    ResourceDefinition resource = resources[i];
                    if (resource == null)
                    {
                        continue;
                    }

                    double current = priceMultipliers.TryGetValue(resource, out double value) ? value : 1d;
                    double shift = (random.NextDouble() * 2d - 1d) * Math.Max(0d, MaxPriceShiftPerRefresh);
                    priceMultipliers[resource] = ClampPriceMultiplier(current + shift);
                }
            }

            nextPriceRefreshDay = currentDay + Math.Max(0.0001d, PriceRefreshIntervalDays);
        }

        public double GetUnitPrice(ResourceDefinition resource)
        {
            if (resource == null)
            {
                return 0d;
            }

            double multiplier = priceMultipliers.TryGetValue(resource, out double value) ? value : 1d;
            return resource.BaseMarketPrice * multiplier * GetEventDemandMultiplier(resource);
        }

        public void SetEventDemandMultiplier(ResourceMarketTag tag, double multiplier)
        {
            if (tag == ResourceMarketTag.None)
            {
                return;
            }

            eventDemandMultipliers[tag] = multiplier <= 0d ? 1d : multiplier;
        }

        public void ClearEventDemandMultiplier(ResourceMarketTag tag)
        {
            if (tag != ResourceMarketTag.None)
            {
                eventDemandMultipliers.Remove(tag);
            }
        }

        public double GetPurchaseUnitPrice(CompanyState company, ResourceDefinition resource)
        {
            return GetUnitPrice(resource) * GetPurchaseMultiplier(company);
        }

        public double GetSaleUnitPrice(CompanyState company, ResourceDefinition resource)
        {
            return GetUnitPrice(resource) * GetSaleMultiplier(company);
        }

        public double Buy(CompanyState company, Inventory inventory, ResourceDefinition resource, double tons)
        {
            return BuyStacks(company, inventory, resource, tons);
        }

        public double BuyStacks(CompanyState company, Inventory inventory, ResourceDefinition resource, double stacks)
        {
            if (company == null || inventory == null || resource == null || stacks <= 0d)
            {
                return 0d;
            }

            double purchasableStacks = System.Math.Min(stacks, inventory.FreeStacks);
            double cost = purchasableStacks * GetPurchaseUnitPrice(company, resource);
            if (purchasableStacks <= 0d || !company.Spend(cost))
            {
                return 0d;
            }

            return inventory.AddStacks(resource, purchasableStacks);
        }

        public double Sell(CompanyState company, Inventory inventory, ResourceDefinition resource, double tons)
        {
            return SellStacks(company, inventory, resource, tons);
        }

        public double SellStacks(CompanyState company, Inventory inventory, ResourceDefinition resource, double stacks)
        {
            if (company == null || inventory == null || resource == null || stacks <= 0d)
            {
                return 0d;
            }

            double sold = inventory.RemoveStacks(resource, stacks);
            double revenue = sold * GetSaleUnitPrice(company, resource);
            company.Earn(revenue);
            return revenue;
        }

        public static double GetPurchaseMultiplier(CompanyState company)
        {
            int trustLevel = company == null ? ExchangeTrustState.MinLevel : company.ExchangeTrust.Level;
            return purchaseMultipliersByTrustLevel[trustLevel];
        }

        public static double GetSaleMultiplier(CompanyState company)
        {
            int trustLevel = company == null ? ExchangeTrustState.MinLevel : company.ExchangeTrust.Level;
            return saleMultipliersByTrustLevel[trustLevel];
        }

        private static double ClampPriceMultiplier(double multiplier)
        {
            if (multiplier < MinPriceMultiplier)
            {
                return MinPriceMultiplier;
            }

            return multiplier > MaxPriceMultiplier ? MaxPriceMultiplier : multiplier;
        }

        private double GetEventDemandMultiplier(ResourceDefinition resource)
        {
            double multiplier = 1d;
            foreach (KeyValuePair<ResourceMarketTag, double> entry in eventDemandMultipliers)
            {
                if (resource.HasMarketTag(entry.Key))
                {
                    multiplier *= entry.Value;
                }
            }

            return multiplier;
        }
    }
}
