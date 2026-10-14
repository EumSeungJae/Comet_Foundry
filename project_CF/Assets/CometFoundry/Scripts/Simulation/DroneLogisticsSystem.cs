using System.Collections.Generic;
using CometFoundry.Core;
using CometFoundry.Data;
using UnityEngine;

namespace CometFoundry.Simulation
{
    public sealed class DroneLogisticsSystem
    {
        public const double ConveyorPathSpeedMultiplier = 1.2d;

        public bool TryCreateDedicatedLink(
            CometState comet,
            SimulationId linkId,
            FacilityInstance hangar,
            FacilityInstance source,
            FacilityInstance destination,
            ResourceDefinition resource,
            int droneCount,
            out DroneLogisticsLink link)
        {
            link = null;
            if (comet == null || hangar == null || source == null || destination == null || resource == null || droneCount <= 0)
            {
                return false;
            }

            if (hangar.Definition == null || !hangar.Definition.IsDroneHangar)
            {
                return false;
            }

            if (GetAssignedDroneCount(comet, hangar) + droneCount > hangar.Definition.DroneCapacity)
            {
                return false;
            }

            if (GetAssignedLinkCount(comet, hangar) >= hangar.Definition.DroneLinkCapacity)
            {
                return false;
            }

            double speedMultiplier = GetPathSpeedMultiplier(comet, source, destination);
            if (speedMultiplier <= 0d)
            {
                return false;
            }

            link = new DroneLogisticsLink(linkId, hangar, source, destination, resource, droneCount, speedMultiplier);
            comet.AddDroneLink(link);
            return true;
        }

        public int GetAssignedDroneCount(CometState comet, FacilityInstance hangar)
        {
            if (comet == null || hangar == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < comet.DroneLinks.Count; i++)
            {
                if (comet.DroneLinks[i].Hangar == hangar)
                {
                    count += comet.DroneLinks[i].AssignedDroneCount;
                }
            }

            return count;
        }

        public int GetAssignedLinkCount(CometState comet, FacilityInstance hangar)
        {
            if (comet == null || hangar == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < comet.DroneLinks.Count; i++)
            {
                if (comet.DroneLinks[i].Hangar == hangar)
                {
                    count++;
                }
            }

            return count;
        }

        public bool HasNavigablePath(CometState comet, FacilityInstance source, FacilityInstance destination)
        {
            return GetPathSpeedMultiplier(comet, source, destination) > 0d;
        }

        public double GetPathSpeedMultiplier(CometState comet, FacilityInstance source, FacilityInstance destination)
        {
            if (comet == null || source == null || destination == null)
            {
                return 0d;
            }

            List<Vector2Int> starts = GetAdjacentNavigableTiles(comet, source);
            List<Vector2Int> goals = GetAdjacentNavigableTiles(comet, destination);
            if (starts.Count == 0 || goals.Count == 0)
            {
                return 0d;
            }

            HashSet<Vector2Int> goalSet = new HashSet<Vector2Int>(goals);
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            for (int i = 0; i < starts.Count; i++)
            {
                queue.Enqueue(starts[i]);
                visited.Add(starts[i]);
            }

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (goalSet.Contains(current))
                {
                    return HasFullyPavedPath(comet, starts, goalSet) ? ConveyorPathSpeedMultiplier : 1d;
                }

                EnqueueIfNavigable(comet, current + Vector2Int.up, queue, visited);
                EnqueueIfNavigable(comet, current + Vector2Int.down, queue, visited);
                EnqueueIfNavigable(comet, current + Vector2Int.left, queue, visited);
                EnqueueIfNavigable(comet, current + Vector2Int.right, queue, visited);
            }

            return 0d;
        }

        private bool HasFullyPavedPath(CometState comet, List<Vector2Int> starts, HashSet<Vector2Int> goals)
        {
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            for (int i = 0; i < starts.Count; i++)
            {
                if (!IsConveyorPath(comet, starts[i]))
                {
                    continue;
                }

                queue.Enqueue(starts[i]);
                visited.Add(starts[i]);
            }

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (goals.Contains(current))
                {
                    return true;
                }

                EnqueueIfPaved(comet, current + Vector2Int.up, queue, visited);
                EnqueueIfPaved(comet, current + Vector2Int.down, queue, visited);
                EnqueueIfPaved(comet, current + Vector2Int.left, queue, visited);
                EnqueueIfPaved(comet, current + Vector2Int.right, queue, visited);
            }

            return false;
        }

        private static void EnqueueIfPaved(CometState comet, Vector2Int coordinate, Queue<Vector2Int> queue, HashSet<Vector2Int> visited)
        {
            if (visited.Contains(coordinate) || !IsConveyorPath(comet, coordinate))
            {
                return;
            }

            visited.Add(coordinate);
            queue.Enqueue(coordinate);
        }

        private static void EnqueueIfNavigable(CometState comet, Vector2Int coordinate, Queue<Vector2Int> queue, HashSet<Vector2Int> visited)
        {
            if (visited.Contains(coordinate) || !IsDroneNavigable(comet, coordinate))
            {
                return;
            }

            visited.Add(coordinate);
            queue.Enqueue(coordinate);
        }

        private static List<Vector2Int> GetAdjacentNavigableTiles(CometState comet, FacilityInstance facility)
        {
            List<Vector2Int> tiles = new List<Vector2Int>();
            Vector2Int footprint = facility.Definition.FootprintSize;

            for (int x = 0; x < footprint.x; x++)
            {
                AddIfNavigable(comet, new Vector2Int(facility.Anchor.x + x, facility.Anchor.y - 1), tiles);
                AddIfNavigable(comet, new Vector2Int(facility.Anchor.x + x, facility.Anchor.y + footprint.y), tiles);
            }

            for (int y = 0; y < footprint.y; y++)
            {
                AddIfNavigable(comet, new Vector2Int(facility.Anchor.x - 1, facility.Anchor.y + y), tiles);
                AddIfNavigable(comet, new Vector2Int(facility.Anchor.x + footprint.x, facility.Anchor.y + y), tiles);
            }

            return tiles;
        }

        private static void AddIfNavigable(CometState comet, Vector2Int coordinate, List<Vector2Int> tiles)
        {
            if (IsDroneNavigable(comet, coordinate) && !tiles.Contains(coordinate))
            {
                tiles.Add(coordinate);
            }
        }

        private static bool IsDroneNavigable(CometState comet, Vector2Int coordinate)
        {
            CometTile tile = comet.Grid.GetTile(coordinate);
            return tile != null && tile.IsDroneNavigable;
        }

        private static bool IsConveyorPath(CometState comet, Vector2Int coordinate)
        {
            CometTile tile = comet.Grid.GetTile(coordinate);
            return tile != null && tile.IsConveyorPath;
        }
    }
}
