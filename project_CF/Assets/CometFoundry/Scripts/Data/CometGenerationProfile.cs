using System;
using System.Collections.Generic;
using UnityEngine;

namespace CometFoundry.Data
{
    [Serializable]
    public sealed class ResourceDepositRoll
    {
        [SerializeField] private ResourceDefinition resource;
        [SerializeField, Range(0f, 1f)] private float probability = 0.5f;
        [SerializeField] private double minTons = 100000d;
        [SerializeField] private double maxTons = 2000000d;

        public ResourceDefinition Resource => resource;
        public float Probability => probability;
        public double MinStacks => minTons;
        public double MaxStacks => maxTons;
        public double MinTons => MinStacks;
        public double MaxTons => MaxStacks;

        public void ConfigurePrototype(ResourceDefinition resourceDefinition, float rollProbability, double minimumStacks, double maximumStacks)
        {
            resource = resourceDefinition;
            probability = Mathf.Clamp01(rollProbability);
            minTons = minimumStacks;
            maxTons = maximumStacks;
        }
    }

    [CreateAssetMenu(menuName = "Comet Foundry/Data/Comet Generation Profile")]
    public sealed class CometGenerationProfile : ScriptableObject
    {
        [SerializeField] private string profileId = "default";
        [SerializeField] private Vector2 sizeRange = new Vector2(3f, 12f);
        [SerializeField] private Vector2Int depositCountRange = new Vector2Int(0, 4);
        [SerializeField] private List<ResourceDepositRoll> deposits = new List<ResourceDepositRoll>();

        public string ProfileId => profileId;
        public Vector2 SizeRange => sizeRange;
        public Vector2Int DepositCountRange => depositCountRange;
        public IReadOnlyList<ResourceDepositRoll> Deposits => deposits;

        public void ConfigurePrototype(string id, Vector2 cometSizeRange, Vector2Int cometDepositCountRange, IEnumerable<ResourceDepositRoll> depositRolls)
        {
            profileId = id;
            sizeRange = cometSizeRange;
            depositCountRange = cometDepositCountRange;
            deposits.Clear();
            if (depositRolls == null)
            {
                return;
            }

            deposits.AddRange(depositRolls);
        }
    }
}
