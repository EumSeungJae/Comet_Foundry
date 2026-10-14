using System.Linq;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed partial class FoundryProductionTests
    {
        [TestCase("refinery-basic", "smelt-iron", "iron-ore", 2, false)]
        [TestCase("refinery-basic", "smelt-iron", "iron-ore", 2, true)]
        [TestCase("factory-metal", "make-frame", "plate", 1, false)]
        public void DuplicateProducers_AllReceiveScarceInputsAfterPriorityAgeSaturates(string building, string recipeId, string resourceId, int initial, bool reload)
        {
            using (var session = new FoundrySession())
            {
                foreach (var mine in session.Comet.Facilities.Where(f => f.ActiveRecipe != null)) mine.ClearRecipe();
                var resource = session.Content.FindResource(resourceId);
                var recipe = session.Content.FindRecipe(recipeId);
                var storage = session.Comet.Facilities.First(f => f.Definition.IsWarehouse);
                var producers = new FacilityInstance[3];
                for (int i = 0; i < producers.Length; i++)
                {
                    var anchor = new Vector2Int(4 + i * 3, 3);
                    Assert.IsTrue(session.Build(session.Content.FindBuilding(building), anchor, out var error), error);
                    producers[i] = session.FacilityAt(anchor);
                    Assert.IsTrue(producers[i].AssignRecipe(recipe));
                    producers[i].Inventory.AddStacks(resource, initial);
                }
                Assert.IsFalse(session.Power.HasDeficit);
                if (reload)
                {
                    Assert.IsTrue(session.ImportState(session.ExportState()), "Existing production inventories must survive save/load.");
                    producers = producers.Select(p => session.FacilityAt(p.Anchor)).ToArray();
                    storage = session.Comet.Facilities.First(f => f.Definition.IsWarehouse);
                }
                // Supply is intermittent, as when several buildings compete for a slow upstream line.
                // Only the source receives test material; every producer must receive it via real drones.
                for (int release = 0; release < 9; release++)
                {
                    Assert.AreEqual(2, storage.Inventory.AddStacks(resource, 2));
                    session.Advance(16);
                }
                foreach (var producer in producers)
                    Assert.Greater(session.ProducedBy(producer, recipeId), 0,
                        producer.Id + " starved at " + producer.Inventory.GetAmount(resource) + " input stacks: " + session.Status(producer));
                double batches = producers.Sum(p => session.ProducedBy(p, recipeId)) / recipe.Outputs[0].Stacks;
                Assert.AreEqual(initial * producers.Length + 18 - batches * recipe.Inputs[0].Stacks,
                    session.TotalStock(resource), 1e-8, "Fair scheduling must preserve material quantities.");
            }
        }

        [Test]
        public void DuplicateMines_BothResumeWhenStorageSpaceOnlyOpensIntermittently()
        {
            using (var session = new FoundrySession())
            {
                var ore = session.Content.FindResource("iron-ore");
                var copperMine = session.Comet.Facilities.First(f => f.ActiveRecipe?.RecipeId == "extract-copper");
                copperMine.ClearRecipe();
                var first = session.Comet.Facilities.First(f => f.ActiveRecipe?.RecipeId == "extract-iron");
                Assert.IsTrue(session.Build(session.Content.FindBuilding("mine-small"), new Vector2Int(11, 9), out var error), error);
                var second = session.FacilityAt(new Vector2Int(11, 9));
                first.Inventory.AddStacks(ore, first.Inventory.CapacityStacks);
                second.Inventory.AddStacks(ore, second.Inventory.CapacityStacks);
                var storage = session.Comet.Facilities.First(f => f.Definition.IsWarehouse);
                storage.Inventory.AddStacks(session.Content.FindResource("water"), storage.Inventory.CapacityStacks - 2);
                for (int release = 0; release < 10; release++)
                {
                    storage.Inventory.RemoveStacks(ore, 2);
                    session.Advance(16);
                }
                Assert.Greater(session.ProducedBy(first, "extract-iron"), 0);
                Assert.Greater(session.ProducedBy(second, "extract-iron"), 0,
                    "The later-built mine must also drain output and resume, rather than stay full forever.");
            }
        }
    }
}
