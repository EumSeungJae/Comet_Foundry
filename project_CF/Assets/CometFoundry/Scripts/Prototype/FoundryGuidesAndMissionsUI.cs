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
        
        private FacilityInstance demolitionTarget;
        private bool GameOverlayOpen => tutorialChoice || TutorialPausesTime || missionList || practicePopup || demolitionTarget != null;
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
                Label(new Rect(400, 340, 640, 125), "채굴기부터 4차 자재 생산, 물류와 전력, 거래소와 미션, 혜성 구매·이동·물자 운송까지 14개 주제를 순서대로 실습합니다. 노란 박스의 타깃과 단계별 팝업을 따라 진행하세요.\n\n건설 안내는 확인 버튼 없이 바로 조작합니다. 건설되면 다음 단계로 자동 진행합니다.", text);
                if (Btn(new Rect(400, 490, 310, 54), "튜토리얼 시작", true)) StartTutorialGuide();
                if (Btn(new Rect(730, 490, 310, 54), "안내 없이 시작")) { tutorialChoice = false; session.SkipTutorial(); }
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
