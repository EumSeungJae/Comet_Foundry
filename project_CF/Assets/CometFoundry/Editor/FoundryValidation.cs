using System;
using System.IO;
using System.Linq;
using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Prototype;
using CometFoundry.Simulation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Validation can run inside the already-open Unity editor, without opening a second project instance.
[InitializeOnLoad]
public static class FoundryValidation
{
    private const string Request = "Temp/FoundryValidation.request";
    private const string PreviewRequest = "Temp/FoundryPreviewBuild.request";
    private const string RevisionRequest = "Temp/FoundryRevisionValidation.request";
    private const string MonthlyRequest = "Temp/FoundryMonthlyValidation.request";
    private const string TutorialTestRequest = "Temp/FoundryTutorialTests.request";
    private const string TutorialBuildRequest = "Temp/FoundryTutorialBuild.request";
    private static bool tutorialTestsRunning;
    static FoundryValidation() { EditorApplication.update += CheckRequest; }
    [MenuItem("Comet Foundry/Open Playable Scene")]
    public static void OpenPlayable()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene("Assets/CometFoundry/Scenes/FoundryPlayableScene.unity");
    }
    [MenuItem("Comet Foundry/Build Windows Playable")]
    public static void BuildPlayable()
    {
        var output = Environment.GetEnvironmentVariable("COMET_FOUNDRY_BUILD_OUTPUT")
            ?? Path.GetFullPath("Builds/CometFoundry/CometFoundry.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        string originalProduct = PlayerSettings.productName;
        try
        {
            PlayerSettings.productName = "Comet Foundry";
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/CometFoundry/Scenes/FoundryPlayableScene.unity" },
                locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + result.summary.result);
        }
        finally { PlayerSettings.productName = originalProduct; }
        Debug.Log("Comet Foundry Windows build succeeded: " + output);
    }
    private static void CheckRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || tutorialTestsRunning) return;
        if (File.Exists(TutorialTestRequest)) { File.Delete(TutorialTestRequest); RunTutorialTests(); return; }
        if (File.Exists(TutorialBuildRequest))
        {
            File.Delete(TutorialBuildRequest);
            try { BuildPlayable(); File.WriteAllText("ValidationArtifacts/GuidedTutorial-expansion-build.txt", "PASS"); }
            catch (Exception exception) { File.WriteAllText("ValidationArtifacts/GuidedTutorial-expansion-build.txt", "FAIL: " + exception); Debug.LogException(exception); }
            return;
        }
        if (File.Exists(PreviewRequest))
        {
            File.Delete(PreviewRequest);
            string original = Environment.GetEnvironmentVariable("COMET_FOUNDRY_BUILD_OUTPUT");
            try
            {
                Environment.SetEnvironmentVariable("COMET_FOUNDRY_BUILD_OUTPUT", Path.GetFullPath("Builds/ValidationPreview/CometFoundry.exe"));
                BuildPlayable(); File.WriteAllText("ValidationArtifacts/preview-build.txt", "PASS");
            }
            catch (Exception exception) { File.WriteAllText("ValidationArtifacts/preview-build.txt", "FAIL: " + exception); Debug.LogException(exception); }
            finally { Environment.SetEnvironmentVariable("COMET_FOUNDRY_BUILD_OUTPUT", original); }
            return;
        }
        if (File.Exists(RevisionRequest)) { File.Delete(RevisionRequest); RunRevision(); return; }
        if (File.Exists(MonthlyRequest)) { File.Delete(MonthlyRequest); RunMonthly(); return; }
        if (!File.Exists(Request)) return;
        File.Delete(Request); Run();
    }
    [MenuItem("Comet Foundry/Validate Guided Tutorial")]
    public static void RunTutorialTests()
    {
        Directory.CreateDirectory("ValidationArtifacts");
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var callbacks = new TutorialTestCallbacks(api);
        api.RegisterCallbacks(callbacks);
        tutorialTestsRunning = true;
        try
        {
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, groupNames = new[] {
                "CometFoundry.Tests.FoundryGuidedTutorialTests", "CometFoundry.Tests.FoundryRevisionTests",
                "CometFoundry.Tests.FoundryProductionTests", "CometFoundry.Tests.FoundryManagementTests", "CometFoundry.Tests.FoundryMissionCatalogTests" } }));
        }
        catch { tutorialTestsRunning = false; api.UnregisterCallbacks(callbacks); UnityEngine.Object.DestroyImmediate(api); throw; }
    }
    private sealed class TutorialTestCallbacks : ICallbacks
    {
        private readonly TestRunnerApi api;
        public TutorialTestCallbacks(TestRunnerApi runner) { api = runner; }
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            try { TestRunnerApi.SaveResultToFile(result, "ValidationArtifacts/GuidedTutorial-expansion-tests.xml"); }
            finally { tutorialTestsRunning = false; api.UnregisterCallbacks(this); UnityEngine.Object.DestroyImmediate(api); }
        }
    }
    [MenuItem("Comet Foundry/Validate Playable Game")]
    public static void Run()
    {
        Directory.CreateDirectory("ValidationArtifacts");
        try
        {
            Require(Resources.Load<Font>("Fonts/NEXON_Maplestory/TTF/Maplestory Light") != null, "bundled Maplestory Light");
            Require(Resources.Load<Font>("Fonts/NEXON_Maplestory/TTF/Maplestory Bold") != null, "bundled Maplestory Bold");
            Require(Resources.Load<Texture2D>("Title/CometFoundryCover") != null, "title cover texture");
            using (var session = new FoundrySession())
            {
                Require(!session.Power.HasDeficit, "starter power");
                Require(session.Content.Recipes.Count == 16 && session.Content.Resources.Count == 16, "content count");
                session.Advance(180);
                Require(session.Comet.Facilities.Count == 5 && session.DroneWorkerCapacity == 4 && session.Sales == 0, "explicit starter drone provider");
                Require(session.Comet.Deposits[0].RemainingStacks < 2400, "mining depletes deposit");
                Require(session.TotalStock(session.Content.FindResource("copper-ore")) > 0, "starter copper mining");
                // Safe completion when output storage grows (ice -> water increases stack count).
                var facility = new FacilityInstance(new SimulationId("validation"), session.Content.FindBuilding("refinery-basic"));
                facility.AssignRecipe(session.Content.FindRecipe("melt-ice")); facility.Inventory.SetCapacity(2);
                facility.Inventory.AddStacks(session.Content.FindResource("ice"), 2);
                var company = new CompanyState(100);
                Require(!facility.TickProduction(10d / 300, company), "full output blocks completion");
                Require(company.Credits == 100 && facility.Inventory.GetAmount(session.Content.FindResource("ice")) == 2, "blocked batch preserves costs");
                facility.Inventory.SetCapacity(3);
                Require(facility.TickProduction(4d / 300, company) && facility.Inventory.GetAmount(session.Content.FindResource("water")) == 3, "production resumes safely");
                var refinery = new FacilityInstance(new SimulationId("time-test"), session.Content.FindBuilding("refinery-basic"));
                refinery.AssignRecipe(session.Content.FindRecipe("smelt-iron")); refinery.Inventory.AddStacks(session.Content.FindResource("iron-ore"), 9);
                Require(refinery.TickProduction(12d / 300, company) && refinery.Inventory.GetAmount(session.Content.FindResource("iron")) == 4, "multiple batches and remainder");
                double progress = refinery.ProgressDays;
                refinery.AssignRecipe(session.Content.FindRecipe("smelt-iron")); Require(Math.Abs(refinery.ProgressDays - progress) < 1e-9, "reselection retains progress");
                refinery.AssignRecipe(session.Content.FindRecipe("smelt-copper")); Require(refinery.ProgressDays == 0, "recipe change resets progress");
                var mine = session.Content.FindBuilding("mine-small");
                Require(!session.Build(mine, new Vector2Int(4, 4), out _), "mine requires node");
                Require(session.Build(mine, new Vector2Int(3, 9), out _), "construction on ice node");
                Require(session.FacilityAt(new Vector2Int(3, 9)).ActiveRecipe.RecipeId == "extract-ice", "mine chooses node resource");
                string previous = PlayerPrefs.GetString(FoundrySession.SaveKey, ""); bool hadSave = PlayerPrefs.HasKey(FoundrySession.SaveKey);
                try
                {
                    double credits = session.Company.Credits, sales = session.Sales;
                    int count = session.Comet.Facilities.Count;
                    session.Save(); session.NewGame(); Require(session.Load(), "save loads");
                    Require(session.Company.Credits == credits && session.Sales == sales && session.Comet.Facilities.Count == count, "save roundtrip");
                }
                finally
                {
                    if (hadSave) PlayerPrefs.SetString(FoundrySession.SaveKey, previous); else PlayerPrefs.DeleteKey(FoundrySession.SaveKey);
                    PlayerPrefs.Save();
                }
                var art = new FoundryBuildingArt();
                var chart = FoundryChartRaster.Create(session.PriceHistory(session.Content.Resources[0], 6), 598, 308,
                    session.Content.Resources[0].BaseMarketPrice * .55, session.Content.Resources[0].BaseMarketPrice * 1.45,
                    session.Content.Resources[0].BaseMarketPrice, Color.white);
                File.WriteAllBytes("ValidationArtifacts/exchange-chart.png", chart.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(chart);
                try
                {
                    Directory.CreateDirectory("Assets/CometFoundry/Art/Playable");
                    foreach (var definition in session.Content.Buildings)
                    {
                        Texture2D texture = art.Get(definition.FacilityType);
                        Require(texture.width == 128, "building sprite");
                        File.WriteAllBytes("Assets/CometFoundry/Art/Playable/" + definition.FacilityId + ".png", texture.EncodeToPNG());
                    }
                }
                finally { art.Dispose(); }
                File.WriteAllText("ValidationArtifacts/result.txt", "PASS: bundled Maplestory Light/Bold; title cover texture; four starter facilities; 16 resources / 16 recipes; mining; production; storage blocking; cost conservation; multiple batches; recipe changes; placement; save/load; 12 building sprites.");
                Debug.Log("Comet Foundry validation passed.");
            }
            AssetDatabase.Refresh();
            foreach (string file in Directory.GetFiles("Assets/CometFoundry/Art/Playable", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 128;
                importer.filterMode = FilterMode.Point; importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            }
        }
        catch (Exception exception)
        {
            File.WriteAllText("ValidationArtifacts/result.txt", "FAIL: " + exception); Debug.LogException(exception);
        }
    }
    [MenuItem("Comet Foundry/Validate Calendar and Monthly Charts")]
    public static void RunMonthly()
    {
        Directory.CreateDirectory("ValidationArtifacts");
        try
        {
            Require(FoundrySession.DateAt(0) == "1년 01월 01일", "initial calendar date");
            Require(FoundrySession.DateAt(29) == "1년 01월 30일", "30-day month end");
            Require(FoundrySession.DateAt(30) == "1년 02월 01일", "month rollover");
            Require(FoundrySession.DateAt(360) == "2년 01월 01일", "year rollover");
            using (var clock = new FoundrySession())
            {
                clock.Advance(4.9);
                Require(clock.CalendarDate == "1년 01월 01일", "day stays before five seconds");
                clock.Advance(.1);
                Require(clock.CalendarDate == "1년 01월 02일", "day advances at five seconds");
                clock.Advance(145);
                Require(clock.CalendarDate == "1년 02월 01일", "month advances at 150 seconds");
                var oldSave = JsonUtility.FromJson<FoundryWorldSave>(clock.ExportState());
                oldSave.version = 2; oldSave.seconds = clock.Day * 300; oldSave.calendarSecondsPerDay = 0;
                using (var restored = new FoundrySession())
                {
                    Require(restored.ImportState(JsonUtility.ToJson(oldSave)), "old 300-second save imports");
                    Require(restored.CalendarDate == clock.CalendarDate, "old save preserves calendar date");
                    Require(restored.Balances.Count == clock.Balances.Count, "old save preserves monthly history");
                }
            }
            using (var s = new FoundrySession())
            {
                foreach (var material in s.Content.Resources)
                    for (int month = -24; month <= 24; month++)
                    {
                        double day = month * 30, ratio = s.PriceRatio(material, day);
                        Require(Math.Abs(ratio - s.PriceRatio(material, day + 29.999)) < 1e-10, "price held for 30 days");
                        Require(s.MarketEventAt(material, day) == s.MarketEventAt(material, day + 29.999), "event held for 30 days");
                        bool active = s.MarketEventAt(material, day) != null;
                        Require(ratio >= (active ? .55 : .70) - 1e-10 && ratio <= (active ? 1.45 : 1.30) + 1e-10, "monthly price limits");
                    }
                var saved = JsonUtility.FromJson<FoundryWorldSave>(s.ExportState());
                saved.seconds = 29.96 * FoundrySession.RealSecondsPerDay;
                Require(s.ImportState(JsonUtility.ToJson(saved)), "load near month boundary");
                double initial = s.Balances[0].value;
                s.Company.Spend(123);
                s.Advance(.1);
                Require(s.Balances.Count == 1 && s.Balances[0].value == initial, "intra-month spending preserves snapshot");
                s.Advance(.3);
                Require(s.Month == 1 && s.Balances.Count == 2, "new month records balance");
                Require(s.Balances[1].day == 30 && Math.Abs(s.Balances[1].value - s.Company.Credits) < 1e-8, "monthly balance equals actual credits");
                var r = s.Content.Resources[0];
                Require(Math.Abs(s.SellUnitPrice(r) / (r.BaseMarketPrice * MarketSystem.GetSaleMultiplier(s.Company)) - s.PriceRatio(r, 30)) < 1e-8, "trade quote uses refreshed monthly price");
                double snapshot = s.Balances[1].value;
                s.Company.Spend(100); s.Advance(.1);
                Require(s.Balances[1].value == snapshot, "monthly snapshot remains immutable");
                foreach (int months in new[] { 6, 12, 18, 24 })
                {
                    var points = s.PriceHistory(r, months);
                    Require(points.Count == months + 1 && points.Last().day == 30, "monthly graph range");
                    for (int p = 1; p < points.Count; p++) Require(points[p].day - points[p - 1].day == 30, "30-day graph spacing");
                }
                using (var restored = new FoundrySession())
                {
                    Require(restored.ImportState(s.ExportState()), "monthly save roundtrip");
                    Require(restored.CalendarDate == s.CalendarDate && restored.Balances.Last().value == snapshot, "date and snapshot preserved on load");
                }
                var chart = FoundryChartRaster.Create(s.PriceHistory(r, 6), 598, 308, r.BaseMarketPrice * .55, r.BaseMarketPrice * 1.45, r.BaseMarketPrice, Color.white, true);
                try { File.WriteAllBytes("ValidationArtifacts/monthly-price-curve.png", chart.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(chart); }
            }
            File.WriteAllText("ValidationArtifacts/monthly-result.txt", "PASS: 5-second day; 150-second month; legacy calendar save migration; calendar/month/year rollovers; all-material 30-day fixed prices/events; price limits; month-boundary trade quote refresh; immutable balance snapshots; monthly graph ranges; save/load.");
        }
        catch (Exception exception)
        {
            File.WriteAllText("ValidationArtifacts/monthly-result.txt", "FAIL: " + exception); Debug.LogException(exception);
        }
    }
    [MenuItem("Comet Foundry/Validate Missions and Demolition")]
    public static void RunRevision()
    {
        Directory.CreateDirectory("ValidationArtifacts");
        try
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CometFoundry.Tests.FoundryRevisionTests")).FirstOrDefault(t => t != null);
            Require(type != null, "revision test assembly loaded");
            var instance = Activator.CreateInstance(type);
            var methods = type.GetMethods().Where(m => m.GetCustomAttributes(false).Any(a => a.GetType().Name == "TestAttribute")).ToList();
            foreach (var method in methods) method.Invoke(instance, null);
            File.WriteAllText("ValidationArtifacts/revision-result.txt", "PASS: " + string.Join("; ", methods.Select(m => m.Name)));
        }
        catch (Exception exception) { File.WriteAllText("ValidationArtifacts/revision-result.txt", "FAIL: " + (exception.InnerException ?? exception)); Debug.LogException(exception); }
    }
    private static void Require(bool condition, string name) { if (!condition) throw new Exception("Failed: " + name); }
}
