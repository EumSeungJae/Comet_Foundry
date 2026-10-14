# 건설 완료 자동 확인과 튜토리얼 확인 버튼 제거

작업일: 2026-10-14

행동 단계에서 추가로 눌러야 했던 `확인 · 직접 진행` 버튼과 `guideActing` 확인 상태를 제거했다. 건설 선택·배치뿐 아니라 생산품 선택 등 이어지는 행동 안내도 바로 조작할 수 있다. 설명 전용 단계의 다음 버튼은 유지한다.

## 실제 건설 확인

- 건설 안내 시작 시 기존 시설 객체 목록을 보관한다.
- 해당 혜성에 목표 종류의 새 시설 객체가 추가되었는지 검사한다.
- 설치 면적의 모든 타일이 시설로 점유되었고, 점유 시설 식별자가 새 시설의 식별자와 일치하는지 검사한다.
- 보드에서 해당 좌표의 시설이 실제로 새 시설 객체인지 확인한다.
- 실제 건설 성공 직후 다음 단계로 즉시 진행한다. 갱신 중에도 같은 검사로 완료를 감지한다.
- 시설 개수만 비교하지 않으므로 기존 시설을 철거한 뒤 같은 자리에 다시 건설해도 정상 진행한다.

검사 구현: `FoundryGuidedTutorialUI.cs`의 `FindGuidedConstruction`, `IsGuidedFacilityPlaced`, `CheckGuidedConstruction`.
건설 성공 연결: `FoundryPlayableController.cs`의 `session.Build` 성공 분기.

## 검증

Unity EditMode 테스트 36개 통과, 실패 0개. `GuidedTutorial-auto-build-tests.xml` 참조.

Windows 실행 파일 갱신 성공: `Builds/CometFoundry/CometFoundry.exe`. 빌드 로그: `GuidedTutorial-auto-build-build.log`.

확인 버튼 없이 전체 튜토리얼을 진행하고, 건설 실패 시 단계 유지, 동일 시설 수에서의 재건설 완료, 시설 목록에만 있고 점유 타일이 없는 상태의 완료 거부를 확인했다. 기존 생산·물류·혜성 구매·이동·운송·미션·저장 테스트도 함께 통과했다.

열려 있는 Unity 에디터를 유지한 상태에서 분리된 검증 사본을 사용했고, 검증 파일과 원본이 일치하는지 해시로 확인했다. Unity에서 이전 코드로 Play를 실행 중이었다면 Play를 정지하고 스크립트 컴파일이 끝난 뒤 다시 실행해야 새 코드가 적용된다.
