using System;
using System.Collections.Generic;
using UnityEngine;

namespace CometFoundry.Data
{
    [Serializable]
    public sealed class ResourceQuantity
    {
        [SerializeField] private ResourceDefinition resource;
        [SerializeField] private double tons;

        public ResourceDefinition Resource => resource;
        public double Stacks => tons;
        public double Tons => Stacks;

        public ResourceQuantity() { }

        public ResourceQuantity(ResourceDefinition definition, double stacks)
        {
            resource = definition;
            tons = stacks;
        }
    }

    [CreateAssetMenu(menuName = "Comet Foundry/Data/Recipe")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        [SerializeField] private string recipeId;
        [SerializeField] private string displayName;
        [SerializeField] private double durationDays = 1d;
        [SerializeField] private double productionCost;
        [SerializeField, TextArea] private string description;
        [SerializeField] private List<ResourceQuantity> inputs = new List<ResourceQuantity>();
        [SerializeField] private List<ResourceQuantity> outputs = new List<ResourceQuantity>();

        public string RecipeId => recipeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public double DurationDays => durationDays;
        public double ProductionCost => productionCost;
        public string Description => description;
        public IReadOnlyList<ResourceQuantity> Inputs => inputs;
        public IReadOnlyList<ResourceQuantity> Outputs => outputs;

        public void ConfigurePrototype(string id, string label, double days, double cost,
            string productDescription, IEnumerable<ResourceQuantity> ingredients, IEnumerable<ResourceQuantity> products)
        {
            recipeId = id;
            displayName = label;
            durationDays = Math.Max(0.000001d, days);
            productionCost = Math.Max(0d, cost);
            description = productDescription;
            inputs = ingredients == null ? new List<ResourceQuantity>() : new List<ResourceQuantity>(ingredients);
            outputs = products == null ? new List<ResourceQuantity>() : new List<ResourceQuantity>(products);
        }
    }
}
