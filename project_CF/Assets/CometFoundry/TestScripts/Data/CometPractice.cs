using UnityEngine;

public class CometPractice : MonoBehaviour
{
     // 혜성의 객체를 만드는 코드
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

        /*
         * 혜성의 아이디, 코드, 크기
         * 
         * 혜성의 넘버링은 혜성이름이 중복되지 않게 하기위한 조치.
         * 혜성의 자원매장량은 차후 등급 시스템을 넣어 매장량 조절을 할것이다.
         * 
         * 임시 객체의 데이터로 자원을 설정.
         */
    }

    void PrintCometInfo(CometData comet)
    {
        Debug.Log("혜성 이름 : " + comet.cometName);
        Debug.Log("혜성 등급 : " + comet.cometGrade);
        Debug.Log("철 매장량 : " + comet.ironAmount);
        Debug.Log("구리 매장량 : " + comet.copperAmount);
    }

}
