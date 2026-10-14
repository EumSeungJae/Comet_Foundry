# Comet Foundry 개발 기록 구역

이 폴더는 Comet Foundry 개발 흐름을 끊기지 않게 유지하기 위한 작업 기록 공간이다.

게임에 직접 포함되는 런타임 문서는 `Assets/CometFoundry/Docs`에 둔다. 이곳은 개발 중 판단, 작업 로그, 다음 작업, 인수인계를 기록한다.

## 현재 구현 상태

기준일: 2026-10-14

튜토리얼은 14개 주제, 82개 단계로 구성한다. 노란색 박스로 조작 대상을 표시하고 각 단계의 팝업에서 할 일을 안내한다. 생산 시설 건설부터 거래소, 미션, 혜성 관리, 혜성 구매, 혜성 간 이동, 물자 수송까지 순서대로 진행한다.

건물 선택이나 설치 전에 튜토리얼 확인 버튼을 누를 필요가 없다. 새 건물의 실제 배치와 점유 상태를 코드로 확인하면 다음 단계로 자동 진행한다. 생산 및 거래 등의 행동 단계도 조건 달성 시 자동 진행하며, 설명을 읽는 단계에서만 다음 버튼을 사용한다.

미션은 튜토리얼용 1개와 일반 미션 15개다. 일반 미션은 2차·3차·4차 가공 단계별로 5개씩 구성하며, 각 미션은 2~5종의 소재를 요구한다. 요구 소재를 모두 확보해야 납품할 수 있고 보상은 완료 시 한 번만 지급한다.

| 미션 | 주요 보상 및 요구 사항 |
| --- | --- |
| 10. 신규 혜성 개척 지원 | 자금 29,600 및 혜성 1개 |
| 11. 수송선 정비 계약 | 자금 28,100 및 수송선 1대 |
| 15. 우주선 부품납품 | 5종 소재 총 280개 납품, 자금 197,300 |

전체 요구 소재와 보상 산정 기준은 [미션 구성표](Planning/MissionCatalog-2026-10-14.md)에 정리했다. 저장 형식은 버전 4이며 버전 2·3 저장 데이터도 불러온다. 이전 저장에서 수주 중이던 기존 미션은 기존 계약 조건을 유지한다.

## 실행 및 확인

- Windows 실행 파일: [CometFoundry.exe](../../Builds/CometFoundry/CometFoundry.exe)
- Unity 플레이 장면: [FoundryPlayableScene.unity](../../Assets/CometFoundry/Scenes/FoundryPlayableScene.unity)
- 조작과 게임 진행 안내: [플레이 가이드](../../Assets/CometFoundry/Docs/PlayableGuide.md)
- 최신 검증: [미션 개편 검증 결과](Validation/MissionCatalog-2026-10-14/README.md)
- 튜토리얼 검증: [튜토리얼 검증 결과](Validation/Tutorial-2026-10-14/README.md), [건설 자동 진행 검증](Validation/Tutorial-2026-10-14/AutoConstruction.md)
- 해상도 검증: [해상도별 UI 결과와 실제 화면](Validation/Resolution-2026-10-14/README.md)
- 생산·물류 검증: [중복 시설 생산 중단 원인과 수정](Validation/ProductionStall-2026-10-14/README.md)

최신 생산·물류 수정 관련 테스트 51개가 모두 통과했고 Windows 빌드 및 실행 종료를 확인했다. 저장소 전체 검사는 97개 중 92개 통과이며 수정 전부터 있던 5개 실패는 검증 보고서에 기록했다. 표시 창으로 실제 5개 해상도의 대표 화면을 검수했으며 혜성 자원 설명의 잘림을 수정했다. QHD·울트라와이드 요청은 현재 환경에서 1920×1080으로 생성되어 실제 해당 크기의 출력 검증은 남아 있다. Unity에서 이미 플레이 중이라면 플레이를 종료하고 스크립트 컴파일이 끝난 뒤 다시 실행해야 변경 사항을 확인할 수 있다.

## 폴더 안내

최근 수정: [드론 통로 튜토리얼·혜성 내부 손상 완화](IssueHistory/CometRoutes-2026-10-14.md).

최근 수정: [드론 4대 병렬 운송·생산 대기열 2개 제한](IssueHistory/ParallelDrones-2026-10-14.md).

- `Logs`: 실제 작업 내역과 변경 기록
- `Planning`: 우선순위, 구현 순서, 설계 메모
- `Handoff`: 다음 작업 세션에서 바로 이어가기 위한 요약
- `IssueHistory`: 이전 문제점, 검증 보고서, 실행 로그 모음. [이슈 자료 안내](IssueHistory/README.md)와 [실행 로그 한글 안내](IssueHistory/실행로그_안내.md)를 참고한다.
- `Validation`: 작업별 테스트, 빌드, 실행 검증 기록
- `Portfolio`: 기존 프로젝트 소개 및 포트폴리오 자료. 최신 14쪽 포트폴리오와 기술서·테스트 근거는 [Presentations/Portfolio 안내](../../Presentations/Portfolio/README.md)에 모았다.

## 기록 규칙

큰 작업을 할 때마다 아래 문서를 갱신한다.

- [Logs/WorkLog.md](Logs/WorkLog.md): 오늘 무엇을 했는지
- [Planning/NextSteps.md](Planning/NextSteps.md): 다음에 무엇을 할지
- [Planning/DevelopmentRoadmap.md](Planning/DevelopmentRoadmap.md): 개발 축, 분야별 작업 범위, 장기 우선순위
- [Planning/GDD.md](Planning/GDD.md): 게임 디자인 문서
- [Planning/DecisionLog.md](Planning/DecisionLog.md): 되돌리기 어려운 설계 결정을 왜 했는지
- [Handoff/CurrentState.md](Handoff/CurrentState.md): 다음 세션 시작점
