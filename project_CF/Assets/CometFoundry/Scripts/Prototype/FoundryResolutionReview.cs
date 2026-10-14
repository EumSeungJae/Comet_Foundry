using System;
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
        // Opt-in fixture only. It never reads or writes the player's save.
        private bool resolutionReviewActive;
        private string resolutionReviewScene;
        private readonly HashSet<string> resolutionTextWarnings = new HashSet<string>();
        private readonly List<string> resolutionReviewRows = new List<string>();

        private void ReviewTextFit(Rect rect, string value, GUIStyle style)
        {
            if (string.IsNullOrEmpty(value) || style == null) return;
            float needed = style.CalcHeight(new GUIContent(value), rect.width);
            if (needed <= rect.height + 2) return;
            resolutionTextWarnings.Add($"{resolutionReviewScene}\t{needed:0.##}/{rect.height:0.##}\t{value.Replace('\n', ' ')}");
        }

        private IEnumerator ReviewResolutionFrame(string directory, string name)
        {
            resolutionReviewScene = name;
            yield return null;
            yield return new WaitForEndOfFrame();
            CaptureFrame(Path.Combine(directory, name + ".png"));
            float scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
            Vector2 offset = new Vector2((Screen.width - 1440 * scale) / 2, (Screen.height - 900 * scale) / 2);
            string geometry = "-";
            if (tutorialGuide)
            {
                var target = GuideTarget(); var popup = GuidePopupRect(target);
                var screenTarget = new Rect(offset + target.position * scale, target.size * scale);
                var screenPopup = new Rect(offset + popup.position * scale, popup.size * scale);
                bool inScreen = screenTarget.xMin >= 0 && screenTarget.yMin >= 0 && screenTarget.xMax <= Screen.width + .01f && screenTarget.yMax <= Screen.height + .01f
                    && screenPopup.xMin >= 0 && screenPopup.yMin >= 0 && screenPopup.xMax <= Screen.width + .01f && screenPopup.yMax <= Screen.height + .01f;
                bool clear = !popup.Overlaps(new Rect(target.x - 4, target.y - 4, target.width + 8, target.height + 8));
                geometry = $"{(inScreen && clear ? "PASS" : "FAIL")} target={screenTarget} popup={screenPopup}";
                if (!inScreen || !clear) Debug.LogError("Resolution guide geometry failed: " + name + " " + geometry);
            }
            resolutionReviewRows.Add($"{name}\t{Screen.width}x{Screen.height}\t{scale:0.###}\t{small.fontSize * scale:0.##}\t{geometry}");
        }

        private FacilityInstance ReviewBuild(string id)
        {
            var existing = session.Comet.Facilities.FirstOrDefault(f => f.Definition.FacilityId == id);
            if (existing != null) return existing;
            var definition = session.Content.FindBuilding(id);
            for (int y = 0; y < session.Comet.Grid.Height; y++) for (int x = 0; x < session.Comet.Grid.Width; x++)
            {
                var anchor = new Vector2Int(x, y);
                if (session.PlacementError(definition, anchor) != null) continue;
                if (session.Build(definition, anchor, out _)) return session.FacilityAt(anchor);
            }
            throw new InvalidOperationException("Review fixture cannot place " + id);
        }

        private IEnumerator CaptureResolutionReview(string directory)
        {
            resolutionReviewActive = true; speed = 0;
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(2);
            yield return ReviewResolutionFrame(directory, "01-title");
            showingTitle = false; tutorialChoice = true;
            yield return ReviewResolutionFrame(directory, "02-tutorial-choice");
            tutorialChoice = false;
            session.Company.Earn(100000);
            var refinery = ReviewBuild("refinery-basic");
            refinery.AssignRecipe(session.Content.FindRecipe("smelt-iron"));
            ReviewBuild("market-terminal");
            yield return ReviewResolutionFrame(directory, "03-board");
            selected = refinery; preview = refinery.ActiveRecipe;
            yield return ReviewResolutionFrame(directory, "04-production");
            selected = null; page = FoundryPage.Exchange;
            yield return ReviewResolutionFrame(directory, "05-exchange-6months");
            marketMonths = 24;
            yield return ReviewResolutionFrame(directory, "06-exchange-24months");
            page = FoundryPage.Board; missionList = true;
            yield return ReviewResolutionFrame(directory, "07-missions-top");
            missionScroll.y = 100000;
            yield return ReviewResolutionFrame(directory, "08-missions-bottom");
            missionList = false; session.AcceptMission(session.Missions.Last().Id, out _);
            yield return ReviewResolutionFrame(directory, "09-mission-five-materials");
            foreach (int number in new[] { 10, 11 })
            {
                session.AbandonMission();
                session.AcceptMission(session.Missions.First(m => m.Number == number).Id, out _);
                yield return ReviewResolutionFrame(directory, $"09-mission-reward-{number}");
            }
            session.AbandonMission(); page = FoundryPage.Management;
            session.Explore(out _); session.Advance(8.1);
            yield return ReviewResolutionFrame(directory, "10-exploration");
            session.Colonize(0, out _); fleet = true;
            yield return ReviewResolutionFrame(directory, "11-fleet");
            demolitionTarget = refinery;
            yield return ReviewResolutionFrame(directory, "12-demolition");
            demolitionTarget = null; page = FoundryPage.Board;
            StartTutorialGuide();
            guideDestinationColony = 1;
            var reviewedChapters = new HashSet<int>();
            // These are layout fixtures, not proof that tutorial objectives were played.
            for (int i = 0; i < guidedSteps.Count; i++)
            {
                var step = guidedSteps[i];
                bool firstInChapter = reviewedChapters.Add(step.Chapter);
                if (!firstInChapter && step.Action != GuideAction.SelectBuilding && step.Action != GuideAction.PlaceBuilding &&
                    step.Action != GuideAction.SelectRecipe && step.Action != GuideAction.ConfirmRecipe &&
                    step.Action != GuideAction.SelectSource && step.Action != GuideAction.SelectDestination &&
                    step.Action != GuideAction.SelectCargo && step.Action != GuideAction.DispatchCargo &&
                    step.Action != GuideAction.WaitCargo && step.Action != GuideAction.OpenCargoDock &&
                    step.Target != "prices" && step.Target != "mission-row" && step.Target != "balance" &&
                    step.Target != "exploration" && step.Target != "fleet" && step.Target != "cargo-stock" && step.Target != "colony-board") continue;
                if (step.Building != null && step.Action != GuideAction.SelectBuilding && step.Action != GuideAction.PlaceBuilding)
                    ReviewBuild(step.Building);
                guidePage = i; EnterGuideStep();
                if (step.Action == GuideAction.OpenCargoDock || step.Target == "cargo-stock")
                {
                    session.SwitchColony(guideDestinationColony);
                    var dock = session.Dock(session.ActiveColony);
                    if (step.Target == "cargo-stock") { selected = dock; dock.Inventory.AddStacks(session.Content.FindResource("water"), 3); }
                }
                if (step.Target == "mission-row") { missionList = true; missionScroll = Vector2.zero; }
                yield return ReviewResolutionFrame(directory, $"guide-{i + 1:00}-{step.Action}");
                selected = null; missionList = false;
            }
            tutorialGuide = false;
            File.WriteAllLines(Path.Combine(directory, "layout.tsv"), new[] { "scene\tactual-resolution\tscale\tsmall-text-pixels\tguide-geometry" }.Concat(resolutionReviewRows));
            File.WriteAllLines(Path.Combine(directory, "text-fit-warnings.tsv"), resolutionTextWarnings.OrderBy(s => s));
            Debug.Log($"Resolution review complete: {Screen.width}x{Screen.height}, {resolutionReviewRows.Count} frames, {resolutionTextWarnings.Count} text fit warnings.");
            Application.Quit();
        }
    }
}
