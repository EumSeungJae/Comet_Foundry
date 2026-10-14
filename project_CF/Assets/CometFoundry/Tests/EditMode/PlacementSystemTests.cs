using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class PlacementSystemTests
    {
        [Test]
        public void TryPlaceFacility_OccupiesGridTile()
        {
            CometState comet = new CometState(new SimulationId("comet-test"), "Test Comet", Vector2.zero, 4f, 4, 4);
            FacilityDefinition definition = ScriptableObject.CreateInstance<FacilityDefinition>();
            PlacementSystem placement = new PlacementSystem();

            bool placed = placement.TryPlaceFacility(comet, new SimulationId("facility-test"), definition, new Vector2Int(1, 1), out FacilityInstance facility);

            Assert.IsTrue(placed);
            Assert.IsNotNull(facility);
            Assert.AreEqual(CometTileOccupant.Facility, comet.Grid.GetTile(new Vector2Int(1, 1)).Occupant);
        }

        [Test]
        public void TryPlaceRail_FailsOnOccupiedTile()
        {
            CometState comet = new CometState(new SimulationId("comet-test"), "Test Comet", Vector2.zero, 4f, 4, 4);
            FacilityDefinition definition = ScriptableObject.CreateInstance<FacilityDefinition>();
            PlacementSystem placement = new PlacementSystem();

            placement.TryPlaceFacility(comet, new SimulationId("facility-test"), definition, Vector2Int.zero, out _);
            bool placedRail = placement.TryPlaceRail(comet, new SimulationId("rail-test"), Vector2Int.zero, out _);

            Assert.IsFalse(placedRail);
        }

        [Test]
        public void TryPlaceRailPath_OccupiesEveryPathTile()
        {
            CometState comet = new CometState(new SimulationId("comet-test"), "Test Comet", Vector2.zero, 4f, 5, 5);
            PlacementSystem placement = new PlacementSystem();

            var path = placement.GetManhattanPath(new Vector2Int(0, 0), new Vector2Int(2, 1));
            bool placed = placement.TryPlaceRailPath(comet, "rail-test", path, 0, out int placedCount);

            Assert.IsTrue(placed);
            Assert.AreEqual(4, placedCount);
            Assert.AreEqual(1, comet.RailLines.Count);
            Assert.AreEqual(new Vector2Int(0, 0), comet.RailLines[0].Path[0]);
            Assert.AreEqual(new Vector2Int(2, 1), comet.RailLines[0].Path[3]);
            Assert.AreEqual(new Vector2Int(0, 0), comet.RailLines[0].Start);
            Assert.AreEqual(new Vector2Int(2, 1), comet.RailLines[0].End);
            Assert.AreEqual(3, comet.RailLines[0].SegmentCount);
            Assert.AreEqual(CometTileOccupant.Rail, comet.Grid.GetTile(new Vector2Int(0, 0)).Occupant);
            Assert.AreEqual(CometTileOccupant.Rail, comet.Grid.GetTile(new Vector2Int(1, 0)).Occupant);
            Assert.AreEqual(CometTileOccupant.Rail, comet.Grid.GetTile(new Vector2Int(2, 0)).Occupant);
            Assert.AreEqual(CometTileOccupant.Rail, comet.Grid.GetTile(new Vector2Int(2, 1)).Occupant);
        }

        [Test]
        public void TryPlaceConveyorPath_SpendsCreditsAndCreatesDronePathTile()
        {
            CompanyState company = new CompanyState(1000d);
            CometState comet = new CometState(new SimulationId("comet-test"), "Test Comet", Vector2.zero, 4f, 4, 4);
            PlacementSystem placement = new PlacementSystem();

            bool placed = placement.TryPlaceConveyorPath(company, comet, new SimulationId("path-test"), new Vector2Int(1, 1), out ConveyorPathSegment conveyorPath);

            Assert.IsTrue(placed);
            Assert.IsNotNull(conveyorPath);
            Assert.AreEqual(700d, company.Credits);
            Assert.AreEqual(CometTileOccupant.ConveyorPath, comet.Grid.GetTile(new Vector2Int(1, 1)).Occupant);
            Assert.IsTrue(comet.Grid.GetTile(new Vector2Int(1, 1)).IsDroneNavigable);
        }

        [Test]
        public void TryPlaceConveyorPath_FailsOnFacilityTile()
        {
            CompanyState company = new CompanyState(1000d);
            CometState comet = new CometState(new SimulationId("comet-test"), "Test Comet", Vector2.zero, 4f, 4, 4);
            FacilityDefinition definition = ScriptableObject.CreateInstance<FacilityDefinition>();
            PlacementSystem placement = new PlacementSystem();
            placement.TryPlaceFacility(comet, new SimulationId("facility-test"), definition, new Vector2Int(1, 1), out _);

            bool placed = placement.TryPlaceConveyorPath(company, comet, new SimulationId("path-test"), new Vector2Int(1, 1), out _);

            Assert.IsFalse(placed);
            Assert.AreEqual(1000d, company.Credits);
        }

        [Test]
        public void TryPlaceWarehouse_CreatesLargeStorageFacility()
        {
            CometState comet = new CometState(new SimulationId("comet-test"), "Test Comet", Vector2.zero, 4f, 4, 4);
            FacilityDefinition warehouse = FacilityDefinition.CreatePrototypeWarehouse();
            PlacementSystem placement = new PlacementSystem();

            bool placed = placement.TryPlaceFacility(comet, new SimulationId("warehouse-test"), warehouse, new Vector2Int(1, 1), out FacilityInstance facility);

            Assert.IsTrue(placed);
            Assert.IsNotNull(facility);
            Assert.IsTrue(facility.Definition.IsWarehouse);
            Assert.AreEqual(FacilityType.Warehouse, facility.Definition.FacilityType);
            Assert.AreEqual(new Vector2Int(2, 2), facility.Definition.FootprintSize);
            Assert.AreEqual(500d, facility.Inventory.CapacityTons);
            Assert.AreEqual(2500d, facility.Definition.BuildCost);
            Assert.AreEqual(25d, facility.Definition.OperatingCostPerDay);
        }
    }
}
