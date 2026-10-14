using System.Linq;
using CometFoundry.Simulation;
using CometFoundry.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed partial class FoundryProductionTests
    {
        [Test] public void ChartRaster_DrawsContinuousLineAndSplitsColorAtRegularPrice()
        {
            var points = new[] { new FoundryHistoryPoint(0, 55), new FoundryHistoryPoint(10, 145) };
            var texture = FoundryChartRaster.Create(points, 101, 101, 55, 145, 100, Color.white);
            try
            {
                var pixels = texture.GetPixels32();
                for (int x = 0; x <= 100; x++)
                    Assert.Greater(pixels[x * 101 + x].a, 0, "Every pixel along the rising diagonal must be connected.");
                Assert.AreEqual(FoundryChartRaster.Fall, pixels[25 * 101 + 25]);
                Assert.AreEqual(FoundryChartRaster.Rise, pixels[75 * 101 + 75]);
                Assert.AreEqual(0, pixels[100 * 101].a, "Drawing must stay on the intended curve.");
                Assert.AreEqual(0, pixels[100].a);
            }
            finally { Object.DestroyImmediate(texture); }
        }
        [Test] public void ChartRaster_HandlesBoundaryValuesAndSingleBalancePoint()
        {
            var texture = FoundryChartRaster.Create(new[] { new FoundryHistoryPoint(0, 100) }, 100, 50, 90, 110, 0, Color.green);
            try { Assert.IsTrue(texture.GetPixels32().Any(p => p.a > 0)); }
            finally { Object.DestroyImmediate(texture); }
        }
        private static void Market(FoundrySession s) => BuildRecipe(s, "market-terminal", 12, 7, null);
        [Test] public void Trade_AllGradesConserveStockAndMoneyAndRejectInvalidAmounts()
        {
            using (var s = new FoundrySession())
            {
                var r = s.Content.FindResource("iron-ore");
                Assert.IsFalse(s.Trade(r, true, 1, out _)); Market(s);
                double cash = s.Company.Credits, stock = s.TotalStock(r), buy = s.BuyUnitPrice(r);
                Assert.IsTrue(s.Trade(r, true, 7, out _)); Assert.AreEqual(stock + 7, s.TotalStock(r), 1e-8);
                Assert.AreEqual(cash - buy * 7, s.Company.Credits, 1e-8);
                double sell = s.SellUnitPrice(r); Assert.IsTrue(s.Trade(r, false, 3, out _));
                Assert.AreEqual(stock + 4, s.TotalStock(r), 1e-8); Assert.AreEqual(cash - buy * 7 + sell * 3, s.Company.Credits, 1e-8);
                cash = s.Company.Credits; Assert.IsFalse(s.Trade(r, false, 999, out _)); Assert.IsFalse(s.Trade(r, true, 0, out _));
                Assert.AreEqual(cash, s.Company.Credits);
                foreach (var resource in s.Content.Resources) Assert.IsTrue(s.Trade(resource, true, 1, out var message), message);
            }
        }
        [TestCase(6)] [TestCase(12)] [TestCase(18)] [TestCase(24)]
        public void PriceGraph_CoversRequestedMonthsWithReproduciblePositiveValues(int months)
        {
            using (var s = new FoundrySession())
            {
                var r = s.Content.FindResource("iron"); var points = s.PriceHistory(r, months);
                Assert.AreEqual(s.Month * 30 - months * 30, points.First().day, 1e-8); Assert.AreEqual(s.Month * 30, points.Last().day, 1e-8);
                Assert.IsTrue(points.All(p => p.value > 0)); CollectionAssert.AreEqual(points.Select(p => p.value), s.PriceHistory(r, months).Select(p => p.value));
                Assert.AreEqual(1, s.BalanceHistory(months).Count, "Player balance must not invent a pre-game history.");
            }
        }
        [Test] public void Survey_ThreeCandidates_ColonizationChargesPortAndAllowsSelection()
        {
            using (var s = new FoundrySession())
            {
                Market(s); Assert.IsTrue(s.Explore(out _)); Assert.AreEqual(0, s.Prospects.Count); s.Advance(8.1);
                Assert.AreEqual(3, s.Prospects.Count); Assert.AreEqual(3, s.Prospects.Select(p => p.State.DistanceCode).Distinct().Count());
                var offer = s.Prospects[1]; double cash = s.Company.Credits;
                Assert.IsTrue(s.Colonize(1, out var message), message); Assert.AreEqual(cash - offer.Cost, s.Company.Credits, 1e-8);
                Assert.AreEqual(2, s.Colonies.Count); Assert.AreEqual(0, s.Prospects.Count);
                Assert.IsNotNull(s.Dock(s.Colonies[1])); Assert.IsTrue(s.SwitchColony(1)); Assert.AreSame(offer.State, s.Comet); Assert.IsTrue(s.HasMarket);
            }
        }
        [Test] public void Shipping_SaveInFlight_RestoresAndDeliversExactCargo()
        {
            using (var s = new FoundrySession()) using (var restored = new FoundrySession())
            {
                Market(s); s.Explore(out _); s.Advance(8.1); Assert.IsTrue(s.Colonize(0, out _));
                var r = s.Content.FindResource("water"); Assert.IsTrue(s.Trade(r, true, 20, out _));
                Assert.IsFalse(s.Dispatch(0, 0, 0, r, 10, out _));
                Assert.IsTrue(s.Dispatch(0, 0, 1, r, 10, out var msg), msg);
                Assert.AreEqual(10, s.Stock(s.Colonies[0], r)); Assert.AreEqual(10, s.Company.Ships[0].Cargo.GetAmount(r));
                string save = s.ExportState(); Assert.IsTrue(restored.ImportState(save));
                var rr = restored.Content.FindResource("water"); Assert.AreEqual(1, restored.Voyages.Count);
                restored.Advance(restored.Voyages[0].Duration + 1);
                Assert.AreEqual(10, restored.Stock(restored.Colonies[1], rr), 1e-8); Assert.AreEqual(10, restored.Stock(restored.Colonies[0], rr), 1e-8);
                Assert.AreEqual(0, restored.Company.Ships[0].Cargo.UsedStacks); Assert.AreEqual(0, restored.Voyages.Count);
                double cash = restored.Company.Credits; Assert.IsFalse(restored.ImportState("{}")); Assert.AreEqual(cash, restored.Company.Credits);
            }
        }
        [Test] public void Prices_NormalLockHoldsWithoutSyntheticEvents()
        {
            using (var s = new FoundrySession())
            {
                foreach (var r in s.Content.Resources)
                {
                    Assert.Greater(r.BaseMarketPrice, 0);
                    for (double day = -720; day <= 720; day += .5)
                    {
                        double ratio = s.PriceRatio(r, day); bool active = s.MarketEventAt(r, day) != null;
                        Assert.GreaterOrEqual(ratio, active ? .55 - 1e-10 : .70 - 1e-10);
                        Assert.LessOrEqual(ratio, active ? 1.45 + 1e-10 : 1.30 + 1e-10);
                        Assert.IsFalse(active, "No playable market events are implemented.");
                    }

                }
            }
        }
        [Test] public void NoSyntheticEventPeak_AndSavePreservesMonthlyQuote()
        {
            using (var s = new FoundrySession())
            {
                var r = s.Content.Resources[0];
                Assert.IsNull(s.MarketEventAt(r, 60)); Assert.LessOrEqual(s.PriceRatio(r, 60), 1.30);
                Assert.IsNull(s.MarketEventAt(r, 150)); Assert.GreaterOrEqual(s.PriceRatio(r, 150), .70);
                Assert.IsNull(s.MarketEventAt(r, 90)); Assert.LessOrEqual(s.PriceRatio(r, 90), 1.30);
                string save = s.ExportState(); using (var loaded = new FoundrySession())
                { Assert.IsTrue(loaded.ImportState(save)); Assert.AreEqual(s.PriceRatio(r, 60), loaded.PriceRatio(loaded.Content.Resources[0], 60)); }
            }
        }
        [Test] public void FullPort_KeepsUndeliveredCargoUntilSpaceBecomesAvailable()
        {
            using (var s = new FoundrySession())
            {
                Market(s); s.Explore(out _); s.Advance(8.1); s.Colonize(0, out _);
                var r = s.Content.FindResource("water"); s.Trade(r, true, 10, out _);
                Assert.IsTrue(s.Dispatch(0, 0, 1, r, 10, out _));
                var dock = s.Dock(s.Colonies[1]); dock.Inventory.AddStacks(r, dock.Inventory.CapacityStacks);
                s.Advance(s.Voyages[0].Duration + 1); Assert.AreEqual(10, s.Company.Ships[0].Cargo.GetAmount(r));
                dock.Inventory.RemoveStacks(r, 4); s.Advance(1); Assert.AreEqual(6, s.Company.Ships[0].Cargo.GetAmount(r));
                dock.Inventory.RemoveStacks(r, 6); s.Advance(1); Assert.AreEqual(0, s.Company.Ships[0].Cargo.UsedStacks); Assert.AreEqual(0, s.Voyages.Count);
            }
        }
        [Test] public void BackgroundColony_ContinuesProducingWhileAnotherIsSelected()
        {
            using (var s = new FoundrySession())
            {
                Market(s); BuildRecipe(s, "refinery-basic", 5, 3, "smelt-iron");
                s.Explore(out _); s.Advance(8.1); s.Colonize(0, out _);
                double made = s.TutorialIronMade; s.SwitchColony(1); s.Advance(60);
                Assert.Greater(s.TutorialIronMade, made); Assert.AreEqual(1, s.ActiveColonyIndex);
            }
        }
        [Test] public void Tutorial_ActualConstructionProductionAndTradesCompleteAllSteps()
        {
            using (var s = new FoundrySession())
            {
                Assert.AreEqual(FoundryTutorialStep.BuildRefinery, s.TutorialStep);
                BuildRecipe(s, "refinery-basic", 5, 3, "smelt-iron"); s.RefreshTutorial();
                Assert.AreEqual(FoundryTutorialStep.ProduceIron, s.TutorialStep); s.Advance(60);
                Assert.AreEqual(FoundryTutorialStep.BuildFactory, s.TutorialStep);
                BuildRecipe(s, "factory-metal", 8, 3, "roll-plate"); s.Advance(100);
                Assert.AreEqual(FoundryTutorialStep.BuildMarket, s.TutorialStep); Market(s);
                Assert.AreEqual(FoundryTutorialStep.SellMaterial, s.TutorialStep);
                Assert.IsTrue(s.Trade(s.Content.FindResource("plate"), false, 1, out var msg), msg);
                Assert.AreEqual(FoundryTutorialStep.BuyMaterial, s.TutorialStep);
                Assert.IsTrue(s.Trade(s.Content.FindResource("water"), true, 1, out _)); Assert.AreEqual(FoundryTutorialStep.Complete, s.TutorialStep);
            }
        }
    }
}
