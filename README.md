# Project - CometFoundry

## 프로젝트 소개
Comet Foundry는 혜성을 탐사하고 자원을 채굴하여 공장을 운영하는 우주 공장을 경영하는 게임입니다.

## 개발 환경
- 게임 엔진 : Unity 6
- 프로그래밍 언어 : C#
- 개발도구 : Visual Studio Code


### [AIscript](https://github.com/EumSeungJae/Comet_Foundry/tree/main/project_CF/Assets/CometFoundry/Scripts)
코멧파운드리를 만들기위해 AI가 작성하고 만들어낸 코드


### [MYscript](https://github.com/EumSeungJae/Comet_Foundry/tree/main/project_CF/Assets/CometFoundry/TestScripts)
코멧파운드리를 구현하기위해 직접 기능을 구현해낸 코드

## 구현된 주요 시스템

### 1. 혜성 탐사 시스템
- 탐사 시 무작위 혜성 3개 생성
- 혜성 등급에 따른 자원 노드 수 결정
- 철, 구리, 얼음 자원의 노드 수, 총 매장량 무작위 배정

### 2. 플레이어 데이터 시스템
- 플레이어 이름 및 자금 관리
- 소유 혜성 목록 관리
- 선택한 혜성을 플레이어 데이터에 등록

### 3. 건물 건설 및 관리 시스템
- 혜성에 건설될 객체 생성
- 혜성에 건설 가능 / 불능 판정 생성
- 소유한 혜성의 건물 데이터 등록후 출력
- 건설된 건물의 Type에 따른 건물기능 결정

## 구현 진행 상황
- [x] [혜성 데이터 클래스 구현](https://github.com/EumSeungJae/Comet_Foundry/blob/main/project_CF/Assets/CometFoundry/TestScripts/Data/TestCometData.cs)
- [x] [혜성 3개 무작위 생성 및 자원 노드, 매장량 생성](https://github.com/EumSeungJae/Comet_Foundry/blob/main/project_CF/Assets/CometFoundry/TestScripts/Data/TestCometGenerate.cs)
- [x] [혜성 선택 및 소유 데이터 연결](https://github.com/EumSeungJae/Comet_Foundry/blob/main/project_CF/Assets/CometFoundry/TestScripts/Data/TestCometGenerate.cs)
- [x] [혜성 탐사 UI 구현]
- [ ] [자원 채굴 시스템 구현]
