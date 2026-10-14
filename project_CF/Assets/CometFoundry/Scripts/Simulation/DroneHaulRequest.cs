using System;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    public enum DroneHaulRequestType
    {
        ProductionInput,
        MarketReservation,
        Storage
    }

    [Serializable]
    public sealed class DroneHaulRequest
    {
        public DroneHaulRequest(
            DroneHaulRequestType requestType,
            FacilityInstance source,
            FacilityInstance destination,
            ResourceDefinition resource,
            double requestedStacks,
            int buildingPriority = 0)
        {
            RequestType = requestType;
            Source = source;
            Destination = destination;
            Resource = resource;
            RequestedStacks = Math.Max(0d, requestedStacks);
            BuildingPriority = Math.Max(0, buildingPriority);
        }

        public DroneHaulRequestType RequestType { get; }
        public FacilityInstance Source { get; }
        public FacilityInstance Destination { get; }
        public ResourceDefinition Resource { get; }
        public double RequestedStacks { get; }
        public int BuildingPriority { get; }
        public int PriorityScore => GetTypePriority(RequestType) * 1000 + BuildingPriority * 100;

        public static int GetTypePriority(DroneHaulRequestType requestType)
        {
            switch (requestType)
            {
                case DroneHaulRequestType.ProductionInput:
                    return 3;
                case DroneHaulRequestType.MarketReservation:
                    return 2;
                default:
                    return 1;
            }
        }
    }
}
