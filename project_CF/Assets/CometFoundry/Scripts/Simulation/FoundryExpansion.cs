using System;
using System.Collections.Generic;
using System.Linq;
using CometFoundry.Core;
using CometFoundry.Data;
using UnityEngine;

namespace CometFoundry.Simulation
{
    public sealed class FoundryColony
    {
        public FoundryColony(CometState state, string name) { State = state; Name = name; }
        public CometState State { get; }
        public string Name { get; }
        public double Revenue { get; internal set; }
        internal DroneHaulRequest Flight;
        internal double FlightElapsed, FlightDuration;
        internal readonly Dictionary<FacilityInstance, double> LastSupply = new Dictionary<FacilityInstance, double>();
    }
    public sealed class FoundryProspect
    {
        public CometState State;
        public Vector2Int PortAnchor;
        public double Cost;
    }
    [Serializable] public sealed class FoundryHistoryPoint
    {
        public double day, value;
        public FoundryHistoryPoint(double time, double amount) { day = time; value = amount; }
    }
    public sealed class FoundryVoyage
    {
        public CargoShipState Ship;
        public FoundryColony Source, Destination;
        public double Duration;
    }
    public enum FoundryTutorialStep
    {
        BuildRefinery, SetIronRecipe, ProduceIron, BuildFactory, SetPlateRecipe, ProducePlate,
        BuildMarket, SellMaterial, BuyMaterial, Complete
    }

    public sealed partial class FoundrySession
    {
        public const double SurveyCost = 1500, SurveySeconds = 8, ShipPurchaseCost = 12000;
        public const double DaysPerMonth = 30, RealSecondsPerDay = 5;
        private readonly List<FoundryColony> colonies = new List<FoundryColony>();
        private readonly List<FoundryProspect> prospects = new List<FoundryProspect>();
        private readonly List<FoundryVoyage> voyages = new List<FoundryVoyage>();
        private readonly List<FoundryHistoryPoint> balances = new List<FoundryHistoryPoint>();
        private int activeColony, surveyCount;
        private double surveyRemaining;
        public IReadOnlyList<FoundryColony> Colonies => colonies;
        public IReadOnlyList<FoundryProspect> Prospects => prospects;
        public IReadOnlyList<FoundryVoyage> Voyages => voyages;
        public IReadOnlyList<FoundryHistoryPoint> Balances => balances;
        public FoundryColony ActiveColony => colonies[activeColony];
        public int ActiveColonyIndex => activeColony;
        public MarketSystem Market { get; } = new MarketSystem(1977);
        public double Day => Math.Round(Seconds, 6) / RealSecondsPerDay;
        public int Month => (int)(Day / DaysPerMonth);
        public string CalendarDate => DateAt(Day);
        public int DaysUntilMarketRefresh => (int)Math.Ceiling((Month + 1) * DaysPerMonth - Day);
        public static string DateAt(double day)
        {
            if (day < 0) return $"개척 전 {Math.Ceiling(-day):0}일";
            int elapsed = (int)Math.Floor(day), month = elapsed / (int)DaysPerMonth;
            return $"{month / 12 + 1}년 {month % 12 + 1:00}월 {elapsed % (int)DaysPerMonth + 1:00}일";
        }
        public static string MonthAt(double day)
        {
            int month = (int)Math.Floor(day / DaysPerMonth);
            return month < 0 ? $"개척 전 {-month}개월" : $"{month / 12 + 1}년 {month % 12 + 1}월";
        }
        public bool IsExploring => surveyRemaining > 0;
        public double SurveyRemainingSeconds => surveyRemaining;
        public bool HasMarket => colonies.Any(c => c.State.Facilities.Any(f => f.Definition.FacilityType == FacilityType.MarketTerminal));
        public FoundryTutorialStep TutorialStep { get; private set; }
        public double TutorialIronMade { get; private set; }
        public double TutorialPlateMade { get; private set; }
        public int ManualPurchases { get; private set; }
        public int ManualSales { get; private set; }
        private void ResetExpansion()
        {
            surveyCount = 0; surveyRemaining = 0; prospects.Clear(); voyages.Clear(); balances.Clear();
            activeMissionId = null; completedMissionIds.Clear();
            MissionPracticeStep = FoundryMissionPracticeStep.Inactive; PracticePlateMade = 0;
            TutorialStep = FoundryTutorialStep.BuildRefinery;
            TutorialIronMade = TutorialPlateMade = 0; ManualPurchases = ManualSales = 0;
            Company.AddShip(new SimulationId("ship-0"), Content.Courier);
            RecordBalance(); RefreshMarket();
        }
        public bool SwitchColony(int index)
        {
            if (index < 0 || index >= colonies.Count) return false;
            activeColony = index; return true;
        }
        public double Stock(FoundryColony colony, ResourceDefinition resource)
            => colony.State.Facilities.Sum(f => f.Inventory.GetAmount(resource));
        public double TotalStock(ResourceDefinition resource) => colonies.Sum(c => Stock(c, resource));
        public double FreeStorage(FoundryColony colony)
            => colony.State.Facilities.Where(IsTradeStorage).Sum(f => f.Inventory.FreeStacks);
        private static bool IsTradeStorage(FacilityInstance facility)
            => facility.Definition.IsWarehouse || facility.Definition.FacilityType == FacilityType.LogisticsHub || facility.Definition.FacilityType == FacilityType.MarketTerminal;
        public double BuyUnitPrice(ResourceDefinition resource) => Market.GetPurchaseUnitPrice(Company, resource);
        public double SellUnitPrice(ResourceDefinition resource) => Market.GetSaleUnitPrice(Company, resource);
        public int MaxTradeQuantity(ResourceDefinition resource, bool buy)
        {
            if (resource == null || !HasMarket) return 0;
            return (int)Math.Min(10000, Math.Floor(buy
                ? Math.Min(FreeStorage(ActiveColony), Company.Credits / BuyUnitPrice(resource))
                : Stock(ActiveColony, resource)));
        }
        public string TradeError(ResourceDefinition resource, bool buy, int quantity)
        {
            if (!HasMarket) return "거래소 터미널을 먼저 건설하세요.";
            if (resource == null || !Content.Resources.Contains(resource)) return "소재를 선택하세요.";
            if (quantity <= 0) return "거래 수량은 1스택 이상이어야 합니다.";
            if (!buy && Stock(ActiveColony, resource) + 1e-8 < quantity) return "현재 혜성의 소재 재고가 부족합니다.";
            if (buy && FreeStorage(ActiveColony) + 1e-8 < quantity) return "창고·항구·거래소의 빈 보관 공간이 부족합니다.";
            if (buy && Company.Credits + 1e-8 < quantity * BuyUnitPrice(resource)) return "구입 솔라가 부족합니다.";
            return null;
        }
        public bool Trade(ResourceDefinition resource, bool buy, int quantity, out string message)
        {
            RefreshMarket(); message = TradeError(resource, buy, quantity);
            if (message != null) return false;
            double total = quantity * (buy ? BuyUnitPrice(resource) : SellUnitPrice(resource));
            if (buy)
            {
                if (!Company.Spend(total)) { message = "구입 솔라가 부족합니다."; return false; }
                double left = quantity;
                foreach (var facility in Comet.Facilities.Where(IsTradeStorage))
                { left -= facility.Inventory.AddStacks(resource, left); if (left <= 1e-8) break; }
                ManualPurchases++;
            }
            else
            {
                RemoveStock(ActiveColony, resource, quantity);
                Company.Earn(total); Sales += total; ActiveColony.Revenue += total; ManualSales++;
                if (resource.ResourceId == "equipment") EquipmentDelivered += quantity;
            }
            RecordBalance(); RefreshTutorial();
            message = $"{ActiveColony.Name}: {resource.DisplayName} {quantity}스택 {(buy ? "구입" : "판매")} 완료 · {total:N1} 솔라";
            return true;
        }
        private void RemoveStock(FoundryColony colony, ResourceDefinition resource, double quantity)
        {
            foreach (var facility in colony.State.Facilities)
            { quantity -= facility.Inventory.RemoveStacks(resource, quantity); if (quantity <= 1e-8) break; }
        }
        // Reproducible daily universe prices: saving/loading cannot reroll today's quote.
        // Market events are not implemented in the playable session yet.
        public string MarketEventAt(ResourceDefinition resource, double day) => null;
        public double PriceRatio(ResourceDefinition resource, double day)
        {
            double monthDay = Math.Floor(day / DaysPerMonth) * DaysPerMonth;
            int index = Content.Resources.IndexOf(resource) + 1;
            double normal = 0.20 * Math.Sin(monthDay / 29 + index * 1.71)
                + 0.11 * Math.Sin(monthDay / 9.7 + index * 0.37) + 0.045 * Math.Cos(monthDay / 3.1 + index);
            return 1 + Math.Max(-0.30, Math.Min(0.30, normal));
        }
        private void RefreshMarket()
        {
            foreach (var resource in Content.Resources) Market.SetPriceMultiplier(resource, PriceRatio(resource, Day));
        }
        public List<FoundryHistoryPoint> PriceHistory(ResourceDefinition resource, int months)
        {
            if (months != 6 && months != 12 && months != 18 && months != 24) throw new ArgumentOutOfRangeException(nameof(months));
            var points = new List<FoundryHistoryPoint>();
            double end = Month * DaysPerMonth;
            double start = end - months * DaysPerMonth;
            for (double day = start; day <= end; day += DaysPerMonth)
                points.Add(new FoundryHistoryPoint(day, resource.BaseMarketPrice * PriceRatio(resource, day)));
            return points;
        }
        private void RecordBalance()
        {
            if (balances.Count == 0 || (int)(balances[balances.Count - 1].day / DaysPerMonth) < Month)
                balances.Add(new FoundryHistoryPoint(Month * DaysPerMonth, Company.Credits));
            // Completed monthly snapshots remain unchanged by trades within a month.
        }
        public List<FoundryHistoryPoint> BalanceHistory(int months)
            => balances.Where(p => p.day >= Math.Max(0, (Month - months) * DaysPerMonth)).ToList();
        public bool Explore(out string message)
        {
            if (!HasMarket) { message = "거래소 터미널을 먼저 건설하세요."; return false; }
            if (IsExploring) { message = "이미 혜성을 탐사 중입니다."; return false; }
            if (!Company.Spend(SurveyCost)) { message = "탐사 솔라가 부족합니다."; return false; }
            prospects.Clear(); surveyCount++; surveyRemaining = SurveySeconds; RecordBalance();
            message = "탐사대를 파견했습니다. 탐사 완료 후 혜성 후보 3개를 비교할 수 있습니다."; return true;
        }
        private void FinishSurvey()
        {
            var profile = ScriptableObject.CreateInstance<CometGenerationProfile>();
            try
            {
                var rolls = Content.Resources.Where(r => r.CanBeMined).Select(r =>
                { var roll = new ResourceDepositRoll(); roll.ConfigurePrototype(r, 1, 1200, 12000); return roll; }).ToList();
                profile.ConfigurePrototype("playable-survey", new Vector2(3, 12), new Vector2Int(0, 3), rolls);
                var generator = new CometGenerator(1701 + surveyCount * 37);
                var existing = colonies.Select(c => c.State).ToList();
                for (int i = 0; i < 3; i++)
                {
                    FoundryProspect prospect = null;
                    for (int attempt = 0; attempt < 256 && prospect == null; attempt++)
                    {
                        var state = generator.Generate(profile, 1000 + surveyCount * 3 + i, Vector2.zero, existing);
                        if (TryFindPortAnchor(state, out var anchor))
                            prospect = new FoundryProspect { State = state, PortAnchor = anchor,
                                Cost = 8000 + state.Grid.Width * state.Grid.Height * 45 + state.DistanceMinutes * 5 + Content.FindBuilding("port-small").BuildCost };
                    }
                    if (prospect == null) throw new InvalidOperationException("항구 건설 가능한 혜성을 찾지 못했습니다.");
                    prospects.Add(prospect); existing.Add(prospect.State);
                }
            }
            finally { if (Application.isPlaying) UnityEngine.Object.Destroy(profile); else UnityEngine.Object.DestroyImmediate(profile); }
        }
        private bool TryFindPortAnchor(CometState state, out Vector2Int anchor)
        {
            var size = Content.FindBuilding("port-small").FootprintSize;
            for (int y = 0; y < state.Grid.Height; y++) for (int x = 0; x < state.Grid.Width; x++)
            {
                var tile = new Vector2Int(x, y);
                if (!state.Grid.CanOccupy(tile, size)) continue;
                if (state.Deposits.Any(d => d.Coordinate.x >= x && d.Coordinate.x < x + size.x && d.Coordinate.y >= y && d.Coordinate.y < y + size.y)) continue;
                anchor = tile; return true;
            }
            anchor = Vector2Int.zero; return false;
        }
        public bool Colonize(int offerIndex, out string message)
        {
            if (!HasMarket || offerIndex < 0 || offerIndex >= prospects.Count) { message = "개척할 탐사 후보를 선택하세요."; return false; }
            var offer = prospects[offerIndex];
            if (Company.Credits < offer.Cost) { message = "항구를 포함한 개척 솔라가 부족합니다."; return false; }
            if (!placement.TryPlaceFacility(offer.State, new SimulationId("facility-" + nextId++), Content.FindBuilding("port-small"), offer.PortAnchor, out _))
            { message = "항구를 건설할 공간이 없습니다."; return false; }
            Company.Spend(offer.Cost);
            colonies.Add(new FoundryColony(offer.State, "개척지 " + colonies.Count)); Company.AddComet(offer.State);
            prospects.Clear(); RecordBalance();
            message = "항구를 건설하고 새 혜성의 소유권을 확보했습니다. 혜성 목록에서 선택해 개발하세요."; return true;
        }
        public FacilityInstance Dock(FoundryColony colony)
            => FindDock(colony, colony == colonies[0]);
        private static FacilityInstance FindDock(FoundryColony colony, bool home)
            => colony.State.Facilities.FirstOrDefault(f => f.Definition.FacilityType == FacilityType.LogisticsHub)
                ?? (home ? colony.State.Facilities.FirstOrDefault(f => f.Definition.IsWarehouse) : null);
        public double ShippingSeconds(FoundryColony source, FoundryColony destination)
            => 8 + Math.Abs(source.State.DistanceMinutes - destination.State.DistanceMinutes) * 0.025;
        public double ShippingCost(FoundryColony source, FoundryColony destination, int quantity)
            => 25 + quantity * 0.5 + ShippingSeconds(source, destination) * 2;
        public bool PurchaseShip(out string message)
        {
            if (!HasMarket || !Company.Spend(ShipPurchaseCost)) { message = "거래소 또는 수송선 구매 솔라가 부족합니다."; return false; }
            Company.AddShip(new SimulationId("ship-" + Company.Ships.Count), Content.Courier); RecordBalance();
            message = "100스택 소형 수송선을 구입했습니다."; return true;
        }
        public string ShippingError(int shipIndex, int from, int to, ResourceDefinition resource, int quantity)
        {
            if (!HasMarket) return "거래소 터미널을 먼저 건설하세요.";
            if (from < 0 || to < 0 || from >= colonies.Count || to >= colonies.Count || from == to) return "서로 다른 출발·도착 혜성을 선택하세요.";
            if (shipIndex < 0 || shipIndex >= Company.Ships.Count || Company.Ships[shipIndex].Status != CargoShipStatus.Idle) return "대기 중인 수송선을 선택하세요.";
            if (resource == null || !Content.Resources.Contains(resource) || quantity <= 0) return "소재와 1스택 이상의 수량을 선택하세요.";
            if (Dock(colonies[from]) == null || Dock(colonies[to]) == null) return "출발·도착 혜성에 항구가 필요합니다. 시작 혜성의 기본 창고는 임시 선착장을 지원합니다.";
            if (Stock(colonies[from], resource) + 1e-8 < quantity) return "출발 혜성의 소재 재고가 부족합니다.";
            if (Company.Ships[shipIndex].Cargo.FreeStacks < quantity) return "수송선 적재 한도를 초과했습니다.";
            if (Dock(colonies[to]).Inventory.FreeStacks < quantity) return "도착 항구의 빈 보관 공간이 부족합니다.";
            if (Company.Credits < ShippingCost(colonies[from], colonies[to], quantity)) return "수송 솔라가 부족합니다.";
            return null;
        }
        public bool Dispatch(int shipIndex, int from, int to, ResourceDefinition resource, int quantity, out string message)
        {
            message = ShippingError(shipIndex, from, to, resource, quantity); if (message != null) return false;
            var source = colonies[from]; var destination = colonies[to]; var ship = Company.Ships[shipIndex];
            double duration = ShippingSeconds(source, destination);
            var route = new TransportRoute(new SimulationId("route-" + nextId++), Dock(source), Dock(destination), duration / RealSecondsPerDay);
            if (!ship.AssignRoute(route)) { message = "수송선을 배정할 수 없습니다."; return false; }
            Company.Spend(ShippingCost(source, destination, quantity)); RemoveStock(source, resource, quantity);
            ship.Cargo.AddStacks(resource, quantity); ship.MarkInTransit(); Company.AddRoute(route);
            voyages.Add(new FoundryVoyage { Ship = ship, Source = source, Destination = destination, Duration = duration }); RecordBalance();
            message = $"{source.Name} → {destination.Name}: {resource.DisplayName} {quantity}스택 운송 시작."; return true;
        }
        private void TickExpansion(double seconds)
        {
            RefreshMarket();
            if (IsExploring) { surveyRemaining = Math.Max(0, surveyRemaining - seconds); if (!IsExploring) FinishSurvey(); }
            for (int i = voyages.Count - 1; i >= 0; i--)
            {
                var voyage = voyages[i]; voyage.Ship.AdvanceTravelByDays(seconds / RealSecondsPerDay);
                if (voyage.Ship.Status != CargoShipStatus.Unloading) continue;
                var dock = Dock(voyage.Destination); if (dock == null) continue;
                foreach (var resource in Content.Resources)
                {
                    double delivered = dock.Inventory.AddStacks(resource, voyage.Ship.Cargo.GetAmount(resource));
                    voyage.Ship.Cargo.RemoveStacks(resource, delivered);
                }
                if (voyage.Ship.Cargo.UsedStacks <= 1e-8) { voyage.Ship.ClearRoute(); voyages.RemoveAt(i); }
            }
        }
        public void SkipTutorial() => TutorialStep = FoundryTutorialStep.Complete;
        public void RestartTutorial() { TutorialStep = FoundryTutorialStep.BuildRefinery; RefreshTutorial(); }
        public void RefreshTutorial()
        {
            RefreshMissionPractice();
            var facilities = colonies[0].State.Facilities;
            bool proceed = true;
            while (proceed && TutorialStep != FoundryTutorialStep.Complete)
            {
                switch (TutorialStep)
                {
                    case FoundryTutorialStep.BuildRefinery: proceed = facilities.Any(f => f.Definition.FacilityType == FacilityType.Refinery); break;
                    case FoundryTutorialStep.SetIronRecipe: proceed = facilities.Any(f => f.ActiveRecipe?.RecipeId == "smelt-iron"); break;
                    case FoundryTutorialStep.ProduceIron: proceed = TutorialIronMade >= 2; break;
                    case FoundryTutorialStep.BuildFactory: proceed = facilities.Any(f => f.Definition.FacilityId == "factory-metal"); break;
                    case FoundryTutorialStep.SetPlateRecipe: proceed = facilities.Any(f => f.ActiveRecipe?.RecipeId == "roll-plate"); break;
                    case FoundryTutorialStep.ProducePlate: proceed = TutorialPlateMade >= 2; break;
                    case FoundryTutorialStep.BuildMarket: proceed = facilities.Any(f => f.Definition.FacilityType == FacilityType.MarketTerminal); break;
                    case FoundryTutorialStep.SellMaterial: proceed = ManualSales > 0; break;
                    case FoundryTutorialStep.BuyMaterial: proceed = ManualPurchases > 0; break;
                }
                if (proceed) TutorialStep++;
            }
        }
    }
}
