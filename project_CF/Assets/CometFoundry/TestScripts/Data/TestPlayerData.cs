using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;


[Serializable]

public class TestPlayerData
{
    // 유저의 이름과 보유자금
    public string userName;
    public int userMoeny;


    // 유저의 보유혜성목록
    public List<TestCometData> userComet = new List<TestCometData>();


}

/*
 * 2026-10-17
 * 테스트 플레이어 데이터의 역할
 * 
 * 유저가 소유한 자금, 혜성, 수송선, 아이템 등을 저장하는 역할을 수행
 * 
 * 1차 테스트 준비
 * 혜성의 1~3의 혜성을 생성후 하나의 혜성을 목록에 넣고 선택되지 않은 혜성의 데이터는 삭제
 * 유저의 혜성보유 목록에 선택된 혜성을 저장 및 보존.
 * 
 * 
 */