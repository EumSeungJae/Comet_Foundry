using CometFoundry.Simulation;
using UnityEditor.Animations;
using UnityEngine;

public class TestBuilding
{
    // 그리드의 건설 데이터를 확인하고 건설이 값을 반환한다.
    // 조건 1. 그리드외가 아닐것.
    // 조건 2. 그리드의 건설값이 없을것.
    // 조건 3. 그리드 위일것.

    public bool CanBuild(TestCometData comet, int x ,int y)
    {
        if (x<0||x>=comet.cometWidth) return false;
        if (x<0||x>=comet.cometLength) return false;

        if (comet.buildingGrid[x, y] != null) return false;

        return true;
    }



}
