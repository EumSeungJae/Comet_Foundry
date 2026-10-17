using CometFoundry.Simulation;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TestCometFoundryManager : MonoBehaviour
{

    private TestPlayerData testPlayer;
    private TestCometGenerate cometGenerate;


    void Start()
    {
        // 시작시 플레이어 데이터 생성 및 기본 혜성 부여.
        TestPlayerData testPlayer = new TestPlayerData();

        testPlayer.userName = "잉어킹";
        testPlayer.userMoeny = 50000;

        testPlayer.userComet = new List<TestCometData>();

        //List<TestCometData> comets = new List<TestCometData>();

        TestCometData startComet = new TestCometData();

        startComet.cometID = 00;
        startComet.cometName = "갸라도스 - " + startComet.cometID;

        startComet.resourceNodeCount = 5;

        startComet.ironAmount = 50000;
        startComet.copperAmount = 30000;
        startComet.iceAmount = 10000;

        testPlayer.userComet.Add(startComet);

        // 학습의 과정중 스크립트간 상속이 아닌 GameObject에 두 스크립트를 부착하여
        // GetComponent 로 가져오는 방법을 학습

        // 같은 GameObject의 생성기 가져오기
        cometGenerate = GetComponent<TestCometGenerate>();

        // 컴포넌트 연동확인
        if(cometGenerate == null)
        {
            Debug.LogError("TestCometGenerate 컴포넌트가 없습니다.");
            return;
        }

        // 혜성 다중생성
        List<TestCometData> cometIndex = cometGenerate.CreateCometData();

        // 테스트 혜성 중 두번째 혜성 선택
        cometGenerate.selectSaveCometData(testPlayer, cometIndex, 1);


        // 시작시 저장된 혜성을 보유 혜성 목록에서 데이터를 출력하는 실험.
        Debug.Log("이름 - " + testPlayer.userName);
        Debug.Log("혜성 이름 - " + testPlayer.userComet[0].cometName);
        Debug.Log("철 자원- " + testPlayer.userComet[0].ironAmount);
        Debug.Log("구리 자원- " + testPlayer.userComet[0].copperAmount);
        Debug.Log("얼음 자원- " + testPlayer.userComet[0].iceAmount);

        Debug.Log("혜성 이름 - " + testPlayer.userComet[1].cometName);
        Debug.Log("철 자원- " + testPlayer.userComet[1].ironAmount);
        Debug.Log("구리 자원- " + testPlayer.userComet[1].copperAmount);
        Debug.Log("얼음 자원- " + testPlayer.userComet[1].iceAmount);

        cometGenerate.selectDeleteCometData(testPlayer, 1);


    }

}
