using CometFoundry.Core;
using CometFoundry.Data;
using CometFoundry.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace CometFoundry.Tests
{
    public sealed class MarketEventSystemTests
    {
        [Test]
        public void WarRumor_TemporarilyRaisesMilitaryGoodsOnly()
        {
            MarketSystem market = new MarketSystem();
            MarketEventSystem events = new MarketEventSystem(41);
            ResourceDefinition military = CreateResource("ammo", 100d, ResourceMarketTag.MilitaryGoods);
            ResourceDefinition civilian = CreateResource("food", 100d, ResourceMarketTag.None);

            events.TriggerWarRumor(market);

            Assert.AreEqual(MarketEventKind.WarRumor, events.ActiveEvent.Kind);
            Assert.AreEqual(115d, market.GetUnitPrice(military));
            Assert.AreEqual(100d, market.GetUnitPrice(civilian));
        }

        [Test]
        public void WarConfirmed_StartsAtOneStackAndRaisesMilitaryGoodsByThirtyToEightyPercent()
        {
            MarketSystem market = new MarketSystem();
            MarketEventSystem events = new MarketEventSystem(43);
            ResourceDefinition military = CreateResource("ammo", 100d, ResourceMarketTag.MilitaryGoods);

            events.TriggerWarConfirmed(market);

            Assert.AreEqual(MarketEventKind.WarConfirmed, events.ActiveEvent.Kind);
            Assert.AreEqual(1, events.ActiveEvent.StackCount);
            Assert.That(events.ActiveEvent.DemandMultiplier, Is.InRange(1.3d, 1.8d));
            Assert.That(market.GetUnitPrice(military), Is.InRange(130d, 180d));
        }

        [Test]
        public void WarEndChance_IncreasesByStack()
        {
            Assert.AreEqual(0.2d, MarketEventSystem.GetWarEndChance(1));
            Assert.AreEqual(0.4d, MarketEventSystem.GetWarEndChance(2));
            Assert.AreEqual(0.8d, MarketEventSystem.GetWarEndChance(3));
        }

        [Test]
        public void Update_RefreshesEventsEveryTwoGameWeeks()
        {
            MarketSystem market = new MarketSystem();
            MarketEventSystem events = new MarketEventSystem(47);
            SimulationClock clock = new SimulationClock();

            events.Update(clock, market);
            double firstRefreshDay = events.NextEventRefreshDay;
            clock.AdvanceSeconds(SimulationClock.DefaultRealSecondsPerGameDay * 13d);
            events.Update(clock, market);

            Assert.AreEqual(firstRefreshDay, events.NextEventRefreshDay);

            clock.AdvanceSeconds(SimulationClock.DefaultRealSecondsPerGameDay);
            events.Update(clock, market);

            Assert.AreEqual(28d, events.NextEventRefreshDay, 0.0001d);
        }

        private static ResourceDefinition CreateResource(string id, double baseMarketPrice, ResourceMarketTag tags)
        {
            ResourceDefinition resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            resource.ConfigurePrototype(id, id, ResourceCategory.Component, baseMarketPrice, false, 3, tags);
            return resource;
        }
    }
}
