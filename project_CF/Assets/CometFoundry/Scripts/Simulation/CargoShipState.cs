using System;
using CometFoundry.Core;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    public enum CargoShipStatus
    {
        Idle,
        Loading,
        InTransit,
        Unloading
    }

    public enum CargoShipOwnership
    {
        PlayerOwned,
        Rented,
        ExchangeConsignment
    }

    [Serializable]
    public sealed class CargoShipState
    {
        public CargoShipState(SimulationId id, CargoShipDefinition definition)
            : this(id, definition, CargoShipOwnership.PlayerOwned, 0d)
        {
        }

        public CargoShipState(SimulationId id, CargoShipDefinition definition, CargoShipOwnership ownership, double rentalExpiresDay = 0d)
        {
            Id = id;
            Definition = definition;
            Ownership = ownership;
            RentalExpiresDay = rentalExpiresDay;
            Durability = 100d;
            SpeedMultiplier = 1d;
            Cargo = new Inventory(definition != null ? definition.CargoCapacityStacks : 0d);
        }

        public SimulationId Id { get; }
        public CargoShipDefinition Definition { get; }
        public CargoShipOwnership Ownership { get; }
        public Inventory Cargo { get; }
        public TransportRoute AssignedRoute { get; private set; }
        public CargoShipStatus Status { get; private set; }
        public double TravelProgress { get; private set; }
        public double SpeedMultiplier { get; private set; }
        public double Durability { get; private set; }
        public double RentalExpiresDay { get; }
        public bool IsRental => Ownership == CargoShipOwnership.Rented;
        public bool IsExpired(double currentDay) => IsRental && currentDay >= RentalExpiresDay;
        public double EffectiveSpeedUnitsPerDay => (Definition != null ? Definition.SpeedUnitsPerDay : 0d) * SpeedMultiplier;
        public double DurabilityDrainMultiplier => GetDurabilityDrainMultiplier(SpeedMultiplier);

        public bool AssignRoute(TransportRoute route)
        {
            if (route == null || !route.IsEnabled || Status != CargoShipStatus.Idle || Durability <= 0d)
            {
                return false;
            }

            AssignedRoute = route;
            Status = CargoShipStatus.Loading;
            TravelProgress = 0d;
            return true;
        }

        public void ClearRoute()
        {
            AssignedRoute = null;
            Status = CargoShipStatus.Idle;
            TravelProgress = 0d;
        }

        public void MarkInTransit()
        {
            if (AssignedRoute != null)
            {
                Status = CargoShipStatus.InTransit;
            }
        }

        public void AdvanceTravel(double normalizedDelta)
        {
            if (Status != CargoShipStatus.InTransit)
            {
                return;
            }

            TravelProgress = Clamp01(TravelProgress + normalizedDelta);
            if (TravelProgress >= 1d)
            {
                Status = CargoShipStatus.Unloading;
            }
        }

        public bool SetSpeedMultiplier(double multiplier)
        {
            if (Ownership != CargoShipOwnership.PlayerOwned || !IsAllowedSpeedMultiplier(multiplier))
            {
                return false;
            }

            SpeedMultiplier = multiplier;
            return true;
        }

        public void AdvanceTravelByDays(double deltaDays)
        {
            if (Status != CargoShipStatus.InTransit || AssignedRoute == null || deltaDays <= 0d)
            {
                return;
            }

            double speed = EffectiveSpeedUnitsPerDay;
            if (speed <= 0d)
            {
                return;
            }

            AdvanceTravel(deltaDays * speed / AssignedRoute.DistanceUnits);
            ApplyDurabilityLoss(deltaDays * DurabilityDrainMultiplier);
        }

        public void ApplyDurabilityLoss(double amount)
        {
            if (Ownership != CargoShipOwnership.PlayerOwned || amount <= 0d)
            {
                return;
            }

            Durability = Math.Max(0d, Durability - amount);
        }

        public static bool IsAllowedSpeedMultiplier(double multiplier)
        {
            return multiplier == 0.5d
                || multiplier == 0.75d
                || multiplier == 1d
                || multiplier == 1.25d
                || multiplier == 1.5d
                || multiplier == 1.75d
                || multiplier == 2d;
        }

        public static double GetDurabilityDrainMultiplier(double speedMultiplier)
        {
            if (speedMultiplier == 0.5d)
            {
                return 0.5d;
            }

            if (speedMultiplier == 0.75d)
            {
                return 0.75d;
            }

            if (speedMultiplier == 1.25d)
            {
                return 1.5d;
            }

            if (speedMultiplier == 1.5d)
            {
                return 2d;
            }

            if (speedMultiplier == 1.75d)
            {
                return 3d;
            }

            if (speedMultiplier == 2d)
            {
                return 4d;
            }

            return 1d;
        }

        private static double Clamp01(double value)
        {
            if (value < 0d)
            {
                return 0d;
            }

            return value > 1d ? 1d : value;
        }
    }
}
