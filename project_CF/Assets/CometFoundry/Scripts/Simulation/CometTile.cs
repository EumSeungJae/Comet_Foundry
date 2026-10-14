using System;
using CometFoundry.Core;

namespace CometFoundry.Simulation
{
    public enum CometTileTerrain
    {
        Stable,
        Rough,
        Cracked,
        Void
    }

    public enum CometTileOccupant
    {
        Empty,
        Facility,
        Rail,
        ConveyorPath
    }

    [Serializable]
    public sealed class CometTile
    {
        public CometTile(int x, int y, CometTileTerrain terrain)
        {
            X = x;
            Y = y;
            Terrain = terrain;
        }

        public int X { get; }
        public int Y { get; }
        public CometTileTerrain Terrain { get; private set; }
        public CometTileOccupant Occupant { get; private set; }
        public SimulationId OccupantId { get; private set; } = SimulationId.Empty;
        public bool IsBuildable => Terrain != CometTileTerrain.Void && Occupant == CometTileOccupant.Empty;
        public bool IsDroneNavigable => Terrain != CometTileTerrain.Void && Occupant != CometTileOccupant.Facility;
        public bool IsConveyorPath => Occupant == CometTileOccupant.ConveyorPath;

        public void SetTerrain(CometTileTerrain terrain)
        {
            Terrain = terrain;
        }

        public void Occupy(CometTileOccupant occupant, SimulationId occupantId)
        {
            Occupant = occupant;
            OccupantId = occupantId;
        }

        public void ClearOccupant()
        {
            Occupant = CometTileOccupant.Empty;
            OccupantId = SimulationId.Empty;
        }
    }
}
