using UnityEngine;

using System.Collections.Generic;
using UnityEditor;


public class TestCometGenerate : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        List<TestCometData> comets = new List<TestCometData>();

        // 첫번째 혜성 생성
        TestCometData cometA = new TestCometData();
        cometA.cometName = "Comet-A";
        cometA.ironAmount = 100;

        // 두번째 혜성 생성
        TestCometData cometB = new TestCometData();
        cometB.cometName = "Comet-B";
        cometB.ironAmount = 50;

        // 리스트에 추가
        comets.Add(cometA);
        comets.Add(cometB);

        // 리스트의 카운트(생성된 혜성객체의 수)에 따른 매장량 출력
        for (int i = 0; i < comets.Count; i++)
        {
            Debug.Log(comets[i].cometName);
            Debug.Log(comets[i].ironAmount);

        }
    }


}
