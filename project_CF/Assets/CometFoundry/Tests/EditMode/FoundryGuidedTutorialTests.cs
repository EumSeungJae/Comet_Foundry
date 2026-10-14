using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CometFoundry.Data;
using CometFoundry.Prototype;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class FoundryGuidedTutorialTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Get(object owner, string name) => owner.GetType().GetField(name, Private | BindingFlags.Public)?.GetValue(owner)
            ?? owner.GetType().GetProperty(name, Private | BindingFlags.Public)?.GetValue(owner);
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
        private static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Private).Invoke(owner, args);

        [Test]
        public void WalkthroughCoversFourteenChaptersAndCompletesRealObjectives()
        {
            var cameraBefore = Camera.main;
            var go = new GameObject("Guided tutorial test");
            try
            {
                var controller = go.AddComponent<FoundryPlayableController>();
                // EditMode does not automatically invoke Awake.
                if (Get(controller, "session") == null) Call(controller, "Awake");
                var session = (FoundrySession)Get(controller, "session");
                Call(controller, "StartTutorialGuide");
                var steps = (IList)Get(controller, "guidedSteps");
                CollectionAssert.AreEqual(Enumerable.Range(1, 14).ToArray(), steps.Cast<object>().Select(s => (int)Get(s, "Chapter")).Distinct().ToArray());
                int iterations = 0;
                while ((bool)Get(controller, "tutorialGuide"))
                {
                    Assert.Less(iterations++, 100, "Guide must finish without looping.");
                    var step = Get(controller, "CurrentGuide");
                    string action = Get(step, "Action").ToString();
                    if (action != "Read")
                        Assert.IsFalse((bool)Get(controller, "TutorialPausesTime"), "This action must not require a tutorial acknowledgement: " + action);
                    string building = Get(step, "Building") as string, recipeId = Get(step, "Recipe") as string;
                    var target = (Rect)Call(controller, "GuideTarget");
                    var popup = (Rect)Call(controller, "GuidePopupRect", target);
                    Assert.IsFalse(popup.Overlaps(target), "Popup must not hide target: " + Get(step, "Title"));
                    Assert.That(popup.xMin, Is.GreaterThanOrEqualTo(0)); Assert.That(popup.xMax, Is.LessThanOrEqualTo(1440));
                    Assert.That(target.yMin, Is.GreaterThanOrEqualTo(0)); Assert.That(target.yMax, Is.LessThanOrEqualTo(900));
                    var bodyStyle = (GUIStyle)Call(controller, "Style", 15, Color.white, FontStyle.Normal);
                    Assert.LessOrEqual(bodyStyle.CalcHeight(new GUIContent((string)Get(step, "Body")), popup.width - 36), popup.height - 165,
                        "Instruction text must fit: " + Get(step, "Title"));
                    switch (action)
                    {
                        case "Read": Call(controller, "NextGuideStep"); continue;
                        case "SelectBuilding": Set(controller, "build", session.Content.FindBuilding(building)); break;
                        case "PlaceBuilding":
                            var anchor = (Vector2Int?)Call(controller, "GuidePlacement", step);
                            Assert.IsTrue(anchor.HasValue, "A usable placement must exist: " + building);
                            Assert.IsTrue(session.Build(session.Content.FindBuilding(building), anchor.Value, out var error), error); break;
                        case "OpenFacility": Set(controller, "selected", Get(controller, "guideFacility")); break;
                        case "SelectRecipe": Set(controller, "preview", session.Content.FindRecipe(recipeId)); break;
                        case "ConfirmRecipe":
                            Assert.IsTrue(((FacilityInstance)Get(controller, "guideFacility")).AssignRecipe(session.Content.FindRecipe(recipeId)));
                            Set(controller, "selected", null); break;
                        case "Produce":
                            var facility = (FacilityInstance)Get(controller, "guideFacility");
                            var recipe = session.Content.FindRecipe(recipeId);
                            // Supply exact inputs so this checks real production independently of drone timing.
                            foreach (var input in recipe.Inputs) facility.Inventory.AddStacks(input.Resource, input.Stacks * 3);
                            Assert.IsFalse((bool)Call(controller, "GuideObjectiveMet"), "Stored inputs must not count as production.");
                            session.Advance(recipe.DurationDays * 300 * 3 + .2); break;
                        case "OpenExchange": Set(controller, "page", FoundryPage.Exchange); break;
                        case "Buy": Assert.IsTrue(session.Trade(session.Content.Resources[0], true, 1, out _)); break;
                        case "Sell": Assert.IsTrue(session.Trade(session.Content.Resources[0], false, 1, out _)); break;
                        case "OpenMissions": Set(controller, "missionList", true); break;
                        case "AcceptMission": Assert.IsTrue(session.AcceptMission((string)Get(controller, "guideMissionId"), out _)); break;
                        case "MissionProduce":
                            foreach (var demand in session.ActiveMission.Requirements)
                                session.Comet.Facilities.First(f => f.Definition.IsWarehouse).Inventory.AddStacks(session.Content.FindResource(demand.ResourceId), demand.Amount);
                            break;
                        case "DeliverMission": Assert.IsTrue(session.DeliverMission(out _)); break;
                        case "OpenManagement": Set(controller, "page", FoundryPage.Management); break;
                        case "Survey": Assert.IsTrue(session.Explore(out _)); break;
                        case "WaitSurvey": session.Advance(8.1); break;
                        case "SelectProspect": Set(controller, "prospectIndex", 0); break;
                        case "PurchaseColony": Assert.IsTrue(session.Colonize((int)Get(controller, "prospectIndex"), out _)); break;
                        case "SwitchDestination": Assert.IsTrue(session.SwitchColony((int)Get(controller, "guideDestinationColony"))); break;
                        case "SwitchOrigin": Assert.IsTrue(session.SwitchColony((int)Get(controller, "guideOriginColony"))); break;
                        case "PrepareCargo": Assert.IsTrue(session.Trade(session.Content.FindResource("water"), true, 3, out _)); break;
                        case "OpenFleet": Set(controller, "fleet", true); break;
                        case "SelectSource": Set(controller, "shipFrom", Get(controller, "guideOriginColony")); break;
                        case "SelectDestination": Set(controller, "shipTo", Get(controller, "guideDestinationColony")); break;
                        case "SelectCargo": Set(controller, "cargoIndex", session.Content.Resources.FindIndex(r => r.ResourceId == "water")); Set(controller, "cargoQuantity", 3); break;
                        case "DispatchCargo":
                            Assert.IsTrue(session.Dispatch(0, (int)Get(controller, "shipFrom"), (int)Get(controller, "shipTo"), session.Content.FindResource("water"), 3, out var shippingError), shippingError); break;
                        case "WaitCargo":
                            Assert.IsFalse((bool)Call(controller, "GuideObjectiveMet"), "Departure must not count as delivered cargo.");
                            var voyage = (FoundryVoyage)Get(controller, "guideVoyage");
                            var dock = session.Dock(voyage.Destination); double capacity = dock.Inventory.CapacityStacks;
                            dock.Inventory.SetCapacity(0); session.Advance(voyage.Duration + 1);
                            Assert.IsFalse((bool)Call(controller, "GuideObjectiveMet"), "Arrival with a full dock must not count as delivered cargo.");
                            dock.Inventory.SetCapacity(capacity); session.Advance(.2); break;
                        case "OpenCargoDock": Set(controller, "selected", session.Dock(session.ActiveColony)); break;
                        default: Assert.Fail("Unhandled tutorial action: " + action); break;
                    }
                    Assert.IsTrue((bool)Call(controller, "GuideObjectiveMet"), "Objective should complete: " + Get(step, "Title"));
                    Call(controller, "CheckGuidedTutorial");
                }
                Assert.IsTrue(session.MissionCompleted(FoundrySession.PracticeMissionId));
                Assert.Greater(session.TotalStock(session.Content.FindResource("electronic")), 0);
                Assert.AreEqual(3, session.Dock(session.Colonies[(int)Get(controller, "guideDestinationColony")]).Inventory.GetAmount(session.Content.FindResource("water")));
                Assert.AreEqual(0, session.Voyages.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                if (cameraBefore == null && Camera.main != null) UnityEngine.Object.DestroyImmediate(Camera.main.gameObject);
            }
        }

        [Test]
        public void ConstructionStartsImmediatelyAndAdvancesOnlyAfterSuccessfulPlacement()
        {
            var cameraBefore = Camera.main;
            var go = new GameObject("Tutorial automatic construction test");
            try
            {
                var controller = go.AddComponent<FoundryPlayableController>();
                if (Get(controller, "session") == null) Call(controller, "Awake");
                var session = (FoundrySession)Get(controller, "session");
                Call(controller, "StartTutorialGuide");
                Assert.IsFalse((bool)Get(controller, "TutorialPausesTime"));
                Set(controller, "build", session.Content.FindBuilding("mine-small"));
                Call(controller, "CheckGuidedTutorial");
                Assert.AreEqual("PlaceBuilding", Get(Get(controller, "CurrentGuide"), "Action").ToString());
                Assert.IsFalse((bool)Get(controller, "TutorialPausesTime"));
                Assert.IsNotNull(Get(controller, "build"), "The selected building must stay ready to place.");
                int page = (int)Get(controller, "guidePage");
                Assert.IsFalse(session.Build(session.Content.FindBuilding("mine-small"), Vector2Int.zero, out _));
                Call(controller, "CheckGuidedTutorial"); Assert.AreEqual(page, Get(controller, "guidePage"));
                int previousCount = session.Comet.Facilities.Count;
                Assert.IsTrue(session.Demolish(session.Comet.Facilities.First(f => f.Definition.FacilityId == "mine-small"), out _));
                var anchor = (Vector2Int?)Call(controller, "GuidePlacement", Get(controller, "CurrentGuide"));
                Assert.IsTrue(session.Build(session.Content.FindBuilding("mine-small"), anchor.Value, out _));
                Assert.AreEqual(previousCount, session.Comet.Facilities.Count, "Replacing a building must work even without increasing the facility count.");
                var placed = session.FacilityAt(anchor.Value); var occupiedTile = session.Comet.Grid.GetTile(anchor.Value);
                occupiedTile.ClearOccupant();
                Assert.IsFalse((bool)Call(controller, "CheckGuidedConstruction"), "A facility list entry without its occupied board tile must not count as built.");
                occupiedTile.Occupy(CometTileOccupant.Facility, placed.Id);
                Assert.IsTrue((bool)Call(controller, "CheckGuidedConstruction"));
                Assert.AreEqual("OpenFacility", Get(Get(controller, "CurrentGuide"), "Action").ToString());
                Assert.IsFalse((bool)Get(controller, "TutorialPausesTime"), "No post-construction acknowledgement should be required.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                if (cameraBefore == null && Camera.main != null) UnityEngine.Object.DestroyImmediate(Camera.main.gameObject);
            }
        }

        [Test]
        public void PurchasingOutputsDoesNotSatisfyProductionAndRestartClearsObservations()
        {
            using (var session = new FoundrySession())
            {
                Assert.IsTrue(session.Build(session.Content.FindBuilding("factory-metal"), new Vector2Int(8, 3), out _));
                Assert.IsTrue(session.Build(session.Content.FindBuilding("market-terminal"), new Vector2Int(12, 7), out _));
                var facility = session.FacilityAt(new Vector2Int(8, 3));
                facility.AssignRecipe(session.Content.FindRecipe("roll-plate"));
                Assert.IsTrue(session.Trade(session.Content.FindResource("plate"), true, 4, out _));
                Assert.AreEqual(0, session.ProducedBy(facility, "roll-plate"));
                facility.Inventory.AddStacks(session.Content.FindResource("iron"), 2);
                session.Advance(6.1);
                Assert.AreEqual(2, session.ProducedBy(facility, "roll-plate"));
                session.NewGame(); Assert.AreEqual(0, session.ProducedBy(facility, "roll-plate"));
            }
        }

        [Test]
        public void StarterEconomySuppliesTheEntireGuidedProductionChainThroughDrones()
        {
            var cameraBefore = Camera.main;
            var go = new GameObject("Tutorial drone chain test");
            try
            {
                var controller = go.AddComponent<FoundryPlayableController>();
                if (Get(controller, "session") == null) Call(controller, "Awake");
                var session = (FoundrySession)Get(controller, "session");
                Call(controller, "StartTutorialGuide");
                var steps = (IList)Get(controller, "guidedSteps");
                foreach (var step in steps.Cast<object>().Where(s => Get(s, "Action").ToString() == "Produce"))
                {
                    string building = (string)Get(step, "Building"), recipeId = (string)Get(step, "Recipe");
                    var facility = session.Comet.Facilities.FirstOrDefault(f => f.Definition.FacilityId == building);
                    if (facility == null || building == "mine-small")
                    {
                        var anchor = (Vector2Int?)Call(controller, "GuidePlacement", step);
                        Assert.IsTrue(anchor.HasValue, building);
                        Assert.IsTrue(session.Build(session.Content.FindBuilding(building), anchor.Value, out var error), error);
                        facility = session.FacilityAt(anchor.Value);
                    }
                    Assert.IsTrue(facility.AssignRecipe(session.Content.FindRecipe(recipeId)));
                    double before = session.ProducedBy(facility, recipeId);
                    double required = recipeId == "draw-wire" ? 9 : recipeId == "make-circuit" ? 2 : session.Content.FindRecipe(recipeId).Outputs[0].Stacks;
                    int ticks = 0;
                    while (session.ProducedBy(facility, recipeId) - before < required - 1e-8 && ticks++ < 1800) session.Advance(.1);
                    Assert.GreaterOrEqual(session.ProducedBy(facility, recipeId) - before + 1e-8, required,
                        recipeId + " stalled: " + session.Status(facility) + " / " + session.DroneStatus);
                }
                Assert.Greater(session.TotalStock(session.Content.FindResource("electronic")), 0);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                if (cameraBefore == null && Camera.main != null) UnityEngine.Object.DestroyImmediate(Camera.main.gameObject);
            }
        }
    }
}
