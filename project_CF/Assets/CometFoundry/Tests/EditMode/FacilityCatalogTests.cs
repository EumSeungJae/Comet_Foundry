using System.Linq;
using CometFoundry.Data;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class FacilityCatalogTests
    {
        [Test]
        public void PrototypeDefinitions_IncludeApprovedBuildingSizes()
        {
            var definitions = FacilityCatalog.CreatePrototypeDefinitions();

            AssertSize(definitions, "mine-small", 1, 1);
            AssertSize(definitions, "mine-large", 1, 2);
            AssertSize(definitions, "mine-deep", 3, 3);
            AssertSize(definitions, "refinery-basic", 1, 2);
            AssertSize(definitions, "refinery-chemical", 1, 3);
            AssertSize(definitions, "refinery-cryogenic", 2, 3);
            AssertSize(definitions, "factory-metal", 2, 2);
            AssertSize(definitions, "factory-electronics", 2, 3);
            AssertSize(definitions, "factory-precision", 2, 4);
            AssertSize(definitions, "assembly-basic", 2, 2);
            AssertSize(definitions, "assembly-advanced", 2, 3);
            AssertSize(definitions, "assembly-heavy", 3, 3);
            AssertSize(definitions, "warehouse-small", 1, 2);
            AssertSize(definitions, "warehouse-medium", 2, 2);
            AssertSize(definitions, "warehouse-large", 2, 4);
            AssertSize(definitions, "tank-small", 2, 2);
            AssertSize(definitions, "tank-medium", 2, 3);
            AssertSize(definitions, "tank-large", 2, 5);
            AssertSize(definitions, "port-small", 2, 3);
            AssertSize(definitions, "port-docking-hub", 2, 5);
            AssertSize(definitions, "port-large-terminal", 3, 6);
            AssertSize(definitions, "market-terminal", 1, 2);
            AssertSize(definitions, "market-branch", 2, 2);
            AssertSize(definitions, "market-corporate-center", 2, 3);
            AssertSize(definitions, "lab-basic", 1, 2);
            AssertSize(definitions, "lab-industrial", 2, 2);
            AssertSize(definitions, "lab-space-engineering", 2, 4);
            AssertSize(definitions, "power-plant-small", 1, 1);
            AssertSize(definitions, "power-plant-medium", 1, 3);
            AssertSize(definitions, "power-plant-large", 2, 3);
            AssertSize(definitions, "drone-hangar", 2, 2);
        }

        private static void AssertSize(System.Collections.Generic.IReadOnlyList<FacilityDefinition> definitions, string id, int width, int height)
        {
            FacilityDefinition definition = definitions.FirstOrDefault(item => item.FacilityId == id);
            Assert.IsNotNull(definition, id);
            Assert.AreEqual(new Vector2Int(width, height), definition.FootprintSize);
        }
    }
}
