using UnityEngine;
using System.Collections.Generic;
using UnityEditor;


public class TestCometGenerate : MonoBehaviour
{
    void Start()
    {
        TestPlayerData testPlayerData = new TestPlayerData();
    }


    public List<TestCometData> CreateCometData()
    {
        List<TestCometData> comets = new List<TestCometData>();


        // 3개의 혜성 객체 생성
        for (int i = 0; i < 3; i++)
        {
            TestCometData comet = new TestCometData();
            comet.cometID = i + 1;
            comet.cometName = "comet - " + comet.cometID;

            // 혜성의 등급을 5개로 나눈다.
            comet.cometGrade = Random.Range(0, 5);

            // 혜성의 등급을 매개로 혜성에 생성될 자원노드의 총 수량을 정한다.
            switch (comet.cometGrade)
            {
                case 0:
                    comet.resourceNodeCount = 5;
                    break;
                case 1:
                    comet.resourceNodeCount = 4;
                    break;
                case 2:
                    comet.resourceNodeCount = 3;
                    break;
                case 3:
                    comet.resourceNodeCount = 2;
                    break;
                case 4:
                    comet.resourceNodeCount = 1;
                    break;
            }

            // 혜성의 총 자원노드의 수를 가지고 배열로 자원을 정한다.
            // 이후 랜덤함수를 이용하여 객체의 각 자원매장량에 덧셈을 더한다.
            for (int y = 0; y < comet.resourceNodeCount; y++)
            {
                // 총 자원노드의 수에따른 자원노드 생성준비
                int Nodetype = Random.Range(1, 4);
                comet.totalResourceAmount[y] = Nodetype;

                // 1 = iron, 2 = copper, 3 = ice
                // 각 배열의 번호에 맞는 자원을 랜덤으로 추가한다.
                switch (comet.totalResourceAmount[y])
                {
                    case 0:
                        break;
                    case 1:
                        comet.ironAmount = comet.ironAmount + Random.Range(2500, 5000);
                        break;
                    case 2:
                        comet.copperAmount = comet.copperAmount + Random.Range(2500, 5000);
                        break;
                    case 3:
                        comet.iceAmount = comet.iceAmount + Random.Range(2500, 5000);
                        break;
                }
            }

            comet.cometWidth = Random.Range(10, 19);
            comet.cometLength = Random.Range(10, 19);

            comets.Add(comet);
        }

        // 리스트의 카운트(생성된 혜성객체의 수)에 따른 매장량 출력
        for (int i = 0; i < comets.Count; i++)
        {
            Debug.Log(comets[i].cometName);
            Debug.Log("iron " + comets[i].ironAmount);
            Debug.Log("copper " + comets[i].copperAmount);
            Debug.Log("ice " + comets[i].iceAmount);
        }

        return comets;
    }

    // 혜성을 선택후 남은 혜성의 데이터를 정리하기 위한 함수.
    public void selectSaveCometData(TestPlayerData player, List<TestCometData>comets, int selectedIndex)
    {

        // 차후 에러방지.
        if (selectedIndex < 0 || selectedIndex >= comets.Count)
        {
            Debug.LogError("잘못된 혜성 선택");
            return;
        }

        // 유저의 혜성리스트에 선택된 혜성 등록.
        player.userComet.Add(comets[selectedIndex]);

        // 선택하지 않은 혜성 후보 정리
        comets.Clear();



        // 차후 혜성의 영구 선택의 밑준비
        // while (true){}
        
    }

    public void selectDeleteCometData(TestPlayerData player, int selectedIndex)
    {
        // 삭제할 인덱스 검사
        // 생성된 혜성이 아니라 이미 있는 혜성들을 가져오기에
        // 함수의 매개변수 List<TestCometData>comets 삭제 후
        // 플레이어 데이터에서 직접 호출

        if (selectedIndex < 0 || selectedIndex >= player.userComet.Count)
        {
            Debug.LogError("잘못된 혜성 선택");
            return;
        }

        Debug.Log("혜성 이름 - " + player.userComet[selectedIndex].cometName + "가 삭제되었습니다.");
        
        // 유저의 보유혜성중 선택된 혜성 삭제.

        // Remove   특정 객체를 찾아서 삭제
        // RemoveAt 지정 인덱스에 있는 객체 삭제

        // 즉 배열자체, 단일객체를 삭제시 Remove, 배열 내부에 있는 내부 객체는 RemoveAt
        player.userComet.RemoveAt(selectedIndex);

    }


}

/*
 * 2026-10-16
 * 목표
 * 혜성의 생성 및 혜성 데이터에 랜덤값을 입력하기위한 준비
 * 
 * 1차 테스트 혜성의 객체 단일생성 성공
 * 2차 테스트 혜성의 객체 다중생성 실패 - 원인 : 반복문의 조건 값의 설정실패.
 * 3차 테스트 혜성의 객체 다중생성 성공 - 개선후 정상적으로 출력이 되는 결과
 * 
 * 해당 코드를 createCometData 함수로 변경.
 * 
 * 2026-10-17
 * 목표
 * 혜성의 생성후 선택된 혜성이 플레이어 데이터로 이동되며 나머지 혜성의 데이터는 삭제한다.
 * 
 * 
 */