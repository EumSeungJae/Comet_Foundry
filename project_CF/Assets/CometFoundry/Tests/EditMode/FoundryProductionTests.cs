using System.Linq;
using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed partial class FoundryProductionTests
    {
        private FoundryContent content;
        [SetUp] public void SetUp() => content = new FoundryContent();
        [TearDown] public void TearDown() => content.Dispose();
        private FacilityInstance Refinery(double capacity = 60)
        {
            var definition = content.FindBuilding("refinery-basic");
            var facility = new FacilityInstance(new SimulationId("test"), definition);
            facility.Inventory.SetCapacity(capacity);
            facility.AssignRecipe(content.FindRecipe("smelt-iron"));
            return facility;
        }
        [Test] public void Batch_ConsumesExactMaterialsAndCostOnlyAtCompletion()
        {
            var facility = Refinery(); var company = new CompanyState(100);
            facility.Inventory.AddStacks(content.FindResource("iron-ore"), 3);
            Assert.IsFalse(facility.TickProduction(2d / 300, company));
            Assert.AreEqual(100, company.Credits);
            Assert.AreEqual(3, facility.Inventory.GetAmount(content.FindResource("iron-ore")));
            Assert.IsTrue(facility.TickProduction(3d / 300, company));
            Assert.AreEqual(96, company.Credits);
            Assert.AreEqual(0, facility.Inventory.GetAmount(content.FindResource("iron-ore")));
            Assert.AreEqual(2, facility.Inventory.GetAmount(content.FindResource("iron")));
        }
        [Test] public void FullOutputInventory_DoesNotConsumeIceOrLoseWater()
        {
            var facility = Refinery(2); var company = new CompanyState(100);
            facility.AssignRecipe(content.FindRecipe("melt-ice"));
            facility.Inventory.AddStacks(content.FindResource("ice"), 2);
            Assert.IsFalse(facility.TickProduction(10d / 300, company));
            Assert.AreEqual(2, facility.Inventory.GetAmount(content.FindResource("ice")));
            Assert.AreEqual(0, facility.Inventory.GetAmount(content.FindResource("water")));
            Assert.AreEqual(100, company.Credits);
            facility.Inventory.SetCapacity(3);
            Assert.IsTrue(facility.TickProduction(4d / 300, company));
            Assert.AreEqual(3, facility.Inventory.GetAmount(content.FindResource("water")));
        }
        [Test] public void ReselectingSameRecipe_PreservesProgress_ChangingRecipeResetsIt()
        {
            var facility = Refinery(); var company = new CompanyState(100);
            facility.Inventory.AddStacks(content.FindResource("iron-ore"), 9);
            facility.TickProduction(2d / 300, company);
            facility.AssignRecipe(content.FindRecipe("smelt-iron"));
            Assert.AreEqual(2d / 300, facility.ProgressDays, 1e-8);
            facility.AssignRecipe(content.FindRecipe("smelt-copper"));
            Assert.AreEqual(0, facility.ProgressDays);
            Assert.AreEqual(9, facility.Inventory.GetAmount(content.FindResource("iron-ore")));
        }
        [Test] public void InsufficientCredits_PausesWithoutMaterialLoss()
        {
            var facility = Refinery(); var company = new CompanyState(3);
            facility.Inventory.AddStacks(content.FindResource("iron-ore"), 3);
            Assert.IsFalse(facility.TickProduction(5d / 300, company));
            Assert.AreEqual(3, facility.Inventory.GetAmount(content.FindResource("iron-ore")));
            company.Earn(1);
            Assert.IsTrue(facility.TickProduction(5d / 300, company));
            Assert.AreEqual(0, company.Credits);
        }
        [Test] public void LargeTick_CompletesMultipleBatchesWithoutDiscardingTime()
        {
            var facility = Refinery(); var company = new CompanyState(100);
            facility.Inventory.AddStacks(content.FindResource("iron-ore"), 9);
            Assert.IsTrue(facility.TickProduction(12d / 300, company));
            Assert.AreEqual(4, facility.Inventory.GetAmount(content.FindResource("iron")));
            Assert.AreEqual(92, company.Credits);
            Assert.AreEqual(2d / 300, facility.ProgressDays, 1e-8);
        }
        [Test] public void StarterGame_HasFourFacilitiesAndMinesWithoutAutomaticSales()
        {
            using (var session = new FoundrySession())
            {
                Assert.IsFalse(session.Power.HasDeficit);
                session.Advance(180);
                Assert.AreEqual(4, session.Comet.Facilities.Count);
                Assert.AreEqual(0, session.Sales);
                Assert.Less(session.Comet.Deposits[0].RemainingStacks, 2400);
                Assert.Greater(session.TotalStock(session.Content.FindResource("iron-ore")), 0);
            }
        }
        [Test] public void Buildings_RespectResourceNodesFootprintsAndBuildCosts()
        {
            using (var session = new FoundrySession())
            {
                var mine = session.Content.FindBuilding("mine-small");
                Assert.IsFalse(session.Build(mine, new Vector2Int(4, 4), out _));
                double credits = session.Company.Credits;
                Assert.IsTrue(session.Build(mine, new Vector2Int(3, 9), out _));
                Assert.AreEqual(credits - mine.BuildCost, session.Company.Credits);
                Assert.AreEqual("extract-ice", session.FacilityAt(new Vector2Int(3, 9)).ActiveRecipe.RecipeId);
                Assert.IsFalse(session.Build(mine, new Vector2Int(3, 9), out _));
            }
        }

        [Test] public void CompleteIndustrialChain_CanReachFiveEquipmentDeliveries()
        {
            using (var session = new FoundrySession())
            {
                BuildRecipe(session, "refinery-basic", 5, 3, "smelt-iron");
                BuildRecipe(session, "factory-metal", 8, 3, "roll-plate");
                BuildRecipe(session, "factory-electronics", 9, 7, "draw-wire");
                BuildRecipe(session, "assembly-basic", 11, 4, "make-equipment");
                BuildRecipe(session, "market-terminal", 12, 7, null);
                session.AutoSell = true;
                BuildRecipe(session, "power-plant-large", 3, 0, null);
                BuildRecipe(session, "refinery-basic", 10, 0, "smelt-copper");
                BuildRecipe(session, "factory-metal", 0, 3, "cut-gear");
                BuildRecipe(session, "factory-metal", 0, 7, "make-frame");
                BuildRecipe(session, "factory-electronics", 6, 4, "make-circuit");
                BuildRecipe(session, "assembly-basic", 3, 4, "make-mechanical");
                BuildRecipe(session, "assembly-basic", 7, 0, "make-electronic");
                session.FacilityAt(new Vector2Int(11, 4)).AssignRecipe(session.Content.FindRecipe("make-equipment"));
                Assert.IsFalse(session.Power.HasDeficit);
                session.Advance(1200);
                string diagnostics = string.Join("\n", session.Comet.Facilities.Select(f =>
                    f.Definition.DisplayName + " / " + f.ActiveRecipe?.DisplayName + " / " + session.Status(f) + " / " +
                    string.Join(", ", f.Inventory.Amounts.Select(a => a.Key.DisplayName + "=" + a.Value))));
                Assert.GreaterOrEqual(session.EquipmentDelivered, 5, "The opening objective must be achievable through the full production chain.\n" + diagnostics);
            }
        }
        private static void BuildRecipe(FoundrySession session, string building, int x, int y, string recipe)
        {
            var anchor = new Vector2Int(x, y);
            Assert.IsTrue(session.Build(session.Content.FindBuilding(building), anchor, out var message), message);
            if (recipe != null) Assert.IsTrue(session.FacilityAt(anchor).AssignRecipe(session.Content.FindRecipe(recipe)));
        }
    }
}
