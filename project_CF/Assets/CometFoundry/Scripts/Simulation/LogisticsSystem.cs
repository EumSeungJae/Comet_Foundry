using System;
using System.Collections.Generic;
using System.Linq;
using CometFoundry.Core;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    public sealed class LogisticsSystem
    {
        public TransportRoute FindBestDispatchRoute(IEnumerable<TransportRoute> routes)
        {
            if (routes == null)
            {
                return null;
            }

            return routes
                .Where(route => route != null
                    && route.IsEnabled
                    && route.Rule.CanDispatch(route.Source?.Inventory, route.Destination?.Inventory))
                .OrderByDescending(route => route.Rule.Priority)
                .FirstOrDefault();
        }

        public double MoveCargo(FacilityInstance source, Inventory cargo, ResourceDefinition resource, double requestedStacks)
        {
            return MoveCargoStacks(source, cargo, resource, requestedStacks);
        }

        public double MoveCargoStacks(FacilityInstance source, Inventory cargo, ResourceDefinition resource, double requestedStacks)
        {
            if (source == null || cargo == null || resource == null || requestedStacks <= 0d)
            {
                return 0d;
            }

            double removed = source.Inventory.RemoveStacks(resource, Math.Min(requestedStacks, cargo.FreeStacks));
            return cargo.AddStacks(resource, removed);
        }

        public double UnloadCargo(Inventory cargo, FacilityInstance destination, ResourceDefinition resource, double requestedStacks)
        {
            return UnloadCargoStacks(cargo, destination, resource, requestedStacks);
        }

        public double UnloadCargoStacks(Inventory cargo, FacilityInstance destination, ResourceDefinition resource, double requestedStacks)
        {
            if (cargo == null || destination == null || resource == null || requestedStacks <= 0d)
            {
                return 0d;
            }

            double removed = cargo.RemoveStacks(resource, Math.Min(requestedStacks, destination.Inventory.FreeStacks));
            return destination.Inventory.AddStacks(resource, removed);
        }

        public CargoShipState RentShip(CompanyState company, SimulationId id, CargoShipDefinition definition, double currentDay, double rentalDays, double rentalCostPerDay)
        {
            if (company == null || definition == null || rentalDays <= 0d || rentalCostPerDay < 0d)
            {
                return null;
            }

            if (!company.Spend(rentalDays * rentalCostPerDay))
            {
                return null;
            }

            return company.AddShip(id, definition, CargoShipOwnership.Rented, currentDay + rentalDays);
        }

        public CargoShipState CreateExchangeConsignmentShip(SimulationId id, CargoShipDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            return new CargoShipState(id, definition, CargoShipOwnership.ExchangeConsignment);
        }
    }
}
