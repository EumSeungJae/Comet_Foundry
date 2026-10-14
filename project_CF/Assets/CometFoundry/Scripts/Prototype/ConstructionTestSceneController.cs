using System.Collections.Generic;
using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CometFoundry.Prototype
{
    public enum ConstructionTestMode
    {
        Facility,
        Rail
    }

    public sealed class ConstructionTestSceneController : MonoBehaviour
    {
        [SerializeField] private int gridWidth = 12;
        [SerializeField] private int gridHeight = 8;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Color stableColor = new Color(0.12f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color roughColor = new Color(0.19f, 0.18f, 0.14f, 1f);
        [SerializeField] private Color voidColor = new Color(0.04f, 0.05f, 0.07f, 1f);
        [SerializeField] private Color facilityWaitingColor = new Color(0.82f, 0.18f, 0.16f, 1f);
        [SerializeField] private Color facilitySatisfiedColor = new Color(0.15f, 0.65f, 0.92f, 1f);
        [SerializeField] private Color railColor = new Color(0.95f, 0.72f, 0.24f, 1f);
        [SerializeField] private Color ghostValidColor = new Color(0.2f, 0.9f, 0.55f, 0.45f);
        [SerializeField] private Color ghostInvalidColor = new Color(1f, 0.25f, 0.2f, 0.45f);
        [SerializeField] private Color railPreviewColor = new Color(0.95f, 0.86f, 0.25f, 0.65f);
        [SerializeField] private Color conveyorTestOrbColor = new Color(0.7f, 0.95f, 1f, 1f);
        [SerializeField] private Color producedOrbColor = new Color(0.35f, 1f, 0.48f, 1f);
        [SerializeField] private float conveyorTestSpeedTilesPerSecond = 2.5f;

        private readonly PlacementSystem placementSystem = new PlacementSystem();
        private readonly List<Vector2Int> railPreviewPath = new List<Vector2Int>();
        private readonly List<ConveyorTestOrb> conveyorTestOrbs = new List<ConveyorTestOrb>();
        private readonly Dictionary<SimulationId, int> facilityStoredItems = new Dictionary<SimulationId, int>();

        private CometState comet;
        private FacilityDefinition testStructure;
        private SpriteRenderer[,] tileRenderers;
        private SpriteRenderer[,] overlayRenderers;
        private Camera mainCamera;
        private Sprite whiteSprite;
        private ConstructionTestMode mode;
        private bool isDraggingRail;
        private bool railPreviewValid;
        private int facilityIndex;
        private int railIndex;
        private int conveyorHandoffCount;
        private int facilityDeliveryCount;
        private int facilityProductionCount;
        private string statusMessage = "2x2 테스트 구조물 배치 모드";

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 0f, -10f);
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 5.8f;
                mainCamera.backgroundColor = new Color(0.03f, 0.04f, 0.06f, 1f);
            }

            whiteSprite = CreateWhiteSprite();
            CreatePrototypeData();
            ResetBoard();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            UpdateConveyorTestOrbs(Time.deltaTime);

            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                CancelRailPreview("모드 전환으로 레일 배치 취소");
                mode = mode == ConstructionTestMode.Facility ? ConstructionTestMode.Rail : ConstructionTestMode.Facility;
                statusMessage = mode == ConstructionTestMode.Facility ? "2x2 테스트 구조물 배치 모드" : "컨베이어 레일 드래그 모드";
            }

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                CancelRailPreview("보드 초기화");
                ResetBoard();
            }

            if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
            {
                ConfirmRailPreview();
            }

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelRailPreview("레일 배치 취소");
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                BeginMouseAction();
            }

            if (mouse != null && mouse.leftButton.isPressed && isDraggingRail)
            {
                UpdateRailPreviewFromMouse();
            }

            if (mouse != null && mouse.leftButton.wasReleasedThisFrame && isDraggingRail)
            {
                statusMessage = railPreviewValid
                    ? "레일 경로 준비 완료. Enter 또는 확정 버튼으로 건설"
                    : "현재 경로에는 배치할 수 없는 타일이 있음";
            }

            RefreshOverlay();
        }

        private void OnGUI()
        {
            const int width = 390;
            GUILayout.BeginArea(new Rect(16, 16, width, 245), GUI.skin.box);
            GUILayout.Label("Comet Foundry 건설 테스트");
            GUILayout.Space(6);
            GUILayout.Label($"모드: {(mode == ConstructionTestMode.Facility ? "2x2 구조물" : "컨베이어 레일")}");
            GUILayout.Label($"혜성 내부 배치도: {gridWidth} x {gridHeight}");
            GUILayout.Label($"시설 수: {comet.Facilities.Count} / 레일 수: {comet.Rails.Count}");
            GUILayout.Label($"방향 테스트 구체: {conveyorTestOrbs.Count}");
            GUILayout.Label($"컨베이어 전달 횟수: {conveyorHandoffCount}");
            GUILayout.Label($"건물 입고 횟수: {facilityDeliveryCount}");
            GUILayout.Label($"건물 생산 횟수: {facilityProductionCount}");
            if (mode == ConstructionTestMode.Rail)
            {
                GUILayout.Label($"프리뷰 길이: {railPreviewPath.Count} / 상태: {(railPreviewValid ? "배치 가능" : "배치 불가")}");
            }

            GUILayout.Space(6);
            GUILayout.Label("좌클릭: 구조물 배치 또는 레일 드래그 시작");
            GUILayout.Label("Enter: 레일 확정 / Esc: 레일 취소");
            GUILayout.Label("Tab: 구조물/레일 모드 전환 / R: 초기화");
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUI.enabled = mode == ConstructionTestMode.Rail && railPreviewPath.Count > 0 && railPreviewValid;
            if (GUILayout.Button("레일 확정"))
            {
                ConfirmRailPreview();
            }

            GUI.enabled = mode == ConstructionTestMode.Rail && railPreviewPath.Count > 0;
            if (GUILayout.Button("취소"))
            {
                CancelRailPreview("레일 배치 취소");
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label(statusMessage);
            GUILayout.EndArea();
        }

        private void CreatePrototypeData()
        {
            testStructure = ScriptableObject.CreateInstance<FacilityDefinition>();
            testStructure.ConfigurePrototype("test-structure-2x2", "2x2 테스트 구조물", FacilityType.Factory, new Vector2Int(2, 2), 100d);
        }

        private void ResetBoard()
        {
            ClearVisuals();

            comet = new CometState(new SimulationId("construction-test-comet"), "건설 테스트 혜성", Vector2.zero, 5f, gridWidth, gridHeight);
            ApplyTestTerrain(comet.Grid);
            CreateGridVisuals();
            railPreviewPath.Clear();
            conveyorTestOrbs.Clear();
            facilityStoredItems.Clear();
            conveyorHandoffCount = 0;
            facilityDeliveryCount = 0;
            facilityProductionCount = 0;
            isDraggingRail = false;
            railPreviewValid = false;
            statusMessage = "보드 초기화 완료";
        }

        private void ApplyTestTerrain(CometGrid grid)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                grid.GetTile(new Vector2Int(x, 0)).SetTerrain(CometTileTerrain.Rough);
            }

            grid.GetTile(new Vector2Int(3, 3)).SetTerrain(CometTileTerrain.Void);
            grid.GetTile(new Vector2Int(8, 5)).SetTerrain(CometTileTerrain.Void);
        }

        private void CreateGridVisuals()
        {
            tileRenderers = new SpriteRenderer[gridWidth, gridHeight];
            overlayRenderers = new SpriteRenderer[gridWidth, gridHeight];

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);
                    tileRenderers[x, y] = CreateTileRenderer($"Tile {x},{y}", x, y, 0f, GetTerrainColor(comet.Grid.GetTile(coordinate)));
                    overlayRenderers[x, y] = CreateTileRenderer($"Overlay {x},{y}", x, y, -0.1f, Color.clear);
                    overlayRenderers[x, y].transform.localScale = Vector3.one * tileSize * 0.82f;
                }
            }
        }

        private SpriteRenderer CreateTileRenderer(string objectName, int x, int y, float z, Color color)
        {
            GameObject tileObject = new GameObject(objectName);
            tileObject.transform.SetParent(transform);
            tileObject.transform.position = GridToWorld(new Vector2Int(x, y), z);
            tileObject.transform.localScale = Vector3.one * tileSize * 0.94f;

            SpriteRenderer renderer = tileObject.AddComponent<SpriteRenderer>();
            renderer.sprite = whiteSprite;
            renderer.color = color;
            return renderer;
        }

        private void BeginMouseAction()
        {
            if (!TryGetMouseGridPosition(out Vector2Int coordinate))
            {
                statusMessage = "마우스가 배치도 밖에 있음";
                return;
            }

            if (mode == ConstructionTestMode.Facility)
            {
                TryPlaceFacility(coordinate);
                return;
            }

            isDraggingRail = true;
            railPreviewPath.Clear();
            AddRailPreviewCoordinate(coordinate);
            statusMessage = "레일 경로 드래그 중";
        }

        private void TryPlaceFacility(Vector2Int coordinate)
        {
            SimulationId id = new SimulationId($"test-structure-{facilityIndex:000}");
            bool placed = placementSystem.TryPlaceFacility(comet, id, testStructure, coordinate, out FacilityInstance facility);
            statusMessage = placed ? $"2x2 구조물 배치 성공: {coordinate}" : $"2x2 구조물 배치 실패: {coordinate}";
            if (placed)
            {
                facilityStoredItems[facility.Id] = 0;
                facilityIndex++;
            }

            RefreshTiles();
        }

        private void UpdateRailPreviewFromMouse()
        {
            if (TryGetMouseGridPosition(out Vector2Int coordinate))
            {
                AddRailPreviewCoordinate(coordinate);
            }
        }

        private void AddRailPreviewCoordinate(Vector2Int coordinate)
        {
            if (railPreviewPath.Count == 0)
            {
                railPreviewPath.Add(coordinate);
                railPreviewValid = placementSystem.CanPlaceRailPath(comet, railPreviewPath);
                return;
            }

            Vector2Int last = railPreviewPath[railPreviewPath.Count - 1];
            if (last == coordinate)
            {
                return;
            }

            int existingIndex = railPreviewPath.IndexOf(coordinate);
            if (existingIndex >= 0)
            {
                railPreviewPath.RemoveRange(existingIndex + 1, railPreviewPath.Count - existingIndex - 1);
                railPreviewValid = placementSystem.CanPlaceRailPath(comet, railPreviewPath);
                statusMessage = "레일 경로 되돌리기";
                return;
            }

            if (!IsAdjacent(last, coordinate))
            {
                statusMessage = "인접한 타일로 드래그해야 연결됨";
                return;
            }

            railPreviewPath.Add(coordinate);
            railPreviewValid = placementSystem.CanPlaceRailPath(comet, railPreviewPath);
        }

        private static bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            int distance = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
            return distance == 1;
        }

        private void ConfirmRailPreview()
        {
            if (mode != ConstructionTestMode.Rail || railPreviewPath.Count == 0)
            {
                return;
            }

            if (!railPreviewValid)
            {
                statusMessage = "배치 불가능한 레일 경로는 확정할 수 없음";
                return;
            }

            SimulationId lineId = new SimulationId($"rail-line-{comet.RailLines.Count:000}");
            bool placed = placementSystem.TryPlaceRailPath(comet, lineId, "rail", railPreviewPath, railIndex, out int placedCount, out RailLine railLine);
            if (placed)
            {
                railIndex += placedCount;
                if (IsNaturalSourceRailLine(railLine))
                {
                    CreateConveyorTestOrb(railLine);
                }

                statusMessage = $"레일 {placedCount}칸 건설 완료";
                railPreviewPath.Clear();
                isDraggingRail = false;
                railPreviewValid = false;
                RefreshTiles();
                TrySpawnProducedOrbsFromSatisfiedFacilities();
            }
            else
            {
                statusMessage = "레일 확정 실패";
            }
        }

        private void CreateConveyorTestOrb(RailLine railLine)
        {
            if (railLine == null || railLine.Path.Count == 0)
            {
                return;
            }

            GameObject orbObject = new GameObject($"Conveyor Test Orb {conveyorTestOrbs.Count:000}");
            orbObject.transform.SetParent(transform);
            orbObject.transform.position = GridToWorld(railLine.Path[0], -0.25f);
            orbObject.transform.localScale = Vector3.one * tileSize * 0.34f;

            SpriteRenderer renderer = orbObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite(32);
            renderer.color = conveyorTestOrbColor;

            conveyorTestOrbs.Add(new ConveyorTestOrb(railLine, renderer.transform));
        }

        private void UpdateConveyorTestOrbs(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || conveyorTestOrbs.Count == 0)
            {
                return;
            }

            for (int i = 0; i < conveyorTestOrbs.Count; i++)
            {
                ConveyorTestOrb orb = conveyorTestOrbs[i];
                if (orb.WaitingFacility != null)
                {
                    TryResumeWaitingOrb(orb);
                    continue;
                }

                if (orb.Line.Path.Count == 1)
                {
                    orb.Transform.position = GridToWorld(orb.Line.Path[0], -0.25f);
                    continue;
                }

                orb.Distance += conveyorTestSpeedTilesPerSecond * deltaSeconds;
                while (orb.Line.SegmentCount > 0 && orb.Distance >= orb.Line.SegmentCount)
                {
                    orb.Distance -= orb.Line.SegmentCount;
                    RailLine nextLine = FindNextRailLine(orb.Line);
                    if (nextLine == null)
                    {
                    FacilityInstance intakeFacility = FindConnectedFacilityIntake(orb.Line.End);
                    if (intakeFacility != null)
                    {
                        if (IsFacilityInputFull(intakeFacility))
                        {
                            HoldOrbAtFacilityInput(orb, intakeFacility);
                            break;
                        }

                        DeliverOrbToFacility(orb, intakeFacility);
                        break;
                    }

                        continue;
                    }

                    orb.Line = nextLine;
                    conveyorHandoffCount++;
                    statusMessage = $"컨베이어 전달: {nextLine.Id}";
                }

                if (orb.Line.Path.Count == 1)
                {
                    orb.Transform.position = GridToWorld(orb.Line.Path[0], -0.25f);
                    continue;
                }

                int segmentIndex = Mathf.FloorToInt(orb.Distance);
                float segmentT = orb.Distance - segmentIndex;
                Vector3 from = GridToWorld(orb.Line.Path[segmentIndex], -0.25f);
                Vector3 to = GridToWorld(orb.Line.Path[segmentIndex + 1], -0.25f);
                orb.Transform.position = Vector3.Lerp(from, to, segmentT);
            }
        }

        private void DeliverOrbToFacility(ConveyorTestOrb orb, FacilityInstance facility)
        {
            int stored = facilityStoredItems.TryGetValue(facility.Id, out int current) ? current : 0;
            facilityStoredItems[facility.Id] = stored + 1;
            facilityDeliveryCount++;
            orb.Distance = 0f;
            orb.WaitingFacility = null;
            orb.Transform.position = GridToWorld(orb.Line.Start, -0.25f);

            statusMessage = $"건물 입고: {facility.Id} ({facilityStoredItems[facility.Id]}/3)";
            RefreshTiles();
            TrySpawnProducedOrbFromFacility(facility);
        }

        private void HoldOrbAtFacilityInput(ConveyorTestOrb orb, FacilityInstance facility)
        {
            orb.WaitingFacility = facility;
            orb.Distance = orb.Line.SegmentCount;
            orb.Transform.position = GridToWorld(orb.Line.End, -0.25f);
            statusMessage = $"건물 입력 대기: {facility.Id} 내부 스택 가득 참";
        }

        private void TryResumeWaitingOrb(ConveyorTestOrb orb)
        {
            if (orb.WaitingFacility == null)
            {
                return;
            }

            if (IsFacilityInputFull(orb.WaitingFacility))
            {
                orb.Transform.position = GridToWorld(orb.Line.End, -0.25f);
                TrySpawnProducedOrbFromFacility(orb.WaitingFacility);
                return;
            }

            DeliverOrbToFacility(orb, orb.WaitingFacility);
        }

        private bool IsFacilityInputFull(FacilityInstance facility)
        {
            return facility != null
                && facilityStoredItems.TryGetValue(facility.Id, out int storedItems)
                && storedItems >= 3;
        }

        private void TrySpawnProducedOrbsFromSatisfiedFacilities()
        {
            for (int i = 0; i < comet.Facilities.Count; i++)
            {
                TrySpawnProducedOrbFromFacility(comet.Facilities[i]);
            }
        }

        private void TrySpawnProducedOrbFromFacility(FacilityInstance facility)
        {
            if (facility == null || !facilityStoredItems.TryGetValue(facility.Id, out int storedItems) || storedItems < 3)
            {
                return;
            }

            for (int i = 0; i < comet.RailLines.Count; i++)
            {
                RailLine candidate = comet.RailLines[i];
                if (!IsRailStartOnFacilityBottomOutput(facility, candidate.Start))
                {
                    continue;
                }

                facilityStoredItems[facility.Id] = storedItems - 3;
                CreateProducedOrb(candidate);
                facilityProductionCount++;
                RefreshTiles();
                statusMessage = $"건물 생산 배출: {facility.Id} -> {candidate.Id}";
                return;
            }
        }

        private void CreateProducedOrb(RailLine railLine)
        {
            if (railLine == null || railLine.Path.Count == 0)
            {
                return;
            }

            GameObject orbObject = new GameObject($"Produced Orb {facilityProductionCount:000}");
            orbObject.transform.SetParent(transform);
            orbObject.transform.position = GridToWorld(railLine.Path[0], -0.25f);
            orbObject.transform.localScale = Vector3.one * tileSize * 0.42f;

            SpriteRenderer renderer = orbObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite(32);
            renderer.color = producedOrbColor;

            conveyorTestOrbs.Add(new ConveyorTestOrb(railLine, renderer.transform));
        }

        private bool IsNaturalSourceRailLine(RailLine railLine)
        {
            if (railLine == null || railLine.Path.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < comet.Facilities.Count; i++)
            {
                if (IsRailStartOnFacilityBottomOutput(comet.Facilities[i], railLine.Start))
                {
                    return false;
                }
            }

            return true;
        }

        private RailLine FindNextRailLine(RailLine currentLine)
        {
            if (currentLine == null || currentLine.Path.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < comet.RailLines.Count; i++)
            {
                RailLine candidate = comet.RailLines[i];
                if (candidate == currentLine || candidate.Path.Count == 0)
                {
                    continue;
                }

                if (IsSameOrAdjacent(currentLine.End, candidate.Start))
                {
                    return candidate;
                }
            }

            return null;
        }

        private FacilityInstance FindConnectedFacilityIntake(Vector2Int railEnd)
        {
            for (int i = 0; i < comet.Facilities.Count; i++)
            {
                FacilityInstance facility = comet.Facilities[i];
                Vector2Int footprint = facility.Definition.FootprintSize;

                for (int y = 0; y < footprint.y; y++)
                {
                    for (int x = 0; x < footprint.x; x++)
                    {
                        Vector2Int facilityTile = new Vector2Int(facility.Anchor.x + x, facility.Anchor.y + y);
                        if (IsSameOrAdjacent(railEnd, facilityTile))
                        {
                            return facility;
                        }
                    }
                }
            }

            return null;
        }

        private bool IsRailStartOnFacilityBottomOutput(FacilityInstance facility, Vector2Int railStart)
        {
            if (facility == null)
            {
                return false;
            }

            Vector2Int footprint = facility.Definition.FootprintSize;
            int bottomY = facility.Anchor.y - 1;
            if (railStart.y != bottomY)
            {
                return false;
            }

            return railStart.x >= facility.Anchor.x && railStart.x < facility.Anchor.x + footprint.x;
        }

        private static bool IsSameOrAdjacent(Vector2Int a, Vector2Int b)
        {
            int distance = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
            return distance <= 1;
        }

        private void CancelRailPreview(string message)
        {
            if (railPreviewPath.Count > 0 || isDraggingRail)
            {
                statusMessage = message;
            }

            railPreviewPath.Clear();
            isDraggingRail = false;
            railPreviewValid = false;
        }

        private void RefreshTiles()
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    CometTile tile = comet.Grid.GetTile(new Vector2Int(x, y));
                    tileRenderers[x, y].color = GetTileColor(tile);
                }
            }
        }

        private void RefreshOverlay()
        {
            if (overlayRenderers == null)
            {
                return;
            }

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    overlayRenderers[x, y].color = Color.clear;
                }
            }

            if (mode == ConstructionTestMode.Rail && railPreviewPath.Count > 0)
            {
                Color previewColor = railPreviewValid ? railPreviewColor : ghostInvalidColor;
                for (int i = 0; i < railPreviewPath.Count; i++)
                {
                    Vector2Int coordinate = railPreviewPath[i];
                    if (comet.Grid.Contains(coordinate))
                    {
                        overlayRenderers[coordinate.x, coordinate.y].color = previewColor;
                    }
                }

                return;
            }

            if (!TryGetMouseGridPosition(out Vector2Int mouseCoordinate))
            {
                return;
            }

            if (mode == ConstructionTestMode.Facility)
            {
                bool canPlace = comet.Grid.CanOccupy(mouseCoordinate, testStructure.FootprintSize);
                foreach (CometTile tile in comet.Grid.GetFootprintTiles(mouseCoordinate, testStructure.FootprintSize))
                {
                    overlayRenderers[tile.X, tile.Y].color = canPlace ? ghostValidColor : ghostInvalidColor;
                }
            }
            else
            {
                bool canPlace = comet.Grid.GetTile(mouseCoordinate).IsBuildable;
                overlayRenderers[mouseCoordinate.x, mouseCoordinate.y].color = canPlace ? ghostValidColor : ghostInvalidColor;
            }
        }

        private bool TryGetMouseGridPosition(out Vector2Int coordinate)
        {
            coordinate = default;
            if (mainCamera == null)
            {
                return false;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            Vector2 mousePosition = mouse.position.ReadValue();
            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0f));
            coordinate = WorldToGrid(world);
            return comet != null && comet.Grid.Contains(coordinate);
        }

        private Vector3 GridToWorld(Vector2Int coordinate, float z)
        {
            float originX = -(gridWidth - 1) * tileSize * 0.5f;
            float originY = -(gridHeight - 1) * tileSize * 0.5f;
            return new Vector3(originX + coordinate.x * tileSize, originY + coordinate.y * tileSize, z);
        }

        private Vector2Int WorldToGrid(Vector3 world)
        {
            float originX = -(gridWidth - 1) * tileSize * 0.5f;
            float originY = -(gridHeight - 1) * tileSize * 0.5f;
            int x = Mathf.RoundToInt((world.x - originX) / tileSize);
            int y = Mathf.RoundToInt((world.y - originY) / tileSize);
            return new Vector2Int(x, y);
        }

        private Color GetTileColor(CometTile tile)
        {
            if (tile.Occupant == CometTileOccupant.Facility)
            {
                FacilityInstance facility = FindFacilityById(tile.OccupantId);
                if (facility != null && facilityStoredItems.TryGetValue(facility.Id, out int storedItems) && storedItems >= 3)
                {
                    return facilitySatisfiedColor;
                }

                return facilityWaitingColor;
            }

            if (tile.Occupant == CometTileOccupant.Rail)
            {
                return railColor;
            }

            return GetTerrainColor(tile);
        }

        private FacilityInstance FindFacilityById(SimulationId id)
        {
            for (int i = 0; i < comet.Facilities.Count; i++)
            {
                if (comet.Facilities[i].Id.Equals(id))
                {
                    return comet.Facilities[i];
                }
            }

            return null;
        }

        private Color GetTerrainColor(CometTile tile)
        {
            switch (tile.Terrain)
            {
                case CometTileTerrain.Rough:
                    return roughColor;
                case CometTileTerrain.Cracked:
                    return new Color(0.24f, 0.13f, 0.16f, 1f);
                case CometTileTerrain.Void:
                    return voidColor;
                default:
                    return stableColor;
            }
        }

        private Sprite CreateWhiteSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size);
            float radius = (size - 1) * 0.5f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void ClearVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private sealed class ConveyorTestOrb
        {
            public ConveyorTestOrb(RailLine line, Transform transform)
            {
                Line = line;
                Transform = transform;
            }

            public RailLine Line { get; set; }
            public Transform Transform { get; }
            public float Distance { get; set; }
            public FacilityInstance WaitingFacility { get; set; }
        }
    }
}
