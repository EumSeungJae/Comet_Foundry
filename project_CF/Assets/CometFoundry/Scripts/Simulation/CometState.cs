using System;
using System.Collections.Generic;
using CometFoundry.Core;
using CometFoundry.Data;
using UnityEngine;

namespace CometFoundry.Simulation
{
    public enum CometRole
    {
        Unassigned,
        Mining,
        Refining,
        Assembly,
        LogisticsHub,
        ResearchBase,
        MarketHub
    }

    public enum CometGrade
    {
        F,
        E,
        D,
        C,
        B,
        A,
        S
    }

    [Serializable]
    public sealed class ResourceDeposit
    {
        public ResourceDeposit(ResourceDefinition resource, double initialStacks)
            : this(resource, initialStacks, -1, Vector2Int.zero)
        {
        }

        public ResourceDeposit(ResourceDefinition resource, double initialStacks, int nodeIndex, Vector2Int coordinate)
        {
            Resource = resource;
            InitialStacks = Math.Max(0d, initialStacks);
            RemainingStacks = InitialStacks;
            NodeIndex = nodeIndex;
            Coordinate = coordinate;
        }

        public ResourceDefinition Resource { get; }
        public double InitialStacks { get; }
        public double RemainingStacks { get; private set; }
        public int NodeIndex { get; }
        public Vector2Int Coordinate { get; }
        public bool HasCoordinate => NodeIndex >= 0;
        public double InitialTons => Resource != null ? Resource.GetWeightForStacks(InitialStacks) : InitialStacks;
        public double RemainingTons => Resource != null ? Resource.GetWeightForStacks(RemainingStacks) : RemainingStacks;
        public bool IsDepleted => RemainingStacks <= 0.0001d;

        public double Extract(double requestedTons)
        {
            if (Resource == null)
            {
                return ExtractStacks(requestedTons);
            }

            double extractedStacks = ExtractStacks(Resource.GetStacksForWeight(requestedTons));
            return Resource.GetWeightForStacks(extractedStacks);
        }

        public double ExtractStacks(double requestedStacks)
        {
            if (requestedStacks <= 0d || IsDepleted)
            {
                return 0d;
            }

            double extracted = Math.Min(requestedStacks, RemainingStacks);
            RemainingStacks -= extracted;
            return extracted;
        }
    }

    [Serializable]
    public sealed class CometState
    {
        private readonly List<ResourceDeposit> deposits = new List<ResourceDeposit>();
        private readonly List<FacilityInstance> facilities = new List<FacilityInstance>();
        private readonly List<RailSegment> rails = new List<RailSegment>();
        private readonly List<RailLine> railLines = new List<RailLine>();
        private readonly List<ConveyorPathSegment> conveyorPaths = new List<ConveyorPathSegment>();
        private readonly List<DroneLogisticsLink> droneLinks = new List<DroneLogisticsLink>();

        public CometState(SimulationId id, string displayName, Vector2 position, float radius, int gridWidth = 16, int gridHeight = 16)
            : this(id, displayName, position, radius, CometGrade.S, CometGrade.S, "SS0000A1", 1f, 0, "A1", 5, gridWidth, gridHeight)
        {
        }

        public CometState(
            SimulationId id,
            string displayName,
            Vector2 position,
            float radius,
            CometGrade grade,
            CometGrade sizeGrade,
            string seedCode,
            float buildableRatio,
            int resourceTypeCount,
            string distanceCode,
            int distanceMinutes,
            int gridWidth,
            int gridHeight)
        {
            Id = id;
            DisplayName = displayName;
            Position = position;
            Radius = radius;
            Grade = grade;
            SizeGrade = sizeGrade;
            SeedCode = seedCode;
            BuildableRatio = Mathf.Clamp01(buildableRatio);
            ResourceTypeCount = Mathf.Clamp(resourceTypeCount, 0, 3);
            DistanceCode = distanceCode;
            DistanceMinutes = Mathf.Max(0, distanceMinutes);
            Grid = new CometGrid(gridWidth, gridHeight);
        }

        public SimulationId Id { get; }
        public string DisplayName { get; }
        public Vector2 Position { get; }
        public float Radius { get; }
        public CometGrade Grade { get; }
        public CometGrade SizeGrade { get; }
        public string SeedCode { get; }
        public float BuildableRatio { get; }
        public int ResourceTypeCount { get; }
        public string DistanceCode { get; }
        public int DistanceMinutes { get; }
        public CometRole Role { get; set; }
        public CometGrid Grid { get; }
        public IReadOnlyList<ResourceDeposit> Deposits => deposits;
        public IReadOnlyList<FacilityInstance> Facilities => facilities;
        public IReadOnlyList<RailSegment> Rails => rails;
        public IReadOnlyList<RailLine> RailLines => railLines;
        public IReadOnlyList<ConveyorPathSegment> ConveyorPaths => conveyorPaths;
        public IReadOnlyList<DroneLogisticsLink> DroneLinks => droneLinks;

        public void AddDeposit(ResourceDeposit deposit)
        {
            if (deposit != null)
            {
                deposits.Add(deposit);
            }
        }

        public void AddFacility(FacilityInstance facility)
        {
            if (facility != null)
            {
                facilities.Add(facility);
            }
        }

        public bool RemoveFacility(FacilityInstance facility) => facilities.Remove(facility);

        public void AddRail(RailSegment rail)
        {
            if (rail != null)
            {
                rails.Add(rail);
            }
        }

        public void AddRailLine(RailLine railLine)
        {
            if (railLine != null)
            {
                railLines.Add(railLine);
            }
        }

        public void AddConveyorPath(ConveyorPathSegment conveyorPath)
        {
            if (conveyorPath != null)
            {
                conveyorPaths.Add(conveyorPath);
            }
        }

        public void AddDroneLink(DroneLogisticsLink droneLink)
        {
            if (droneLink != null)
            {
                droneLinks.Add(droneLink);
            }
        }
    }
}
