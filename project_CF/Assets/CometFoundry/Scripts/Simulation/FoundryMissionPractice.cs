using System.Linq;
using CometFoundry.Data;

namespace CometFoundry.Simulation
{
    public enum FoundryMissionPracticeStep { Inactive, BuildMarket, AcceptMission, BuildFactory, SetRecipe, ProducePlates, Deliver, Complete }
    public sealed partial class FoundrySession
    {
        public const string PracticeMissionId = "tutorial-plate";
        public FoundryMissionPracticeStep MissionPracticeStep { get; private set; }
        public double PracticePlateMade { get; private set; }
        public bool MissionPracticeActive => MissionPracticeStep != FoundryMissionPracticeStep.Inactive && MissionPracticeStep != FoundryMissionPracticeStep.Complete;
        public string DroneStatus => !HasLogistics ? "지원시설 없음 · 드론저장소 건설 필요"
            : Power.HasDeficit ? "전력 부족 · 발전소를 확보하세요"
            : Flight != null ? "물류 드론 운송 중"
            : "드론 대기 · 재료/보관 공간/빈 통로 확인";
        public bool StartMissionPractice(out string message)
        {
            if (MissionPracticeActive) { message = "진행 중인 철판 미션 실습을 계속합니다."; return true; }
            if (ActiveMission != null && ActiveMission.Id != PracticeMissionId)
            { message = "진행 미션을 완료하거나 수주 취소한 뒤 실습을 시작하세요."; return false; }
            PracticePlateMade = 0;
            MissionPracticeStep = MissionCompleted(PracticeMissionId) ? FoundryMissionPracticeStep.Complete : FoundryMissionPracticeStep.BuildMarket;
            RefreshMissionPractice(); message = MissionPracticeStep == FoundryMissionPracticeStep.Complete ? "철판 실습 미션은 이미 완료했습니다." : "철판 4스택 생산·납품 실습을 시작합니다.";
            return true;
        }
        public void StopMissionPractice() { MissionPracticeStep = FoundryMissionPracticeStep.Inactive; }
        private void RecordPracticeProduction(RecipeDefinition recipe)
        {
            if (!MissionPracticeActive || ActiveMission?.Id != PracticeMissionId) return;
            foreach (var output in recipe.Outputs) if (output.Resource.ResourceId == "plate") PracticePlateMade += output.Stacks;
        }
        private void RefreshMissionPractice()
        {
            if (!MissionPracticeActive) return;
            if (MissionCompleted(PracticeMissionId)) { MissionPracticeStep = FoundryMissionPracticeStep.Complete; return; }
            var facilities = colonies.SelectMany(c => c.State.Facilities);
            // Re-evaluate prerequisites if a building is demolished or a mission abandoned.
            if (!HasMarket) MissionPracticeStep = FoundryMissionPracticeStep.BuildMarket;
            else if (ActiveMission?.Id != PracticeMissionId) MissionPracticeStep = FoundryMissionPracticeStep.AcceptMission;
            else if (!facilities.Any(f => f.Definition.FacilityId == "factory-metal")) MissionPracticeStep = FoundryMissionPracticeStep.BuildFactory;
            else if (!facilities.Any(f => f.ActiveRecipe?.RecipeId == "roll-plate")) MissionPracticeStep = FoundryMissionPracticeStep.SetRecipe;
            else if (PracticePlateMade < 4 || MissionStock < 4) MissionPracticeStep = FoundryMissionPracticeStep.ProducePlates;
            else MissionPracticeStep = FoundryMissionPracticeStep.Deliver;
        }
    }
}
