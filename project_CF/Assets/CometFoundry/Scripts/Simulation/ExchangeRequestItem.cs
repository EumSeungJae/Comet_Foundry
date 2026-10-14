using System;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    [Serializable]
    public sealed class ExchangeRequestItem
    {
        public ExchangeRequestItem(ResourceDefinition resource, double requiredStacks, double unitPrice)
        {
            Resource = resource;
            RequiredStacks = Math.Max(0d, requiredStacks);
            UnitPrice = Math.Max(0d, unitPrice);
        }

        public ResourceDefinition Resource { get; }
        public double RequiredStacks { get; }
        public double RequiredTons => RequiredStacks;
        public double UnitPrice { get; }
        public double TotalPayment => RequiredStacks * UnitPrice;
    }
}
