using System;
using System.Collections.Generic;
using CometFoundry.Core;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    [Serializable]
    public sealed class CompanyState
    {
        private readonly List<CometState> comets = new List<CometState>();
        private readonly List<TransportRoute> routes = new List<TransportRoute>();
        private readonly List<CargoShipState> ships = new List<CargoShipState>();

        public CompanyState(double startingCredits)
        {
            Credits = startingCredits;
        }

        public double Credits { get; private set; }
        public double Solar => Credits;
        public ExchangeTrustState ExchangeTrust { get; } = new ExchangeTrustState();
        public IReadOnlyList<CometState> Comets => comets;
        public IReadOnlyList<TransportRoute> Routes => routes;
        public IReadOnlyList<CargoShipState> Ships => ships;

        public bool Spend(double amount)
        {
            if (amount <= 0d)
            {
                return true;
            }

            if (Credits < amount)
            {
                return false;
            }

            Credits -= amount;
            return true;
        }

        public void Earn(double amount)
        {
            if (amount > 0d)
            {
                Credits += amount;
            }
        }

        public void AddComet(CometState comet)
        {
            if (comet != null)
            {
                comets.Add(comet);
            }
        }

        public void AddRoute(TransportRoute route)
        {
            if (route != null)
            {
                routes.Add(route);
            }
        }

        public CargoShipState AddShip(SimulationId id, CargoShipDefinition definition)
        {
            return AddShip(id, definition, CargoShipOwnership.PlayerOwned);
        }

        public CargoShipState AddShip(SimulationId id, CargoShipDefinition definition, CargoShipOwnership ownership, double rentalExpiresDay = 0d)
        {
            CargoShipState ship = new CargoShipState(id, definition, ownership, rentalExpiresDay);
            ships.Add(ship);
            return ship;
        }
    }
}
