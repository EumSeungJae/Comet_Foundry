using System.Linq;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class FoundryParallelDroneTests
    {
        private static FoundrySession Fixture(bool extra)
        {
            var s = new FoundrySession();
            foreach (var f in s.Comet.Facilities) f.ClearRecipe();
            foreach (var p in new[] { new Vector2Int(4,3), new Vector2Int(6,3), new Vector2Int(8,3), new Vector2Int(10,3), new Vector2Int(3,6), new Vector2Int(10,6) })
            {
                Assert.IsTrue(s.Build(s.Content.FindBuilding("refinery-basic"), p, out var error), error);
                s.FacilityAt(p).AssignRecipe(s.Content.FindRecipe("smelt-iron"));
            }
            if (extra) Assert.IsTrue(s.Build(s.Content.FindBuilding("drone-hangar"), new Vector2Int(0,4), out var error), error);
            s.Comet.Facilities.First(f => f.Definition.IsWarehouse).Inventory.AddStacks(s.Content.FindResource("iron-ore"), 120);
            Assert.IsFalse(s.Power.HasDeficit);
            return s;
        }

        [TestCase(false,4)]
        [TestCase(true,12)]
        public void EveryProviderDispatchesItsConfiguredWorkers(bool extra, int count)
        {
            using (var s = Fixture(extra))
            {
                s.Advance(.1);
                Assert.AreEqual(count, s.DroneWorkerCapacity);
                Assert.AreEqual(count, s.ActiveDroneCount);
                Assert.AreEqual(count, s.DroneFlights.Select(f => (f.Provider, f.Slot)).Distinct().Count());
                foreach (var provider in s.Comet.Facilities.Where(f => f.Definition.IsDroneHangar))
                    Assert.AreEqual(provider.Definition.DroneCapacity, s.ProviderActiveDrones(provider));
                Assert.Greater(s.DroneFlights.Select(f => f.Request.Destination).Distinct().Count(), 1);
                for (int i = 0; i < 200; i++)
                {
                    s.Advance(.1);
                    foreach (var f in s.Comet.Facilities.Where(f => f.Definition.SupportedRecipes.Count > 0)) Assert.LessOrEqual(s.DroneQueueCount(f), 2);
                    Assert.LessOrEqual(s.ActiveDroneCount, count);
                }
                Assert.Greater(s.CompletedDroneDeliveries, count);
            }
        }

        [Test]
        public void HangarIncreasesDeliveredMaterialUnderLoad()
        {
            using (var one = Fixture(false)) using (var two = Fixture(true))
            {
                one.Advance(3); two.Advance(3);
                Assert.Greater(two.DeliveredDroneStacks, one.DeliveredDroneStacks);
            }
        }

        [Test]
        public void WarehouseDoesNotProvideWorkersAndDemolitionCancelsJobs()
        {
            using (var s = Fixture(false))
            {
                s.Advance(.1);
                var provider = s.Comet.Facilities.First(f => f.Definition.IsDroneHangar);
                Assert.IsTrue(s.Demolish(provider, out var error), error);
                Assert.AreEqual(0, s.ActiveDroneCount);
                s.Advance(20);
                Assert.IsFalse(s.HasLogistics);
                Assert.AreEqual(0, s.CompletedDroneDeliveries);
            }
        }

        [Test]
        public void PowerDeficitPausesAllConcurrentFlights()
        {
            using (var s = Fixture(true))
            {
                s.Advance(.1);
                var flights = s.DroneFlights.ToArray();
                Assert.IsTrue(s.Demolish(s.Comet.Facilities.First(f => f.Definition.IsPowerPlant), out var error), error);
                Assert.IsTrue(s.Power.HasDeficit);
                var elapsed = flights.Select(f => f.Elapsed).ToArray();
                s.Advance(5);
                CollectionAssert.AreEqual(elapsed, flights.Select(f => f.Elapsed).ToArray());
                Assert.AreEqual(0, s.CompletedDroneDeliveries);
            }
        }

        [Test]
        public void ReservationsAndSaveReloadPreserveMaterial()
        {
            using (var s = Fixture(true))
            {
                var ore = s.Content.FindResource("iron-ore");
                s.Advance(.1);
                Assert.LessOrEqual(s.DroneFlights.Sum(f => f.Request.RequestedStacks), s.TotalStock(ore));
                foreach (var group in s.DroneFlights.GroupBy(f => f.Request.Destination))
                    Assert.LessOrEqual(group.Sum(f => f.Request.RequestedStacks), group.Key.Inventory.FreeStacks);
                Assert.IsTrue(s.ImportState(s.ExportState()));
                Assert.AreEqual(12, s.DroneWorkerCapacity, "Loaded small and medium providers must retain their distinct capacities.");
                Assert.AreEqual(120, s.TotalStock(s.Content.FindResource("iron-ore")));
                Assert.AreEqual(0, s.ActiveDroneCount);
                foreach (var f in s.Comet.Facilities) f.ClearRecipe();
                s.Advance(20);
                Assert.AreEqual(120, s.TotalStock(s.Content.FindResource("iron-ore")));
            }
        }
    }
}
