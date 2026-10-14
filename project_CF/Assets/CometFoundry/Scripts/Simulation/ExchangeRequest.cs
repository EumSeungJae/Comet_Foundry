using System;
using System.Collections.Generic;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    [Serializable]
    public sealed class ExchangeRequest
    {
        private readonly List<ExchangeRequestItem> items;

        public ExchangeRequest(string requestId, IReadOnlyList<ExchangeRequestItem> deliveryItems, int difficultyLevel, double unitPrice, int trustExperienceReward)
        {
            RequestId = requestId;
            items = deliveryItems == null ? new List<ExchangeRequestItem>() : new List<ExchangeRequestItem>(deliveryItems);
            DifficultyLevel = Math.Max(RequestSystem.MinDifficultyLevel, Math.Min(RequestSystem.MaxDifficultyLevel, difficultyLevel));
            UnitPrice = Math.Max(0d, unitPrice);
            TrustExperienceReward = Math.Max(0, trustExperienceReward);
        }

        public string RequestId { get; }
        public IReadOnlyList<ExchangeRequestItem> Items => items;
        public ResourceDefinition Resource => items.Count > 0 ? items[0].Resource : null;
        public double RequiredStacks
        {
            get
            {
                double total = 0d;
                foreach (ExchangeRequestItem item in items)
                {
                    total += item.RequiredStacks;
                }

                return total;
            }
        }

        public double RequiredTons => RequiredStacks;
        public int DifficultyLevel { get; }
        public double UnitPrice { get; }
        public double TotalPayment
        {
            get
            {
                double total = 0d;
                foreach (ExchangeRequestItem item in items)
                {
                    total += item.TotalPayment;
                }

                return total;
            }
        }

        public int TrustExperienceReward { get; }
    }
}
