using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CometFoundry.Data;
using CometFoundry.Simulation;
using UnityEngine;

namespace CometFoundry.Prototype
{
    public sealed partial class FoundryPlayableController : MonoBehaviour
    {
        private FoundrySession session;
        private FoundryBuildingArt art;
        private FacilityDefinition build;
        private FacilityInstance selected;
        private RecipeDefinition preview;
        private GUIStyle text, title, small, button, heading;
        private Font font, boldFont;
        private bool ownsFallbackFont;
        private float speed = 1;
        private Vector2 buildScroll;
        private Vector2 inventoryScroll, popupScroll;
        private string notice = "발전소·기본 창고·채굴기 2개가 준비됐습니다. 상단 튜토리얼에서 채굴기 건설부터 차근차근 시작하세요.";
        private bool confirmNew;
        private static readonly Color Ink = new Color32(229, 236, 230, 255);
        private static readonly Color Muted = new Color32(137, 159, 162, 255);
        private static readonly Color Orange = new Color32(255, 146, 62, 255);
        private static readonly Color Panel = new Color32(22, 34, 42, 255);
        private static readonly Color Green = new Color32(125, 214, 177, 255);
        private static readonly Rect Board = new Rect(340, 204, 768, 576);
        private float MapCell => Mathf.Min(Board.width / session.Comet.Grid.Width, Board.height / session.Comet.Grid.Height);
        private Rect MapRect => new Rect(Board.x + (Board.width - session.Comet.Grid.Width * MapCell) / 2, Board.y + (Board.height - session.Comet.Grid.Height * MapCell) / 2, session.Comet.Grid.Width * MapCell, session.Comet.Grid.Height * MapCell);

        private void Awake()
        {
            Application.runInBackground = true;
            session = new FoundrySession(); art = new FoundryBuildingArt();
            if (Camera.main == null)
            {
                var camera = new GameObject("Foundry Camera").AddComponent<Camera>();
                camera.tag = "MainCamera"; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(10, 18, 25, 255);
            }
            font = Resources.Load<Font>("Fonts/NEXON_Maplestory/TTF/Maplestory Light");
            boldFont = Resources.Load<Font>("Fonts/NEXON_Maplestory/TTF/Maplestory Bold");
            if (font == null) { font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 16); ownsFallbackFont = true; Debug.LogError("Maplestory Light font asset is missing."); }
            titleArtwork = Resources.Load<Texture2D>("Title/CometFoundryCover");
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < arguments.Length; i++)
            {
                if (arguments[i] == "-foundryCapture") StartCoroutine(CaptureReview(arguments[i + 1]));
                if (arguments[i] == "-foundryTutorialCapture") StartCoroutine(CaptureGuidedReview(arguments[i + 1]));
            }
        }
        private IEnumerator CaptureReview(string directory)
        {
            Directory.CreateDirectory(directory);
            speed = 0;
            yield return new WaitForSecondsRealtime(2);
            yield return new WaitForEndOfFrame();
            CaptureFrame(Path.Combine(directory, "title.png"));
            showingTitle = false;
            yield return new WaitForEndOfFrame();
            CaptureFrame(Path.Combine(directory, "gameplay.png"));
            tutorialChoice = true; yield return new WaitForEndOfFrame(); CaptureFrame(Path.Combine(directory, "tutorial-choice.png"));
            StartTutorialGuide(); yield return new WaitForEndOfFrame(); CaptureFrame(Path.Combine(directory, "tutorial-guide.png"));
            guidePage = guidedSteps.Count - 1; EnterGuideStep(); yield return new WaitForEndOfFrame(); CaptureFrame(Path.Combine(directory, "tutorial-last-page.png"));
            tutorialGuide = false; session.Build(session.Content.FindBuilding("market-terminal"), new Vector2Int(12, 7), out _);
            missionList = true; yield return new WaitForEndOfFrame(); CaptureFrame(Path.Combine(directory, "mission-list.png"));
            missionList = false; session.AcceptMission(session.Missions[0].Id, out _);
            yield return new WaitForEndOfFrame(); CaptureFrame(Path.Combine(directory, "mission-hud.png"));
            demolitionTarget = session.FacilityAt(new Vector2Int(2, 3));
            yield return new WaitForEndOfFrame(); CaptureFrame(Path.Combine(directory, "demolition-popup.png"));
            demolitionTarget = null; page = FoundryPage.Exchange;
            yield return new WaitForEndOfFrame(); CaptureFrame(Path.Combine(directory, "exchange-no-events.png"));
            page = FoundryPage.Board;
            yield return new WaitForSecondsRealtime(1);
            foreach (var facility in session.Comet.Facilities)
                if (facility.Definition.FacilityType == FacilityType.Refinery) { selected = facility; preview = facility.ActiveRecipe; break; }
            yield return new WaitForEndOfFrame();
            CaptureFrame(Path.Combine(directory, "production-popup.png"));
            yield return new WaitForSecondsRealtime(1);
            Application.Quit();
        }
        private void CaptureFrame(string path)
        {
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var capture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                ScreenCapture.CaptureScreenshotIntoRenderTexture(target);
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                capture.Apply(); File.WriteAllBytes(path, capture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Destroy(capture); }

        }
        private void Update() { if (!showingTitle && !GameOverlayOpen && !TutorialPausesTime) session.Advance(Math.Min(Time.unscaledDeltaTime, 0.25f) * speed); if (!showingTitle) { CheckGuidedTutorial(); if (!tutorialGuide) CheckPracticePopup(); } }
        private void OnDestroy()
        {
            if (priceChartTexture != null) Destroy(priceChartTexture);
            if (balanceChartTexture != null) Destroy(balanceChartTexture);
            session?.Dispose(); art?.Dispose(); if (ownsFallbackFont && font != null) Destroy(font);
        }
        private GUIStyle Style(int size, Color color, FontStyle weight = FontStyle.Normal)
        {
            return new GUIStyle { font = weight == FontStyle.Bold && boldFont != null ? boldFont : font, fontSize = size, fontStyle = weight, normal = { textColor = color }, wordWrap = true, alignment = TextAnchor.MiddleLeft };
        }
        private void OnGUI()
        {
            if (session == null) return;
            if (text == null)
            {
                text = Style(15, Ink); title = Style(29, Ink, FontStyle.Bold);
                small = Style(12, Muted); heading = Style(19, Ink, FontStyle.Bold);
                button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 14, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 3, 3) };
            }
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            bool oldEnabled = GUI.enabled;
            float scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1440 * scale) / 2, (Screen.height - 900 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            Fill(new Rect(0, 0, 1440, 900), new Color32(10, 18, 25, 255));
            for (int i = 0; i < 75; i++) Fill(new Rect((i * 193 + 71) % 1430, (i * 137 + 21) % 900, i % 3 == 0 ? 2 : 1, 1), new Color32(66, 85, 98, 255));
            if (showingTitle)
            {
                DrawTitleScreen();
                GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.enabled = oldEnabled;
                return;
            }
            if (!GameInputBlocked) HandleKeys();
            GUI.enabled = (selected == null) && !GameInputBlocked;
            DrawHeader();
            GUI.enabled = (selected == null && page == FoundryPage.Board) && !GameInputBlocked;
            DrawBuildMenu(); DrawBoard(); DrawSidebar();
            GUI.enabled = (true) && !GameInputBlocked;
            Fill(new Rect(24, 818, 1392, 58), Panel);
            Label(new Rect(42, 828, 1352, 35), notice, text);
            if (page == FoundryPage.Exchange) DrawExchange();
            if (page == FoundryPage.Management) DrawManagement();
            if (selected != null) DrawPopup();
            if (GameOverlayOpen) DrawGameOverlay();
            if (tutorialGuide) DrawGuidedTutorial();
            GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.enabled = oldEnabled;
        }
        private void HandleKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode == KeyCode.Escape) { selected = null; build = null; demolishMode = false; page = FoundryPage.Board; confirmNew = false; e.Use(); }
            if (e.keyCode == KeyCode.Space && selected == null) { speed = speed == 0 ? 1 : 0; e.Use(); }
        }
        private void DrawHeader()
        {
            Fill(new Rect(24, 24, 5, 60), Orange);
            Label(new Rect(43, 25, 520, 39), "COMET FOUNDRY", title);
            Label(new Rect(44, 65, 540, 22), $"A-0 관문  /  {session.Comet.DistanceCode}  /  {session.ActiveColony.Name}", small);
            Label(new Rect(610, 27, 200, 25), $"{session.Company.Credits:N0} 솔라", heading);
            var power = session.Power;
            Label(new Rect(610, 61, 240, 22), $"전력 {power.ConsumedPower:0} / {power.GeneratedPower:0}  ·  {(power.HasDeficit ? "공급 부족" : "공급 안정")}", power.HasDeficit ? Style(13, Orange) : small);
            if (Btn(new Rect(880, 29, 75, 34), speed == 0 ? "▶ 재개" : "Ⅱ 정지")) speed = speed == 0 ? 1 : 0;
            if (Btn(new Rect(963, 29, 62, 34), "1×", speed == 1)) speed = 1;
            if (Btn(new Rect(1033, 29, 62, 34), "4×", speed == 4)) speed = 4;
            if (Btn(new Rect(1117, 29, 75, 34), "저장")) { session.Save(); notice = "모든 혜성·거래·수송·튜토리얼 상태를 저장했습니다."; }
            if (Btn(new Rect(1200, 29, 112, 34), "튜토리얼")) StartTutorialGuide();
            if (Btn(new Rect(1320, 29, 96, 34), "타이틀"))
            {
                showingTitle = true; hasActiveGame = true; selected = null; build = null; confirmNew = false; titleNotice = "";
            }
            Label(new Rect(885, 65, 525, 21), $"{session.CalendarDate} · 개척 {1 + (int)session.Day}일차  ·  운송 {(session.Flight == null ? "대기" : "진행 중")}  ·  Space 일시정지", small);
            Fill(new Rect(24, 109, 1392, 2), new Color32(51, 70, 79, 255));
            Label(new Rect(24, 123, 280, 27), "01  /  건설 계획", heading);
            Label(new Rect(340, 123, 768, 27), "02  /  혜성 개발 구역", heading);
            Label(new Rect(1132, 123, 284, 27), "03  /  개척 현황", heading);
        }
        private void DrawBuildMenu()
        {
            Fill(new Rect(24, 164, 292, 630), Panel);
            Label(new Rect(42, 175, 255, 25), "시설 선택 → 빈 타일 클릭", small);
            buildScroll = GUI.BeginScrollView(new Rect(40, 209, 272, 532), buildScroll, new Rect(0, 0, 252, session.Content.Buildings.Count * 54));
            float y = 0;
            foreach (var definition in session.Content.Buildings)
            {
                if (Btn(new Rect(0, y, 248, 53), "", build == definition))
                { demolishMode = false; build = build == definition ? null : definition; notice = build == null ? "건설 선택을 취소했습니다." : definition.DisplayName + " 배치 위치를 선택하세요. Esc / 우클릭으로 취소."; }
                GUI.DrawTexture(new Rect(8, y + 7, 37, 37), art.Get(definition.FacilityType), ScaleMode.ScaleToFit);
                Label(new Rect(54, y + 4, 182, 24), definition.DisplayName, text);
                Label(new Rect(54, y + 28, 182, 18), $"{definition.FootprintSize.x}×{definition.FootprintSize.y}  /  {definition.BuildCost:N0} 솔라", small);
                y += 54;
            }
            GUI.EndScrollView();
            if (Btn(new Rect(42, 755, 252, 29), demolishMode ? "철거 모드 종료" : "시설 철거", demolishMode)) { demolishMode = !demolishMode; build = null; notice = "철거할 시설을 클릭하세요. Esc로 취소합니다."; }
        }
        private Rect TileRect(Vector2Int anchor, Vector2Int size)
            => new Rect(MapRect.x + anchor.x * MapCell, MapRect.y + (session.Comet.Grid.Height - anchor.y - size.y) * MapCell, size.x * MapCell, size.y * MapCell);
        private void DrawBoard()
        {
            Fill(new Rect(330, 164, 788, 630), Panel);
            Label(new Rect(345, 171, 740, 22), demolishMode ? "철거 모드: 건물 클릭 → 확인 팝업  /  Esc 취소" : build == null ? "건물 클릭: 생산 설정  /  " + session.DroneStatus : "건설: " + build.DisplayName + "  /  주황색은 배치 불가, 초록색은 배치 가능", small);
            for (int x = 0; x < session.Comet.Grid.Width; x++) for (int y = 0; y < session.Comet.Grid.Height; y++)
            {
                var coordinate = new Vector2Int(x, y); var tile = session.Comet.Grid.GetTile(coordinate);
                Rect rect = TileRect(coordinate, Vector2Int.one);
                if (tile.Terrain == CometTileTerrain.Void) continue;
                rect.x++; rect.y++; rect.width -= 2; rect.height -= 2;
                Fill(rect, (x + y) % 2 == 0 ? new Color32(41, 54, 60, 255) : new Color32(37, 50, 57, 255));
                var deposit = session.DepositAt(coordinate);
                if (deposit != null && tile.Occupant == CometTileOccupant.Empty)
                {
                    Color color = deposit.Resource.ResourceId == "iron-ore" ? new Color32(185, 170, 151, 255) : deposit.Resource.ResourceId == "ice" ? new Color32(145, 210, 223, 255) : Orange;
                    Fill(new Rect(rect.x + 9, rect.y + 11, 16, 17), color); Fill(new Rect(rect.x + 24, rect.y + 18, 13, 14), color * 0.85f);
                    Label(new Rect(rect.x + 4, rect.y + 30, 42, 16), deposit.Resource.DisplayName, Style(9, Ink));
                }
            }
            foreach (var facility in session.Comet.Facilities)
            {
                Rect rect = TileRect(facility.Anchor, facility.Definition.FootprintSize);
                Fill(new Rect(rect.x + 5, rect.y + 8, rect.width - 6, rect.height - 7), new Color32(10, 17, 22, 200));
                GUI.DrawTexture(new Rect(rect.x + 1, rect.y, rect.width - 2, rect.height - 6), art.Get(facility.Definition.FacilityType), ScaleMode.StretchToFill);
                string status = session.Status(facility);
                Color indicator = status == "생산 중" || status == "가동 중" ? Green : Orange;
                Fill(new Rect(rect.x + 4, rect.y + 4, 5, 5), indicator);
                if (facility.ActiveRecipe != null)
                {
                    Fill(new Rect(rect.x + 4, rect.yMax - 6, rect.width - 8, 4), new Color32(18, 27, 32, 255));
                    Fill(new Rect(rect.x + 4, rect.yMax - 6, (rect.width - 8) * Mathf.Clamp01((float)(facility.ProgressDays / facility.ActiveRecipe.DurationDays)), 4), Orange);
                }
                if (rect.width >= 80) Label(new Rect(rect.x + 7, rect.yMax - 26, rect.width - 14, 18), facility.ActiveRecipe?.Outputs[0].Resource.DisplayName ?? facility.Definition.DisplayName, Style(10, DarkText()));
            }
            var flight = session.Flight;
            if (flight != null)
            {
                Vector2 source = TileRect(flight.Source.Anchor, flight.Source.Definition.FootprintSize).center;
                Vector2 destination = TileRect(flight.Destination.Anchor, flight.Destination.Definition.FootprintSize).center;
                Vector2 position = Vector2.Lerp(source, destination, session.FlightProgress);
                for (int i = 1; i < 15; i++)
                { Vector2 dot = Vector2.Lerp(source, destination, i / 15f); Fill(new Rect(dot.x, dot.y, 2, 2), new Color32(255, 146, 62, 65)); }
                Fill(new Rect(position.x - 5, position.y - 3, 10, 6), Ink); Fill(new Rect(position.x - 2, position.y - 2, 4, 4), Orange);
            }
            var e = Event.current;
            if (GameInputBlocked || selected != null || page != FoundryPage.Board || !MapRect.Contains(e.mousePosition)) return;
            Vector2Int hover = new Vector2Int(Mathf.FloorToInt((e.mousePosition.x - MapRect.x) / MapCell), session.Comet.Grid.Height - 1 - Mathf.FloorToInt((e.mousePosition.y - MapRect.y) / MapCell));
            if (build != null)
            {
                Rect ghost = TileRect(hover, build.FootprintSize);
                Fill(ghost, session.PlacementError(build, hover) == null ? new Color(0.4f, 0.9f, 0.7f, 0.3f) : new Color(1, 0.4f, 0.1f, 0.4f));
            }
            if (e.type == EventType.MouseDown && e.button == 1) { build = null; demolishMode = false; e.Use(); }
            if (e.type != EventType.MouseDown || e.button != 0) return;
            var clicked = session.FacilityAt(hover);
            if (clicked != null)
            {
                if (demolishMode) { demolitionTarget = clicked; e.Use(); return; }
                if (clicked.Definition.FacilityType == FacilityType.MarketTerminal) OpenExchange();
                else { selected = clicked; preview = clicked.ActiveRecipe ?? FirstAllowed(clicked); popupScroll = Vector2.zero; }
            }
            else if (build != null && !demolishMode)
            {
                if (session.Build(build, hover, out var message)) { build = null; CheckGuidedConstruction(); }
                notice = message;
            }
            else
            {
                var deposit = session.DepositAt(hover);
                notice = deposit == null ? "왼쪽에서 시설을 선택해 건설하거나, 기존 건물을 클릭하세요." : $"{deposit.Resource.DisplayName} 매장량 {deposit.RemainingStacks:N0} 스택. 소형 채굴기를 이 노드 위에 건설하세요.";
            }
            e.Use();
        }
        private RecipeDefinition FirstAllowed(FacilityInstance facility)
        {
            foreach (var recipe in facility.Definition.SupportedRecipes) if (session.CanSelect(facility, recipe)) return recipe;
            return null;
        }
        private void DrawSidebar()
        {
            Fill(new Rect(1132, 164, 284, 630), Panel);
            DrawMissionHud();
            Label(new Rect(1150, 389, 246, 26), "현재 혜성 재고 / 스택", heading);
            inventoryScroll = GUI.BeginScrollView(new Rect(1144, 426, 260, 265), inventoryScroll, new Rect(0, 0, 238, session.Content.Resources.Count * 28));
            float y = 0;
            foreach (var resource in session.Content.Resources)
            {
                double amount = 0; foreach (var facility in session.Comet.Facilities) amount += facility.Inventory.GetAmount(resource);
                Label(new Rect(4, y, 173, 24), resource.DisplayName, small);
                Label(new Rect(181, y, 51, 24), amount.ToString("0.#"), amount > 0 ? text : small); y += 28;
            }
            GUI.EndScrollView();
            bool enabled = GUI.enabled; GUI.enabled = (enabled && session.HasMarket) && !GameInputBlocked;
            if (Btn(new Rect(1150, 714, 246, 33), "거래소 열기")) OpenExchange();
            if (Btn(new Rect(1150, 752, 246, 33), "혜성관리")) { page = FoundryPage.Management; build = null; }
            GUI.enabled = (enabled) && !GameInputBlocked;
        }
        private void DrawPopup()
        {
            Fill(new Rect(0, 0, 1440, 900), new Color(0, 0, 0, 0.7f));
            Rect window = new Rect(250, 135, 940, 650);
            Fill(window, Panel); Fill(new Rect(250, 135, 940, 4), Orange);
            GUI.DrawTexture(new Rect(273, 158, 54, 54), art.Get(selected.Definition.FacilityType), ScaleMode.ScaleToFit);
            Label(new Rect(341, 159, 650, 34), selected.Definition.DisplayName, title);
            Label(new Rect(342, 198, 600, 24), $"좌표 ({selected.Anchor.x}, {selected.Anchor.y})  /  {session.Status(selected)}  /  전력 {selected.Definition.BasePowerConsumption:0}", small);
            if (Btn(new Rect(1000, 158, 85, 35), "철거")) { demolitionTarget = selected; return; }
            if (Btn(new Rect(1098, 158, 69, 35), "닫기")) { selected = null; return; }
            Fill(new Rect(273, 234, 894, 1), new Color32(54, 71, 80, 255));
            if (selected.Definition.SupportedRecipes.Count == 0) { DrawStoragePopup(); return; }
            Label(new Rect(274, 248, 235, 25), "생산품 선택", heading);
            float y = 287;
            foreach (var recipe in selected.Definition.SupportedRecipes)
            {
                GUI.enabled = (session.CanSelect(selected, recipe)) && !GameInputBlocked;
                if (Btn(new Rect(274, y, 254, 55), recipe.DisplayName + (recipe == selected.ActiveRecipe ? "  • 가동" : ""), preview == recipe)) preview = recipe;
                GUI.enabled = (true) && !GameInputBlocked; y += 64;
            }
            if (preview == null) return;
            Label(new Rect(557, 251, 595, 31), preview.DisplayName, heading);
            Label(new Rect(557, 291, 595, 43), preview.Description, text);
            Fill(new Rect(555, 350, 610, 78), new Color32(34, 48, 56, 255));
            Label(new Rect(572, 358, 270, 23), "생산 비용 / 1회", small);
            Label(new Rect(572, 387, 270, 28), $"{preview.ProductionCost:N0} 솔라 + 아래 재료", heading);
            Label(new Rect(883, 358, 259, 23), "생산 소요 시간 / 1회", small);
            Label(new Rect(883, 387, 259, 28), $"{preview.DurationDays * 300:0.#}초  (기본 속도)", heading);
            Label(new Rect(557, 443, 590, 24), "필요 재료  /  건물 보유량", heading);
            y = 479;
            if (preview.Inputs.Count == 0) Label(new Rect(557, y, 585, 24), "재료 없음 · 자원 노드 매장량을 소비합니다.", text);
            foreach (var input in preview.Inputs)
            {
                double have = selected.Inventory.GetAmount(input.Resource);
                Label(new Rect(557, y, 595, 25), $"{input.Resource.DisplayName}  {input.Stacks:0.#} 스택   /   보유 {have:0.#}", have >= input.Stacks ? text : Style(15, Orange)); y += 30;
            }
            Label(new Rect(557, 555, 590, 24), "완성품", heading);
            y = 590;
            foreach (var output in preview.Outputs)
            {
                Label(new Rect(557, y, 595, 28), $"{output.Resource.DisplayName}  × {output.Stacks:0.#} 스택   ·   기준가 {output.Resource.BaseMarketPrice:N0} 솔라 / 스택", Style(16, Green)); y += 30;
            }
            Label(new Rect(557, 636, 593, 32), "비용과 재료는 완성 시 차감됩니다. 다른 생산품으로 변경하면 진행도가 초기화됩니다.", small);
            if (selected.ActiveRecipe != null)
                Label(new Rect(274, 666, 254, 26), $"진행 {Mathf.Clamp01((float)(selected.ProgressDays / selected.ActiveRecipe.DurationDays)) * 100:0}% · {selected.ActiveRecipe.DisplayName}", small);
            if (Btn(new Rect(557, 689, 370, 47), preview == selected.ActiveRecipe ? "현재 생산품 유지" : "이 생산품으로 설정", true))
            {
                if (session.CanSelect(selected, preview) && selected.AssignRecipe(preview))
                { notice = selected.Definition.DisplayName + ": " + preview.DisplayName + " 설정 완료. 드론이 필요한 재료를 자동 공급합니다."; session.RefreshTutorial(); selected = null; return; }
            }
            if (Btn(new Rect(942, 689, 221, 47), "생산 중지")) { selected.ClearRecipe(); preview = FirstAllowed(selected); }
            Label(new Rect(274, 744, 885, 25), $"시설 보관량 {selected?.Inventory.UsedStacks ?? 0:0.#} / {selected?.Inventory.CapacityStacks ?? 0:0} 스택  ·  Esc 닫기", small);
        }
        private void DrawStoragePopup()
        {
            string help = selected.Definition.IsWarehouse ? "기본 창고의 지원 드론이 생산 재료와 재고를 자동 운반합니다. 시작 혜성에서는 임시 선착장도 지원합니다." : selected.Definition.IsPowerPlant ? "혜성 전체 시설에 전력을 공급합니다. 전력이 부족하면 생산과 운송이 일시 정지됩니다." : selected.Definition.IsDroneHangar ? "물류 드론 지원 시설입니다. 전력과 빈 통로가 필요합니다. 수용 드론 " + selected.Definition.DroneCapacity + "기 / 연결 " + selected.Definition.DroneLinkCapacity + "개. " + session.DroneStatus : "혜성 간 수송선이 도착해 화물을 하역합니다. 보관 공간이 부족하면 수송선에 화물을 보관하고 대기합니다.";
            Label(new Rect(274, 251, 885, 65), help, text);
            Label(new Rect(274, 325, 885, 30), selected.Definition.IsPowerPlant ? $"전력 생산 {selected.Definition.PowerOutput:0}" : selected.Definition.IsDroneHangar ? $"드론 수용 {selected.Definition.DroneCapacity}기 · 연결 {selected.Definition.DroneLinkCapacity}개" : $"보관량 {selected.Inventory.UsedStacks:0.#} / {selected.Inventory.CapacityStacks:0} 스택", heading);
            popupScroll = GUI.BeginScrollView(new Rect(274, 377, 888, 333), popupScroll, new Rect(0, 0, 857, Math.Max(300, selected.Inventory.Amounts.Count * 39)));
            float y = 0;
            foreach (var amount in selected.Inventory.Amounts)
            {
                Fill(new Rect(0, y, 847, 35), new Color32(34, 48, 56, 255));
                Label(new Rect(12, y, 820, 35), $"{amount.Key.DisplayName}   {amount.Value:0.#} 스택", text); y += 39;
            }
            if (y == 0) Label(new Rect(12, 8, 800, 35), "보관 중인 물자가 없습니다.", small);
            GUI.EndScrollView();
            Label(new Rect(274, 742, 885, 25), "Esc 또는 닫기로 돌아가기", small);
        }
        private static Color DarkText() => new Color32(29, 44, 51, 255);
        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
        private static void Label(Rect rect, string value, GUIStyle style) => GUI.Label(rect, value, style);
        private bool Btn(Rect rect, string value, bool accent = false)
        {
            Color old = GUI.backgroundColor; GUI.backgroundColor = accent ? Orange : new Color32(63, 82, 90, 255);
            bool enabled = GUI.enabled; if (tutorialGuide && !drawingGuide && (TutorialPausesTime || TutorialPointerOverPopup)) GUI.enabled = false;
            bool result = GUI.Button(rect, value, button); GUI.enabled = enabled; GUI.backgroundColor = old; return result;
        }
    }
}
