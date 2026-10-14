using System;
using System.Collections.Generic;
using CometFoundry.Data;

namespace CometFoundry.Core
{
    [Serializable]
    public sealed class Inventory
    {
        private readonly Dictionary<ResourceDefinition, double> amounts = new Dictionary<ResourceDefinition, double>();

        public Inventory(double capacityStacks)
        {
            CapacityStacks = Math.Max(0d, capacityStacks);
        }

        public double CapacityStacks { get; private set; }
        public double UsedStacks { get; private set; }
        public double FreeStacks => Math.Max(0d, CapacityStacks - UsedStacks);
        public double CapacityTons => CapacityStacks;
        public double UsedTons => UsedStacks;
        public double FreeTons => FreeStacks;
        public IReadOnlyDictionary<ResourceDefinition, double> Amounts => amounts;

        public void SetCapacity(double capacityStacks)
        {
            CapacityStacks = Math.Max(0d, capacityStacks);
        }

        public double GetAmount(ResourceDefinition resource)
        {
            if (resource == null)
            {
                return 0d;
            }

            return amounts.TryGetValue(resource, out double amount) ? amount : 0d;
        }

        public double Add(ResourceDefinition resource, double requestedStacks)
        {
            return AddStacks(resource, requestedStacks);
        }

        public double AddStacks(ResourceDefinition resource, double requestedStacks)
        {
            if (resource == null || requestedStacks <= 0d)
            {
                return 0d;
            }

            double accepted = Math.Min(requestedStacks, FreeStacks);
            if (accepted <= 0d)
            {
                return 0d;
            }

            amounts[resource] = GetAmount(resource) + accepted;
            UsedStacks += accepted;
            return accepted;
        }

        public double Remove(ResourceDefinition resource, double requestedStacks)
        {
            return RemoveStacks(resource, requestedStacks);
        }

        public double RemoveStacks(ResourceDefinition resource, double requestedStacks)
        {
            if (resource == null || requestedStacks <= 0d)
            {
                return 0d;
            }

            double current = GetAmount(resource);
            double removed = Math.Min(current, requestedStacks);
            if (removed <= 0d)
            {
                return 0d;
            }

            double remaining = current - removed;
            if (remaining <= 0.0001d)
            {
                amounts.Remove(resource);
            }
            else
            {
                amounts[resource] = remaining;
            }

            UsedStacks = Math.Max(0d, UsedStacks - removed);
            return removed;
        }

        public bool Has(ResourceDefinition resource, double stacks)
        {
            return HasStacks(resource, stacks);
        }

        public bool HasStacks(ResourceDefinition resource, double stacks)
        {
            return GetAmount(resource) >= stacks;
        }
    }
}
