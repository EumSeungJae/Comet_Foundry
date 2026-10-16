using UnityEngine;


public class TestGenerateComet : MonoBehaviour
{
    public TestCometData TestCreateComet()
    {
        TestCometData comet = new TestCometData();

        comet.cometID = Random.Range(1000, 9999);
        comet.cometName = "Comet-" + comet.cometID;
        comet.cometGrade = Random.Range(1, 4);

        comet.ironAmount = Random.Range(100, 501);
        comet.copperAmount = Random.Range(50, 301);
        comet.IceAmount = Random.Range(20, 201);

        return comet;
    }
    

}


