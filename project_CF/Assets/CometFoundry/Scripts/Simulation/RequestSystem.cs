using System;
using System.Collections.Generic;
using CometFoundry.Core;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    public sealed class RequestSystem
    {
        public const int MinDifficultyLevel = 0;
        public const int MaxDifficultyLevel = 5;
        public const double RefreshIntervalSeconds = 1800d;
        public const double BasePaymentMultiplier = 1.2d;
        public const double TrustPaymentMultiplierStep = 0.1d;

        private static readonly int[] trustExperienceByDifficulty = { 1, 2, 4, 8, 16, 32 };
        private static readonly RequestDifficultyRule[] difficultyRules =
        {
            new RequestDifficultyRule(1, 1, 0, 0),
            new RequestDifficultyRule(1, 2, 1, 2),
            new RequestDifficultyRule(1, 3, 2, 3),
            new RequestDifficultyRule(2, 4, 2, 3),
            new RequestDifficultyRule(3, 5, 3, 4),
            new RequestDifficultyRule(4, 6, 4, 5)
        };

        private readonly List<ExchangeRequest> openRequests = new List<ExchangeRequest>();
        private readonly Dictionary<ResourceDefinition, double> priceMultipliers = new Dictionary<ResourceDefinition, double>();
        private readonly Random random;
        private double nextRefreshDay;
        private int nextRequestNumber;

        public RequestSystem(int seed = 0)
        {
            random = seed == 0 ? new Random() : new Random(seed);
        }

        public IReadOnlyList<ExchangeRequest> OpenRequests => openRequests;
        public double NextRefreshDay => nextRefreshDay;

        public void SetPriceMultiplier(ResourceDefinition resource, double multiplier)
        {
            if (resource != null)
            {
                priceMultipliers[resource] = multiplier <= 0d ? 1d : multiplier;
            }
        }

        public double GetUnitMarketPrice(ResourceDefinition resource)
        {
            if (resource == null)
            {
                return 0d;
            }

            double multiplier = priceMultipliers.TryGetValue(resource, out double value) ? value : 1d;
            return resource.BaseMarketPrice * multiplier;
        }

        public double GetRequestPaymentMultiplier(CompanyState company)
        {
            int trustLevel = company == null ? ExchangeTrustState.MinLevel : company.ExchangeTrust.Level;
            return BasePaymentMultiplier + TrustPaymentMultiplierStep * trustLevel;
        }

        public int GetOpenRequestLimit(CompanyState company)
        {
            int trustLevel = company == null ? ExchangeTrustState.MinLevel : company.ExchangeTrust.Level;
            return Math.Min(6, 3 + trustLevel);
        }

        public int GetMinGeneratedDifficulty(CompanyState company)
        {
            int trustLevel = company == null ? ExchangeTrustState.MinLevel : company.ExchangeTrust.Level;
            return Math.Max(MinDifficultyLevel, trustLevel);
        }

        public int GetMaxGeneratedDifficulty(CompanyState company)
        {
            int trustLevel = company == null ? ExchangeTrustState.MinLevel : company.ExchangeTrust.Level;
            return Math.Min(MaxDifficultyLevel, trustLevel + 2);
        }

        public void Update(SimulationClock clock, CompanyState company, IReadOnlyList<ResourceDefinition> resources)
        {
            if (clock == null)
            {
                return;
            }

            if (nextRefreshDay <= 0d || clock.Day >= nextRefreshDay)
            {
                Refresh(company, resources, clock.Day);
            }
        }

        public void Refresh(CompanyState company, IReadOnlyList<ResourceDefinition> resources, double currentDay)
        {
            openRequests.Clear();

            int requestCount = GetOpenRequestLimit(company);
            for (int i = 0; i < requestCount; i++)
            {
                ExchangeRequest request = CreateRequest(company, resources);
                if (request != null)
                {
                    openRequests.Add(request);
                }
            }

            nextRefreshDay = currentDay + RefreshIntervalSeconds / SimulationClock.SecondsPerDay;
        }

        public bool CompleteRequest(CompanyState company, Inventory inventory, string requestId, out double payment)
        {
            payment = 0d;
            if (company == null || inventory == null || string.IsNullOrWhiteSpace(requestId))
            {
                return false;
            }

            int index = openRequests.FindIndex(request => request.RequestId == requestId);
            if (index < 0)
            {
                return false;
            }

            ExchangeRequest request = openRequests[index];
            Dictionary<ResourceDefinition, double> requiredAmounts = new Dictionary<ResourceDefinition, double>();
            foreach (ExchangeRequestItem item in request.Items)
            {
                if (item.Resource == null || item.RequiredStacks <= 0d)
                {
                    return false;
                }

                requiredAmounts[item.Resource] = requiredAmounts.TryGetValue(item.Resource, out double current) ? current + item.RequiredStacks : item.RequiredStacks;
            }

            foreach (KeyValuePair<ResourceDefinition, double> requirement in requiredAmounts)
            {
                if (!inventory.HasStacks(requirement.Key, requirement.Value))
                {
                    return false;
                }
            }

            foreach (KeyValuePair<ResourceDefinition, double> requirement in requiredAmounts)
            {
                inventory.RemoveStacks(requirement.Key, requirement.Value);
            }

            payment = request.TotalPayment;
            company.Earn(payment);
            company.ExchangeTrust.AddExperience(request.TrustExperienceReward);
            openRequests.RemoveAt(index);
            return true;
        }

        public static int GetTrustExperienceReward(int difficultyLevel)
        {
            int clampedLevel = Math.Max(MinDifficultyLevel, Math.Min(MaxDifficultyLevel, difficultyLevel));
            return trustExperienceByDifficulty[clampedLevel];
        }

        private ExchangeRequest CreateRequest(CompanyState company, IReadOnlyList<ResourceDefinition> resources)
        {
            if (resources == null || resources.Count == 0)
            {
                return null;
            }

            int difficulty = random.Next(GetMinGeneratedDifficulty(company), GetMaxGeneratedDifficulty(company) + 1);
            RequestDifficultyRule rule = GetDifficultyRule(difficulty);
            int itemCount = random.Next(rule.MinItemCount, rule.MaxItemCount + 1);
            List<ExchangeRequestItem> items = new List<ExchangeRequestItem>();

            for (int i = 0; i < itemCount; i++)
            {
                ResourceDefinition resource = SelectResourceByGrade(resources, rule.MinProcessingGrade, rule.MaxProcessingGrade);
                double unitPrice = GetUnitMarketPrice(resource) * GetRequestPaymentMultiplier(company);
                items.Add(new ExchangeRequestItem(resource, 1d, unitPrice));
            }

            double representativeUnitPrice = items.Count > 0 ? items[0].UnitPrice : 0d;
            string id = $"exchange-request-{nextRequestNumber++:0000}";

            return new ExchangeRequest(id, items, difficulty, representativeUnitPrice, GetTrustExperienceReward(difficulty));
        }

        public static RequestDifficultyRule GetDifficultyRule(int difficultyLevel)
        {
            int clampedLevel = Math.Max(MinDifficultyLevel, Math.Min(MaxDifficultyLevel, difficultyLevel));
            return difficultyRules[clampedLevel];
        }

        private ResourceDefinition SelectResourceByGrade(IReadOnlyList<ResourceDefinition> resources, int minGrade, int maxGrade)
        {
            List<ResourceDefinition> candidates = new List<ResourceDefinition>();
            foreach (ResourceDefinition resource in resources)
            {
                if (resource != null && resource.ProcessingGrade >= minGrade && resource.ProcessingGrade <= maxGrade)
                {
                    candidates.Add(resource);
                }
            }

            if (candidates.Count > 0)
            {
                return candidates[random.Next(candidates.Count)];
            }

            return resources[random.Next(resources.Count)];
        }
    }

    public readonly struct RequestDifficultyRule
    {
        public RequestDifficultyRule(int minItemCount, int maxItemCount, int minProcessingGrade, int maxProcessingGrade)
        {
            MinItemCount = minItemCount;
            MaxItemCount = maxItemCount;
            MinProcessingGrade = minProcessingGrade;
            MaxProcessingGrade = maxProcessingGrade;
        }

        public int MinItemCount { get; }
        public int MaxItemCount { get; }
        public int MinProcessingGrade { get; }
        public int MaxProcessingGrade { get; }
    }
}
