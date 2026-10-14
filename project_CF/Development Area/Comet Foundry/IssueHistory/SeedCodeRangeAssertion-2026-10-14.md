# 혜성 시드 코드 테스트의 범위 예외 분석

## 정정

이전 안내의 ‘혜성 생성 중 범위 예외’는 발생 위치가 부정확했다. `Generate` 호출은 반환됐고 코드 길이 및 앞 두 등급 문자 검사까지 진행됐다. 이후 테스트의 문자 범위 제약 생성 중 예외가 발생했다. 이 실패 기록은 실제 생성기의 난수 범위 오류를 의미하지 않는다.

## 확인 근거

`ValidationArtifacts/CometRoutes-2026-10-14/tests.xml`의 호출 스택:

1. `NUnit.Framework.Constraints.RangeConstraint` 생성자
2. `NUnit.Framework.Is.InRange`
3. `CometGeneratorTests.Generate_CreatesEightCharacterStructuredSeedCode`, 118줄

실패 줄: `Assert.That(comet.SeedCode[2], Is.InRange('0', '3'));`

같은 검사 방식이 119~120줄의 거리 문자 A~Z 및 숫자 0~9에도 사용된다.

프로젝트의 `com.unity.ext.nunit` 2.0.5 DLL을 별도 PowerShell 프로세스에서 읽기 전용으로 로드하고, 혜성 생성 없이 제약만 생성해 재현했다.

| 입력 범위 | 결과 |
|---|---|
| 문자 '0'~'3' | `from must be less than to` 예외 |
| 문자 'A'~'Z' | 같은 예외 |
| 정수 48~51 | 정상 |

따라서 직접 원인은 현재 NUnit의 문자 범위 제약 처리다. 내부 라이브러리 구현을 변경하거나 생성기 난수 범위를 변경할 필요가 있다는 근거는 없다.

## 후속 조치

문자와 양 끝값을 모두 정수 문자 코드로 바꿔 범위를 검사하거나 명시적인 허용 문자 집합을 검사한다. 예: `Assert.That((int)comet.SeedCode[2], Is.InRange((int)'0', (int)'3'));`. 이후 이 테스트를 다시 실행해야 아직 도달하지 못한 거리·크기 검사까지 성공하는지 확정할 수 있다. 이번 요청에서는 테스트·생성 코드는 변경하지 않고 원인을 분석했다.

시장 이벤트·신뢰도 가격·의뢰 갱신의 나머지 실패 4개는 사용자 지시에 따라 [차후 개선 항목](../Planning/NextSteps.md)에 기록했다.
