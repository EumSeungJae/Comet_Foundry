using UnityEngine;

using System.Collections.Generic;
using UnityEditor;


public class TestCometGenerate : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
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
            for (int y = 0; y > comet.resourceNodeCount; y++)
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
                        comet.ironAmount = comet.ironAmount+Random.Range(2500, 5000);
                        break;
                    case 2:
                        comet.copperAmount = comet.copperAmount + Random.Range(2500, 5000);
                        break;
                    case 3:
                        comet.iceAmount = comet.iceAmount + Random.Range(2500, 5000);
                        break;

                }

            }

            comets.Add(comet);

        }


        // 리스트의 카운트(생성된 혜성객체의 수)에 따른 매장량 출력
        for (int i = 0; i < comets.Count; i++)
        {
            Debug.Log(comets[i].cometName);
            Debug.Log(comets[i].ironAmount);
            Debug.Log(comets[i].copperAmount); 
            Debug.Log(comets[i].iceAmount);
        }
    }


}
