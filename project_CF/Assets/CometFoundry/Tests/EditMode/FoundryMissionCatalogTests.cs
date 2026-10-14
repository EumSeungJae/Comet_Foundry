using System;
using System.Linq;
using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class FoundryMissionCatalogTests
    {
        private static void BuildMarket(FoundrySession session) => Assert.IsTrue(session.Build(session.Content.FindBuilding("market-terminal"), new Vector2Int(12, 7), out _));
        private static void Supply(FoundrySession session, FoundryMission mission)
        {
            foreach (var demand in mission.Requirements)
            {
                double remaining = demand.Amount;
                foreach (var facility in session.Colonies.SelectMany(c => c.State.Facilities).Where(f => f.Definition.IsWarehouse ||
                    f.Definition.FacilityType == FacilityType.MarketTerminal || f.Definition.FacilityType == FacilityType.LogisticsHub))
                    remaining -= facility.Inventory.AddStacks(session.Content.FindResource(demand.ResourceId), remaining);
                Assert.AreEqual(0, remaining, "Test storage must fit " + demand.ResourceId);
            }
        }
        [Test]
        public void FifteenContractsHaveFivePerGradeAndRewardsMatchThePricingPolicy()
        {
            using (var session = new FoundrySession())
            {
                var general = session.Missions.Where(m => m.Number > 0).ToList();
                Assert.AreEqual(16, session.Missions.Count); Assert.AreEqual(1, session.Missions.Count(m => m.Number == 0));
                CollectionAssert.AreEqual(Enumerable.Range(1, 15).ToArray(), general.Select(m => m.Number).ToArray());
                foreach (int grade in new[] { 2, 3, 4 }) Assert.AreEqual(5, general.Count(m => m.ProcessingGrade == grade));
                foreach (var mission in general)
                {
                    Assert.That(mission.Requirements.Count, Is.InRange(2, 5));
                    Assert.AreEqual(mission.Requirements.Count, mission.Requirements.Select(r => r.ResourceId).Distinct().Count());
                    Assert.AreEqual(mission.ProcessingGrade, mission.Requirements.Max(r => session.Content.FindResource(r.ResourceId).ProcessingGrade));
                    foreach (var requirement in mission.Requirements) Assert.GreaterOrEqual(session.Content.FindResource(requirement.ResourceId).ProcessingGrade, 2);
                    double baseValue = mission.Requirements.Sum(r => session.Content.FindResource(r.ResourceId).BaseMarketPrice * r.Amount);
                    double multiplier = mission.Number == 10 ? 1.05 : mission.Number == 11 ? 1.30 : mission.ProcessingGrade == 2 ? 1.35 : mission.ProcessingGrade == 3 ? 1.55 : 1.80;
                    Assert.AreEqual(Math.Ceiling(baseValue * multiplier / 100) * 100, mission.Reward, mission.Name);
                }
                Assert.AreEqual(FoundryMissionReward.Comet, general.Single(m => m.Number == 10).BonusReward);
                Assert.AreEqual(FoundryMissionReward.CargoShip, general.Single(m => m.Number == 11).BonusReward);
                Assert.AreEqual("우주선 부품납품", general.Last().Name);
                Assert.AreEqual(280, general.Last().Requirements.Sum(r => r.Amount));
            }
        }
        [Test]
        public void MissingOneMaterialPreservesAllInventoryAndRewards()
        {
            using (var session = new FoundrySession())
            {
                BuildMarket(session); var mission = session.Missions.Single(m => m.Number == 1);
                Assert.IsTrue(session.AcceptMission(mission.Id, out _));
                var warehouse = session.Comet.Facilities.First(f => f.Definition.IsWarehouse);
                warehouse.Inventory.AddStacks(session.Content.FindResource("plate"), 15);
                warehouse.Inventory.AddStacks(session.Content.FindResource("wire"), 11);
                string before = session.ExportState();
                Assert.IsFalse(session.CanDeliverMission); Assert.IsFalse(session.DeliverMission(out _)); Assert.AreEqual(before, session.ExportState());
                warehouse.Inventory.AddStacks(session.Content.FindResource("wire"), 1);
                double cash = session.Company.Credits;
                Assert.IsTrue(session.CanDeliverMission); Assert.IsTrue(session.DeliverMission(out _));
                Assert.AreEqual(3, session.TotalStock(session.Content.FindResource("plate"))); Assert.AreEqual(0, session.TotalStock(session.Content.FindResource("wire")));
                Assert.AreEqual(cash + mission.Reward, session.Company.Credits); Assert.IsFalse(session.DeliverMission(out _));
            }
        }
        [Test]
        public void TenthContractGrantsAUsableUniqueCometOnceAndPreservesSurveyCandidates()
        {
            using (var session = new FoundrySession()) using (var restored = new FoundrySession())
            {
                BuildMarket(session); Assert.IsTrue(session.Explore(out _)); session.Advance(8.1);
                var candidates = session.Prospects.Select(p => p.State.DistanceCode).ToArray();
                var mission = session.Missions.Single(m => m.Number == 10); Supply(session, mission); Assert.IsTrue(session.AcceptMission(mission.Id, out _));
                double cash = session.Company.Credits; Assert.IsTrue(session.DeliverMission(out _));
                Assert.AreEqual(2, session.Colonies.Count); Assert.AreEqual(2, session.Company.Comets.Count); Assert.AreEqual(0, session.ActiveColonyIndex);
                var gift = session.Colonies.Last(); Assert.IsNotNull(session.Dock(gift)); Assert.AreEqual(3, gift.State.Deposits.Count);
                Assert.AreEqual(3, gift.State.ResourceTypeCount); StringAssert.StartsWith("CC3012", gift.State.SeedCode);
                Assert.AreEqual(CometGenerator.GetDistanceMinutes(gift.State.DistanceCode[0], gift.State.DistanceCode[1] - '0'), gift.State.DistanceMinutes);
                Assert.AreEqual(100, gift.State.Grid.Width * gift.State.Grid.Height); CollectionAssert.AreEqual(candidates, session.Prospects.Select(p => p.State.DistanceCode).ToArray());
                Assert.IsFalse(candidates.Contains(gift.State.DistanceCode)); Assert.AreEqual(cash + mission.Reward, session.Company.Credits);
                Assert.IsFalse(session.AcceptMission(mission.Id, out _)); Assert.IsFalse(session.DeliverMission(out _));
                Assert.IsTrue(restored.ImportState(session.ExportState())); Assert.AreEqual(2, restored.Colonies.Count); Assert.IsTrue(restored.MissionCompleted(mission.Id));
                Assert.IsNotNull(restored.Dock(restored.Colonies.Last())); Assert.IsFalse(restored.AcceptMission(mission.Id, out _));
            }
        }
        [Test]
        public void EleventhContractGrantsOneHundredStackShipOnceAndPersistsIt()
        {
            using (var session = new FoundrySession()) using (var restored = new FoundrySession())
            {
                BuildMarket(session); var mission = session.Missions.Single(m => m.Number == 11); Supply(session, mission); Assert.IsTrue(session.AcceptMission(mission.Id, out _));
                double cash = session.Company.Credits; Assert.IsTrue(session.DeliverMission(out _));
                Assert.AreEqual(2, session.Company.Ships.Count); Assert.AreEqual(100, session.Company.Ships.Last().Cargo.CapacityStacks);
                Assert.AreEqual(cash + mission.Reward, session.Company.Credits); Assert.IsFalse(session.AcceptMission(mission.Id, out _));
                Assert.IsTrue(restored.ImportState(session.ExportState())); Assert.AreEqual(2, restored.Company.Ships.Count); Assert.IsFalse(restored.AcceptMission(mission.Id, out _));
            }
        }
        [Test]
        public void FinalContractConsumesAllFiveMaterialsAcrossStorageAndPreservesUnrelatedStock()
        {
            using (var session = new FoundrySession())
            {
                BuildMarket(session); var mission = session.Missions.Single(m => m.Number == 15); Supply(session, mission);
                session.Comet.Facilities.First(f => f.Definition.FacilityType == FacilityType.MarketTerminal).Inventory.AddStacks(session.Content.FindResource("iron"), 5);
                Assert.IsTrue(session.AcceptMission(mission.Id, out _)); double cash = session.Company.Credits;
                Assert.IsTrue(session.DeliverMission(out _));
                foreach (var demand in mission.Requirements) Assert.AreEqual(0, session.TotalStock(session.Content.FindResource(demand.ResourceId)));
                Assert.AreEqual(5, session.TotalStock(session.Content.FindResource("iron"))); Assert.AreEqual(cash + 197300, session.Company.Credits);
            }
        }
        [Test]
        public void OldActiveContractKeepsItsOriginalSingleMaterialAndRewardAcrossMigration()
        {
            using (var session = new FoundrySession()) using (var restored = new FoundrySession())
            {
                BuildMarket(session); var oldSave = JsonUtility.FromJson<FoundryWorldSave>(session.ExportState());
                oldSave.version = 3; oldSave.activeMission = "wire-order"; oldSave.completedMissions.Add("gear-order");
                Assert.IsTrue(session.ImportState(JsonUtility.ToJson(oldSave))); Assert.AreEqual(1, session.ActiveMission.Requirements.Count);
                Assert.AreEqual(18, session.ActiveMission.Amount); Assert.AreEqual(3500, session.ActiveMission.Reward);
                Assert.IsTrue(restored.ImportState(session.ExportState())); Assert.AreEqual("wire-order", restored.ActiveMission.Id);
                Supply(restored, restored.ActiveMission); double cash = restored.Company.Credits; Assert.IsTrue(restored.DeliverMission(out _));
                Assert.AreEqual(cash + 3500, restored.Company.Credits); Assert.IsTrue(restored.MissionCompleted("gear-order"));
                Assert.IsFalse(restored.AcceptMission("wire-order", out _)); Assert.IsTrue(restored.AcceptMission("tier2-01", out _));
                Assert.AreEqual(2, restored.ActiveMission.Requirements.Count);
            }
        }
        [Test]
        public void ShipRewardCapacityFailureDoesNotConsumeAnyMissionMaterial()
        {
            using (var session = new FoundrySession())
            {
                BuildMarket(session); var mission = session.Missions.Single(m => m.Number == 11); Supply(session, mission); Assert.IsTrue(session.AcceptMission(mission.Id, out _));
                for (int i = session.Company.Ships.Count; i < 128; i++) session.Company.AddShip(new SimulationId("ship-" + i), session.Content.Courier);
                string before = session.ExportState(); Assert.IsFalse(session.CanDeliverMission); Assert.IsFalse(session.DeliverMission(out _)); Assert.AreEqual(before, session.ExportState());
            }
        }
    }
}
