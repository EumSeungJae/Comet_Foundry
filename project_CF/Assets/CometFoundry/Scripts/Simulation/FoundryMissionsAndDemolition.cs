using System;
using System.Collections.Generic;
using System.Linq;
using CometFoundry.Core;
using CometFoundry.Data;
using UnityEngine;

namespace CometFoundry.Simulation
{
    public enum FoundryMissionReward { None, Comet, CargoShip }
    public sealed class FoundryMissionRequirement
    {
        public FoundryMissionRequirement(string resourceId, int amount)
        {
            if (string.IsNullOrEmpty(resourceId) || amount <= 0) throw new ArgumentException("Invalid mission requirement");
            ResourceId = resourceId; Amount = amount;
        }
        public string ResourceId { get; }
        public int Amount { get; }
    }
    public sealed class FoundryMission
    {
        public FoundryMission(string id, string name, string resource, int amount, double reward)
            : this(id, name, id == "tutorial-plate" ? 0 : -1, LegacyGrade(resource), reward, FoundryMissionReward.None, new FoundryMissionRequirement(resource, amount)) { }
        public FoundryMission(string id, string name, int number, int grade, double reward, FoundryMissionReward bonus, params FoundryMissionRequirement[] requirements)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name) || reward < 0 || double.IsNaN(reward) || double.IsInfinity(reward) ||
                requirements == null || requirements.Length == 0 || requirements.Any(r => r == null) || requirements.Select(r => r.ResourceId).Distinct().Count() != requirements.Length)
                throw new ArgumentException("Invalid mission definition");
            Id = id; Name = name; Number = number; ProcessingGrade = grade; Reward = reward; BonusReward = bonus;
            Requirements = Array.AsReadOnly((FoundryMissionRequirement[])requirements.Clone());
        }
        private static int LegacyGrade(string resource) => resource == "gear" || resource == "frame" || resource == "circuit" ? 3 :
            resource == "mechanical" || resource == "electronic" ? 4 : resource == "consumer" || resource == "equipment" ? 5 : 2;
        public string Id { get; }
        public string Name { get; }
        public int Number { get; }
        public int ProcessingGrade { get; }
        public IReadOnlyList<FoundryMissionRequirement> Requirements { get; }
        // Single-resource compatibility for the tutorial and retired contracts.
        public string ResourceId => Requirements[0].ResourceId;
        public int Amount => Requirements[0].Amount;
        public double Reward { get; }
        public FoundryMissionReward BonusReward { get; }
        public string RewardCaption => $"{Reward:N0} 솔라" + (BonusReward == FoundryMissionReward.Comet ? " + 혜성 1개" : BonusReward == FoundryMissionReward.CargoShip ? " + 수송선 1척" : "");
    }
    public sealed partial class FoundrySession
    {
        private static FoundryMissionRequirement Demand(string resource, int amount) => new FoundryMissionRequirement(resource, amount);
        private static readonly FoundryMission[] missions = {
            new FoundryMission("tutorial-plate", "첫 가공 계약 · 철판 납품", "plate", 4, 1500),
            new FoundryMission("tier2-01", "관문 외벽 및 배선 보수", 1, 2, 3400, FoundryMissionReward.None, Demand("plate", 12), Demand("wire", 12)),
            new FoundryMission("tier2-02", "관문 통신망 배선", 2, 2, 4600, FoundryMissionReward.None, Demand("plate", 10), Demand("wire", 24)),
            new FoundryMission("tier2-03", "수송선 화물칸 보강", 3, 2, 5700, FoundryMissionReward.None, Demand("plate", 28), Demand("wire", 12)),
            new FoundryMission("tier2-04", "개척 전력망 확장", 4, 2, 8200, FoundryMissionReward.None, Demand("plate", 24), Demand("wire", 36)),
            new FoundryMission("tier2-05", "궤도 기지 외장 공사", 5, 2, 13300, FoundryMissionReward.None, Demand("plate", 48), Demand("wire", 48)),
            new FoundryMission("tier3-06", "수송 장치 구동부 정비", 6, 3, 7500, FoundryMissionReward.None, Demand("gear", 12), Demand("frame", 8)),
            new FoundryMission("tier3-07", "탐사선 골격 보강", 7, 3, 13800, FoundryMissionReward.None, Demand("frame", 16), Demand("plate", 24), Demand("wire", 12)),
            new FoundryMission("tier3-08", "관문 제어망 교체", 8, 3, 17100, FoundryMissionReward.None, Demand("circuit", 20), Demand("gear", 16), Demand("wire", 30)),
            new FoundryMission("tier3-09", "궤도 기지 자동화", 9, 3, 29800, FoundryMissionReward.None, Demand("frame", 20), Demand("gear", 20), Demand("circuit", 24), Demand("wire", 30)),
            new FoundryMission("tier3-10", "신규 혜성 개척 지원", 10, 3, 29600, FoundryMissionReward.Comet, Demand("frame", 24), Demand("gear", 24), Demand("circuit", 30), Demand("plate", 40), Demand("wire", 40)),
            new FoundryMission("tier4-11", "수송선 정비 계약", 11, 4, 28100, FoundryMissionReward.CargoShip, Demand("mechanical", 12), Demand("electronic", 10), Demand("frame", 16)),
            new FoundryMission("tier4-12", "항법 제어 모듈 공급", 12, 4, 60100, FoundryMissionReward.None, Demand("mechanical", 16), Demand("electronic", 20), Demand("circuit", 24)),
            new FoundryMission("tier4-13", "개척 기지 유지보수 부품", 13, 4, 88500, FoundryMissionReward.None, Demand("mechanical", 24), Demand("electronic", 20), Demand("frame", 30), Demand("gear", 36)),
            new FoundryMission("tier4-14", "궤도 조선소 설비 확장", 14, 4, 134500, FoundryMissionReward.None, Demand("mechanical", 32), Demand("electronic", 28), Demand("frame", 40), Demand("circuit", 40), Demand("plate", 60)),
            new FoundryMission("tier4-15", "우주선 부품납품", 15, 4, 197300, FoundryMissionReward.None, Demand("mechanical", 40), Demand("electronic", 40), Demand("frame", 60), Demand("gear", 80), Demand("circuit", 60))
        };
        private static readonly FoundryMission[] retiredMissions = {
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
        private FoundryMission legacyActiveMission;
        private readonly HashSet<string> completedMissionIds = new HashSet<string>();
        public IReadOnlyList<FoundryMission> Missions { get; } = Array.AsReadOnly(missions);
        public FoundryMission ActiveMission => legacyActiveMission ?? missions.FirstOrDefault(m => m.Id == activeMissionId);
        public bool MissionCompleted(string id) => completedMissionIds.Contains(id);
        public double MissionRequirementStock(FoundryMissionRequirement requirement) => TotalStock(Content.FindResource(requirement.ResourceId));
        public double MissionStock => ActiveMission == null ? 0 : ActiveMission.Requirements.Sum(MissionRequirementStock);
        public bool CanDeliverMission => HasMarket && ActiveMission != null && ActiveMission.Requirements.All(r => MissionRequirementStock(r) + 1e-8 >= r.Amount)
            && (!MissionPracticeActive || ActiveMission.Id != PracticeMissionId || PracticePlateMade >= ActiveMission.Amount)
            && (ActiveMission.BonusReward != FoundryMissionReward.CargoShip || Company.Ships.Count < 128);
        public bool AcceptMission(string id, out string message)
        {
            var mission = missions.FirstOrDefault(m => m.Id == id);
            if (!HasMarket || mission == null || ActiveMission != null || MissionCompleted(id))
            { message = "거래소가 필요하며 미션은 한 번에 하나만 수주할 수 있습니다."; return false; }
            if (MissionPracticeActive && id != PracticeMissionId)
            { message = "실습 중에는 ‘첫 가공 계약 · 철판 납품’을 수주하세요."; return false; }
            legacyActiveMission = null; activeMissionId = id;
            message = mission.Name + " 수주 완료. 모든 요구 소재를 확보한 뒤 한 번에 납품하세요."; return true;
        }
        private FoundryColony PrepareMissionRewardComet(ref int preparedId)
        {
            // Deterministic usable colony; never consumes survey candidates or exploration progress.
            string coordinate = null;
            for (int i = 0; i < 26 * 10; i++)
            {
                string candidate = ((char)('A' + i / 10)).ToString() + (i % 10);
                if (candidate == "A0") continue;
                if (colonies.Any(c => c.State.DistanceCode == candidate) || prospects.Any(p => p.State.DistanceCode == candidate)) continue;
                coordinate = candidate; break;
            }
            if (coordinate == null) return null;
            int distanceMinutes = CometGenerator.GetDistanceMinutes(coordinate[0], coordinate[1] - '0');
            var position = Vector2.right * Mathf.Max(.5f, distanceMinutes / 60f * 4);
            var state = new CometState(new SimulationId("mission-comet-" + preparedId++), "개척 지원 혜성", position, 5,
                CometGrade.C, CometGrade.C, "CC3012" + coordinate, 1f, 3, coordinate, distanceMinutes, 10, 10);
            state.AddDeposit(new ResourceDeposit(Content.FindResource("iron-ore"), 4000, 0, new Vector2Int(3, 3)));
            state.AddDeposit(new ResourceDeposit(Content.FindResource("copper-ore"), 4000, 1, new Vector2Int(7, 3)));
            state.AddDeposit(new ResourceDeposit(Content.FindResource("ice"), 4000, 2, new Vector2Int(5, 7)));
            if (!placement.TryPlaceFacility(state, new SimulationId("facility-" + preparedId++), Content.FindBuilding("port-small"), Vector2Int.zero, out _)) return null;
            return new FoundryColony(state, "개척 지원 혜성");
        }
        public bool DeliverMission(out string message)
        {
            var mission = ActiveMission;
            if (!HasMarket || mission == null)
            { message = "거래소와 진행 중인 미션이 필요합니다."; return false; }
            var missing = mission.Requirements.FirstOrDefault(r => MissionRequirementStock(r) + 1e-8 < r.Amount);
            if (missing != null)
            { message = $"{Content.FindResource(missing.ResourceId).DisplayName} 재고가 부족합니다. {MissionRequirementStock(missing):0.#}/{missing.Amount} 스택"; return false; }
            if (MissionPracticeActive && mission.Id == PracticeMissionId && PracticePlateMade < mission.Amount)
            { message = "실습에서는 철판을 직접 생산한 뒤 납품하세요."; return false; }
            if (mission.BonusReward == FoundryMissionReward.CargoShip && Company.Ships.Count >= 128)
            { message = "수송선 보유 한도에 도달하여 보상을 받을 수 없습니다. 납품 재고는 유지됩니다."; return false; }
            int preparedId = nextId;
            FoundryColony rewardColony = mission.BonusReward == FoundryMissionReward.Comet ? PrepareMissionRewardComet(ref preparedId) : null;
            if (mission.BonusReward == FoundryMissionReward.Comet && rewardColony == null)
            { message = "보상 혜성을 추가할 빈 좌표가 없습니다. 납품 재고는 유지됩니다."; return false; }
            // Validate every material and reward first; a partial delivery never consumes inventory.
            foreach (var requirement in mission.Requirements)
            {
                var resource = Content.FindResource(requirement.ResourceId); double remaining = requirement.Amount;
                foreach (var colony in colonies) foreach (var facility in colony.State.Facilities)
                    remaining -= facility.Inventory.RemoveStacks(resource, remaining);
            }
            if (rewardColony != null) { colonies.Add(rewardColony); Company.AddComet(rewardColony.State); nextId = preparedId; }
            if (mission.BonusReward == FoundryMissionReward.CargoShip) Company.AddShip(new SimulationId("ship-" + Company.Ships.Count), Content.Courier);
            Company.Earn(mission.Reward); completedMissionIds.Add(mission.Id); activeMissionId = null; legacyActiveMission = null;
            RecordBalance(); RefreshMissionPractice();
            message = mission.Name + " 완료! 보상 " + mission.RewardCaption + "를 받았습니다."; return true;
        }
        public void AbandonMission() { activeMissionId = null; legacyActiveMission = null; }
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
