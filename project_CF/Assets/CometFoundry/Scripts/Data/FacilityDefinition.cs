using System.Collections.Generic;
using UnityEngine;

namespace CometFoundry.Data
{
    public enum FacilityType
    {
        Mine,
        Refinery,
        Factory,
        Assembly,
        Warehouse,
        FluidTank,
        LogisticsHub,
        DroneHangar,
        PowerPlant,
        ResearchLab,
        MarketTerminal
    }

    [CreateAssetMenu(menuName = "Comet Foundry/Data/Facility")]
    public sealed class FacilityDefinition : ScriptableObject
    {
        [SerializeField] private string facilityId;
        [SerializeField] private string displayName;
        [SerializeField] private FacilityType facilityType;
        [SerializeField] private double buildCost;
        [SerializeField] private double storageCapacityTons;
        [SerializeField] private double operatingCostPerDay;
        [SerializeField] private double powerOutput;
        [SerializeField] private int droneCapacity;
        [SerializeField] private int droneLinkCapacity;
        [SerializeField] private Vector2Int footprintSize = Vector2Int.one;
        [SerializeField] private List<RecipeDefinition> supportedRecipes = new List<RecipeDefinition>();

        public string FacilityId => facilityId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public FacilityType FacilityType => facilityType;
        public double BuildCost => buildCost;
        public double StorageCapacityStacks => storageCapacityTons;
        public double StorageCapacityTons => StorageCapacityStacks;
        public double OperatingCostPerDay => operatingCostPerDay;
        public double PowerOutput => powerOutput;
        public int DroneCapacity => droneCapacity;
        public int DroneLinkCapacity => droneLinkCapacity;
        public Vector2Int FootprintSize => new Vector2Int(Mathf.Max(1, footprintSize.x), Mathf.Max(1, footprintSize.y));
        public int FootprintArea => FootprintSize.x * FootprintSize.y;
        public double BasePowerConsumption => facilityType == FacilityType.PowerPlant || facilityType == FacilityType.DroneHangar ? 0d : FootprintArea;
        public IReadOnlyList<RecipeDefinition> SupportedRecipes => supportedRecipes;
        public bool IsWarehouse => facilityType == FacilityType.Warehouse;
        public bool IsDroneHangar => facilityType == FacilityType.DroneHangar;
        public bool IsPowerPlant => facilityType == FacilityType.PowerPlant;

        public void SetSupportedRecipes(IEnumerable<RecipeDefinition> recipes)
        {
            supportedRecipes = recipes == null ? new List<RecipeDefinition>() : new List<RecipeDefinition>(recipes);
        }

        public void ConfigurePrototype(string id, string label, FacilityType type, Vector2Int footprint, double capacityStacks = 0d)
        {
            ConfigurePrototype(id, label, type, footprint, capacityStacks, 0d, 0d);
        }

        public void ConfigurePrototype(string id, string label, FacilityType type, Vector2Int footprint, double capacityStacks, double cost, double dailyOperatingCost)
        {
            ConfigurePrototype(id, label, type, footprint, capacityStacks, cost, dailyOperatingCost, 0d, 0, 0);
        }

        public void ConfigurePrototype(
            string id,
            string label,
            FacilityType type,
            Vector2Int footprint,
            double capacityStacks,
            double cost,
            double dailyOperatingCost,
            double generatedPower,
            int hangarDroneCapacity,
            int hangarLinkCapacity)
        {
            facilityId = id;
            displayName = label;
            facilityType = type;
            footprintSize = footprint;
            storageCapacityTons = capacityStacks;
            buildCost = cost;
            operatingCostPerDay = dailyOperatingCost;
            powerOutput = Mathf.Max(0f, (float)generatedPower);
            droneCapacity = Mathf.Max(0, hangarDroneCapacity);
            droneLinkCapacity = Mathf.Max(0, hangarLinkCapacity);
        }

        public static FacilityDefinition CreatePrototypeWarehouse()
        {
            FacilityDefinition warehouse = CreateInstance<FacilityDefinition>();
            warehouse.ConfigurePrototype(
                "warehouse-basic",
                "기본 창고",
                FacilityType.Warehouse,
                new Vector2Int(2, 2),
                500d,
                2500d,
                25d);
            return warehouse;
        }

        public static FacilityDefinition CreatePrototypeSmallPowerPlant()
        {
            return CreatePrototypePowerPlant("power-plant-small", "소형 발전소", new Vector2Int(1, 1), 4d);
        }

        public static FacilityDefinition CreatePrototypeMediumPowerPlant()
        {
            return CreatePrototypePowerPlant("power-plant-medium", "중형 발전소", new Vector2Int(1, 3), 16d);
        }

        public static FacilityDefinition CreatePrototypeLargePowerPlant()
        {
            return CreatePrototypePowerPlant("power-plant-large", "대형 발전소", new Vector2Int(2, 3), 32d);
        }

        public static FacilityDefinition CreatePrototypeDroneStorage()
        {
            var storage = CreateInstance<FacilityDefinition>();
            storage.ConfigurePrototype("drone-storage", "드론저장소", FacilityType.DroneHangar,
                new Vector2Int(1, 2), 0d, 1800d, 0d, 0d, 4, 4);
            return storage;
        }

        public static FacilityDefinition CreatePrototypeDroneHangar()
        {
            FacilityDefinition hangar = CreateInstance<FacilityDefinition>();
            hangar.ConfigurePrototype(
                "drone-hangar",
                "드론 격납고",
                FacilityType.DroneHangar,
                new Vector2Int(2, 2),
                0d,
                4000d,
                20d,
                0d,
                8,
                8);
            return hangar;
        }

        public static FacilityDefinition CreatePrototypePowerPlant(string id, string label, Vector2Int footprint, double generatedPower)
        {
            FacilityDefinition powerPlant = CreateInstance<FacilityDefinition>();
            powerPlant.ConfigurePrototype(
                id,
                label,
                FacilityType.PowerPlant,
                footprint,
                0d,
                generatedPower * 500d,
                generatedPower * 2d,
                generatedPower,
                0,
                0);
            return powerPlant;
        }
    }
}
