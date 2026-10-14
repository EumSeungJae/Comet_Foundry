using UnityEngine;

namespace CometFoundry.Data
{
    [CreateAssetMenu(menuName = "Comet Foundry/Data/Cargo Ship")]
    public sealed class CargoShipDefinition : ScriptableObject
    {
        [SerializeField] private string shipId;
        [SerializeField] private string displayName;
        [SerializeField] private double cargoCapacityTons;
        [SerializeField] private double speedUnitsPerDay;
        [SerializeField] private double operatingCostPerDay;
        [SerializeField] private double fuelUsePerUnit;

        public string ShipId => shipId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public double CargoCapacityStacks => cargoCapacityTons;
        public double CargoCapacityTons => CargoCapacityStacks;
        public double SpeedUnitsPerDay => speedUnitsPerDay;
        public double OperatingCostPerDay => operatingCostPerDay;
        public double FuelUsePerUnit => fuelUsePerUnit;

        public void ConfigurePrototype(string id, string label, double capacityStacks, double speedPerDay, double costPerDay, double fuelPerUnit)
        {
            shipId = id;
            displayName = label;
            cargoCapacityTons = capacityStacks;
            speedUnitsPerDay = speedPerDay;
            operatingCostPerDay = costPerDay;
            fuelUsePerUnit = fuelPerUnit;
        }
    }
}
