using System;
using CometFoundry.Core;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    [Serializable]
    public sealed class DroneLogisticsLink
    {
        public DroneLogisticsLink(
            SimulationId id,
            FacilityInstance hangar,
            FacilityInstance source,
            FacilityInstance destination,
            ResourceDefinition resource,
            int assignedDroneCount,
            double speedMultiplier,
            double minimumPickupStacks = 0d,
            RoutePriority priority = RoutePriority.Normal)
        {
            Id = id;
            Hangar = hangar;
            Source = source;
            Destination = destination;
            Resource = resource;
            AssignedDroneCount = Math.Max(0, assignedDroneCount);
            SpeedMultiplier = Math.Max(1d, speedMultiplier);
            MinimumPickupStacks = Math.Max(0d, minimumPickupStacks);
            Priority = priority;
        }

        public SimulationId Id { get; }
        public FacilityInstance Hangar { get; }
        public FacilityInstance Source { get; }
        public FacilityInstance Destination { get; }
        public ResourceDefinition Resource { get; }
        public int AssignedDroneCount { get; }
        public double SpeedMultiplier { get; }
        public double MinimumPickupStacks { get; }
        public double MinimumPickupTons => MinimumPickupStacks;
        public RoutePriority Priority { get; }
    }
}
