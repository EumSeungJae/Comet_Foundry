using System.Collections.Generic;
using UnityEngine;

namespace CometFoundry.Data
{
    [CreateAssetMenu(menuName = "Comet Foundry/Data/Research")]
    public sealed class ResearchDefinition : ScriptableObject
    {
        [SerializeField] private string researchId;
        [SerializeField] private string displayName;
        [SerializeField] private double researchCost;
        [SerializeField] private List<ResearchDefinition> prerequisites = new List<ResearchDefinition>();

        public string ResearchId => researchId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public double ResearchCost => researchCost;
        public IReadOnlyList<ResearchDefinition> Prerequisites => prerequisites;
    }
}
