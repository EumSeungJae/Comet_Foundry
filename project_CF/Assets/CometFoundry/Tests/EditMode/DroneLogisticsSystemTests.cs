using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class DroneLogisticsSystemTests
    {
        [Test]
        public void CreateDedicatedLink_AssignsDronesFromHangarToBuildingPair()
        {
            CometState comet = new CometState(new SimulationId("comet"), "Comet", Vector2.zero, 4f, 8, 8);
            PlacementSystem placement = new PlacementSystem();
            FacilityInstance hangar = Place(comet, placement, FacilityDefinition.CreatePrototypeDroneHangar(), "hangar", 0, 0);
            FacilityInstance source = Place(comet, placement, CreateFacility("source", FacilityType.Mine, 1, 1), "source", 3, 0);
            FacilityInstance destination = Place(comet, placement, FacilityDefinition.CreatePrototypeWarehouse(), "destination", 5, 2);
            ResourceDefinition ore = CreateResource();
            DroneLogisticsSystem drones = new DroneLogisticsSystem();

            bool created = drones.TryCreateDedicatedLink(comet, new SimulationId("link"), hangar, source, destination, ore, 4, out DroneLogisticsLink link);

            Assert.IsTrue(created);
            Assert.IsNotNull(link);
            Assert.AreEqual(1, comet.DroneLinks.Count);
            Assert.AreEqual(4, drones.GetAssignedDroneCount(comet, hangar));
            Assert.AreEqual(source, link.Source);
            Assert.AreEqual(destination, link.Destination);
            Assert.AreEqual(1d, link.SpeedMultiplier);
        }

        [Test]
        public void CreateDedicatedLink_RejectsOverCapacityAssignments()
        {
            CometState comet = new CometState(new SimulationId("comet"), "Comet", Vector2.zero, 4f, 10, 10);
            PlacementSystem placement = new PlacementSystem();
            FacilityInstance hangar = Place(comet, placement, FacilityDefinition.CreatePrototypeDroneHangar(), "hangar", 0, 0);
            FacilityInstance source = Place(comet, placement, CreateFacility("source", FacilityType.Mine, 1, 1), "source", 3, 0);
            FacilityInstance destination = Place(comet, placement, FacilityDefinition.CreatePrototypeWarehouse(), "destination", 6, 3);
            ResourceDefinition ore = CreateResource();
            DroneLogisticsSystem drones = new DroneLogisticsSystem();

            bool created = drones.TryCreateDedicatedLink(comet, new SimulationId("link"), hangar, source, destination, ore, 9, out _);

            Assert.IsFalse(created);
            Assert.AreEqual(0, comet.DroneLinks.Count);
        }

        [Test]
        public void HasNavigablePath_FailsWhenBuildingsBlockEveryRoute()
        {
            CometState comet = new CometState(new SimulationId("comet"), "Comet", Vector2.zero, 4f, 5, 3);
            PlacementSystem placement = new PlacementSystem();
            FacilityInstance source = Place(comet, placement, CreateFacility("source", FacilityType.Mine, 1, 1), "source", 0, 1);
            FacilityInstance destination = Place(comet, placement, CreateFacility("destination", FacilityType.Factory, 1, 1), "destination", 4, 1);
            Place(comet, placement, CreateFacility("wall-a", FacilityType.Warehouse, 1, 3), "wall-a", 2, 0);
            DroneLogisticsSystem drones = new DroneLogisticsSystem();

            Assert.IsFalse(drones.HasNavigablePath(comet, source, destination));
        }

        [Test]
        public void CreateDedicatedLink_UsesConveyorPathSpeedMultiplierOnFullyPavedRoute()
        {
            CompanyState company = new CompanyState(3000d);
            CometState comet = new CometState(new SimulationId("comet"), "Comet", Vector2.zero, 4f, 7, 3);
            PlacementSystem placement = new PlacementSystem();
            FacilityInstance hangar = Place(comet, placement, FacilityDefinition.CreatePrototypeDroneHangar(), "hangar", 0, 0);
            FacilityInstance source = Place(comet, placement, CreateFacility("source", FacilityType.Mine, 1, 1), "source", 2, 1);
            FacilityInstance destination = Place(comet, placement, CreateFacility("destination", FacilityType.Factory, 1, 1), "destination", 6, 1);
            ResourceDefinition ore = CreateResource();

            placement.TryPlaceConveyorPath(company, comet, new SimulationId("path-0"), new Vector2Int(3, 1), out _);
            placement.TryPlaceConveyorPath(company, comet, new SimulationId("path-1"), new Vector2Int(4, 1), out _);
            placement.TryPlaceConveyorPath(company, comet, new SimulationId("path-2"), new Vector2Int(5, 1), out _);
            DroneLogisticsSystem drones = new DroneLogisticsSystem();

            bool created = drones.TryCreateDedicatedLink(comet, new SimulationId("link"), hangar, source, destination, ore, 4, out DroneLogisticsLink link);

            Assert.IsTrue(created);
            Assert.AreEqual(DroneLogisticsSystem.ConveyorPathSpeedMultiplier, link.SpeedMultiplier);
        }

        private static FacilityInstance Place(CometState comet, PlacementSystem placement, FacilityDefinition definition, string id, int x, int y)
        {
            placement.TryPlaceFacility(comet, new SimulationId(id), definition, new Vector2Int(x, y), out FacilityInstance facility);
            return facility;
        }

        private static FacilityDefinition CreateFacility(string id, FacilityType type, int width, int height)
        {
            FacilityDefinition definition = ScriptableObject.CreateInstance<FacilityDefinition>();
            definition.ConfigurePrototype(id, id, type, new Vector2Int(width, height), 10d);
            return definition;
        }

        private static ResourceDefinition CreateResource()
        {
            ResourceDefinition resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            resource.ConfigurePrototype("ore", "Ore", ResourceCategory.Raw, 100d, true);
            return resource;
        }
    }
}
