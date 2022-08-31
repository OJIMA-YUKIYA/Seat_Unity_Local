//シリアル通信でシグナル＆パラメータ(Pulse)送信する
//.2 パルスのみ送信に変更

using UnityEngine;
using System.Collections;

public class LowerLimbMotionController : MonoBehaviour
{
    public SerialHandler serialHandler;
    
    private float timeElapsed, timeArduino;
    public int numMotor = 4;//モータ数
    //出力パルス（送信）
    public int[] targetPulseUp = new int[4] { 0, 0, 0, 0 };//左ペダル、左スライダ、右ペダル、右スライダ[pulse]
    public int[] targetPulseDown = new int[4] { 0, 0, 0, 0, };//左ペダル、左スライダ、右ペダル、右スライダ[pulse]
    public int[] driveTimeUp = new int[4] { 560, 560, 560, 560 };//左ペダル、左スライダ、右ペダル、右スライダ[pulse]
    public int[] driveTimeDown = new int[4] { 280, 840, 280, 840 };//左ペダル、左スライダ、右ペダル、右スライダ[pulse]



    //ペダル
    [SerializeField, Range(-55, 25)]
    int leftPedalUp = 25 ;
    [SerializeField, Range(-55, 25)]
    int leftPedalDown =  0 ;
    [SerializeField, Range(-55, 25)]
    int rightPedalUp = 25;
    [SerializeField, Range(-55, 25)]
    int rightPedalDown = 0;
    private const float maxAngle = 25f; //[mm]
    private const float minAngle = -55f; //[mm]
    private const float resolutionPedal = 0.0144f; //[degrees/pulse]
    private const float footLength = 155f;
    private float rightPedalUp_f = 0f;
    private float rightPedalDown_f = 0f;
    private float leftPedalUp_f = 0f;
    private float leftPedalDown_f = 0f;
    private float a;
    private float b;
    private float c;
    private float d;
    //スライダ
    [SerializeField, Range(-50, 50)]
    int leftSliderForward = 36;//[mm]
    [SerializeField, Range(-50, 50)]
    int leftSliderBackward = -24;//[mm]
    [SerializeField, Range(-50, 50)]
    int rightSliderForward = 36;//[mm]
    [SerializeField, Range(-50, 50)]
    int rightSliderBackward = -24;//[mm]

    public const float maxPosition = 90f;  //[mm]
    private const float minPosition = -90f;  //[mm]
    private const float resolutionSlider = 0.012f; //[mm/pulse]

    void Start ()
    {
        serialHandler.OnDataReceived += OnDataReceived;
    }
	


    void FixedUpdate()
    {
        a = Mathf.Asin(leftPedalUp / footLength);
        b = Mathf.Asin(leftPedalUp / footLength) * Mathf.Rad2Deg;
        c = Mathf.Asin(leftPedalUp / footLength) * Mathf.Rad2Deg / resolutionPedal;

        if (Input.GetKeyDown(KeyCode.S))
        {
            leftPedalUp_f = -(Mathf.Asin(leftPedalUp / footLength) * Mathf.Rad2Deg / resolutionPedal);
            leftPedalDown_f =-(Mathf.Asin(leftPedalDown / footLength) * Mathf.Rad2Deg) / resolutionPedal;
            rightPedalUp_f = Mathf.Asin(rightPedalUp / footLength) * Mathf.Rad2Deg / resolutionPedal; 
            rightPedalDown_f = Mathf.Asin(rightPedalDown / footLength) * Mathf.Rad2Deg / resolutionPedal;

            targetPulseUp[0] = (int)leftPedalUp_f;//-(Up)
            targetPulseDown[0] = (int)leftPedalDown_f;
            targetPulseUp[1] = (int)(leftSliderForward / resolutionSlider);
            targetPulseDown[1] = (int)(leftSliderBackward / resolutionSlider);
            targetPulseUp[2] = (int)rightPedalUp_f;
            targetPulseDown[2] = (int)rightPedalDown_f;
            targetPulseUp[3] = (int)(rightSliderForward / resolutionSlider);
            targetPulseDown[3] = (int)(rightSliderBackward / resolutionSlider);


            serialHandler.Write("s" +  "," );
            for (int i = 0; i < numMotor; i++)
            {
                // serialHandler.Write(targetPulseUp[i].ToString() + "," + targetPulseDown[i].ToString() + "," + driveTimeUp[i].ToString() + "," + driveTimeDown[i].ToString() + ",");
                serialHandler.Write(targetPulseUp[i].ToString() + "," + targetPulseDown[i].ToString() + "," );
            }
            serialHandler.Write("e");
            Debug.Log("START");
            Debug.Log(leftPedalUp_f);
            Debug.Log(a);
            Debug.Log(b);
            Debug.Log(c);
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {

            serialHandler.Write("l" + ","  + "e");
            Debug.Log("LAST");

        }
        else if (Input.GetKeyDown(KeyCode.U))
        {
            leftPedalUp_f = -(Mathf.Asin(leftPedalUp / footLength) * Mathf.Rad2Deg / resolutionPedal);
            leftPedalDown_f = -(Mathf.Asin(leftPedalDown / footLength) * Mathf.Rad2Deg) / resolutionPedal;
            rightPedalUp_f = Mathf.Asin(rightPedalUp / footLength) * Mathf.Rad2Deg / resolutionPedal;
            rightPedalDown_f = Mathf.Asin(rightPedalDown / footLength) * Mathf.Rad2Deg / resolutionPedal;

            targetPulseUp[0] = (int)leftPedalUp_f;//-(Up)
            targetPulseDown[0] = (int)leftPedalDown_f;
            targetPulseUp[1] = (int)(leftSliderForward / resolutionSlider);
            targetPulseDown[1] = (int)(leftSliderBackward / resolutionSlider);
            targetPulseUp[2] = (int)rightPedalUp_f;
            targetPulseDown[2] = (int)rightPedalDown_f;
            targetPulseUp[3] = (int)(rightSliderForward / resolutionSlider);
            targetPulseDown[3] = (int)(rightSliderBackward / resolutionSlider);

            serialHandler.Write("u" + ",");
            for (int i = 0; i < numMotor; i++) {
                //  serialHandler.Write( targetPulseUp[i].ToString() + "," + targetPulseDown[i].ToString() + "," + driveTimeUp[i].ToString() + "," + driveTimeDown[i].ToString() + ",");
                serialHandler.Write(targetPulseUp[i].ToString() + "," + targetPulseDown[i].ToString() + "," );
            }
            serialHandler.Write("e");
            Debug.Log("Update");
        }else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Quit();
        }
            
        else {
        }
    }

    void OnDataReceived(string message)
    {
        var data = message.Split(new string[] { "," }, System.StringSplitOptions.None);
       
        // if (data.Length != 2) return;
        /*
        float tmp = timeArduino;
        if (!float.TryParse(data[0], out tmp))
        {
            return;
        }
        timeArduino = tmp;
        */
        
        try
        {
            Debug.Log(message);
             //Debug.Log(data.Length.ToString() + ", " + data[0] + ", " + data[1]+"," + data[2] + "," + data[3]);
             
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
        
    }
    void Quit()
    {
        serialHandler.Close();



        UnityEditor.EditorApplication.isPlaying = false;

        UnityEngine.Application.Quit();

    }
}
