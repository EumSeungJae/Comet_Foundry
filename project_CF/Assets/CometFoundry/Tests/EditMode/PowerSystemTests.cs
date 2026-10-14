using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class PowerSystemTests
    {
        [Test]
        public void PrototypePowerPlants_UseApprovedOutputs()
        {
            FacilityDefinition small = FacilityDefinition.CreatePrototypeSmallPowerPlant();
            FacilityDefinition medium = FacilityDefinition.CreatePrototypeMediumPowerPlant();
            FacilityDefinition large = FacilityDefinition.CreatePrototypeLargePowerPlant();

            Assert.AreEqual(new Vector2Int(1, 1), small.FootprintSize);
            Assert.AreEqual(4d, small.PowerOutput);
            Assert.AreEqual(0d, small.BasePowerConsumption);

            Assert.AreEqual(new Vector2Int(1, 3), medium.FootprintSize);
            Assert.AreEqual(16d, medium.PowerOutput);
            Assert.AreEqual(0d, medium.BasePowerConsumption);

            Assert.AreEqual(new Vector2Int(2, 3), large.FootprintSize);
            Assert.AreEqual(32d, large.PowerOutput);
            Assert.AreEqual(0d, large.BasePowerConsumption);
        }

        [Test]
        public void BuildingsConsumeOnePowerPerFootprintTile()
        {
            FacilityDefinition factory = ScriptableObject.CreateInstance<FacilityDefinition>();
            factory.ConfigurePrototype("factory", "Factory", FacilityType.Factory, new Vector2Int(2, 4), 0d);

            Assert.AreEqual(8d, factory.BasePowerConsumption);
        }

        [Test]
        public void DroneHangar_UsesUnifiedTwoByTwoCapacityAndNoBasePower()
        {
            FacilityDefinition hangar = FacilityDefinition.CreatePrototypeDroneHangar();

            Assert.AreEqual(FacilityType.DroneHangar, hangar.FacilityType);
            Assert.AreEqual(new Vector2Int(2, 2), hangar.FootprintSize);
            Assert.AreEqual(0d, hangar.BasePowerConsumption);
            Assert.AreEqual(32, hangar.DroneCapacity);
            Assert.AreEqual(8, hangar.DroneLinkCapacity);
        }

        [Test]
        public void PowerReport_IncludesBuildingAndDronePower()
        {
            CometState comet = new CometState(new SimulationId("comet"), "Comet", Vector2.zero, 4f, 8, 8);
            PlacementSystem placement = new PlacementSystem();
            FacilityDefinition powerPlant = FacilityDefinition.CreatePrototypeMediumPowerPlant();
            FacilityDefinition hangarDefinition = FacilityDefinition.CreatePrototypeDroneHangar();
            FacilityDefinition sourceDefinition = ScriptableObject.CreateInstance<FacilityDefinition>();
            sourceDefinition.ConfigurePrototype("source", "Source", FacilityType.Mine, new Vector2Int(1, 1), 10d);
            FacilityDefinition destinationDefinition = FacilityDefinition.CreatePrototypeWarehouse();

            placement.TryPlaceFacility(comet, new SimulationId("power"), powerPlant, new Vector2Int(0, 0), out _);
            placement.TryPlaceFacility(comet, new SimulationId("hangar"), hangarDefinition, new Vector2Int(2, 0), out FacilityInstance hangar);
            placement.TryPlaceFacility(comet, new SimulationId("source"), sourceDefinition, new Vector2Int(5, 0), out FacilityInstance source);
            placement.TryPlaceFacility(comet, new SimulationId("destination"), destinationDefinition, new Vector2Int(5, 3), out FacilityInstance destination);

            ResourceDefinition ore = ScriptableObject.CreateInstance<ResourceDefinition>();
            ore.ConfigurePrototype("ore", "Ore", ResourceCategory.Raw, 100d, true);
            DroneLogisticsSystem drones = new DroneLogisticsSystem();
            drones.TryCreateDedicatedLink(comet, new SimulationId("link"), hangar, source, destination, ore, 5, out _);

            PowerReport report = new PowerSystem().Calculate(comet);

            Assert.AreEqual(16d, report.GeneratedPower);
            Assert.AreEqual(7d, report.ConsumedPower);
            Assert.AreEqual(9d, report.AvailablePower);
            Assert.IsFalse(report.HasDeficit);
        }
    }
}
