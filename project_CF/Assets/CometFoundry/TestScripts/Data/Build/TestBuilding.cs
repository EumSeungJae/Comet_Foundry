using UnityEditor.Animations;
using UnityEngine;

public class TestBuilding : MonoBehaviour
{
    private bool[,] buildingGrid;

    // 함수의 목적
    // 생성된 혜성의 크기값을 가져와서 반영한다.
    // 2차원 배열을 만들어 반복문을 사용하여 Prefab을 출력한다.
    public void LoadBuildingGrid(int width, int length)
    {
        buildingGrid = new bool[width, length];

        for (int y = 0; y < length; y++)
        {


            for (int x = 0; x < width; x++)
            {
                buildingGrid[x, y] = false;
            }
        }

        Debug.Log("건설 그리드 생성");
    }

    public void LoadBilding()
    {

    }

    public bool CanBuild(int x, int y)
    {
        if (buildingGrid == null) return false;

        if (x < 0 || y < 0 || 
            x >= buildingGrid.GetLongLength(0) || 
            y >= buildingGrid.GetLongLength(1))
        return false;

        return !buildingGrid[x, y];
    }



}
