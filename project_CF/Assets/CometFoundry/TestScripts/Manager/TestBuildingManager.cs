using CometFoundry.Simulation;
using UnityEngine;

public class TestBuildingManager : MonoBehaviour
{
    TestBuilding testBuilding = new TestBuilding();

    public void build(TestCometData comet, TestBuildingData buildingData, int x, int y)
    {
       bool result = testBuilding.CanBuild(comet, x, y);

        if (result == true)
        {
            comet.buildingGrid[x, y] = buildingData;

            Debug.Log("건설 성공");
            Debug.Log("건물 이름 : " + buildingData.buildingName);
            Debug.Log("건설 좌표 : " + x + ", " + y);
        }
        else
        {
            Debug.Log("건설 실패");
        }
    }

    private void Start()
    {
        TestCometData comet = new TestCometData();

        TestPlayerData player = GetComponentInParent<TestPlayerData>();

        comet.cometName = player.userComet[0].cometName;

        comet.cometWidth = player.userComet[0].cometWidth;
        comet.cometLength = player.userComet[0].cometLength;

        comet.buildingGrid = new TestBuildingData[comet.cometWidth, comet.cometLength];

        TestBuildingData building = new TestBuildingData();

        building.buildingName = "drill";

        build(comet, building, 2, 3);

        

    }


}
 