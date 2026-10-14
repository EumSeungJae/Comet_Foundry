using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class CometGeneratorTests
    {
        [Test]
        public void GradeRules_ReturnExpectedGridSizeAndBuildableRatio()
        {
            Assert.AreEqual(16, CometGenerator.GetGridSize(CometGrade.S));
            Assert.AreEqual(14, CometGenerator.GetGridSize(CometGrade.A));
            Assert.AreEqual(13, CometGenerator.GetGridSize(CometGrade.B));
            Assert.AreEqual(12, CometGenerator.GetGridSize(CometGrade.C));
            Assert.AreEqual(11, CometGenerator.GetGridSize(CometGrade.D));
            Assert.AreEqual(10, CometGenerator.GetGridSize(CometGrade.E));
            Assert.AreEqual(9, CometGenerator.GetGridSize(CometGrade.F));

            Assert.AreEqual(1f, CometGenerator.GetBuildableRatio(CometGrade.S));
            Assert.AreEqual(0.9f, CometGenerator.GetBuildableRatio(CometGrade.A));
            Assert.AreEqual(0.8f, CometGenerator.GetBuildableRatio(CometGrade.B));
            Assert.AreEqual(0.7f, CometGenerator.GetBuildableRatio(CometGrade.C));
            Assert.AreEqual(0.6f, CometGenerator.GetBuildableRatio(CometGrade.D));
            Assert.AreEqual(0.5f, CometGenerator.GetBuildableRatio(CometGrade.E));
            Assert.AreEqual(0.4f, CometGenerator.GetBuildableRatio(CometGrade.F));
        }

        [Test]
        public void ResourceGradeRules_ReturnExpectedMaximumStacks()
        {
            Assert.AreEqual(200000d, CometGenerator.GetMaxResourceStacks(CometGrade.S));
            Assert.AreEqual(80000d, CometGenerator.GetMaxResourceStacks(CometGrade.A));
            Assert.AreEqual(30000d, CometGenerator.GetMaxResourceStacks(CometGrade.B));
            Assert.AreEqual(12000d, CometGenerator.GetMaxResourceStacks(CometGrade.C));
            Assert.AreEqual(5000d, CometGenerator.GetMaxResourceStacks(CometGrade.D));
            Assert.AreEqual(2000d, CometGenerator.GetMaxResourceStacks(CometGrade.E));
            Assert.AreEqual(900d, CometGenerator.GetMaxResourceStacks(CometGrade.F));
        }

        [Test]
        public void ResourceNodeCount_SplitsLargeDepositsIntoMultipleNodes()
        {
            Assert.AreEqual(1, CometGenerator.GetResourceNodeCount(100d));
            Assert.AreEqual(2, CometGenerator.GetResourceNodeCount(500d));
            Assert.AreEqual(3, CometGenerator.GetResourceNodeCount(1500d));
            Assert.AreEqual(4, CometGenerator.GetResourceNodeCount(3500d));
            Assert.AreEqual(5, CometGenerator.GetResourceNodeCount(8000d));
            Assert.AreEqual(6, CometGenerator.GetResourceNodeCount(8001d));
        }

        [Test]
        public void ResourceDeposit_StoresStacksAndReportsWeightByResource()
        {
            ResourceDefinition resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            resource.ConfigurePrototype("steel", "Steel", ResourceCategory.Refined, 100d, false, 1, ResourceMarketTag.None, 2d);
            ResourceDeposit deposit = new ResourceDeposit(resource, 10d, 0, new Vector2Int(2, 3));

            double extractedTons = deposit.Extract(6d);

            Assert.AreEqual(10d, deposit.InitialStacks);
            Assert.AreEqual(20d, deposit.InitialTons);
            Assert.AreEqual(6d, extractedTons);
            Assert.AreEqual(7d, deposit.RemainingStacks);
            Assert.AreEqual(14d, deposit.RemainingTons);
            Assert.AreEqual(new Vector2Int(2, 3), deposit.Coordinate);
            Assert.IsTrue(deposit.HasCoordinate);
        }

        [Test]
        public void Generate_CreatesEightCharacterStructuredSeedCode()
        {
            CometGenerationProfile profile = ScriptableObject.CreateInstance<CometGenerationProfile>();
            profile.ConfigurePrototype("test", new Vector2(1f, 16f), new Vector2Int(0, 0), null);
            CometGenerator generator = new CometGenerator(1);

            CometState comet = generator.Generate(profile, 0, Vector2.zero);

            Assert.AreEqual(8, comet.SeedCode.Length);
            CollectionAssert.Contains(new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'S' }, comet.SeedCode[0]);
            CollectionAssert.Contains(new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'S' }, comet.SeedCode[1]);
            Assert.That(comet.SeedCode[2], Is.InRange('0', '3'));
            Assert.That(comet.SeedCode[6], Is.InRange('A', 'Z'));
            Assert.That(comet.SeedCode[7], Is.InRange('0', '9'));
            Assert.AreNotEqual("A0", comet.DistanceCode);
            Assert.AreEqual(CometGenerator.GetGridSize(comet.SizeGrade), comet.Grid.Width);
            Assert.AreEqual(comet.Grid.Width, comet.Grid.Height);
        }

        [Test]
        public void DistanceCode_UsesFiveMinuteNumbersAndOneHourLetters()
        {
            Assert.AreEqual(5, CometGenerator.GetDistanceMinutes('A', 1));
            Assert.AreEqual(45, CometGenerator.GetDistanceMinutes('A', 9));
            Assert.AreEqual(60, CometGenerator.GetDistanceMinutes('B', 0));
            Assert.AreEqual(65, CometGenerator.GetDistanceMinutes('B', 1));
            Assert.AreEqual(1500, CometGenerator.GetDistanceMinutes('Z', 0));
        }

        [Test]
        public void Generate_AppliesBuildableRatioToGrid()
        {
            CometGenerationProfile profile = ScriptableObject.CreateInstance<CometGenerationProfile>();
            profile.ConfigurePrototype("test", new Vector2(1f, 16f), new Vector2Int(0, 0), null);
            CometGenerator generator = new CometGenerator(2);

            CometState comet = generator.Generate(profile, 0, Vector2.zero);

            int buildableTiles = CountBuildableTiles(comet.Grid);
            int expectedBuildableTiles = Mathf.RoundToInt(comet.Grid.Width * comet.Grid.Height * comet.BuildableRatio);
            Assert.AreEqual(expectedBuildableTiles, buildableTiles);
        }

        [Test]
        public void Generate_AvoidsDistanceCodeAlreadyUsedByExistingComets()
        {
            CometGenerationProfile profile = ScriptableObject.CreateInstance<CometGenerationProfile>();
            profile.ConfigurePrototype("test", new Vector2(1f, 16f), new Vector2Int(0, 0), null);
            CometGenerator generator = new CometGenerator(5);
            List<CometState> existingComets = new List<CometState>();

            for (int i = 0; i < 30; i++)
            {
                CometState comet = generator.Generate(profile, i, Vector2.zero, existingComets);
                Assert.IsFalse(existingComets.Exists(existing => existing.DistanceCode == comet.DistanceCode));
                existingComets.Add(comet);
            }
        }

        private static int CountBuildableTiles(CometGrid grid)
        {
            int count = 0;
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (grid.GetTile(new Vector2Int(x, y)).IsBuildable)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
