using CometFoundry.Simulation;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TestCometManager : MonoBehaviour
{

    private TestPlayerData testPlayer;
    private TestCometGenerate cometGenerate;


    // 탐사후보 리스트
    private List<TestCometData> cometCandidates;

    public TestPlayerData TestGetCometData(TestPlayerData userData, int index)
    {
        TestPlayerData copyData = new TestPlayerData();

        copyData.userComet[index].cometID = userData.userComet[index].cometID;
        copyData.userComet[index].cometName = userData.userComet[index].cometName;
        copyData.userComet[index].cometWidth = userData.userComet[index].cometWidth;
        copyData.userComet[index].cometLength = userData.userComet[index].cometLength;

        copyData.userComet[index].ironAmount = userData.userComet[index].ironAmount;
        copyData.userComet[index].copperAmount = userData.userComet[index].copperAmount;
        copyData.userComet[index].iceAmount = userData.userComet[index].iceAmount;

        return copyData;
    }


    public void OnGenerateButton()
    {
        // 혜성 생성버튼 함수
        if (cometGenerate == null)
        {
            Debug.LogWarning("생성클래스 연결되지않음");
        }

        cometCandidates = cometGenerate.CreateCometData();

        Debug.Log("혜성탐사 실행");

        for (int i = 0; i < cometCandidates.Count; i++)
        {
            Debug.Log("발견 : " + cometCandidates[i].cometName);
        }

    }

    public void OnSelectButton(int selectedButtonIndex)
    {
        // 혜성 지정저장 버튼
        if (cometGenerate == null || cometCandidates == null)
        {
            Debug.LogWarning("저장대상의 누락");
            return;
        }

        cometGenerate.selectSaveCometData(testPlayer, cometCandidates, selectedButtonIndex);

    }

    public void OnDeleteButton(int selectedDeleteIndex)
    {
        // 혜성 지정삭제 버튼
        if (cometGenerate == null)
        {
            Debug.LogWarning("저장대상의 누락");
            return;
        }

        cometGenerate.selectDeleteCometData(testPlayer, selectedDeleteIndex);

    }
}
