using System;
using UnityEngine;

[Serializable]
public class CometData
{
    // 혜성 기본 스테이터스
    public int cometID;
    public string cometName;
    public int cometGrade;

    // 혜성 보유 자원 노드
    public int ironAmount;
    public int copperAmount;
    public int IceAmount;
}
