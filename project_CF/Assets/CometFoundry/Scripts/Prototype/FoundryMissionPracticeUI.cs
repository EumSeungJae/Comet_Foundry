using CometFoundry.Simulation;
using UnityEngine;

namespace CometFoundry.Prototype
{
    public sealed partial class FoundryPlayableController
    {
        private bool practicePopup;
        private FoundryMissionPracticeStep lastPracticeStep;
        private void CheckPracticePopup()
        {
            var step = session.MissionPracticeStep;
            if (step == FoundryMissionPracticeStep.Inactive) { lastPracticeStep = step; practicePopup = false; return; }
            if (step == lastPracticeStep) return;
            lastPracticeStep = step; practicePopup = true;
        }
        private void DrawPracticePopup()
        {
            var step = session.MissionPracticeStep;
            string instruction;
            switch (step)
            {
                case FoundryMissionPracticeStep.BuildMarket:
                    instruction = "1. 거래소 터미널을 건설하세요.\n왼쪽 시설 목록에서 거래소를 선택하고 빈 타일에 건설합니다. 시작 혜성 추천 위치는 (12,7)입니다."; break;
                case FoundryMissionPracticeStep.AcceptMission:
                    instruction = "2. 지정 미션을 수주하세요.\n미션받기 → ‘첫 가공 계약 · 철판 납품’을 선택합니다. 철판 4스택, 보상 1,500 솔라입니다. 다른 미션이 진행 중이면 목록에서 수주 취소하거나 완료하세요."; break;
                case FoundryMissionPracticeStep.BuildFactory:
                    instruction = "3. 금속 가공소를 건설하세요.\n왼쪽 시설 목록에서 금속 가공소를 선택합니다. 시작 혜성 추천 위치는 (8,3)입니다. 드론이 이동할 빈 통로를 남겨주세요."; break;
                case FoundryMissionPracticeStep.SetRecipe:
                    instruction = "4. 2차 가공품 생산을 설정하세요.\n금속 가공소 클릭 → 철판 압연 선택 → 이 생산품으로 설정을 누릅니다. 투입 재료와 완성품 수량을 확인하세요."; break;
                case FoundryMissionPracticeStep.ProducePlates:
                    instruction = $"5. 철판 4스택을 직접 생산하세요.\n실습 중 생산 {session.PracticePlateMade:0.#}/4 스택 · 확보 재고 {session.MissionStock:0.#}/4 스택\n\n정제소의 철괴 제련으로 재료를 만들거나 거래소에서 철괴를 구매합니다. 드론이 철괴를 금속 가공소로 운반합니다. 전력·드론 지원시설·빈 통로·보관 공간을 확인하세요. 철판을 구매하는 것만으로는 실습을 통과할 수 없습니다."; break;
                case FoundryMissionPracticeStep.Deliver:
                    instruction = "6. 생산한 철판을 미션에 납품하세요.\n우측 미션 HUD에서 미션 납품을 누릅니다. 철판 4스택만 차감되고 보상 1,500 솔라를 받습니다. 일반 판매는 미션 납품으로 인정되지 않습니다."; break;
                default:
                    instruction = "철판 미션 실습 완료!\n2차 가공품 생산과 거래소 미션 납품을 완료했습니다. 미션 목록에서 구리선·기어·프레임·회로기판과 더 높은 등급의 미션에 도전하세요. 저장 버튼으로 진행을 기록하세요."; break;
            }
            OverlayFrame(new Rect(270, 235, 900, 445), "튜토리얼 실습 / 철판 미션");
            Label(new Rect(300, 318, 840, 229), instruction, Style(18, Ink));
            if (Btn(new Rect(300, 585, 390, 54), step == FoundryMissionPracticeStep.AcceptMission ? "미션 목록 열기" : step == FoundryMissionPracticeStep.Complete ? "플레이 계속" : "확인 · 실습 진행", true))
            { practicePopup = false; if (step == FoundryMissionPracticeStep.AcceptMission) missionList = true; }
            if (session.MissionPracticeActive && Btn(new Rect(710, 585, 430, 54), "실습 안내 종료 · 수주 미션 유지"))
            { session.StopMissionPractice(); practicePopup = false; }
        }
    }
}
