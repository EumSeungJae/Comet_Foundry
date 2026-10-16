using System;
using UnityEngine;

using System.Collections.Generic;



[Serializable]
public class TestCometData
{
    // 혜성 기본 스테이터스
    public int cometID;
    public string cometName;

    // 혜성의 등급
    public int cometGrade;

    // 혜성 보유 자원 노드
    public int resourceNodeCount;
    public int[] totalResourceAmount = { 0, 0, 0, 0, 0 };

    // 혜성 보유자원 총량
    public int ironAmount;
    public int copperAmount;
    public int iceAmount;
}

/*
 * 혜성의 아이디, 코드, 등급
 * 
 * 혜성의 넘버링은 혜성이름이 중복되지 않게 하기위한 조치.
 * 혜성의 자원매장량은 차후 등급 시스템을 넣어 매장량 조절을 할것이다.
 * 
 * 임시 객체의 데이터로 자원을 설정.
 */