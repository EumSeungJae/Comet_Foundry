using UnityEngine;

public class CometPractice : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CometData comet = new CometData();

        comet.cometID = 1001;
        comet.cometName = "Comet - 001";
        comet.cometGrade = 2;

        comet.ironAmount = 500;
        comet.copperAmount = 300;
        comet.IceAmount = 100;

        PrintCometInfo(comet);
    }

    void PrintCometInfo(CometData comet)
    {
        Debug.Log("Çý¼º ÀÌ¸§ : " + comet.cometName);
        Debug.Log
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
