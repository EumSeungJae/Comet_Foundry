using System;
using System.Collections.Generic;
using UnityEngine;

namespace CometFoundry.Simulation
{
    [Serializable]
    public sealed class CometGrid
    {
        private readonly CometTile[,] tiles;

        public CometGrid(int width, int height)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            tiles = new CometTile[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    tiles[x, y] = new CometTile(x, y, CometTileTerrain.Stable);
                }
            }
        }

        public int Width { get; }
        public int Height { get; }

        public bool Contains(Vector2Int coordinate)
        {
            return coordinate.x >= 0 && coordinate.y >= 0 && coordinate.x < Width && coordinate.y < Height;
        }

        public CometTile GetTile(Vector2Int coordinate)
        {
            return Contains(coordinate) ? tiles[coordinate.x, coordinate.y] : null;
        }

        public IEnumerable<CometTile> GetFootprintTiles(Vector2Int anchor, Vector2Int footprintSize)
        {
            Vector2Int size = NormalizeFootprint(footprintSize);
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    CometTile tile = GetTile(new Vector2Int(anchor.x + x, anchor.y + y));
                    if (tile != null)
                    {
                        yield return tile;
                    }
                }
            }
        }

        public bool CanOccupy(Vector2Int anchor, Vector2Int footprintSize)
        {
            Vector2Int size = NormalizeFootprint(footprintSize);
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    CometTile tile = GetTile(new Vector2Int(anchor.x + x, anchor.y + y));
                    if (tile == null || !tile.IsBuildable)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static Vector2Int NormalizeFootprint(Vector2Int footprintSize)
        {
            return new Vector2Int(Mathf.Max(1, footprintSize.x), Mathf.Max(1, footprintSize.y));
        }
    }
}
