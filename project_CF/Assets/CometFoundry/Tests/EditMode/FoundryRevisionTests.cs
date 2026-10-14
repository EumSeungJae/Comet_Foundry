using System;
using System.Linq;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class FoundryRevisionTests
    {
        private static void BuildMarket(FoundrySession s) => Assert.IsTrue(s.Build(s.Content.FindBuilding("market-terminal"), new Vector2Int(12, 7), out _));
        [Test] public void NoUncreatedEventCanExceedThirtyPercent()
        {
            using (var s = new FoundrySession()) foreach (var r in s.Content.Resources)
                for (int month = -24; month <= 24; month++)
                {
                    double day = month * 30, ratio = s.PriceRatio(r, day);
                    Assert.IsNull(s.MarketEventAt(r, day)); Assert.That(ratio, Is.InRange(.70 - 1e-10, 1.30 + 1e-10));
                    Assert.AreEqual(ratio, s.PriceRatio(r, day + 29.999), 1e-10);
                }
        }
        [Test] public void DemolitionPreservesStockClearsFootprintAndAllowsRebuild()
        {
            using (var s = new FoundrySession())
            {
                var definition = s.Content.FindBuilding("refinery-basic"); var anchor = new Vector2Int(5, 3);
                Assert.IsTrue(s.Build(definition, anchor, out _)); var f = s.FacilityAt(anchor);
                var r = s.Content.FindResource("iron"); f.Inventory.AddStacks(r, 7);
                double cash = s.Company.Credits, stock = s.TotalStock(r);
                Assert.IsTrue(s.Demolish(f, out _)); Assert.AreEqual(stock, s.TotalStock(r), 1e-8); Assert.AreEqual(cash, s.Company.Credits);
                Assert.IsNull(s.FacilityAt(anchor)); Assert.IsTrue(s.Comet.Grid.CanOccupy(anchor, definition.FootprintSize));
                Assert.IsFalse(s.Demolish(f, out _)); Assert.IsTrue(s.Build(definition, anchor, out _));
                using (var loaded = new FoundrySession()) { Assert.IsTrue(loaded.ImportState(s.ExportState())); Assert.AreEqual(stock, loaded.TotalStock(loaded.Content.FindResource("iron")), 1e-8); }
            }
        }
        [Test] public void DemolitionRejectsInsufficientStorageWithoutLosingStock()
        {
            using (var s = new FoundrySession())
            {
                var warehouse = s.Comet.Facilities.First(f => f.Definition.IsWarehouse);
                var r = s.Content.FindResource("iron"); warehouse.Inventory.AddStacks(r, 10);
                Assert.IsFalse(s.Demolish(warehouse, out _)); Assert.IsTrue(s.Comet.Facilities.Contains(warehouse)); Assert.AreEqual(10, s.TotalStock(r));
            }
        }
        [Test] public void MissionsRequireMarketAndConsumeOnlyDemandOnce()
        {
            using (var s = new FoundrySession())
            {
                var m = s.Missions[0]; Assert.IsFalse(s.AcceptMission(m.Id, out _)); BuildMarket(s);
                Assert.IsTrue(s.AcceptMission(m.Id, out _)); Assert.IsFalse(s.AcceptMission(s.Missions[1].Id, out _));
                Assert.IsFalse(s.DeliverMission(out _));
                var r = s.Content.FindResource(m.ResourceId); s.Comet.Facilities.First(f => f.Definition.IsWarehouse).Inventory.AddStacks(r, m.Amount + 3);
                double cash = s.Company.Credits; Assert.IsTrue(s.DeliverMission(out _)); Assert.AreEqual(3, s.TotalStock(r));
                Assert.AreEqual(cash + m.Reward, s.Company.Credits); Assert.IsTrue(s.MissionCompleted(m.Id));
                Assert.IsFalse(s.DeliverMission(out _)); Assert.IsFalse(s.AcceptMission(m.Id, out _));
                using (var loaded = new FoundrySession()) { Assert.IsTrue(loaded.ImportState(s.ExportState())); Assert.IsTrue(loaded.MissionCompleted(m.Id)); Assert.IsFalse(loaded.AcceptMission(m.Id, out _)); }
            }
        }
        [Test] public void ActiveMissionSurvivesSaveAndAbandonPreservesStock()
        {
            using (var s = new FoundrySession()) using (var loaded = new FoundrySession())
            {
                BuildMarket(s); var m = s.Missions[1]; Assert.IsTrue(s.AcceptMission(m.Id, out _));
                var r = s.Content.FindResource(m.ResourceId); s.Comet.Facilities.First(f => f.Definition.IsWarehouse).Inventory.AddStacks(r, 4);
                Assert.IsTrue(loaded.ImportState(s.ExportState())); Assert.AreEqual(m.Id, loaded.ActiveMission.Id); Assert.AreEqual(4, loaded.MissionStock);
                loaded.AbandonMission(); Assert.IsNull(loaded.ActiveMission); Assert.AreEqual(4, loaded.TotalStock(loaded.Content.FindResource(m.ResourceId)));
                Assert.IsTrue(loaded.AcceptMission(m.Id, out _));
                var invalid = JsonUtility.FromJson<FoundryWorldSave>(loaded.ExportState()); invalid.activeMission = "unknown";
                Assert.IsFalse(loaded.ImportState(JsonUtility.ToJson(invalid))); Assert.AreEqual(m.Id, loaded.ActiveMission.Id);
                invalid.activeMission = null; invalid.completedMissions = null;
                Assert.IsTrue(loaded.ImportState(JsonUtility.ToJson(invalid)), "Older saves without mission fields remain supported.");
            }
        }
        [Test] public void ShippingDockCannotBeDemolishedUntilVoyageEnds()
        {
            using (var s = new FoundrySession())
            {
                BuildMarket(s); Assert.IsTrue(s.Explore(out _)); s.Advance(8.1); Assert.IsTrue(s.Colonize(0, out _));
                var r = s.Content.FindResource("water"); Assert.IsTrue(s.Trade(r, true, 10, out _)); Assert.IsTrue(s.Dispatch(0, 0, 1, r, 10, out _));
                var source = s.Dock(s.Colonies[0]); Assert.IsFalse(s.Demolish(source, out _));
                s.SwitchColony(1); Assert.IsFalse(s.Demolish(s.Dock(s.ActiveColony), out _));
                s.Advance(s.Voyages[0].Duration + 1); Assert.AreEqual(0, s.Voyages.Count);
            }
        }        [Test] public void SixteenMissionsStartAtSecondProcessingGradeAndOldRawMissionIsRetired()
        {
            using (var s = new FoundrySession())
            {
                Assert.AreEqual(16, s.Missions.Count);
                Assert.AreEqual(16, s.Missions.Select(m => m.Id).Distinct().Count());
                foreach (var m in s.Missions) foreach (var requirement in m.Requirements) Assert.GreaterOrEqual(s.Content.FindResource(requirement.ResourceId).ProcessingGrade, 2);
                BuildMarket(s); Assert.IsFalse(s.AcceptMission("first-iron", out _));
                var saved = JsonUtility.FromJson<FoundryWorldSave>(s.ExportState()); saved.activeMission = "first-iron"; saved.completedMissions.Add("first-iron");
                Assert.IsTrue(s.ImportState(JsonUtility.ToJson(saved))); Assert.IsNull(s.ActiveMission);
                Assert.IsTrue(s.AcceptMission(FoundrySession.PracticeMissionId, out _));
            }
        }
        [Test] public void PracticeRequiresRealPlateProductionAndSurvivesSave()
        {
            using (var s = new FoundrySession()) using (var loaded = new FoundrySession())
            {
                Assert.IsTrue(s.StartMissionPractice(out _)); Assert.AreEqual(FoundryMissionPracticeStep.BuildMarket, s.MissionPracticeStep);
                BuildMarket(s); s.Advance(.1); Assert.AreEqual(FoundryMissionPracticeStep.AcceptMission, s.MissionPracticeStep);
                Assert.IsFalse(s.AcceptMission(s.Missions.First(m => m.Number == 2).Id, out _), "Practice requires the designated plate mission.");
                Assert.IsTrue(s.AcceptMission(FoundrySession.PracticeMissionId, out _));
                Assert.IsTrue(s.Build(s.Content.FindBuilding("factory-metal"), new Vector2Int(8, 3), out _));
                var factory = s.FacilityAt(new Vector2Int(8, 3)); factory.AssignRecipe(s.Content.FindRecipe("roll-plate"));
                Assert.IsTrue(s.Trade(s.Content.FindResource("plate"), true, 4, out _));
                double cash = s.Company.Credits; Assert.IsFalse(s.DeliverMission(out _)); Assert.AreEqual(cash, s.Company.Credits);
                factory.Inventory.AddStacks(s.Content.FindResource("iron"), 4);
                s.Advance(6); Assert.AreEqual(2, s.PracticePlateMade);
                Assert.IsTrue(loaded.ImportState(s.ExportState())); Assert.AreEqual(2, loaded.PracticePlateMade);
                loaded.Advance(6.1); Assert.AreEqual(4, loaded.PracticePlateMade); Assert.AreEqual(FoundryMissionPracticeStep.Deliver, loaded.MissionPracticeStep);
                Assert.IsTrue(loaded.DeliverMission(out _)); Assert.AreEqual(FoundryMissionPracticeStep.Complete, loaded.MissionPracticeStep);
            }
        }
        [Test] public void SmallDroneStorageRestoresLogisticsAndPreservesFootprintOnLoad()
        {
            using (var s = new FoundrySession()) using (var loaded = new FoundrySession())
            {
                Assert.IsTrue(s.Demolish(s.Comet.Facilities.First(f => f.Definition.IsWarehouse), out _)); Assert.IsFalse(s.HasLogistics);
                var storage = s.Content.FindBuilding("drone-storage"); Assert.AreEqual(new Vector2Int(1, 2), storage.FootprintSize); Assert.AreEqual(8, storage.DroneCapacity);
                Assert.IsTrue(s.Build(storage, new Vector2Int(5, 3), out _)); Assert.IsTrue(s.HasHangar); Assert.IsTrue(s.HasLogistics);
                Assert.IsTrue(s.Build(s.Content.FindBuilding("refinery-basic"), new Vector2Int(5, 6), out _));
                s.FacilityAt(new Vector2Int(5, 6)).AssignRecipe(s.Content.FindRecipe("smelt-iron")); s.Advance(60);
                Assert.Greater(s.TutorialIronMade, 0, "Drones supply the refinery after storage construction.");
                Assert.IsTrue(loaded.ImportState(s.ExportState())); Assert.IsTrue(loaded.HasHangar);
                Assert.AreEqual(new Vector2Int(1, 2), loaded.FacilityAt(new Vector2Int(5, 3)).Definition.FootprintSize);
            }
        }
        [Test] public void NewColonyExplainsDronePowerRequirement()
        {
            using (var s = new FoundrySession())
            {
                BuildMarket(s); s.Explore(out _); s.Advance(8.1); Assert.IsTrue(s.Colonize(0, out _)); s.SwitchColony(1);
                Assert.IsTrue(s.Power.HasDeficit); StringAssert.Contains("전력 부족", s.DroneStatus);
                foreach (string id in new[] { "drone-storage", "power-plant-small", "power-plant-small" })
                {
                    var definition = s.Content.FindBuilding(id);
                    var positions = Enumerable.Range(0, s.Comet.Grid.Width).SelectMany(x => Enumerable.Range(0, s.Comet.Grid.Height).Select(y => new Vector2Int(x, y)));
                    Assert.IsTrue(positions.Any(pos => s.PlacementError(definition, pos) == null), "No placement for " + id);
                    var anchor = positions.First(pos => s.PlacementError(definition, pos) == null);
                    Assert.IsTrue(s.Build(definition, anchor, out _));
                }
                Assert.IsTrue(s.HasHangar); Assert.IsFalse(s.Power.HasDeficit); StringAssert.DoesNotContain("전력 부족", s.DroneStatus);
            }
        }

    }
}
