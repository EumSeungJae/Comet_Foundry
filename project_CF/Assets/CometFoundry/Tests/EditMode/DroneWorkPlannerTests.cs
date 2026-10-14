using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class DroneWorkPlannerTests
    {
        [Test]
        public void SelectNextRequest_PrioritizesProductionBeforeMarketAndStorage()
        {
            ResourceDefinition ore = CreateResource();
            FacilityInstance source = CreateFacility("source", 30d);
            FacilityInstance production = CreateFacility("production", 10d);
            FacilityInstance market = CreateFacility("market", 10d);
            FacilityInstance storage = CreateFacility("storage", 10d);
            source.Inventory.AddStacks(ore, 9d);
            DroneWorkPlanner planner = new DroneWorkPlanner();

            DroneHaulRequest selected = planner.SelectNextRequest(new[]
            {
                new DroneHaulRequest(DroneHaulRequestType.Storage, source, storage, ore, 3d, 9),
                new DroneHaulRequest(DroneHaulRequestType.MarketReservation, source, market, ore, 3d, 0),
                new DroneHaulRequest(DroneHaulRequestType.ProductionInput, source, production, ore, 3d, 0)
            });

            Assert.AreEqual(DroneHaulRequestType.ProductionInput, selected.RequestType);
        }

        [Test]
        public void SelectNextRequest_SkipsBlockedHigherPriorityRequest()
        {
            ResourceDefinition ore = CreateResource();
            FacilityInstance source = CreateFacility("source", 30d);
            FacilityInstance fullProduction = CreateFacility("production", 3d);
            FacilityInstance market = CreateFacility("market", 10d);
            source.Inventory.AddStacks(ore, 6d);
            fullProduction.Inventory.AddStacks(ore, 3d);
            DroneWorkPlanner planner = new DroneWorkPlanner();

            DroneHaulRequest selected = planner.SelectNextRequest(new[]
            {
                new DroneHaulRequest(DroneHaulRequestType.ProductionInput, source, fullProduction, ore, 3d),
                new DroneHaulRequest(DroneHaulRequestType.MarketReservation, source, market, ore, 3d)
            });

            Assert.AreEqual(DroneHaulRequestType.MarketReservation, selected.RequestType);
        }

        [Test]
        public void EqualPriority_UsesOldestRouteWithoutPromotingStorageAboveProduction()
        {
            var ore = CreateResource();
            var source = CreateFacility("source", 30);
            var first = CreateFacility("first", 10);
            var second = CreateFacility("second", 10);
            try
            {
                source.Inventory.AddStacks(ore, 6);
                var recent = new DroneHaulRequest(DroneHaulRequestType.ProductionInput, source, first, ore, 2, 9, 16);
                var waiting = new DroneHaulRequest(DroneHaulRequestType.ProductionInput, source, second, ore, 2, 9, 160);
                var storage = new DroneHaulRequest(DroneHaulRequestType.Storage, source, first, ore, 2, 9, 100000);
                Assert.AreSame(waiting, new DroneWorkPlanner().SelectNextRequest(new[] { recent, storage, waiting }));
            }
            finally
            {
                Object.DestroyImmediate(ore);
                Object.DestroyImmediate(source.Definition); Object.DestroyImmediate(first.Definition); Object.DestroyImmediate(second.Definition);
            }
        }

        [Test]
        public void UrgentInput_StillOutranksAnOlderBufferTopUp()
        {
            var ore = CreateResource();
            var source = CreateFacility("source", 30);
            var destination = CreateFacility("destination", 10);
            try
            {
                source.Inventory.AddStacks(ore, 6);
                var topUp = new DroneHaulRequest(DroneHaulRequestType.ProductionInput, source, destination, ore, 2, 4, 100000);
                var urgent = new DroneHaulRequest(DroneHaulRequestType.ProductionInput, source, destination, ore, 2, 5, 0);
                Assert.AreSame(urgent, new DroneWorkPlanner().SelectNextRequest(new[] { topUp, urgent }));
            }
            finally { Object.DestroyImmediate(ore); Object.DestroyImmediate(source.Definition); Object.DestroyImmediate(destination.Definition); }
        }

        private static ResourceDefinition CreateResource()
        {
            ResourceDefinition resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            resource.ConfigurePrototype("ore", "Ore", ResourceCategory.Raw, 100d, true);
            return resource;
        }

        private static FacilityInstance CreateFacility(string id, double stackCapacity)
        {
            FacilityDefinition definition = ScriptableObject.CreateInstance<FacilityDefinition>();
            definition.ConfigurePrototype(id, id, FacilityType.Warehouse, Vector2Int.one, stackCapacity);
            return new FacilityInstance(new SimulationId(id), definition);
        }
    }
}
