using System;
using CometFoundry.Simulation;
using UnityEngine;

namespace CometFoundry.Prototype
{
    public sealed partial class FoundryPlayableController
    {
        private bool tutorialChoice, tutorialGuide, missionList, demolishMode;
        private int guidePage;
        private Vector2 missionScroll;
        private string practiceStartError = "";
        private FacilityInstance demolitionTarget;
        private bool GameOverlayOpen => tutorialChoice || tutorialGuide || missionList || practicePopup || demolitionTarget != null;
        private static readonly string[] GuideTitles = { "개척 시작과 목표",
            "날짜·정지·배속",
            "보드와 자원 노드",
            "건설과 비용",
            "철거와 재배치",
            "전력과 공급 부족",
            "창고와 재고 단위",
            "물류 드론과 통로",
            "생산 팝업과 레시피",
            "첫 철괴·철판 생산",
            "생산 사슬과 완제품",
            "거래소 건설과 거래",
            "시세 곡선과 월간 소지금",
            "거래소 미션 수주",
            "미션 진행과 납품",
            "혜성 탐사와 개척",
            "수송선과 혜성 간 운송",
            "저장·불러오기와 마무리", "실습 / 철판 미션 수행" };
        private static readonly string[] GuideText = { "혜성에서 원재료를 채굴하고 가공·조립하여 거래소에 판매하거나 미션에 납품합니다.\n\n시작 시설은 발전소, 기본 창고, 철광석·구리광석 채굴기입니다. 먼저 정제소를 건설해 철괴와 구리괴를 만들고, 금속 가공소와 조립시설로 생산 사슬을 확장하세요.\n\n안내는 좌측 주제를 골라 읽을 수 있습니다. 닫은 뒤 상단 튜토리얼 버튼으로 언제든 다시 볼 수 있습니다.",
            "기본 속도에서 5초마다 하루가 지납니다. 한 달은 30일, 한 해는 12개월입니다. 따라서 기본 속도에서 150초마다 월간 시세와 소지금 기록이 갱신됩니다.\n\n상단 정지·1×·4× 버튼으로 속도를 조절합니다. Space로 정지/재개, Esc로 생산 팝업 또는 건설 선택을 취소합니다.\n\n튜토리얼·미션 목록·철거 확인 팝업에서는 시간이 멈춥니다. 생산 시간은 생산 팝업에 표시된 실제 초 단위를 기준으로 확인하세요.",
            "건설은 사각 타일 위에서 진행됩니다. 시설 크기만큼 빈 건설 가능 타일이 필요하며, 혜성 밖에는 배치할 수 없습니다.\n\n철광석·구리광석·얼음 노드는 매장량이 한정되어 있습니다. 노드를 클릭하면 남은 매장량을 확인할 수 있습니다.\n\n채굴기는 자원 노드 위에만 건설할 수 있고, 해당 노드에 맞는 채굴 생산품이 자동 설정됩니다. 다른 건물로 노드를 덮을 수 없습니다.",
            "왼쪽 시설 버튼을 누르고 빈 타일을 클릭하여 건설합니다. 버튼에 시설 크기와 솔라 비용이 표시됩니다.\n\n미리보기는 초록색이면 배치 가능, 주황색이면 배치 불가입니다. 솔라 부족·타일 점유·자원 노드·혜성 밖 위치를 확인하세요.\n\nEsc 또는 보드 우클릭으로 선택을 취소합니다. 시설을 빽빽하게 채우지 말고 물류 드론이 지날 통로를 남겨두세요.",
            "왼쪽 시설 철거를 누른 뒤 철거할 건물을 클릭합니다. 생산 팝업의 철거 버튼도 사용할 수 있습니다. 확인 팝업에서 실제 철거를 결정합니다.\n\n건설비는 환불되지 않고 진행 중인 생산은 사라집니다. 재고는 같은 혜성의 다른 창고·항구·거래소로 옮깁니다. 공간이 부족하면 철거할 수 없습니다.\n\n수송선이 이용 중인 항구·창고는 운송이 끝나기 전 철거할 수 없습니다. 발전소를 철거하면 전력이 부족해질 수 있습니다.",
            "상단 전력 표시에서 소비량과 생산량을 확인합니다. 발전소가 전력을 공급하며 생산시설과 물류시설이 소비합니다.\n\n전력이 부족하면 해당 혜성의 생산과 드론 운송이 멈춥니다. 발전소를 추가하거나 사용하지 않는 시설을 철거하여 공급을 확보하세요. 좁은 혜성에서는 1×1 소형 발전소(전력 4)를 여러 개 건설할 수 있습니다.\n\n각 혜성은 자체 전력 상태를 갖습니다. 새 혜성을 개척한 뒤에도 발전소를 준비해야 합니다.",
            "자원은 스택 단위로 보관·거래됩니다. 우측 재고 목록은 현재 혜성의 모든 시설 재고를 합산합니다.\n\n시설마다 보관 한도가 있습니다. 입력 재료가 충분해도 완성품을 넣을 공간이 없으면 생산이 대기합니다. 창고를 늘리거나 재고를 판매·납품하세요.\n\n거래소 목록의 두 수치는 현재 혜성 재고 / 전체 혜성 재고입니다. 수송선에 실린 화물은 납품 가능한 시설 재고에 포함되지 않습니다.",
            "1×2 드론저장소(1,800 솔라, 드론 8기 지원), 기본 창고와 드론 지원 시설이 생산 재료·재고를 자동으로 운반합니다. 채굴기 → 정제소 → 가공소 → 조립시설 순으로 재료가 공급됩니다.\n\n건물 사이에 빈 타일 통로를 확보하세요. 새 혜성에서도 지원시설과 전력이 필요합니다. 드론이 오가지 못하면 재료가 있어도 필요한 시설로 들어가지 않습니다.\n\n생산이 멈추면 전력, 재료 보유량, 목적지 보관 공간, 통로를 차례로 확인하세요. 기본 창고는 시작 혜성의 임시 선착장도 지원합니다.",
            "생산 건물을 클릭하면 생산품 선택 팝업이 열립니다. 생산 비용, 필요 재료/보유량, 소요 시간, 완성품 수량, 기준가를 확인할 수 있습니다.\n\n생산품을 고른 뒤 이 생산품으로 설정을 누르세요. 비용과 재료는 완성 시 함께 차감됩니다. 부족하면 생산이 대기합니다.\n\n다른 생산품으로 변경하면 진행도가 초기화됩니다. 같은 생산품 유지 시 진행도는 유지됩니다. 생산 중지 버튼은 레시피와 진행도를 초기화합니다.",
            "정제소를 빈 땅에 건설하고 철괴 제련을 설정하세요. 추천 위치는 시작 혜성 (5,3)입니다. 드론이 철광석을 공급하여 철괴를 만듭니다.\n\n다음으로 금속 가공소를 건설하고 철판 압연을 설정하세요. 추천 위치는 (8,3)입니다. 철괴가 철판으로 가공됩니다.\n\n생산 완료 재고를 확인한 뒤 거래소를 건설하세요. 위치는 예시이므로 현재 빈 타일과 통로를 우선 확인합니다.",
            "철광석 → 철괴 → 철판·기어·프레임 → 기계부품으로 가공합니다.\n구리광석 → 구리괴 → 구리선·회로기판 → 전자부품으로 가공합니다.\n\n기계부품과 전자부품을 모아 산업장비를 조립할 수 있습니다. 프레임·전자부품으로 고급 드론을, 철판·물로 생활소비재를 만듭니다.\n\n얼음 → 물 정제도 준비하세요. 필요한 시설과 투입 수량은 각 건물의 생산품 팝업에서 확인할 수 있습니다.",
            "거래소 터미널을 건설하면 거래소, 혜성관리, 미션받기가 활성화됩니다. 시작 혜성의 추천 위치는 (12,7)입니다.\n\n거래소에서 소재를 선택하고 구입 또는 판매를 누릅니다. 슬라이더·±1·최대 버튼으로 수량을 지정한 뒤 거래 확인을 누르세요.\n\n구입에는 솔라와 빈 보관 공간이 필요합니다. 판매는 현재 혜성의 재고만 사용합니다. 구매한 소재는 현재 혜성의 창고·항구·거래소에 입고됩니다. 실제 구매·판매 단가는 우측에서 확인합니다.",
            "일반 시세는 정가 대비 ±30% 범위에서 30일마다 갱신됩니다. 현재 플레이 버전에는 시장 이벤트가 없으므로 이벤트로 ±30%를 초과하지 않습니다.\n\n가격 곡선은 월별 기록 사이를 부드럽게 연결한 표시입니다. 빨강은 정가 이상, 파랑은 정가 미만이며 마우스를 올리면 실제 월별 기록을 볼 수 있습니다.\n\n혜성관리 소지금 그래프는 시작 금액과 매월 1일의 소지금을 비교합니다. 현재 소지금은 실시간, 전월 증감은 월초 기록 기준입니다. 6·12·18·24개월 범위를 선택할 수 있습니다.",
            "우측 미션받기를 누르면 거래소가 주는 미션 목록이 열립니다. 납품 자원, 필요 스택, 보상을 비교하여 미션을 선택하세요.\n\n동시에 하나만 수주할 수 있고, 완료한 미션은 다시 수주할 수 없습니다. 미션에는 기한이 없습니다.\n\nHUD에는 진행 미션과 확보 재고가 표시됩니다. 미션 목록에서 수주 취소를 누르면 미션을 다시 선택할 수 있습니다. 취소 자체로 자원은 소비되지 않습니다.",
            "생산하거나 거래소에서 구매하여 미션 자원을 확보하세요. 진행 수치는 모든 혜성의 시설에 보관된 해당 자원을 합산합니다.\n\n필요 재고가 모이면 HUD의 미션 납품 버튼을 누릅니다. 요구한 스택만 재고에서 차감되고 보상이 지급됩니다. 보상은 한 번만 받을 수 있습니다.\n\n일반 판매는 미션 납품으로 인정되지 않습니다. 납품 전에 재고를 판매하면 진행 수치가 줄어듭니다. 미션 납품에도 거래소가 필요합니다.",
            "혜성관리에서 새 탐사 시작을 누르면 1,500 솔라를 사용하여 8초 동안 탐사합니다. 발견한 세 후보의 크기, 자원 매장량, 개척 비용을 비교하세요.\n\n원하는 후보를 선택하고 개척하기를 누르면 항구가 포함된 새 혜성을 얻습니다. 보유 혜성 목록을 선택하여 개발 보드를 전환합니다.\n\n다른 혜성을 보고 있어도 기존 혜성은 계속 생산합니다. 새 혜성의 전력·창고·물류·자원 생산을 준비하세요.",
            "혜성관리의 수송선 관리에서 수송선, 출발 혜성, 도착 혜성, 화물과 수량을 선택합니다. 두 혜성 모두 선착장이 필요합니다.\n\n운송 비용과 소요 시간을 확인하고 출항하세요. 소형 수송선은 100스택을 보관하며, 추가 수송선 구매에는 12,000 솔라가 필요합니다.\n\n도착 선착장이 가득 차면 화물을 수송선에 보관하고 하역 공간이 생길 때까지 대기합니다. 필요한 재고를 판매·이동하거나 저장 공간을 확보하세요.",
            "상단 저장 버튼은 혜성, 자원, 재고, 생산, 수송, 날짜, 소지금 기록과 미션 상태를 저장합니다. 자동 저장은 아니므로 작업 후 직접 저장하세요.\n\n타이틀에서 저장한 개척 불러오기를 선택합니다. 상단 튜토리얼 버튼으로 이 안내를 다시 실행할 수 있습니다.\n\n타이틀에서 개척 재개는 현재 진행으로 돌아오며 새 개척 시작은 확인 후 진행을 초기화합니다. 기존 저장은 다시 저장하기 전까지 유지됩니다.", "후반 실습에서는 거래소의 ‘첫 가공 계약 · 철판 납품’을 수주하고, 철판 4스택을 직접 생산해 납품합니다.\n\n미션 실습 시작을 누르면 진행 단계마다 팝업으로 다음 행동을 안내합니다. 안내 확인 후에는 팝업을 닫고 실제 보드와 거래소에서 진행합니다.\n\n거래소 건설 → 지정 미션 수주 → 금속 가공소 건설 → 철판 압연 설정 → 철괴 공급 및 철판 직접 생산 → 미션 납품 순서입니다.\n\n원재료·철괴 등 1차 소재는 미션 대상이 아닙니다. 이번 실습은 2차 가공품인 철판을 대상으로 합니다. 철괴 구매는 가능하지만 철판 구매만으로 실습을 완료할 수 없습니다." };
        private void StartTutorialGuide()
        {
            tutorialChoice = false; tutorialGuide = true; guidePage = 0; practiceStartError = "";
            selected = null; build = null; demolishMode = false; demolitionTarget = null; missionList = false;
        }
        private void DrawMissionHud()
        {
            Label(new Rect(1150, 177, 246, 29), "거래소 미션", heading);
            var mission = session.ActiveMission;
            if (mission == null)
            {
                Label(new Rect(1150, 215, 246, 82), session.HasMarket ? "미션을 선택해 자원을 납품하고 보상을 받으세요." : "거래소 터미널을 건설하면 미션을 받을 수 있습니다.", text);
            }
            else
            {
                Label(new Rect(1150, 213, 246, 45), mission.Name, text);
                Label(new Rect(1150, 263, 246, 22), $"{session.Content.FindResource(mission.ResourceId).DisplayName}  {Math.Min(session.MissionStock, mission.Amount):0.#}/{mission.Amount} 스택", small);
                Label(new Rect(1150, 288, 246, 22), $"보상 {mission.Reward:N0} 솔라", Style(14, Green));
            }
            bool enabled = GUI.enabled; GUI.enabled = enabled && session.HasMarket;
            if (Btn(new Rect(1150, 316, 246, 29), mission == null ? "미션받기" : "미션 목록")) missionList = true;
            GUI.enabled = enabled && session.HasMarket && mission != null && session.MissionStock >= mission.Amount;
            if (mission != null && Btn(new Rect(1150, 351, 246, 30), "미션 납품", true)) session.DeliverMission(out notice);
            GUI.enabled = enabled;
        }
        private void OverlayFrame(Rect window, string caption)
        {
            Fill(new Rect(0, 0, 1440, 900), new Color(0, 0, 0, .78f));
            Fill(window, Panel); Fill(new Rect(window.x, window.y, window.width, 4), Orange);
            Label(new Rect(window.x + 26, window.y + 22, window.width - 52, 38), caption, title);
        }
        private void DrawGameOverlay()
        {
            GUI.enabled = true;
            if (practicePopup && !tutorialChoice && !tutorialGuide) { DrawPracticePopup(); return; }
            if (demolitionTarget != null) { DrawDemolition(); return; }
            if (tutorialChoice)
            {
                OverlayFrame(new Rect(370, 265, 700, 330), "개척 안내를 확인하시겠습니까?");
                Label(new Rect(400, 340, 640, 125), "건설과 철거, 생산과 물류, 거래소와 미션, 혜성 탐사와 수송을 안내합니다. 안내 중에는 시간이 멈춥니다.\n\n건너뛰어도 상단 튜토리얼 버튼으로 다시 볼 수 있습니다.", text);
                if (Btn(new Rect(400, 490, 310, 54), "튜토리얼 시작", true)) StartTutorialGuide();
                if (Btn(new Rect(730, 490, 310, 54), "안내 없이 시작")) { tutorialChoice = false; session.SkipTutorial(); }
                return;
            }
            if (tutorialGuide)
            {
                OverlayFrame(new Rect(170, 110, 1100, 680), $"개척 안내 {guidePage + 1}/{GuideTitles.Length}");
                for (int i = 0; i < GuideTitles.Length; i++)
                    if (Btn(new Rect(194, 187 + i * 26, 294, 24), $"{i + 1:00}  {GuideTitles[i]}", i == guidePage)) guidePage = i;
                Label(new Rect(518, 192, 710, 39), GuideTitles[guidePage], heading);
                Label(new Rect(518, 252, 710, 398), GuideText[guidePage], Style(18, Ink));
                Label(new Rect(518, 658, 710, 30), string.IsNullOrEmpty(practiceStartError) ? "좌측 주제 선택 · 안내를 닫으면 플레이가 재개됩니다." : practiceStartError, small);
                GUI.enabled = guidePage > 0;
                if (Btn(new Rect(518, 713, 165, 45), "이전")) guidePage--;
                GUI.enabled = true;
                if (Btn(new Rect(698, 713, 245, 45), guidePage == GuideTitles.Length - 1 ? "미션 실습 시작" : "다음", true))
                { if (guidePage == GuideTitles.Length - 1) { if (session.StartMissionPractice(out notice)) { tutorialGuide = false; practicePopup = true; lastPracticeStep = session.MissionPracticeStep; } else practiceStartError = notice; } else guidePage++; }
                if (Btn(new Rect(958, 713, 270, 45), "안내 닫기")) tutorialGuide = false;
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) { tutorialGuide = false; Event.current.Use(); }
                return;
            }
            if (missionList) DrawMissionList();
        }
        private void DrawMissionList()
        {
            OverlayFrame(new Rect(260, 125, 920, 665), "거래소 / 납품 미션 목록");
            Label(new Rect(286, 194, 860, 27), "한 번에 하나 수주 · 기한 없음 · 납품 시 자원 차감 · 보상은 1회 지급", small);
            missionScroll = GUI.BeginScrollView(new Rect(286, 239, 866, 442), missionScroll, new Rect(0, 0, 844, session.Missions.Count * 88));
            for (int i = 0; i < session.Missions.Count; i++)
            {
                var m = session.Missions[i]; float y = i * 88;
                Fill(new Rect(0, y, 844, 78), new Color32(32, 48, 56, 255));
                Label(new Rect(17, y + 8, 660, 26), m.Name, heading);
                Label(new Rect(17, y + 42, 660, 23), $"{session.Content.FindResource(m.ResourceId).DisplayName} {m.Amount} 스택 · 보상 {m.Reward:N0} 솔라", text);
                GUI.enabled = session.HasMarket && session.ActiveMission == null && !session.MissionCompleted(m.Id) && (!session.MissionPracticeActive || m.Id == FoundrySession.PracticeMissionId);
                string label = session.MissionCompleted(m.Id) ? "완료" : session.ActiveMission?.Id == m.Id ? "진행 중" : "수주";
                if (Btn(new Rect(690, y + 16, 138, 46), label, true)) { session.AcceptMission(m.Id, out notice); missionList = false; }
                GUI.enabled = true;
            }
            GUI.EndScrollView();
            if (session.ActiveMission != null && Btn(new Rect(286, 716, 270, 43), "현재 미션 수주 취소")) { session.AbandonMission(); notice = "수주를 취소했습니다. 재고는 유지됩니다."; }
            if (Btn(new Rect(882, 716, 270, 43), "닫기")) missionList = false;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) { missionList = false; Event.current.Use(); }
        }
        private void DrawDemolition()
        {
            OverlayFrame(new Rect(370, 240, 700, 410), "시설 철거 확인");
            string error = session.DemolitionError(demolitionTarget);
            Label(new Rect(400, 318, 640, 175), demolitionTarget.Definition.DisplayName + $"\n재고 {demolitionTarget.Inventory.UsedStacks:0.#} 스택은 다른 보관시설로 옮깁니다.\n건설비 환불 없음 · 생산 진행 초기화 · 빈 타일 복구\n" + (demolitionTarget.Definition.IsPowerPlant ? "발전소 철거 후 전력 부족이 발생할 수 있습니다.\n" : "") + (error ?? "철거 후 같은 위치에 새 시설을 건설할 수 있습니다."), text);
            GUI.enabled = error == null;
            if (Btn(new Rect(400, 551, 310, 54), "철거 확인", true))
            {
                if (session.Demolish(demolitionTarget, out notice)) { demolitionTarget = null; selected = null; build = null; demolishMode = false; ResetPanelSelection(); }
            }
            GUI.enabled = true;
            if (Btn(new Rect(730, 551, 310, 54), "취소")) demolitionTarget = null;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) { demolitionTarget = null; Event.current.Use(); }
        }
    }
}
