using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CometFoundry.Simulation;
using UnityEngine;

namespace CometFoundry.Prototype
{
    public sealed partial class FoundryPlayableController
    {
        // Explicit validation fixture; never loads or writes a player save.
        private IEnumerator CaptureDroneReview(string directory)
        {
            resolutionReviewActive = true; speed = 0;
            showingTitle = false; tutorialChoice = false; tutorialGuide = false;
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(2);
            var rows = new List<string> { "providers\tseconds\tactive\tcapacity\tdelivered-stacks\tmaximum-production-queue" };
            foreach (int providers in new[] { 0, 1, 2 })
            {
                session.NewGame(); selected = null; page = FoundryPage.Board;
                foreach (var f in session.Comet.Facilities) f.ClearRecipe();
                foreach (var p in new[] { new Vector2Int(4,3), new Vector2Int(6,3), new Vector2Int(8,3), new Vector2Int(10,3), new Vector2Int(3,6), new Vector2Int(10,6) })
                {
                    if (!session.Build(session.Content.FindBuilding("refinery-basic"), p, out var error)) throw new System.InvalidOperationException(error);
                    session.FacilityAt(p).AssignRecipe(session.Content.FindRecipe("smelt-iron"));
                }
                if (providers == 0) session.Demolish(session.Comet.Facilities.First(f => f.Definition.IsDroneHangar), out _);
                if (providers == 2 && !session.Build(session.Content.FindBuilding("drone-hangar"), new Vector2Int(0,4), out var message)) throw new System.InvalidOperationException(message);
                session.Comet.Facilities.First(f => f.Definition.IsWarehouse).Inventory.AddStacks(session.Content.FindResource("iron-ore"), 120);
                for (int frame = 0; frame < 12; frame++)
                {
                    session.Advance(.25);
                    rows.Add($"{providers}\t{session.Seconds:0.00}\t{session.ActiveDroneCount}\t{session.DroneWorkerCapacity}\t{session.DeliveredDroneStacks}\t{session.Comet.Facilities.Where(f => f.Definition.SupportedRecipes.Count > 0).Max(f => session.DroneQueueCount(f))}");
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    CaptureFrame(Path.Combine(directory, $"providers-{providers}-frame-{frame:00}.png"));
                }
            }
            File.WriteAllLines(Path.Combine(directory, "metrics.tsv"), rows);
            Application.Quit();
        }
    }
}
