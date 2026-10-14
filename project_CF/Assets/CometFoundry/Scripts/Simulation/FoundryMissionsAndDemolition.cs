using System;
using System.Collections.Generic;
using System.Linq;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    public sealed class FoundryMission
    {
        public FoundryMission(string id, string name, string resource, int amount, double reward)
        { Id = id; Name = name; ResourceId = resource; Amount = amount; Reward = reward; }
        public string Id { get; }
        public string Name { get; }
        public string ResourceId { get; }
        public int Amount { get; }
        public double Reward { get; }
    }
    public sealed partial class FoundrySession
    {
        private static readonly FoundryMission[] missions = {
            new FoundryMission("tutorial-plate", "첫 가공 계약 · 철판 납품", "plate", 4, 1500),
            new FoundryMission("plate-order", "관문 외벽 보수", "plate", 12, 5000),
            new FoundryMission("wire-order", "관문 배선 정비", "wire", 18, 3500),
            new FoundryMission("gear-order", "수송 장치 기어 교체", "gear", 8, 4000),
            new FoundryMission("frame-order", "개척 장비 프레임 보강", "frame", 6, 5000),
            new FoundryMission("circuit-order", "제어 회로 교체", "circuit", 6, 6500),
            new FoundryMission("mechanical-order", "정비용 기계부품 조달", "mechanical", 4, 8000),
            new FoundryMission("electronic-order", "관문 전자 모듈 보급", "electronic", 4, 8500),
            new FoundryMission("consumer-order", "개척민 생활 보급", "consumer", 12, 6000),
            new FoundryMission("equipment-order", "지구권 산업장비 납품", "equipment", 3, 16000)
        };
        private string activeMissionId;
        private readonly HashSet<string> completedMissionIds = new HashSet<string>();
        public IReadOnlyList<FoundryMission> Missions => missions;
        public FoundryMission ActiveMission => missions.FirstOrDefault(m => m.Id == activeMissionId);
        public bool MissionCompleted(string id) => completedMissionIds.Contains(id);
        public double MissionStock => ActiveMission == null ? 0 : TotalStock(Content.FindResource(ActiveMission.ResourceId));
        public bool AcceptMission(string id, out string message)
        {
            var mission = missions.FirstOrDefault(m => m.Id == id);
            if (!HasMarket || mission == null || Content.FindResource(mission.ResourceId).ProcessingGrade < 2 || ActiveMission != null || MissionCompleted(id))
            { message = "거래소가 필요하며 미션은 한 번에 하나만 수주할 수 있습니다."; return false; }
            if (MissionPracticeActive && id != PracticeMissionId)
            { message = "실습 중에는 ‘첫 가공 계약 · 철판 납품’을 수주하세요."; return false; }
            activeMissionId = id; message = mission.Name + " 수주 완료. 전체 혜성 재고를 모아 납품하세요."; return true;
        }
        public bool DeliverMission(out string message)
        {
            var mission = ActiveMission;
            if (!HasMarket || mission == null || MissionStock + 1e-8 < mission.Amount)
            { message = "거래소 또는 납품할 자원이 부족합니다."; return false; }
            if (MissionPracticeActive && mission.Id == PracticeMissionId && PracticePlateMade < mission.Amount)
            { message = "실습에서는 철판을 직접 생산한 뒤 납품하세요."; return false; }
            var resource = Content.FindResource(mission.ResourceId);
            double remaining = mission.Amount;
            foreach (var colony in colonies) foreach (var facility in colony.State.Facilities)
                remaining -= facility.Inventory.RemoveStacks(resource, remaining);
            completedMissionIds.Add(mission.Id); activeMissionId = null;
            Company.Earn(mission.Reward); RecordBalance(); RefreshMissionPractice();
            message = mission.Name + $" 완료! 보상 {mission.Reward:N0} 솔라를 받았습니다."; return true;
        }
        public void AbandonMission() { activeMissionId = null; }
        public string DemolitionError(FacilityInstance facility)
        {
            if (facility == null || !Comet.Facilities.Contains(facility)) return "현재 혜성의 시설을 선택하세요.";
            if (voyages.Any(v => (v.Source == ActiveColony || v.Destination == ActiveColony)
                && (v.Ship.AssignedRoute.Source == facility || v.Ship.AssignedRoute.Destination == facility)))
                return "수송선이 이용 중인 항구·창고는 운송 종료 후 철거하세요.";
            double space = Comet.Facilities.Where(f => f != facility && IsTradeStorage(f)).Sum(f => f.Inventory.FreeStacks);
            if (space + 1e-8 < facility.Inventory.UsedStacks) return "재고를 옮길 다른 창고·항구·거래소의 공간이 부족합니다.";
            return null;
        }
        public bool Demolish(FacilityInstance facility, out string message)
        {
            message = DemolitionError(facility); if (message != null) return false;
            var destinations = Comet.Facilities.Where(f => f != facility && IsTradeStorage(f)).ToList();
            foreach (var stock in facility.Inventory.Amounts.ToList())
            {
                double remaining = stock.Value;
                foreach (var destination in destinations)
                {
                    double moved = destination.Inventory.AddStacks(stock.Key, remaining);
                    facility.Inventory.RemoveStacks(stock.Key, moved); remaining -= moved;
                }
            }
            if (Flight != null && (Flight.Source == facility || Flight.Destination == facility)) Flight = null;
            lastSupply.Remove(facility);
            foreach (var tile in Comet.Grid.GetFootprintTiles(facility.Anchor, facility.Definition.FootprintSize)) tile.ClearOccupant();
            Comet.RemoveFacility(facility);
            message = facility.Definition.DisplayName + " 철거 완료. 재고는 이관했으며 건설비는 환불되지 않습니다.";
            return true;
        }
    }
}
