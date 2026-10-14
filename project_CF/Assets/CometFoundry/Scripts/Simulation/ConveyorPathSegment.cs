using System;
using CometFoundry.Core;
using UnityEngine;

namespace CometFoundry.Simulation
{
    [Serializable]
    public sealed class ConveyorPathSegment
    {
        public ConveyorPathSegment(SimulationId id, Vector2Int coordinate)
        {
            Id = id;
            Coordinate = coordinate;
        }

        public SimulationId Id { get; }
        public Vector2Int Coordinate { get; }
    }
}
