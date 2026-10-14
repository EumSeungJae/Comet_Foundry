using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CometFoundry.Data;
using CometFoundry.Simulation;
using UnityEngine;

namespace CometFoundry.Prototype
{
    public sealed partial class FoundryPlayableController
    {
        private enum GuideAction { Read, SelectBuilding, PlaceBuilding, OpenFacility, SelectRecipe, ConfirmRecipe, Produce, OpenExchange, Buy, Sell, OpenMissions, AcceptMission, MissionProduce, DeliverMission, OpenManagement,
            Survey, WaitSurvey, SelectProspect, PurchaseColony, SwitchDestination, SwitchOrigin, PrepareCargo, OpenFleet, SelectSource, SelectDestination, SelectCargo, DispatchCargo, WaitCargo, OpenCargoDock }
        private sealed class GuideStep
        {
            public int Chapter;
            public string Title, Body, Building, Recipe, Target;
            public GuideAction Action;
            public FoundryPage Page;
            public bool Fleet;
        }
        private readonly List<GuideStep> guidedSteps = new List<GuideStep>();
        private bool drawingGuide;
        private int guideTradeCount;
        private HashSet<FacilityInstance> guideExistingFacilities;
        private string guideConstructionBuilding;
        private FoundryColony guideConstructionColony;
        private double guideProductionStart;
        private FacilityInstance guideFacility;
        private string guideMissionId;
        private int guideOriginColony, guideDestinationColony = -1, guideColonyCount;
        private FoundryVoyage guideVoyage;
        private HashSet<FoundryVoyage> guideExistingVoyages;
        private double guideDestinationStock;
        private const string GuideCargoId = "water";
        private const int GuideCargoAmount = 3;
        private int GuideChapterCount => guidedSteps[guidedSteps.Count - 1].Chapter;
        private static readonly Color TutorialYellow = new Color32(255, 224, 48, 255);
        private GuideStep CurrentGuide => guidedSteps[Mathf.Clamp(guidePage, 0, guidedSteps.Count - 1)];
        private bool TutorialPausesTime => tutorialGuide && CurrentGuide.Action == GuideAction.Read;
        private bool TutorialPointerOverPopup => tutorialGuide && Event.current != null && GuidePopupRect(GuideTarget()).Contains(Event.current.mousePosition);
        private bool GameInputBlocked => GameOverlayOpen || TutorialPointerOverPopup;

        private void AddGuide(int chapter, string caption, string body, GuideAction action = GuideAction.Read,
            string building = null, string recipe = null, string target = null, FoundryPage view = FoundryPage.Board, bool showFleet = false)
        {
            guidedSteps.Add(new GuideStep { Chapter = chapter, Title = caption, Body = body, Action = action,
                Building = building, Recipe = recipe, Target = target, Page = view, Fleet = showFleet });
        }
        private void AddProductionGuide(int chapter, string caption, string building, string recipe, string explanation, bool construct = true)
        {
            if (construct)
            {
                AddGuide(chapter, caption + " / 시설 선택", "노란 박스의 시설 버튼을 누르세요.\n건설 비용과 크기를 확인합니다. 이미 있는 시설은 ‘기존 시설 사용’으로 이어갈 수 있습니다.", GuideAction.SelectBuilding, building, recipe);
                AddGuide(chapter, caption + " / 건설", "노란 박스의 추천 위치에 시설을 건설하세요. 다른 빈 위치도 사용할 수 있습니다.\n채굴기는 자원 노드 위에만 배치합니다. 건물 사이에는 드론 통로를 남겨주세요.", GuideAction.PlaceBuilding, building, recipe);
            }
            AddGuide(chapter, caption + " / 시설 열기", explanation + "\n\n노란 박스의 건물을 클릭해 생산 팝업을 여세요.", GuideAction.OpenFacility, building, recipe);
            AddGuide(chapter, caption + " / 생산품 선택", "노란 박스의 생산품을 선택하세요.\n필요 재료와 건물 보유량, 생산 비용, 소요 시간, 완성품 수량을 확인합니다.", GuideAction.SelectRecipe, building, recipe);
            AddGuide(chapter, caption + " / 생산 설정", "‘이 생산품으로 설정’ 또는 ‘현재 생산품 유지’를 누르세요.\n재료는 드론이 자동 공급합니다. 다른 생산품으로 바꾸면 진행도가 초기화됩니다.", GuideAction.ConfirmRecipe, building, recipe);
            AddGuide(chapter, caption + " / 생산 확인", explanation + "\n\n안내된 수량이 이 시설에서 생산될 때까지 기다리세요. 전력·재료·통로·보관 공간을 확인합니다. 상단 1× / 4×로 시간을 진행할 수 있습니다.", GuideAction.Produce, building, recipe);
        }
        private void CreateGuidedSteps()
        {
            guidedSteps.Clear();
            AddProductionGuide(1, "채굴기 건설 · 원자재", "mine-small", "extract-iron", "철광석은 원자재입니다. 채굴기는 유한한 매장량을 소비하며 철광석 4스택을 생산합니다. 노드 종류에 맞는 생산품이 자동 설정됩니다.");
            AddProductionGuide(2, "정제소 건설 · 1차 자재", "refinery-basic", "smelt-iron", "철광석 3스택 → 철괴 2스택.\n정제소는 철괴·구리괴·물을 만드는 1차 생산시설입니다.");
            AddProductionGuide(3, "금속 가공소 건설 · 2차 자재", "factory-metal", "roll-plate", "철괴 2스택 → 철판 2스택.\n철판은 회로기판과 프레임의 재료입니다. 이 시설에서 기어·프레임도 생산합니다.");
            AddProductionGuide(4, "전자 가공 준비 · 구리괴", "refinery-basic", "smelt-copper", "먼저 정제소를 구리괴 제련으로 바꿉니다.\n구리광석 3스택 → 구리괴 2스택. 시작 구리 채굴기가 원자재를 공급합니다.", false);
            AddProductionGuide(4, "전자 가공소 건설 · 구리선", "factory-electronics", "draw-wire", "구리괴 2스택 → 구리선 3스택.\n구리선은 2차 소재입니다. 세 번 생산해 9스택을 확보한 뒤 3차 소재인 회로기판으로 넘어갑니다.");
            AddProductionGuide(4, "전자 가공소 · 3차 자재", "factory-electronics", "make-circuit", "구리선 2스택 + 철판 1스택 → 회로기판 1스택.\n생산품을 회로기판으로 바꿔 두 번 생산합니다. 다음 조립 단계에 회로기판 2스택이 필요합니다.", false);
            AddProductionGuide(4, "조립 재료 공급 · 구리선 재개", "factory-electronics", "draw-wire", "회로기판을 확보했으니 전자 가공소를 구리선 가공으로 다시 설정합니다.\n회로기판 생산 중에는 구리선이 입력 재료로 예약됩니다. 구리선 생산으로 바꾸면 회로기판과 구리선을 조립소에 공급할 수 있습니다.", false);
            AddProductionGuide(5, "일반 조립소 건설 · 4차 자재", "assembly-basic", "make-electronic", "회로기판 2스택 + 구리선 1스택 → 전자부품 1스택.\n회로기판을 두 번 생산하고 남은 구리선을 공급하세요. 일반 조립소에서는 기어·프레임으로 기계부품도 조립합니다.");
            AddGuide(5, "조립 이후의 생산 사슬", "기계부품 + 전자부품 → 산업장비 완제품.\n일반 조립소는 4차 부품뿐 아니라 완제품도 생산합니다. 생산품 팝업에서 각 조합을 확인하세요.", target: "facility", building: "assembly-basic");
            AddGuide(6, "창고의 역할과 기능", "창고는 재고를 보관합니다. 운반은 별도의 드론저장소/격납고가 담당하며, 시작 창고는 임시 선착장도 지원합니다.\n시설 보관 공간이 가득 차면 생산이 대기합니다. 창고 추가나 판매·납품으로 공간을 확보하세요.", building: "warehouse-small", target: "facility");
            AddGuide(6, "항구의 역할과 기능", "항구는 혜성 간 수송선의 입출항과 화물 보관을 담당합니다. 두 혜성에 선착장이 있어야 운송할 수 있습니다.\n도착 항구가 가득 차면 화물은 수송선에 남아 하역을 기다립니다. 노란 박스는 항구 건설 버튼입니다.", building: "port-small", target: "build");
            AddGuide(7, "드론 격납고의 역할과 기능", "소형 드론저장소는 4기, 중형 드론 격납고는 8기가 병렬 작업합니다. 시작 드론저장소는 4기를 제공합니다.\n생산 건물의 입출고 대기열은 최대 2개이며 남는 드론은 다른 시설을 맡습니다. 지원시설이 없으면 운반하지 않습니다. 전력과 빈 통로가 필요합니다.", building: "drone-hangar", target: "build");
            AddGuide(7, "드론의 이동 · 빈 통로 확보", "드론은 건설되지 않은 빈 타일을 상하좌우로 이동합니다. 건물이나 손상된 빈 공간은 통과할 수 없습니다.\n저장소·격납고에서 자원을 가져올 건물과 받을 건물까지 폭 1칸 이상의 통로를 연결하세요. 건물로 통로를 막으면 운송이 멈춥니다.", target: "colony-board");
            AddGuide(7, "드론의 작업 · 회수부터 복귀까지", "드론은 지원시설 출발 → 자원 회수 → 목적지 배송 → 복귀 순서로 움직입니다. 주황은 배송, 초록은 복귀입니다.\n통로가 있어도 전력·출발 재고·도착 공간이 부족하면 대기합니다. 상단 작업 수와 생산 건물의 대기열을 확인하세요.", target: "colony-board");
            AddGuide(7, "발전소의 역할과 기능", "발전소는 현재 혜성의 전력을 공급합니다. 상단은 전력 소비 / 생산입니다.\n공급이 부족하면 생산과 드론 운송이 멈춥니다. 각 혜성에 발전소를 확보하세요. 소형 발전소는 좁은 곳에도 배치할 수 있습니다.", target: "power");
            AddGuide(8, "거래소 터미널 / 시설 선택", "거래소 터미널을 선택하세요.\n터미널을 건설하면 거래소·미션·혜성관리를 사용할 수 있습니다.", GuideAction.SelectBuilding, "market-terminal");
            AddGuide(8, "거래소 터미널 / 건설", "추천 위치의 빈 타일에 거래소 터미널을 건설하세요.\n소재 구매·판매와 납품을 위한 보관 공간도 제공합니다.", GuideAction.PlaceBuilding, "market-terminal");
            AddGuide(9, "거래소 열기", "노란 박스의 ‘거래소 열기’를 누르세요.\n터미널 건물을 클릭해도 같은 화면을 열 수 있습니다.", GuideAction.OpenExchange, target: "exchange");
            AddGuide(9, "거래소 / 보유 소재", "왼쪽 목록에서 소재를 선택합니다.\n두 수치는 현재 혜성 / 전체 혜성의 시설 재고입니다. 판매는 현재 혜성 재고만 사용하며, 수송선 화물은 시설 재고에 포함되지 않습니다.", target: "materials", view: FoundryPage.Exchange);
            AddGuide(9, "거래소 / 시장 시세 차트", "선택한 소재의 월별 시세를 표시합니다. 6·12·18·24개월 범위를 선택하고 곡선 위에 마우스를 올려 기록을 확인하세요.\n30일마다 시세가 갱신됩니다. 빨강은 정가 이상, 파랑은 정가 미만이며 일반 변동은 ±30%입니다.", target: "prices", view: FoundryPage.Exchange);
            AddGuide(9, "거래소 / 구매 실습", "‘구입’을 선택하고 수량 1스택으로 ‘거래 확인’을 누르세요.\n구매 단가와 합계를 먼저 확인합니다. 구매에는 솔라와 빈 보관 공간이 필요하며 소재는 현재 혜성에 입고됩니다.", GuideAction.Buy, target: "trade", view: FoundryPage.Exchange);
            AddGuide(9, "거래소 / 판매 실습", "‘판매’를 선택하고 방금 구매한 소재 1스택을 ‘거래 확인’으로 판매하세요.\n구매와 판매 단가는 다를 수 있습니다. 거래가 성공하면 재고와 현재 소지금이 변경됩니다.", GuideAction.Sell, target: "trade", view: FoundryPage.Exchange);
            AddGuide(10, "미션 / 목록 열기", "‘미션받기’ 또는 ‘미션 목록’을 누르세요.\n요구 자원·스택 수·보상을 비교합니다. 동시에 하나만 수주할 수 있고 기한은 없습니다.", GuideAction.OpenMissions, target: "missions");
            AddGuide(10, "미션 / 수주", "노란 박스의 미션을 수주하세요. 처음에는 철판 4스택 납품을 안내합니다. 이미 수주한 미션은 그대로 이어갑니다.\n완료한 미션은 다시 수주할 수 없습니다. 일반 판매는 미션 납품으로 인정되지 않습니다.", GuideAction.AcceptMission, target: "mission-row");
            AddGuide(10, "미션 / 재고 확보", "철판 미션이라면 금속 가공소를 철판 압연으로, 정제소를 철괴 제련으로 설정하세요. 이전에 만든 재고도 사용할 수 있습니다.\n부족한 자원은 생산하거나 거래소에서 구매합니다. HUD는 전체 혜성의 확보 재고를 표시합니다.", GuideAction.MissionProduce, target: "mission-stock");
            AddGuide(10, "미션 / 납품과 완료", "모든 요구 소재를 확보했습니다. 노란 박스의 ‘모든 소재 납품’을 누르세요.\n요구 스택만 한 번에 차감되고 보상이 지급됩니다. 소재 하나라도 부족하면 아무 재고도 차감하지 않습니다.", GuideAction.DeliverMission, target: "deliver");
            AddGuide(10, "미션 / 완료 확인", "미션 완료! 납품 자원이 차감되고 보상이 소지금에 반영됐습니다.\n미션 목록에서 완료 상태를 확인할 수 있습니다. 다음은 혜성관리입니다.", target: "credits");
            AddGuide(11, "혜성관리 열기", "노란 박스의 ‘혜성관리’를 누르세요.\n보유 혜성, 자금 변동, 새 혜성 탐사, 수송선 관리를 차례로 살펴봅니다.", GuideAction.OpenManagement, target: "management");
            AddGuide(11, "혜성관리 / 보유 혜성", "보유 혜성의 이름·좌표·남은 자원·시설 수·누적 판매 수익을 확인합니다.\n혜성 슬롯을 선택하면 개발 구역이 전환됩니다. 다른 혜성을 보고 있어도 기존 혜성은 계속 생산합니다.", target: "colonies", view: FoundryPage.Management);
            AddGuide(11, "혜성관리 / 개인자금 변동지표", "현재 소지금은 실시간 금액입니다. 아래 그래프는 시작 금액과 매월 1일의 소지금 기록을 보여줍니다.\n전월 증감은 월초 기록끼리 비교합니다. 6·12·18·24개월 범위를 선택해 자금 흐름을 확인하세요.", target: "balance", view: FoundryPage.Management);
            AddGuide(11, "혜성관리 / 혜성 소개와 탐사", "1,500 솔라로 8초 동안 탐사하면 세 후보가 나타납니다. 크기·자원 매장량·개척 비용을 비교해 선택하세요.\n개척하면 항구가 포함된 새 혜성을 얻습니다. 새 혜성에는 전력·창고·물류와 생산시설을 준비해야 합니다.", target: "exploration", view: FoundryPage.Management);
            AddGuide(11, "혜성관리 / 수송선 관리", "수송선·출발 혜성·도착 혜성·물자·수량을 선택하고 비용·시간을 확인해 운송합니다. 두 혜성에 선착장이 필요합니다.\n수송선은 100스택을 싣습니다. 추가 구매는 12,000 솔라입니다. 하역 공간이 없으면 선내에서 대기합니다.", target: "fleet", view: FoundryPage.Management, showFleet: true);
            AddGuide(12, "혜성 구매 / 탐사 시작", "노란 박스의 ‘새 탐사 시작’을 누르세요. 탐사비 1,500 솔라를 사용합니다.\n이미 탐사 후보가 있으면 후보 선택으로 바로 이어집니다.", GuideAction.Survey, target: "survey", view: FoundryPage.Management);
            AddGuide(12, "혜성 구매 / 탐사 완료 대기", "탐사가 완료될 때까지 기다리세요. 기본 속도에서 8초가 걸립니다.\n탐사가 끝나면 후보 선택 단계로 자동 진행합니다.", GuideAction.WaitSurvey, target: "survey", view: FoundryPage.Management);
            AddGuide(12, "혜성 구매 / 후보 선택", "세 후보의 크기·매장량·개척 비용을 비교하고 하나를 클릭하세요.\n항구를 포함한 가격입니다. 소지금으로 구매할 수 있는 후보를 선택합니다.", GuideAction.SelectProspect, target: "prospect", view: FoundryPage.Management);
            AddGuide(12, "혜성 구매 / 소유권 확보", "‘선택한 혜성 개척하기’를 누르세요. 개척비가 차감되고 항구가 포함된 혜성이 보유 목록에 추가됩니다.\n실제 구매가 완료되면 이동 안내로 자동 진행합니다.", GuideAction.PurchaseColony, target: "purchase-colony", view: FoundryPage.Management);
            AddGuide(13, "혜성 간 이동 / 새 혜성 선택", "노란 박스의 새 혜성 슬롯을 클릭하세요. 선택한 혜성의 개발 구역으로 전환됩니다.\n이 화면 전환은 물자를 자동으로 옮기지 않습니다. 물자는 수송선으로 별도로 운송합니다.", GuideAction.SwitchDestination, target: "destination-colony", view: FoundryPage.Management);
            AddGuide(13, "혜성 간 이동 / 개발 구역 확인", "구매한 혜성의 개발 보드입니다. 항구와 자원 노드가 준비되어 있습니다.\n각 혜성은 전력·재고·물류를 따로 갖습니다. 생산을 시작하려면 발전소와 생산·지원 시설을 건설하세요.", target: "colony-board");
            AddGuide(13, "혜성 간 이동 / 출발 혜성 복귀", "보유 혜성 목록에서 노란 박스의 출발 혜성을 클릭하세요.\n이제 출발 혜성의 물자를 새 혜성으로 실제 운송합니다.", GuideAction.SwitchOrigin, target: "origin-colony", view: FoundryPage.Management);
            AddGuide(14, "물자 이동 / 화물 준비", "출발 혜성에서 물 3스택을 확보하세요. 거래소의 ‘구입’ → 수량 3 → ‘거래 확인’을 누릅니다.\n이미 물 3스택이 있으면 해당 재고를 사용해 자동 진행합니다.", GuideAction.PrepareCargo, target: "trade", view: FoundryPage.Exchange);
            AddGuide(14, "물자 이동 / 수송선 관리 열기", "노란 박스의 ‘수송선 관리’를 누르세요.\n대기 중인 수송선으로 물 3스택을 새 혜성에 보냅니다. 출발과 도착 모두 선착장이 필요합니다.", GuideAction.OpenFleet, target: "fleet-tab", view: FoundryPage.Management);
            AddGuide(14, "물자 이동 / 출발 혜성 선택", "출발 혜성 선택의 ◀ / ▶로 물을 준비한 혜성을 선택하세요.\n아래 ‘출발 재고’가 해당 혜성에 보관된 물자 수량입니다.", GuideAction.SelectSource, target: "ship-source", view: FoundryPage.Management, showFleet: true);
            AddGuide(14, "물자 이동 / 도착 혜성 선택", "도착 혜성 선택의 ◀ / ▶로 구매한 새 혜성을 선택하세요.\n출발과 도착은 서로 다른 혜성이어야 합니다.", GuideAction.SelectDestination, target: "ship-destination", view: FoundryPage.Management, showFleet: true);
            AddGuide(14, "물자 이동 / 물과 수량 선택", "운송 물자 선택의 ◀ / ▶로 ‘물’을 선택하세요. 수량 슬라이더로 3스택 이상을 지정합니다.\n수송선은 최대 100스택을 적재합니다. 운송 비용과 소요 시간을 확인하세요.", GuideAction.SelectCargo, target: "ship-cargo", view: FoundryPage.Management, showFleet: true);
            AddGuide(14, "물자 이동 / 운송 시작", "대기 중인 수송선을 선택하고 ‘운송 시작’을 누르세요.\n출발 재고가 수송선에 실리고 운송 비용이 차감됩니다. 도착할 때까지 화물은 수송선에 보관됩니다.", GuideAction.DispatchCargo, target: "dispatch", view: FoundryPage.Management, showFleet: true);
            AddGuide(14, "물자 이동 / 도착과 하역 대기", "수송선이 도착해 물을 하역할 때까지 기다리세요. 실제 하역이 끝나야 다음 안내로 넘어갑니다.\n도착 항구가 가득 차면 화물은 선내에 남습니다. 도착 혜성의 재고를 판매해 공간을 확보하세요.", GuideAction.WaitCargo, target: "ship-status", view: FoundryPage.Management, showFleet: true);
            AddGuide(14, "물자 이동 / 도착 혜성으로 이동", "노란 박스의 도착 혜성 슬롯을 클릭하세요.\n다음 단계에서 항구에 하역된 물을 직접 확인합니다.", GuideAction.SwitchDestination, target: "destination-colony", view: FoundryPage.Management);
            AddGuide(14, "물자 이동 / 도착 항구 열기", "노란 박스의 항구를 클릭해 보관 재고 팝업을 여세요.\n출발 혜성에서 보낸 물이 도착 항구에 들어왔는지 확인합니다.", GuideAction.OpenCargoDock, target: "cargo-dock");
            AddGuide(14, "물자 이동 / 하역 재고 확인", "항구 재고에 운송한 물이 표시됩니다. 혜성 선택은 개발 화면을 바꾸고, 수송선은 실제 물자를 옮깁니다.\n이제 다른 소재도 같은 순서로 혜성 간 운송할 수 있습니다.", target: "cargo-stock");
            AddGuide(14, "튜토리얼 완료", "생산·거래·미션과 혜성 구매, 혜성 간 이동, 물자 운송을 모두 실습했습니다.\n상단 ‘저장’으로 개척 진행을 기록하세요. ‘튜토리얼’ 버튼으로 처음부터 다시 안내받을 수 있습니다.", target: "save", view: FoundryPage.Management, showFleet: true);
        }
        private void StartTutorialGuide()
        {
            session.StopMissionPractice(); practicePopup = false;
            tutorialChoice = false; tutorialGuide = true; guidePage = 0;
            guideOriginColony = session.ActiveColonyIndex; guideDestinationColony = -1; guideVoyage = null;
            CreateGuidedSteps(); EnterGuideStep();
        }
        private FacilityInstance GuideBuilding(string id) => session.Comet.Facilities.FirstOrDefault(f => f.Definition.FacilityId == id);
        private void EnterGuideStep()
        {
            var step = CurrentGuide;
            selected = null; build = null; demolishMode = false; demolitionTarget = null; missionList = false;
            page = step.Page; fleet = step.Fleet;
            if (step.Action == GuideAction.PlaceBuilding) build = session.Content.FindBuilding(step.Building);
            if (step.Action == GuideAction.Survey || step.Action == GuideAction.WaitSurvey || step.Action == GuideAction.WaitCargo) speed = Math.Max(1, speed);
            if (step.Action == GuideAction.SelectProspect) prospectIndex = -1;
            if (step.Action == GuideAction.PurchaseColony) guideColonyCount = session.Colonies.Count;
            if (step.Action == GuideAction.SwitchDestination || step.Action == GuideAction.SwitchOrigin)
            {
                int targetIndex = step.Action == GuideAction.SwitchDestination ? guideDestinationColony : guideOriginColony;
                colonyScroll.y = Mathf.Clamp(targetIndex * 95 - 95, 0, Math.Max(0, session.Colonies.Count * 95 - 285));
            }
            if (step.Action == GuideAction.PrepareCargo)
            { materialIndex = session.Content.Resources.FindIndex(r => r.ResourceId == GuideCargoId); buying = true; tradeQuantity = GuideCargoAmount; }
            if (step.Action == GuideAction.SelectSource) shipFrom = guideDestinationColony;
            if (step.Action == GuideAction.SelectDestination) shipTo = guideOriginColony;
            if (step.Action == GuideAction.SelectCargo) { cargoIndex = 0; cargoQuantity = 1; }
            if (step.Action == GuideAction.DispatchCargo) guideExistingVoyages = new HashSet<FoundryVoyage>(session.Voyages);
            if (step.Target == "cargo-stock" && guideDestinationColony >= 0) selected = session.Dock(session.Colonies[guideDestinationColony]);
            if (step.Building != null)
            {
                if (guideFacility == null || guideFacility.Definition.FacilityId != step.Building || !session.Comet.Facilities.Contains(guideFacility)) guideFacility = GuideBuilding(step.Building);
                if (step.Action == GuideAction.SelectBuilding || (step.Action == GuideAction.PlaceBuilding &&
                    (guideExistingFacilities == null || guideConstructionBuilding != step.Building || guideConstructionColony != session.ActiveColony)))
                {
                    guideExistingFacilities = new HashSet<FacilityInstance>(session.Comet.Facilities);
                    guideConstructionBuilding = step.Building; guideConstructionColony = session.ActiveColony;
                }
                int index = session.Content.Buildings.FindIndex(b => b.FacilityId == step.Building);
                buildScroll.y = Mathf.Clamp(index * 54 - 120, 0, Math.Max(0, session.Content.Buildings.Count * 54 - 532));
            }
            if (step.Action == GuideAction.SelectRecipe || step.Action == GuideAction.ConfirmRecipe)
            { selected = guideFacility; preview = guideFacility == null ? null : step.Action == GuideAction.ConfirmRecipe ? session.Content.FindRecipe(step.Recipe) : FirstAllowed(guideFacility); }
            if (step.Action == GuideAction.Produce) { guideProductionStart = session.ProducedBy(guideFacility, step.Recipe); speed = Math.Max(1, speed); }
            if (step.Action == GuideAction.Buy || step.Action == GuideAction.Sell)
            { guideTradeCount = step.Action == GuideAction.Buy ? session.ManualPurchases : session.ManualSales; buying = step.Action == GuideAction.Buy; tradeQuantity = 1; }
            if (step.Action == GuideAction.AcceptMission)
            {
                missionList = true;
                guideMissionId = session.ActiveMission?.Id ?? session.Missions.FirstOrDefault(m => !session.MissionCompleted(m.Id))?.Id;
                int index = session.Missions.ToList().FindIndex(m => m.Id == guideMissionId);
                missionScroll.y = index < 0 ? 0 : Math.Max(0, MissionRowOffset(index) - 88);
            }
            if (step.Action == GuideAction.MissionProduce) speed = Math.Max(1, speed);
        }
        private void NextGuideStep()
        {
            if (guidePage + 1 >= guidedSteps.Count) { tutorialGuide = false; selected = null; session.SkipTutorial(); notice = $"{GuideChapterCount}개 주제 튜토리얼을 완료했습니다. 저장으로 진행을 기록하세요."; return; }
            guidePage++; EnterGuideStep();
        }
        private bool GuideObjectiveMet()
        {
            var step = CurrentGuide;
            switch (step.Action)
            {
                case GuideAction.SelectBuilding: return build?.FacilityId == step.Building;
                case GuideAction.PlaceBuilding:
                    return FindGuidedConstruction(step.Building) != null;
                case GuideAction.OpenFacility: if (selected?.Definition.FacilityId != step.Building) return false; guideFacility = selected; return true;
                case GuideAction.SelectRecipe: return selected == guideFacility && preview?.RecipeId == step.Recipe;
                case GuideAction.ConfirmRecipe: return selected == null && guideFacility?.ActiveRecipe?.RecipeId == step.Recipe;
                case GuideAction.Produce:
                    double required = step.Recipe == "draw-wire" ? 9 : step.Recipe == "make-circuit" ? 2 : session.Content.FindRecipe(step.Recipe).Outputs[0].Stacks;
                    return session.ProducedBy(guideFacility, step.Recipe) - guideProductionStart + 1e-8 >= required;
                case GuideAction.OpenExchange: return page == FoundryPage.Exchange;
                case GuideAction.Buy: return session.ManualPurchases > guideTradeCount;
                case GuideAction.Sell: return session.ManualSales > guideTradeCount;
                case GuideAction.OpenMissions: return missionList;
                case GuideAction.AcceptMission:
                    if (session.ActiveMission != null) { guideMissionId = session.ActiveMission.Id; return true; }
                    return guideMissionId == null;
                case GuideAction.MissionProduce: return guideMissionId == null || session.MissionCompleted(guideMissionId) || (session.ActiveMission?.Id == guideMissionId && session.CanDeliverMission);
                case GuideAction.DeliverMission: return guideMissionId == null || session.MissionCompleted(guideMissionId);
                case GuideAction.OpenManagement: return page == FoundryPage.Management;
                case GuideAction.Survey: return session.IsExploring || session.Prospects.Count > 0;
                case GuideAction.WaitSurvey: return !session.IsExploring && session.Prospects.Count > 0;
                case GuideAction.SelectProspect: return prospectIndex >= 0 && prospectIndex < session.Prospects.Count;
                case GuideAction.PurchaseColony:
                    if (session.Colonies.Count <= guideColonyCount) return false;
                    guideDestinationColony = session.Colonies.Count - 1; return true;
                case GuideAction.SwitchDestination: return guideDestinationColony >= 0 && session.ActiveColonyIndex == guideDestinationColony;
                case GuideAction.SwitchOrigin: return session.ActiveColonyIndex == guideOriginColony;
                case GuideAction.PrepareCargo: return session.Stock(session.Colonies[guideOriginColony], session.Content.FindResource(GuideCargoId)) >= GuideCargoAmount;
                case GuideAction.OpenFleet: return page == FoundryPage.Management && fleet;
                case GuideAction.SelectSource: return shipFrom == guideOriginColony;
                case GuideAction.SelectDestination: return shipTo == guideDestinationColony;
                case GuideAction.SelectCargo: return session.Content.Resources[cargoIndex].ResourceId == GuideCargoId && cargoQuantity >= GuideCargoAmount;
                case GuideAction.DispatchCargo:
                    var voyage = session.Voyages.FirstOrDefault(v => !guideExistingVoyages.Contains(v) && v.Source == session.Colonies[guideOriginColony] &&
                        v.Destination == session.Colonies[guideDestinationColony] && v.Ship.Cargo.GetAmount(session.Content.FindResource(GuideCargoId)) >= GuideCargoAmount);
                    if (voyage == null) return false;
                    guideVoyage = voyage; guideDestinationStock = session.Stock(voyage.Destination, session.Content.FindResource(GuideCargoId)); return true;
                case GuideAction.WaitCargo: return guideVoyage != null && !session.Voyages.Contains(guideVoyage) &&
                    guideVoyage.Ship.Cargo.UsedStacks <= 1e-8 && session.Stock(guideVoyage.Destination, session.Content.FindResource(GuideCargoId)) >= guideDestinationStock + GuideCargoAmount;
                case GuideAction.OpenCargoDock: return guideDestinationColony >= 0 && selected != null && selected == session.Dock(session.Colonies[guideDestinationColony]);
                default: return false;
            }
        }
        private void CheckGuidedTutorial()
        {
            if (!tutorialGuide || CurrentGuide.Action == GuideAction.Read) return;
            if (CheckGuidedConstruction()) return;
            if (GuideObjectiveMet()) { NextGuideStep(); return; }
            var step = CurrentGuide;
            if (step.Chapter >= 9 && !session.HasMarket)
            {
                guidePage = guidedSteps.FindIndex(s => s.Chapter == 8);
                EnterGuideStep(); notice = "거래소 터미널을 다시 건설해야 다음 안내를 진행할 수 있습니다."; return;
            }
            if ((step.Action == GuideAction.MissionProduce || step.Action == GuideAction.DeliverMission) && guideMissionId != null &&
                !session.MissionCompleted(guideMissionId) && session.ActiveMission?.Id != guideMissionId)
            {
                guidePage = guidedSteps.FindIndex(s => s.Action == GuideAction.AcceptMission);
                EnterGuideStep(); return;
            }
            // Reopening a popup or replacing a demolished building remains possible.
            if (step.Building != null && step.Action != GuideAction.SelectBuilding && step.Action != GuideAction.PlaceBuilding &&
                (guideFacility == null || !session.Comet.Facilities.Contains(guideFacility)))
            {
                guidePage = guidedSteps.FindIndex(s => s.Building == step.Building && s.Action == GuideAction.SelectBuilding);
                EnterGuideStep(); notice = "필요한 시설을 다시 건설하거나 기존 시설을 선택하세요.";
            }
            if ((step.Action == GuideAction.SelectRecipe || step.Action == GuideAction.ConfirmRecipe) && selected == null)
            { selected = guideFacility; preview = session.Content.FindRecipe(step.Recipe); }
        }
        private FacilityInstance FindGuidedConstruction(string buildingId)
        {
            if (guideExistingFacilities == null || guideConstructionBuilding != buildingId || guideConstructionColony != session.ActiveColony) return null;
            var constructed = session.Comet.Facilities.FirstOrDefault(f => f.Definition.FacilityId == buildingId &&
                !guideExistingFacilities.Contains(f) && IsGuidedFacilityPlaced(f));
            if (constructed != null) guideFacility = constructed;
            return constructed;
        }
        private bool IsGuidedFacilityPlaced(FacilityInstance facility)
        {
            var size = facility.Definition.FootprintSize;
            for (int x = 0; x < size.x; x++) for (int y = 0; y < size.y; y++)
            {
                var tile = session.Comet.Grid.GetTile(facility.Anchor + new Vector2Int(x, y));
                if (tile == null || tile.Occupant != CometTileOccupant.Facility || !tile.OccupantId.Equals(facility.Id)) return false;
            }
            return session.FacilityAt(facility.Anchor) == facility;
        }
        private bool CheckGuidedConstruction()
        {
            if (!tutorialGuide) return false;
            var step = CurrentGuide;
            if (step.Action != GuideAction.SelectBuilding && step.Action != GuideAction.PlaceBuilding) return false;
            if (FindGuidedConstruction(step.Building) == null) return false;
            // Advance from the actual placed instance, even when demolition and rebuilding keep the total count unchanged.
            int next = guidedSteps.FindIndex(s => s.Chapter == step.Chapter && s.Building == step.Building && s.Recipe == step.Recipe && s.Action == GuideAction.OpenFacility);
            if (next < 0) next = guidedSteps.FindIndex(s => s.Action == GuideAction.OpenExchange);
            guidePage = next; EnterGuideStep(); return true;
        }
        private Rect GuideBuildButton(string id)
        {
            int index = session.Content.Buildings.FindIndex(b => b.FacilityId == id);
            return new Rect(40, 209 + index * 54 - buildScroll.y, 248, 53);
        }
        private Vector2Int? GuidePlacement(GuideStep step)
        {
            var definition = session.Content.FindBuilding(step.Building);
            for (int y = 0; y < session.Comet.Grid.Height; y++) for (int x = 0; x < session.Comet.Grid.Width; x++)
            {
                var position = new Vector2Int(x, y);
                if (session.PlacementError(definition, position) != null) continue;
                if (definition.FacilityType == FacilityType.Mine && session.DepositAt(position)?.Resource != session.Content.FindRecipe(step.Recipe).Outputs[0].Resource) continue;
                // Leave a clear border around production facilities for drone routes.
                bool clear = true;
                if (definition.FacilityType != FacilityType.Mine)
                    for (int dx = -1; dx <= definition.FootprintSize.x; dx++) for (int dy = -1; dy <= definition.FootprintSize.y; dy++)
                    {
                        var nearby = new Vector2Int(x + dx, y + dy);
                        if (session.FacilityAt(nearby) != null || session.DepositAt(nearby) != null) clear = false;
                    }
                if (clear) return position;
            }
            return null;
        }
        private Rect GuideTarget()
        {
            var step = CurrentGuide;
            if (step.Action == GuideAction.SelectBuilding || step.Target == "build") return GuideBuildButton(step.Building);
            if (step.Action == GuideAction.PlaceBuilding)
            {
                var position = GuidePlacement(step);
                return position.HasValue ? TileRect(position.Value, session.Content.FindBuilding(step.Building).FootprintSize) : GuideBuildButton(step.Building);
            }
            if (step.Action == GuideAction.SelectRecipe && selected != null)
            {
                int index = selected.Definition.SupportedRecipes.ToList().FindIndex(r => r.RecipeId == step.Recipe);
                return new Rect(274, 287 + index * 64, 254, 55);
            }
            if (step.Action == GuideAction.ConfirmRecipe) return new Rect(557, 689, 370, 47);
            if (step.Action == GuideAction.OpenFacility || step.Action == GuideAction.Produce || step.Target == "facility")
            {
                var facility = step.Target == "facility" ? GuideBuilding(step.Building) : guideFacility;
                if (facility != null) return TileRect(facility.Anchor, facility.Definition.FootprintSize);
                if (step.Target == "facility") return GuideBuildButton(step.Building);
            }
            switch (step.Target)
            {
                case "power": return new Rect(604, 57, 258, 32);
                case "credits": return new Rect(604, 24, 258, 31);
                case "save": return new Rect(1117, 29, 75, 34);
                case "exchange": return new Rect(1150, 714, 246, 33);
                case "materials": return new Rect(24, 164, 292, 630);
                case "prices": return new Rect(350, 228, 746, 439);
                case "trade": return new Rect(1132, 164, 284, 630);
                case "missions": return new Rect(1150, 375, 246, 29);
                case "mission-row":
                    int index = session.Missions.ToList().FindIndex(m => m.Id == guideMissionId);
                    if (index < 0) return new Rect(286, 194, 860, 27);
                    return new Rect(976, 255 + MissionRowOffset(index) - missionScroll.y, 138, 46);
                case "mission-stock": return new Rect(1150, 210, 246, 160);
                case "deliver": return new Rect(1150, 410, 246, 30);
                case "management": return new Rect(1150, 752, 246, 33);
                case "colonies": return new Rect(40, 178, 687, 324);
                case "balance": return new Rect(42, 515, 681, 258);
                case "exploration": return new Rect(778, 178, 618, 576);
                case "fleet": return new Rect(778, 178, 618, 576);
                case "survey": return new Rect(780, 297, 610, 40);
                case "prospect": return new Rect(780, 352, 610, 313);
                case "purchase-colony": return new Rect(780, 698, 610, 52);
                case "destination-colony": return new Rect(40, 217 + Math.Max(0, guideDestinationColony) * 95 - colonyScroll.y, 654, 88);
                case "origin-colony": return new Rect(40, 217 + guideOriginColony * 95 - colonyScroll.y, 654, 88);
                case "colony-board": return MapRect;
                case "fleet-tab": return new Rect(1088, 178, 308, 37);
                case "ship-source": return new Rect(915, 292, 475, 32);
                case "ship-destination": return new Rect(915, 337, 475, 32);
                case "ship-cargo": return new Rect(780, 382, 610, 123);
                case "dispatch": return new Rect(780, 644, 610, 44);
                case "ship-status": return new Rect(915, 239, 475, 32);
                case "cargo-dock":
                    var dock = guideDestinationColony < 0 ? null : session.Dock(session.Colonies[guideDestinationColony]);
                    return dock == null ? MapRect : TileRect(dock.Anchor, dock.Definition.FootprintSize);
                case "cargo-stock": return new Rect(274, 377, 888, 50);
            }
            return new Rect(1200, 29, 112, 34);
        }
        private Rect GuidePopupRect(Rect target)
        {
            // Keep the entire instruction panel away from the highlighted control.
            if (selected != null)
            {
                var besideProduction = new Rect(24, 454, 480, 340);
                if (!besideProduction.Overlaps(new Rect(target.x - 6, target.y - 6, target.width + 12, target.height + 12))) return besideProduction;
            }
            foreach (var candidate in new[] { new Rect(936, 120, 480, 340), new Rect(24, 120, 480, 340), new Rect(936, 454, 480, 340), new Rect(24, 454, 480, 340), new Rect(460, 454, 480, 340) })
                if (!candidate.Overlaps(new Rect(target.x - 6, target.y - 6, target.width + 12, target.height + 12))) return candidate;
            // The central chart leaves only a narrow side column free.
            return new Rect(24, 164, 292, 420);
        }
        private void DrawGuidedTutorial()
        {
            drawingGuide = true;
            GUI.enabled = true;
            var step = CurrentGuide; var target = GuideTarget(); var window = GuidePopupRect(target);
            Fill(new Rect(target.x - 4, target.y - 4, target.width + 8, 4), TutorialYellow);
            Fill(new Rect(target.x - 4, target.yMax, target.width + 8, 4), TutorialYellow);
            Fill(new Rect(target.x - 4, target.y, 4, target.height), TutorialYellow);
            Fill(new Rect(target.xMax, target.y, 4, target.height), TutorialYellow);
            Fill(window, Panel); Fill(new Rect(window.x, window.y, window.width, 4), TutorialYellow);
            float contentWidth = window.width - 36, buttonY = window.yMax - 42;
            Label(new Rect(window.x + 18, window.y + 13, contentWidth, 22), $"튜토리얼 {step.Chapter}/{GuideChapterCount} · 세부 단계 {guidePage + 1}/{guidedSteps.Count}", Style(window.width < 400 ? 11 : 13, TutorialYellow));
            Label(new Rect(window.x + 18, window.y + 42, contentWidth, 40), step.Title, Style(window.width < 400 ? 16 : 17, Ink, FontStyle.Bold));
            string body = step.Body;
            if (step.Action == GuideAction.SelectSource) body += "\n선택할 출발 혜성: " + session.Colonies[guideOriginColony].Name;
            if (step.Action == GuideAction.SelectDestination && guideDestinationColony >= 0) body += "\n선택할 도착 혜성: " + session.Colonies[guideDestinationColony].Name;
            if (step.Action == GuideAction.MissionProduce && session.ActiveMission != null && session.ActiveMission.Id != FoundrySession.PracticeMissionId)
            {
                var mission = session.ActiveMission;
                body = "수주한 미션: " + mission.Name + "\n" + string.Join("\n", mission.Requirements.Select(r => $"{session.Content.FindResource(r.ResourceId).DisplayName} {r.Amount}스택")) +
                    "\n모든 소재를 생산하거나 구매하세요. 전체 혜성의 시설 재고를 모으면 일괄 납품할 수 있습니다.";
            }
            Label(new Rect(window.x + 18, window.y + 88, contentWidth, window.height - 165), body, Style(15, Ink));
            string status = step.Action == GuideAction.Read ? "설명 중 시간 정지 · 읽은 뒤 다음 설명으로 진행합니다." : "목표를 직접 수행하세요. 완료 시 자동으로 다음 단계로 넘어갑니다.";
            if (step.Action == GuideAction.PlaceBuilding) status = "건설 완료 자동 감지 중 · 확인 버튼 없이 타일에 배치하세요.";
            if (step.Action == GuideAction.WaitSurvey) status = $"탐사 완료까지 {session.SurveyRemainingSeconds:0.0}초 · 완료 시 자동 진행";
            if (step.Action == GuideAction.WaitCargo && guideVoyage != null) status = $"수송 상태: {guideVoyage.Ship.Status} · 선내 화물 {guideVoyage.Ship.Cargo.UsedStacks:0.#}스택";
            if (step.Action == GuideAction.Produce) status = "생산 상태: " + session.Status(guideFacility) + (step.Recipe == "draw-wire" ? $" · 구리선 {session.ProducedBy(guideFacility, step.Recipe) - guideProductionStart:0.#}/9" : step.Recipe == "make-circuit" ? $" · 회로기판 {session.ProducedBy(guideFacility, step.Recipe) - guideProductionStart:0.#}/2" : "");
            Label(new Rect(window.x + 18, window.yMax - 73, contentWidth, 26), status, small);
            if (step.Action == GuideAction.Read)
            {
                if (Btn(new Rect(window.x + 18, buttonY, contentWidth - 108, 30), guidePage == guidedSteps.Count - 1 ? "완료 · 플레이 계속" : "확인 · 다음 설명", true)) NextGuideStep();
            }
            else if ((step.Action == GuideAction.SelectBuilding || step.Action == GuideAction.PlaceBuilding) && GuideBuilding(step.Building) != null)
            {
                if (Btn(new Rect(window.x + 18, buttonY, contentWidth - 108, 30), "기존 시설 사용", true))
                {
                    guideFacility = GuideBuilding(step.Building);
                    guidePage = guidedSteps.FindIndex(s => s.Chapter == step.Chapter && s.Building == step.Building && s.Recipe == step.Recipe && s.Action == GuideAction.OpenFacility);
                    // The terminal has no production popup: continue with chapter 9.
                    if (guidePage < 0) guidePage = guidedSteps.FindIndex(s => s.Action == GuideAction.OpenExchange);
                    EnterGuideStep();
                }
            }
            if (Btn(new Rect(window.xMax - 116, buttonY, 98, 30), "안내 종료")) { tutorialGuide = false; selected = null; build = null; missionList = false; notice = "튜토리얼 안내를 종료했습니다. 상단 버튼으로 다시 시작할 수 있습니다."; }
            drawingGuide = false;
        }
        private IEnumerator CaptureGuidedReview(string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(2);
            showingTitle = false; StartTutorialGuide();
            int previousChapter = 0;
            for (int i = 0; i < guidedSteps.Count; i++)
            {
                var step = guidedSteps[i];
                if (step.Building != null && GuideBuilding(step.Building) == null &&
                    step.Action != GuideAction.SelectBuilding)
                {
                    var anchor = GuidePlacement(step);
                    if (anchor.HasValue) session.Build(session.Content.FindBuilding(step.Building), anchor.Value, out _);
                }
                bool capture = step.Chapter != previousChapter || step.Action == GuideAction.SelectRecipe || step.Action == GuideAction.ConfirmRecipe ||
                    step.Target == "prices" || step.Target == "materials" || step.Target == "trade" || step.Target == "mission-row" ||
                    step.Target == "balance" || step.Target == "exploration" || step.Target == "fleet";
                previousChapter = step.Chapter;
                if (!capture) continue;
                guidePage = i; EnterGuideStep();
                yield return new WaitForEndOfFrame();
                CaptureFrame(Path.Combine(directory, $"{step.Chapter:00}-{i + 1:00}-{step.Action}.png"));
            }
            tutorialGuide = false; Application.Quit();
        }
    }
}
