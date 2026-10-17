using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TestCometFoundryManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 시작시 기본 혜성 부여.
        TestPlayerData testPlayer = new TestPlayerData();

        testPlayer.userName = "잉어킹";
        testPlayer.userMoeny = 50000;

        List<TestCometData> comets = new List<TestCometData>();
        TestCometData comet = new TestCometData();

        comet.cometID = 00&2;
        comet.cometName = "갸라도스 - " + comet.cometID;

        comet.resourceNodeCount = 5;
        
        comet.ironAmount = 50000;
        comet.copperAmount = 30000;
        comet.iceAmount = 10000;

        comets.Add(comet);
        testPlayer.userComet = comets;

        // 시작시 저장된 혜성을 보유 혜성 목록에서 데이터를 출력하는 실험.
        Debug.Log("이름 - " + testPlayer.userName);
        Debug.Log("혜성 이름 - " + testPlayer.userComet[0].cometName);
        Debug.Log("철 자원- " + testPlayer.userComet[0].ironAmount);
        Debug.Log("구리 자원- " + testPlayer.userComet[0].copperAmount);
        Debug.Log("얼음 자원- " + testPlayer.userComet[0].iceAmount);



    }

}
