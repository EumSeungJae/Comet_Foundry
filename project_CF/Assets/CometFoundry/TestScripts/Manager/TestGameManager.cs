using System.Collections.Generic;
using UnityEngine;

public class TestGameManager : MonoBehaviour
{
    TestPlayerData mainPlayerData = new TestPlayerData();
    void Start()
    {
        if(mainPlayerData.userSetting == false)
        {
            TestCreateUserSet(mainPlayerData);
        }

        if (mainPlayerData == null)
        {
            Debug.Log("플레이어 데이터 없습니다.");
            return;
        }

        if (mainPlayerData.userComet.Count == 0)
        {
            Debug.Log("보유한 혜성이 없습니다.");
            return;
        }

    }
    
    void TestCreateUserSet(TestPlayerData player)
    {
        player.userSetting = true;
        player.TestAddMoney(50000);

        mainPlayerData = new TestPlayerData();
        mainPlayerData.userComet = new List<TestCometData>();

        TestCometData startComet = new TestCometData();

        startComet.cometID = 01;
        startComet.cometName = "헤스티아 - " + startComet.cometID;

        startComet.resourceNodeCount = 5;

        startComet.totalResourceAmount[0] = 1;
        startComet.totalResourceAmount[1] = 1;
        startComet.totalResourceAmount[2] = 2;
        startComet.totalResourceAmount[3] = 2;
        startComet.totalResourceAmount[4] = 3;

        startComet.ironAmount = 50000;
        startComet.copperAmount = 30000;
        startComet.iceAmount = 10000;

        mainPlayerData.userComet.Add(startComet);

        mainPlayerData.userSetting = true;
    }

}
