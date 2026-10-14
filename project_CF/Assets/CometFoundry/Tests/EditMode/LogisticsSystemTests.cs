using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class LogisticsSystemTests
    {
        [Test]
        public void PlayerOwnedShip_CanChangeSpeedAndConsumesDurabilityBySpeedRule()
        {
            CargoShipDefinition definition = CreateShipDefinition();
            CargoShipState ship = new CargoShipState(new SimulationId("ship-owned"), definition);
            TransportRoute route = new TransportRoute(new SimulationId("route"), null, null, 10d);

            Assert.IsTrue(ship.SetSpeedMultiplier(2d));
            Assert.IsTrue(ship.AssignRoute(route));
            ship.MarkInTransit();
            ship.AdvanceTravelByDays(1d);

            Assert.AreEqual(0.2d, ship.TravelProgress, 0.0001d);
            Assert.AreEqual(96d, ship.Durability, 0.0001d);
        }

        [Test]
        public void RentedShip_CannotChangeSpeedAndExpiresByGameDay()
        {
            CompanyState company = new CompanyState(1000d);
            LogisticsSystem logistics = new LogisticsSystem();
            CargoShipDefinition definition = CreateShipDefinition();

            CargoShipState ship = logistics.RentShip(company, new SimulationId("ship-rented"), definition, 3d, 2d, 100d);

            Assert.IsNotNull(ship);
            Assert.AreEqual(CargoShipOwnership.Rented, ship.Ownership);
            Assert.AreEqual(800d, company.Credits);
            Assert.IsFalse(ship.SetSpeedMultiplier(1.5d));
            Assert.IsFalse(ship.IsExpired(4.99d));
            Assert.IsTrue(ship.IsExpired(5d));
        }

        [Test]
        public void ExchangeConsignmentShip_IsNotOwnedByCompany()
        {
            LogisticsSystem logistics = new LogisticsSystem();
            CargoShipDefinition definition = CreateShipDefinition();

            CargoShipState ship = logistics.CreateExchangeConsignmentShip(new SimulationId("exchange-ship"), definition);

            Assert.IsNotNull(ship);
            Assert.AreEqual(CargoShipOwnership.ExchangeConsignment, ship.Ownership);
        }

        [Test]
        public void DefaultClockTimeScale_MakesFiveRealMinutesOneGameDay()
        {
            SimulationClock clock = new SimulationClock();

            clock.AdvanceSeconds(SimulationClock.DefaultRealSecondsPerGameDay);

            Assert.AreEqual(1d, clock.Day, 0.0001d);
        }

        [Test]
        public void UnloadCargo_CanStoreGoodsInWarehouseInventory()
        {
            ResourceDefinition ore = ScriptableObject.CreateInstance<ResourceDefinition>();
            ore.ConfigurePrototype("ore", "Ore", ResourceCategory.Raw, 100d, true);
            FacilityDefinition warehouseDefinition = FacilityDefinition.CreatePrototypeWarehouse();
            FacilityInstance warehouse = new FacilityInstance(new SimulationId("warehouse"), warehouseDefinition);
            Inventory cargo = new Inventory(20d);
            cargo.Add(ore, 20d);
            LogisticsSystem logistics = new LogisticsSystem();

            double unloaded = logistics.UnloadCargo(cargo, warehouse, ore, 20d);

            Assert.AreEqual(20d, unloaded);
            Assert.AreEqual(20d, warehouse.Inventory.GetAmount(ore));
            Assert.AreEqual(480d, warehouse.Inventory.FreeTons);
        }

        private static CargoShipDefinition CreateShipDefinition()
        {
            CargoShipDefinition definition = ScriptableObject.CreateInstance<CargoShipDefinition>();
            definition.ConfigurePrototype("cargo", "Cargo", 100d, 1d, 10d, 0d);
            return definition;
        }
    }
}
