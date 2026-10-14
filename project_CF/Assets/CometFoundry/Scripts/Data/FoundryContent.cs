using System;
using System.Collections.Generic;
using UnityEngine;

namespace CometFoundry.Data
{
    // Initial playable balance. Times are real seconds at 1x (300 seconds per game day).
    public sealed class FoundryContent : IDisposable
    {
        public readonly List<ResourceDefinition> Resources = new List<ResourceDefinition>();
        public readonly List<RecipeDefinition> Recipes = new List<RecipeDefinition>();
        public readonly List<FacilityDefinition> Buildings = new List<FacilityDefinition>();
        public CargoShipDefinition Courier { get; private set; }
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

        public FoundryContent()
        {
            ResourceDefinition ironOre = Resource("iron-ore", "철광석", 0, 12);
            ResourceDefinition copperOre = Resource("copper-ore", "구리광석", 0, 16);
            ResourceDefinition ice = Resource("ice", "얼음", 0, 8);
            ResourceDefinition iron = Resource("iron", "철괴", 1, 48);
            ResourceDefinition copper = Resource("copper", "구리괴", 1, 64);
            ResourceDefinition water = Resource("water", "물", 1, 24);
            ResourceDefinition plate = Resource("plate", "철판", 2, 110);
            ResourceDefinition wire = Resource("wire", "구리선", 2, 95);
            ResourceDefinition gear = Resource("gear", "기어", 3, 185);
            ResourceDefinition frame = Resource("frame", "프레임", 3, 320);
            ResourceDefinition circuit = Resource("circuit", "회로기판", 3, 260);
            ResourceDefinition mechanical = Resource("mechanical", "기계부품", 4, 720);
            ResourceDefinition electronic = Resource("electronic", "전자부품", 4, 780);
            ResourceDefinition equipment = Resource("equipment", "산업장비", 5, 2400);
            ResourceDefinition drone = Resource("advanced-drone", "고급 드론", 5, 2800);
            ResourceDefinition consumer = Resource("consumer", "생활소비재", 5, 520);

            Recipe("extract-iron", "철광석 채굴", 4, 0, "제련에 쓰이는 철광석. 노드의 매장량을 소비합니다.", new ResourceQuantity[0], Q(ironOre, 4));
            Recipe("extract-copper", "구리광석 채굴", 4, 0, "전선과 전자부품의 원재료. 노드의 매장량을 소비합니다.", new ResourceQuantity[0], Q(copperOre, 4));
            Recipe("extract-ice", "얼음 채취", 4, 0, "물과 생활소비재의 원재료. 노드의 매장량을 소비합니다.", new ResourceQuantity[0], Q(ice, 4));
            Recipe("smelt-iron", "철괴 제련", 5, 4, "철판·기어·프레임 생산의 기초 소재.", Q(ironOre, 3), Q(iron, 2));
            Recipe("smelt-copper", "구리괴 제련", 5, 5, "구리선 생산의 기초 소재.", Q(copperOre, 3), Q(copper, 2));
            Recipe("melt-ice", "물 정제", 4, 2, "얼음을 정제한 물. 생활소비재 조립에 사용합니다.", Q(ice, 2), Q(water, 3));
            Recipe("roll-plate", "철판 압연", 6, 8, "회로기판과 산업용 프레임에 사용합니다.", Q(iron, 2), Q(plate, 2));
            Recipe("cut-gear", "기어 가공", 6, 10, "기계부품의 동력 전달 부품.", Q(iron, 2), Q(gear, 1));
            Recipe("make-frame", "프레임 가공", 8, 14, "기계부품과 고급 드론의 골격.", Q(plate, 2), Q(frame, 1));
            Recipe("draw-wire", "구리선 가공", 5, 6, "회로기판의 전도성 소재.", Q(copper, 2), Q(wire, 3));
            Recipe("make-circuit", "회로기판 제작", 8, 15, "전자부품과 고급 드론의 제어 회로.", Q(wire, 2, plate, 1), Q(circuit, 1));
            Recipe("make-mechanical", "기계부품 조립", 10, 25, "산업장비를 구성하는 기계 모듈.", Q(gear, 2, frame, 1), Q(mechanical, 1));
            Recipe("make-electronic", "전자부품 조립", 10, 25, "산업장비의 전자 제어 모듈.", Q(circuit, 2, wire, 1), Q(electronic, 1));
            Recipe("make-equipment", "산업장비 조립", 14, 60, "지구권 공장에 납품하는 완제품. 초기 납품 목표 품목입니다.", Q(mechanical, 1, electronic, 1), Q(equipment, 1));
            Recipe("make-drone", "고급 드론 조립", 16, 70, "지구권에 판매하는 정밀 드론 완제품.", Q(frame, 1, electronic, 2), Q(drone, 1));
            Recipe("make-consumer", "생활소비재 조립", 8, 18, "초기 현금 흐름을 확보하기 좋은 생활용품 묶음.", Q(plate, 1, water, 2), Q(consumer, 2));

            Building("mine-small", "소형 채굴기", FacilityType.Mine, 1, 1, 40, 1200, "extract-iron", "extract-copper", "extract-ice");
            Building("refinery-basic", "기초 정제소", FacilityType.Refinery, 1, 2, 60, 2200, "smelt-iron", "smelt-copper", "melt-ice");
            Building("factory-metal", "금속 가공소", FacilityType.Factory, 2, 2, 80, 3000, "roll-plate", "cut-gear", "make-frame");
            Building("factory-electronics", "전자 가공소", FacilityType.Factory, 2, 3, 80, 4000, "draw-wire", "make-circuit");
            Building("assembly-basic", "일반 조립소", FacilityType.Assembly, 2, 2, 100, 4500, "make-mechanical", "make-electronic", "make-equipment", "make-drone", "make-consumer");
            Building("warehouse-small", "소형 창고", FacilityType.Warehouse, 1, 2, 250, 2500);
            Building("market-terminal", "거래소 터미널", FacilityType.MarketTerminal, 1, 2, 100, 2500);
            Building("port-small", "소형 항구", FacilityType.LogisticsHub, 2, 3, 300, 5000);
            FacilityDefinition power = FacilityDefinition.CreatePrototypeLargePowerPlant();
            owned.Add(power); Buildings.Add(power);
            FacilityDefinition hangar = FacilityDefinition.CreatePrototypeDroneHangar();
            owned.Add(hangar); Buildings.Add(hangar);
            var storage = FacilityDefinition.CreatePrototypeDroneStorage();
            owned.Add(storage); Buildings.Add(storage);
            var smallPower = FacilityDefinition.CreatePrototypeSmallPowerPlant();
            owned.Add(smallPower); Buildings.Add(smallPower);
            Courier = ScriptableObject.CreateInstance<CargoShipDefinition>();
            Courier.ConfigurePrototype("courier", "소형 수송선", 100, 1, 0, 0);
            owned.Add(Courier);
        }

        public ResourceDefinition FindResource(string id) => Resources.Find(r => r.ResourceId == id);
        public RecipeDefinition FindRecipe(string id) => Recipes.Find(r => r.RecipeId == id);
        public FacilityDefinition FindBuilding(string id) => Buildings.Find(b => b.FacilityId == id);
        private ResourceDefinition Resource(string id, string label, int grade, double price)
        {
            var resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            resource.ConfigurePrototype(id, label, grade == 0 ? ResourceCategory.Raw : grade == 1 ? ResourceCategory.Refined : grade == 5 ? ResourceCategory.FinishedGood : ResourceCategory.Component, price, grade == 0, grade);
            Resources.Add(resource); owned.Add(resource); return resource;
        }
        private void Recipe(string id, string label, double seconds, double cost, string description, ResourceQuantity[] inputs, ResourceQuantity[] outputs)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            recipe.ConfigurePrototype(id, label, seconds / 300d, cost, description, inputs, outputs);
            Recipes.Add(recipe); owned.Add(recipe);
        }
        private void Building(string id, string label, FacilityType type, int w, int h, double capacity, double cost, params string[] recipeIds)
        {
            var building = ScriptableObject.CreateInstance<FacilityDefinition>();
            building.ConfigurePrototype(id, label, type, new Vector2Int(w, h), capacity, cost, 0d);
            var supported = new List<RecipeDefinition>();
            foreach (string recipeId in recipeIds) supported.Add(FindRecipe(recipeId));
            building.SetSupportedRecipes(supported); Buildings.Add(building); owned.Add(building);
        }
        private static ResourceQuantity[] Q(ResourceDefinition a, double count) => new[] { new ResourceQuantity(a, count) };
        private static ResourceQuantity[] Q(ResourceDefinition a, double count, ResourceDefinition b, double other) => new[] { new ResourceQuantity(a, count), new ResourceQuantity(b, other) };
        public void Dispose()
        {
            foreach (var asset in owned)
                if (Application.isPlaying) UnityEngine.Object.Destroy(asset); else UnityEngine.Object.DestroyImmediate(asset);
            owned.Clear();
        }
    }
}
