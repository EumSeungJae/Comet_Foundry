using UnityEngine;

namespace CometFoundry.Data
{
    [System.Flags]
    public enum ResourceMarketTag
    {
        None = 0,
        MilitaryGoods = 1 << 0
    }

    public enum ResourceCategory
    {
        Raw,
        Refined,
        Component,
        FinishedGood,
        Fuel,
        Science
    }

    [CreateAssetMenu(menuName = "Comet Foundry/Data/Resource")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        [SerializeField] private string resourceId;
        [SerializeField] private string displayName;
        [SerializeField] private ResourceCategory category;
        [SerializeField] private double baseMarketPrice;
        [SerializeField] private bool canBeMined;
        [SerializeField] private int processingGrade;
        [SerializeField] private ResourceMarketTag marketTags;
        [SerializeField] private double stackWeightTons = 1d;

        public string ResourceId => resourceId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public ResourceCategory Category => category;
        public double BaseMarketPrice => baseMarketPrice;
        public bool CanBeMined => canBeMined;
        public int ProcessingGrade => processingGrade;
        public ResourceMarketTag MarketTags => marketTags;
        public double StackWeightTons => stackWeightTons <= 0d ? 1d : stackWeightTons;

        public bool HasMarketTag(ResourceMarketTag tag)
        {
            return (marketTags & tag) == tag;
        }

        public double GetWeightForStacks(double stacks)
        {
            return Mathf.Max(0f, (float)stacks) * StackWeightTons;
        }

        public double GetStacksForWeight(double tons)
        {
            return Mathf.Max(0f, (float)tons) / StackWeightTons;
        }

        public void ConfigurePrototype(string id, string label, ResourceCategory resourceCategory, double marketPrice, bool mineable, int grade = 0, ResourceMarketTag tags = ResourceMarketTag.None, double weightPerStackTons = 1d)
        {
            resourceId = id;
            displayName = label;
            category = resourceCategory;
            baseMarketPrice = marketPrice;
            canBeMined = mineable;
            processingGrade = Mathf.Clamp(grade, 0, 5);
            marketTags = tags;
            stackWeightTons = weightPerStackTons <= 0d ? 1d : weightPerStackTons;
        }
    }
}
