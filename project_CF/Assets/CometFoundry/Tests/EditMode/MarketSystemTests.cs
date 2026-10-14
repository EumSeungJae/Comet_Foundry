using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class MarketSystemTests
    {
        [Test]
        public void Currency_UsesSolarDisplayName()
        {
            Assert.AreEqual("솔라", Currency.Name);
            Assert.AreEqual("SOL", Currency.Code);
            Assert.AreEqual("1250 솔라", Currency.Format(1250d));
        }

        [Test]
        public void TrustLevelZero_UsesExpensivePurchasesAndLowSalePrice()
        {
            CompanyState company = new CompanyState(1000d);
            Inventory inventory = new Inventory(10d);
            ResourceDefinition ore = CreateResource(100d);
            MarketSystem market = new MarketSystem();

            double purchased = market.Buy(company, inventory, ore, 2d);
            double revenue = market.Sell(company, inventory, ore, 1d);

            Assert.AreEqual(2d, purchased);
            Assert.AreEqual(650d, company.Credits);
            Assert.AreEqual(50d, revenue);
            Assert.AreEqual(700d, company.Credits);
        }

        [Test]
        public void TrustLevelThree_UsesBasePurchasePriceAndImprovedSalePrice()
        {
            CompanyState company = new CompanyState(1000d);
            company.ExchangeTrust.AddExperience(100 + 300 + 900);
            Inventory inventory = new Inventory(10d);
            ResourceDefinition ore = CreateResource(100d);
            MarketSystem market = new MarketSystem();

            double purchased = market.Buy(company, inventory, ore, 2d);
            double revenue = market.Sell(company, inventory, ore, 1d);

            Assert.AreEqual(2d, purchased);
            Assert.AreEqual(800d, company.Credits);
            Assert.AreEqual(80d, revenue);
            Assert.AreEqual(880d, company.Credits);
        }

        [Test]
        public void SetPriceMultiplier_ClampsWithinThirtyPercentOfBasePrice()
        {
            MarketSystem market = new MarketSystem();
            ResourceDefinition ore = CreateResource(100d);

            market.SetPriceMultiplier(ore, 2d);
            Assert.AreEqual(130d, market.GetUnitPrice(ore));

            market.SetPriceMultiplier(ore, 0.1d);
            Assert.AreEqual(70d, market.GetUnitPrice(ore));
        }

        [Test]
        public void UpdatePrices_RefreshesOnlyOnScheduledGameDay()
        {
            MarketSystem market = new MarketSystem(23);
            SimulationClock clock = new SimulationClock();
            ResourceDefinition ore = CreateResource(100d);
            market.PriceRefreshIntervalDays = 2d;

            market.UpdatePrices(clock, new[] { ore });
            double firstPrice = market.GetUnitPrice(ore);
            clock.AdvanceSeconds(SimulationClock.DefaultRealSecondsPerGameDay);
            market.UpdatePrices(clock, new[] { ore });

            Assert.AreEqual(firstPrice, market.GetUnitPrice(ore));

            clock.AdvanceSeconds(SimulationClock.DefaultRealSecondsPerGameDay);
            market.UpdatePrices(clock, new[] { ore });

            Assert.AreNotEqual(firstPrice, market.GetUnitPrice(ore));
        }

        [Test]
        public void RefreshPrices_KeepsPricesWithinThirtyPercentOfBasePrice()
        {
            MarketSystem market = new MarketSystem(31);
            ResourceDefinition ore = CreateResource(100d);

            for (int day = 0; day < 100; day++)
            {
                market.RefreshPrices(new[] { ore }, day);
                Assert.That(market.GetUnitPrice(ore), Is.InRange(70d, 130d));
            }
        }

        private static ResourceDefinition CreateResource(double baseMarketPrice)
        {
            ResourceDefinition resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            resource.ConfigurePrototype("ore", "Ore", ResourceCategory.Raw, baseMarketPrice, true);
            return resource;
        }
    }
}
