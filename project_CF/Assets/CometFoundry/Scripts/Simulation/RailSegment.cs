using System;
using CometFoundry.Core;
using UnityEngine;

namespace CometFoundry.Simulation
{
    [Serializable]
    public sealed class RailSegment
    {
        public RailSegment(SimulationId id, Vector2Int coordinate, SimulationId lineId)
        {
            Id = id;
            Coordinate = coordinate;
            LineId = lineId;
        }

        public SimulationId Id { get; }
        public Vector2Int Coordinate { get; }
        public SimulationId LineId { get; }
    }

    [Serializable]
    public sealed class RailLine
    {
        private readonly System.Collections.Generic.List<Vector2Int> path = new System.Collections.Generic.List<Vector2Int>();

        public RailLine(SimulationId id, System.Collections.Generic.IEnumerable<Vector2Int> coordinates)
        {
            Id = id;
            if (coordinates == null)
            {
                return;
            }

            foreach (Vector2Int coordinate in coordinates)
            {
                path.Add(coordinate);
            }
        }

        public SimulationId Id { get; }
        public System.Collections.Generic.IReadOnlyList<Vector2Int> Path => path;
        public Vector2Int Start => path.Count > 0 ? path[0] : Vector2Int.zero;
        public Vector2Int End => path.Count > 0 ? path[path.Count - 1] : Vector2Int.zero;
        public int SegmentCount => UnityEngine.Mathf.Max(0, path.Count - 1);
    }
}
