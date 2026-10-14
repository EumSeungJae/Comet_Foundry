using System;
using System.Collections.Generic;
using CometFoundry.Core;
using CometFoundry.Data;
using UnityEngine;

namespace CometFoundry.Simulation
{
    [Serializable]
    public sealed class FacilityInstance
    {
        public FacilityInstance(SimulationId id, FacilityDefinition definition)
            : this(id, definition, Vector2Int.zero)
        {
        }

        public FacilityInstance(SimulationId id, FacilityDefinition definition, Vector2Int anchor)
        {
            Id = id;
            Definition = definition;
            Anchor = anchor;
            Inventory = new Inventory(definition != null ? definition.StorageCapacityStacks : 0d);
        }

        public SimulationId Id { get; }
        public FacilityDefinition Definition { get; }
        public Vector2Int Anchor { get; }
        public Inventory Inventory { get; }
        public RecipeDefinition ActiveRecipe { get; private set; }
        public double ProgressDays { get; private set; }

        public bool AssignRecipe(RecipeDefinition recipe)
        {
            if (Definition == null || recipe == null)
            {
                return false;
            }

            for (int i = 0; i < Definition.SupportedRecipes.Count; i++)
            {
                if (Definition.SupportedRecipes[i] == recipe)
                {
                    if (ActiveRecipe == recipe) return true;
                    ActiveRecipe = recipe;
                    ProgressDays = 0d;
                    return true;
                }
            }

            return false;
        }

        public void ClearRecipe()
        {
            ActiveRecipe = null;
            ProgressDays = 0d;
        }

        public void RestoreProgress(double days)
        {
            ProgressDays = ActiveRecipe == null || double.IsNaN(days) || double.IsInfinity(days)
                ? 0d : Math.Max(0d, Math.Min(days, ActiveRecipe.DurationDays));
        }

        public bool TickProduction(double deltaDays)
        {
            return TickProduction(deltaDays, null);
        }

        // Costs and materials commit together at completion. A blocked batch keeps its progress.
        public bool TickProduction(double deltaDays, CompanyState company)
        {
            if (deltaDays <= 0d || double.IsNaN(deltaDays) || double.IsInfinity(deltaDays)
                || GetProductionBlockReason(company) != null) return false;
            ProgressDays += deltaDays;
            bool completed = false;
            while (ProgressDays + 1e-10d >= ActiveRecipe.DurationDays)
            {
                if (GetProductionBlockReason(company) != null) { ProgressDays = Math.Min(ProgressDays, ActiveRecipe.DurationDays); break; }
                if (ActiveRecipe.ProductionCost > 0d && !company.Spend(ActiveRecipe.ProductionCost)) break;
                ConsumeInputs();
                ProduceOutputs();
                ProgressDays = Math.Max(0d, ProgressDays - ActiveRecipe.DurationDays);
                completed = true;
            }
            return completed;
        }

        public string GetProductionBlockReason(CompanyState company = null)
        {
            if (ActiveRecipe == null) return "생산품 미설정";
            if (ActiveRecipe.DurationDays <= 0d || double.IsNaN(ActiveRecipe.DurationDays)
                || double.IsInfinity(ActiveRecipe.DurationDays)) return "잘못된 생산 시간";
            var required = new Dictionary<ResourceDefinition, double>();
            double consumed = 0d, produced = 0d;
            foreach (ResourceQuantity input in ActiveRecipe.Inputs)
            {
                if (input.Resource == null || input.Stacks <= 0d || double.IsNaN(input.Stacks) || double.IsInfinity(input.Stacks)) return "잘못된 재료 데이터";
                required.TryGetValue(input.Resource, out double amount);
                required[input.Resource] = amount + input.Stacks;
                consumed += input.Stacks;
            }
            foreach (var input in required)
                if (!Inventory.HasStacks(input.Key, input.Value)) return "재료 대기: " + input.Key.DisplayName;
            foreach (ResourceQuantity output in ActiveRecipe.Outputs)
            {
                if (output.Resource == null || output.Stacks <= 0d || double.IsNaN(output.Stacks) || double.IsInfinity(output.Stacks)) return "잘못된 완성품 데이터";
                produced += output.Stacks;
            }
            if (Inventory.FreeStacks + consumed + 1e-9d < produced) return "보관 공간 부족";
            if (ActiveRecipe.ProductionCost > 0d && (company == null || company.Credits < ActiveRecipe.ProductionCost)) return "솔라 부족";
            return null;
        }

        private void ConsumeInputs()
        {
            foreach (ResourceQuantity input in ActiveRecipe.Inputs)
            {
                Inventory.RemoveStacks(input.Resource, input.Stacks);
            }
        }

        private void ProduceOutputs()
        {
            foreach (ResourceQuantity output in ActiveRecipe.Outputs)
            {
                Inventory.AddStacks(output.Resource, output.Stacks);
            }
        }
    }
}
