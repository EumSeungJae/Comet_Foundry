using CometFoundry.Core;
using CometFoundry.Data;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class InventoryTests
    {
        [Test]
        public void Add_ClampsToCapacity()
        {
            Inventory inventory = new Inventory(10d);
            ResourceDefinition resource = ScriptableObject.CreateInstance<ResourceDefinition>();

            double accepted = inventory.Add(resource, 25d);
            Assert.AreEqual(10d, accepted);
            Assert.AreEqual(10d, inventory.UsedTons);
            Assert.AreEqual(0d, inventory.FreeTons);
        }

        [Test]
        public void AddStacks_ClampsToStackCapacity()
        {
            Inventory inventory = new Inventory(10d);
            ResourceDefinition resource = ScriptableObject.CreateInstance<ResourceDefinition>();

            double accepted = inventory.AddStacks(resource, 25d);

            Assert.AreEqual(10d, accepted);
            Assert.AreEqual(10d, inventory.UsedStacks);
            Assert.AreEqual(0d, inventory.FreeStacks);
            Assert.IsTrue(inventory.HasStacks(resource, 10d));
        }

        [Test]
        public void ResourceDefinition_ConvertsStacksAndWeight()
        {
            ResourceDefinition resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            resource.ConfigurePrototype("iron-ore", "Iron Ore", ResourceCategory.Raw, 100d, true, 0, ResourceMarketTag.None, 1.5d);

            Assert.AreEqual(1.5d, resource.StackWeightTons);
            Assert.AreEqual(7.5d, resource.GetWeightForStacks(5d));
            Assert.AreEqual(5d, resource.GetStacksForWeight(7.5d));
        }
    }
}
