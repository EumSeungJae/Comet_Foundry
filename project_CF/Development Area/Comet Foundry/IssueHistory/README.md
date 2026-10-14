# 이전 문제점 및 검증 자료 모음

자료 정리일: 2026-10-14
한글화 작업일: 2026-10-14

원본 경로를 유지하기 위해 자료를 이 폴더에 복사했다. 설명 문서는 한글화했으며, 과거 기록이므로 현재 해결 여부는 별도로 확인해야 한다. 코드 식별자, 명령어, 원본 파일 경로는 정확한 대조를 위해 유지했다.

## 문제점과 검증 문서

| 문서 | 내용 | 원본 경로 |
| --- | --- | --- |
| [기술 부족 사항 목록](TechnicalGapList.md) | 2026-08-26 당시 미완성 기술 13개 분야와 필요 작업 | Assets/CometFoundry/Docs/TechnicalGapList.md |
| [검증 보고서 — 2026-10-08](ValidationReport-2026-10-08.md) | 전체 55개 테스트 중 5개 실패 기록과 시각 검증 한계 | Development Area/Comet Foundry/Validation/Report.md |
| [게임플레이 수정 검증](GameplayRevision-review.md) | 회귀 테스트 결과와 팝업 시각 검수 미완료 기록 | ValidationArtifacts/GameplayRevision/review.md |

## 실행 로그

엔진이 출력한 원시 로그는 검색·진단에 사용할 수 있도록 원문을 보존했다. 주요 문구의 한글 설명은 [실행 로그 읽는 법](실행로그_안내.md)에 정리했다.

- [일반 실행 로그](GameplayRevision-player.log): 원본 ValidationArtifacts/GameplayRevision/player.log.
- [오프스크린 실행 로그](GameplayRevision-player-offscreen.log): 원본 ValidationArtifacts/GameplayRevision/player-offscreen.log.
- [최종 실행 로그](GameplayRevision-player-final.log): 원본 ValidationArtifacts/GameplayRevision/player-final.log.

## 포함되지 않은 원시 결과

검증 보고서에서 참조한 Temp/FoundryValidation-tests.xml 및 Temp/FoundryValidation-production-tests.xml은 자료 정리 시점에 존재하지 않아 포함하지 못했다.