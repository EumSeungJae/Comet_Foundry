# 현재 상태

## 2026-10-08 — 첫 플레이 버전 추가

- 시작 장면: `Assets/CometFoundry/Scenes/FoundryPlayableScene.unity` (Build Settings 첫 장면).
- 16종 자원과 16종 레시피, 9종 건설 시설의 플레이 루프 연결.
- 흰색 주색 / 주황색 보조색 건물 그래픽.
- 생산 건물 클릭 → 생산품 선택 팝업. 생산 코스트, 재료 수량/보유량, 소요시간, 완성품 수량/설명/기준가 표시.
- 실제 매장량 채굴, 드론 자동 물류, 시설 재고, 전력 부족 정지, 완제품 판매, 산업장비 5개 납품 목표.
- 일시정지·배속·로컬 저장/불러오기.
- 상세 데이터와 조작법: `Assets/CometFoundry/Docs/PlayableGuide.md`.
- 신규 생산 테스트 8개 통과. 전체 테스트 실행에서 기존 시스템 관련 5개 실패는 가이드에 기록.

아래는 이전 구현 상태 기록이다.

마지막 갱신: 2026-08-12

## 프로젝트

- 이름: Comet Foundry
- 엔진: Unity `6000.3.19f1`
- 장르: 2D 산업 경영 및 물류 시뮬레이션
- 작업 경로: `D:/Programming/project_CF`

## 구현된 것

런타임 프레임워크는 `Assets/CometFoundry` 아래에 있다.

핵심 공통 요소:

- `SimulationClock`
- `SimulationId`
- `Inventory`

정적 데이터:

- `ResourceDefinition`
- `RecipeDefinition`
- `FacilityDefinition`
- `CargoShipDefinition`
- `CometGenerationProfile`
- `ResearchDefinition`

런타임 시뮬레이션:

- `CompanyState`
- `CometState`
- `CometGrid`
- `CometTile`
- `ResourceDeposit`
- `FacilityInstance`
- `PlacementSystem`
- `RailSegment`
- `TransportRoute`
- `AutomationRule`
- `CargoShipState`
- `ResearchSystem`
- `CometFoundryGame`

프로토타입:

- `WorldPrototypeScene`
- `WorldPrototypeSceneController`
- 시작 시 혜성 8개 생성
- 혜성별 8자리 구조화 시드코드 생성
- 1번째 자리 활성화 코드, 2번째 자리 크기 코드, 3번째 자리 자원 종류 수, 4~6번째 자리 자원 종류 코드, 7~8번째 자리 거리 코드 적용
- 활성화 코드와 크기 코드에 따른 내부 보드 크기와 건설 가능 타일 활성화 비율 적용
- 거리 코드 `A0`은 A-0 관문 좌표로 예약하고 혜성 생성에서는 제외
- 새 혜성 거리 코드가 기존 소유 혜성과 중복되면 다시 산출
- 혜성별 월드 위치, 반경, 내부 보드 크기, 자원 매장량 표시
- WASD/방향키 카메라 이동
- 마우스 휠 카메라 줌
- 좌클릭 혜성 선택
- `G` 키로 혜성 추가 생성
- `ConstructionTestScene`
- `ConstructionTestSceneController`
- 12x8 혜성 내부 테스트 보드
- 2x2 테스트 구조물 배치
- 마우스가 지나간 인접 타일 기반 컨베이어 레일 프리뷰
- 레일 확정/취소
- 확정된 레일 방향을 보여주는 이동 구체
- 레일 끝에서 다른 레일 시작점으로 테스트 구체 전달
- 레일 끝에서 건물로 테스트 구체 입고
- 건물 입고 3개 달성 시 빨간색에서 파란색으로 전환
- 파란 건물이 하단 라인에 연결된 컨베이어 시작점으로 생산 구체 배출
- 생산 시 건물 내부 스택 3개 소비
- 입력 조건이 가득 찬 건물 앞에서 컨베이어 구체 대기
- 건물 출력 컨베이어 자연 구체 생성 방지
- 임시 HUD

문서:

- `Assets/CometFoundry/Docs/FrameworkOverview.md`
- `Assets/CometFoundry/Docs/SystemBacklog.md`

개발 기록:

- `Development Area/Comet Foundry/README.md`
- `Development Area/Comet Foundry/Logs/WorkLog.md`
- `Development Area/Comet Foundry/Planning/DecisionLog.md`
- `Development Area/Comet Foundry/Planning/DevelopmentRoadmap.md`
- `Development Area/Comet Foundry/Planning/GDD.md`
- `Development Area/Comet Foundry/Planning/NextSteps.md`
- `Development Area/Comet Foundry/Handoff/CurrentState.md`

## 알려진 검증 한계

- `dotnet build project_CF.sln`은 현재 게임 스크립트를 실질적으로 컴파일하지 못한다. 솔루션에 생성된 Unity C# 프로젝트 항목이 없기 때문이다.
- 이번 세션에서는 `C:/Program Files/Unity/Hub/Editor` 아래에서 Unity `6000.3.19f1`을 확인하지 못했다.
- `git`은 PATH에 잡혀 있지 않았다.

## 다음 시작 지점

Unity `6000.3.19f1`에서 프로젝트를 열어 `.meta`와 `.csproj` 파일을 생성한 뒤, Console에서 컴파일 오류를 확인한다.

월드 1단계 확인은 `Assets/CometFoundry/Scenes/WorldPrototypeScene.unity`에서 진행한다.

확인할 것:

- 시작 시 혜성 8개가 생성되는지
- 혜성마다 활성화 코드, 크기 코드, 자원 코드, 거리 코드를 담은 8자리 시드코드가 생성되는지
- 혜성 크기가 서로 다르게 표시되는지
- 등급별 내부 보드 크기와 건설 가능 타일 활성화 비율이 맞는지
- 거리 코드가 `A0`을 제외하고 `A1`~`Z9` 범위에서 생성되는지
- 새 혜성 거리 코드가 기존 소유 혜성과 중복되지 않는지
- 일부 혜성에 철광석, 구리광석, 얼음 매장량이 붙는지
- 좌클릭으로 혜성을 선택할 수 있는지
- 선택 HUD에 위치, 반경, 내부 보드 크기, 매장 자원이 표시되는지
- WASD/방향키로 카메라가 이동하는지
- 마우스 휠로 줌 인/아웃되는지
- `G`로 혜성이 추가 생성되는지

월드 테스트가 안정적이면 다음에는 혜성 내부 활성화 패턴을 시드코드 해시 기반으로 고정한다. 현재는 생성기 난수 흐름으로 비활성 타일을 고르므로, 나중에 자원 생성 순서가 바뀌면 같은 시드코드의 내부 형태가 흔들릴 수 있다.

그 다음 `Assets/CometFoundry/Scenes/ConstructionTestScene.unity`를 열고 플레이 모드에서 건설 테스트를 진행한다.

확인할 것:

- 좌클릭으로 2x2 구조물이 배치되는지
- 점유된 타일에는 추가 구조물이나 레일이 막히는지
- `Tab`으로 구조물/레일 모드가 전환되는지
- `R`로 테스트 보드가 초기화되는지
- 레일 모드에서 마우스가 지나간 타일만 프리뷰되는지
- 비인접 타일로 건너뛸 때 자동 경로가 만들어지지 않는지
- `Enter` 또는 HUD 버튼으로 레일이 확정되는지
- `Esc` 또는 HUD 버튼으로 프리뷰가 취소되는지
- 레일 확정 후 구체가 드래그한 방향으로 이동하는지
- 첫 번째 레일 끝과 두 번째 레일 시작점을 맞닿게 만들었을 때 구체가 다음 레일로 전달되는지
- 레일 끝을 건물에 맞닿게 만들었을 때 구체가 건물로 입고되는지
- 건물에 3개가 입고되면 색상이 빨간색에서 파란색으로 바뀌는지
- 파란 건물의 바로 아래쪽에 컨베이어 시작점을 만들면 생산 구체가 배출되는지
- 생산 후 건물 내부 스택이 3개 줄어드는지
- 출력 컨베이어 시작점에 자연 구체가 생기지 않는지
- 건물 스택이 가득 찼을 때 입력 구체가 컨베이어 끝에서 대기하는지

## 최근 방향 결정

혜성 내부 개발 화면은 XCOM 방식의 사각 타일 보드로 사용한다. 건물과 물자 이송 레일은 이 보드 위에 배치된다.

회사는 `A0` 좌표의 A-0 관문과 연결된 기업으로 잡았다. A-0 관문은 혜성 좌표와 지구권 거래의 기준점이며, 그 안에서 혜성거래소가 기능한다. 혜성거래소는 플레이어 대신 혜성을 찾고 소개하며, 납품받은 물건을 지구로 보내 판매한다. 건물 건설에는 가공품/완성품 재료가 필요하며, 재료를 전부 자체수급할 수 있으면 건설 비용을 90% 절감한다. 혜성거래소 뉴스는 미래 지구 이벤트의 가능성 신호이며, 실제 이벤트 결과에 따라 제품군별 수요와 시세가 변화한다.

## 임시 비활성화

- `MarketSystem`
- `LogisticsSystem`

건설 테스트를 먼저 진행하기 위해 조건부 컴파일 처리했다. 다시 활성화하려면 scripting define symbol에 `COMET_FOUNDRY_ENABLE_MARKET`, `COMET_FOUNDRY_ENABLE_LOGISTICS`를 추가한다.

## 다음 개발 예정

다음 작업은 시설 체계 확장이다.

1. 원재료 생산시설 제작
2. 원재료 가공시설 제작
3. 가공소재 부품화 및 완제품화 시설 제작
4. 창고시설 제작
5. 판매시설 제작
