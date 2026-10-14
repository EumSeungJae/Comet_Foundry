using System;
using CometFoundry.Core;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    public enum MarketEventKind
    {
        None,
        WarRumor,
        WarConfirmed
    }

    [Serializable]
    public sealed class MarketEventState
    {
        public MarketEventState(MarketEventKind kind, ResourceMarketTag affectedTag, int stackCount, double demandMultiplier)
        {
            Kind = kind;
            AffectedTag = affectedTag;
            StackCount = Math.Max(0, Math.Min(MarketEventSystem.MaxEventStacks, stackCount));
            DemandMultiplier = Math.Max(1d, demandMultiplier);
        }

        public MarketEventKind Kind { get; }
        public ResourceMarketTag AffectedTag { get; }
        public int StackCount { get; }
        public double DemandMultiplier { get; }
    }

    public sealed class MarketEventSystem
    {
        public const double EventRefreshIntervalDays = 14d;
        public const int MaxEventStacks = 3;

        private readonly Random random;
        private double nextEventRefreshDay;

        public MarketEventSystem(int seed = 0)
        {
            random = seed == 0 ? new Random() : new Random(seed);
        }

        public MarketEventState ActiveEvent { get; private set; }
        public double NextEventRefreshDay => nextEventRefreshDay;

        public void Update(SimulationClock clock, MarketSystem market)
        {
            if (clock == null || market == null)
            {
                return;
            }

            if (nextEventRefreshDay <= 0d || clock.Day >= nextEventRefreshDay)
            {
                Refresh(clock.Day, market);
            }
        }

        public void TriggerWarRumor(MarketSystem market)
        {
            SetActiveEvent(market, new MarketEventState(MarketEventKind.WarRumor, ResourceMarketTag.MilitaryGoods, 1, 1.15d));
        }

        public void TriggerWarConfirmed(MarketSystem market)
        {
            SetActiveEvent(market, CreateWarConfirmedEvent(1));
        }

        public void Refresh(double currentDay, MarketSystem market)
        {
            if (market == null)
            {
                return;
            }

            if (ActiveEvent != null && ActiveEvent.Kind == MarketEventKind.WarConfirmed)
            {
                if (RollEventEnds(ActiveEvent.StackCount))
                {
                    ClearActiveEvent(market);
                }
                else
                {
                    int nextStack = Math.Min(MaxEventStacks, ActiveEvent.StackCount + 1);
                    SetActiveEvent(market, CreateWarConfirmedEvent(nextStack));
                }
            }
            else if (ActiveEvent != null && ActiveEvent.Kind == MarketEventKind.WarRumor)
            {
                SetActiveEvent(market, CreateWarConfirmedEvent(1));
            }

            nextEventRefreshDay = currentDay + EventRefreshIntervalDays;
        }

        public void ClearActiveEvent(MarketSystem market)
        {
            if (ActiveEvent != null && market != null)
            {
                market.ClearEventDemandMultiplier(ActiveEvent.AffectedTag);
            }

            ActiveEvent = null;
        }

        public static double GetWarEndChance(int stackCount)
        {
            if (stackCount <= 1)
            {
                return 0.2d;
            }

            if (stackCount == 2)
            {
                return 0.4d;
            }

            return 0.8d;
        }

        private MarketEventState CreateWarConfirmedEvent(int stackCount)
        {
            int clampedStack = Math.Max(1, Math.Min(MaxEventStacks, stackCount));
            double demandMultiplier = 1d;
            for (int i = 0; i < clampedStack; i++)
            {
                demandMultiplier *= 1.3d + random.NextDouble() * 0.5d;
            }

            return new MarketEventState(MarketEventKind.WarConfirmed, ResourceMarketTag.MilitaryGoods, clampedStack, demandMultiplier);
        }

        private bool RollEventEnds(int stackCount)
        {
            return random.NextDouble() < GetWarEndChance(stackCount);
        }

        private void SetActiveEvent(MarketSystem market, MarketEventState marketEvent)
        {
            ActiveEvent = marketEvent;
            market.SetEventDemandMultiplier(marketEvent.AffectedTag, marketEvent.DemandMultiplier);
        }
    }
}
