using System;
using CometFoundry.Core;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    public enum RoutePriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    [Serializable]
    public sealed class AutomationRule
    {
        public ResourceDefinition Resource { get; set; }
        public double MinimumPickupStacks { get; set; }
        public double DestinationFreeCapacityRequiredStacks { get; set; }
        public double MinimumPickupTons
        {
            get => MinimumPickupStacks;
            set => MinimumPickupStacks = value;
        }

        public double DestinationFreeCapacityRequiredTons
        {
            get => DestinationFreeCapacityRequiredStacks;
            set => DestinationFreeCapacityRequiredStacks = value;
        }

        public RoutePriority Priority { get; set; } = RoutePriority.Normal;

        public bool CanDispatch(Inventory source, Inventory destination)
        {
            if (Resource == null || source == null || destination == null)
            {
                return false;
            }

            return source.GetAmount(Resource) >= MinimumPickupStacks
                && destination.FreeStacks >= DestinationFreeCapacityRequiredStacks;
        }
    }

    [Serializable]
    public sealed class TransportRoute
    {
        public TransportRoute(SimulationId id, FacilityInstance source, FacilityInstance destination, double distanceUnits = 1d)
        {
            Id = id;
            Source = source;
            Destination = destination;
            DistanceUnits = Math.Max(0.0001d, distanceUnits);
            Rule = new AutomationRule();
        }

        public SimulationId Id { get; }
        public FacilityInstance Source { get; }
        public FacilityInstance Destination { get; }
        public double DistanceUnits { get; private set; }
        public AutomationRule Rule { get; }
        public bool IsEnabled { get; set; } = true;

        public void SetDistance(double distanceUnits)
        {
            DistanceUnits = Math.Max(0.0001d, distanceUnits);
        }
    }
}
