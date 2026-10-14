using System;
using System.Collections.Generic;
using CometFoundry.Core;
using CometFoundry.Data;
using UnityEngine;

namespace CometFoundry.Simulation
{
    public sealed class CometGenerator
    {
        private const int MaximumGridSize = 16;
        private const int MinutesPerDistanceNumber = 5;
        private const int MinutesPerDistanceLetter = 60;
        private const int MaxResourceNodesPerResource = 6;
        private static readonly CometGrade[] GradeRollOrder =
        {
            CometGrade.S,
            CometGrade.A,
            CometGrade.B,
            CometGrade.C,
            CometGrade.D,
            CometGrade.E,
            CometGrade.F
        };
        private static readonly char[] DistanceLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        private readonly System.Random random;

        public CometGenerator(int seed)
        {
            random = new System.Random(seed);
        }

        public CometState Generate(CometGenerationProfile profile, int index, Vector2 position)
        {
            return Generate(profile, index, position, null);
        }

        public CometState Generate(CometGenerationProfile profile, int index, Vector2 position, IEnumerable<CometState> existingComets)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            HashSet<string> usedDistanceCodes = CollectUsedDistanceCodes(existingComets);
            CometSeed cometSeed = CreateSeedCode(profile, usedDistanceCodes);
            int gridSize = GetGridSize(cometSeed.SizeGrade);
            float buildableRatio = GetBuildableRatio(cometSeed.ActivationGrade);
            float radius = Mathf.Lerp(profile.SizeRange.x, profile.SizeRange.y, (float)gridSize / MaximumGridSize);
            Vector2 cometPosition = position + GetPositionFromDistance(cometSeed.DistanceMinutes);
            CometState comet = new CometState(
                new SimulationId($"comet-{index:0000}"),
                cometSeed.Code,
                cometPosition,
                radius,
                cometSeed.ActivationGrade,
                cometSeed.SizeGrade,
                cometSeed.Code,
                buildableRatio,
                cometSeed.ResourceTypeCount,
                cometSeed.DistanceCode,
                cometSeed.DistanceMinutes,
                gridSize,
                gridSize);

            ApplyBuildablePattern(comet.Grid, buildableRatio);

            for (int i = 0; i < cometSeed.ResourceTypeCount; i++)
            {
                int rollIndex = cometSeed.ResourceRollIndexes[i];
                if (rollIndex < 0 || rollIndex >= profile.Deposits.Count)
                {
                    continue;
                }

                ResourceDepositRoll roll = profile.Deposits[rollIndex];
                if (roll.Resource == null)
                {
                    continue;
                }

                double totalStacks = Math.Min(GetMaxResourceStacks(comet.Grade), Lerp(roll.MinStacks, roll.MaxStacks, random.NextDouble()));
                AddResourceNodes(comet, roll.Resource, totalStacks);
            }

            return comet;
        }

        public static double GetMaxResourceStacks(CometGrade grade)
        {
            switch (grade)
            {
                case CometGrade.S:
                    return 200000d;
                case CometGrade.A:
                    return 80000d;
                case CometGrade.B:
                    return 30000d;
                case CometGrade.C:
                    return 12000d;
                case CometGrade.D:
                    return 5000d;
                case CometGrade.E:
                    return 2000d;
                default:
                    return 900d;
            }
        }

        public static int GetGridSize(CometGrade grade)
        {
            switch (grade)
            {
                case CometGrade.S:
                    return 16;
                case CometGrade.A:
                    return 14;
                case CometGrade.B:
                    return 13;
                case CometGrade.C:
                    return 12;
                case CometGrade.D:
                    return 11;
                case CometGrade.E:
                    return 10;
                default:
                    return 9;
            }
        }

        public static float GetBuildableRatio(CometGrade grade)
        {
            switch (grade)
            {
                case CometGrade.S:
                    return 1f;
                case CometGrade.A:
                    return 0.98f;
                case CometGrade.B:
                    return 0.96f;
                case CometGrade.C:
                    return 0.94f;
                case CometGrade.D:
                    return 0.92f;
                case CometGrade.E:
                    return 0.90f;
                default:
                    return 0.88f;
            }
        }

        public static int GetDistanceMinutes(char letterCode, int numberCode)
        {
            int letterIndex = Mathf.Clamp(char.ToUpperInvariant(letterCode) - 'A', 0, 25);
            int number = Mathf.Clamp(numberCode, 0, 9);
            return letterIndex * MinutesPerDistanceLetter + number * MinutesPerDistanceNumber;
        }

        private Vector2 GetPositionFromDistance(int distanceMinutes)
        {
            float distanceHours = distanceMinutes / 60f;
            float distanceUnits = Mathf.Max(0.5f, distanceHours * 4f);
            float angle = (float)(random.NextDouble() * Math.PI * 2d);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distanceUnits;
        }

        private CometGrade RollGrade()
        {
            return GradeRollOrder[random.Next(0, GradeRollOrder.Length)];
        }

        private CometSeed CreateSeedCode(CometGenerationProfile profile, HashSet<string> usedDistanceCodes)
        {
            CometGrade activationGrade = RollGrade();
            CometGrade sizeGrade = RollGrade();
            int resourceTypeCount = random.Next(0, 4);
            int[] resourceRollIndexes = CreateResourceRollIndexes(profile, resourceTypeCount);
            string distanceCode = RollUniqueDistanceCode(usedDistanceCodes, out char distanceLetter, out int distanceNumber);
            string code = string.Concat(
                GetGradeCode(activationGrade),
                GetGradeCode(sizeGrade),
                resourceTypeCount,
                ToResourceCode(resourceRollIndexes[0]),
                ToResourceCode(resourceRollIndexes[1]),
                ToResourceCode(resourceRollIndexes[2]),
                distanceCode);

            return new CometSeed(
                code,
                activationGrade,
                sizeGrade,
                resourceTypeCount,
                resourceRollIndexes,
                distanceCode,
                GetDistanceMinutes(distanceLetter, distanceNumber));
        }

        private string RollUniqueDistanceCode(HashSet<string> usedDistanceCodes, out char distanceLetter, out int distanceNumber)
        {
            usedDistanceCodes ??= new HashSet<string>();
            for (int attempt = 0; attempt < 512; attempt++)
            {
                distanceLetter = DistanceLetters[random.Next(0, DistanceLetters.Length)];
                distanceNumber = random.Next(distanceLetter == 'A' ? 1 : 0, 10);
                string code = $"{distanceLetter}{distanceNumber}";
                if (!usedDistanceCodes.Contains(code))
                {
                    usedDistanceCodes.Add(code);
                    return code;
                }
            }

            for (int letterIndex = 0; letterIndex < DistanceLetters.Length; letterIndex++)
            {
                distanceLetter = DistanceLetters[letterIndex];
                int startNumber = distanceLetter == 'A' ? 1 : 0;
                for (distanceNumber = startNumber; distanceNumber <= 9; distanceNumber++)
                {
                    string code = $"{distanceLetter}{distanceNumber}";
                    if (!usedDistanceCodes.Contains(code))
                    {
                        usedDistanceCodes.Add(code);
                        return code;
                    }
                }
            }

            throw new InvalidOperationException("No available comet distance codes remain.");
        }

        private static HashSet<string> CollectUsedDistanceCodes(IEnumerable<CometState> existingComets)
        {
            HashSet<string> usedDistanceCodes = new HashSet<string>();
            if (existingComets == null)
            {
                return usedDistanceCodes;
            }

            foreach (CometState comet in existingComets)
            {
                if (comet != null && !string.IsNullOrWhiteSpace(comet.DistanceCode))
                {
                    usedDistanceCodes.Add(comet.DistanceCode);
                }
            }

            return usedDistanceCodes;
        }

        private static char GetGradeCode(CometGrade grade)
        {
            return grade == CometGrade.S ? 'S' : grade.ToString()[0];
        }

        private int[] CreateResourceRollIndexes(CometGenerationProfile profile, int resourceTypeCount)
        {
            int[] indexes = { -1, -1, -1 };
            if (profile.Deposits.Count == 0 || resourceTypeCount <= 0)
            {
                return indexes;
            }

            List<int> availableIndexes = new List<int>();
            for (int i = 0; i < profile.Deposits.Count && i < 10; i++)
            {
                availableIndexes.Add(i);
            }

            for (int i = 0; i < resourceTypeCount && availableIndexes.Count > 0; i++)
            {
                int pick = random.Next(0, availableIndexes.Count);
                indexes[i] = availableIndexes[pick];
                availableIndexes.RemoveAt(pick);
            }

            return indexes;
        }

        private static char ToResourceCode(int resourceRollIndex)
        {
            return resourceRollIndex < 0 ? 'X' : (char)('0' + Mathf.Clamp(resourceRollIndex, 0, 9));
        }

        private void ApplyBuildablePattern(CometGrid grid, float buildableRatio)
        {
            int tileCount = grid.Width * grid.Height;
            int targetBuildableTiles = Mathf.RoundToInt(tileCount * Mathf.Clamp01(buildableRatio));
            int inactiveTiles = Mathf.Max(0, tileCount - targetBuildableTiles);

            // Damage the outer rim, preserving an uninterrupted interior for construction and logistics.
            var rim = new List<Vector2Int>();
            for (int y = 0; y < grid.Height; y++) for (int x = 0; x < grid.Width; x++)
                if (x == 0 || y == 0 || x == grid.Width - 1 || y == grid.Height - 1) rim.Add(new Vector2Int(x, y));
            for (int i = rim.Count - 1; i > 0; i--)
            {
                int pick = random.Next(i + 1);
                var swap = rim[i]; rim[i] = rim[pick]; rim[pick] = swap;
            }
            int removed = 0;
            foreach (var coordinate in rim)
            {
                if (removed >= inactiveTiles) break;
                var previous = grid.GetTile(coordinate).Terrain;
                grid.GetTile(coordinate).SetTerrain(CometTileTerrain.Void);
                if (ActiveTilesConnected(grid, tileCount - removed - 1)) removed++;
                else grid.GetTile(coordinate).SetTerrain(previous);
            }
        }

        private static bool ActiveTilesConnected(CometGrid grid, int expected)
        {
            var queue = new Queue<Vector2Int>(); var visited = new HashSet<Vector2Int>();
            var start = new Vector2Int(grid.Width / 2, grid.Height / 2);
            queue.Enqueue(start); visited.Add(start);
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var direction in directions)
                {
                    var next = current + direction;
                    var tile = grid.GetTile(next);
                    if (tile == null || tile.Terrain == CometTileTerrain.Void || !visited.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }
            return visited.Count == expected;
        }

        private void AddResourceNodes(CometState comet, ResourceDefinition resource, double totalStacks)
        {
            if (comet == null || resource == null || totalStacks <= 0d)
            {
                return;
            }

            int nodeCount = GetResourceNodeCount(totalStacks);
            double remainingStacks = totalStacks;
            for (int i = 0; i < nodeCount; i++)
            {
                int nodesLeft = nodeCount - i;
                double nodeStacks = i == nodeCount - 1 ? remainingStacks : Math.Max(1d, remainingStacks / nodesLeft);
                remainingStacks -= nodeStacks;
                comet.AddDeposit(new ResourceDeposit(resource, nodeStacks, i, PickActiveTile(comet.Grid)));
            }
        }

        public static int GetResourceNodeCount(double totalStacks)
        {
            if (totalStacks <= 100d)
            {
                return 1;
            }

            if (totalStacks <= 500d)
            {
                return 2;
            }

            if (totalStacks <= 1500d)
            {
                return 3;
            }

            if (totalStacks <= 3500d)
            {
                return 4;
            }

            if (totalStacks <= 8000d)
            {
                return 5;
            }

            return MaxResourceNodesPerResource;
        }

        private Vector2Int PickActiveTile(CometGrid grid)
        {
            for (int attempt = 0; attempt < 256; attempt++)
            {
                Vector2Int coordinate = new Vector2Int(random.Next(0, grid.Width), random.Next(0, grid.Height));
                if (grid.GetTile(coordinate).Terrain != CometTileTerrain.Void)
                {
                    return coordinate;
                }
            }

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    if (grid.GetTile(coordinate).Terrain != CometTileTerrain.Void)
                    {
                        return coordinate;
                    }
                }
            }

            return Vector2Int.zero;
        }

        private sealed class CometSeed
        {
            public CometSeed(
                string code,
                CometGrade activationGrade,
                CometGrade sizeGrade,
                int resourceTypeCount,
                int[] resourceRollIndexes,
                string distanceCode,
                int distanceMinutes)
            {
                Code = code;
                ActivationGrade = activationGrade;
                SizeGrade = sizeGrade;
                ResourceTypeCount = resourceTypeCount;
                ResourceRollIndexes = resourceRollIndexes;
                DistanceCode = distanceCode;
                DistanceMinutes = distanceMinutes;
            }

            public string Code { get; }
            public CometGrade ActivationGrade { get; }
            public CometGrade SizeGrade { get; }
            public int ResourceTypeCount { get; }
            public int[] ResourceRollIndexes { get; }
            public string DistanceCode { get; }
            public int DistanceMinutes { get; }
        }

        private static double Lerp(double min, double max, double t)
        {
            return min + (max - min) * Clamp01(t);
        }

        private static double Clamp01(double value)
        {
            if (value < 0d)
            {
                return 0d;
            }

            return value > 1d ? 1d : value;
        }
    }
}
