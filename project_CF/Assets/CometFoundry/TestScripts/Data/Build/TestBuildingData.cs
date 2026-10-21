using UnityEngine;

public enum BuildType
{
    Miner,      // 채굴기
    Smelter,    // 제련소
    Assembler,  // 조립기
    Battery,    // 발전기
    Storage     // 창고
}

[System.Serializable]
public class TestBuildingData
{
    // 건물의 이름 과 타입
    public int bildingID;
    public string buildingName;
    public BuildType buildingType;
    
    public int buildingCost;


    // public int buildingLevel; 차후개발

}
