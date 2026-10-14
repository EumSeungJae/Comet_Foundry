using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class RequestSystemTests
    {
        [Test]
        public void Refresh_OpensThreeRequestsAtTrustLevelZero()
        {
            CompanyState company = new CompanyState(0d);
            RequestSystem requests = new RequestSystem(7);
            ResourceDefinition ore = CreateResource("ore", 100d, 0);
            ResourceDefinition ingot = CreateResource("ingot", 200d, 1);
            ResourceDefinition plate = CreateResource("plate", 300d, 2);

            requests.Refresh(company, new[] { ore, ingot, plate }, 0d);

            Assert.AreEqual(3, requests.OpenRequests.Count);
            Assert.GreaterOrEqual(requests.OpenRequests[0].DifficultyLevel, 0);
            Assert.LessOrEqual(requests.OpenRequests[0].DifficultyLevel, 2);
            AssertRuleMatchesRequest(requests.OpenRequests[0]);
        }

        [Test]
        public void TrustLevel_IncreasesRequestLimitDifficultyAndPaymentMultiplier()
        {
            CompanyState company = new CompanyState(0d);
            company.ExchangeTrust.AddExperience(100 + 300 + 900);
            RequestSystem requests = new RequestSystem(11);
            ResourceDefinition part = CreateResource("part", 400d, 4);
            ResourceDefinition product = CreateResource("product", 500d, 5);

            requests.Refresh(company, new[] { part, product }, 0d);

            Assert.AreEqual(ExchangeTrustState.MaxLevel, company.ExchangeTrust.Level);
            Assert.AreEqual(6, requests.OpenRequests.Count);
            Assert.GreaterOrEqual(requests.OpenRequests[0].DifficultyLevel, 3);
            Assert.LessOrEqual(requests.OpenRequests[0].DifficultyLevel, RequestSystem.MaxDifficultyLevel);
            Assert.AreEqual(1.5d * requests.OpenRequests[0].Resource.BaseMarketPrice, requests.OpenRequests[0].UnitPrice);
            AssertRuleMatchesRequest(requests.OpenRequests[0]);
        }

        [Test]
        public void Update_RefreshesEveryThirtySimulationMinutes()
        {
            CompanyState company = new CompanyState(0d);
            RequestSystem requests = new RequestSystem(13);
            SimulationClock clock = new SimulationClock();
            ResourceDefinition ore = CreateResource("ore", 100d, 0);

            requests.Update(clock, company, new[] { ore });
            string firstRequestId = requests.OpenRequests[0].RequestId;
            clock.AdvanceSeconds(RequestSystem.RefreshIntervalSeconds - 1d);
            requests.Update(clock, company, new[] { ore });

            Assert.AreEqual(firstRequestId, requests.OpenRequests[0].RequestId);

            clock.AdvanceSeconds(1d);
            requests.Update(clock, company, new[] { ore });

            Assert.AreNotEqual(firstRequestId, requests.OpenRequests[0].RequestId);
        }

        [Test]
        public void CompleteRequest_PaysAndAddsTrustExperience()
        {
            CompanyState company = new CompanyState(0d);
            Inventory inventory = new Inventory(20d);
            RequestSystem requests = new RequestSystem(17);
            ResourceDefinition ore = CreateResource("ore", 100d, 0);
            requests.Refresh(company, new[] { ore }, 0d);
            ExchangeRequest request = requests.OpenRequests[0];
            foreach (ExchangeRequestItem item in request.Items)
            {
                inventory.Add(item.Resource, item.RequiredTons);
            }

            bool completed = requests.CompleteRequest(company, inventory, request.RequestId, out double payment);

            Assert.IsTrue(completed);
            Assert.AreEqual(request.TotalPayment, payment);
            Assert.AreEqual(payment, company.Credits);
            Assert.AreEqual(0d, inventory.GetAmount(ore));
            Assert.AreEqual(RequestSystem.GetTrustExperienceReward(request.DifficultyLevel), company.ExchangeTrust.Experience);
        }

        [Test]
        public void DifficultyRules_MatchItemCountAndProcessingGradeRanges()
        {
            AssertRule(0, 1, 1, 0, 0);
            AssertRule(1, 1, 2, 1, 2);
            AssertRule(2, 1, 3, 2, 3);
            AssertRule(3, 2, 4, 2, 3);
            AssertRule(4, 3, 5, 3, 4);
            AssertRule(5, 4, 6, 4, 5);
        }

        private static ResourceDefinition CreateResource(string id, double baseMarketPrice, int processingGrade)
        {
            ResourceDefinition resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            resource.ConfigurePrototype(id, id, ResourceCategory.Raw, baseMarketPrice, true, processingGrade);
            return resource;
        }

        private static void AssertRuleMatchesRequest(ExchangeRequest request)
        {
            RequestDifficultyRule rule = RequestSystem.GetDifficultyRule(request.DifficultyLevel);
            Assert.That(request.Items.Count, Is.InRange(rule.MinItemCount, rule.MaxItemCount));
            foreach (ExchangeRequestItem item in request.Items)
            {
                Assert.That(item.Resource.ProcessingGrade, Is.InRange(rule.MinProcessingGrade, rule.MaxProcessingGrade));
            }
        }

        private static void AssertRule(int difficulty, int minItemCount, int maxItemCount, int minGrade, int maxGrade)
        {
            RequestDifficultyRule rule = RequestSystem.GetDifficultyRule(difficulty);
            Assert.AreEqual(minItemCount, rule.MinItemCount);
            Assert.AreEqual(maxItemCount, rule.MaxItemCount);
            Assert.AreEqual(minGrade, rule.MinProcessingGrade);
            Assert.AreEqual(maxGrade, rule.MaxProcessingGrade);
        }
    }
}
