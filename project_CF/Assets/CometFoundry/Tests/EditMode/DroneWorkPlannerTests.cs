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
