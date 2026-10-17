# Comet Foundry

혜성을 탐사하고 자원을 채굴하여 생산 시설을 확장하는 우주 공장 경영 시뮬레이션 게임.

## 1. 프로젝트 개요

| 프로젝트명     Comet Foundry 
| 장르          우주 공장 경영 시뮬레이션 
| 개발 엔진      Unity 6 
| 개발 언어      C# 
| 개발 환경      Visual Studio Code 
| 플랫폼         WEP
| 개발 인원      1인 개발

## 2. 게임 소개

Comet Foundry는 우주에 존재하는 혜성을 탐사하고, 다양한 광물 자원을 채굴하여 공장을 운영하는 경영 시뮬레이션 게임입니다.

플레이어는 혜성을 확보하고 생산 시설을 건설하여 자원을 가공하며, 거래소를 통해 수익을 창출하고 사업을 확장합니다.

최종적으로 인류 이주 프로젝트에 필요한 우주선 건조를 목표로 합니다.

## 3. 주요 시스템

### 혜성 탐사 시스템
- 무작위 혜성 생성          - 테스트 -
- 혜성 등급 및 자원량 설정
- 탐사 후보 3개 제공
- 혜성 선택 및 확보

### 자원 생산 시스템
- 광물 채굴
- 생산 시설 건설
- 자원 가공 및 부품 제작

### 거래소 시스템
- 자원 구매 및 판매
- 보유 자원 관리
- 거래 신뢰도 시스템

### 혜성 관리 시스템
- 보유 혜성 목록
- 혜성별 생산 시설 관리
- 회사 수익 관리

## 4. 개발 환경

- Unity: [사용 버전]
- Language: C#
- IDE: Visual Studio Code
- Version Control: Git

## 5. 설치 및 실행

A. GitHub 이용
1. 프로젝트 저장소를 복제합니다.
2. Unity Hub를 실행합니다.
3. Add Project를 통해 프로젝트 폴더를 추가합니다.
4. 프로젝트에 맞는 Unity 버전으로 실행합니다.
5. 시작 Scene을 열고 Play 버튼을 누릅니다.

B. UnityPlay 이용
아래 주소 이용

--------

## 6. 프로젝트 구조

```text
Assets/
└── CometFoundry
    ├──── Scenes/
    ├── Scripts/
    ├── TestScripts/
    │   ├── TestCometData.cs        // 객체 데이터 준비
    │   ├── TestCometGenerate.cs    // 다중 객체생성 및 디버그 테스트
    │   ├── TestCometPractice.cs    // 단일 객체 디버그 테스트
    │   └── TestGenerateComet.cs    // 단일 객체생성 테스트
    ├── Prefabs/
    ├── Materials/
    └── UI/
```

※ 실제 프로젝트의 폴더 구성에 맞게 수정합니다.

## 7. 개발 진행 현황

- [x] 혜성 데이터 클래스 구현
- [x] 리스트를 이용한 혜성 관리 실습
- [ ] 혜성 랜덤 생성 시스템 완성
- [ ] 혜성 탐사 UI 구현
- [ ] 혜성 선택 기능
- [ ] 생산 시설 시스템
- [ ] 거래소 시스템

## 8. 개발자 정보

- 개발자: 
- 담당 분야: 게임 기획 / 프로그래밍
- GitHub: https://github.com/EumSeungJae/Comet_Foundry/commits/main/

## 9. 라이선스

라이선스 및 외부 에셋 사용 조건은 추후 명시합니다.
