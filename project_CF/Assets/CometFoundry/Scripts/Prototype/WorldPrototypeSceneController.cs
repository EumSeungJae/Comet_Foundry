using System.Collections.Generic;
using CometFoundry.Data;
using CometFoundry.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CometFoundry.Prototype
{
    public sealed class WorldPrototypeSceneController : MonoBehaviour
    {
        [SerializeField] private int seed = 1739;
        [SerializeField] private int startingComets = 8;
        [SerializeField] private float cameraPanSpeed = 14f;
        [SerializeField] private float cameraZoomSpeed = 2.5f;
        [SerializeField] private float minCameraSize = 4f;
        [SerializeField] private float maxCameraSize = 18f;
        [SerializeField] private Color backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
        [SerializeField] private Color cometColor = new Color(0.58f, 0.68f, 0.78f, 1f);
        [SerializeField] private Color selectedCometColor = new Color(0.95f, 0.72f, 0.24f, 1f);
        [SerializeField] private Color emptyCometColor = new Color(0.22f, 0.25f, 0.3f, 1f);

        private readonly List<CometView> cometViews = new List<CometView>();

        private Camera mainCamera;
        private CometFoundryGame game;
        private CometGenerationProfile generationProfile;
        private Sprite circleSprite;
        private int generationIndex;
        private CometState selectedComet;
        private string statusMessage = "월드 프로토타입 준비";

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 0f, -10f);
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 11f;
                mainCamera.backgroundColor = backgroundColor;
            }

            game = GetComponent<CometFoundryGame>();
            if (game == null)
            {
                game = gameObject.AddComponent<CometFoundryGame>();
            }

            circleSprite = CreateCircleSprite(64);
            generationProfile = CreatePrototypeGenerationProfile();
            GenerateInitialWorld();
        }

        private void Update()
        {
            HandleCameraInput(Time.deltaTime);
            HandleSelectionInput();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
            {
                GenerateComet();
            }
        }

        private void OnGUI()
        {
            const int width = 430;
            GUILayout.BeginArea(new Rect(16, 16, width, 265), GUI.skin.box);
            GUILayout.Label("Comet Foundry 월드 테스트");
            GUILayout.Space(6);
            GUILayout.Label($"혜성 수: {game.Company.Comets.Count}");
            GUILayout.Label($"시드: {seed}");
            GUILayout.Label("WASD/방향키: 카메라 이동");
            GUILayout.Label("마우스 휠: 줌");
            GUILayout.Label("좌클릭: 혜성 선택");
            GUILayout.Label("G: 혜성 1개 추가 생성");
            GUILayout.Space(6);

            if (selectedComet == null)
            {
                GUILayout.Label("선택된 혜성: 없음");
            }
            else
            {
                GUILayout.Label($"선택된 혜성: {selectedComet.DisplayName}");
                GUILayout.Label($"시드코드: {selectedComet.SeedCode}");
                GUILayout.Label($"활성화/크기 등급: {selectedComet.Grade} / {selectedComet.SizeGrade}");
                GUILayout.Label($"위치: {FormatVector(selectedComet.Position)} / 반경: {selectedComet.Radius:0.0}");
                GUILayout.Label($"내부 보드: {selectedComet.Grid.Width} x {selectedComet.Grid.Height}");
                GUILayout.Label($"건설 가능 비율: {selectedComet.BuildableRatio:P0}");
                GUILayout.Label($"거리 코드: {selectedComet.DistanceCode} / {selectedComet.DistanceMinutes}분");
                GUILayout.Label($"매장 자원: {selectedComet.Deposits.Count}");
                for (int i = 0; i < selectedComet.Deposits.Count; i++)
                {
                    ResourceDeposit deposit = selectedComet.Deposits[i];
                    GUILayout.Label($"- {deposit.Resource.DisplayName}: {deposit.RemainingTons:0} t");
                }
            }

            GUILayout.Space(6);
            GUILayout.Label(statusMessage);
            GUILayout.EndArea();
        }

        private void GenerateInitialWorld()
        {
            ClearCometViews();
            generationIndex = 0;
            selectedComet = null;

            for (int i = 0; i < startingComets; i++)
            {
                GenerateComet();
            }

            statusMessage = $"{startingComets}개 혜성 생성 완료";
        }

        private void GenerateComet()
        {
            CometGenerator generator = new CometGenerator(seed + generationIndex * 97);
            CometState comet = generator.Generate(generationProfile, generationIndex, Vector2.zero, game.Company.Comets);
            game.Company.AddComet(comet);
            CreateCometView(comet);
            generationIndex++;
            statusMessage = $"{comet.DisplayName} 생성";
        }

        private void CreateCometView(CometState comet)
        {
            GameObject cometObject = new GameObject(comet.DisplayName);
            cometObject.transform.SetParent(transform);
            cometObject.transform.position = new Vector3(comet.Position.x, comet.Position.y, 0f);
            cometObject.transform.localScale = Vector3.one * Mathf.Max(0.7f, comet.Radius * 0.35f);

            SpriteRenderer renderer = cometObject.AddComponent<SpriteRenderer>();
            renderer.sprite = circleSprite;
            renderer.color = comet.Deposits.Count == 0 ? emptyCometColor : cometColor;

            cometViews.Add(new CometView(comet, renderer));
        }

        private void HandleCameraInput(float deltaSeconds)
        {
            if (mainCamera == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector2 pan = Vector2.zero;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    pan.x -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    pan.x += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    pan.y -= 1f;
                }

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    pan.y += 1f;
                }

                if (pan.sqrMagnitude > 1f)
                {
                    pan.Normalize();
                }

                Vector3 movement = new Vector3(pan.x, pan.y, 0f) * cameraPanSpeed * deltaSeconds;
                mainCamera.transform.position += movement;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.01f)
            {
                mainCamera.orthographicSize = Mathf.Clamp(
                    mainCamera.orthographicSize - Mathf.Sign(scrollY) * cameraZoomSpeed,
                    minCameraSize,
                    maxCameraSize);
            }
        }

        private void HandleSelectionInput()
        {
            if (mainCamera == null)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Vector2 mousePosition = mouse.position.ReadValue();
            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0f));
            selectedComet = FindCometAt(world);
            RefreshCometColors();
            statusMessage = selectedComet == null ? "빈 공간 선택" : $"{selectedComet.DisplayName} 선택";
        }

        private CometState FindCometAt(Vector3 world)
        {
            CometState closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < cometViews.Count; i++)
            {
                CometView view = cometViews[i];
                float pickRadius = Mathf.Max(0.7f, view.Comet.Radius * 0.35f);
                float distance = Vector2.Distance(world, view.Comet.Position);
                if (distance <= pickRadius && distance < closestDistance)
                {
                    closest = view.Comet;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private void RefreshCometColors()
        {
            for (int i = 0; i < cometViews.Count; i++)
            {
                CometView view = cometViews[i];
                if (view.Comet == selectedComet)
                {
                    view.Renderer.color = selectedCometColor;
                }
                else
                {
                    view.Renderer.color = view.Comet.Deposits.Count == 0 ? emptyCometColor : cometColor;
                }
            }
        }

        private CometGenerationProfile CreatePrototypeGenerationProfile()
        {
            CometGenerationProfile profile = ScriptableObject.CreateInstance<CometGenerationProfile>();
            ResourceDefinition ironOre = ScriptableObject.CreateInstance<ResourceDefinition>();
            ResourceDefinition copperOre = ScriptableObject.CreateInstance<ResourceDefinition>();
            ResourceDefinition ice = ScriptableObject.CreateInstance<ResourceDefinition>();

            ironOre.ConfigurePrototype("iron-ore", "철광석", ResourceCategory.Raw, 18d, true);
            copperOre.ConfigurePrototype("copper-ore", "구리광석", ResourceCategory.Raw, 24d, true);
            ice.ConfigurePrototype("ice", "얼음", ResourceCategory.Raw, 12d, true);

            ResourceDepositRoll ironRoll = new ResourceDepositRoll();
            ResourceDepositRoll copperRoll = new ResourceDepositRoll();
            ResourceDepositRoll iceRoll = new ResourceDepositRoll();

            ironRoll.ConfigurePrototype(ironOre, 0.85f, 120000d, 2200000d);
            copperRoll.ConfigurePrototype(copperOre, 0.55f, 80000d, 1400000d);
            iceRoll.ConfigurePrototype(ice, 0.45f, 100000d, 1800000d);

            profile.ConfigurePrototype(
                "prototype-world",
                new Vector2(2.8f, 8.5f),
                new Vector2Int(0, 3),
                new[] { ironRoll, copperRoll, iceRoll });
            return profile;
        }

        private void ClearCometViews()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            cometViews.Clear();
        }

        private static string FormatVector(Vector2 value)
        {
            return $"{value.x:0.0}, {value.y:0.0}";
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size);
            float radius = (size - 1) * 0.5f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float edge = Mathf.Clamp01(radius + 0.5f - distance);
                    float crater = Mathf.PerlinNoise(x * 0.13f, y * 0.13f) * 0.18f;
                    texture.SetPixel(x, y, new Color(0.85f - crater, 0.9f - crater, 1f - crater, edge));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private sealed class CometView
        {
            public CometView(CometState comet, SpriteRenderer renderer)
            {
                Comet = comet;
                Renderer = renderer;
            }

            public CometState Comet { get; }
            public SpriteRenderer Renderer { get; }
        }
    }
}
