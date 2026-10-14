using System.Collections.Generic;
using CometFoundry.Core;
using CometFoundry.Data;
using UnityEngine;

namespace CometFoundry.Simulation
{
    public sealed class PlacementSystem
    {
        public const double ConveyorPathBuildCostPerTile = 300d;

        public bool TryPlaceFacility(CometState comet, SimulationId facilityId, FacilityDefinition definition, Vector2Int anchor, out FacilityInstance facility)
        {
            facility = null;
            if (comet == null || definition == null || !comet.Grid.CanOccupy(anchor, definition.FootprintSize))
            {
                return false;
            }

            facility = new FacilityInstance(facilityId, definition, anchor);
            foreach (CometTile tile in comet.Grid.GetFootprintTiles(anchor, definition.FootprintSize))
            {
                tile.Occupy(CometTileOccupant.Facility, facilityId);
            }

            comet.AddFacility(facility);
            return true;
        }

        public bool TryPlaceRail(CometState comet, SimulationId railId, Vector2Int coordinate, out RailSegment rail)
        {
            return TryPlaceRail(comet, railId, SimulationId.Empty, coordinate, out rail);
        }

        public bool TryPlaceRail(CometState comet, SimulationId railId, SimulationId lineId, Vector2Int coordinate, out RailSegment rail)
        {
            rail = null;
            if (comet == null)
            {
                return false;
            }

            CometTile tile = comet.Grid.GetTile(coordinate);
            if (tile == null || !tile.IsBuildable)
            {
                return false;
            }

            rail = new RailSegment(railId, coordinate, lineId);
            tile.Occupy(CometTileOccupant.Rail, railId);
            comet.AddRail(rail);
            return true;
        }

        public bool CanPlaceRailPath(CometState comet, IReadOnlyList<Vector2Int> path)
        {
            if (comet == null || path == null || path.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < path.Count; i++)
            {
                CometTile tile = comet.Grid.GetTile(path[i]);
                if (tile == null || !tile.IsBuildable)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryPlaceRailPath(CometState comet, string railIdPrefix, IReadOnlyList<Vector2Int> path, int startIndex, out int placedCount)
        {
            return TryPlaceRailPath(comet, new SimulationId($"{railIdPrefix}-line-{startIndex:000}"), railIdPrefix, path, startIndex, out placedCount, out _);
        }

        public bool TryPlaceRailPath(CometState comet, SimulationId lineId, string railIdPrefix, IReadOnlyList<Vector2Int> path, int startIndex, out int placedCount, out RailLine railLine)
        {
            placedCount = 0;
            railLine = null;
            if (!CanPlaceRailPath(comet, path))
            {
                return false;
            }

            for (int i = 0; i < path.Count; i++)
            {
                SimulationId id = new SimulationId($"{railIdPrefix}-{startIndex + i:000}");
                TryPlaceRail(comet, id, lineId, path[i], out _);
                placedCount++;
            }

            railLine = new RailLine(lineId, path);
            comet.AddRailLine(railLine);
            return true;
        }

        public bool TryPlaceConveyorPath(CompanyState company, CometState comet, SimulationId pathId, Vector2Int coordinate, out ConveyorPathSegment conveyorPath)
        {
            conveyorPath = null;
            if (company == null || comet == null)
            {
                return false;
            }

            CometTile tile = comet.Grid.GetTile(coordinate);
            if (tile == null || !tile.IsBuildable || !company.Spend(ConveyorPathBuildCostPerTile))
            {
                return false;
            }

            conveyorPath = new ConveyorPathSegment(pathId, coordinate);
            tile.Occupy(CometTileOccupant.ConveyorPath, pathId);
            comet.AddConveyorPath(conveyorPath);
            return true;
        }

        public IReadOnlyList<Vector2Int> GetManhattanPath(Vector2Int from, Vector2Int to)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int current = from;

            path.Add(current);

            while (current.x != to.x)
            {
                current.x += current.x < to.x ? 1 : -1;
                path.Add(current);
            }

            while (current.y != to.y)
            {
                current.y += current.y < to.y ? 1 : -1;
                path.Add(current);
            }

            return path;
        }
    }
}
